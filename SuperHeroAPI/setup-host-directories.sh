#!/bin/bash

# Script to set up host directories for file storage

echo "Setting up host directories for file storage..."

# Create the directory structure on the host
HOST_DIR="/var/www/ncatbird.ru/html"
HOST_DOCX_DIR="$HOST_DIR/docx"

# Create base directory
echo "Creating base directory: $HOST_DIR"
mkdir -p $HOST_DIR
if [ $? -ne 0 ]; then
  echo "Failed to create $HOST_DIR. Please run this script with sudo."
  exit 1
fi

# Create docx directory
echo "Creating docx directory: $HOST_DOCX_DIR"
mkdir -p $HOST_DOCX_DIR
if [ $? -ne 0 ]; then
  echo "Failed to create $HOST_DOCX_DIR."
  exit 1
fi

# Set permissions (777 to ensure the container can write)
echo "Setting permissions on directories..."
chmod -R 777 /var/www/ncatbird.ru
if [ $? -ne 0 ]; then
  echo "Failed to set permissions. Please run this script with sudo."
  exit 1
fi

# Create a test file to verify write permissions
echo "Creating test file to verify permissions..."
echo "Test file" > "$HOST_DOCX_DIR/host_test_file.txt"
if [ $? -ne 0 ]; then
  echo "Failed to create test file. Please check permissions."
  exit 1
fi

echo "Checking ownership and permissions:"
ls -la $HOST_DIR
ls -la $HOST_DOCX_DIR

echo "Host directories have been set up successfully."
echo "Now restart the Kubernetes deployments to use these directories."
echo "Run ./restart-containers.sh to apply the changes." 