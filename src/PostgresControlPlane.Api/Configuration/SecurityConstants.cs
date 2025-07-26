namespace PostgresControlPlane.Api.Configuration;

public static class ApplicationRoles
{
    public const string SuperAdministrator = "superadmin";
    public const string DefaultUser = "guests";
}

public static class AuthorizationPolicies
{
    public const string IdentityManage = "Identity.Manage";
    public const string DatabaseRead = "Database.Read";
    public const string DatabaseExecute = "Database.Execute";
    public const string DeploymentsManage = "Deployments.Manage";
    public const string FilesManage = "Files.Manage";
    public const string AuditRead = "Audit.Read";
}

public static class CorsPolicies
{
    public const string Frontend = "Frontend";
}

public static class RateLimitPolicies
{
    public const string Authentication = "Authentication";
    public const string Database = "Database";
}
