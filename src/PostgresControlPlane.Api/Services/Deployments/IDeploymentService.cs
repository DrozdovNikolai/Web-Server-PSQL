using PostgresControlPlane.Api.Models;

namespace PostgresControlPlane.Api.Services.Deployments
{
    public interface IDeploymentService
    {
        Task<(IReadOnlyList<Deployment> Items, int Total)> GetDeployments(
            int offset,
            int limit,
            CancellationToken cancellationToken);
        Task<Deployment?> GetDeploymentById(int id, CancellationToken cancellationToken);
        Task<Deployment?> CreateDeployment(Deployment deployment, CancellationToken cancellationToken);
        Task<Deployment?> UpdateDeployment(int id, Deployment update, CancellationToken cancellationToken);
        Task<bool> DeleteDeployment(int id, CancellationToken cancellationToken);
        Task<bool> RestartDeployment(int id, CancellationToken cancellationToken);
    }
}
