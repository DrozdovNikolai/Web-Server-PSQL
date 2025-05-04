#!/bin/bash

# Script to directly fix the host mount issue and verify file storage

echo "===== Direct Host Mount Fix ====="

# Create host directories with proper permissions
HOST_DIR="/var/www/ncatbird.ru/html"
HOST_DOCX_DIR="$HOST_DIR/docx"

echo "Setting up host directories with proper permissions..."
sudo mkdir -p $HOST_DOCX_DIR
sudo chmod -R 777 /var/www/ncatbird.ru
echo "Created and set permissions for $HOST_DOCX_DIR"

# Create test files both on host and from the pod to verify 
# bidirectional visibility
echo "Creating test file on host..."
TEST_FILE="$HOST_DOCX_DIR/host_test_file_$(date +%s).txt"
echo "This is a test file created on host at $(date)" | sudo tee $TEST_FILE
sudo chmod 777 $TEST_FILE
echo "Created test file: $TEST_FILE"

# List all running pods
echo "Finding running pods..."
PODS=$(kubectl get pods --field-selector=status.phase=Running -o jsonpath='{.items[*].metadata.name}')

if [ -z "$PODS" ]; then
  echo "No running pods found."
else
  echo "Found running pods: $PODS"

  for POD in $PODS; do
    echo "Testing file creation from pod $POD..."
    
    # Create test file from pod
    POD_TEST_FILE="pod_test_from_${POD}_$(date +%s).txt"
    kubectl exec $POD -- sh -c "echo 'Test file created from pod $POD at $(date)' > /var/www/ncatbird.ru/html/docx/$POD_TEST_FILE" || echo "Failed to create file in pod"
    
    # Check if file exists on host
    if [ -f "$HOST_DOCX_DIR/$POD_TEST_FILE" ]; then
      echo "SUCCESS: File created from pod $POD appears on host:"
      cat "$HOST_DOCX_DIR/$POD_TEST_FILE"
    else
      echo "FAILURE: File created from pod $POD does NOT appear on host."
      echo "This indicates the volume mount is not working correctly."
    fi
    
    # Check if host test file is visible in pod
    echo "Checking if host-created file is visible in pod..."
    kubectl exec $POD -- sh -c "if [ -f '$(basename $TEST_FILE)' ]; then echo 'SUCCESS: Host file is visible in pod'; cat '$(basename $TEST_FILE)'; else echo 'FAILURE: Host file is NOT visible in pod'; fi" || echo "Failed to check file in pod"
  done
fi

# Fix the volume mounting
echo "Applying volume mount fixes to all deployments..."

# Get all deployments
DEPLOYMENTS=$(kubectl get deployments -o jsonpath='{.items[*].metadata.name}')

if [ -z "$DEPLOYMENTS" ]; then
  echo "No deployments found."
else
  echo "Found deployments: $DEPLOYMENTS"

  for DEPLOYMENT in $DEPLOYMENTS; do
    echo "Patching deployment: $DEPLOYMENT"
    
    # Apply the volume patch
    kubectl patch deployment $DEPLOYMENT --patch '{
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
            ],
            "containers": [
              {
                "name": "'$DEPLOYMENT'",
                "volumeMounts": [
                  {
                    "name": "file-upload-volume",
                    "mountPath": "/var/www/ncatbird.ru/html",
                    "readOnly": false
                  }
                ]
              }
            ]
          }
        }
      }
    }' --type=strategic || echo "Failed to patch deployment $DEPLOYMENT"
  done

  echo "Restarting deployments for changes to take effect..."
  kubectl rollout restart deployment
  kubectl rollout status deployment --timeout=120s
fi

# Verify fix after restart
echo "Verifying fix after restart..."
sleep 5  # Give time for pods to stabilize

NEW_PODS=$(kubectl get pods --field-selector=status.phase=Running -o jsonpath='{.items[*].metadata.name}')

if [ -z "$NEW_PODS" ]; then
  echo "No running pods found after restart."
else
  echo "Found running pods after restart: $NEW_PODS"

  for POD in $NEW_PODS; do
    echo "Testing file creation from pod $POD after fix..."
    
    # Create test file from pod
    POD_TEST_FILE="pod_test_after_fix_from_${POD}_$(date +%s).txt"
    kubectl exec $POD -- sh -c "echo 'Test file created from pod $POD after fix at $(date)' > /var/www/ncatbird.ru/html/docx/$POD_TEST_FILE" || echo "Failed to create file in pod"
    
    # Check if file exists on host
    if [ -f "$HOST_DOCX_DIR/$POD_TEST_FILE" ]; then
      echo "SUCCESS: File created from pod $POD after fix appears on host:"
      cat "$HOST_DOCX_DIR/$POD_TEST_FILE"
    else
      echo "FAILURE: File created from pod $POD after fix does NOT appear on host."
      echo "The volume mount is still not working correctly."
    fi
  done
fi

echo "Fix attempt completed."
echo "Check the host directory for files: $HOST_DOCX_DIR"
ls -la $HOST_DOCX_DIR
echo ""
echo "If no files are visible, there may be a more complex issue with your Kubernetes setup."
echo "Try manually creating files in this directory while monitoring both the host and container." 