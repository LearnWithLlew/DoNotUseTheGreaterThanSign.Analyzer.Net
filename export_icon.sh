#!/usr/bin/env bash
# Exports icon.svg to icon.png (128x128, the size NuGet's PackageIcon wants).
# NuGet only accepts PNG/JPEG, so re-run this after every edit to icon.svg.
# Pass a second argument to also write a bigger preview, e.g. ./export_icon.sh icon.svg /tmp/preview.png
# Requires Inkscape (brew install inkscape).
set -euo pipefail
cd "$(dirname "$0")"

svg="${1:-icon.svg}"

inkscape "$svg" --export-type=png --export-filename=icon.png -w 128 -h 128

if [[ -n "${2:-}" ]]; then
  inkscape "$svg" --export-type=png --export-filename="$2" -w 512 -h 512
fi
