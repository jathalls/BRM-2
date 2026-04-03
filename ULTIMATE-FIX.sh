#!/bin/zsh

# ═══════════════════════════════════════════════════════════════
#  ULTIMATE FIX - Clean Build and Run
#  This fixes the namespace issue and builds correctly
# ═══════════════════════════════════════════════════════════════

echo ""
echo "╔═══════════════════════════════════════════════════════════════╗"
echo "║                                                               ║"
echo "║     🚀 BRM-2 - ULTIMATE FIX AND LAUNCH                       ║"
echo "║     (Preprocessor symbol fix applied)                        ║"
echo "║                                                               ║"
echo "╚═══════════════════════════════════════════════════════════════╝"
echo ""

cd "$(dirname "$0")" || exit 1

echo "📍 Working directory: $(pwd)"
echo ""

# ═══════════════════════════════════════════════════════════════
# STEP 1: Nuclear Clean
# ═══════════════════════════════════════════════════════════════
echo "════════════════════════════════════════════════════════════════"
echo " STEP 1/5: Nuclear Clean (Remove ALL cached files)"
echo "════════════════════════════════════════════════════════════════"
echo ""

echo "🗑️  Removing ALL bin and obj directories..."
find . -type d \( -name "bin" -o -name "obj" \) -exec rm -rf {} + 2>/dev/null || true

echo "✅ All cached build files removed"
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
    exit 1
fi

echo "✅ .NET SDK: $(dotnet --version)"
echo ""

# ═══════════════════════════════════════════════════════════════
# STEP 3: Restore Packages
# ═══════════════════════════════════════════════════════════════
echo "════════════════════════════════════════════════════════════════"
echo " STEP 3/5: Restoring NuGet Packages"
echo "════════════════════════════════════════════════════════════════"
echo ""

cd BRM-2
echo "📥 Restoring packages..."
dotnet restore --verbosity quiet
if [ $? -ne 0 ]; then
    echo "❌ Package restore failed"
    exit 1
fi
cd ..

echo "✅ Packages restored"
echo ""

# ═══════════════════════════════════════════════════════════════
# STEP 4: Build for MacCatalyst ONLY
# ═══════════════════════════════════════════════════════════════
echo "════════════════════════════════════════════════════════════════"
echo " STEP 4/5: Building for MacCatalyst"
echo "════════════════════════════════════════════════════════════════"
echo ""

echo "🔨 Building BPASpectrogramM library..."
cd BPASpectrogramM
if ! dotnet build -f net10.0-maccatalyst -c Debug; then
    echo ""
    echo "❌ BPASpectrogramM build FAILED!"
    echo ""
    echo "The preprocessor symbols __MACCATALYST__ should be defined."
    echo "Please check the error output above."
    exit 1
fi
cd ..
echo "✅ BPASpectrogramM built successfully"
echo ""

echo "🔨 Building BRM-2 application..."
cd BRM-2
if ! dotnet build -f net10.0-maccatalyst -c Debug; then
    echo ""
    echo "❌ BRM-2 build FAILED!"
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

echo "🚀 Starting BRM-2..."
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo ""

cd BRM-2
dotnet run -f net10.0-maccatalyst

echo ""
echo "════════════════════════════════════════════════════════════════"
echo " Application Closed"
echo "════════════════════════════════════════════════════════════════"
echo ""
