#!/bin/sh
# Publishes the spike for osx-arm64 and wraps it in a minimal TrayAot.app with LSUIElement (no Dock icon).
set -eu
cd "$(dirname "$0")"
mkdir -p publish
dotnet publish -c Release -r osx-arm64 -o publish/osx-arm64 2>&1 | tee publish/osx-arm64.log

app=publish/TrayAot.app
rm -rf "$app"
mkdir -p "$app/Contents/MacOS"
cp publish/osx-arm64/TrayAot "$app/Contents/MacOS/TrayAot"
cat > "$app/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleIdentifier</key><string>com.estatell.trayaot-spike</string>
  <key>CFBundleName</key><string>TrayAot</string>
  <key>CFBundleExecutable</key><string>TrayAot</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>0.0.0</string>
  <key>LSMinimumSystemVersion</key><string>15.0</string>
  <key>LSUIElement</key><true/>
</dict>
</plist>
PLIST
codesign --force --sign - "$app"
echo "Built $app ($(stat -f %z "$app/Contents/MacOS/TrayAot") bytes)"
