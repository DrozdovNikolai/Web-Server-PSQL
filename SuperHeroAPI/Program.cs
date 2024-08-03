global using SuperHeroAPI.Models;
global using PostgreSQL.Data;
global using Npgsql.EntityFrameworkCore.PostgreSQL;

using SuperHeroAPI.Services.SuperHeroService;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;


using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Serilog;
using SuperHeroAPI.Data;
using Microsoft.AspNetCore.Identity;
using DynamicAuthorization.Mvc.Core.Extensions;
using DynamicAuthorization.Mvc.JsonStore.Extensions;
using DynamicAuthorization.Mvc.Ui;




//var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
var builder = WebApplication.CreateBuilder(args);
var MyAllowAllOrigins = "_myAllowAllOrigins";


/*
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                             policy =>
                             {
                                 policy.WithOrigins("http://localhost:8080")
                                        .AllowAnyHeader();
                             });
});
*/
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
builder.Services.AddAuthentication().AddJwtBearer(options =>
{
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    ValidateAudience = false,
    ValidateIssuer = false,
    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String("B3N4rqHgVy9FREwfnK25in0GSfk8NyNz7Vz17gc5vL4="))
    };
});


Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration).CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddScoped<ISuperHeroService, SuperHeroService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermission, PermissionService>();
builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql("Host=195.93.252.168:5432; Database=superherodb; Username=postgres; Password=plsworkpls"));
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseSerilogRequestLogging();

//app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();


//app.UseCors(MyAllowSpecificOrigins);
 app.UseCors(MyAllowAllOrigins);

app.MapControllers();

app.Run();
