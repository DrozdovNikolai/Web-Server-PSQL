global using SuperHeroAPI.Models;
global using PostgreSQL.Data;
global using Npgsql.EntityFrameworkCore.PostgreSQL;

using SuperHeroAPI.Services.SuperHeroService;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using DotNetEnv;

using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Serilog;
using SuperHeroAPI.Data;
using Microsoft.AspNetCore.Identity;
using DynamicAuthorization.Mvc.Core.Extensions;
using DynamicAuthorization.Mvc.JsonStore.Extensions;
using DynamicAuthorization.Mvc.Ui;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using OfficeOpenXml;
using SuperHeroAPI.Middleware;
using SuperHeroAPI.Services;
using SuperHeroAPI.Services.ContainerService;

// Р’РєР»СЋС‡Р°РµРј РїРѕРґРґРµСЂР¶РєСѓ СѓСЃС‚Р°СЂРµРІС€РµРіРѕ РїРѕРІРµРґРµРЅРёСЏ РґР»СЏ timestamp Р±РµР· time zone
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Load environment variables from .env file
Env.Load();

ExcelPackage.License.SetNonCommercialPersonal("Nikolai");

var builder = WebApplication.CreateBuilder(args);
var MyAllowAllOrigins = "_myAllowAllOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowAllOrigins,
                      policy =>
                      {
                          policy.AllowAnyOrigin()
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

// Add services to the container.
var mvcBuilder = builder.Services.AddControllersWithViews();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateAudience = false,
        ValidateIssuer = false,
        IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String("B3N4rqHgVy9FREwfnK25in0GSfk8NyNz7Vz17gc5vL4="))
    };

    options.Events = new JwtBearerEvents
    {
        //           .
        OnMessageReceived = context =>
        {
            //   OPTIONS,   
            if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                //   context.Fail(),    
                return Task.CompletedTask;
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            //   OPTIONS,   
            if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
                return;

            var tokenString = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            Console.WriteLine($"Extracted token: {tokenString}");

            if (string.IsNullOrEmpty(tokenString))
            {
                context.Fail("No token provided.");
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<DataContext>();
            var authToken = await db.UserAuthTokens.FirstOrDefaultAsync(t => t.Token == tokenString);

            if (authToken?.Token == null)
            {
                Console.WriteLine("Token not found in database.");
                context.Fail("Token not found in database.");
                return;
            }
            if (authToken.IsRevoked)
            {
                Console.WriteLine("Token has been revoked.");
                context.Fail("Token has been revoked.");
                return;
            }
            if (authToken.Expiration < DateTime.UtcNow)
            {
                Console.WriteLine("Token has expired.");
                context.Fail("Token has expired.");
                return;
            }
            Console.WriteLine("Token is valid.");
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        }
    };
});

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration).CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddScoped<ISuperHeroService, SuperHeroService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermission, PermissionService>();
builder.Services.AddScoped<IContainerService, ContainerService>();
builder.Services.AddHostedService<DatabaseSetupService>();
builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql($"Host={Env.GetString("DB_HOST")}:{Env.GetString("DB_PORT")}; Database={Env.GetString("DB_NAME")}; Username={Env.GetString("DB_USER")}; Password={Env.GetString("DB_PASSWORD")}")
           .UseSnakeCaseNamingConvention());
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AuditoriumsPolicy", policy =>
        policy.RequireAssertion(context =>
        {

            return context.User.HasClaim(claim => claim.Type == "Permission" && claim.Value == "dickandballs");
        }));
});

var app = builder.Build();

// Set the base path for the application
app.UsePathBase("/server");
app.UseRouting();

if (true)
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "swagger/{documentName}/swagger.json";
        
        options.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
        {
            // Extract the container name from the path if it exists
            string path = httpReq.Path.Value ?? "";
            string basePath = "";

            // Handle two possible path formats:
            // 1. /{app}/server/swagger/... (e.g., /ums/server/swagger)
            // 2. /{app}/containers/{container-name}/server/swagger/... (e.g., /ums/containers/tsts8/server/swagger)
            var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (pathSegments.Length >= 2 && pathSegments[1] == "server")
            {
                // Format 1: /{app}/server/...
                basePath = $"/{pathSegments[0]}";
            }
            else if (pathSegments.Length >= 4 && pathSegments[1] == "containers" && pathSegments[3] == "server")
            {
                // Format 2: /{app}/containers/{container-name}/server/...
                basePath = $"/{pathSegments[0]}/containers/{pathSegments[2]}";
            }

            var serverUrl = $"{httpReq.Scheme}://{httpReq.Host.Value}{basePath}/server";
            swaggerDoc.Servers = new List<OpenApiServer>
            {
                new OpenApiServer { Url = serverUrl }
            };
        });
    });

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("v1/swagger.json", "SuperHeroAPI V1");
        options.RoutePrefix = "swagger";
        
        // Add DocExpansion to show all endpoints expanded
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        
        // Add custom JavaScript to fix API URLs
        options.HeadContent = @"
            <script>
                window.onload = function() {
                    // Fix for Swagger UI's client URLs when running under container path
                    const oldFetch = window.fetch;
                    window.fetch = function(url, init) {
                        if (typeof url === 'string' && url.includes('/api/')) {
                            // Check if we're in a container URL
                            if (window.location.pathname.includes('/containers/')) {
                                const segments = window.location.pathname.split('/');
                                if (segments.length >= 4) {
                                    const containerPath = `/${segments[1]}/containers/${segments[3]}`;
                                    // Only modify URLs that don't already include the container path
                                    if (!url.startsWith(containerPath)) {
                                        url = url.replace('/server', `${containerPath}/server`);
                                    }
                                }
                            } else if (window.location.pathname.includes('/server/')) {
                                const appSegment = window.location.pathname.split('/')[1];
                                // Only modify URLs that don't already include the app path
                                if (!url.startsWith(`/${appSegment}`)) {
                                    url = url.replace('/server', `/${appSegment}/server`);
                                }
                            }
                        }
                        
                        return oldFetch.call(window, url, init);
                    };
                };
            </script>";
    });
}


app.UseCors(MyAllowAllOrigins);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseRequestLogging();

app.MapControllers();

app.Run();