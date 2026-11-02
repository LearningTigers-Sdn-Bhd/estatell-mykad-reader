#!/bin/sh
# Makes the spike's tray icons from card.svg. Needs rsvg-convert and ImageMagick.
# icon-on-light.ico: dark glyph for a light Windows taskbar. icon-on-dark.ico: white glyph.
# menubar.png: black template image, 36 px for an 18 pt macOS menu bar icon.
set -eu
cd "$(dirname "$0")"
tmp=$(mktemp -d)
render() {
    sed "s/currentColor/$1/g" card.svg | rsvg-convert -w "$2" -h "$2" -o "$3"
}
for size in 16 20 24 32 40 48 64; do
    render '#1f1f1f' "$size" "$tmp/on-light-$size.png"
    render '#ffffff' "$size" "$tmp/on-dark-$size.png"
done
magick "$tmp"/on-light-16.png "$tmp"/on-light-20.png "$tmp"/on-light-24.png "$tmp"/on-light-32.png "$tmp"/on-light-40.png "$tmp"/on-light-48.png "$tmp"/on-light-64.png icon-on-light.ico
magick "$tmp"/on-dark-16.png "$tmp"/on-dark-20.png "$tmp"/on-dark-24.png "$tmp"/on-dark-32.png "$tmp"/on-dark-40.png "$tmp"/on-dark-48.png "$tmp"/on-dark-64.png icon-on-dark.ico
render '#000000' 36 menubar.png
rm -rf "$tmp"
