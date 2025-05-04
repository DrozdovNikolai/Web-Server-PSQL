#!/bin/bash

# Script to check and fix permissions for file upload directory in container

echo "Checking file upload directory structure..."

# Check if base directory exists
if [ ! -d "/var/www/ncatbird.ru" ]; then
  echo "Base directory /var/www/ncatbird.ru does not exist. Creating it..."
  mkdir -p /var/www/ncatbird.ru
  if [ $? -ne 0 ]; then
    echo "Failed to create base directory. Please check container permissions."
    exit 1
  fi
fi

# Check if html directory exists
if [ ! -d "/var/www/ncatbird.ru/html" ]; then
  echo "HTML directory /var/www/ncatbird.ru/html does not exist. Creating it..."
  mkdir -p /var/www/ncatbird.ru/html
  if [ $? -ne 0 ]; then
    echo "Failed to create HTML directory. Please check container permissions."
    exit 1
  fi
fi

# Check if docx directory exists
if [ ! -d "/var/www/ncatbird.ru/html/docx" ]; then
  echo "Docx directory /var/www/ncatbird.ru/html/docx does not exist. Creating it..."
  mkdir -p /var/www/ncatbird.ru/html/docx
  if [ $? -ne 0 ]; then
    echo "Failed to create docx directory. Please check container permissions."
    exit 1
  fi
fi

# Set permissions recursively
echo "Setting permissions on directories..."
chmod -R 777 /var/www/ncatbird.ru
if [ $? -ne 0 ]; then
  echo "Failed to set permissions. Please check container privileges."
  exit 1
fi

# Check ownership
echo "Current ownership of directories:"
ls -la /var/www/ncatbird.ru
ls -la /var/www/ncatbird.ru/html
ls -la /var/www/ncatbird.ru/html/docx

# Try to create a test file
echo "Testing file creation..."
touch /var/www/ncatbird.ru/html/docx/test_file.txt
if [ $? -ne 0 ]; then
  echo "Failed to create test file. There might be permission issues."
  exit 1
else
  echo "Test file created successfully."
  rm /var/www/ncatbird.ru/html/docx/test_file.txt
fi

echo "Directory structure and permissions check completed."
echo "If there were no errors, the file upload should now work correctly." 