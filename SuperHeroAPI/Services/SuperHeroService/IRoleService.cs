namespace SuperHeroAPI.Services.SuperHeroService
{
    public interface IRoleService
    {
       



        Task<List<Role>> GetAllRoles();

        Task<Role?> GetSingleRole(int id);

        Task<List<Role>> AddRole(Role role);

        Task<List<Role>?> UpdateRole(int id, Role request);
        Task<List<Role>?> DeleteRole(int id);
    }
}
