#!/bin/bash

# Script to fix host path issues in K8s/Minikube environments by creating symlinks

echo "===== Kubernetes Host Path Fix ====="

# Get the node name where the pod is running
NODE=$(kubectl get pod -o=jsonpath='{.items[0].spec.nodeName}')
if [ -z "$NODE" ]; then
  echo "ERROR: Could not determine the Kubernetes node."
  exit 1
fi

echo "Working with Kubernetes node: $NODE"

# Get more info about the node's paths
echo "Getting node filesystem information..."
kubectl describe node $NODE | grep -i -A 5 "filesystem"

# Create a directory path that will be more likely to work with K8s
KUBE_DIR="/tmp/kubeshare"
DOCX_DIR="${KUBE_DIR}/docx"

echo "Setting up shared directory: ${KUBE_DIR}"
sudo mkdir -p ${DOCX_DIR}
sudo chmod -R 777 ${KUBE_DIR}

# Create a symlink from the desired path to our working path
echo "Creating symlink from /var/www/ncatbird.ru/html to ${KUBE_DIR}..."
sudo mkdir -p /var/www/ncatbird.ru
sudo rm -rf /var/www/ncatbird.ru/html # Remove if exists
sudo ln -sf ${KUBE_DIR} /var/www/ncatbird.ru/html
sudo ls -la /var/www/ncatbird.ru/

# Test if the symlink works
echo "Testing symlink..."
echo "Test file via symlink" | sudo tee /var/www/ncatbird.ru/html/symlink_test.txt
if [ -f "${KUBE_DIR}/symlink_test.txt" ]; then
  echo "SUCCESS: Symlink is working correctly."
else
  echo "ERROR: Symlink not working correctly."
  exit 1
fi

# Update all deployments to use the new path
echo "Patching all deployments to use ${KUBE_DIR}..."
DEPLOYMENTS=$(kubectl get deployments -o jsonpath='{.items[*].metadata.name}')

if [ -z "$DEPLOYMENTS" ]; then
  echo "No deployments found."
  exit 1
fi

for DEPLOYMENT in $DEPLOYMENTS; do
  echo "Patching deployment: $DEPLOYMENT"
  
  # Apply the patch using the direct path
  kubectl patch deployment $DEPLOYMENT --patch '{
    "spec": {
      "template": {
        "spec": {
          "volumes": [
            {
              "name": "file-upload-volume",
              "hostPath": {
                "path": "'${KUBE_DIR}'",
                "type": "DirectoryOrCreate"
              }
            }
          ]
        }
      }
    }
  }' --type=strategic
  
  if [ $? -eq 0 ]; then
    echo "Successfully patched deployment $DEPLOYMENT"
  else
    echo "Failed to patch deployment $DEPLOYMENT"
  fi
done

# Restart deployments
echo "Restarting deployments to apply changes..."
kubectl rollout restart deployment
echo "Waiting for deployments to be ready..."
kubectl rollout status deployment --all --timeout=120s

# Verify fix after restart
echo "Verifying fix after restart..."
sleep 5  # Give time for pods to stabilize

NEW_PODS=$(kubectl get pods --field-selector=status.phase=Running -o jsonpath='{.items[*].metadata.name}')

if [ -z "$NEW_PODS" ]; then
  echo "No running pods found after restart."
else
  echo "Testing with running pods: $NEW_PODS"
  
  # Use the first running pod for testing
  TEST_POD=$(echo $NEW_PODS | cut -d' ' -f1)
  echo "Testing with pod: $TEST_POD"
  
  # Create test files in both directions
  echo "Creating test file on host..."
  TEST_FILE="${DOCX_DIR}/host_test_$(date +%s).txt"
  echo "Host test file created at $(date)" | sudo tee $TEST_FILE
  sudo chmod 777 $TEST_FILE
  
  echo "Creating test file from pod..."
  POD_TEST_FILE="pod_test_$(date +%s).txt"
  kubectl exec $TEST_POD -- sh -c "mkdir -p /var/www/ncatbird.ru/html/docx && echo 'Pod test file created at $(date)' > /var/www/ncatbird.ru/html/docx/$POD_TEST_FILE"
  
  # Verify bidirectional visibility
  echo "Checking if pod's file appears on host..."
  if [ -f "${DOCX_DIR}/$POD_TEST_FILE" ]; then
    echo "SUCCESS: File created from pod appears on host:"
    cat "${DOCX_DIR}/$POD_TEST_FILE"
  else
    echo "FAILURE: File created from pod does NOT appear on host."
    ls -la ${DOCX_DIR}
  fi
  
  echo "Checking if host file is visible in pod..."
  kubectl exec $TEST_POD -- sh -c "ls -la /var/www/ncatbird.ru/html/docx && if [ -f '/var/www/ncatbird.ru/html/docx/$(basename $TEST_FILE)' ]; then echo 'SUCCESS: Host file is visible in pod'; cat '/var/www/ncatbird.ru/html/docx/$(basename $TEST_FILE)'; else echo 'FAILURE: Host file is NOT visible in pod'; fi"
fi

echo "Fix completed. Test file uploads through your API now."
echo "Files should appear in: ${DOCX_DIR}"
echo "They should also be accessible via symlink at: /var/www/ncatbird.ru/html/docx" 