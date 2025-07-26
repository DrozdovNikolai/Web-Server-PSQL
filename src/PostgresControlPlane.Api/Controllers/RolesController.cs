using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostgresControlPlane.Api.Configuration;
using PostgresControlPlane.Api.Models.Contracts;

namespace PostgresControlPlane.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = AuthorizationPolicies.IdentityManage)]
public sealed class RolesController : ControllerBase
{
    private readonly DataContext _context;

    public RolesController(DataContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleResponse>>> GetRoles(
        CancellationToken cancellationToken)
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new RoleResponse(role.Id, role.Name))
            .ToListAsync(cancellationToken);

        return Ok(roles);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoleResponse>> GetRole(
        int id,
        CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .Select(candidate => new RoleResponse(candidate.Id, candidate.Name))
            .SingleOrDefaultAsync(cancellationToken);

        return role is null ? NotFound() : Ok(role);
    }

    [HttpPost]
    public async Task<ActionResult<RoleResponse>> CreateRole(
        CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim().ToLowerInvariant();
        if (await _context.Roles.AnyAsync(role => role.Name == name, cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Role name is already in use"
            });
        }

        var role = new Role { Name = name };
        _context.Roles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);

        var response = new RoleResponse(role.Id, role.Name);
        return CreatedAtAction(nameof(GetRole), new { id = role.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoleResponse>> UpdateRole(
        int id,
        UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var role = await _context.Roles.SingleOrDefaultAsync(
            candidate => candidate.Id == id,
            cancellationToken);
        if (role is null)
        {
            return NotFound();
        }

        if (IsBuiltIn(role.Name))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Built-in roles cannot be renamed"
            });
        }

        var name = request.Name.Trim().ToLowerInvariant();
        if (await _context.Roles.AnyAsync(
            candidate => candidate.Id != id && candidate.Name == name,
            cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Role name is already in use"
            });
        }

        role.Name = name;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new RoleResponse(role.Id, role.Name));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRole(int id, CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .Include(candidate => candidate.UserRoles)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (role is null)
        {
            return NotFound();
        }

        if (IsBuiltIn(role.Name) || role.UserRoles.Count > 0)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "The role is built-in or still assigned to users"
            });
        }

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static bool IsBuiltIn(string roleName) =>
        roleName is ApplicationRoles.SuperAdministrator or ApplicationRoles.DefaultUser;
}
