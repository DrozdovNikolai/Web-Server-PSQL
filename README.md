# SuperHero API Kubernetes Deployment

This project provides a Dockerized .NET 8 API with Kubernetes deployment and a Vue.js admin panel for managing containers.

## Prerequisites

- Docker installed and running
- Kubernetes cluster (local like Minikube/Docker Desktop or remote)
- kubectl configured to access your cluster
- Node.js and npm for the admin panel

## Project Structure

```
├── SuperHeroAPI/         # .NET 8 API source code
├── admin-panel/          # Vue.js admin panel
├── k8s/                  # Kubernetes manifest files
│   ├── configmap.yaml    # ConfigMap for non-sensitive configuration
│   ├── deployment.yaml   # Deployment definition
│   ├── namespace.yaml    # Namespace definition
│   ├── secret.yaml       # Secret for sensitive configuration
│   └── service.yaml      # Service definition
├── Dockerfile            # Dockerfile for building the API container
├── deploy.ps1            # PowerShell deployment script
└── README.md             # This file
```

## Setup Instructions

### 1. Build and Deploy the API

Run the deployment script:

```powershell
.\deploy.ps1
```

This will:
- Build the Docker image for the API
- Create the Kubernetes namespace
- Apply all Kubernetes configurations
- Deploy the API to your Kubernetes cluster

### 2. Run the Admin Panel

```bash
cd admin-panel
npm install
npm run serve
```

The admin panel will be available at http://localhost:8080

## Admin Panel Features

- List all deployed containers
- View container status and details
- Create new container deployments
- Update existing container configurations
- Delete containers
- Restart containers

## Environment Configuration

Each container deployment requires the following environment variables:

- DB_HOST: PostgreSQL database host
- DB_PORT: PostgreSQL database port
- DB_NAME: PostgreSQL database name
- DB_USER: PostgreSQL database user
- DB_PASSWORD: PostgreSQL database password
- DB_USERNAME: Admin username
- DB_PASSWORD_USER: Admin password

## Kubernetes Configuration

- Deployments use a ConfigMap for non-sensitive configuration
- Sensitive data is stored in Kubernetes Secrets
- Each deployment gets its own Service for network access

## Troubleshooting

- Check container logs: `kubectl logs -n superhero-api deployment/[container-name]`
- Check pod status: `kubectl get pods -n superhero-api`
- View service details: `kubectl describe service [service-name] -n superhero-api` 