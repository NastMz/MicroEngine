#!/usr/bin/env bash
# Runs the .NET tests the planner selected (docs/ci.md, "What runs for a change"):
#   dotnet-test.sh <tests> <filter> <results-dir>
# <tests> is "all" (the whole solution), a comma-separated list of test projects (.github/scripts/projects.mjs), or
# empty: a change that no test project depends on (a sample) is still compiled with the solution.
set -euo pipefail
tests=$1 filter=$2 results=$3
if [ "$tests" = all ]; then
  exec dotnet test MicroEngine.slnx -c Release --filter "$filter" --logger trx --results-directory "$results"
fi
if [ -z "$tests" ]; then
  echo "No test project depends on this change; building the solution."
  exec dotnet build MicroEngine.slnx -c Release
fi
IFS=, read -ra projects <<< "$tests"
status=0
for project in "${projects[@]}"; do
  name=$(basename "$project" .csproj)
  dotnet test "$project" -c Release --filter "$filter" --logger "trx;LogFileName=$name.trx" --results-directory "$results" || status=1
done
exit "$status"
