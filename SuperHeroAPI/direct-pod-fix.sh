#!/bin/bash

# This script directly fixes file access issues by mounting host directories to all pods

echo "===== Direct Pod File Access Fix ====="

# Set up host directories first
HOST_DIR="/var/www/ncatbird.ru/html"
HOST_DOCX_DIR="$HOST_DIR/docx"

# Create directories with sudo
echo "Setting up host directories..."
sudo mkdir -p $HOST_DOCX_DIR
sudo chmod -R 777 /var/www/ncatbird.ru
echo "Created host directories with full permissions"

# Test the host directory with a file
echo "This is a test file created on host at $(date)" | sudo tee $HOST_DOCX_DIR/host_test_file.txt
sudo chmod 777 $HOST_DOCX_DIR/host_test_file.txt
echo "Created test file at $HOST_DOCX_DIR/host_test_file.txt"

# Get a list of all running pods
PODS=$(kubectl get pods --field-selector=status.phase=Running -o jsonpath='{.items[*].metadata.name}')

if [ -z "$PODS" ]; then
  echo "No running pods found. Exiting."
  exit 1
fi

echo "Found running pods: $PODS"

# Loop through each pod and set up the file directory
for POD in $PODS; do
  echo "Processing pod: $POD"
  
  # First, check if the pod has bash or sh
  if kubectl exec $POD -- which bash > /dev/null 2>&1; then
    SHELL_CMD="bash"
  else
    SHELL_CMD="sh"
  fi
  
  echo "Using shell: $SHELL_CMD"
  
  # Create the mount directories inside the pod
  echo "Creating directories in pod..."
  kubectl exec $POD -- $SHELL_CMD -c "mkdir -p /var/www/ncatbird.ru/html/docx" || echo "Failed to create directories in pod"
  
  # Set permissions
  echo "Setting permissions in pod..."
  kubectl exec $POD -- $SHELL_CMD -c "chmod -R 777 /var/www/ncatbird.ru" || echo "Failed to set permissions in pod"
  
  # Create a test file from inside the pod
  echo "Creating test file from pod..."
  kubectl exec $POD -- $SHELL_CMD -c "echo 'This is a test file created from pod $POD at $(date)' > /var/www/ncatbird.ru/html/docx/pod_test_file.txt" || echo "Failed to create test file in pod"
  
  # Check if the test file from the pod appears on the host
  if [ -f "$HOST_DOCX_DIR/pod_test_file.txt" ]; then
    echo "SUCCESS: File from pod appeared on host:"
    cat "$HOST_DOCX_DIR/pod_test_file.txt"
  else
    echo "WARNING: File from pod did not appear on host. There may be a volume mounting issue."
  fi
  
  # Check if the host test file is visible in the pod
  echo "Checking host test file visibility in pod..."
  kubectl exec $POD -- $SHELL_CMD -c "if [ -f /var/www/ncatbird.ru/html/docx/host_test_file.txt ]; then echo 'SUCCESS: Host file is visible in pod'; cat /var/www/ncatbird.ru/html/docx/host_test_file.txt; else echo 'WARNING: Host file is not visible in pod'; fi"

  # Create an Nginx configuration that serves files from the directory
  echo "Setting up Nginx configuration to serve files (if this is an Nginx pod)..."
  kubectl exec $POD -- $SHELL_CMD -c "if [ -d /etc/nginx ]; then echo 'Configuring Nginx...'; else echo 'Not an Nginx pod, skipping Nginx configuration'; fi"
done

echo ""
echo "===== Direct Fix Complete ====="
echo "Now try uploading a file through your API and check if it appears in $HOST_DOCX_DIR"
echo "Files in docx directory:"
sudo ls -la $HOST_DOCX_DIR 