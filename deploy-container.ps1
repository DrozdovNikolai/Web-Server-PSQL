param (
    [Parameter(Mandatory=$true)]
    [string]$ContainerName,
    
    [Parameter(Mandatory=$true)]
    [string]$DbHost,
    
    [Parameter(Mandatory=$true)]
    [string]$DbPort,
    
    [Parameter(Mandatory=$true)]
    [string]$DbName,
    
    [Parameter(Mandatory=$true)]
    [string]$DbUser,
    
    [Parameter(Mandatory=$true)]
    [string]$DbPassword,
    
    [Parameter(Mandatory=$true)]
    [string]$DbUsername,
    
    [Parameter(Mandatory=$true)]
    [string]$DbPasswordUser,
    
    [Parameter(Mandatory=$false)]
    [int]$NodePort = 30000
)

# Read template file
$template = Get-Content -Path "container-template.yaml" -Raw

# Replace placeholders with parameters
$containerYaml = $template -replace "CONTAINER_NAME", $ContainerName
$containerYaml = $containerYaml -replace "HOST_VALUE", $DbHost
$containerYaml = $containerYaml -replace "PORT_VALUE", $DbPort
$containerYaml = $containerYaml -replace "NAME_VALUE", $DbName
$containerYaml = $containerYaml -replace "USER_VALUE", $DbUser
$containerYaml = $containerYaml -replace "PASSWORD_VALUE", $DbPassword
$containerYaml = $containerYaml -replace "USERNAME_VALUE", $DbUsername
$containerYaml = $containerYaml -replace "PASSWORD_USER_VALUE", $DbPasswordUser
$containerYaml = $containerYaml -replace "3XXXX", $NodePort

# Save to a temporary file
$tempFile = ".\${ContainerName}-deployment.yaml"
$containerYaml | Out-File -FilePath $tempFile

# Apply the configuration
Write-Host "Deploying container $ContainerName..."
kubectl apply -f $tempFile

Write-Host "`nDeployment completed.`n"
Write-Host "You can access the container at:"
Write-Host "  - Ingress URL: http://localhost/$ContainerName/swagger"
Write-Host "  - NodePort URL: http://localhost:$NodePort/server/swagger"
Write-Host "`nTo check status, run:"
Write-Host "  kubectl get pods | Select-String $ContainerName" 