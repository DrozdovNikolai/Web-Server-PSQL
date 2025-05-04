using Microsoft.EntityFrameworkCore;
using PostgreSQL.Data;
using SuperHeroAPI.Models;
using System.Diagnostics;
using k8s;
using k8s.Models;
using System.Text;

namespace SuperHeroAPI.Services.ContainerService
{
    public class ContainerService : IContainerService
    {
        private readonly DataContext _context;
        private readonly ILogger<ContainerService> _logger;
        private readonly Kubernetes _kubernetesClient;

        public ContainerService(DataContext context, ILogger<ContainerService> logger)
        {
            _context = context;
            _logger = logger;
            
            // Initialize Kubernetes client - adjust for your environment
            var config = KubernetesClientConfiguration.BuildDefaultConfig();
            _kubernetesClient = new Kubernetes(config);
        }

        public async Task<List<Container>> GetAllContainers()
        {
            return await _context.Containers.ToListAsync();
        }

        public async Task<Container?> GetContainerById(int id)
        {
            return await _context.Containers.FindAsync(id);
        }

        public async Task<Container> CreateContainer(Container container)
        {
            try
            {
                // Add container to database
                container.Status = "Pending";
                container.CreatedAt = DateTime.UtcNow;
                
                _context.Containers.Add(container);
                await _context.SaveChangesAsync();

                // Deploy container to Kubernetes
                await DeployToKubernetes(container);
                
                // Update status
                container.Status = "Running";
                await _context.SaveChangesAsync();
                
                return container;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating container");
                
                // Update status to failed if already created
                if (container.Id != 0)
                {
                    container.Status = "Failed";
                    await _context.SaveChangesAsync();
                }
                
                throw;
            }
        }

        public async Task<Container?> UpdateContainer(int id, Container container)
        {
            var existingContainer = await _context.Containers.FindAsync(id);
            if (existingContainer == null)
                return null;

            // Update properties
            existingContainer.Name = container.Name;
            existingContainer.DbHost = container.DbHost;
            existingContainer.DbPort = container.DbPort;
            existingContainer.DbName = container.DbName;
            existingContainer.DbUser = container.DbUser;
            existingContainer.DbPassword = container.DbPassword;
            existingContainer.DbUsername = container.DbUsername;
            existingContainer.DbPasswordUser = container.DbPasswordUser;
            existingContainer.UpdatedAt = DateTime.UtcNow;

            try
            {
                // Update Kubernetes deployment
                await UpdateKubernetesDeployment(existingContainer);
                
                await _context.SaveChangesAsync();
                return existingContainer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating container");
                throw;
            }
        }

        public async Task<bool> DeleteContainer(int id)
        {
            var container = await _context.Containers.FindAsync(id);
            if (container == null)
                return false;

            try
            {
                // Delete from Kubernetes
                await DeleteFromKubernetes(container);
                
                // Delete from database
                _context.Containers.Remove(container);
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting container");
                throw;
            }
        }

        public async Task<bool> RestartContainer(int id)
        {
            var container = await _context.Containers.FindAsync(id);
            if (container == null)
                return false;

            try
            {
                // Restart in Kubernetes
                await RestartKubernetesDeployment(container);
                
                container.Status = "Running";
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting container");
                container.Status = "Failed";
                await _context.SaveChangesAsync();
                throw;
            }
        }

        // Helper methods for Kubernetes interaction

        private async Task DeployToKubernetes(Container container)
        {
            try
            {
                // Create ConfigMap
                var configMap = new V1ConfigMap
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = $"{container.Name.ToLower()}-config",
                        NamespaceProperty = "default"
                    },
                    Data = new Dictionary<string, string>
                    {
                        ["DB_HOST"] = container.DbHost,
                        ["DB_PORT"] = container.DbPort,
                        ["DB_NAME"] = container.DbName
                    }
                };
                await _kubernetesClient.CreateNamespacedConfigMapAsync(configMap, "default");

