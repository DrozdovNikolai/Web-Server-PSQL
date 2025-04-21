# Setup script for SuperHero API Admin Panel

# Navigate to admin panel directory
Set-Location -Path "./admin-panel"

# Check if Node.js is installed
try {
    $nodeVersion = node -v
    $npmVersion = npm -v
    Write-Host "Node.js $nodeVersion and npm $npmVersion are installed" -ForegroundColor Green
} catch {
    Write-Host "Error: Node.js or npm is not installed." -ForegroundColor Red
    Write-Host "Please install Node.js from https://nodejs.org/" -ForegroundColor Yellow
    exit 1
}

# Install dependencies
Write-Host "Installing dependencies..." -ForegroundColor Green
npm install

# Run the development server
Write-Host "Starting the admin panel..." -ForegroundColor Green
Write-Host "The admin panel will be available at http://localhost:8080" -ForegroundColor Cyan
npm run serve 