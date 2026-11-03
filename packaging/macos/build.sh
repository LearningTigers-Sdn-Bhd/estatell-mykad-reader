#!/bin/bash
# Builds three packages in artifacts/macos. Run from anywhere on a Mac.
#   estatell-mykad-reader-v{version}.pkg                 universal binary
#   estatell-mykad-reader-v{version}_apple-silicon.pkg   arm64 only
#   estatell-mykad-reader-v{version}_intel.pkg           x64 only
#
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

chmod 755 "$HERE/scripts/preinstall" "$HERE/scripts/postinstall"

# build_package <folder> <file name suffix> <host architectures> <rid>...
build_package() {
    local variant="$1" suffix="$2" architectures="$3"
    shift 3

    local dir="$WORK/$variant"
    local root="$dir/pkgroot"
    local app="$root/$SUPPORT/Estatell MyKad Reader.app"
    local uninstall="$root/Applications/Estatell/Uninstall Estatell MyKad Reader.app"
    local binaries=()
    for rid in "$@"; do
        binaries+=("$WORK/publish/$rid/mykad-reader")
    done

    mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources" "$root/Library/LaunchAgents" "$(dirname "$uninstall")"
    lipo -create -output "$app/Contents/MacOS/mykad-reader" "${binaries[@]}"
    sed "s/@VERSION@/$VERSION/g" "$HERE/Info.plist" > "$app/Contents/Info.plist"
    cp "$PROJECT/Tray/Icons/AppIcon.icns" "$app/Contents/Resources/AppIcon.icns"
    cp "$HERE/uninstall.sh" "$root/$SUPPORT/uninstall.sh"
    cp "$REPO/packaging/THIRD-PARTY-NOTICES.txt" "$root/$SUPPORT/THIRD-PARTY-NOTICES.txt"
    cp "$HERE/$LABEL.plist" "$root/Library/LaunchAgents/$LABEL.plist"
    chmod 755 "$root/$SUPPORT/uninstall.sh"
    chmod 644 "$root/Library/LaunchAgents/$LABEL.plist"

    osacompile -o "$uninstall" "$HERE/uninstall.applescript"
    /usr/libexec/PlistBuddy -c "Add :CFBundleIdentifier string com.estatell.mykad-reader.uninstall" \
        -c "Set :CFBundleName Uninstall Estatell MyKad Reader" "$uninstall/Contents/Info.plist"

    # Extended attributes would ship as ._* sidecar files.
    xattr -cr "$root"
    # Apple Silicon only runs signed code, and lipo and PlistBuddy change what was signed. This is an ad-hoc signature.
    codesign --force --sign - "$app"
    codesign --force --sign - "$uninstall"

    # The bundles must install where we put them. A relocatable bundle moves to wherever macOS finds a copy with its id.
    pkgbuild --analyze --root "$root" "$dir/components.plist"
    local i=0
    while /usr/libexec/PlistBuddy -c "Print :$i" "$dir/components.plist" >/dev/null 2>&1; do
        /usr/libexec/PlistBuddy -c "Delete :$i:BundleIsRelocatable" "$dir/components.plist" 2>/dev/null || true
        /usr/libexec/PlistBuddy -c "Add :$i:BundleIsRelocatable bool false" "$dir/components.plist"
        i=$((i + 1))
    done

    pkgbuild --root "$root" --scripts "$HERE/scripts" --component-plist "$dir/components.plist" \
        --identifier "$LABEL" --version "$VERSION" --install-location / \
        "$dir/mykad-reader.pkg"

    sed -e "s/@VERSION@/$VERSION/g" -e "s/@ARCHITECTURES@/$architectures/g" "$HERE/distribution.xml" > "$dir/distribution.xml"
    productbuild --distribution "$dir/distribution.xml" --package-path "$dir" \
        "$OUT/estatell-mykad-reader-v$VERSION$suffix.pkg"

    echo "Built $OUT/estatell-mykad-reader-v$VERSION$suffix.pkg"
}

build_package universal "" "arm64,x86_64" osx-arm64 osx-x64
build_package apple-silicon "_apple-silicon" "arm64" osx-arm64
build_package intel "_intel" "x86_64" osx-x64
