#!/bin/bash

# Script to setup host directories and restart containers after applying fixes

# First, set up the host directories
echo "Setting up host directories..."
HOST_DIR="/var/www/ncatbird.ru/html"
HOST_DOCX_DIR="$HOST_DIR/docx"

# Create directory structure on host
sudo mkdir -p $HOST_DOCX_DIR
if [ $? -ne 0 ]; then
  echo "Failed to create host directories. Please ensure you have sudo access."
  exit 1
fi

# Set open permissions
sudo chmod -R 777 /var/www/ncatbird.ru
if [ $? -ne 0 ]; then
  echo "Failed to set permissions on host directories."
  exit 1
fi

echo "Host directories created and permissions set."
sudo ls -la $HOST_DIR
sudo ls -la $HOST_DOCX_DIR

# Get all container deployments
DEPLOYMENTS=$(kubectl get deployments -o jsonpath='{.items[*].metadata.name}')

echo "Found the following deployments:"
echo $DEPLOYMENTS

# Restart each deployment to apply changes
for DEPLOYMENT in $DEPLOYMENTS
do
  echo "Restarting deployment: $DEPLOYMENT"
  kubectl rollout restart deployment/$DEPLOYMENT
  
  # Check if the restart was successful
  if [ $? -eq 0 ]; then
    echo "Successfully restarted $DEPLOYMENT"
  else
    echo "Failed to restart $DEPLOYMENT"
  fi
done

echo "Waiting for deployments to stabilize..."
kubectl rollout status deployment --all --timeout=300s

echo "Creating and checking file directories on running containers..."
for POD in $(kubectl get pods -o jsonpath='{.items[*].metadata.name}')
do
  echo "Checking directories on pod: $POD"
  kubectl exec $POD -- /bin/sh -c "mkdir -p /var/www/ncatbird.ru/html/docx && chmod -R 777 /var/www/ncatbird.ru && echo 'Pod $POD test file at $(date)' > /var/www/ncatbird.ru/html/docx/pod_test_file.txt && ls -la /var/www/ncatbird.ru/html/docx"
  
  if [ $? -eq 0 ]; then
    echo "Successfully created and tested directories on $POD"
    # Check if the file appears on the host
    if [ -f "$HOST_DOCX_DIR/pod_test_file.txt" ]; then
      echo "SUCCESS: Test file from pod appeared on host at $HOST_DOCX_DIR"
      sudo cat "$HOST_DOCX_DIR/pod_test_file.txt"
    else
      echo "WARNING: Test file from pod did not appear on host. Volume mounting may not be working correctly."
      sudo ls -la $HOST_DOCX_DIR
    fi
  else
    echo "Warning: Could not set up directories on $POD"
  fi
done

echo "Testing file creation on host that should appear in containers..."
TEST_FILE="$HOST_DOCX_DIR/host_created_test_file.txt"
sudo echo "Host created test file at $(date)" > $TEST_FILE
sudo chmod 777 $TEST_FILE

echo "Container restart and setup complete. Now test file upload functionality."
echo "Files uploaded through the API should now appear in $HOST_DOCX_DIR" 