#!/usr/bin/env sh
set -eu

machine="$(uname -m)"
rid="${1:-}"
if [ -z "$rid" ]; then
  rid="osx-arm64"
  [ "$machine" = "x86_64" ] && rid="osx-x64"
fi
output="${2:-artifacts/installer}"
version="${3:-0.1.0}"
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
cat > "$app/Contents/Info.plist" <<'PLIST'
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
hdiutil create -volname "Frame Theory Studio AI" -srcfolder "$app" -ov -format UDZO "$output/FrameTheoryStudioAI-$rid.dmg"
printf 'Built unsigned macOS DMG: %s\n' "$output/FrameTheoryStudioAI-$rid.dmg"
