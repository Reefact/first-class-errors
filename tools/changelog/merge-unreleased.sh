#!/bin/sh
# Merge a freshly generated "[Unreleased]" changelog block into a changelog file,
# IDEMPOTENTLY: the block REPLACES the existing "## [Unreleased]" section rather
# than being prepended, so re-running the drafter refreshes the pending section in
# place instead of stacking duplicate "## [Unreleased]" headings. Every released
# "## [x.y.z]" section below it is left untouched.
#
# Usage: tools/changelog/merge-unreleased.sh <changelog-file> <block-file> <package> <train>
#   <block-file> holds the generated Markdown and MUST start with "## [Unreleased]".
#   When <changelog-file> does not exist it is created with a Keep a Changelog
#   preamble naming <package> and the <train> release train; the preamble is then
#   never rewritten by later runs.

set -eu

file="${1:?usage: merge-unreleased.sh <changelog-file> <block-file> <package> <train>}"
block="${2:?block file required}"
package="${3:-this package}"
train="${4:-}"

[ -f "$block" ] || { echo "merge-unreleased: block file not found: $block" >&2; exit 2; }

# First run for this train: lay down a standard preamble. Later runs keep it as-is
# (the awk below only ever touches the file from the first "## " heading onward).
if [ ! -f "$file" ]; then
  mkdir -p "$(dirname "$file")"
  {
    echo "# Changelog"
    echo
    echo "All notable, user-facing changes to **${package}** are documented here."
    echo
    echo "The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)"
    echo "and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html)."
    if [ -n "$train" ]; then
      # Root-relative link: the changelog may live in a subdirectory (a future
      # train's file is created wherever trains.sh points), and GitHub resolves
      # a leading-slash Markdown link from the repository root either way.
      echo "Releases are cut from the \`${train}\` train (see [CONTRIBUTING.md](/CONTRIBUTING.md))."
    fi
    echo
  } > "$file"
fi

# Rebuild the file as: the preamble (everything before the first "## " heading),
# then the new [Unreleased] block, then every released "## " section — skipping any
# existing "## [Unreleased]" section so it is replaced, not duplicated.
tmp="$(mktemp)"
awk -v blockfile="$block" '
  BEGIN { in_preamble = 1; skip = 0 }
  # First "## " heading closes the preamble and is where the new block goes.
  /^## / && in_preamble {
    in_preamble = 0
    while ((getline line < blockfile) > 0) print line
    print ""
  }
  in_preamble { print; next }
  # Drop the old [Unreleased] section, from its heading up to the next "## ".
  /^## \[[Uu]nreleased\]/ { skip = 1; next }
  /^## / && skip { skip = 0 }
  skip { next }
  { print }
  END {
    # No "## " heading at all (freshly created file): append the block.
    if (in_preamble) {
      while ((getline line < blockfile) > 0) print line
      print ""
    }
  }
' "$file" > "$tmp"

mv "$tmp" "$file"
