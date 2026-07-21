#!/bin/sh
# Guard the analyzer count advertised in the package README against the real set
# of diagnostics, so the storefront claim can never silently drift again (it did:
# the bullet sat at "16 (FCE001-FCE016)" long after the code reached FCE022).
#
# Source of truth: FirstClassErrors.Analyzers/DiagnosticIds.cs — every FCExxx
# constant it declares is one shipped diagnostic. The flagship bullet in
# FirstClassErrors/README.nuget.md advertises both a count and a range:
#
#     **22 Roslyn analyzers in the box (`FCE001`-`FCE022`).**
#
# This check derives the count and the low/high ids from DiagnosticIds.cs and
# fails if the README's number or either range endpoint disagrees. Numbering is
# a stable handle, not a contiguous contract (see the DiagnosticIds.cs summary),
# so the count is the number of declared ids and the range is their numeric
# min-max — a retired id in the middle keeps this correct.
#
# Usage:  tools/analyzer-count-check/check-analyzer-count.sh
# Exit:   0 = README matches the code, 1 = drift found, 2 = usage/parse error.
#
# Runs in the CI check (.github/workflows/analyzers.yml). It reads only two
# tracked files and needs no toolchain, so it runs anywhere a POSIX shell does.

set -u

# --- locate the repo files, independent of the caller's CWD -------------------
script_dir=$(cd "$(dirname "$0")" && pwd)
root=$(cd "$script_dir/../.." && pwd)
ids_file="$root/FirstClassErrors.Analyzers/DiagnosticIds.cs"
readme="$root/FirstClassErrors/README.nuget.md"

fail() { printf 'analyzer-count-check: %s\n' "$1" >&2; exit "${2:-1}"; }

[ -f "$ids_file" ] || fail "diagnostic ids not found: $ids_file" 2
[ -f "$readme" ]   || fail "package README not found: $readme" 2

# --- source of truth: the declared FCExxx ids --------------------------------
ids=$(grep -oE '"FCE[0-9]+"' "$ids_file" | tr -d '"' | sort -u)
[ -n "$ids" ] || fail "no FCExxx constants found in $ids_file" 2

count=$(printf '%s\n' "$ids" | grep -c .)
low_num=$(printf '%s\n' "$ids" | sed -E 's/^FCE0*//' | sort -n | head -1)
high_num=$(printf '%s\n' "$ids" | sed -E 's/^FCE0*//' | sort -n | tail -1)
low=$(printf 'FCE%03d' "$low_num")
high=$(printf 'FCE%03d' "$high_num")

# --- the claim advertised in the README --------------------------------------
claim=$(grep -E 'Roslyn analyzers in the box' "$readme" | head -1)
[ -n "$claim" ] || fail "no 'N Roslyn analyzers in the box (...)' claim found in $readme" 2

claimed_count=$(printf '%s\n' "$claim" | grep -oE '[0-9]+ Roslyn analyzers in the box' | grep -oE '^[0-9]+')
claimed_codes=$(printf '%s\n' "$claim" | grep -oE 'FCE[0-9]+')
claimed_low=$(printf '%s\n' "$claimed_codes" | head -1)
claimed_high=$(printf '%s\n' "$claimed_codes" | tail -1)

# --- compare -----------------------------------------------------------------
status=0
if [ "${claimed_count:-}" != "$count" ]; then
  printf 'analyzer-count-check: README says %s analyzers, DiagnosticIds.cs declares %s\n' \
    "${claimed_count:-?}" "$count" >&2
  status=1
fi
if [ "${claimed_low:-}" != "$low" ] || [ "${claimed_high:-}" != "$high" ]; then
  printf 'analyzer-count-check: README range (%s-%s) does not match the code range (%s-%s)\n' \
    "${claimed_low:-?}" "${claimed_high:-?}" "$low" "$high" >&2
  status=1
fi

if [ "$status" -ne 0 ]; then
  printf '\nUpdate the bullet in FirstClassErrors/README.nuget.md to read:\n' >&2
  printf '  **%s Roslyn analyzers in the box (`%s`-`%s`).**\n' "$count" "$low" "$high" >&2
  exit 1
fi

printf 'analyzer-count-check: OK - %s analyzers (%s-%s), README matches DiagnosticIds.cs\n' \
  "$count" "$low" "$high"
