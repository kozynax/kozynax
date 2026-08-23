#!/usr/bin/env bash
# Zip a publish/build folder into a portable archive.
# Usage: make-zip.sh <source-dir> <archive-basename> <output-dir>
set -euo pipefail

SRC="$(cd "${1:?source dir}" && pwd)"
BASENAME="${2:?archive basename}"
OUT_DIR="${3:?output dir}"

mkdir -p "${OUT_DIR}"
OUT_DIR="$(cd "${OUT_DIR}" && pwd)"
ABS_OUT="${OUT_DIR}/${BASENAME}.zip"
rm -f "${ABS_OUT}"

if command -v zip >/dev/null 2>&1; then
  (
    cd "${SRC}"
    zip -qry "${ABS_OUT}" .
  )
elif command -v python3 >/dev/null 2>&1; then
  python3 - "${SRC}" "${ABS_OUT}" <<'PY'
import sys, zipfile
from pathlib import Path

src, out = Path(sys.argv[1]), Path(sys.argv[2])
with zipfile.ZipFile(out, "w", compression=zipfile.ZIP_DEFLATED) as zf:
    for path in sorted(src.rglob("*")):
        if path.is_file():
            zf.write(path, path.relative_to(src).as_posix())
PY
else
  echo "Neither zip nor python3 is available to create ${ABS_OUT}" >&2
  exit 1
fi

echo "${ABS_OUT}"
