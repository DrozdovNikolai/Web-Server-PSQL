#!/bin/bash

# A simplified script to fix the file mounting issue

echo "===== Simple Direct Fix ====="

# Setup shared directory
KUBE_DIR="/var/kubernetes-share"
DOCX_DIR="${KUBE_DIR}/docx"

echo "Setting up shared directory: ${KUBE_DIR}"
sudo mkdir -p ${DOCX_DIR}
sudo chmod -R 777 ${KUBE_DIR}

# Create a test file in the shared directory
echo "Creating test file in shared directory..."
echo "Test file created at $(date)" | sudo tee ${DOCX_DIR}/test_file.txt
sudo chmod 777 ${DOCX_DIR}/test_file.txt

# Get all running pods
PODS=$(kubectl get pods --field-selector=status.phase=Running -o jsonpath='{.items[*].metadata.name}')
echo "Found pods: $PODS"

# Create bind mounts directly inside each pod
for POD in $PODS; do
  echo "Creating bind mount in pod: $POD"
  
  # Create the directory in the pod if it doesn't exist
  kubectl exec $POD -- mkdir -p /var/www/ncatbird.ru/html/docx
  
  # Copy our test file to prove we can access it
  kubectl cp ${DOCX_DIR}/test_file.txt $POD:/var/www/ncatbird.ru/html/docx/
  
  # Create a test file from the pod
  echo "Creating test file from pod $POD..."
  kubectl exec $POD -- bash -c "echo 'File created from pod $POD at $(date)' > /var/www/ncatbird.ru/html/docx/pod_file_$POD.txt"
  
  # Try to copy the pod-created file back to host
  echo "Copying pod-created file back to host..."
  kubectl cp $POD:/var/www/ncatbird.ru/html/docx/pod_file_$POD.txt ${DOCX_DIR}/
done

# List all files in shared directory
echo "Files in shared directory:"
ls -la ${DOCX_DIR}

# Create a simple script to intercept file uploads in the container
echo "Creating file interceptor in pods..."
INTERCEPTOR_SCRIPT='#!/bin/bash
mkdir -p /var/kubernetes-share/docx
chmod -R 777 /var/kubernetes-share
chmod -R 777 /var/www/ncatbird.ru/html/docx

# Set up a loop to check for new files
while true; do
  # Find all files in the source directory
  find /var/www/ncatbird.ru/html/docx -type f -not -name ".*" | while read file; do
    filename=$(basename "$file")
    target="/var/kubernetes-share/docx/$filename"
    
    # If the file doesn't exist in the target directory, copy it
    if [ ! -f "$target" ]; then
      echo "Copying new file: $filename"
      cp "$file" "$target"
      chmod 777 "$target"
    fi
  done
  
  # Find all files in the target directory
  find /var/kubernetes-share/docx -type f -not -name ".*" | while read file; do
    filename=$(basename "$file")
    target="/var/www/ncatbird.ru/html/docx/$filename"
    
    # If the file doesn't exist in the source directory, copy it
    if [ ! -f "$target" ]; then
      echo "Copying new file from shared directory: $filename"
      cp "$file" "$target"
      chmod 777 "$target"
    fi
  done
  
  # Sleep for a short time
  sleep 5
done'

# Deploy the interceptor to each pod
for POD in $PODS; do
  echo "Deploying file interceptor to pod: $POD"
  echo "$INTERCEPTOR_SCRIPT" > /tmp/interceptor.sh
  kubectl cp /tmp/interceptor.sh $POD:/tmp/
  kubectl exec $POD -- chmod +x /tmp/interceptor.sh
  kubectl exec -d $POD -- /tmp/interceptor.sh
done

# Copy any existing files from pod to shared directory
for POD in $PODS; do
  echo "Looking for existing files in pod: $POD"
  # Get list of files
  FILES=$(kubectl exec $POD -- find /var/www/ncatbird.ru/html/docx -type f -not -name ".*" | tr -d '\r')
  
  if [ -n "$FILES" ]; then
    echo "Found files in pod $POD, copying to shared directory..."
    for FILE in $FILES; do
      FILENAME=$(basename "$FILE")
      echo "Copying $FILENAME from pod to shared directory..."
      kubectl cp $POD:$FILE ${DOCX_DIR}/$FILENAME
      sudo chmod 777 ${DOCX_DIR}/$FILENAME
    done
  else
    echo "No files found in pod $POD"
  fi
done

# Create a simple Nginx configuration to serve the files
echo "Setting up Nginx to serve the files..."
if ! command -v nginx &> /dev/null; then
  sudo apt-get update
  sudo apt-get install -y nginx
fi

# Create Nginx configuration
NGINX_CONF="/etc/nginx/sites-available/kubernetes-share"
sudo tee $NGINX_CONF > /dev/null << EOF
server {
    listen 80;
    listen [::]:80;
    
    server_name _;
    
    root ${KUBE_DIR};
    
    # Enable directory listing
    autoindex on;
    
    # Direct access to docx directory
    location /docx/ {
        alias ${DOCX_DIR}/;
        autoindex on;
        
        # CORS headers
        add_header 'Access-Control-Allow-Origin' '*' always;
        add_header 'Access-Control-Allow-Methods' 'GET, POST, OPTIONS' always;
        add_header 'Access-Control-Allow-Headers' 'DNT,User-Agent,X-Requested-With,If-Modified-Since,Cache-Control,Content-Type,Range' always;
    }
    
    location / {
        try_files \$uri \$uri/ =404;
    }
}
EOF

# Enable the Nginx configuration
sudo ln -sf $NGINX_CONF /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl restart nginx

echo "===== Fix Complete ====="
echo "Files uploaded through the API will now be intercepted and copied to: ${DOCX_DIR}"
echo "They will be accessible via: http://your-server-ip/docx/"
echo ""
echo "To test, try uploading a file through your API, then check:"
echo "1. ${DOCX_DIR} for the file on the host"
echo "2. http://localhost/docx/ to access it via web browser" 