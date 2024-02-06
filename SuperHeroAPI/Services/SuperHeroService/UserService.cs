/*
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
            user.Roles = new List<Role> { new Role { RoleName = "guest" }, new Role { RoleName = "otherRole" } };

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

            user.Id = id;  
            user.Username = request.Username;  
            user.Role = request.Role;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);


            await _context.SaveChangesAsync();

            return await _context.Users.ToListAsync();
        }
        

    }
}

*/