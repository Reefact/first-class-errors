#!/bin/sh
# Single source of truth for the release trains.
#
# The published trains version independently and each owns a tag prefix, a set
# of Conventional Commit scopes, a NuGet package label, and a changelog file. That
# mapping used to be copied verbatim into tools/packaging/release-notes.sh and
# tools/changelog/collect-prs.sh; it now lives here, once. The scripts and the
# changelog workflow *source* this file, so the partition can never drift between
# "what a release publishes" and "what the changelog documents".
#
# This file is meant to be SOURCED (`. tools/trains.sh`), not executed — it only
# defines functions and mutates nothing.
#
# ── Adding a train ────────────────────────────────────────────────────────────
# Add one row to trains_rows() below, then make the static edits GitHub forces
# (tag trigger, choice options, commit-lint scopes, packaging). The full checklist
# is doc/handwritten/for-maintainers/AddingAReleaseTrain.en.md.
#
# Row format (pipe-separated, no spaces around the pipes except inside the label):
#   <id>|<tag-prefix>|<scopes csv>|<changelog file>|<package label>
# Keep the scopes a subset of the closed SCOPES list in
# tools/commit-lint/lint-commit-message.sh.
trains_rows() {
  cat <<'ROWS'
lib|lib-v|core,analyzers,testing,binder|CHANGELOG.md|FirstClassErrors, FirstClassErrors.Testing and FirstClassErrors.RequestBinder
cli|cli-v|cli,gendoc|FirstClassErrors.Cli/CHANGELOG.md|FirstClassErrors.Cli (the fce .NET tool)
dum|dum-v|dummies|Dummies/CHANGELOG.md|Dummies
ROWS
}

# _train_field <id> <field-name> — echo one field of a train's row, or nothing if
# the id is unknown. Fields: prefix | scopes | changelog | package.
_train_field() {
  _tf_id="$1"; _tf_field="$2"
  trains_rows | while IFS='|' read -r id prefix scopes changelog package; do
    [ "$id" = "$_tf_id" ] || continue
    case "$_tf_field" in
      prefix)    printf '%s\n' "$prefix" ;;
      scopes)    printf '%s\n' "$scopes" ;;
      changelog) printf '%s\n' "$changelog" ;;
      package)   printf '%s\n' "$package" ;;
    esac
  done
}

train_ids()     { trains_rows | cut -d'|' -f1; }
prefix_of()     { _train_field "$1" prefix; }
scopes_of()     { _train_field "$1" scopes; }
changelog_of()  { _train_field "$1" changelog; }
package_of()    { _train_field "$1" package; }

# require_train <id> — succeed if <id> is a known train, else print the known ids
# to stderr and return 1. Callers decide the exit code.
require_train() {
  if [ -n "$(prefix_of "$1")" ]; then
    return 0
  fi
  printf 'unknown train "%s" (known: %s)\n' \
    "$1" "$(train_ids | tr '\n' ' ' | sed 's/ *$//')" >&2
  return 1
}
