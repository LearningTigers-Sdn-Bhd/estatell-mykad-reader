#!/bin/bash
# Builds artifacts/macos/estatell-mykad.pkg with a universal binary. Run from anywhere on a Mac.
#   packaging/macos/build.sh [version]
# The version defaults to the one in Directory.Build.props.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/../.." && pwd)"
PROJECT="$REPO/src/Estatell.MyKad.Bridge"
OUT="$REPO/artifacts/macos"
WORK="$OUT/work"
LABEL=com.estatell.mykad-reader
SUPPORT="Library/Application Support/Estatell/MyKadReader"
VERSION="${1:-$(dotnet msbuild "$PROJECT" -getProperty:Version)}"

rm -rf "$OUT"
mkdir -p "$WORK"

for rid in osx-arm64 osx-x64; do
    dotnet publish "$PROJECT" -c Release -r "$rid" -p:Version="$VERSION" -o "$WORK/publish/$rid"
done

ROOT="$WORK/pkgroot"
APP="$ROOT/$SUPPORT/Estatell MyKad Reader.app"
UNINSTALL="$ROOT/Applications/Estatell/Uninstall Estatell MyKad Reader.app"

mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources" "$ROOT/Library/LaunchAgents" "$(dirname "$UNINSTALL")"
lipo -create -output "$APP/Contents/MacOS/mykad-reader" \
    "$WORK/publish/osx-arm64/mykad-reader" "$WORK/publish/osx-x64/mykad-reader"
sed "s/@VERSION@/$VERSION/g" "$HERE/Info.plist" > "$APP/Contents/Info.plist"
cp "$PROJECT/Tray/Icons/AppIcon.icns" "$APP/Contents/Resources/AppIcon.icns"
cp "$HERE/uninstall.sh" "$ROOT/$SUPPORT/uninstall.sh"
cp "$REPO/packaging/THIRD-PARTY-NOTICES.txt" "$ROOT/$SUPPORT/THIRD-PARTY-NOTICES.txt"
cp "$HERE/$LABEL.plist" "$ROOT/Library/LaunchAgents/$LABEL.plist"
chmod 755 "$ROOT/$SUPPORT/uninstall.sh" "$HERE/scripts/preinstall" "$HERE/scripts/postinstall"
chmod 644 "$ROOT/Library/LaunchAgents/$LABEL.plist"

osacompile -o "$UNINSTALL" "$HERE/uninstall.applescript"
/usr/libexec/PlistBuddy -c "Add :CFBundleIdentifier string com.estatell.mykad-reader.uninstall" \
    -c "Set :CFBundleName Uninstall Estatell MyKad Reader" "$UNINSTALL/Contents/Info.plist"

# Extended attributes would ship as ._* sidecar files.
xattr -cr "$ROOT"
# Apple Silicon only runs signed code, and lipo and PlistBuddy change what was signed. This is an ad-hoc signature.
codesign --force --sign - "$APP"
codesign --force --sign - "$UNINSTALL"

# The bundles must install where we put them. A relocatable bundle moves to wherever macOS finds a copy with its id.
pkgbuild --analyze --root "$ROOT" "$WORK/components.plist"
i=0
while /usr/libexec/PlistBuddy -c "Print :$i" "$WORK/components.plist" >/dev/null 2>&1; do
    /usr/libexec/PlistBuddy -c "Delete :$i:BundleIsRelocatable" "$WORK/components.plist" 2>/dev/null || true
    /usr/libexec/PlistBuddy -c "Add :$i:BundleIsRelocatable bool false" "$WORK/components.plist"
    i=$((i + 1))
done

pkgbuild --root "$ROOT" --scripts "$HERE/scripts" --component-plist "$WORK/components.plist" \
    --identifier "$LABEL" --version "$VERSION" --install-location / \
    "$WORK/mykad-reader.pkg"

sed "s/@VERSION@/$VERSION/g" "$HERE/distribution.xml" > "$WORK/distribution.xml"
productbuild --distribution "$WORK/distribution.xml" --package-path "$WORK" "$OUT/estatell-mykad.pkg"

echo "Built $OUT/estatell-mykad.pkg (version $VERSION)"