                // Create Secret
                var secret = new V1Secret
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = $"{container.Name.ToLower()}-secrets",
                        NamespaceProperty = "default"
                    },
                    Type = "Opaque",
                    StringData = new Dictionary<string, string>
                    {
                        ["DB_USER"] = container.DbUser,
                        ["DB_PASSWORD"] = container.DbPassword,
                        ["DB_USERNAME"] = container.DbUsername,
                        ["DB_PASSWORD_USER"] = container.DbPasswordUser
                    }
                };
                await _kubernetesClient.CreateNamespacedSecretAsync(secret, "default");

                // Create PersistentVolumeClaim for file uploads
                var pvcName = $"{container.Name.ToLower()}-file-pvc";
                var pvc = new V1PersistentVolumeClaim
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = pvcName,
                        NamespaceProperty = "default"
                    },
                    Spec = new V1PersistentVolumeClaimSpec
                    {
                        AccessModes = new List<string> { "ReadWriteOnce" },
                        Resources = new V1ResourceRequirements
                        {
                            Requests = new Dictionary<string, ResourceQuantity>
                            {
                                ["storage"] = new ResourceQuantity("5Gi")
                            }
                        },
                        //StorageClassName = null
                    }
                };
                await _kubernetesClient.CreateNamespacedPersistentVolumeClaimAsync(pvc, "default");

                // Create Deployment
                var deployment = new V1Deployment
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = container.Name.ToLower(),
                        NamespaceProperty = "default"
                    },
                    Spec = new V1DeploymentSpec
                    {
                        Replicas = 1,
                        Selector = new V1LabelSelector
                        {
                            MatchLabels = new Dictionary<string, string>
                            {
                                ["app"] = container.Name.ToLower()
                            }
                        },
                        Template = new V1PodTemplateSpec
                        {
                            Metadata = new V1ObjectMeta
                            {
                                Labels = new Dictionary<string, string>
                                {
                                    ["app"] = container.Name.ToLower()
                                }
                            },
                            Spec = new V1PodSpec
                            {
                                // Add init container to setup directories and permissions
                                InitContainers = new List<V1Container>
                                {
                                    new V1Container
                                    {
                                        Name = "init-file-permissions",
                                        Image = "busybox",
                                        Command = new List<string> { "/bin/sh", "-c" },
                                        Args = new List<string> 
                                        { 
                                            "mkdir -p /var/www/ncatbird.ru/html/docx && " +
                                            "chmod -R 777 /var/www/ncatbird.ru && " +
                                            "touch /var/www/ncatbird.ru/html/docx/init_test.txt && " +
                                            "echo 'Init container initialized at $(date)' > /var/www/ncatbird.ru/html/docx/init_test.txt && " +
                                            "echo 'Directory contents:' && " +
                                            "ls -la /var/www/ncatbird.ru/html/docx"
                                        },
                                        SecurityContext = new V1SecurityContext
                                        {
                                            Privileged = true,
                                            RunAsUser = 0 // Run as root
                                        },
                                        VolumeMounts = new List<V1VolumeMount>
                                        {
                                            new V1VolumeMount
                                            {
                                                Name = "file-upload-volume",
                                                MountPath = "/var/www/ncatbird.ru/html",
                                                ReadOnlyProperty = false
                                            }
                                        }
                                    }
                                },
                                Containers = new List<V1Container>
                                {
                                    new V1Container
                                    {
                                        Name = container.Name.ToLower(),
                                        Image = "superhero-api:latest",
                                        ImagePullPolicy = "IfNotPresent",
                                        Ports = new List<V1ContainerPort>
                                        {
                                            new V1ContainerPort
                                            {
                                                ContainerPort = 8080
                                            }
                                        },
                                        Env = new List<V1EnvVar>
                                        {
                                            // ConfigMap references
                                            new V1EnvVar
                                            {
                                                Name = "DB_HOST",
                                                ValueFrom = new V1EnvVarSource
                                                {
                                                    ConfigMapKeyRef = new V1ConfigMapKeySelector
                                                    {
                                                        Name = $"{container.Name.ToLower()}-config",
                                                        Key = "DB_HOST"
                                                    }
                                                }
                                            },
                                            new V1EnvVar
                                            {
                                                Name = "DB_PORT",
                                                ValueFrom = new V1EnvVarSource
                                                {
                                                    ConfigMapKeyRef = new V1ConfigMapKeySelector
                                                    {
                                                        Name = $"{container.Name.ToLower()}-config",
                                                        Key = "DB_PORT"
                                                    }
                                                }
                                            },
                                            new V1EnvVar
                                            {
                                                Name = "DB_NAME",
                                                ValueFrom = new V1EnvVarSource
                                                {
                                                    ConfigMapKeyRef = new V1ConfigMapKeySelector
                                                    {
                                                        Name = $"{container.Name.ToLower()}-config",
                                                        Key = "DB_NAME"
                                                    }
                                                }
                                            },
                                            
                                            // Secret references
                                            new V1EnvVar
                                            {
                                                Name = "DB_USER",
                                                ValueFrom = new V1EnvVarSource
                                                {
                                                    SecretKeyRef = new V1SecretKeySelector
                                                    {
                                                        Name = $"{container.Name.ToLower()}-secrets",
                                                        Key = "DB_USER"
                                                    }
                                                }
                                            },
                                            new V1EnvVar
                                            {
                                                Name = "DB_PASSWORD",
                                                ValueFrom = new V1EnvVarSource
                                                {
                                                    SecretKeyRef = new V1SecretKeySelector
                                                    {
                                                        Name = $"{container.Name.ToLower()}-secrets",
                                                        Key = "DB_PASSWORD"
                                                    }
                                                }
                                            },
                                            new V1EnvVar
                                            {
                                                Name = "DB_USERNAME",
                                                ValueFrom = new V1EnvVarSource
                                                {
                                                    SecretKeyRef = new V1SecretKeySelector
                                                    {
                                                        Name = $"{container.Name.ToLower()}-secrets",
                                                        Key = "DB_USERNAME"
                                                    }
                                                }
                                            },
                                            new V1EnvVar
                                            {
                                                Name = "DB_PASSWORD_USER",
                                                ValueFrom = new V1EnvVarSource
                                                {
                                                    SecretKeyRef = new V1SecretKeySelector
                                                    {
                                                        Name = $"{container.Name.ToLower()}-secrets",
                                                        Key = "DB_PASSWORD_USER"
                                                    }
                                                }
                                            }
                                        },
                                        // Add volume mounts for file uploads
                                        VolumeMounts = new List<V1VolumeMount>
                                        {
                                            new V1VolumeMount
                                            {
                                                Name = "file-upload-volume",
                                                MountPath = "/var/www/ncatbird.ru/html",
                                                ReadOnlyProperty = false
                                            }
                                        }
                                    }
                                },
                                // Add volumes to the pod spec
                                Volumes = new List<V1Volume>
                                {
                                    new V1Volume
                                    {
                                        Name = "file-upload-volume",
                                        // Use HostPath instead of PersistentVolumeClaim to store files directly on the host
                                        HostPath = new V1HostPathVolumeSource
                                        {
                                            Path = "/var/www/ncatbird.ru/html",
                                            Type = "DirectoryOrCreate"
                                        }
                                    }
                                },
                                // Add security context to allow writing to the mounted volume
                                SecurityContext = new V1PodSecurityContext
                                {
                                    FsGroup = 1000,
                                    RunAsUser = 1000,
                                    RunAsGroup = 1000
                                }
                            }
                        }
                    }
                };
                await _kubernetesClient.CreateNamespacedDeploymentAsync(deployment, "default");

                // Create Service
                var service = new V1Service
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = container.Name.ToLower(),
                        NamespaceProperty = "default"
                    },
                    Spec = new V1ServiceSpec
                    {
                        Selector = new Dictionary<string, string>
                        {
                            ["app"] = container.Name.ToLower()
                        },
                        Ports = new List<V1ServicePort>
                        {
                            new V1ServicePort
                            {
                                Port = 81,
                                TargetPort = 8080
                            }
                        },
                        Type = "ClusterIP"
                    }
                };
                await _kubernetesClient.CreateNamespacedServiceAsync(service, "default");

                // Create Ingress
                var ingress = new V1Ingress
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = $"{container.Name.ToLower()}-ingress",
                        NamespaceProperty = "default",
                        Annotations = new Dictionary<string, string>
                        {
                            ["kubernetes.io/ingress.class"] = "nginx",
                            ["nginx.ingress.kubernetes.io/rewrite-target"] = "/$2",
                            ["nginx.ingress.kubernetes.io/use-regex"] = "true",
                            ["nginx.ingress.kubernetes.io/proxy-connect-timeout"] = "300",
                            ["nginx.ingress.kubernetes.io/proxy-send-timeout"] = "300",
                            ["nginx.ingress.kubernetes.io/proxy-read-timeout"] = "300",
                            ["nginx.ingress.kubernetes.io/proxy-body-size"] = "10m",
                            ["nginx.ingress.kubernetes.io/ssl-redirect"] = "false"
                        }
                    },
                    Spec = new V1IngressSpec
                    {
                        Rules = new List<V1IngressRule>
                        {
                            new V1IngressRule
                            {
                                Http = new V1HTTPIngressRuleValue
                                {
                                    Paths = new List<V1HTTPIngressPath>
                                    {
                                        new V1HTTPIngressPath
                                        {
                                            Path = $"/{container.Name.ToLower()}/server(/|$)(.*)",
                                            PathType = "ImplementationSpecific",
                                            Backend = new V1IngressBackend
                                            {
                                                Service = new V1IngressServiceBackend
                                                {
                                                    Name = container.Name.ToLower(),
                                                    Port = new V1ServiceBackendPort
                                                    {
                                                        Number = 81
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
                await _kubernetesClient.CreateNamespacedIngressAsync(ingress, "default");

                // Create a separate ingress for file access
                var fileIngress = new V1Ingress
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = $"{container.Name.ToLower()}-file-ingress",
                        NamespaceProperty = "default",
                        Annotations = new Dictionary<string, string>
                        {
                            ["kubernetes.io/ingress.class"] = "nginx",
                            ["nginx.ingress.kubernetes.io/rewrite-target"] = "/docx/$2",
                            ["nginx.ingress.kubernetes.io/use-regex"] = "true",
                            ["nginx.ingress.kubernetes.io/proxy-body-size"] = "100m",
                            ["nginx.ingress.kubernetes.io/ssl-redirect"] = "false"
                        }
                    },
                    Spec = new V1IngressSpec
                    {
                        Rules = new List<V1IngressRule>
                        {
                            new V1IngressRule
                            {
                                Http = new V1HTTPIngressRuleValue
                                {
                                    Paths = new List<V1HTTPIngressPath>
                                    {
                                        new V1HTTPIngressPath
                                        {
                                            Path = $"/{container.Name.ToLower()}/docx(/|$)(.*)",
                                            PathType = "ImplementationSpecific",
                                            Backend = new V1IngressBackend
                                            {
                                                Service = new V1IngressServiceBackend
                                                {
                                                    Name = container.Name.ToLower(),
                                                    Port = new V1ServiceBackendPort
                                                    {
                                                        Number = 81
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
                await _kubernetesClient.CreateNamespacedIngressAsync(fileIngress, "default");
                
                // Get the ingress host if available, otherwise use localhost
                string ingressHost = await GetIngressHost();
                
                // Update the URL to include the full path to the Swagger UI
                container.ExternalUrl = $"http://{ingressHost}/{container.Name.ToLower()}/server/swagger";
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deploying to Kubernetes");
                throw;
            }
        }

        private async Task UpdateKubernetesDeployment(Container container)
        {
            try
            {
                // Update ConfigMap
                var configMap = await _kubernetesClient.ReadNamespacedConfigMapAsync(
                    $"{container.Name.ToLower()}-config", "default");
                
                configMap.Data["DB_HOST"] = container.DbHost;
                configMap.Data["DB_PORT"] = container.DbPort;
                configMap.Data["DB_NAME"] = container.DbName;
                
                await _kubernetesClient.ReplaceNamespacedConfigMapAsync(
                    configMap, $"{container.Name.ToLower()}-config", "default");

                // Update Secret
                var secret = await _kubernetesClient.ReadNamespacedSecretAsync(
                    $"{container.Name.ToLower()}-secrets", "default");
                
                secret.StringData = new Dictionary<string, string>
                {
                    ["DB_USER"] = container.DbUser,
                    ["DB_PASSWORD"] = container.DbPassword,
                    ["DB_USERNAME"] = container.DbUsername,
                    ["DB_PASSWORD_USER"] = container.DbPasswordUser
                };
                
                await _kubernetesClient.ReplaceNamespacedSecretAsync(
                    secret, $"{container.Name.ToLower()}-secrets", "default");

                // Restart deployment to pick up changes
                await RestartKubernetesDeployment(container);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Kubernetes deployment");
                throw;
            }
        }

        private async Task DeleteFromKubernetes(Container container)
        {
            try
            {
                // Delete File Ingress
                await _kubernetesClient.DeleteNamespacedIngressAsync(
                    $"{container.Name.ToLower()}-file-ingress", "default");

                // Delete Ingress
                await _kubernetesClient.DeleteNamespacedIngressAsync(
                    $"{container.Name.ToLower()}-ingress", "default");

                // Delete Service
                await _kubernetesClient.DeleteNamespacedServiceAsync(
                    container.Name.ToLower(), "default");

                // Delete Deployment
                await _kubernetesClient.DeleteNamespacedDeploymentAsync(
                    container.Name.ToLower(), "default");

                // Delete ConfigMap
                await _kubernetesClient.DeleteNamespacedConfigMapAsync(
                    $"{container.Name.ToLower()}-config", "default");

                // Delete Secret
                await _kubernetesClient.DeleteNamespacedSecretAsync(
                    $"{container.Name.ToLower()}-secrets", "default");
            
                // Delete PersistentVolumeClaim for file uploads
                await _kubernetesClient.DeleteNamespacedPersistentVolumeClaimAsync(
                    $"{container.Name.ToLower()}-file-pvc", "default");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting from Kubernetes");
                throw;
            }
        }

        private async Task RestartKubernetesDeployment(Container container)
        {
            try
            {
                // Patch deployment with a simple annotation change to trigger a restart
                var patch = new V1Patch(
                    "{\"spec\":{\"template\":{\"metadata\":{\"annotations\":" +
                    "{\"kubectl.kubernetes.io/restartedAt\":\"" + 
                    DateTime.UtcNow.ToString("o") + "\"}}}}}", 
                    V1Patch.PatchType.StrategicMergePatch);
                
                await _kubernetesClient.PatchNamespacedDeploymentAsync(
                    patch, container.Name.ToLower(), "default");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting Kubernetes deployment");
                throw;
            }
        }

        // Helper method to get the Ingress host
        private async Task<string> GetIngressHost()
        {
            try
            {
                // Try to get the Ingress controller Service to determine the host
                var ingressServices = await _kubernetesClient.ListNamespacedServiceAsync(
                    "ingress-nginx", 
                    labelSelector: "app.kubernetes.io/name=ingress-nginx");
            
                // Look for the ingress controller service
                var ingressService = ingressServices?.Items?.FirstOrDefault(s => s.Metadata.Name.Contains("controller"));
            
                if (ingressService != null)
                {
                    // Check for LoadBalancer IP or hostname
                    if (ingressService.Status?.LoadBalancer?.Ingress != null && 
                        ingressService.Status.LoadBalancer.Ingress.Count > 0)
                    {
                        var ingress = ingressService.Status.LoadBalancer.Ingress.First();
                        if (!string.IsNullOrEmpty(ingress?.Ip))
                            return ingress.Ip;
                        if (!string.IsNullOrEmpty(ingress?.Hostname))
                            return ingress.Hostname;
                    }
            
                    // Check for NodePort
                    var httpPort = ingressService.Spec?.Ports?.FirstOrDefault(p => p.Name == "http");
                    if (httpPort != null && httpPort.NodePort.HasValue)
                    {
                        // Use node IP with node port
                        var nodes = await _kubernetesClient.ListNodeAsync();
                        if (nodes?.Items != null && nodes.Items.Count > 0)
                        {
                            var node = nodes.Items.First();
                            var nodeAddress = node.Status?.Addresses?.FirstOrDefault(a => a.Type == "ExternalIP")?.Address;
                
                            if (!string.IsNullOrEmpty(nodeAddress))
                                return $"{nodeAddress}:{httpPort.NodePort.Value}";
                        }
                    }
                }
            
                // Check if Minikube is being used
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "minikube",
                            Arguments = "ip",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        }
                    };
                
                    process.Start();
                    string minikubeIp = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();
                
                    if (!string.IsNullOrEmpty(minikubeIp?.Trim()))
                        return minikubeIp.Trim();
                }
                catch
                {
                    // Minikube not available, continue with fallback
                }
            
                // If we're here, use the current host
                // Get the server URL from the Kubernetes client config
                var serverUrl = _kubernetesClient.BaseUri?.Host;
                if (!string.IsNullOrEmpty(serverUrl) && serverUrl != "localhost" && serverUrl != "kubernetes.default.svc")
                {
                    return serverUrl;
                }
            
                // Last fallback to localhost
                _logger.LogInformation("Using default hostname 'localhost' for service URL");
                return "localhost";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining ingress host, defaulting to localhost");
                return "localhost";
            }
        }
    }
} 
