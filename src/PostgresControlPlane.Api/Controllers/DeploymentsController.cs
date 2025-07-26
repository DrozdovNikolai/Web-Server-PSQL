using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostgresControlPlane.Api.Configuration;
using PostgresControlPlane.Api.Models.Contracts;
using PostgresControlPlane.Api.Services.Deployments;

namespace PostgresControlPlane.Api.Controllers;

[ApiController]
[Route("api/deployments")]
[Authorize(Policy = AuthorizationPolicies.DeploymentsManage)]
public sealed class DeploymentsController : ControllerBase
{
    private const int MaximumPageSize = 100;
    private readonly IDeploymentService _deploymentService;

    public DeploymentsController(IDeploymentService deploymentService)
    {
        _deploymentService = deploymentService;
    }

    [HttpGet]
    public async Task<ActionResult<DeploymentPageResponse>> GetDeployments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetOffset(page, pageSize, out var offset))
        {
            return InvalidPagination();
        }

        var result = await _deploymentService.GetDeployments(offset, pageSize, cancellationToken);
        return Ok(new DeploymentPageResponse(
            page,
            pageSize,
            result.Total,
            result.Items.Select(MapResponse).ToArray()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DeploymentResponse>> GetDeployment(
        int id,
        CancellationToken cancellationToken)
    {
        var deployment = await _deploymentService.GetDeploymentById(id, cancellationToken);
        return deployment is null ? NotFound() : Ok(MapResponse(deployment));
    }

    [HttpPost]
    public async Task<ActionResult<DeploymentResponse>> CreateDeployment(
        DeploymentRequest request,
        CancellationToken cancellationToken)
    {
        var deployment = await _deploymentService.CreateDeployment(MapEntity(request), cancellationToken);
        if (deployment is null)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Deployment name is already in use"
            });
        }

        var response = MapResponse(deployment);
        return CreatedAtAction(nameof(GetDeployment), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DeploymentResponse>> UpdateDeployment(
        int id,
        DeploymentRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _deploymentService.GetDeploymentById(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }
        if (!string.Equals(existing.Name, request.Name, StringComparison.Ordinal))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "A deployment cannot be renamed"
            });
        }

        var deployment = await _deploymentService.UpdateDeployment(id, MapEntity(request), cancellationToken);
        return Ok(MapResponse(deployment!));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteDeployment(int id, CancellationToken cancellationToken)
    {
        return await _deploymentService.DeleteDeployment(id, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/restart")]
    public async Task<IActionResult> RestartDeployment(int id, CancellationToken cancellationToken)
    {
        return await _deploymentService.RestartDeployment(id, cancellationToken) ? NoContent() : NotFound();
    }

    private static Deployment MapEntity(DeploymentRequest request)
    {
        return new Deployment
        {
            Name = request.Name,
            DatabaseHost = request.DatabaseHost,
            DatabasePort = request.DatabasePort,
            DatabaseName = request.DatabaseName,
            DatabaseUser = request.DatabaseUser,
            DatabasePassword = request.DatabasePassword,
            ApplicationAdminUsername = request.ApplicationAdminUsername,
            ApplicationAdminPassword = request.ApplicationAdminPassword
        };
    }

    private static DeploymentResponse MapResponse(Deployment deployment)
    {
        return new DeploymentResponse(
            deployment.Id,
            deployment.Name,
            deployment.Status,
            deployment.DatabaseHost,
            deployment.DatabasePort,
            deployment.DatabaseName,
            deployment.ExternalUrl,
            deployment.CreatedAt,
            deployment.UpdatedAt);
    }

    private static bool TryGetOffset(int page, int pageSize, out int offset)
    {
        offset = 0;
        if (page < 1 ||
            pageSize is < 1 or > MaximumPageSize ||
            page - 1 > int.MaxValue / Math.Max(pageSize, 1))
        {
            return false;
        }

        offset = checked((page - 1) * pageSize);
        return true;
    }

    private ObjectResult InvalidPagination()
    {
        return Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid pagination",
            detail: $"Page must be at least 1 and page size must be between 1 and {MaximumPageSize}.");
    }
}
