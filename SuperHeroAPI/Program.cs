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

// Включаем поддержку устаревшего поведения для timestamp без time zone
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
var mvcBuilder= builder.Services.AddControllersWithViews();
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

// Create a middleware to rewrite API URLs in Swagger UI
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    var method = context.Request.Method;
    
    // If this is a request from Swagger UI to the API
    if (path != null && path.Contains("/api/") && context.Request.Headers.Referer.ToString().Contains("/swagger"))
    {
        // Get the referrer URL to determine the base path
        var referer = context.Request.Headers.Referer.ToString();
        var uri = new Uri(referer);
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        // Extract the base path based on known patterns
        string basePath = "";
        if (segments.Length >= 2 && segments[1] == "server")
        {
            // Format: /{app}/server/swagger
            basePath = $"/{segments[0]}/server";
        }
        else if (segments.Length >= 4 && segments[1] == "containers" && segments[3] == "server")
        {
            // Format: /{app}/containers/{container}/server/swagger
            basePath = $"/{segments[0]}/containers/{segments[2]}/server";
        }
        
        // Update the request path if needed
        if (!string.IsNullOrEmpty(basePath) && !path.StartsWith(basePath))
        {
            // Modify the path to include the correct base path
            var apiPath = path.Substring(path.IndexOf("/api/"));
            var newPath = basePath + apiPath;
            context.Request.Path = newPath;
        }
    }
    
    await next();
});

// Set the base path for the application
app.UsePathBase("/server");
app.UseRouting();

if (true)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("./v1/swagger.json", "SuperHeroAPI V1");
        options.RoutePrefix = "swagger";
        
        // Intercept requests to fix API URLs
        options.UseRequestInterceptor(@"
            function(req) {
                // If this is an API request that starts with /api/
                if (req.url && req.url.startsWith('/api/')) {
                    // Get the base path from the current URL (everything before /swagger)
                    const pathname = window.location.pathname;
                    const basePath = pathname.substring(0, pathname.indexOf('/swagger'));
                    
                    // Replace the URL with the correct path
                    req.url = basePath + req.url;
                    console.log('Intercepted request URL:', req.url);
                }
                return req;
            }
        ");
        
        // Also intercept fetch requests (for older Swagger UI versions)
        options.HeadContent = @"
            <script>
                window.addEventListener('load', function() {
                    // Store the original fetch function
                    const originalFetch = window.fetch;
                    
                    // Override the fetch function
                    window.fetch = function(resource, init) {
                        // First handle absolute API URLs
                        if (typeof resource === 'string') {
                            const url = new URL(resource, window.location.origin);
                            const pathname = window.location.pathname;
                            
                            // Check for absolute URLs that need fixing
                            if (url.pathname.startsWith('/api/')) {
                                // Get the base path from the current URL
                                const basePath = pathname.substring(0, pathname.indexOf('/swagger'));
                                
                                // Create the new URL with the correct path
                                url.pathname = basePath + url.pathname;
                                console.log('Redirecting fetch from', resource, 'to', url.toString());
                                
                                // Call the original fetch with the new URL
                                return originalFetch(url.toString(), init);
                            }
                        }
                        
                        // For non-API URLs, just use the original fetch
                        return originalFetch(resource, init);
                    };
                    
                    // Override all button clicks in the Swagger UI
                    const observer = new MutationObserver(function(mutations) {
                        document.querySelectorAll('.opblock-summary-control, .try-out__btn').forEach(button => {
                            if (!button.hasAttribute('url-fixed')) {
                                button.setAttribute('url-fixed', 'true');
                                button.addEventListener('click', function() {
                                    // After a short delay to let the UI update
                                    setTimeout(function() {
                                        // Find the execute button and add our handler
                                        document.querySelectorAll('.execute').forEach(executeBtn => {
                                            if (!executeBtn.hasAttribute('url-fixed')) {
                                                executeBtn.setAttribute('url-fixed', 'true');
                                                executeBtn.addEventListener('click', function() {
                                                    console.log('Execute button clicked - ensuring URL is correct');
                                                });
                                            }
                                        });
                                    }, 500);
                                });
                            }
                        });
                    });
                    
                    // Start observing the document
                    observer.observe(document.documentElement, {
                        childList: true,
                        subtree: true
                    });
                });
            </script>
        ";
    });
}
app.UseSwagger(options =>
{
    options.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
    {
        // Extract the path information
        string path = httpReq.Path.Value ?? "";
        string basePath = "";
        
        // Handle two possible path formats:
        // 1. /{app}/server/swagger/... (e.g., /ums/server/swagger)
        // 2. /{app}/containers/{container-name}/server/swagger/... (e.g., /ums/containers/tsts8/server/swagger)
        var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        if (pathSegments.Length >= 2 && pathSegments[1] == "server")
        {
            // Format 1: /{app}/server/...
            basePath = $"/{pathSegments[0]}/server";
        }
        else if (pathSegments.Length >= 4 && pathSegments[1] == "containers" && pathSegments[3] == "server")
        {
            // Format 2: /{app}/containers/{container-name}/server/...
            basePath = $"/{pathSegments[0]}/containers/{pathSegments[2]}/server";
        }
        else
        {
            // Default fallback
            basePath = "/server";
        }
        
        var serverUrl = $"{httpReq.Scheme}://{httpReq.Host.Value}{basePath}";
        swaggerDoc.Servers = new List<OpenApiServer>
        {
            new OpenApiServer { Url = serverUrl }
        };
        
        // Update operation paths to ensure they're relative to the server URL
        // This is crucial for ensuring API requests go to the correct endpoint
        foreach (var path in swaggerDoc.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                // Add server URL tags that will help client-side JS determine the correct URL
                operation.Value.Tags.Add(new OpenApiTag { Name = $"server-url:{serverUrl}" });
            }
        }
    });
});

app.UseCors(MyAllowAllOrigins);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseRequestLogging();

app.MapControllers();

app.Run();
