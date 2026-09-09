#!/usr/bin/env bash
# Resolve a packaging / MSBuild Version from CI / git.
# Format:
#   on tag v0.90              → 0.90
#   3 commits after v0.90     → 0.90.3
# Always Major.Minor, with optional commit-distance as the 3rd component.
# No leading 'v', no -gSHA prerelease suffix.
set -euo pipefail

# Strip a leading 'v' if present.
normalize_tag() {
  local t="$1"
  t="${t#v}"
  echo "$t"
}

# Keep only Major.Minor from a version-like string.
base_version() {
  local raw="$1"
  if [[ "$raw" =~ ^([0-9]+\.[0-9]+) ]]; then
    echo "${BASH_REMATCH[1]}"
    return
  fi
  echo ""
}

# Ensure the string is acceptable as -p:Version= (NuGet SemVer).
to_nuget_version() {
  local raw="$1"
  raw="$(normalize_tag "$raw")"

  # git describe --long: 0.90-3-gabcdef or 0.90.1-3-gabcdef
  if [[ "$raw" =~ ^([0-9]+(\.[0-9]+){1,3})-([0-9]+)-g[0-9a-fA-F]+$ ]]; then
    local base dist
    base="$(base_version "${BASH_REMATCH[1]}")"
    dist="${BASH_REMATCH[3]}"
    if [[ -n "$base" ]]; then
      if [[ "$dist" == "0" ]]; then
        echo "$base"
      else
        echo "${base}.${dist}"
      fi
      return
    fi
  fi

  # Plain tag / VERSION override: 0.90, 0.90.1, 2.9.3.8 → Major.Minor
  local base
  base="$(base_version "$raw")"
  if [[ -n "$base" ]]; then
    echo "$base"
    return
  fi

  # Bare / non-semver (no usable tags): 0.0.<commit-count>
  local count
  count="$(git rev-list --count HEAD 2>/dev/null || echo 0)"
  echo "0.0.${count}"
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

date -u +%Y.%m.%d
