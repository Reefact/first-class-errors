#!/bin/sh
# Create, update, or remove the single advisory ADR-check comment on a pull
# request. Idempotent by design: the comment is found by a hidden marker so
# repeated runs (one per push) refresh the same comment instead of stacking new
# ones, and a pull request that no longer needs a flag has its stale comment
# removed rather than left to mislead.
#
# Usage: tools/adr-check/upsert-comment.sh <pr-number> <needs-comment: true|false> <body-file>
#   <body-file> must already contain the marker; the workflow prepends it.
#   Needs `gh` authenticated (GH_TOKEN) with pull-requests: write, and
#   GITHUB_REPOSITORY set (owner/repo).

set -eu

pr="${1:?usage: upsert-comment.sh <pr-number> <true|false> <body-file>}"
needs="${2:?}"
body_file="${3:?}"
marker='<!-- adr-check -->'
repo="${GITHUB_REPOSITORY:?GITHUB_REPOSITORY is required}"

# The id of the one existing marked comment, if any. Paginate so it is still
# found on a long thread.
existing="$(gh api "repos/${repo}/issues/${pr}/comments" --paginate \
  --jq "map(select(.body | contains(\"${marker}\")))[0].id // empty")"

if [ "$needs" = "true" ]; then
  # Build the request body with jq so the Markdown is JSON-escaped exactly once.
  jq -n --rawfile body "$body_file" '{body: $body}' > .adr-comment.json
  if [ -n "$existing" ]; then
    gh api -X PATCH "repos/${repo}/issues/comments/${existing}" --input .adr-comment.json >/dev/null
    echo "Updated ADR-check comment #${existing}."
  else
    gh api -X POST "repos/${repo}/issues/${pr}/comments" --input .adr-comment.json >/dev/null
    echo "Posted a new ADR-check comment."
  fi
  rm -f .adr-comment.json
else
  if [ -n "$existing" ]; then
    gh api -X DELETE "repos/${repo}/issues/comments/${existing}" >/dev/null
    echo "Removed stale ADR-check comment #${existing} (nothing to flag)."
  else
    echo "No ADR-check comment needed."
  fi
fi
