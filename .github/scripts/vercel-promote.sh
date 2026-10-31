#!/usr/bin/env bash
# Switches the site's production to a deployment through Vercel's REST API (docs/ci.md, "Deploying the site").
#   vercel-promote.sh current              prints the ID of the deployment production serves now (empty if none)
#   vercel-promote.sh promote <url-or-id>  makes that deployment production and waits until Vercel serves it
# Needs VERCEL_TOKEN, VERCEL_ORG_ID (team or account ID) and VERCEL_PROJECT_ID. The CLI's `promote` and `rollback`
# first read the token's user (/v2/user), which a token scoped to a team cannot do; these calls need only the team
# and the project, like `vercel deploy`.
set -euo pipefail
: "${VERCEL_TOKEN:?}" "${VERCEL_ORG_ID:?}" "${VERCEL_PROJECT_ID:?}"
api=https://api.vercel.com
call() { curl -sS --fail-with-body -H "Authorization: Bearer $VERCEL_TOKEN" "$@"; }
current() { call "$api/v9/projects/$VERCEL_PROJECT_ID?teamId=$VERCEL_ORG_ID" | jq -r '.targets.production.id // empty'; }

case "${1:-}" in
  current) current ;;
  promote)
    target=${2:?deployment URL or ID}
    target=${target#https://}; target=${target%%/*}
    id=$(call "$api/v13/deployments/$target?teamId=$VERCEL_ORG_ID" | jq -r '.id')
    [ -n "$id" ] && [ "$id" != null ] || { echo "error: deployment $target not found" >&2; exit 1; }
    call -X POST -H 'Content-Type: application/json' -d '{}' \
      "$api/v10/projects/$VERCEL_PROJECT_ID/promote/$id?teamId=$VERCEL_ORG_ID" > /dev/null
    for _ in $(seq 60); do
      [ "$(current)" = "$id" ] && { echo "production now serves $id"; exit 0; }
      sleep 5
    done
    echo "error: production does not serve $id after 5 minutes" >&2; exit 1 ;;
  *) echo "usage: vercel-promote.sh current | promote <deployment-url-or-id>" >&2; exit 2 ;;
esac
