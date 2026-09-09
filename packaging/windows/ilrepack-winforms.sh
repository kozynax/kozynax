#!/usr/bin/env bash
# Merge managed dependency DLLs into Kozynax.exe with ILRepack (packaging only).
# Usage: ilrepack-winforms.sh <stage-dir>
set -euo pipefail

STAGE="$(cd "${1:?stage dir}" && pwd)"
cd "${STAGE}"

if [[ ! -f Kozynax.exe ]]; then
  echo "Kozynax.exe not found in ${STAGE}" >&2
  exit 1
fi

mapfile -t DLLS < <(find . -maxdepth 1 -type f -name '*.dll' | sed 's|^\./||' | sort)
if [[ ${#DLLS[@]} -eq 0 ]]; then
  echo "No sibling DLLs to merge; leaving Kozynax.exe as-is."
  exit 0
fi

CACHE="${RUNNER_TEMP:-${TMPDIR:-/tmp}}/ilrepack-2.0.41"
if [[ -z "${ILREPACK_EXE:-}" ]]; then
  if [[ ! -f "${CACHE}/tools/ILRepack.exe" ]]; then
    mkdir -p "${CACHE}"
    NUPKG="${CACHE}/ILRepack.2.0.41.nupkg"
    echo "Downloading ILRepack 2.0.41..."
    curl -fsSL "https://www.nuget.org/api/v2/package/ILRepack/2.0.41" -o "${NUPKG}"
    if command -v unzip >/dev/null 2>&1; then
      unzip -qo "${NUPKG}" -d "${CACHE}"
    else
      python3 - "${NUPKG}" "${CACHE}" <<'PY'
import sys, zipfile
zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])
PY
    fi
  fi
  ILREPACK="${CACHE}/tools/ILRepack.exe"
else
  ILREPACK="${ILREPACK_EXE}"
fi

if [[ ! -f "${ILREPACK}" ]]; then
  echo "ILRepack.exe not found at ${ILREPACK}" >&2
  exit 1
fi

echo "Merging into Kozynax.exe: ${DLLS[*]}"
MERGED="Kozynax.merged.exe"
RSP="ilrepack.rsp"
# Response file avoids Git Bash/MSYS rewriting /out:… into a filesystem path.
{
  echo "/out:${MERGED}"
  echo "/ndebug"
  echo "/internalize"
  echo "/target:winexe"
  echo "Kozynax.exe"
  printf '%s\n' "${DLLS[@]}"
} > "${RSP}"

# Also disable MSYS path conversion for the @rsp invocation on Git Bash.
export MSYS_NO_PATHCONV=1
export MSYS2_ARG_CONV_EXCL='*'

"${ILREPACK}" "@${RSP}"
rm -f "${RSP}"

mv -f "${MERGED}" Kozynax.exe
rm -f "${DLLS[@]}"
rm -f Kozynax.pdb

echo "ILRepack done: $(ls -la Kozynax.exe)"
