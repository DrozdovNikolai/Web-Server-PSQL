using SuperHeroAPI.Models;

namespace SuperHeroAPI.Services.ContainerService
{
    public interface IContainerService
    {
        Task<List<Container>> GetAllContainers();
        Task<Container?> GetContainerById(int id);
        Task<Container> CreateContainer(Container container);
        Task<Container?> UpdateContainer(int id, Container container);
        Task<bool> DeleteContainer(int id);
        Task<bool> RestartContainer(int id);
    }
} 