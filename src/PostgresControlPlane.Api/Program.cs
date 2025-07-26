using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PostgresControlPlane.Api.Configuration;
using PostgresControlPlane.Api.Data;
using PostgresControlPlane.Api.Middleware;
using PostgresControlPlane.Api.Services;
using PostgresControlPlane.Api.Services.DatabaseRoutines;
using PostgresControlPlane.Api.Services.Deployments;
using Serilog;
using Swashbuckle.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options => options.SigningKey.Length >= 32, "JWT signing key must contain at least 32 characters.")
    .ValidateOnStart();

builder.Services
    .AddOptions<StorageOptions>()
    .Bind(builder.Configuration.GetSection(StorageOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<AuditOptions>()
    .Bind(builder.Configuration.GetSection(AuditOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<ProxyOptions>()
    .Bind(builder.Configuration.GetSection(ProxyOptions.SectionName))
    .Validate(options => options.KnownProxies.All(proxy => System.Net.IPAddress.TryParse(proxy, out _)),
        "Every configured trusted proxy must be an IP address.")
    .ValidateOnStart();

var proxyOptions = builder.Configuration
    .GetSection(ProxyOptions.SectionName)
    .Get<ProxyOptions>() ?? new ProxyOptions();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    foreach (var proxy in proxyOptions.KnownProxies)
    {
        options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
    }
});

var storageOptions = builder.Configuration
    .GetSection(StorageOptions.SectionName)
    .Get<StorageOptions>() ?? new StorageOptions();
builder.Services.Configure<FormOptions>(options =>
{
    const long multipartOverheadAllowance = 1024 * 1024;
    options.MultipartBodyLengthLimit = checked(
        storageOptions.MaxFileSizeBytes + multipartOverheadAllowance);
});

builder.Services
    .AddOptions<KubernetesOptions>()
    .Bind(builder.Configuration.GetSection(KubernetesOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options => !options.WorkloadImage.EndsWith(":latest", StringComparison.OrdinalIgnoreCase),
        "Kubernetes workload image must use an immutable version tag or digest.")
    .Validate(options => HasSecurePublicIngressConfiguration(options),
        "A public Kubernetes ingress requires an absolute HTTPS base URL and an existing TLS secret name.")
    .ValidateOnStart();

builder.Services
    .AddOptions<DatabaseInitializationOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseInitializationOptions.SectionName))
    .ValidateOnStart();

builder.Services
    .AddOptions<DatabaseBrowserOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseBrowserOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(HasSafeRoutineConfiguration,
        "Allowed routine schemas must be distinct PostgreSQL identifiers and cannot include the control schema.")
    .ValidateOnStart();

builder.Services
    .AddOptions<BootstrapAdminOptions>()
    .Bind(builder.Configuration.GetSection(BootstrapAdminOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options => !options.Enabled ||
        (options.Username.Trim().Length is >= 3 and <= 100 &&
         options.Password.Length is >= 12 and <= 200),
        "Bootstrap administrator credentials are required when bootstrap is enabled.")
    .ValidateOnStart();

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
        NameClaimType = JwtRegisteredClaimNames.Sub,
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var tokenId = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
            if (string.IsNullOrWhiteSpace(tokenId))
            {
                context.Fail("The token does not identify a session.");
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetRequiredService<DataContext>();
            var timeProvider = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>();
            var now = timeProvider.GetUtcNow().UtcDateTime;

            var sessionIsActive = await dbContext.AuthSessions
                .AsNoTracking()
                .AnyAsync(session =>
                    session.TokenId == tokenId &&
                    !session.IsRevoked &&
                    session.Expiration > now,
                    context.HttpContext.RequestAborted);

            if (!sessionIsActive)
            {
                context.Fail("The session is no longer active.");
            }
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    foreach (var policyName in new[]
    {
        AuthorizationPolicies.IdentityManage,
        AuthorizationPolicies.DatabaseRead,
        AuthorizationPolicies.DatabaseExecute,
        AuthorizationPolicies.DeploymentsManage,
        AuthorizationPolicies.FilesManage,
        AuthorizationPolicies.AuditRead
    })
    {
        options.AddPolicy(policyName, policy =>
            policy.RequireRole(ApplicationRoles.SuperAdministrator));
    }
});

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicies.Frontend, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddControllers();

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Postgres Control Plane API",
        Version = "v1",
        Description = "Role-aware PostgreSQL management and deployment API."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter a valid JWT access token."
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

var connectionString = builder.Configuration.GetConnectionString("MainDatabase");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:MainDatabase is required. Configure it through environment variables or a local secret store.");
}

builder.Services.AddDbContext<DataContext>(options => options
    .UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IDeploymentService, DeploymentService>();
builder.Services.AddSingleton<IExcelReportBuilder, ExcelReportBuilder>();
builder.Services.AddScoped<IDatabaseRoutineService, DatabaseRoutineService>();
builder.Services.AddHostedService<ApplicationInitializationService>();
builder.Services.AddHostedService<AuditRetentionService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds)
                .ToString(CultureInfo.InvariantCulture);
        }

        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests"
            },
            cancellationToken);
    };
    options.AddPolicy(RateLimitPolicies.Authentication, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            static _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddConcurrencyLimiter(RateLimitPolicies.Database, limiter =>
    {
        limiter.PermitLimit = 50;
        limiter.QueueLimit = 100;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

var app = builder.Build();

app.UseForwardedHeaders();

var pathBase = builder.Configuration["Hosting:PathBase"];
if (!string.IsNullOrWhiteSpace(pathBase))
{
    app.UsePathBase(pathBase);
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Correlation-ID", context.TraceIdentifier);
    await next(context);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("v1/swagger.json", "Postgres Control Plane API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors(CorsPolicies.Frontend);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseRequestLogging();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
}).AllowAnonymous();
app.MapControllers();

app.Run();

static bool HasSecurePublicIngressConfiguration(KubernetesOptions options)
{
    if (string.IsNullOrWhiteSpace(options.PublicBaseUrl))
    {
        return true;
    }

    return Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var baseUri) &&
           baseUri.Scheme == Uri.UriSchemeHttps &&
           !string.IsNullOrWhiteSpace(options.IngressTlsSecretName);
}

static bool HasSafeRoutineConfiguration(DatabaseBrowserOptions options)
{
    var schemas = options.AllowedRoutineSchemas;
    return !options.RoutineExecutionEnabled ||
           schemas is { Length: > 0 } &&
           schemas.Distinct(StringComparer.Ordinal).Count() == schemas.Length &&
           schemas.All(schema =>
               !string.Equals(schema, "control", StringComparison.Ordinal) &&
               IsPostgreSqlIdentifier(schema));
}

static bool IsPostgreSqlIdentifier(string value)
{
    if (string.IsNullOrWhiteSpace(value) || value.Length > 63 ||
        !(char.IsAsciiLetter(value[0]) || value[0] == '_'))
    {
        return false;
    }

    return value.Skip(1).All(character =>
        char.IsAsciiLetterOrDigit(character) || character is '_' or '$');
}

public partial class Program;
