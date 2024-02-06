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
        string passwordHash=BCrypt.Net.BCrypt.HashPassword(request.Password);
            user.Username=request.Username;
            user.PasswordHash=passwordHash;
            user.Roles = new List<Role> { new Role { RoleName = "guest" }, new Role { RoleName = "otherRole" } };


            _context.Users.Add(user);
             _context.SaveChanges();
            return Ok(user);
        }


        [HttpPost("login")]
        public  ActionResult<UserReturn> Login(UserDto request)
        {
            UserReturn userReturn = new UserReturn();

            var  user_d =  _context.Users.Where(b => b.Username == request.Username).FirstOrDefault();

            if (user_d == null)
            {
                return BadRequest("User not found.");

            }

            if(!BCrypt.Net.BCrypt.Verify(request.Password, user_d.PasswordHash))
            {
                return BadRequest("Wrong password.");
            }

           

            string token1 = CreateToken(user_d);
            userReturn.Username = request.Username;
            userReturn.Id = user_d.Id;
            userReturn.Roles = user_d.Roles.Select(r => r.RoleName).ToList();
            userReturn.accessToken = token1;

            return Ok(userReturn);
        }


        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>()
    {
        new Claim(ClaimTypes.Name, user.Username),
    };

            // Add all roles as claims
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
            }

            var key = new SymmetricSecurityKey(Convert.FromBase64String("B3N4rqHgVy9FREwfnK25in0GSfk8NyNz7Vz17gc5vL4="));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }

    }
}
