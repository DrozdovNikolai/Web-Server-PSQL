using Microsoft.AspNetCore.Mvc;
using SuperHeroAPI.Services.SuperHeroService;

namespace SuperHeroAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionController : ControllerBase
    {
        private readonly IPermission _permissionService;

        public PermissionController(IPermission permissionService)
        {
            _permissionService = permissionService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Permission>>> GetAllPermissions()
        {
            return await _permissionService.GetAllPermissions();
        }


        [HttpGet("{id}")]
        //[Route("{id}")]
        public async Task<ActionResult<Permission>> GetSinglePermission(int id)
        {
            var result = await _permissionService.GetSinglePermission(id);
            if (result == null)
                return NotFound("Permission not found.");

            return Ok(result);

        }



        [HttpPost]

        public async Task<ActionResult<List<Permission>>> AddPermission(Permission permission)
        {
            var result = await _permissionService.AddPermission(permission);

            return Ok(result);
        }


        [HttpPut("{id}")]
        //[Route("{id}")]
        public async Task<ActionResult<List<Permission>>> UpdatePermission(int id, Permission request)
        {
            var result = await _permissionService.UpdatePermission(id, request);
            if (result == null)
                return NotFound("Permission not found.");

            return Ok(result);
        }



        [HttpDelete("{id}")]
        //[Route("{id}")]
        public async Task<ActionResult<List<Permission>>> DeletePermission(int id)
        {
            var result = await _permissionService.DeletePermission(id);
            if (result == null)
                return NotFound("Permission not found.");

            return Ok(result);
        }
    }
}
