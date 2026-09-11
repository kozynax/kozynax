#!/usr/bin/env bash
# Build a .deb from a published Kozynax.Sdl linux folder.
# Usage: make-deb.sh <publish-dir> <version> <output-dir>
set -euo pipefail

PUBLISH_DIR="${1:?publish dir}"
VERSION="${2:?version}"
OUT_DIR="${3:?output dir}"
ARCH="${ARCH:-amd64}"
PKG_NAME="kozynax"
INSTALL_ROOT="/usr/lib/${PKG_NAME}"

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT

mkdir -p \
  "${STAGE}${INSTALL_ROOT}" \
  "${STAGE}/usr/bin" \
  "${STAGE}/usr/share/applications" \
  "${STAGE}/usr/share/icons/hicolor" \
  "${STAGE}/usr/share/doc/${PKG_NAME}" \
  "${OUT_DIR}"

cp -a "${PUBLISH_DIR}/." "${STAGE}${INSTALL_ROOT}/"
chmod +x "${STAGE}${INSTALL_ROOT}/Kozynax.Sdl" || true
bash "${ROOT}/packaging/pack-roms.sh" "${STAGE}${INSTALL_ROOT}"
find "${STAGE}${INSTALL_ROOT}" -type f \( -name '*.pdb' -o -name '*.dll.config' -o -name createdump \) -delete

cat > "${STAGE}/usr/bin/${PKG_NAME}" <<EOF
#!/bin/sh
exec "${INSTALL_ROOT}/Kozynax.Sdl" "\$@"
EOF
chmod +x "${STAGE}/usr/bin/${PKG_NAME}"

for size in 16 32 48 64 128 256 512; do
  src="${ROOT}/icons/${size}x${size}.png"
  dest="${STAGE}/usr/share/icons/hicolor/${size}x${size}/apps"
  if [[ -f "${src}" ]]; then
    mkdir -p "${dest}"
    cp "${src}" "${dest}/${PKG_NAME}.png"
  fi
done

cat > "${STAGE}/usr/share/applications/${PKG_NAME}.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=Kozynax
Comment=ZX Spectrum emulator (SDL)
Exec=${PKG_NAME}
Icon=${PKG_NAME}
Terminal=false
Categories=Game;Emulator;
StartupNotify=true
EOF

cp "${ROOT}/LICENSE" "${STAGE}/usr/share/doc/${PKG_NAME}/copyright"
cp "${ROOT}/README.md" "${STAGE}/usr/share/doc/${PKG_NAME}/README.md"

if ! command -v fpm >/dev/null 2>&1; then
  echo "fpm is required (gem install fpm)" >&2
  exit 1
fi

fpm -s dir -t deb \
  -n "${PKG_NAME}" \
  -v "${VERSION}" \
  -a "${ARCH}" \
  --license "GPL-compatible (see LICENSE)" \
  --description "ZX Spectrum emulator virtual machine (SDL host)" \
  --url "https://github.com/kozynax/kozynax" \
  --maintainer "Kozynax maintainers" \
  --deb-compression xz \
  -C "${STAGE}" \
  --package "${OUT_DIR}/${PKG_NAME}_${VERSION}_${ARCH}.deb" \
  .
