namespace SuperHeroAPI.Services.SuperHeroService
{
    public interface IPermission
    {

        Task<List<Permission>> GetAllPermissions();

        Task<Permission?> GetSinglePermission(int id);

        Task<List<Permission>> AddPermission(Permission permission);

        Task<List<Permission>?> UpdatePermission(int id, Permission permission);
        Task<List<Permission>?> DeletePermission(int id);
    }
}
