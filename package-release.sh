#!/bin/bash

# BRM-2 Release Packaging Script
# Usage: ./package-release.sh [platform] [architecture]
# Examples:
#   ./package-release.sh maccatalyst
#   ./package-release.sh windows x64
#   ./package-release.sh android

set -e

# Color output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

PLATFORM=${1:-"maccatalyst"}
ARCH=${2:-""}
PACKAGE_DIR="Release-Package-$(date +%Y%m%d-%H%M%S)"
OUTPUT_FILE=""

echo -e "${YELLOW}🔨 BRM-2 Release Packager${NC}"
echo "Platform: $PLATFORM"
[[ ! -z "$ARCH" ]] && echo "Architecture: $ARCH"
echo ""

# Validate platform and construct proper RuntimeIdentifier
case ${PLATFORM} in
    maccatalyst|macos|mac)
        PLATFORM="maccatalyst"
        TFM="net10.0-maccatalyst"
        if [[ ! -z "$ARCH" ]]; then
            RID="maccatalyst-${ARCH}"
        fi
        PLATFORM_NAME="macOS"
        ;;
    windows|win)
        PLATFORM="windows"
        TFM="net10.0-windows10.0.19041.0"
        if [[ ! -z "$ARCH" ]]; then
            RID="win-${ARCH}"
        else
            RID="win-x64"
        fi
        PLATFORM_NAME="Windows"
        ;;
    android)
        PLATFORM="android"
        TFM="net10.0-android"
        RID=${ARCH:-""}
        PLATFORM_NAME="Android"
        ;;
    ios)
        PLATFORM="ios"
        TFM="net10.0-ios"
        if [[ ! -z "$ARCH" ]]; then
            RID="ios-${ARCH}"
        else
            RID="ios-arm64"
        fi
        PLATFORM_NAME="iOS"
        ;;
    *)
        echo -e "${RED}❌ Unknown platform: ${PLATFORM}${NC}"
        echo "Supported platforms: maccatalyst, windows, android, ios"
        exit 1
        ;;
esac

echo -e "${YELLOW}📦 Step 1: Cleaning previous builds...${NC}"
dotnet clean -c Release > /dev/null 2>&1 || true

echo -e "${YELLOW}🔨 Step 2: Publishing release build...${NC}"
if [[ ! -z "$RID" ]]; then
    dotnet publish -f "${TFM}" -c Release -p:RuntimeIdentifier="${RID}"
else
    # For maccatalyst without specified arch, build both architectures
    if [[ "$PLATFORM" == "maccatalyst" ]]; then
        echo "   Building for maccatalyst-x64..."
        dotnet publish -f "${TFM}" -c Release -p:RuntimeIdentifier=maccatalyst-x64
        echo "   Building for maccatalyst-arm64..."
        dotnet publish -f "${TFM}" -c Release -p:RuntimeIdentifier=maccatalyst-arm64
    else
        dotnet publish -f "${TFM}" -c Release
    fi
fi

echo -e "${YELLOW}📁 Step 3: Packaging for distribution...${NC}"
mkdir -p ${PACKAGE_DIR}

