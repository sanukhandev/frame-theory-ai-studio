#!/usr/bin/env sh
set -eu

machine="$(uname -m)"
rid="${1:-}"
if [ -z "$rid" ]; then
  case "$machine" in
    arm64|aarch64) rid="osx-arm64" ;;
    x86_64) rid="osx-x64" ;;
    *)
      printf 'Unsupported macOS host architecture: %s\n' "$machine" >&2
      exit 1
      ;;
  esac
fi
case "$rid" in
  osx-x64|osx-arm64) ;;
  *)
    printf 'Unsupported macOS runtime identifier for DMG packaging: %s\n' "$rid" >&2
    exit 1
    ;;
esac
output="${2:-artifacts/installer}"
version="${3:-0.1.0}"
ffmpeg_directory="${4:-}"
if ! printf '%s\n' "$version" | grep -Eq '^[0-9]+\.[0-9]+\.[0-9]+([.-][0-9A-Za-z.-]+)?$'; then
  printf 'Invalid package version: %s\n' "$version" >&2
  exit 1
fi
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

publish="$work/publish"
app="$work/Frame Theory Studio AI.app"
mkdir -p "$app/Contents/MacOS" "$output"
dotnet publish src/FrameTheory.Desktop/FrameTheory.Desktop.csproj \
  --configuration Release \
  --runtime "$rid" \
  --self-contained true \
  --output "$publish"
cp -R "$publish"/. "$app/Contents/MacOS/"
mv "$app/Contents/MacOS/FrameTheory.Desktop" "$app/Contents/MacOS/FrameTheoryStudio"
chmod +x "$app/Contents/MacOS/FrameTheoryStudio"
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
    cp "$ffmpeg_directory/$tool" "$app/Contents/MacOS/$tool"
    chmod +x "$app/Contents/MacOS/$tool"
  done
fi
cat > "$app/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDisplayName</key><string>Frame Theory Studio AI</string>
  <key>CFBundleExecutable</key><string>FrameTheoryStudio</string>
  <key>CFBundleIdentifier</key><string>ai.frametheory.studio</string>
  <key>CFBundleName</key><string>Frame Theory Studio AI</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>$version</string>
  <key>CFBundleVersion</key><string>$version</string>
</dict>
</plist>
PLIST
plutil -lint "$app/Contents/Info.plist"
hdiutil create -volname "Frame Theory Studio AI" -srcfolder "$app" -ov -format UDZO "$output/FrameTheoryStudioAI-$rid.dmg"
printf 'Built unsigned macOS DMG: %s\n' "$output/FrameTheoryStudioAI-$rid.dmg"
