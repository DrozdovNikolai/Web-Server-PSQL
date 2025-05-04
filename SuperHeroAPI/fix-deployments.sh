#!/bin/bash

# Script to fix existing deployments by patching them to use hostPath volumes

echo "===== Fixing Kubernetes Deployments for File Access ====="

# Set up host directories first
HOST_DIR="/var/www/ncatbird.ru/html"
HOST_DOCX_DIR="$HOST_DIR/docx"

# Create directories with sudo
echo "Setting up host directories..."
sudo mkdir -p $HOST_DOCX_DIR
sudo chmod -R 777 /var/www/ncatbird.ru
echo "Created host directories with full permissions"

# List all deployments
DEPLOYMENTS=$(kubectl get deployments -o jsonpath='{.items[*].metadata.name}')

if [ -z "$DEPLOYMENTS" ]; then
  echo "No deployments found. Exiting."
  exit 1
fi

echo "Found deployments: $DEPLOYMENTS"

# Loop through each deployment and update it
for DEPLOYMENT in $DEPLOYMENTS; do
  echo "Processing deployment: $DEPLOYMENT"
  
  # Create a patch to update the volume definition to use hostPath
  PATCH=$(cat <<EOF
{
  "spec": {
    "template": {
      "spec": {
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
  echo "Patching deployment $DEPLOYMENT to use hostPath volume..."
  echo "$PATCH" | kubectl patch deployment $DEPLOYMENT --patch-file /dev/stdin
  
  if [ $? -eq 0 ]; then
    echo "Successfully patched deployment $DEPLOYMENT"
  else
    echo "Failed to patch deployment $DEPLOYMENT"
  fi
done

echo "Waiting for deployments to stabilize..."
kubectl rollout status deployment --all --timeout=300s

echo "Testing file access in pods..."
PODS=$(kubectl get pods --field-selector=status.phase=Running -o jsonpath='{.items[*].metadata.name}')

for POD in $PODS; do
  echo "Testing file access in pod: $POD"
  
  # Create a test file from the pod
  kubectl exec $POD -- sh -c "mkdir -p /var/www/ncatbird.ru/html/docx && echo 'Test from pod $POD' > /var/www/ncatbird.ru/html/docx/test_$POD.txt" || echo "Failed to create test file in pod $POD"
  
  # Check if the file appears on the host
  if [ -f "$HOST_DOCX_DIR/test_$POD.txt" ]; then
    echo "SUCCESS: File from pod $POD is visible on the host"
    cat "$HOST_DOCX_DIR/test_$POD.txt"
  else
    echo "WARNING: File from pod $POD is not visible on the host"
  fi
done

echo "===== Deployment Fix Complete ====="
echo "Now try uploading a file through your API endpoint."
echo "Files should now appear in $HOST_DOCX_DIR on your host machine." 