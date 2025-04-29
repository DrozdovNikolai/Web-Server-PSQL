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
        
        // Configure SwaggerUI to use the correct base path for API calls
        options.ConfigObject.Urls = new[] 
        {
            new { url = "./v1/swagger.json", name = "SuperHeroAPI V1" }
        };
    });
}
app.UseSwagger(options =>
{
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
    options.RouteTemplate = "swagger/{documentName}/swagger.json";
});

// Add middleware to serve custom Swagger assets
app.Use(async (context, next) =>
{
    if (context.Request.Path.Value?.Contains("/swagger/index.html") == true)
    {
        // Get current path to determine the base URL
        string path = context.Request.Path.Value ?? "";
        string basePath = "";
        
        var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        if (pathSegments.Length >= 2 && pathSegments[1] == "server")
        {
            basePath = $"/{pathSegments[0]}";
        }
        else if (pathSegments.Length >= 4 && pathSegments[1] == "containers" && pathSegments[3] == "server")
        {
            basePath = $"/{pathSegments[0]}/containers/{pathSegments[2]}";
        }
        
        // Read the existing response
        var originalBodyStream = context.Response.Body;
        using (var responseBody = new MemoryStream())
        {
            context.Response.Body = responseBody;
            
            await next.Invoke();
            
            // Reset the position to the beginning
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseBodyText = new StreamReader(responseBody).ReadToEnd();
            
            // Inject our custom script to fix base URL
            var script = $@"
<script type='text/javascript'>
    (function() {{
        // Override fetch and XHR to fix base URLs
        const basePath = '{basePath}/server';
        
        // Store original fetch
        const originalFetch = window.fetch;
        window.fetch = function(input, init) {{
            if (typeof input === 'string' && input.startsWith('/api/')) {{
                input = basePath + input;
            }}
            return originalFetch.call(this, input, init);
        }};
        
        // Store original XMLHttpRequest open
        const originalOpen = XMLHttpRequest.prototype.open;
        XMLHttpRequest.prototype.open = function(method, url, async, user, password) {{
            if (typeof url === 'string' && url.startsWith('/api/')) {{
                url = basePath + url;
            }}
            return originalOpen.call(this, method, url, async, user, password);
        }};
        
        // Also fix the SwaggerUI URLs once loaded
        const checkInterval = setInterval(function() {{
            if (window.ui) {{
                clearInterval(checkInterval);
                console.log('SwaggerUI loaded, fixing URLs');
                
                const baseUrl = window.location.protocol + '//' + window.location.host + basePath + '/server';
                window.ui.specActions.updateBaseUrl(baseUrl);
            }}
        }}, 100);
    }})();
</script>
";
            
            // Insert our script before the closing </body> tag
            responseBodyText = responseBodyText.Replace("</body>", script + "</body>");
            
            // Write the modified response
            var modifiedResponseBytes = System.Text.Encoding.UTF8.GetBytes(responseBodyText);
            context.Response.Body = originalBodyStream;
            await context.Response.Body.WriteAsync(modifiedResponseBytes, 0, modifiedResponseBytes.Length);
        }
        return;
    }
    
    await next.Invoke();
});

app.UseCors(MyAllowAllOrigins);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseRequestLogging();

app.MapControllers();

app.Run();
