namespace SuperHeroAPI.Services.SuperHeroService
{
    public class RoleService : IRoleService
    {
        private readonly DataContext _context;

        public RoleService(DataContext context)
        {
            _context = context;
        }

 





        public async Task<List<Role>> AddRole(Role role)
        {
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return await _context.Roles.ToListAsync();
        }

        public async Task<List<Role>?> DeleteRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return null;

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return await _context.Roles.ToListAsync();
        }

        public async Task<List<Role>> GetAllRoles()
        {
            var roles = await _context.Roles.ToListAsync();
            return roles;
        }

        public async Task<Role?> GetSingleRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return null;
            }
            return role;
        }

        public async Task<List<Role>?> UpdateRole(int id, Role request)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return null;

            role.RoleName = request.RoleName;

           

            await _context.SaveChangesAsync();

            return await _context.Roles.ToListAsync();
        }
    }
}

