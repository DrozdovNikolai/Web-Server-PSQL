using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using PostgresControlPlane.Api.Configuration;
using PostgresControlPlane.Api.Data;

namespace PostgresControlPlane.Api.Services.Deployments;

public sealed partial class DeploymentService : IDeploymentService, IDisposable
{
    private readonly DataContext _context;
    private readonly ILogger<DeploymentService> _logger;
    private readonly KubernetesOptions _options;
    private readonly Kubernetes? _client;

    public DeploymentService(
        DataContext context,
        ILogger<DeploymentService> logger,
        IOptions<KubernetesOptions> options)
    {
        _context = context;
        _logger = logger;
        _options = options.Value;

        if (_options.Enabled)
        {
            _client = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());
        }
    }

    public async Task<(IReadOnlyList<Deployment> Items, int Total)> GetDeployments(
        int offset,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = _context.Deployments.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .AsNoTracking()
            .OrderBy(deployment => deployment.Name)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<Deployment?> GetDeploymentById(int id, CancellationToken cancellationToken)
    {
        return _context.Deployments
            .AsNoTracking()
            .SingleOrDefaultAsync(deployment => deployment.Id == id, cancellationToken);
    }

    public async Task<Deployment?> CreateDeployment(
        Deployment deployment,
        CancellationToken cancellationToken)
    {
        if (await _context.Deployments.AnyAsync(
            existing => existing.Name == deployment.Name,
            cancellationToken))
        {
            return null;
        }

        deployment.Status = _client is null ? "Simulated" : "Pending";
        deployment.CreatedAt = DateTime.UtcNow;
        _context.Deployments.Add(deployment);
        await _context.SaveChangesAsync(cancellationToken);

        if (_client is null)
        {
            return deployment;
        }

        try
        {
            await ProvisionAsync(deployment, cancellationToken);
            deployment.Status = "Running";
            deployment.ExternalUrl = BuildExternalUrl(deployment.Name);
            await _context.SaveChangesAsync(cancellationToken);
            return deployment;
        }
        catch (Exception exception)
        {
            LogProvisioningFailure(_logger, deployment.Name, exception);
            deployment.Status = "Failed";
            try
            {
                await _context.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception persistenceException)
            {
                LogFailureStatusPersistenceFailure(
                    _logger,
                    deployment.Name,
                    persistenceException);
            }

            await TryCleanupAsync(deployment.Name, CancellationToken.None);
            throw;
        }
    }

    public async Task<Deployment?> UpdateDeployment(
        int id,
        Deployment update,
        CancellationToken cancellationToken)
    {
        var deployment = await _context.Deployments.SingleOrDefaultAsync(
            existing => existing.Id == id,
            cancellationToken);
        if (deployment is null)
        {
            return null;
        }

        deployment.DatabaseHost = update.DatabaseHost;
        deployment.DatabasePort = update.DatabasePort;
        deployment.DatabaseName = update.DatabaseName;
        deployment.DatabaseUser = update.DatabaseUser;
        deployment.ApplicationAdminUsername = update.ApplicationAdminUsername;
        deployment.DatabasePassword = update.DatabasePassword;
        deployment.ApplicationAdminPassword = update.ApplicationAdminPassword;
        deployment.UpdatedAt = DateTime.UtcNow;

        if (_client is null)
        {
            deployment.Status = "Simulated";
            await _context.SaveChangesAsync(cancellationToken);
            return deployment;
        }

        await UpdateSecretAsync(deployment, cancellationToken);
        await RestartAsync(deployment.Name, cancellationToken);
        deployment.Status = "Running";
        await _context.SaveChangesAsync(cancellationToken);
        return deployment;
    }

    public async Task<bool> DeleteDeployment(int id, CancellationToken cancellationToken)
    {
        var deployment = await _context.Deployments.SingleOrDefaultAsync(
            existing => existing.Id == id,
            cancellationToken);
        if (deployment is null)
        {
            return false;
        }

        if (_client is not null)
        {
            await CleanupAsync(deployment.Name, cancellationToken);
        }

        _context.Deployments.Remove(deployment);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RestartDeployment(int id, CancellationToken cancellationToken)
    {
        var deployment = await _context.Deployments.SingleOrDefaultAsync(
            existing => existing.Id == id,
            cancellationToken);
        if (deployment is null)
        {
            return false;
        }

        if (_client is not null)
        {
            await RestartAsync(deployment.Name, cancellationToken);
            deployment.Status = "Running";
        }
        else
        {
            deployment.Status = "Simulated";
        }

        deployment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ProvisionAsync(Deployment deployment, CancellationToken cancellationToken)
    {
        var name = deployment.Name;
        var labels = new Dictionary<string, string>
        {
            ["app.kubernetes.io/name"] = "postgres-control-workload",
            ["app.kubernetes.io/instance"] = name,
            ["app.kubernetes.io/managed-by"] = "postgres-control-plane"
        };

        await _client!.CreateNamespacedConfigMapAsync(new V1ConfigMap
        {
            Metadata = Metadata($"{name}-config", labels),
            Data = new Dictionary<string, string>
            {
                ["Jwt__Issuer"] = "PostgresControlPlane",
                ["Jwt__Audience"] = "PostgresControlPlane.Client",
                ["Jwt__AccessTokenLifetimeMinutes"] = "15",
                ["Hosting__PathBase"] = "/server",
                ["Kubernetes__Enabled"] = "false",
                ["DatabaseInitialization__ApplyMigrations"] = "true",
                ["BootstrapAdmin__Enabled"] = "true",
                ["Storage__RootPath"] = "data/uploads"
            }
        }, _options.Namespace, cancellationToken: cancellationToken);

        await _client.CreateNamespacedSecretAsync(new V1Secret
        {
            Metadata = Metadata($"{name}-secrets", labels),
            Type = "Opaque",
            StringData = BuildSecretData(deployment, CreateSigningKey())
        }, _options.Namespace, cancellationToken: cancellationToken);

        await _client.CreateNamespacedPersistentVolumeClaimAsync(new V1PersistentVolumeClaim
        {
            Metadata = Metadata($"{name}-data", labels),
            Spec = new V1PersistentVolumeClaimSpec
            {
                AccessModes = ["ReadWriteOnce"],
                Resources = new V1VolumeResourceRequirements
                {
                    Requests = new Dictionary<string, ResourceQuantity>
                    {
                        ["storage"] = new("1Gi")
                    }
                }
            }
        }, _options.Namespace, cancellationToken: cancellationToken);

        await _client.CreateNamespacedDeploymentAsync(
            BuildDeployment(deployment, labels),
            _options.Namespace,
            cancellationToken: cancellationToken);
        await _client.CreateNamespacedServiceAsync(
            BuildService(name, labels),
            _options.Namespace,
            cancellationToken: cancellationToken);

        if (HasPublicIngress)
        {
            await _client.CreateNamespacedIngressAsync(
                BuildIngress(name, labels),
                _options.Namespace,
                cancellationToken: cancellationToken);
        }
    }

    private V1Deployment BuildDeployment(Deployment deployment, Dictionary<string, string> labels)
    {
        var name = deployment.Name;
        return new V1Deployment
        {
            Metadata = Metadata(name, labels),
            Spec = new V1DeploymentSpec
            {
                Replicas = 1,
                Selector = new V1LabelSelector { MatchLabels = labels },
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta { Labels = labels },
                    Spec = new V1PodSpec
                    {
                        AutomountServiceAccountToken = false,
                        SecurityContext = new V1PodSecurityContext
                        {
                            RunAsNonRoot = true,
                            RunAsUser = 1654,
                            RunAsGroup = 1654,
                            FsGroup = 1654,
                            SeccompProfile = new V1SeccompProfile { Type = "RuntimeDefault" }
                        },
                        Containers =
                        [
                            new V1Container
                            {
                                Name = "api",
                                Image = _options.WorkloadImage,
                                ImagePullPolicy = "IfNotPresent",
                                Ports = [new V1ContainerPort { Name = "http", ContainerPort = 8080 }],
                                EnvFrom =
                                [
                                    new V1EnvFromSource
                                    {
                                        ConfigMapRef = new V1ConfigMapEnvSource { Name = $"{name}-config" }
                                    },
                                    new V1EnvFromSource
                                    {
                                        SecretRef = new V1SecretEnvSource { Name = $"{name}-secrets" }
                                    }
                                ],
                                SecurityContext = new V1SecurityContext
                                {
                                    AllowPrivilegeEscalation = false,
                                    ReadOnlyRootFilesystem = true,
                                    RunAsNonRoot = true,
                                    Capabilities = new V1Capabilities { Drop = ["ALL"] }
                                },
                                Resources = new V1ResourceRequirements
                                {
                                    Requests = new Dictionary<string, ResourceQuantity>
                                    {
                                        ["cpu"] = new("50m"),
                                        ["memory"] = new("96Mi")
                                    },
                                    Limits = new Dictionary<string, ResourceQuantity>
                                    {
                                        ["cpu"] = new("500m"),
                                        ["memory"] = new("512Mi")
                                    }
                                },
                                StartupProbe = HttpProbe("/server/health/live", 30, 2),
                                ReadinessProbe = HttpProbe("/server/health/ready", 3, 10),
                                LivenessProbe = HttpProbe("/server/health/live", 3, 20),
                                VolumeMounts =
                                [
                                    new V1VolumeMount { Name = "data", MountPath = "/app/data" },
                                    new V1VolumeMount { Name = "tmp", MountPath = "/tmp" }
                                ]
                            }
                        ],
                        Volumes =
                        [
                            new V1Volume
                            {
                                Name = "data",
                                PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource
                                {
                                    ClaimName = $"{name}-data"
                                }
                            },
                            new V1Volume { Name = "tmp", EmptyDir = new V1EmptyDirVolumeSource() }
                        ]
                    }
                }
            }
        };
    }

    private V1Service BuildService(string name, Dictionary<string, string> labels)
    {
        return new V1Service
        {
            Metadata = Metadata(name, labels),
            Spec = new V1ServiceSpec
            {
                Type = "ClusterIP",
                Selector = labels,
                Ports = [new V1ServicePort { Name = "http", Port = 80, TargetPort = 8080 }]
            }
        };
    }

    private V1Ingress BuildIngress(string name, Dictionary<string, string> labels)
    {
        var baseUri = new Uri(_options.PublicBaseUrl!);
        return new V1Ingress
        {
            Metadata = new V1ObjectMeta
            {
                Name = $"{name}-ingress",
                NamespaceProperty = _options.Namespace,
                Labels = labels,
                Annotations = new Dictionary<string, string>
                {
                    ["nginx.ingress.kubernetes.io/rewrite-target"] = "/$2",
                    ["nginx.ingress.kubernetes.io/use-regex"] = "true",
                    ["nginx.ingress.kubernetes.io/ssl-redirect"] = "true",
                    ["nginx.ingress.kubernetes.io/proxy-body-size"] = "11m",
                    ["nginx.ingress.kubernetes.io/hsts"] = "true",
                    ["nginx.ingress.kubernetes.io/hsts-max-age"] = "31536000",
                    ["nginx.ingress.kubernetes.io/hsts-include-subdomains"] = "true"
                }
            },
            Spec = new V1IngressSpec
            {
                IngressClassName = "nginx",
                Tls =
                [
                    new V1IngressTLS
                    {
                        Hosts = [baseUri.Host],
                        SecretName = _options.IngressTlsSecretName
                    }
                ],
                Rules =
                [
                    new V1IngressRule
                    {
                        Host = baseUri.Host,
                        Http = new V1HTTPIngressRuleValue
                        {
                            Paths =
                            [
                                new V1HTTPIngressPath
                                {
                                    Path = $"/{name}(/|$)(.*)",
                                    PathType = "ImplementationSpecific",
                                    Backend = new V1IngressBackend
                                    {
                                        Service = new V1IngressServiceBackend
                                        {
                                            Name = name,
                                            Port = new V1ServiceBackendPort { Number = 80 }
                                        }
                                    }
                                }
                            ]
                        }
                    }
                ]
            }
        };
    }

    private async Task UpdateSecretAsync(
        Deployment deployment,
        CancellationToken cancellationToken)
    {
        var secretName = $"{deployment.Name}-secrets";
        var secret = await _client!.ReadNamespacedSecretAsync(
            secretName,
            _options.Namespace,
            cancellationToken: cancellationToken);
        var signingKey = secret.Data is not null && secret.Data.TryGetValue("Jwt__SigningKey", out var encodedKey)
            ? Encoding.UTF8.GetString(encodedKey)
            : CreateSigningKey();
        secret.StringData = BuildSecretData(deployment, signingKey);
        secret.Data = null;
        await _client.ReplaceNamespacedSecretAsync(
            secret,
            secretName,
            _options.Namespace,
            cancellationToken: cancellationToken);
    }

    private async Task RestartAsync(string name, CancellationToken cancellationToken)
    {
        var restartedAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var patch = new V1Patch(
            $"{{\"spec\":{{\"template\":{{\"metadata\":{{\"annotations\":{{\"postgres-control/restarted-at\":\"{restartedAt}\"}}}}}}}}}}",
            V1Patch.PatchType.StrategicMergePatch);
        await _client!.PatchNamespacedDeploymentAsync(
            patch,
            name,
            _options.Namespace,
            cancellationToken: cancellationToken);
    }

    private async Task CleanupAsync(string name, CancellationToken cancellationToken)
    {
        foreach (var operation in GetCleanupOperations(name, cancellationToken))
        {
            await DeleteIfExistsAsync(operation.Delete);
        }
    }

    private static async Task DeleteIfExistsAsync(Func<Task> deleteOperation)
    {
        try
        {
            await deleteOperation();
        }
        catch (HttpOperationException exception)
            when (exception.Response?.StatusCode == HttpStatusCode.NotFound)
        {
            // A missing object is the desired final state for an idempotent cleanup.
        }
    }

    private async Task TryCleanupAsync(string name, CancellationToken cancellationToken)
    {
        foreach (var operation in GetCleanupOperations(name, cancellationToken))
        {
            try
            {
                await DeleteIfExistsAsync(operation.Delete);
            }
            catch (Exception exception)
            {
                LogRollbackCleanupFailure(
                    _logger,
                    operation.Resource,
                    name,
                    exception);
            }
        }
    }

    private IEnumerable<(string Resource, Func<Task> Delete)> GetCleanupOperations(
        string name,
        CancellationToken cancellationToken)
    {
        if (HasPublicIngress)
        {
            yield return (
                "Ingress",
                () => _client!.DeleteNamespacedIngressAsync(
                    $"{name}-ingress",
                    _options.Namespace,
                    cancellationToken: cancellationToken));
        }

        yield return (
            "Service",
            () => _client!.DeleteNamespacedServiceAsync(
                name,
                _options.Namespace,
                cancellationToken: cancellationToken));
        yield return (
            "Deployment",
            () => _client!.DeleteNamespacedDeploymentAsync(
                name,
                _options.Namespace,
                cancellationToken: cancellationToken));
        yield return (
            "ConfigMap",
            () => _client!.DeleteNamespacedConfigMapAsync(
                $"{name}-config",
                _options.Namespace,
                cancellationToken: cancellationToken));
        yield return (
            "Secret",
            () => _client!.DeleteNamespacedSecretAsync(
                $"{name}-secrets",
                _options.Namespace,
                cancellationToken: cancellationToken));
        yield return (
            "PersistentVolumeClaim",
            () => _client!.DeleteNamespacedPersistentVolumeClaimAsync(
                $"{name}-data",
                _options.Namespace,
                cancellationToken: cancellationToken));
    }

    private V1ObjectMeta Metadata(string name, Dictionary<string, string> labels)
    {
        return new V1ObjectMeta
        {
            Name = name,
            NamespaceProperty = _options.Namespace,
            Labels = labels
        };
    }

    private static V1Probe HttpProbe(string path, int failureThreshold, int periodSeconds)
    {
        return new V1Probe
        {
            HttpGet = new V1HTTPGetAction { Path = path, Port = 8080 },
            FailureThreshold = failureThreshold,
            PeriodSeconds = periodSeconds,
            TimeoutSeconds = 2
        };
    }

    private static Dictionary<string, string> BuildSecretData(Deployment deployment, string signingKey)
    {
        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = deployment.DatabaseHost,
            Port = deployment.DatabasePort,
            Database = deployment.DatabaseName,
            Username = deployment.DatabaseUser,
            Password = deployment.DatabasePassword,
            Pooling = true
        }.ConnectionString;

        return new Dictionary<string, string>
        {
            ["ConnectionStrings__MainDatabase"] = connectionString,
            ["Jwt__SigningKey"] = signingKey,
            ["BootstrapAdmin__Enabled"] = "true",
            ["BootstrapAdmin__Username"] = deployment.ApplicationAdminUsername,
            ["BootstrapAdmin__Password"] = deployment.ApplicationAdminPassword
        };
    }

    private static string CreateSigningKey()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private bool HasPublicIngress => !string.IsNullOrWhiteSpace(_options.PublicBaseUrl);

    private string BuildExternalUrl(string name)
    {
        return HasPublicIngress
            ? $"{_options.PublicBaseUrl!.TrimEnd('/')}/{name}/server/health/ready"
            : string.Empty;
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    [LoggerMessage(1, LogLevel.Error, "Provisioning failed for deployment {DeploymentName}")]
    private static partial void LogProvisioningFailure(
        ILogger logger,
        string deploymentName,
        Exception exception);

    [LoggerMessage(
        2,
        LogLevel.Warning,
        "Rollback could not delete {ResourceType} for deployment {DeploymentName}")]
    private static partial void LogRollbackCleanupFailure(
        ILogger logger,
        string resourceType,
        string deploymentName,
        Exception exception);

    [LoggerMessage(3, LogLevel.Warning, "Could not persist failed status for deployment {DeploymentName}")]
    private static partial void LogFailureStatusPersistenceFailure(
        ILogger logger,
        string deploymentName,
        Exception exception);
}
