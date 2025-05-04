#!/bin/bash

# Script to set up Nginx on the host to serve files directly from the uploads directory

echo "===== Setting up Nginx to serve uploaded files ====="

# Check if Nginx is installed
if ! command -v nginx &> /dev/null; then
    echo "Nginx is not installed. Installing now..."
    sudo apt-get update
    sudo apt-get install -y nginx
else
    echo "Nginx is already installed."
fi

# Stop Nginx if it's running
sudo systemctl stop nginx

# Create the upload directories if they don't exist
HOST_DIR="/var/www/ncatbird.ru/html"
HOST_DOCX_DIR="$HOST_DIR/docx"

echo "Creating upload directories..."
sudo mkdir -p $HOST_DOCX_DIR
sudo chmod -R 777 /var/www/ncatbird.ru

# Create a test file
echo "Creating test file..."
echo "This is a test file created at $(date)" | sudo tee $HOST_DOCX_DIR/nginx_test.txt
sudo chmod 644 $HOST_DOCX_DIR/nginx_test.txt

# Create a Nginx configuration file for serving the files
CONFIG_FILE="/etc/nginx/sites-available/file-uploads"
echo "Creating Nginx configuration file: $CONFIG_FILE"

sudo tee $CONFIG_FILE > /dev/null << 'EOF'
server {
    listen 80;
    listen [::]:80;
    
    # Replace with your actual server name
    server_name localhost;
    
    # Set root directory
    root /var/www/ncatbird.ru/html;
    
    # Enable directory listing
    autoindex on;
    
    # Log files
    access_log /var/log/nginx/file-uploads-access.log;
    error_log /var/log/nginx/file-uploads-error.log;
    
    # Direct access to the docx directory
    location /docx/ {
        alias /var/www/ncatbird.ru/html/docx/;
        autoindex on;
        
        # Allow all HTTP methods
        if ($request_method !~ ^(GET|HEAD|POST|PUT|DELETE)$ ) {
            return 405;
        }
        
        # Set CORS headers to allow access from any origin
        add_header 'Access-Control-Allow-Origin' '*' always;
        add_header 'Access-Control-Allow-Methods' 'GET, POST, OPTIONS' always;
        add_header 'Access-Control-Allow-Headers' 'DNT,User-Agent,X-Requested-With,If-Modified-Since,Cache-Control,Content-Type,Range' always;
        add_header 'Access-Control-Expose-Headers' 'Content-Length,Content-Range' always;
    }
    
    # For the API to upload to this directory
    location / {
        try_files $uri $uri/ =404;
    }
}
EOF

# Enable the site
echo "Enabling Nginx site configuration..."
sudo ln -sf /etc/nginx/sites-available/file-uploads /etc/nginx/sites-enabled/

# Test the Nginx configuration
echo "Testing Nginx configuration..."
sudo nginx -t

if [ $? -ne 0 ]; then
    echo "ERROR: Nginx configuration test failed. Please check the configuration."
    exit 1
fi

# Start Nginx
echo "Starting Nginx..."
sudo systemctl start nginx

# Check if Nginx is running
if sudo systemctl is-active --quiet nginx; then
    echo "SUCCESS: Nginx is running."
else
    echo "ERROR: Nginx failed to start."
    exit 1
fi

# Print success message
echo "===== Nginx setup complete ====="
echo "The uploaded files should now be accessible at: http://your-server-ip/docx/"
echo "Test it with: http://localhost/docx/nginx_test.txt"
echo "Files uploaded through your API to $HOST_DOCX_DIR will be automatically served." 