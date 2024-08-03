namespace SuperHeroAPI.Services.SuperHeroService
{
    public class UserService : IUserService
    {
        private readonly DataContext _context;

        public UserService(DataContext context)
        {
            _context = context;
        }

        public async Task<List<User>> AddUser(UserDto request)
        {

            User user = new User();
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            user.Username = request.Username;
            user.PasswordHash = passwordHash;
            user.UserRoles = new List<UserRole> { new UserRole { User = user, RoleId = 2 } };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return await _context.Users.ToListAsync();
        }

        public async Task<List<User>?> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return null;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return await _context.Users.ToListAsync();
        }

        public async Task<List<User>> GetAllUsers()
        {
            var users = await _context.Users.ToListAsync();
            return users;
        }

        public async Task<User?> GetSingleUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return null;
            }
            return user;
        }

        public async Task<List<User>?> UpdateUser(int id, UserUpd request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return null;

            // Update user properties
            user.Username = request.Username;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Update user roles
            user.UserRoles.Clear(); 
            foreach (var roleId in request.RoleIds)
            {
                user.UserRoles.Add(new UserRole { User = user, RoleId = roleId });
            }

            await _context.SaveChangesAsync();

            return await _context.Users.ToListAsync();
        }


    }
}