#!/usr/bin/env bash
# If DIR/roms exists, write DIR/ROMS.PAK (zip of files inside roms/) and drop roms/.
# Usage: pack-roms.sh <dir>
set -euo pipefail

DIR="$(cd "${1:?dir}" && pwd)"
ROMS="${DIR}/roms"
PAK="${DIR}/ROMS.PAK"

if [[ ! -d "${ROMS}" ]]; then
  exit 0
fi

rm -f "${PAK}"

if command -v zip >/dev/null 2>&1; then
  (
    cd "${ROMS}"
    zip -r "${PAK}" .
  )
elif command -v python3 >/dev/null 2>&1; then
  python3 - "${ROMS}" "${PAK}" <<'PY'
import sys, zipfile
from pathlib import Path

src, out = Path(sys.argv[1]), Path(sys.argv[2])
with zipfile.ZipFile(out, "w", compression=zipfile.ZIP_DEFLATED) as zf:
    for path in sorted(src.rglob("*")):
        if path.is_file():
            zf.write(path, path.relative_to(src).as_posix())
PY
else
  echo "Neither zip nor python3 is available to create ${PAK}" >&2
  exit 1
fi

rm -rf "${ROMS}"
