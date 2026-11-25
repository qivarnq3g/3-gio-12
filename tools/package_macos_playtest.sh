#!/bin/bash
set -euo pipefail
app_path="${1:-BuildMac/3gio12.app}"
zip_path="${2:-BuildMac/3gio12-macos-dem-1-$(date +%F).zip}"
executable="$app_path/Contents/MacOS/3gio12"
[[ "$(uname -s)" == Darwin ]] || { echo 'Package this app on macOS with ditto.' >&2; exit 1; }
[[ -d "$app_path" && -x "$executable" ]] || { echo 'Missing app or executable permission.' >&2; exit 1; }
architectures="$(lipo -archs "$executable")"
[[ "$architectures" == *x86_64* && "$architectures" == *arm64* ]] || { echo "Not a universal binary: $architectures" >&2; exit 1; }
[[ ! -e "$zip_path" ]] || { echo "Output already exists: $zip_path" >&2; exit 1; }
mkdir -p "$(dirname "$zip_path")"
ditto -c -k --keepParent "$app_path" "$zip_path"
shasum -a 256 "$zip_path"
