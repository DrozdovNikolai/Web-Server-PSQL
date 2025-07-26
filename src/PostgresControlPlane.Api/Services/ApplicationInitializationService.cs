using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PostgresControlPlane.Api.Configuration;
using PostgresControlPlane.Api.Data;

namespace PostgresControlPlane.Api.Services;

public sealed partial class ApplicationInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DatabaseInitializationOptions _databaseOptions;
    private readonly BootstrapAdminOptions _adminOptions;
    private readonly ILogger<ApplicationInitializationService> _logger;

    public ApplicationInitializationService(
        IServiceProvider serviceProvider,
        IOptions<DatabaseInitializationOptions> databaseOptions,
        IOptions<BootstrapAdminOptions> adminOptions,
        ILogger<ApplicationInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _databaseOptions = databaseOptions.Value;
        _adminOptions = adminOptions.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_databaseOptions.ApplyMigrations && !_adminOptions.Enabled)
        {
            return;
        }

        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        if (_databaseOptions.ApplyMigrations)
        {
            LogApplyingMigrations(_logger);
            await context.Database.MigrateAsync(cancellationToken);
        }

        if (!_adminOptions.Enabled)
        {
            return;
        }

        var superAdminRole = await GetOrCreateRole(
            context,
            ApplicationRoles.SuperAdministrator,
            cancellationToken);
        await GetOrCreateRole(context, ApplicationRoles.DefaultUser, cancellationToken);

        var username = _adminOptions.Username.Trim();
        var admin = await context.Users
            .Include(user => user.UserRoles)
            .SingleOrDefaultAsync(user => user.Username == username, cancellationToken);

        if (admin is null)
        {
            admin = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(_adminOptions.Password, workFactor: 12)
            };
            context.Users.Add(admin);
        }

        if (admin.UserRoles.All(userRole => userRole.RoleId != superAdminRole.Id))
        {
            admin.UserRoles.Add(new UserRole
            {
                User = admin,
                RoleId = superAdminRole.Id
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        LogAdministratorConfigured(_logger, username);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<Role> GetOrCreateRole(
        DataContext context,
        string roleName,
        CancellationToken cancellationToken)
    {
        var role = await context.Roles
            .SingleOrDefaultAsync(candidate => candidate.Name == roleName, cancellationToken);
        if (role is not null)
        {
            return role;
        }

        role = new Role { Name = roleName };
        context.Roles.Add(role);
        await context.SaveChangesAsync(cancellationToken);
        return role;
    }

    [LoggerMessage(1, LogLevel.Information, "Applying pending database migrations")]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(2, LogLevel.Information, "Application administrator {Username} is configured")]
    private static partial void LogAdministratorConfigured(ILogger logger, string username);
}
