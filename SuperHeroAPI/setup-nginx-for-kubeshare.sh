#!/bin/bash

# Script to set up Nginx to serve files from the Kubernetes shared directory

echo "===== Setting up Nginx for Kubernetes shared directory ====="

# Shared directory path
KUBE_DIR="/tmp/kubeshare"
DOCX_DIR="${KUBE_DIR}/docx"

# Create directories if they don't exist
echo "Creating shared directories..."
sudo mkdir -p ${DOCX_DIR}
sudo chmod -R 777 ${KUBE_DIR}

# Create a symlink if it doesn't exist
if [ ! -L "/var/www/ncatbird.ru/html" ]; then
  echo "Creating symlink..."
  sudo mkdir -p /var/www/ncatbird.ru
  sudo rm -rf /var/www/ncatbird.ru/html
  sudo ln -sf ${KUBE_DIR} /var/www/ncatbird.ru/html
fi

# Check if Nginx is installed
if ! command -v nginx &> /dev/null; then
  echo "Nginx is not installed. Installing now..."
  sudo apt-get update
  sudo apt-get install -y nginx
else
  echo "Nginx is already installed."
fi

# Set up Nginx configuration
CONFIG_FILE="/etc/nginx/sites-available/kubeshare"
echo "Creating Nginx configuration file: $CONFIG_FILE"

sudo tee $CONFIG_FILE > /dev/null << EOF
server {
    listen 80;
    listen [::]:80;
    
    # Set your server name or use _
    server_name _;
    
    # Root directory for file serving
    root ${KUBE_DIR};
    
    # Enable directory listing
    autoindex on;
    
    # Log files
    access_log /var/log/nginx/kubeshare-access.log;
    error_log /var/log/nginx/kubeshare-error.log;
    
    # Direct access to docx directory
    location /docx/ {
        alias ${DOCX_DIR}/;
        autoindex on;
        
        # Add CORS headers
        add_header 'Access-Control-Allow-Origin' '*' always;
        add_header 'Access-Control-Allow-Methods' 'GET, POST, OPTIONS' always;
        add_header 'Access-Control-Allow-Headers' 'DNT,User-Agent,X-Requested-With,If-Modified-Since,Cache-Control,Content-Type,Range' always;
        add_header 'Access-Control-Expose-Headers' 'Content-Length,Content-Range' always;
    }
    
    # Root path
    location / {
        try_files \$uri \$uri/ =404;
    }
}
EOF

# Enable the site
echo "Enabling Nginx site..."
sudo ln -sf $CONFIG_FILE /etc/nginx/sites-enabled/

# Test Nginx configuration
echo "Testing Nginx configuration..."
sudo nginx -t
if [ $? -ne 0 ]; then
  echo "ERROR: Nginx configuration test failed."
  exit 1
fi

# Restart Nginx
echo "Restarting Nginx..."
sudo systemctl restart nginx
if ! sudo systemctl is-active --quiet nginx; then
  echo "ERROR: Failed to restart Nginx."
  exit 1
fi

# Create a test file for verification
TEST_FILE="${DOCX_DIR}/nginx_test_$(date +%s).txt"
echo "Creating test file: $TEST_FILE"
echo "This is a test file for Nginx, created at $(date)" | sudo tee $TEST_FILE
sudo chmod 644 $TEST_FILE

# Get server IP
SERVER_IP=$(hostname -I | awk '{print $1}')

echo "===== Nginx Setup Complete ====="
echo "Files uploaded through the API will be saved to: ${DOCX_DIR}"
echo "They should be accessible at: http://${SERVER_IP}/docx/"
echo "Test by accessing: http://${SERVER_IP}/docx/$(basename $TEST_FILE)"
echo ""
echo "Next step: Run the minikube-host-path-fix.sh script to configure Kubernetes." 