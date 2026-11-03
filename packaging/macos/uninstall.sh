#!/bin/bash
# Removes Estatell MyKad Reader. For support use. Staff use Uninstall Estatell MyKad Reader.app, which runs this.
#   sudo "/Library/Application Support/Estatell/MyKadReader/uninstall.sh"
set -e
if [ "$(id -u)" != "0" ]; then
    echo "Run with sudo."
    exit 1
fi
LABEL=com.estatell.mykad-reader
for uid in $(ps -axo uid=,comm= | awk '/loginwindow/ {print $1}' | sort -u); do
    launchctl bootout "gui/$uid/$LABEL" 2>/dev/null || true
done
# Match the binary, not the folder: this script's own command line holds the folder.
pkill -f "Estatell/MyKadReader/Estatell MyKad Reader.app/Contents/MacOS/mykad-reader" 2>/dev/null || true
rm -f "/Library/LaunchAgents/$LABEL.plist"
rm -rf "/Library/Application Support/Estatell/MyKadReader"
rm -rf "/Applications/Estatell/Uninstall Estatell MyKad Reader.app"
rmdir "/Applications/Estatell" 2>/dev/null || true
rmdir "/Library/Application Support/Estatell" 2>/dev/null || true
pkgutil --forget "$LABEL" >/dev/null 2>&1 || true
echo "Estatell MyKad Reader removed."
