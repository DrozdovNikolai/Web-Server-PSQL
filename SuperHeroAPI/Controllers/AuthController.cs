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

            User user = new User();
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            user.Username = request.Username;
            user.PasswordHash = passwordHash;
            user.UserRoles = new List<UserRole> { new UserRole { RoleId = 2 } }; 


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
}
