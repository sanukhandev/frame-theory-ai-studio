#!/usr/bin/env sh
set -eu

rid="${1:-linux-x64}"
output="${2:-artifacts/installer}"
version="${3:-0.1.0}"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

root="$work/root"
publish="$root/opt/frame-theory-studio"
mkdir -p "$publish" "$root/DEBIAN" "$root/usr/bin" "$root/usr/share/applications" "$output"
dotnet publish src/FrameTheory.Desktop/FrameTheory.Desktop.csproj \
  --configuration Release \
  --runtime "$rid" \
  --self-contained true \
  --output "$publish"
cat > "$root/DEBIAN/control" <<CONTROL
Package: frame-theory-studio-ai
Version: $version
Section: graphics
Priority: optional
Architecture: amd64
Maintainer: Frame Theory
Description: Offline-first photo and video editor
CONTROL
cat > "$root/usr/bin/frame-theory-studio-ai" <<'LAUNCHER'
#!/usr/bin/env sh
exec /opt/frame-theory-studio/FrameTheory.Desktop "$@"
LAUNCHER
chmod +x "$root/usr/bin/frame-theory-studio-ai"
cat > "$root/usr/share/applications/frame-theory-studio-ai.desktop" <<'DESKTOP'
[Desktop Entry]
Type=Application
Name=Frame Theory Studio AI
Comment=Offline-first photo and video editor
Exec=frame-theory-studio-ai
Terminal=false
Categories=AudioVideo;Graphics;
DESKTOP
dpkg-deb --build --root-owner-group "$root" "$output/frame-theory-studio-ai_${version}_amd64.deb" >/dev/null
printf 'Built unsigned Linux DEB: %s\n' "$output/frame-theory-studio-ai_${version}_amd64.deb"
