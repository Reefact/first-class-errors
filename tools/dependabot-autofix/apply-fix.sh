#!/bin/sh
# Apply the model's chosen repair to a Dependabot pull request and push it.
#
# Usage: tools/dependabot-autofix/apply-fix.sh <pr-number> <head-branch> <verdict-json> <workdir>
#   <workdir> is a SEPARATE checkout of the pull request's head branch (the
#   workflow checks it out with a push-capable token; the scripts themselves come
#   from the base checkout). Only git operations happen here — apply / reword /
#   rebase / push — never a build. The bumped dependency's code is therefore never
#   executed with the write token; the pushed commit is validated by the ordinary
#   ci run in its read-only Dependabot context.
#
# Env: GH_TOKEN (for `gh pr edit`), GITHUB_REPOSITORY, and GITHUB_OUTPUT to report
# the outcome back to the step. Best-effort: any action that cannot be applied
# cleanly leaves the pull request untouched and reports a skip reason, so the
# workflow falls back to an advisory comment rather than pushing a broken change.

set -eu

pr="${1:?usage: apply-fix.sh <pr-number> <head-branch> <verdict-json> <workdir>}"
branch="${2:?}"
verdict="${3:?}"
workdir="${4:?}"
repo="${GITHUB_REPOSITORY:?GITHUB_REPOSITORY is required}"
out="${GITHUB_OUTPUT:-/dev/stdout}"

report() { printf '%s\n' "$@" >> "$out"; }
skip()   { echo "apply-fix: skipping — $1"; report "applied=false" "reason=$1"; exit 0; }

action="$(jq -r '.action // "none"' "$verdict")"
[ "$action" = "none" ] && skip "no actionable fix"

cd "$workdir"

# Configure the committer so a fix we push carries our identity, whatever the
# original author. This is also the loop guard: if the head commit's COMMITTER is
# already the bot, we have acted on this push — do not act again.
git config user.name  "github-actions[bot]"
git config user.email "41898282+github-actions[bot]@users.noreply.github.com"
if [ "$(git log -1 --format='%cn')" = "github-actions[bot]" ] && [ "$action" != "retitle_pr" ]; then
  skip "already amended by a previous run (waiting for the next Dependabot push)"
fi

case "$action" in
  retitle_pr)
    title="$(jq -r '.pr_title // ""' "$verdict")"
    [ -z "$title" ] && skip "retitle_pr without a pr_title"
    gh pr edit "$pr" --repo "$repo" --title "$title" || skip "gh pr edit failed"
    report "applied=true" "impact=trivial" "summary=retitled the pull request"
    ;;

  rewrite_commit_message)
    jq -r '.commit_message // ""' "$verdict" > .da-msg.txt
    [ -s .da-msg.txt ] || skip "rewrite_commit_message without a commit_message"
    git commit --amend -F .da-msg.txt || skip "git commit --amend failed"
    git push --force-with-lease origin "HEAD:${branch}" || skip "force-push failed"
    report "applied=true" "impact=trivial" "summary=rewrote the commit header"
    ;;

  rebase)
    git fetch --no-tags origin main || skip "could not fetch main"
    if ! git rebase FETCH_HEAD; then
      git rebase --abort || true
      skip "rebase onto main hit conflicts (needs a human)"
    fi
    git push --force-with-lease origin "HEAD:${branch}" || skip "force-push failed"
    report "applied=true" "impact=trivial" "summary=rebased onto main"
    ;;

  apply_patch)
    jq -r '.patch // ""' "$verdict" > .da-patch.diff
    [ -s .da-patch.diff ] || skip "apply_patch without a patch"
    jq -r '.commit_message // ""' "$verdict" > .da-msg.txt
    [ -s .da-msg.txt ] || skip "apply_patch without a commit_message"
    if ! git apply --index --whitespace=nowarn .da-patch.diff; then
      skip "the patch did not apply cleanly (needs a human)"
    fi
    git commit -F .da-msg.txt || skip "git commit failed"
    # A new commit on top of Dependabot's: a fast-forward, no force needed.
    git push origin "HEAD:${branch}" || skip "push failed"
    report "applied=true" "impact=code" "summary=applied a code patch"
    ;;

  *)
    skip "unknown action '${action}'"
    ;;
esac
