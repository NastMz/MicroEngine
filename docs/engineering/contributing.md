<!-- Current authority:2026-10-15 technical delegation supersedes earlier approval-only restrictions; see autonomy.md. Historical dated evidence is not rewritten. -->
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

The current runner and exact serial restore/build/test commands are in [toolchain](toolchain.md). Historical prototype workflows remain in Git history only.
For documentation, read back changed files, check relative links, status consistency
and `git diff --check`. Inspect untracked files explicitly.

## Close work

Report actual evidence, affected IDs, failed/skipped checks and next work. Follow
[document policy](document-policy.md). Coherent commits and normal task-branch pushes to the configured authorized origin are allowed under autonomy.md; PR/main integration/release still need explicit authorization;
use Conventional Commits without AI attribution.

## Start an agent development session

Ask the agent to develop eligible ready tasks. It reads the [backlog](backlog.md),
[execution](execution.md) and [autonomy](autonomy.md), claims one task and continues
independent work after verified closure. Technical architecture approval is delegated under autonomy.md after independent review. Only task-branch push within the explicit origin/authentication scope is authorized; no main integration or release. Select actual ready work from the backlog, including reviewed construction packets.

## Branch naming and delivery

Use feat/<topic>, fix/<topic>, docs/<topic> as appropriate, not codex/*. Current branch: feat/h1-runtime (local rename only, history preserved). Push without force to the same task branch on authorized origin using configured Git authentication. Preserve unrelated local work and verify outgoing commits; never infer permission to merge main, create a PR or rewrite/delete remote history.

## H0 authorized development

ADR-0002 delegates technical H0 choices. See toolchain.md for exact restore/build/test commands and current-state.md for observed checks/limitations. Hosting and contract tests now exist; the external probe/H0 was accepted by the owner on2026-10-15 based on existing evidence; no manual rerun is required. Do not use historical planning-only statements above as current execution authority.
