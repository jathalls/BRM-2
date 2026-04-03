#!/bin/zsh

# ═══════════════════════════════════════════════════════════════
#  BRM-2 - MASTER FIX AND RUN SCRIPT
#  This is the DEFINITIVE script that will fix and run everything
# ═══════════════════════════════════════════════════════════════

set -e  # Exit on any error

echo ""
echo "╔═══════════════════════════════════════════════════════════════╗"
echo "║                                                               ║"
echo "║     🚀 BRM-2 - COMPLETE FIX AND LAUNCH SCRIPT                ║"
echo "║                                                               ║"
echo "║  This script will:                                           ║"
echo "║    ✓ Clean all build artifacts                              ║"
echo "║    ✓ Restore packages                                       ║"
echo "║    ✓ Build for MacCatalyst only                             ║"
echo "║    ✓ Launch the application                                 ║"
echo "║                                                               ║"
echo "╚═══════════════════════════════════════════════════════════════╝"
echo ""

# Navigate to solution directory
SCRIPT_DIR="$(dirname "$0")"
cd "$SCRIPT_DIR" || exit 1

echo "📍 Working directory: $(pwd)"
echo ""

# ═══════════════════════════════════════════════════════════════
# STEP 1: Clean Build Artifacts
# ═══════════════════════════════════════════════════════════════
echo "════════════════════════════════════════════════════════════════"
echo " STEP 1/5: Cleaning Build Artifacts"
echo "════════════════════════════════════════════════════════════════"
echo ""

echo "🧹 Cleaning BRM-2 project..."
cd BRM-2
dotnet clean --configuration Debug --verbosity quiet || true
dotnet clean --configuration Release --verbosity quiet || true
cd ..

echo "🧹 Cleaning BPASpectrogramM project..."
cd BPASpectrogramM
dotnet clean --configuration Debug --verbosity quiet || true
dotnet clean --configuration Release --verbosity quiet || true
cd ..

echo "🗑️  Removing bin and obj directories..."
find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true
find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true

echo "✅ Clean complete"
echo ""

# ═══════════════════════════════════════════════════════════════
# STEP 2: Verify .NET SDK
# ═══════════════════════════════════════════════════════════════
echo "════════════════════════════════════════════════════════════════"
echo " STEP 2/5: Verifying .NET SDK"
echo "════════════════════════════════════════════════════════════════"
echo ""

if ! command -v dotnet &> /dev/null; then
    echo "❌ ERROR: .NET SDK not found!"
    echo "   Please install .NET 8/9/10 from: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✅ .NET SDK: $(dotnet --version)"
echo ""

# Check MAUI workload
if ! dotnet workload list 2>/dev/null | grep -q "maui"; then
    echo "⚠️  MAUI workload not installed. Installing..."
    dotnet workload install maui
    echo "✅ MAUI workload installed"
else
    echo "✅ MAUI workload installed"
fi
echo ""

# ═══════════════════════════════════════════════════════════════
# STEP 3: Restore Packages
# ═══════════════════════════════════════════════════════════════
echo "════════════════════════════════════════════════════════════════"
echo " STEP 3/5: Restoring NuGet Packages"
echo "════════════════════════════════════════════════════════════════"
echo ""

cd BRM-2
echo "📥 Restoring BRM-2 packages..."
dotnet restore
cd ..

echo "✅ Packages restored"
echo ""

# ═══════════════════════════════════════════════════════════════
# STEP 4: Build Projects
# ═══════════════════════════════════════════════════════════════
echo "════════════════════════════════════════════════════════════════"
echo " STEP 4/5: Building Projects for MacCatalyst"
echo "════════════════════════════════════════════════════════════════"
echo ""

echo "🔨 Building BPASpectrogramM (library)..."
cd BPASpectrogramM
if ! dotnet build -f net10.0-maccatalyst -c Debug; then
    echo ""
    echo "❌ BPASpectrogramM build FAILED!"
    echo ""
    echo "This should not happen. The fixes are in place."
    echo "Please report this error with the full output above."
    exit 1
fi
cd ..
echo "✅ BPASpectrogramM built successfully"
echo ""

echo "🔨 Building BRM-2 (application)..."
cd BRM-2
if ! dotnet build -f net10.0-maccatalyst -c Debug; then
    echo ""
    echo "❌ BRM-2 build FAILED!"
    echo ""
    echo "Troubleshooting steps:"
    echo "  1. Make sure Xcode is installed"
    echo "  2. Run: sudo xcodebuild -license accept"
    echo "  3. Run: sudo xcode-select --switch /Applications/Xcode.app/Contents/Developer"
    exit 1
fi
cd ..
echo "✅ BRM-2 built successfully"
echo ""

# ═══════════════════════════════════════════════════════════════
# STEP 5: Launch Application
# ═══════════════════════════════════════════════════════════════
echo "════════════════════════════════════════════════════════════════"
echo " STEP 5/5: Launching BRM-2"
echo "════════════════════════════════════════════════════════════════"
echo ""

echo "🚀 Starting BRM-2 on your Mac..."
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo ""

cd BRM-2
dotnet run -f net10.0-maccatalyst

# ═══════════════════════════════════════════════════════════════
# Done
# ═══════════════════════════════════════════════════════════════
echo ""
echo "════════════════════════════════════════════════════════════════"
echo " Application Closed"
echo "════════════════════════════════════════════════════════════════"
echo ""
echo "✨ To test the audio feature:"
echo "   1. Load a WAV file"
echo "   2. Select a segment"
echo "   3. Choose '0.1x' speed (1/10 speed)"
echo "   4. Press Play ▶"
echo "   5. Verify slow playback with natural pitch!"
echo ""
