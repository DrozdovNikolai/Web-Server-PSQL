#!/bin/bash

# Script to fix container permissions on an existing deployment

# Get the name of the failing deployment
if [ -z "$1" ]; then
  echo "Usage: $0 <deployment-name>"
  echo "Example: $0 testa6r"
  exit 1
fi

DEPLOYMENT_NAME=$1
echo "Fixing permissions for deployment: $DEPLOYMENT_NAME"

# Create a patch to add security context to the init container
PATCH=$(cat <<EOF
{
  "spec": {
    "template": {
      "spec": {
        "initContainers": [
          {
            "name": "init-file-permissions",
            "securityContext": {
              "privileged": true,
              "runAsUser": 0
            }
          }
        ],
        "volumes": [
          {
            "name": "file-upload-volume",
            "hostPath": {
              "path": "/var/www/ncatbird.ru/html",
              "type": "DirectoryOrCreate"
            }
          }
        ]
      }
    }
  }
}
EOF
)

# Apply the patch
echo "Applying security context patch to deployment..."
echo "$PATCH" | kubectl patch deployment $DEPLOYMENT_NAME --patch-file /dev/stdin --type=strategic

# Create the host directory with proper permissions
echo "Setting up host directory with proper permissions..."
sudo mkdir -p /var/www/ncatbird.ru/html/docx
sudo chmod -R 777 /var/www/ncatbird.ru

# Delete the failing pod to force Kubernetes to create a new one with the patch
echo "Deleting pods for deployment to force recreation..."
POD_NAME=$(kubectl get pods | grep $DEPLOYMENT_NAME | awk '{print $1}')
if [ -n "$POD_NAME" ]; then
  kubectl delete pod $POD_NAME
  echo "Deleted pod $POD_NAME. A new pod will be created with the updated configuration."
else
  echo "No pods found for deployment $DEPLOYMENT_NAME."
fi

# Wait for the new pod to be created
echo "Waiting for new pod to be created..."
sleep 5

# Check the status of the new pod
NEW_POD_NAME=$(kubectl get pods | grep $DEPLOYMENT_NAME | awk '{print $1}')
if [ -n "$NEW_POD_NAME" ]; then
  echo "New pod created: $NEW_POD_NAME"
  echo "Checking pod status..."
  kubectl get pod $NEW_POD_NAME
  
  # Wait for the pod to initialize
  echo "Waiting for pod to initialize..."
  kubectl wait --for=condition=Ready pod/$NEW_POD_NAME --timeout=60s
  
  if [ $? -eq 0 ]; then
    echo "SUCCESS: Pod is now running successfully."
  else
    echo "WARNING: Pod is not ready yet. Check logs for more details:"
    kubectl describe pod $NEW_POD_NAME
  fi
else
  echo "ERROR: No new pod found for deployment $DEPLOYMENT_NAME."
fi

echo "Fix completed. Monitor the pod status with: kubectl get pods" 