#!/usr/bin/env bash
# Package / MSBuild Version from git.
#
# Base is the newest two-part git tag reachable from HEAD (v0.90).
#   on that tag              → 0.90
#   N commits after it       → 0.90.N (N is not a git tag)
#
# Ignore VERSION / GITHUB_REF for the third component: those are often the
# two-part tag name and would otherwise produce 0.90 forever.
set -euo pipefail

normalize_tag() {
  local t="$1"
  t="${t#v}"
  echo "$t"
}

# Newest reachable tag that is exactly Major.Minor (optional leading v).
find_base_tag() {
  git tag --merged HEAD --list 'v*.*' --list '*.*' 2>/dev/null \
    | grep -E '^v?[0-9]+\.[0-9]+$' \
    | sort -V \
    | tail -1 || true
}

emit_from_base_tag() {
  local tag="$1"
  local base dist
  base="$(normalize_tag "$tag")"
  dist="$(git rev-list --count "${tag}..HEAD" 2>/dev/null || echo 0)"
  if [[ "$dist" == "0" ]]; then
    echo "$base"
  else
    echo "${base}.${dist}"
  fi
}

if git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  base_tag="$(find_base_tag)"
  if [[ -n "$base_tag" ]]; then
    emit_from_base_tag "$base_tag"
    exit 0
  fi
  # No two-part tag: 0.0.<commit-count>
  echo "0.0.$(git rev-list --count HEAD 2>/dev/null || echo 0)"
  exit 0
fi

date -u +%Y.%m.%d
