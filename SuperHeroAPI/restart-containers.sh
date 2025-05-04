#!/bin/bash

# Script to restart containers after applying fixes

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
  kubectl exec $POD -- /bin/sh -c "mkdir -p /var/www/ncatbird.ru/html/docx && chmod -R 777 /var/www/ncatbird.ru && echo 'test' > /var/www/ncatbird.ru/html/docx/test.txt && ls -la /var/www/ncatbird.ru/html/docx"
  
  if [ $? -eq 0 ]; then
    echo "Successfully created and tested directories on $POD"
  else
    echo "Warning: Could not set up directories on $POD"
  fi
done

echo "Container restart and setup complete. Test file upload functionality now." 