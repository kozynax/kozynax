#!/usr/bin/env bash
# Resolve a packaging / MSBuild Version from CI / git.
# Always prints a NuGet-valid SemVer string (no leading 'v').
set -euo pipefail

# Strip a leading 'v' if present.
normalize_tag() {
  local t="$1"
  t="${t#v}"
  echo "$t"
}

# Ensure the string is acceptable as -p:Version= (NuGet SemVer).
# Bare git SHAs like "0595ecc" are rejected by NuGet.
to_nuget_version() {
  local raw="$1"
  raw="$(normalize_tag "$raw")"

  # Already looks like Major.Minor...
  if [[ "$raw" =~ ^[0-9]+\.[0-9]+ ]]; then
    # git describe: 1.2.3-5-gabcdef → 1.2.3-5.gabcdef (SemVer prerelease parts)
    if [[ "$raw" =~ ^([0-9]+(\.[0-9]+){1,3})-([0-9]+)-g([0-9a-fA-F]+)$ ]]; then
      echo "${BASH_REMATCH[1]}-${BASH_REMATCH[3]}.g${BASH_REMATCH[4]}"
      return
    fi
    echo "$raw"
    return
  fi

  # Bare / non-semver describe output (no tags in repo): use commit count + sha.
  local count sha
  count="$(git rev-list --count HEAD 2>/dev/null || echo 0)"
  sha="$(git rev-parse --short=7 HEAD 2>/dev/null || echo unknown)"
  # Prefix sha with 'g' so the prerelease label never looks numeric (leading zeros).
  echo "0.0.${count}-g${sha}"
}

if [[ -n "${VERSION:-}" ]]; then
  to_nuget_version "$VERSION"
  exit 0
fi

if [[ -n "${GITHUB_REF_TYPE:-}" && "${GITHUB_REF_TYPE}" == "tag" ]]; then
  to_nuget_version "${GITHUB_REF_NAME}"
  exit 0
fi

if git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  if git describe --tags --exact-match HEAD >/dev/null 2>&1; then
    to_nuget_version "$(git describe --tags --exact-match HEAD)"
    exit 0
  fi
  if desc="$(git describe --tags --long 2>/dev/null)"; then
    to_nuget_version "$desc"
    exit 0
  fi
  # No tags at all.
  to_nuget_version "$(git rev-parse --short=7 HEAD)"
  exit 0
fi

date -u +%Y.%m.%d.0
