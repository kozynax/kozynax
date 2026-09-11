#!/usr/bin/env bash
# Build a macOS .app + .dmg from a published Kozynax.Sdl folder.
# Usage: make-dmg.sh <publish-dir> <version> <output-dir> [arch-label]
set -euo pipefail

PUBLISH_DIR="$(cd "${1:?publish dir}" && pwd)"
VERSION="${2:?version}"
OUT_DIR="${3:?output dir}"
ARCH_LABEL="${4:-$(uname -m)}"
APP_NAME="Kozynax"
BUNDLE_ID="org.kozynax.sdl"

mkdir -p "${OUT_DIR}"
OUT_DIR="$(cd "${OUT_DIR}" && pwd)"

STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT

APP="${STAGE}/${APP_NAME}.app"
CONTENTS="${APP}/Contents"
MACOS="${CONTENTS}/MacOS"
RESOURCES="${CONTENTS}/Resources"

mkdir -p "${MACOS}" "${RESOURCES}"
cp -a "${PUBLISH_DIR}/." "${MACOS}/"
chmod +x "${MACOS}/Kozynax.Sdl" || true
bash "$(cd "$(dirname "$0")/.." && pwd)/pack-roms.sh" "${MACOS}"
find "${MACOS}" -type f \( -name '*.pdb' -o -name '*.dll.config' -o -name createdump \) -delete

# Thin launcher so Finder runs the published binary with a stable name.
cat > "${MACOS}/${APP_NAME}" <<'EOF'
#!/bin/bash
DIR="$(cd "$(dirname "$0")" && pwd)"
exec "$DIR/Kozynax.Sdl" "$@"
EOF
chmod +x "${MACOS}/${APP_NAME}"

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ICONSET="${STAGE}/${APP_NAME}.iconset"
mkdir -p "${ICONSET}"
copy_icon() {
  local src="$1" dest="$2"
  if [[ -f "${src}" ]]; then
    cp "${src}" "${dest}"
  fi
}
copy_icon "${ROOT}/icons/16x16.png"     "${ICONSET}/icon_16x16.png"
copy_icon "${ROOT}/icons/32x32.png"     "${ICONSET}/icon_16x16@2x.png"
copy_icon "${ROOT}/icons/32x32.png"     "${ICONSET}/icon_32x32.png"
copy_icon "${ROOT}/icons/64x64.png"     "${ICONSET}/icon_32x32@2x.png"
copy_icon "${ROOT}/icons/128x128.png"   "${ICONSET}/icon_128x128.png"
copy_icon "${ROOT}/icons/256x256.png"   "${ICONSET}/icon_128x128@2x.png"
copy_icon "${ROOT}/icons/256x256.png"   "${ICONSET}/icon_256x256.png"
copy_icon "${ROOT}/icons/512x512.png"   "${ICONSET}/icon_256x256@2x.png"
copy_icon "${ROOT}/icons/512x512.png"   "${ICONSET}/icon_512x512.png"
copy_icon "${ROOT}/icons/1024x1024.png" "${ICONSET}/icon_512x512@2x.png"
ICON_PLIST=""
if command -v iconutil >/dev/null 2>&1; then
  iconutil -c icns -o "${RESOURCES}/${APP_NAME}.icns" "${ICONSET}"
  ICON_PLIST="  <key>CFBundleIconFile</key>
  <string>${APP_NAME}</string>"
else
  echo "iconutil not found; macOS app will have no bundle icon" >&2
fi
rm -rf "${ICONSET}"

cat > "${CONTENTS}/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleExecutable</key>
  <string>${APP_NAME}</string>
  <key>CFBundleIdentifier</key>
  <string>${BUNDLE_ID}</string>
  <key>CFBundleName</key>
  <string>${APP_NAME}</string>
  <key>CFBundleDisplayName</key>
  <string>${APP_NAME}</string>
${ICON_PLIST}
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>${VERSION}</string>
  <key>CFBundleVersion</key>
  <string>${VERSION}</string>
  <key>LSMinimumSystemVersion</key>
  <string>11.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
EOF

DMG_NAME="${APP_NAME}-${VERSION}-macos-${ARCH_LABEL}.dmg"
DMG_PATH="${OUT_DIR}/${DMG_NAME}"
rm -f "${DMG_PATH}"

hdiutil create \
  -volname "${APP_NAME}" \
  -srcfolder "${APP}" \
  -ov \
  -format UDZO \
  "${DMG_PATH}"

# Also keep a portable .app.zip for users who prefer not to mount a dmg.
(
  cd "${STAGE}"
  zip -qry "${OUT_DIR}/${APP_NAME}-${VERSION}-macos-${ARCH_LABEL}.app.zip" "${APP_NAME}.app"
)
