param(
    [string]$Image = "postgres-control-plane:1.0.0"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path "k8s/secret.yaml")) {
    throw "Create k8s/secret.yaml from k8s/secret.example.yaml before deploying."
}

Write-Host "Building $Image" -ForegroundColor Green
docker build --tag $Image .

Write-Host "Applying namespace-scoped resources" -ForegroundColor Green
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/service-account.yaml
kubectl apply -f k8s/k8s-role.yaml
kubectl apply -f k8s/k8s-rolebinding.yaml
kubectl apply -f k8s/resource-limits.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/persistent-volume-claim.yaml
kubectl apply -f k8s/network-policy.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml

kubectl set image deployment/postgres-control api=$Image --namespace postgres-control
kubectl rollout status deployment/postgres-control --namespace postgres-control --timeout 180s

Write-Host "Deployment is ready. Apply k8s/ingress.example.yaml only after configuring its hostname and TLS secret." -ForegroundColor Cyan
