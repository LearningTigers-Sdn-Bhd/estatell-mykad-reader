#!/bin/sh
# Makes the Windows tray icons from card.svg. Needs rsvg-convert and ImageMagick.
# One colour for each state, so the icon shows on both light and dark taskbars.
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
rm -rf "$tmp"
