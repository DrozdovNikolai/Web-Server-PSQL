using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperHeroAPI.Models;
using SuperHeroAPI.Services.ContainerService;

namespace SuperHeroAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize] - removed to allow unauthorized access
    public class ContainersController : ControllerBase
    {
        private readonly IContainerService _containerService;
        private readonly ILogger<ContainersController> _logger;

        public ContainersController(
            IContainerService containerService,
            ILogger<ContainersController> logger)
        {
            _containerService = containerService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<Container>>> GetAllContainers()
        {
            try
            {
                var containers = await _containerService.GetAllContainers();
                return Ok(containers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all containers");
                return StatusCode(500, "An error occurred while fetching containers");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Container>> GetContainerById(int id)
        {
            try
            {
                var container = await _containerService.GetContainerById(id);
                if (container == null)
                    return NotFound($"Container with ID {id} not found");

                return Ok(container);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching container with ID {id}");
                return StatusCode(500, "An error occurred while fetching the container");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Container>> CreateContainer(Container container)
        {
            try
            {
                var createdContainer = await _containerService.CreateContainer(container);
                return CreatedAtAction(
                    nameof(GetContainerById), 
                    new { id = createdContainer.Id }, 
                    new
                    {
                        createdContainer.Id,
                        createdContainer.Name,
                        createdContainer.Status,
                        createdContainer.ExternalUrl,
                        Message = "Container created successfully. Use the ExternalUrl to access the container's API."
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating container");
                return StatusCode(500, "An error occurred while creating the container");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Container>> UpdateContainer(int id, Container container)
        {
            try
            {
                var updatedContainer = await _containerService.UpdateContainer(id, container);
                if (updatedContainer == null)
                    return NotFound($"Container with ID {id} not found");

                return Ok(updatedContainer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating container with ID {id}");
                return StatusCode(500, "An error occurred while updating the container");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteContainer(int id)
        {
            try
            {
                var result = await _containerService.DeleteContainer(id);
                if (!result)
                    return NotFound($"Container with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting container with ID {id}");
                return StatusCode(500, "An error occurred while deleting the container");
            }
        }

        [HttpPost("{id}/restart")]
        public async Task<ActionResult> RestartContainer(int id)
        {
            try
            {
                var result = await _containerService.RestartContainer(id);
                if (!result)
                    return NotFound($"Container with ID {id} not found");

                return Ok(new { message = "Container restarted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restarting container with ID {id}");
                return StatusCode(500, "An error occurred while restarting the container");
            }
        }
    }
} 