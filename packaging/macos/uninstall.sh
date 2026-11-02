#!/bin/bash
# Removes MyKad Reader. For support use. Staff use Uninstall MyKad Reader.app, which runs this.
#   sudo "/Library/Application Support/Estatell/MyKadBridge/uninstall.sh"
set -e
if [ "$(id -u)" != "0" ]; then
    echo "Run with sudo."
    exit 1
fi
LABEL=com.estatell.mykad-bridge
for uid in $(ps -axo uid=,comm= | awk '/loginwindow/ {print $1}' | sort -u); do
    launchctl bootout "gui/$uid/$LABEL" 2>/dev/null || true
done
# Match the binary, not the folder: this script's own command line holds the folder.
pkill -f "Estatell/MyKadBridge/MyKad Reader.app/Contents/MacOS/mykad-bridge" 2>/dev/null || true
rm -f "/Library/LaunchAgents/$LABEL.plist"
rm -rf "/Library/Application Support/Estatell/MyKadBridge"
rm -rf "/Applications/Estatell/Uninstall MyKad Reader.app"
rmdir "/Applications/Estatell" 2>/dev/null || true
rmdir "/Library/Application Support/Estatell" 2>/dev/null || true
pkgutil --forget "$LABEL" >/dev/null 2>&1 || true
echo "MyKad Reader removed."
