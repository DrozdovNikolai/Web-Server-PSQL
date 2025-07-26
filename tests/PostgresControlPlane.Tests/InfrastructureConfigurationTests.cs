using Xunit;

namespace PostgresControlPlane.Tests;

public sealed class InfrastructureConfigurationTests
{
    [Fact]
    public void KubernetesWorkloadUsesRestrictedPodSecurity()
    {
        var deployment = ReadRepositoryFile("k8s", "deployment.yaml");

        Assert.Contains("runAsNonRoot: true", deployment, StringComparison.Ordinal);
        Assert.Contains("readOnlyRootFilesystem: true", deployment, StringComparison.Ordinal);
        Assert.Contains("allowPrivilegeEscalation: false", deployment, StringComparison.Ordinal);
        Assert.Contains("type: RuntimeDefault", deployment, StringComparison.Ordinal);
        Assert.Contains("drop: [\"ALL\"]", deployment, StringComparison.Ordinal);
        Assert.DoesNotContain("hostPath:", deployment, StringComparison.Ordinal);
        Assert.DoesNotContain("privileged: true", deployment, StringComparison.Ordinal);
        Assert.DoesNotContain(":latest", deployment, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void KubernetesAccessIsNamespaceScopedAndQuotaBounded()
    {
        var role = ReadRepositoryFile("k8s", "k8s-role.yaml");
        var limits = ReadRepositoryFile("k8s", "resource-limits.yaml");

        Assert.Contains("kind: Role", role, StringComparison.Ordinal);
        Assert.DoesNotContain("kind: ClusterRole", role, StringComparison.Ordinal);
        Assert.DoesNotContain("nodes", role, StringComparison.Ordinal);
        Assert.DoesNotContain("\"list\"", role, StringComparison.Ordinal);
        Assert.DoesNotContain("\"watch\"", role, StringComparison.Ordinal);
        Assert.Contains("kind: ResourceQuota", limits, StringComparison.Ordinal);
        Assert.Contains("kind: LimitRange", limits, StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "PostgresControlPlane.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine([directory.FullName, .. pathParts]));
    }
}
