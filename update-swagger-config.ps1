# Build the Docker image with the updated Swagger configuration
Write-Host "Building updated Docker image..."
docker build -t superhero-api:latest .

# Get all superhero-api deployments
Write-Host "Retrieving deployments..."
$deployments = kubectl get deployments -o jsonpath='{.items[*].metadata.name}'

# Restart each deployment to pick up the new image
foreach ($deployment in $deployments.Split()) {
    Write-Host "Restarting deployment: $deployment"
    
    # Patch the deployment to force a restart
    kubectl patch deployment $deployment -p "{\"spec\":{\"template\":{\"metadata\":{\"annotations\":{\"kubectl.kubernetes.io/restartedAt\":\"`$(Get-Date -Format o)\"}}}}}"
}

Write-Host "All deployments updated. The Swagger UI should now work correctly with container paths."
Write-Host "Access your containers at: http://localhost/{container-name}/server/swagger" 