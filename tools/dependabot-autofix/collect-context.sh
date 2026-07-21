#!/bin/sh
# Assemble the context the advisory Dependabot triage reasons over: what the pull
# request changes, which of its checks failed, and the failing job logs. Emits one
# delimited bundle on stdout for the workflow to hand to the model as DATA — never
# as instructions (the prompt says so, and the workflow jq-escapes it).
#
# Usage: tools/dependabot-autofix/collect-context.sh <pr-number> <head-sha>
#   Needs `gh` authenticated (GH_TOKEN) with contents: read, pull-requests: read,
#   checks: read and actions: read, and GITHUB_REPOSITORY set (owner/repo).
#
# The pull request is read through the GitHub API (gh pr diff / check-runs / run
# logs); nothing from the branch is ever checked out or executed. That is the whole
# point of running on `workflow_run` in the base context: the bumped dependency's
# code has already run in the read-only ci context, and this triage only *reads*
# the result. It never builds untrusted code with a write token.
#
# Every call is best-effort: a missing diff or missing logs is announced in-band
# and the bundle is still emitted, so a transient gh hiccup degrades the triage
# rather than failing the run.

set -eu

pr="${1:?usage: collect-context.sh <pr-number> <head-sha>}"
sha="${2:?usage: collect-context.sh <pr-number> <head-sha>}"
repo="${GITHUB_REPOSITORY:?GITHUB_REPOSITORY is required}"

DIFF_MAX_BYTES="${DIFF_MAX_BYTES:-120000}"
LOG_MAX_BYTES="${LOG_MAX_BYTES:-120000}"

# --- the pull request itself --------------------------------------------------
printf '<pull_request>\n'
gh pr view "$pr" --repo "$repo" \
  --json number,title,headRefName,author,commits \
  --jq '"number: \(.number)\ntitle: \(.title)\nbranch: \(.headRefName)\nauthor: \(.author.login)\ncommit subjects:\n" + ([.commits[] | "  - \(.messageHeadline)"] | join("\n"))' \
  2>/dev/null || printf '(pull request metadata unavailable)\n'
printf '\n</pull_request>\n\n'

# --- changed files (a strong, cheap signal on its own) ------------------------
printf '<changed_files>\n'
gh pr diff "$pr" --repo "$repo" --name-only 2>/dev/null || printf '(changed-file list unavailable)\n'
printf '</changed_files>\n\n'

# --- the unified diff, capped -------------------------------------------------
if gh pr diff "$pr" --repo "$repo" > .da-diff.txt 2>/dev/null; then
  diff_size="$(wc -c < .da-diff.txt)"
  printf '<diff>\n'
  head -c "$DIFF_MAX_BYTES" .da-diff.txt
  if [ "$diff_size" -gt "$DIFF_MAX_BYTES" ]; then
    printf '\n[... diff truncated: %s of %s bytes shown ...]\n' "$DIFF_MAX_BYTES" "$diff_size"
  fi
  printf '</diff>\n\n'
  rm -f .da-diff.txt
else
  printf '<diff>\n(diff unavailable)\n</diff>\n\n'
fi

# --- which checks failed ------------------------------------------------------
# Only the non-successful check runs on the head commit; the workflow has already
# confirmed at least one failed and that none is still running.
printf '<failing_checks>\n'
gh api "repos/${repo}/commits/${sha}/check-runs" --paginate \
  --jq '.check_runs[] | select(.conclusion != "success" and .conclusion != "neutral" and .conclusion != "skipped")
        | "  - \(.name): \(.conclusion // "pending")"' \
  2>/dev/null || printf '(check-run list unavailable)\n'
printf '</failing_checks>\n\n'

# --- the failing job logs, capped ---------------------------------------------
# Best-effort: pull the failed steps of every failed workflow run on this commit.
# --log-failed keeps the payload to the steps that actually failed.
printf '<failure_logs>\n'
: > .da-logs.txt
# Filter by head_sha through the REST API (a stable filter, unlike `gh run list`).
run_ids="$(gh api "repos/${repo}/actions/runs?head_sha=${sha}&per_page=100" \
  --jq '.workflow_runs[] | select(.conclusion == "failure" or .conclusion == "timed_out" or .conclusion == "startup_failure") | .id' \
  2>/dev/null || true)"
if [ -n "$run_ids" ]; then
  for id in $run_ids; do
    {
      printf '===== failed run %s =====\n' "$id"
      gh run view "$id" --repo "$repo" --log-failed 2>/dev/null || printf '(logs unavailable for run %s)\n' "$id"
      printf '\n'
    } >> .da-logs.txt
  done
  log_size="$(wc -c < .da-logs.txt)"
  head -c "$LOG_MAX_BYTES" .da-logs.txt
  if [ "$log_size" -gt "$LOG_MAX_BYTES" ]; then
    printf '\n[... logs truncated: %s of %s bytes shown ...]\n' "$LOG_MAX_BYTES" "$log_size"
  fi
  rm -f .da-logs.txt
else
  printf '(no failed-run logs could be retrieved)\n'
fi
printf '</failure_logs>\n'
