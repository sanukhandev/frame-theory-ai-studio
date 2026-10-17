#!/usr/bin/env sh
set -eu

rid="${1:-linux-x64}"
output="${2:-artifacts/package}"
self_contained="${3:-false}"

case "$self_contained" in
  true|false) ;;
  *) echo "self-contained must be true or false" >&2; exit 2 ;;
esac

dotnet publish src/FrameTheory.Desktop/FrameTheory.Desktop.csproj \
  --configuration Release \
  --runtime "$rid" \
  --self-contained "$self_contained" \
  --output "$output"

manifest="$output/SHA256SUMS.txt"
: > "$manifest"
find "$output" -type f ! -name SHA256SUMS.txt -print | sort | while IFS= read -r file; do
  if command -v sha256sum >/dev/null 2>&1; then
    hash=$(sha256sum "$file" | cut -d ' ' -f 1)
  else
    hash=$(shasum -a 256 "$file" | cut -d ' ' -f 1)
  fi
  printf '%s  %s\n' "$hash" "${file#"$output"/}" >> "$manifest"
done
printf 'Published %s (self-contained=%s) to %s\n' "$rid" "$self_contained" "$output"
