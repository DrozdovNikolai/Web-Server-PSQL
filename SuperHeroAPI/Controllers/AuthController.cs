using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SuperHeroAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly DataContext _context;

        public AuthController(DataContext context)
        {
            _context = context;
        }





        [HttpPost("register")]
        public ActionResult<User> Register(UserDto request)
        {
            // Step 1: Create a new Role in the Roles table
      

     
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

            string token = CreateToken(user);

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
            // Step 1: Find the user by ID
            var user = await _context.Users.FindAsync(request.Id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Step 2: Update the username if provided
            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                user.Username = request.Username;
            }

            // Step 3: Update the password if provided
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                user.PasswordHash = passwordHash;
            }

            // Step 4: Save the changes to the database
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(user);
        }


        private string CreateToken(dynamic user)
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
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
    public class UpdateUserDto
    {
        public int Id { get; set; }         // User ID to identify the user
        public string Username { get; set; } // New Username (optional)
        public string? Password { get; set; } // New Password (optional, nullable)
    }

}
