#!/bin/bash

echo "🎫 Starting TicketBlaster System..."

# Navigate to the solution directory
cd "TicketBlaster.Solution"

# Restore NuGet packages
echo "📦 Restoring packages..."
dotnet restore

# Build the solution
echo "🔨 Building solution..."
dotnet build

# Run database migrations
echo "🗄️ Setting up database..."
dotnet ef database update --project TicketBlaster.Database --startup-project TicketBlaster.Server

# Start the server
echo "🚀 Starting TicketBlaster Server..."
dotnet run --project TicketBlaster.Server --urls "https://localhost:5001;http://localhost:5000"