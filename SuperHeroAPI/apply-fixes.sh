#!/bin/bash

# Full script to apply all fixes to the file upload functionality

echo "===== SuperHeroAPI File Upload Fix Script ====="
echo "This script will fix the file upload functionality by:"
echo "1. Setting up host directories"
echo "2. Verifying permissions"
echo "3. Restarting Kubernetes deployments"
echo "4. Verifying proper mounting"
echo ""

# Set variables
HOST_DIR="/var/www/ncatbird.ru/html"
HOST_DOCX_DIR="$HOST_DIR/docx"

# 1. Set up host directories
echo "===== Setting up host directories ====="
sudo mkdir -p $HOST_DOCX_DIR
if [ $? -ne 0 ]; then
  echo "ERROR: Failed to create host directories. Please ensure you have sudo access."
  exit 1
fi

echo "Setting directory permissions..."
sudo chmod -R 777 /var/www/ncatbird.ru
if [ $? -ne 0 ]; then
  echo "ERROR: Failed to set directory permissions."
  exit 1
fi

echo "Creating test file in host directory..."
sudo bash -c "echo 'Host test file created at $(date)' > $HOST_DOCX_DIR/host_test.txt"
sudo chmod 777 $HOST_DOCX_DIR/host_test.txt

echo "Host directory setup complete:"
sudo ls -la $HOST_DIR
sudo ls -la $HOST_DOCX_DIR

# 2. Check if Kubernetes is running
echo "===== Checking Kubernetes status ====="
kubectl cluster-info
if [ $? -ne 0 ]; then
  echo "ERROR: Kubernetes is not accessible. Please check your Kubernetes configuration."
  exit 1
fi

# 3. List all deployments
echo "===== Listing deployments ====="
DEPLOYMENTS=$(kubectl get deployments -o jsonpath='{.items[*].metadata.name}')
if [ -z "$DEPLOYMENTS" ]; then
  echo "WARNING: No deployments found in the cluster."
else
  echo "Found deployments: $DEPLOYMENTS"
fi

# 4. Restart deployments
echo "===== Restarting deployments ====="
for DEPLOYMENT in $DEPLOYMENTS
do
  echo "Restarting deployment: $DEPLOYMENT"
  kubectl rollout restart deployment/$DEPLOYMENT
  if [ $? -ne 0 ]; then
    echo "WARNING: Failed to restart $DEPLOYMENT"
  else
    echo "Successfully restarted $DEPLOYMENT"
  fi
done

echo "Waiting for deployments to stabilize..."
kubectl rollout status deployment --all --timeout=300s

# 5. Verify pod status
echo "===== Verifying pod status ====="
PODS=$(kubectl get pods -o jsonpath='{.items[*].metadata.name}')
if [ -z "$PODS" ]; then
  echo "WARNING: No pods found in the cluster."
else
  echo "Found pods: $PODS"
  
  for POD in $PODS
  do
    echo "Checking pod status: $POD"
    STATUS=$(kubectl get pod $POD -o jsonpath='{.status.phase}')
    echo "Pod $POD status: $STATUS"
    
    if [ "$STATUS" == "Running" ]; then
      # Try to create a test file from the pod
      echo "Testing file creation from pod $POD..."
      kubectl exec $POD -- bash -c "mkdir -p /var/www/ncatbird.ru/html/docx && chmod -R 777 /var/www/ncatbird.ru && echo 'Test from pod $POD at $(date)' > /var/www/ncatbird.ru/html/docx/pod_$POD.txt"
      
      # Check if the file appears on the host
      if [ -f "$HOST_DOCX_DIR/pod_$POD.txt" ]; then
        echo "SUCCESS: File created in pod appears on host machine:"
        sudo cat "$HOST_DOCX_DIR/pod_$POD.txt"
      else
        echo "ERROR: File created in pod does not appear on host. Volume mounting is not working properly."
      fi
      
      # Check if host-created file is visible in the pod
      echo "Checking if host files are visible in pod..."
      kubectl exec $POD -- bash -c "ls -la /var/www/ncatbird.ru/html/docx"
      kubectl exec $POD -- bash -c "if [ -f /var/www/ncatbird.ru/html/docx/host_test.txt ]; then echo 'SUCCESS: Host file is visible in pod'; cat /var/www/ncatbird.ru/html/docx/host_test.txt; else echo 'ERROR: Host file is not visible in pod'; fi"
    fi
  done
fi

# 6. Set up nginx configuration if needed
echo "===== Checking Nginx configuration ====="
if kubectl get pods -l app=nginx-ingress-controller -A > /dev/null 2>&1; then
  echo "Nginx ingress controller found. Checking configuration..."
  
  # Look for our ingress
  INGRESS_NAME=$(kubectl get ingress -o jsonpath='{.items[*].metadata.name}' | grep -o '\S*file-ingress\S*')
  if [ -n "$INGRESS_NAME" ]; then
    echo "Found file ingress: $INGRESS_NAME"
    kubectl describe ingress $INGRESS_NAME
  else
    echo "No file ingress found."
  fi
else
  echo "Nginx ingress controller not found. Skip nginx configuration check."
fi

echo "===== File upload fix application complete ====="
echo "Please test the file upload API endpoint now."
echo "Files uploaded through the API should now appear in: $HOST_DOCX_DIR"
echo "And be accessible via your API endpoint." 