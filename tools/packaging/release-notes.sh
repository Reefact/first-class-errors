#!/bin/sh
# Generate GitHub Release notes for ONE release train (lib or cli), containing only the commits
# that belong to that train — so a lib release never lists cli work, and vice versa.
#
# The partition is by Conventional Commit scope (enforced by tools/commit-lint):
#   lib -> scopes core, analyzers, testing   (FirstClassErrors + FirstClassErrors.Testing)
#   cli -> scopes cli, gendoc                 (the fce tool: CLI + GenDoc + worker)
# Commits with no scope (bare `ci:`, `build:`, `chore:` ...) are infrastructure and are left out
# of both trains: these notes describe what changed for the consumer of the package, nothing else.
#
# Usage: tools/packaging/release-notes.sh <scope:lib|cli> <current-tag>
#   Emits Markdown on stdout. Needs full history + tags in the checkout (actions/checkout with
#   fetch-depth: 0) so the previous same-train tag — the lower bound of the range — resolves.

set -eu

if [ "$#" -ne 2 ] || [ -z "$1" ] || [ -z "$2" ]; then
  echo "usage: tools/packaging/release-notes.sh <scope:lib|cli> <current-tag>" >&2
  exit 2
fi
scope="$1"
current_tag="$2"

case "$scope" in
  lib) prefix='lib-v'; train_scopes='core,analyzers,testing' ;;
  cli) prefix='cli-v'; train_scopes='cli,gendoc' ;;
  *)   echo "error: unknown scope '$scope' (expected 'lib' or 'cli')" >&2; exit 2 ;;
esac

# Previous tag of the SAME train (most recent one that is not the current tag). When there is none,
# this is the train's first release: take the whole history up to the current tag.
previous_tag="$(git tag --list "${prefix}*" --sort=-version:refname | grep -Fxv "$current_tag" | head -n1 || true)"
if [ -n "$previous_tag" ]; then
  range="${previous_tag}..${current_tag}"
else
  range="$current_tag"
fi

# One line per commit: "<short-hash><TAB><subject>". Merge commits are skipped — a PR merge commit
# carries no Conventional Commit scope; the real work lives in the commits it brings in.
commits="$(git log "$range" --no-merges --format='%h%x09%s')"

# Keep a commit only when its Conventional Commit scope list intersects this train's scopes. Header
# shape: type(scope[,scope...])[!]: description. A commit with no (scope) group is dropped.
notes=''
while IFS='	' read -r hash subject; do
  [ -z "${subject:-}" ] && continue
  # Extract the first parenthesised scope group ("type(core,cli)!: ..." -> "core,cli"); empty when
  # the header has no scope, which drops the commit.
  scope_group="$(printf '%s' "$subject" | sed -n 's/^[a-z][a-z]*(\([a-z,]*\)).*$/\1/p')"
  [ -z "$scope_group" ] && continue
  matched=0
  OLDIFS=$IFS; IFS=','
  for sc in $scope_group; do
    case ",${train_scopes}," in
      *",${sc},"*) matched=1; break ;;
    esac
  done
  IFS=$OLDIFS
  [ "$matched" = 1 ] && notes="${notes}- ${subject} (${hash})
"
done <<EOF
${commits}
EOF

echo "## What's changed"
echo
if [ -n "$notes" ]; then
  printf '%s' "$notes"
else
  echo "_No user-facing changes in this component._"
fi
