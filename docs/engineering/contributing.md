# Continue MicroEngine from a fresh session

## Read and establish scope

1. Read [index](README.md), [manifesto](manifesto.md) and [requirements](requirements.md).
2. Read [accepted decisions](decisions/README.md), [roadmap](roadmap.md) and approval evidence.
3. Identify authorized work and affected IDs. Repository transition is approved;
   new engine implementation still needs approved scope and contracts.

No previous chat or external memory is required. [History](history.md) explains access
to the retired prototype; do not restore it over active work by default.

## Propose and implement

Use the [ADR template](templates/adr.md) for significant decisions and the
[capability template](templates/capability.md) for concrete behavior. Do not require
these for typos. Keep genre rules in consumers and test public behavior/integration.
Update affected records rather than creating duplicate trackers.

## Checks

No greenfield build/test runner exists. Record commands when projects are approved and
created. Historical prototype workflows are available via Git history only.
For documentation, read back changed files, check relative links, status consistency
and `git diff --check`. Inspect untracked files explicitly.

## Close work

Report actual evidence, affected IDs, failed/skipped checks and next work. Follow
[document policy](document-policy.md). Commit/push/PR need applicable authorization;
use Conventional Commits without AI attribution.
