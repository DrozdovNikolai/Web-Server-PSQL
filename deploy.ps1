# Container deployment script for SuperHeroAPI

# Build the Docker image
Write-Host "Building Docker image..." -ForegroundColor Green
docker build -t superhero-api:latest .

# Check if Kubernetes is available
try {
    $kubeVersion = kubectl version --short
    Write-Host "Kubernetes is available: $kubeVersion" -ForegroundColor Green
} catch {
    Write-Host "Error: Kubernetes is not available or not properly configured." -ForegroundColor Red
    Write-Host "Please ensure kubectl is installed and configured correctly." -ForegroundColor Yellow
    exit 1
}

# Create the Kubernetes namespace if it doesn't exist
Write-Host "Ensuring namespace exists..." -ForegroundColor Green
kubectl apply -f k8s/namespace.yaml

# Apply Kubernetes configurations
Write-Host "Applying Kubernetes configurations..." -ForegroundColor Green
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s-role.yaml
kubectl apply -f k8s-rolebinding.yaml

kubectl apply -f k8s/persistent-volume.yaml
kubectl apply -f k8s/persistent-volume-claim.yaml
kubectl apply -f k8s/file-ingress.yaml
# Check deployment status
Write-Host "Checking deployment status..." -ForegroundColor Green
kubectl get deployments,pods,services,configmaps,secrets -n default

Write-Host "Deployment completed successfully!" -ForegroundColor Green
Write-Host "You can access the admin panel at: http://localhost:8080" -ForegroundColor Cyan 