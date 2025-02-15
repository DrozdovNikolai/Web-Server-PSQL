using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperHeroAPI.Services.SuperHeroService;

namespace SuperHeroAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _iUserService;

        public UserController(IUserService userService)
        {
            _iUserService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAllHeroes()
        {
            return await _iUserService.GetAllUsers();
        }


        [HttpGet("{id}")]
        //[Route("{id}")]
        public async Task<ActionResult<User>> GetSingleUser(int id)
        {
            var result = await _iUserService.GetSingleUser(id);
            if (result == null)
                return NotFound("User not found.");

            return Ok(result);

        }



        [HttpPost]

        public async Task<ActionResult<List<User>>> AddUser(UserDto user)
        {
            var result = await _iUserService.AddUser(user);

            return Ok(result);
        }

        
        [HttpPut("{id}")]
        //[Route("{id}")]
        public async Task<ActionResult<List<User>>> UpdateUser(int id, UserUpd request)
        {
            var result = await _iUserService.UpdateUser(id, request);
            if (result == null)
                return NotFound("Hero not found.");

            return Ok(result);
        }
        

        [HttpDelete("{id}")]
        //[Route("{id}")]
        public async Task<ActionResult<List<User>>> DeleteUser(int id)
        {
            var result = await _iUserService.DeleteUser(id);
            if (result == null)
                return NotFound("User not found.");

            return Ok(result);
        }

        [HttpGet("tokens")]

        public async Task<ActionResult<List<UserAuthToken>>> GetAllTokens()
        {
            var tokens = await _iUserService.GetActiveTokens();
            return Ok(tokens);
        }

        // Деавторизация (отзыв токенов) для пользователя по id
        [HttpPost("deauthorize/{id}")]

        public async Task<ActionResult> DeauthorizeUser(int id)
        {
            var success = await _iUserService.DeauthorizeUserTokens(id);
            if (!success)
                return NotFound("No active tokens found for the user.");

            return Ok($"User with id {id} has been deauthorized.");
        }

    }

}

