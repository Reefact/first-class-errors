#!/bin/sh
# Collect the merged pull requests that belong to ONE release train (lib or cli)
# and emit them as slim JSON on stdout, for the changelog drafter to summarise.
#
# The train partition mirrors tools/packaging/release-notes.sh EXACTLY — by the
# Conventional Commit scope carried by a pull request's own commits, not by
# labels or by the pull request title:
#   lib -> scopes core, analyzers, testing   (FirstClassErrors + FirstClassErrors.Testing)
#   cli -> scopes cli, gendoc                 (the fce tool: CLI + GenDoc + worker)
# A pull request is kept when at least one of its commits carries a scope in the
# train's set. Pull requests whose commits are all scopeless infrastructure
# (bare `ci:` / `chore:` / `docs:` ...) belong to neither train and are dropped —
# the same rule release-notes.sh applies to commits, so the human-facing changelog
# and the generated GitHub Release notes describe the same set of changes.
#
# Usage: tools/changelog/collect-prs.sh <lib|cli>
#   Reads the optional environment variable FROM_REF: the previous tag whose merge
#   time bounds the range. When empty, the train's latest tag is used; when there
#   is none either (the train's first release), the whole history is taken.
#   Needs `gh` authenticated (GH_TOKEN in the environment) and a full checkout
#   (actions/checkout with fetch-depth: 0) so the tag timestamp resolves.
#
# Output: a JSON array [{number,title,body,labels:[name],author}] — possibly [].

set -eu

component="${1:-}"
# Tag prefix and train scopes come from tools/trains.sh (the single source of truth
# shared with release-notes.sh), so the changelog and the release notes never diverge.
. "$(dirname "$0")/../trains.sh"
prefix="$(prefix_of "$component")"
train_scopes="$(scopes_of "$component")"
if [ -z "$prefix" ]; then
  echo "usage: collect-prs.sh <$(train_ids | tr '\n' '|' | sed 's/|$//')>" >&2
  exit 2
fi

# Lower bound of the range: the previous same-train tag (explicit FROM_REF, or the
# train's latest tag). Sorted by version so lib-v1.10.0 outranks lib-v1.9.0.
from="${FROM_REF:-}"
if [ -n "$from" ]; then
  # FROM_REF is free text from the workflow input. Validate it resolves to a real
  # commit before handing it to git: --end-of-options stops a leading-dash value
  # (e.g. "--all", "--output=x") from being parsed as an option — which would
  # silently produce a wrong range or write files instead of failing.
  if ! git rev-parse --verify --quiet --end-of-options "${from}^{commit}" >/dev/null; then
    echo "error: from_ref '$from' does not resolve to a commit" >&2
    exit 2
  fi
else
  from="$(git tag --list "${prefix}*" --sort=-version:refname | head -n1 || true)"
fi

# PRs merged into main, optionally after the tag's merge time. Use the tag commit's
# committer timestamp (ISO 8601) as a strict lower bound so the boundary is
# second-accurate, not day-accurate — a PR merged just before the release is not
# recounted here. With no previous tag this is the train's first release: take
# every merged PR on main.
search='is:merged base:main'
if [ -n "$from" ]; then
  since="$(git log -1 --format=%cI "$from")"
  search="${search} merged:>${since}"
fi

candidates="$(gh pr list --search "$search" --limit 200 \
  --json number,title,body,labels,author)"

# Classify each candidate by the scopes of its own commits. One `gh pr view` per
# candidate is fine for a manually dispatched job (a release spans a handful of
# PRs). `</dev/null` on gh keeps it from consuming the outer loop's stdin.
printf '%s\n' "$candidates" | jq -c '.[]' | while IFS= read -r pr; do
  number="$(printf '%s' "$pr" | jq -r '.number')"
  headlines="$(gh pr view "$number" --json commits \
    --jq '.commits[].messageHeadline' </dev/null 2>/dev/null || true)"

  matched=0
  # Extract the first parenthesised scope group from each commit header
  # ("feat(core,cli)!: ..." -> "core,cli") — the same extraction release-notes.sh
  # uses — and intersect it with this train's scopes.
  while IFS= read -r headline; do
    [ -z "${headline:-}" ] && continue
    group="$(printf '%s' "$headline" | sed -n 's/^[a-z][a-z]*(\([a-z,]*\)).*$/\1/p')"
    [ -z "$group" ] && continue
    OLDIFS=$IFS; IFS=','
    for sc in $group; do
      case ",${train_scopes}," in
        *",${sc},"*) matched=1 ;;
      esac
    done
    IFS=$OLDIFS
    if [ "$matched" = 1 ]; then break; fi
  done <<EOF
${headlines}
EOF

  if [ "$matched" = 1 ]; then
    printf '%s\n' "$pr"
  fi
done | jq -s '[.[] | {number, title, body: (.body // ""), labels: [.labels[].name], author: .author.login}]'
