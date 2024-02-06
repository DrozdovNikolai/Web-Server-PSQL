namespace SuperHeroAPI.Services.SuperHeroService
{
    public class PermissionService : IPermission
    {
        private readonly DataContext _context;

        public PermissionService(DataContext context)
        {
            _context = context;
        }

      




        public async Task<List<Permission>> AddPermission(Permission permission)
        {
            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();
            return await _context.Permissions.ToListAsync();
        }

        public async Task<List<Permission>?> DeletePermission(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
                return null;

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();

            return await _context.Permissions.ToListAsync();
        }

        public async Task<List<Permission>> GetAllPermissions()
        {
            var permission = await _context.Permissions.ToListAsync();
            return permission;
        }

        public async Task<Permission?> GetSinglePermission(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
            {
                return null;
            }
            return permission;
        }

        public async Task<List<Permission>?> UpdatePermission(int id, Permission request)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
                return null;

            permission.TableName = request.TableName;
            permission.Operation= request.Operation;   
            permission.RoleId= request.RoleId;



            await _context.SaveChangesAsync();

            return await _context.Permissions.ToListAsync();
        }
    }
}