case ${PLATFORM} in
    maccatalyst)
        # Find the build output - prefer x64 as it's most compatible
        # The app bundle is created directly in the architecture directory, not in publish/
        if [[ -d "BRM-2/bin/Release/net10.0-maccatalyst/maccatalyst-x64/BRM-2.app" ]]; then
            BUILD_OUTPUT="BRM-2/bin/Release/net10.0-maccatalyst/maccatalyst-x64"
        elif [[ -d "BRM-2/bin/Release/net10.0-maccatalyst/maccatalyst-arm64/BRM-2.app" ]]; then
            BUILD_OUTPUT="BRM-2/bin/Release/net10.0-maccatalyst/maccatalyst-arm64"
        else
            # Fallback: find it anywhere
            BUILD_OUTPUT=$(find BRM-2/bin/Release/net10.0-maccatalyst -name "BRM-2.app" -type d | head -1)
            BUILD_OUTPUT=$(dirname "$BUILD_OUTPUT")
        fi
        
        echo "Copying app bundle from: $BUILD_OUTPUT"
        cp -r "${BUILD_OUTPUT}/BRM-2.app" "${PACKAGE_DIR}/"
        OUTPUT_FILE="BRM-2-Release-macOS-$(date +%Y%m%d).zip"
        zip -r -q "${OUTPUT_FILE}" "${PACKAGE_DIR}"
        echo -e "${GREEN}✓ Created: ${OUTPUT_FILE}${NC}"
        echo "   Size: $(du -h ${OUTPUT_FILE} | cut -f1)"
        ;;
        
    windows)
        BUILD_OUTPUT="BRM-2/bin/Release/net10.0-windows10.0.19041.0/win-${ARCH:-x64}/publish"
        if [[ ! -d "$BUILD_OUTPUT" ]]; then
            BUILD_OUTPUT=$(find BRM-2/bin/Release/net10.0-windows10.0.19041.0 -type d -name "publish" | head -1)
        fi
        
        echo "Copying executables from: $BUILD_OUTPUT"
        cp -r "${BUILD_OUTPUT}/"* "${PACKAGE_DIR}/"
        OUTPUT_FILE="BRM-2-Release-Windows-$(date +%Y%m%d).zip"
        zip -r -q "${OUTPUT_FILE}" "${PACKAGE_DIR}"
        echo -e "${GREEN}✓ Created: ${OUTPUT_FILE}${NC}"
        echo "   Size: $(du -h ${OUTPUT_FILE} | cut -f1)"
        ;;
        
    android)
        PUBLISH_DIR="BRM-2/bin/Release/net10.0-android/publish"
        if [[ -d "$PUBLISH_DIR" ]]; then
            APK_FILES=$(find "$PUBLISH_DIR" -name "*.apk" -o -name "*.aab" 2>/dev/null)
            if [[ ! -z "$APK_FILES" ]]; then
                echo -e "${GREEN}✓ Found APK/AAB files:${NC}"
                echo "$APK_FILES"
                OUTPUT_FILE="${PUBLISH_DIR}"
            else
                echo -e "${YELLOW}⚠ No APK/AAB files found in publish directory${NC}"
                OUTPUT_FILE="${PUBLISH_DIR}"
            fi
        fi
        echo "Publish directory: $PUBLISH_DIR"
        ;;
        
    ios)
        BUILD_DIR="BRM-2/bin/Release/net10.0-ios"
        echo "Build output directory: $BUILD_DIR"
        if [[ -d "$BUILD_DIR" ]]; then
            echo -e "${YELLOW}ℹ For iOS, use Xcode or TestFlight for distribution${NC}"
            echo -e "${GREEN}✓ Build ready at: ${BUILD_DIR}${NC}"
        fi
        ;;
esac

echo ""
echo -e "${GREEN}✅ Packaging complete!${NC}"
echo ""

# Cleanup
rm -rf ${PACKAGE_DIR}

# Display next steps
case ${PLATFORM} in
    maccatalyst)
        echo "📤 Next steps:"
        echo "   1. Transfer ${OUTPUT_FILE} to testing computer"
        echo "   2. Unzip the file"
        echo "   3. Right-click BRM-2.app → Open"
        ;;
    windows)
        echo "📤 Next steps:"
        echo "   1. Transfer ${OUTPUT_FILE} to testing computer"
        echo "   2. Extract the zip file"
        echo "   3. Run BRM-2.exe"
        ;;
    android)
        echo "📤 Next steps:"
        echo "   1. Find APK files in: ${OUTPUT_FILE}"
        echo "   2. Transfer to Android device or emulator"
        echo "   3. Install with: adb install app.apk"
        ;;
    ios)
        echo "📤 Next steps:"
        echo "   1. Open Xcode with the build output"
        echo "   2. Use Ad Hoc provisioning or TestFlight"
        ;;
esac

echo ""
