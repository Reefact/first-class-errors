#!/bin/sh
# Assemble the context the advisory ADR check reasons over: what a branch changes
# against its base, and the current ADR base it must be checked against. Emits one
# delimited bundle on stdout for the workflow to hand to the model as DATA — never
# as instructions (the prompt says so, and the workflow jq-escapes it).
#
# Usage: tools/adr-check/collect-context.sh <base-ref>
#   <base-ref> is any ref or SHA the branch is diffed against — typically the
#   merge-base with main. The diff and the changed-file list come from git, so this
#   runs on a plain branch with no pull request; the ADRs are read from the working
#   tree (the branch's own head).
#
# Two byte caps keep the payload bounded on a huge diff or a large ADR base; each
# truncation is announced in-band so the model — and a human reading the logs —
# knows the view was clipped rather than empty.

set -eu

base="${1:?usage: collect-context.sh <base-ref>}"

DIFF_MAX_BYTES="${DIFF_MAX_BYTES:-200000}"
ADR_MAX_BYTES="${ADR_MAX_BYTES:-120000}"
adr_dir="doc/handwritten/for-maintainers/adr"

# --- changed files (a strong, cheap signal on its own) ------------------------
# The set of touched paths already separates "only tests/docs" from "public API
# surface" before a single line of diff is read.
printf '<changed_files>\n'
git diff --name-only "$base"..HEAD
printf '</changed_files>\n\n'

# --- the unified diff, capped -------------------------------------------------
git diff "$base"..HEAD > .adr-diff.txt
diff_size="$(wc -c < .adr-diff.txt)"
printf '<diff>\n'
head -c "$DIFF_MAX_BYTES" .adr-diff.txt
if [ "$diff_size" -gt "$DIFF_MAX_BYTES" ]; then
  printf '\n[... diff truncated: %s of %s bytes shown ...]\n' "$DIFF_MAX_BYTES" "$diff_size"
fi
printf '</diff>\n\n'
rm -f .adr-diff.txt

# --- the ADR base -------------------------------------------------------------
# Every recorded decision the branch must be checked against. The whole body of
# each ADR is included while the budget lasts (the base is small); once the budget
# is spent, later ADRs fall back to their header plus the one-sentence Decision,
# which is enough to spot a supersession or a conflict.
printf '<adr_base>\n'
printf 'Index (from %s/README.md):\n' "$adr_dir"
sed -n '/^| *ADR *|/,$p' "$adr_dir/README.md" 2>/dev/null || printf '(index unavailable)\n'
printf '\n'

total=0
for f in "$adr_dir"/[0-9]*.md; do
  [ -f "$f" ] || continue
  size="$(wc -c < "$f")"
  total=$((total + size))
  printf -- '--- %s ---\n' "$f"
  if [ "$total" -gt "$ADR_MAX_BYTES" ]; then
    sed -n '1,14p' "$f"
    awk '/^## Decision/{p=1;print;next} /^## /{p=0} p' "$f"
    printf '[... %s body omitted to stay within the ADR budget ...]\n' "$f"
  else
    cat "$f"
  fi
  printf '\n'
done
printf '</adr_base>\n'
