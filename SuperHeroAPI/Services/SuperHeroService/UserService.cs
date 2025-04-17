using Microsoft.EntityFrameworkCore;
using SuperHeroAPI.Models; // Пространство имен, где определены модели User, UserAuthToken, UserRole и т.д.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            
            // Find the guest role by name instead of using a hardcoded role ID
            var guestRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "guest");
            
            if (guestRole != null)
            {
                // Assign the guest role to the new user
                user.UserRoles = new List<UserRole> { new UserRole { User = user, RoleId = guestRole.RoleId } };
            }
            else
            {
                // If guest role doesn't exist, log warning and don't assign any role
                Console.WriteLine("Warning: 'guest' role not found. User created without any role.");
                user.UserRoles = new List<UserRole>();
            }

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
            return await _context.Users.ToListAsync();
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

            // Обновление свойств пользователя
            user.Username = request.Username;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Обновление ролей пользователя
            user.UserRoles.Clear();
            foreach (var roleId in request.RoleIds)
            {
                user.UserRoles.Add(new UserRole { User = user, RoleId = roleId });
            }

            await _context.SaveChangesAsync();
            return await _context.Users.ToListAsync();
        }

        /// <summary>
        /// Получает список активных токенов (не отозванных и не просроченных).
        /// </summary>
        public async Task<List<UserAuthToken>> GetActiveTokens()
        {
            return await _context.UserAuthTokens
                .Where(token => !token.IsRevoked && token.Expiration > DateTime.UtcNow)
                .ToListAsync();
        }

        /// <summary>
        /// Деавторизует (отзывает) все активные токены заданного пользователя.
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>True, если найдены и отозваны токены, иначе False</returns>
        public async Task<bool> DeauthorizeUserTokens(int userId)
        {
            var tokens = await _context.UserAuthTokens
                .Where(token => token.UserId == userId && !token.IsRevoked && token.Expiration > DateTime.UtcNow)
                .ToListAsync();

            if (tokens == null || tokens.Count == 0)
                return false;

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
