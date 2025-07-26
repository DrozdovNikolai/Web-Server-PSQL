using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PostgresControlPlane.Api.Data;
using PostgresControlPlane.Api.Models;

namespace PostgresControlPlane.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"control-plane-tests-{Guid.NewGuid():N}";
    private readonly string _storageRoot = Path.Combine(
        Path.GetTempPath(),
        $"postgres-control-plane-tests-{Guid.NewGuid():N}");
    private readonly ServiceProvider _databaseServices = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:MainDatabase", "Host=localhost;Database=unused");
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-with-more-than-32-characters");
        builder.UseSetting("Jwt:Issuer", "PostgresControlPlane.Tests");
        builder.UseSetting("Jwt:Audience", "PostgresControlPlane.Tests.Client");
        builder.UseSetting("Jwt:AccessTokenLifetimeMinutes", "15");
        builder.UseSetting("Hosting:PathBase", string.Empty);
        builder.UseSetting("Kubernetes:Enabled", "false");
        builder.UseSetting("Kubernetes:Namespace", "postgres-control-tests");
        builder.UseSetting("Kubernetes:WorkloadImage", "postgres-control-plane:1.0.0");
        builder.UseSetting("Storage:RootPath", _storageRoot);
        builder.UseSetting("Storage:MaxFileSizeBytes", "1048576");
        builder.UseSetting("DatabaseInitialization:ApplyMigrations", "false");
        builder.UseSetting("BootstrapAdmin:Enabled", "false");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MainDatabase"] = "Host=localhost;Database=unused",
                ["Jwt:SigningKey"] = "integration-test-signing-key-with-more-than-32-characters",
                ["Jwt:Issuer"] = "PostgresControlPlane.Tests",
                ["Jwt:Audience"] = "PostgresControlPlane.Tests.Client",
                ["Jwt:AccessTokenLifetimeMinutes"] = "15",
                ["Hosting:PathBase"] = string.Empty,
                ["Kubernetes:Enabled"] = "false",
                ["Kubernetes:Namespace"] = "postgres-control-tests",
                ["Kubernetes:WorkloadImage"] = "postgres-control-plane:1.0.0",
                ["Storage:RootPath"] = _storageRoot,
                ["Storage:MaxFileSizeBytes"] = "1048576",
                ["DatabaseInitialization:ApplyMigrations"] = "false",
                ["BootstrapAdmin:Enabled"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DataContext>();
            services.RemoveAll<DbContextOptions<DataContext>>();
            services.AddDbContext<DataContext>(options => options
                .UseInMemoryDatabase(_databaseName)
                .UseInternalServiceProvider(_databaseServices));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _databaseServices.Dispose();
            if (Directory.Exists(_storageRoot))
            {
                Directory.Delete(_storageRoot, recursive: true);
            }
        }
    }

    public async Task<int> SeedUserAsync(string username, string password, string roleName)
    {
        _ = CreateClient();
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var role = await context.Roles.SingleOrDefaultAsync(candidate => candidate.Name == roleName);
        if (role is null)
        {
            role = new Role { Name = roleName };
            context.Roles.Add(role);
            await context.SaveChangesAsync();
        }

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4),
            UserRoles = [new UserRole { RoleId = role.Id, Role = role }]
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }
}
