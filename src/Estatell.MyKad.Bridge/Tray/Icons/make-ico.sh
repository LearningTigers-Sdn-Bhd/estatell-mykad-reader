#!/bin/sh
# Makes the tray and app icons from card.svg. Needs rsvg-convert, ImageMagick and iconutil.
# One colour for each tray state, so the icon shows on both light and dark taskbars.
set -eu
cd "$(dirname "$0")"
tmp=$(mktemp -d)
make_ico() {
    for size in 16 20 24 32 40 48; do
        sed "s/currentColor/$1/g" card.svg | rsvg-convert -w "$size" -h "$size" -o "$tmp/$size.png"
    done
    magick "$tmp"/16.png "$tmp"/20.png "$tmp"/24.png "$tmp"/32.png "$tmp"/40.png "$tmp"/48.png "$2"
}
make_ico '#16a34a' tray-ready.ico
make_ico '#8a8a8a' tray-grey.ico

# The icon of the program file on Windows and of the app on macOS. A placeholder until there is a real logo.
render_app() {
    sed "s/currentColor/#16a34a/g" card.svg | rsvg-convert -w "$1" -h "$1" -o "$2"
}
for size in 16 32 48 256; do
    render_app "$size" "$tmp/app-$size.png"
done
magick "$tmp"/app-16.png "$tmp"/app-32.png "$tmp"/app-48.png "$tmp"/app-256.png app.ico
mkdir "$tmp/AppIcon.iconset"
for size in 16 32 128 256 512; do
    render_app "$size" "$tmp/AppIcon.iconset/icon_${size}x${size}.png"
    render_app "$((size * 2))" "$tmp/AppIcon.iconset/icon_${size}x${size}@2x.png"
done
iconutil -c icns -o AppIcon.icns "$tmp/AppIcon.iconset"
rm -rf "$tmp"
