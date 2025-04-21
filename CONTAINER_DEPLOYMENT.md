# Container Deployment Guide

This guide explains how to deploy and access SuperHeroAPI containers in Kubernetes.

## Access Methods

There are two ways to access your deployed containers:

1. **Through Ingress** - Uses the URL pattern `http://localhost/YOUR_CONTAINER_NAME/swagger`
2. **Through NodePort** - Uses the URL pattern `http://localhost:YOUR_NODEPORT/server/swagger`

## Quick Deploy

To deploy a new container:

```powershell
./deploy-container.ps1 -ContainerName "myapp" -DbHost "postgres" -DbPort "5432" -DbName "mydb" -DbUser "postgres" -DbPassword "mysecretpassword" -DbUsername "Admin" -DbPasswordUser "Admin" -NodePort 30050
```

## Troubleshooting

If you encounter a `502 Bad Gateway` error:

1. Verify the pod is running:
   ```
   kubectl get pods
   ```

2. Check the logs for errors:
   ```
   kubectl logs YOUR_POD_NAME
   ```

3. Ensure the service is properly targeting port 8080:
   ```
   kubectl describe service YOUR_SERVICE_NAME
   ```
   The `targetPort` should be 8080, not 80.

4. Check the ingress configuration:
   ```
   kubectl describe ingress YOUR_INGRESS_NAME
   ```
   The rewrite target should be `/server/$2` to properly handle paths.

## Manual Deployment

If you need to create a deployment manually, follow these steps:

1. Create a Deployment:
   ```yaml
   apiVersion: apps/v1
   kind: Deployment
   metadata:
     name: YOUR_CONTAINER_NAME
   spec:
     # ... (see container-template.yaml)
   ```

2. Create a Service (ClusterIP):
   ```yaml
   apiVersion: v1
   kind: Service
   metadata:
     name: YOUR_CONTAINER_NAME
   spec:
     # ... with targetPort: 8080
   ```

3. Create an Ingress:
   ```yaml
   apiVersion: networking.k8s.io/v1
   kind: Ingress
   metadata:
     annotations:
       nginx.ingress.kubernetes.io/rewrite-target: /server/$2
   spec:
     # ... (see container-template.yaml)
   ```

4. Optionally, create a NodePort service for direct access:
   ```yaml
   apiVersion: v1
   kind: Service
   metadata:
     name: YOUR_CONTAINER_NAME-nodeport
   spec:
     # ... with targetPort: 8080
   ```

## Common Issues

1. **Port mismatch**: The SuperHeroAPI application listens on port 8080 inside the container, so services must target port 8080.
2. **Path rewrite**: Use `/server$1` as the rewrite target to properly handle paths.
3. **Path pattern**: Use `/{container}(/.*)?` with pathType `ImplementationSpecific` for best compatibility.
4. **Ingress conflicts**: Each path must be unique across all ingresses.

## Testing Container Access

You can use the included `test-container-access.ps1` script to verify if your container is working properly:

```powershell
./test-container-access.ps1 -ContainerName yourcontainer
```

This script will:
- Test access to the Swagger UI through the ingress
- Test access to the Swagger JSON definition
- Test access to the SuperHero API
- Test direct access through port-forwarding

If you're experiencing issues, this script can help identify where the problem is occurring. 