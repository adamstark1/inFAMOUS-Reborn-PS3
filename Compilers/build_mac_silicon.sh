#!/bin/bash

cd "$(dirname "$0")/.."

VERSION="1.0.1"

echo "Compiling macOS Silicon (osx-arm64) build for version $VERSION..."

rm -rf "inFAMOUS Reborn.app" ./temp_backend ./temp_launcher

dotnet publish inFAMOUSReborn.csproj -c Release -r osx-arm64 --self-contained true -p:UseAppHost=true -p:Version=$VERSION -o ./temp_backend
dotnet publish inFAMOUSReborn.Launcher/inFAMOUSReborn.Launcher.csproj -c Release -r osx-arm64 --self-contained true -p:UseAppHost=true -p:Version=$VERSION -o ./temp_launcher

mkdir -p "inFAMOUS Reborn.app/Contents/MacOS"
mkdir -p "inFAMOUS Reborn.app/Contents/Backend"
mkdir -p "inFAMOUS Reborn.app/Contents/Resources"

cp -R ./temp_launcher/* "inFAMOUS Reborn.app/Contents/MacOS/"
cp -R ./temp_backend/* "inFAMOUS Reborn.app/Contents/Backend/"
cp infamous.pfx "inFAMOUS Reborn.app/Contents/Backend/"
cp Assets/AppIcon.icns "inFAMOUS Reborn.app/Contents/Resources/"

chmod +x "inFAMOUS Reborn.app/Contents/MacOS/inFAMOUSReborn.Launcher"
chmod +x "inFAMOUS Reborn.app/Contents/Backend/inFAMOUSReborn"

cat > "inFAMOUS Reborn.app/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>inFAMOUSReborn.Launcher</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>CFBundleName</key>
    <string>inFAMOUS Reborn</string>
    <key>CFBundleDisplayName</key>
    <string>inFAMOUS Reborn</string>
    <key>CFBundleIdentifier</key>
    <string>com.adamstark.infamousreborn</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
</dict>
</plist>
EOF

rm -rf ./temp_backend ./temp_launcher
xattr -cr "inFAMOUS Reborn.app"

codesign --force --sign - "inFAMOUS Reborn.app/Contents/Backend/inFAMOUSReborn"
codesign --force --sign - "inFAMOUS Reborn.app/Contents/MacOS/inFAMOUSReborn.Launcher"
codesign --force --deep --sign - "inFAMOUS Reborn.app"

xattr -cr "inFAMOUS Reborn.app"
touch "inFAMOUS Reborn.app"

echo "MacOS Silicon build compiled. Happy trophy hunting! :)"