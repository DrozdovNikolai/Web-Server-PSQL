using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace SuperHeroAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DataContext _context;

        // Статический список для отслеживания активных сессий (JWT токенов)
        private static List<ActiveUser> ActiveUsers = new List<ActiveUser>();

        public AuthController(DataContext context)
        {
            _context = context;
        }
    [HttpPost("getRolesFromToken")]
    public async Task<ActionResult> GetRolesFromToken([FromBody] TokenDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Token))
            return BadRequest(new { error = "Token is required" });

        ClaimsPrincipal principal;
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            principal = tokenHandler.ValidateToken(dto.Token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(Convert.FromBase64String("B3N4rqHgVy9FREwfnK25in0GSfk8NyNz7Vz17gc5vL4=")),
                ValidateIssuer           = false,
                ValidateAudience         = false,
                ClockSkew                = TimeSpan.Zero
            }, out SecurityToken validatedToken);
        }
        catch (SecurityTokenException)
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        // Извлекаем имя пользователя из ClaimTypes.Name
        var username = principal.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { error = "Token does not contain a valid Name claim" });

        // Достаём из БД актуальные роли пользователя
        var roles = await _context.Users
            .AsNoTracking()
            .Where(u => u.Username == username)
            .SelectMany(u => u.UserRoles)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.RoleName)
            .ToListAsync();

        return Ok(new { Roles = roles });
    }
        [HttpPost("register")]
        public ActionResult<User> Register(UserDto request)
        {
            // Шаг 1: Создаем нового пользователя и его роль (для демонстрации создаем роль, совпадающую с именем пользователя)
            User user = new User();
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            user.Username = request.Username;
            user.PasswordHash = passwordHash;

            var newRole = new Role
            {
                RoleName = user.Username
            };

            _context.Roles.Add(newRole);
            _context.SaveChanges();

            user.UserRoles = new List<UserRole>
            {
                new UserRole { RoleId = newRole.RoleId }
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(user);
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserReturn>> Login(UserDto request)
        {
            // Поиск пользователя и его ролей (аналогично Database First)
            var user = _context.Users
                .Where(u => u.Username == request.Username)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.PasswordHash,
                    Roles = u.UserRoles.Select(ur => ur.Role.RoleName)
                })
                .FirstOrDefault();

            if (user == null)
            {
                return BadRequest("User not found.");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return BadRequest("Wrong password.");
            }

            // Определяем время истечения токена
            var expiration = DateTime.UtcNow.AddDays(1);
            string token = CreateToken(user, expiration);

            // Создаем запись в таблице user_auth_tokens
            var tokenRecord = new UserAuthToken
            {
                UserId = user.Id,
                Token = token,
                Expiration = expiration,
                IsRevoked = false
            };

            _context.UserAuthTokens.Add(tokenRecord);
            await _context.SaveChangesAsync();

            var userReturn = new UserReturn
            {
                Id = user.Id,
                Username = user.Username,
                Roles = user.Roles.ToList(),
                accessToken = token,
            };

            return Ok(userReturn);
        }


        [HttpPut("update")]
        public async Task<ActionResult<User>> UpdateUser(UpdateUserDto request)
        {
            // Шаг 1: Поиск пользователя по ID
            var user = await _context.Users.FindAsync(request.Id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Шаг 2: Обновляем имя пользователя, если передано новое значение
            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                user.Username = request.Username;
            }

            // Шаг 3: Обновляем пароль, если передано новое значение
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                user.PasswordHash = passwordHash;
            }

            // Шаг 4: Сохраняем изменения в БД
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(user);
        }

        /// <summary>
        /// Метод для получения информации о текущем пользователе (на основе токена).
        /// Возвращает имя и список ролей для построения интерфейса.
        /// </summary>
        [HttpGet("getUser")]
        [Authorize]
        public async Task<ActionResult> GetUser()
        {
            // Получаем имя пользователя из токена
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            // Запрашиваем данные пользователя и его актуальные роли из базы данных
            var user = await _context.Users
                .Where(u => u.Username == username)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    Roles = u.UserRoles.Select(ur => ur.Role.RoleName)
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound("User not found.");
            }

            return Ok(user);
        }


        /// <summary>
        /// Метод для получения всех авторизованных пользователей с активным JWT токеном.
        /// При каждом запросе производится фильтрация просроченных токенов.
        /// </summary>
        [HttpGet("activeUsers")]
        public ActionResult GetActiveUsers()
        {
            // Удаляем записи с истекшим сроком действия токена
            ActiveUsers.RemoveAll(u => u.Expiration < DateTime.Now);
            return Ok(ActiveUsers);
        }

        /// <summary>
        /// Админский метод для деавторизации пользователя по его id.
        /// Удаляет (деактивирует) активные сессии, связанные с данным пользователем.
        /// </summary>
        [HttpPost("deauthorize/{id}")]
        [Authorize(Roles = "Admin")] // Только администратор может вызвать данный метод
        public ActionResult DeauthorizeUser(int id)
        {
            int removedCount = ActiveUsers.RemoveAll(u => u.Id == id);
            if (removedCount == 0)
            {
                return NotFound("No active session found for the given user.");
            }
            return Ok($"Deauthorized {removedCount} session(s) for user with id {id}.");
        }

        private string CreateToken(dynamic user, DateTime expiration)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username)
            };

            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Convert.FromBase64String("B3N4rqHgVy9FREwfnK25in0GSfk8NyNz7Vz17gc5vL4="));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expiration,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
public class TokenDto
{
    public string Token { get; set; }
}
    // DTO для обновления пользователя
    public class UpdateUserDto
    {
        public int Id { get; set; }         // ID пользователя
        public string Username { get; set; } // Новый логин (опционально)
        public string? Password { get; set; } // Новый пароль (опционально)
    }

    // Класс для возврата активных сессий (пользователь + JWT токен)
    public class ActiveUser
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public List<string> Roles { get; set; }
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
    }
}
