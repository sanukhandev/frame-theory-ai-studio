#!/usr/bin/env sh
set -eu

rid="${1:-linux-x64}"
output="${2:-artifacts/package}"
self_contained="${3:-false}"
ffmpeg_directory="${4:-}"

case "$self_contained" in
  true|false) ;;
  *) echo "self-contained must be true or false" >&2; exit 2 ;;
esac

dotnet publish src/FrameTheory.Desktop/FrameTheory.Desktop.csproj \
  --configuration Release \
  --runtime "$rid" \
  --self-contained "$self_contained" \
  --output "$output"

if [ -n "$ffmpeg_directory" ]; then
  if [ ! -d "$ffmpeg_directory" ]; then
    printf 'FFmpeg directory does not exist: %s\n' "$ffmpeg_directory" >&2
    exit 1
  fi
  for tool in ffmpeg ffprobe; do
    if [ ! -f "$ffmpeg_directory/$tool" ]; then
      printf 'FFmpeg directory must contain %s\n' "$tool" >&2
      exit 1
    fi
    cp "$ffmpeg_directory/$tool" "$output/$tool"
    chmod +x "$output/$tool"
  done
fi

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
