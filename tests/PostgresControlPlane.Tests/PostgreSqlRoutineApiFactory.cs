using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PostgresControlPlane.Api.Data;
using PostgresControlPlane.Api.Models;

namespace PostgresControlPlane.Tests;

public sealed class PostgreSqlRoutineApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    private readonly string _storageRoot = Path.Combine(
        Path.GetTempPath(),
        $"postgres-control-plane-routine-tests-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:MainDatabase", connectionString);
        builder.UseSetting(
            "Jwt:SigningKey",
            "routine-integration-signing-key-with-more-than-32-characters");
        builder.UseSetting("Jwt:Issuer", "PostgresControlPlane.RoutineTests");
        builder.UseSetting("Jwt:Audience", "PostgresControlPlane.RoutineTests.Client");
        builder.UseSetting("Jwt:AccessTokenLifetimeMinutes", "15");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MainDatabase"] = connectionString,
                ["Jwt:SigningKey"] = "routine-integration-signing-key-with-more-than-32-characters",
                ["Jwt:Issuer"] = "PostgresControlPlane.RoutineTests",
                ["Jwt:Audience"] = "PostgresControlPlane.RoutineTests.Client",
                ["Jwt:AccessTokenLifetimeMinutes"] = "15",
                ["Hosting:PathBase"] = string.Empty,
                ["Kubernetes:Enabled"] = "false",
                ["Kubernetes:Namespace"] = "postgres-control-routine-tests",
                ["Kubernetes:WorkloadImage"] = "postgres-control-plane:1.0.0",
                ["Storage:RootPath"] = _storageRoot,
                ["Storage:MaxFileSizeBytes"] = "1048576",
                ["DatabaseInitialization:ApplyMigrations"] = "true",
                ["DatabaseBrowser:RoutineExecutionEnabled"] = "true",
                ["DatabaseBrowser:AllowedRoutineSchemas:0"] = "public",
                ["DatabaseBrowser:AllowProcedures"] = "true",
                ["DatabaseBrowser:AllowSecurityDefiner"] = "false",
                ["DatabaseBrowser:RoutineCommandTimeoutSeconds"] = "1",
                ["DatabaseBrowser:MaxRoutineArguments"] = "16",
                ["DatabaseBrowser:MaxRoutineArgumentLength"] = "128",
                ["DatabaseBrowser:MaxRoutineRows"] = "25",
                ["DatabaseBrowser:RoutineExcelExportEnabled"] = "true",
                ["DatabaseBrowser:MaxExcelRows"] = "100",
                ["DatabaseBrowser:MaxExcelColumns"] = "32",
                ["DatabaseBrowser:MaxExcelCells"] = "1000",
                ["DatabaseBrowser:MaxExcelFileSizeBytes"] = "1048576",
                ["BootstrapAdmin:Enabled"] = "false"
            });
        });
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

    public async Task ExecuteSqlAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<T> ExecuteScalarAsync<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("The query returned null."));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_storageRoot))
        {
            Directory.Delete(_storageRoot, recursive: true);
        }
    }
}
