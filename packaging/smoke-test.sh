#!/bin/bash
# Starts the built app with --no-tray and checks the rules every release must keep. Needs no card reader.
#   packaging/smoke-test.sh <app> <version>
set -uo pipefail

APP="$1"
VERSION="$2"
BASE=http://127.0.0.1:9292
FAILED=0

"$APP" --no-tray &
PID=$!
trap 'kill "$PID" 2>/dev/null || true' EXIT

for _ in $(seq 1 20); do
    curl -s -m 2 -o /dev/null "$BASE/status" && break
    sleep 1
done

check() {
    if [ "$2" = "$3" ]; then
        echo "ok   $1"
    else
        echo "FAIL $1: expected $3, got $2"
        FAILED=1
    fi
}

code() {
    curl -s -m 5 -o /dev/null -w '%{http_code}' "$@"
}

ACTUAL="$(curl -s -m 5 "$BASE/status" | sed -nE 's/.*"version": *"([^"]*)".*/\1/p')"
check "status returns the version" "$ACTUAL" "$VERSION"
check "Estatell Origin is accepted" "$(code -H 'Origin: https://estatell.com' "$BASE/status")" 200
check "foreign Origin is refused" "$(code -H 'Origin: https://evil.example' "$BASE/status")" 403
check "wrong Host is refused" "$(code -H 'Host: evil.example' "$BASE/status")" 403
check "read with no Origin is refused" "$(code "$BASE/read")" 403
check "dev Origin is refused in a release build" "$(code -H 'Origin: http://estatell.test' "$BASE/status")" 403

exit "$FAILED"
