#!/bin/zsh

# Quick diagnostic script to check platform file compilation

echo "════════════════════════════════════════════════════════════"
echo "  🔍 Platform File Compilation Diagnostic"
echo "════════════════════════════════════════════════════════════"
echo ""

cd "$(dirname "$0")/BPASpectrogramM" || exit 1

echo "📋 Testing platform-specific compilation..."
echo ""

# Test MacCatalyst build (should succeed)
echo "1️⃣  Testing MacCatalyst build (should include Mac files)..."
dotnet build -f net10.0-maccatalyst -v quiet > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "   ✅ MacCatalyst build: SUCCESS"
else
    echo "   ❌ MacCatalyst build: FAILED"
    echo ""
    echo "Building with verbose output:"
    dotnet build -f net10.0-maccatalyst
    exit 1
fi
echo ""

# Test Android build (should succeed, excluding iOS/Mac files)
echo "2️⃣  Testing Android build (should exclude iOS/Mac files)..."
dotnet build -f net10.0-android -v quiet > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "   ✅ Android build: SUCCESS (iOS/Mac files excluded)"
else
    echo "   ❌ Android build: FAILED"
    echo ""
    echo "This means the platform exclusions might not be working."
    echo "Building with verbose output:"
    dotnet build -f net10.0-android
fi
echo ""

# Test iOS build (should succeed)
echo "3️⃣  Testing iOS build (should include iOS files)..."
dotnet build -f net10.0-ios -v quiet > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "   ✅ iOS build: SUCCESS"
else
    echo "   ⚠️  iOS build: FAILED (may need iOS SDK)"
fi
echo ""

echo "════════════════════════════════════════════════════════════"
echo "  Diagnostic Complete"
echo "════════════════════════════════════════════════════════════"
echo ""
echo "If MacCatalyst build succeeded, you can run the app with:"
echo "  ./clean-build-run.sh"
echo ""
