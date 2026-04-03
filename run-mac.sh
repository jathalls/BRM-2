#!/bin/zsh

# BRM-2 Quick Launch Script for MacCatalyst
# Run this script to launch BRM-2 on your Mac

echo "=================================="
echo "  BRM-2 Quick Launch (MacCatalyst)"
echo "=================================="
echo ""

# Navigate to project directory
cd "$(dirname "$0")/BRM-2" || exit 1

echo "📍 Current directory: $(pwd)"
echo ""

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ ERROR: .NET SDK not found!"
    echo "   Please install .NET 8/9/10 SDK from: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✅ .NET SDK found: $(dotnet --version)"
echo ""

# Check MAUI workload
echo "📦 Checking MAUI workload..."
if ! dotnet workload list | grep -q "maui"; then
    echo "⚠️  MAUI workload not installed!"
    echo "   Installing MAUI workload..."
    dotnet workload install maui
    if [ $? -ne 0 ]; then
        echo "❌ Failed to install MAUI workload"
        exit 1
    fi
    echo "✅ MAUI workload installed"
else
    echo "✅ MAUI workload already installed"
fi
echo ""

# Clean previous builds
echo "🧹 Cleaning previous builds..."
dotnet clean -f net10.0-maccatalyst > /dev/null 2>&1
echo "✅ Clean complete"
echo ""

# Restore packages
echo "📥 Restoring NuGet packages..."
dotnet restore
if [ $? -ne 0 ]; then
    echo "❌ Package restore failed"
    exit 1
fi
echo "✅ Packages restored"
echo ""

# Build for MacCatalyst
echo "🔨 Building BRM-2 for MacCatalyst..."
dotnet build -f net10.0-maccatalyst -c Debug
if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    echo ""
    echo "Troubleshooting:"
    echo "1. Make sure Xcode is installed"
    echo "2. Accept Xcode license: sudo xcodebuild -license accept"
    echo "3. Set Xcode path: sudo xcode-select --switch /Applications/Xcode.app/Contents/Developer"
    exit 1
fi
echo "✅ Build successful!"
echo ""

# Run the app
echo "🚀 Launching BRM-2..."
echo ""
dotnet run -f net10.0-maccatalyst

echo ""
echo "=================================="
echo "  BRM-2 Closed"
echo "=================================="
