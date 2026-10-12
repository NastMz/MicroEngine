# Continue MicroEngine from a fresh session

## Read and establish scope

1. Read [the index](README.md), [manifesto](manifesto.md) and [requirements](requirements.md).
2. Read [accepted decisions](decisions/README.md); distinguish proposals from approval.
3. Check [current work](roadmap.md#current-work), implementation evidence and open questions.
4. Identify authorized work and affected IDs. Current authorization is documentation only.

Previous chats, agent memory and installed skills are not prerequisites.
Root CONTRIBUTING.md describes prototype conventions; this guide covers the new direction.

## Propose and implement

Use the [ADR template](templates/adr.md) for significant architectural choices and request
explicit owner approval. Use the [capability template](templates/capability.md) for concrete
behavior. Neither is required for a typo or small clarification.

When implementation is authorized, test public behavior and relevant integration,
keep genre rules in consumers and update affected records. External examples consume
public APIs without engine internals. Do not duplicate task trackers.

## Existing checks — prototype only

Existing CI uses .NET 9 and these commands from the repository root:

~~~sh
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build --verbosity normal
~~~

Source: [build workflow](../../.github/workflows/build.yml).
Commands restore dependencies and generate artifacts; run only within authorized scope.
They were not run for this documentary foundation. Success does not verify greenfield
capabilities. New runners remain undecided until new projects exist.

For documentation, read back files, check relative links and approval/status consistency,
and run git diff --check. Inspect untracked files explicitly because diff omits them.

## Close the work

Report affected IDs/files, observed checks, limitations, delivery state and next action.
Follow the [document policy](document-policy.md). Commit/push/PR require authorization;
use Conventional Commits without AI attribution.
