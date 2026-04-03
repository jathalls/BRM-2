#!/bin/zsh

# BRM-2 Clean Build and Run Script
# This script will clean everything and rebuild from scratch

echo "════════════════════════════════════════════════════════════"
echo "  🧹 BRM-2 Clean Build & Launch"
echo "════════════════════════════════════════════════════════════"
echo ""

# Navigate to solution directory
cd "$(dirname "$0")" || exit 1
echo "📍 Solution directory: $(pwd)"
echo ""

# Clean all projects
echo "🧹 Cleaning all projects..."
cd BRM-2
dotnet clean --configuration Debug
dotnet clean --configuration Release
cd ..

cd BPASpectrogramM  
dotnet clean --configuration Debug
dotnet clean --configuration Release
cd ..

echo "✅ Clean complete"
echo ""

# Remove bin and obj folders to ensure complete clean
echo "🗑️  Removing bin and obj folders..."
find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true
echo "✅ Folders removed"
echo ""

# Restore packages
echo "📥 Restoring NuGet packages..."
cd BRM-2
dotnet restore
if [ $? -ne 0 ]; then
    echo "❌ Package restore failed"
    exit 1
fi
cd ..

echo "✅ Packages restored"
echo ""

# Build BPASpectrogramM library for MacCatalyst only
echo "🔨 Building BPASpectrogramM library (MacCatalyst)..."
cd BPASpectrogramM
dotnet build -f net10.0-maccatalyst -c Debug
if [ $? -ne 0 ]; then
    echo "❌ BPASpectrogramM build failed!"
    echo ""
    echo "📝 This should not happen. The platform exclusions are in place."
    echo "   Please share the full error output."
    exit 1
fi
cd ..
echo "✅ BPASpectrogramM built successfully"
echo ""

# Build BRM-2 application for MacCatalyst
echo "🔨 Building BRM-2 application (MacCatalyst)..."
cd BRM-2
dotnet build -f net10.0-maccatalyst -c Debug
if [ $? -ne 0 ]; then
    echo "❌ BRM-2 build failed!"
    exit 1
fi
cd ..
echo "✅ BRM-2 built successfully"
echo ""

# Run the application
echo "🚀 Launching BRM-2..."
echo ""
cd BRM-2
dotnet run -f net10.0-maccatalyst

echo ""
echo "════════════════════════════════════════════════════════════"
echo "  App closed"
echo "════════════════════════════════════════════════════════════"
