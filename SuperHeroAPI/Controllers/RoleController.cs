using Microsoft.AspNetCore.Mvc;
using SuperHeroAPI.Services.SuperHeroService;

namespace SuperHeroAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Role>>> GetAllRoles()
        {
            return await _roleService.GetAllRoles(); 
        }


        [HttpGet("{id}")]
        //[Route("{id}")]
        public async Task<ActionResult<Role>> GetSingleRole(int id)
        {
            var result = await _roleService.GetSingleRole(id);
            if (result == null)
                return NotFound("Role not found.");

            return Ok(result);

        }



        [HttpPost]

        public async Task<ActionResult<List<Role>>> AddRole(Role role)
        {
            var result = await _roleService.AddRole(role);

            return Ok(result);
        }


        [HttpPut("{id}")]
        //[Route("{id}")]
        public async Task<ActionResult<List<Role>>> UpdateRole(int id, Role request)
        {
            var result = await _roleService.UpdateRole(id, request);
            if (result == null)
                return NotFound("Role not found.");

            return Ok(result);
        }



        [HttpDelete("{id}")]
        //[Route("{id}")]
        public async Task<ActionResult<List<Role>>> DeleteRole(int id)
        {
            var result = await _roleService.DeleteRole(id);
            if (result == null)
                return NotFound("Permission not found.");

            return Ok(result);
        }
    }
}
