param (
    [Parameter(Mandatory=$true)]
    [string]$ContainerName
)

Write-Host "Testing access to $ContainerName container..." -ForegroundColor Cyan

# Test Ingress access to the container
Write-Host "`nTesting ingress access..." -ForegroundColor Yellow
Write-Host "Testing Swagger UI:"
try {
    $response = Invoke-WebRequest -Uri "http://localhost/$ContainerName/swagger" -UseBasicParsing
    Write-Host "✅ Swagger UI accessible: Status $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ Swagger UI not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nTesting Swagger JSON:"
try {
    $response = Invoke-WebRequest -Uri "http://localhost/$ContainerName/swagger/v1/swagger.json" -UseBasicParsing
    Write-Host "✅ Swagger JSON accessible: Status $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ Swagger JSON not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nTesting SuperHero API:"
try {
    $response = Invoke-WebRequest -Uri "http://localhost/$ContainerName/api/SuperHero" -UseBasicParsing
    Write-Host "✅ SuperHero API accessible: Status $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ SuperHero API not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Test direct pod port-forward access
Write-Host "`nTesting pod access via port-forwarding..." -ForegroundColor Yellow
$pod = kubectl get pods -l app=$ContainerName -o jsonpath="{.items[0].metadata.name}"

if ($pod) {
    Write-Host "Found pod: $pod"
    Write-Host "Starting port-forward on port 8088..."
    
    # Start port-forward in the background
    $job = Start-Job -ScriptBlock {
        param($pod)
        kubectl port-forward pod/$pod 8088:8080
    } -ArgumentList $pod
    
    # Give port-forward time to start
    Start-Sleep -Seconds 2
    
    # Test access through port-forward
    Write-Host "`nTesting Swagger UI through port-forward:"
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:8088/server/swagger" -UseBasicParsing
        Write-Host "✅ Swagger UI accessible via port-forward: Status $($response.StatusCode)" -ForegroundColor Green
    } catch {
        Write-Host "❌ Swagger UI not accessible via port-forward: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Stop the port-forward job
    Stop-Job $job
    Remove-Job $job
} else {
    Write-Host "❌ No pod found for app=$ContainerName" -ForegroundColor Red
}

Write-Host "`nTest complete!" -ForegroundColor Cyan 