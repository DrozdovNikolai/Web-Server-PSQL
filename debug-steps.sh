#!/bin/bash

# Check ingress configurations
echo "=== INGRESS CONFIGURATIONS ==="
kubectl describe ingress tsts6-ingress
kubectl describe ingress tsts6-file-ingress

# Check the nginx ingress controller configuration
echo -e "\n=== NGINX INGRESS CONTROLLER ==="
kubectl get pods -n ingress-nginx
kubectl describe configmap -n ingress-nginx nginx-configuration

# Check ingress controller logs
echo -e "\n=== INGRESS CONTROLLER LOGS ==="
NGINX_POD=$(kubectl get pods -n ingress-nginx -l app.kubernetes.io/name=ingress-nginx -o jsonpath='{.items[0].metadata.name}')
kubectl logs -n ingress-nginx $NGINX_POD --tail=50

# Check services
echo -e "\n=== SERVICES ==="
kubectl get svc
kubectl describe svc tsts6

# Test the service directly
echo -e "\n=== TESTING SERVICE DIRECTLY ==="
kubectl get pods -l app=tsts6
POD_NAME=$(kubectl get pods -l app=tsts6 -o jsonpath='{.items[0].metadata.name}')
if [ ! -z "$POD_NAME" ]; then
  echo "Testing service in pod $POD_NAME..."
  kubectl exec $POD_NAME -- curl -v localhost:8080/swagger/index.html
fi

# Check if SSL redirect is enabled
echo -e "\n=== CHECKING SSL SETTINGS ==="
kubectl get cm -n ingress-nginx nginx-configuration -o yaml | grep ssl-redirect 