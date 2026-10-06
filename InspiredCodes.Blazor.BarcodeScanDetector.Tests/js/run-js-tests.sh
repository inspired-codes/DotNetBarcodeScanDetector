#!/usr/bin/env bash
# Runs the tests of barcodeScanListener.js in headless Chrome or Chromium.
# Needs python3 (a static file server for the repository) and a Chrome/Chromium binary.
# Without them: serve the repository root over HTTP and open js/barcodeScanListener.test.html in a browser.
set -euo pipefail

repo="$(cd "$(dirname "$0")/../.." && pwd)"
browser="${CHROME:-$(command -v google-chrome || command -v chromium || command -v chromium-browser || true)}"
if [ -z "$browser" ]; then
    echo "no Chrome or Chromium found (set CHROME=/path/to/chrome)" >&2
    exit 2
fi
port="${PORT:-8765}"
page="http://127.0.0.1:$port/InspiredCodes.Blazor.BarcodeScanDetector.Tests/js/barcodeScanListener.test.html"

python3 -m http.server "$port" --bind 127.0.0.1 --directory "$repo" >/dev/null 2>&1 &
server=$!
trap 'kill "$server" 2>/dev/null' EXIT
for _ in $(seq 50); do
    python3 -c "import urllib.request,sys; urllib.request.urlopen(sys.argv[1])" "$page" 2>/dev/null && break
    sleep 0.1
done

# --virtual-time-budget lets the tests' timers run before the page is dumped
dom="$("$browser" --headless=new --disable-gpu --no-first-run --virtual-time-budget=10000 --dump-dom "$page" 2>/dev/null)"
echo "$dom" | grep -o -E '(PASS|FAIL|RESULT)[^<]*'
echo "$dom" | grep -q 'RESULT: ALL'
