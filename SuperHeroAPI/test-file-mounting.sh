#!/bin/bash

# Script to directly test file mounting from inside a pod

if [ "$#" -lt 1 ]; then
  echo "Usage: $0 <pod-name>"
  echo "Example: $0 superhero-api-79db54bd8d-x7h7v"
  exit 1
fi

POD_NAME=$1
echo "Testing file mounting from pod: $POD_NAME"

# Host directories
HOST_DIR="/var/www/ncatbird.ru/html"
HOST_DOCX_DIR="$HOST_DIR/docx"

# Create host directories first
echo "Creating host directories and setting permissions..."
sudo mkdir -p $HOST_DOCX_DIR
sudo chmod -R 777 /var/www/ncatbird.ru
sudo ls -la $HOST_DOCX_DIR

# Create a test file on the host
UNIQUE_ID=$(date +%s)
HOST_TEST_FILE="$HOST_DOCX_DIR/host_file_$UNIQUE_ID.txt"
echo "Creating test file on host: $HOST_TEST_FILE"
echo "This is a file created on host at $(date). ID: $UNIQUE_ID" | sudo tee $HOST_TEST_FILE
sudo chmod 777 $HOST_TEST_FILE

# Try to access the pod
echo "Checking pod exists..."
kubectl get pod $POD_NAME > /dev/null
if [ $? -ne 0 ]; then
  echo "ERROR: Pod $POD_NAME not found."
  exit 1
fi

# Create a test file from the pod
POD_TEST_FILE="pod_file_$UNIQUE_ID.txt"
echo "Creating test file from pod: $POD_TEST_FILE"
kubectl exec $POD_NAME -- sh -c "echo 'This is a file created from pod $POD_NAME at $(date). ID: $UNIQUE_ID' > /var/www/ncatbird.ru/html/docx/$POD_TEST_FILE" || echo "Failed to create file in pod"

# Check what's in the pod's directory
echo "Listing directory in pod..."
kubectl exec $POD_NAME -- sh -c "ls -la /var/www/ncatbird.ru/html/docx" || echo "Failed to list directory in pod"

# Check if pod's file appears on host
echo "Checking if pod's file appears on host..."
if [ -f "$HOST_DOCX_DIR/$POD_TEST_FILE" ]; then
  echo "SUCCESS: File created from pod appears on host:"
  cat "$HOST_DOCX_DIR/$POD_TEST_FILE"
else
  echo "FAILURE: File created from pod does NOT appear on host."
  sudo ls -la $HOST_DOCX_DIR
fi

# Check if host file is visible in pod
echo "Checking if host file is visible in pod..."
kubectl exec $POD_NAME -- sh -c "if [ -f '/var/www/ncatbird.ru/html/docx/$(basename $HOST_TEST_FILE)' ]; then echo 'SUCCESS: Host file is visible in pod'; cat '/var/www/ncatbird.ru/html/docx/$(basename $HOST_TEST_FILE)'; else echo 'FAILURE: Host file is NOT visible in pod'; fi" || echo "Failed to check file in pod"

# Check pod mount points
echo "Checking pod mount points..."
kubectl exec $POD_NAME -- sh -c "mount | grep ncatbird" || echo "No relevant mount points found"

# Check pod volume information
echo "Checking pod volume information..."
kubectl describe pod $POD_NAME | grep -A 10 "Volumes:" || echo "No volume information found"

echo "Test complete. Please analyze the results to determine the mount issue." 