#!/usr/bin/env sh
set -eu

rid="${1:-linux-x64}"
output="${2:-artifacts/installer}"
version="${3:-0.1.0}"
ffmpeg_directory="${4:-}"
case "$rid" in
  linux-x64) deb_arch="amd64" ;;
  linux-arm64) deb_arch="arm64" ;;
  *)
    printf 'Unsupported Linux runtime identifier for DEB packaging: %s\n' "$rid" >&2
    exit 1
    ;;
esac
if ! printf '%s\n' "$version" | grep -Eq '^[0-9]+\.[0-9]+\.[0-9]+([.-][0-9A-Za-z.-]+)?$'; then
  printf 'Invalid package version: %s\n' "$version" >&2
  exit 1
fi
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
if [ -n "$ffmpeg_directory" ]; then
  if [ ! -d "$ffmpeg_directory" ]; then
    printf 'FFmpeg directory does not exist: %s\n' "$ffmpeg_directory" >&2
    exit 1
  fi
  if [ -L "$ffmpeg_directory" ]; then
    printf 'FFmpeg directory cannot be a symlink.\n' >&2
    exit 1
  fi
  for tool in ffmpeg ffprobe; do
    if [ ! -f "$ffmpeg_directory/$tool" ]; then
      printf 'FFmpeg directory must contain %s\n' "$tool" >&2
      exit 1
    fi
    if [ -L "$ffmpeg_directory/$tool" ]; then
      printf 'FFmpeg directory cannot contain a symlink: %s\n' "$tool" >&2
      exit 1
    fi
  done
  for bundle_file in "$ffmpeg_directory"/*; do
    [ -e "$bundle_file" ] || continue
    if [ -L "$bundle_file" ]; then
      printf 'FFmpeg directory cannot contain a symlink: %s\n' "$(basename "$bundle_file")" >&2
      exit 1
    fi
    if [ -f "$bundle_file" ]; then
      cp "$bundle_file" "$publish/$(basename "$bundle_file")"
    fi
  done
  chmod +x "$publish/ffmpeg" "$publish/ffprobe"
fi
cat > "$root/DEBIAN/control" <<CONTROL
Package: frame-theory-studio-ai
Version: $version
Section: graphics
Priority: optional
Architecture: $deb_arch
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
dpkg-deb --build --root-owner-group "$root" "$output/frame-theory-studio-ai_${version}_${deb_arch}.deb" >/dev/null
printf 'Built unsigned Linux DEB: %s\n' "$output/frame-theory-studio-ai_${version}_${deb_arch}.deb"
