# MicroEngine contributor entry point

Read [the engineering index](docs/engineering/README.md) before planning or editing.
Repository context must be sufficient without previous chats or agent memory.

Technical autonomy by default, human intervention by exception (owner2026-10-15). Technical ADRs/contracts/dependencies, next roadmap horizons and objective acceptance are delegated after required review; apply the complete exception list in [autonomy](docs/engineering/autonomy.md).

## Current authorization and status

- The next version is a greenfield, general-purpose C#/.NET 2D framework.
- The prototype is retired from the active tree and preserved at `legacy-final`; read docs/engineering/history.md.
- Repository transition was approved and completed. New technical construction is delegated under docs/engineering/autonomy.md; specifications and reviewed significant ADRs must precede implementation.
- ECS, scenes, DI and storage remain unselected. ADR-0003/H1-R1 approve the bounded Loop/Desktop primitive tracer, Raylib isolated to Desktop; no broader backend scope inferred.
- Agents may approve technical ADRs after independent review under the owner delegation; a recommendation alone is not approval.

## Permanent architectural directive

Apply [ADR-0004](docs/engineering/decisions/0004-replaceable-infrastructure.md)/NFR-005 in every subsequent horizon: implementation-independent foundations, public replaceable providers, minimal cohesive opt-in contracts, heterogeneous optional capabilities and explicit target assumptions. Future Windows/Linux/macOS/browser/WebAssembly possibilities are not current support/export commitments. No speculative abstractions/backends or changed functional acceptance.

Apply NFR-006/[ADR-0012](docs/engineering/decisions/0012-playable-platformer-infrastructure.md) as a permanent graphical-style review criterion: no artificial Core sprite-size/resolution/sampling/art-style restriction; explicit demonstrated provider budgets, no speculative renderer.

KPF-1 owner restriction2026-10-18: local commits only; no push or main integration without new explicit authorization. Previous standing positive-feedback delivery rules do not override this horizon restriction.

## Working procedure

1. Read the manifesto, requirements, accepted decisions, roadmap, backlog, execution
   protocol and autonomy policy linked from the index.
2. Identify the authorized scope and linked requirement IDs; ask about genuine ambiguity.
3. Find accepted ADRs and approved capability specifications. If none apply, state that.
4. Record significant architectural changes in an ADR and obtain independent review before delegated technical approval and implementation.
5. For authorized behavior changes, test observable public behavior and update the
   affected requirement, specification, decision and milestone evidence.
6. Report actual checks, limitations and next work. Never infer verification from code existence.

Use the [contribution guide](docs/engineering/contributing.md) for commands and the
[document policy](docs/engineering/document-policy.md) for lifecycle and approval rules.
Do not promote statuses without the evidence and authority those rules require.

## Artifact conventions

- Technical artifacts default to English; follow an explicit language request when provided.
- Keep documentation proportional. Do not create a specification or ADR for every small edit.
- Preserve superseded decisions and historical material; link replacements.
- Coherent commits and normal task-branch pushes to the authorized origin are allowed; see autonomy.md for destination/authentication scope. Positive owner feedback authorizes normal main integration of the evaluated, technically verified increment under autonomy.md (owner2026-10-17). No PR/release, force-push or unrelated publication by inference.
- Use feat/<topic>, fix/<topic> or docs/<topic> branch names, never codex/*. Preserve historical branch references.
- Use Conventional Commits when committing is authorized; no AI attribution or Co-Authored-By.

## Agent skills

The optional [.atl skill registry](.atl/skill-registry.md) describes local skills, not
product decisions. Its absence or machine-specific paths must not prevent onboarding.
Load applicable available skills without expanding the user's authorized scope.

## AI-first task execution

Use docs/engineering/backlog.md as the sole task-state source. During an authorized
development session select the highest-priority eligible ready task, claim it, execute
its scoped packet, verify/review, synchronize docs and record evidence before completion.
Continue independent ready work without routine permission prompts. Follow
[execution](docs/engineering/execution.md) for state transitions, interruptions and stopping.
Autonomy is the default within approved scope: agents implement, verify, correct and record
evidence rather than assigning executable checks to the owner. Escalate only the exceptions in autonomy.md; subjective product acceptance is human-owned, objective technical acceptance is delegated. Preserve acceptance criteria and coverage.
H0 construction is authorized within ADR-0002 after the reviewed H0-1 contract/readiness gate. Technical decisions/contracts and objectively verified milestones may be approved under delegated authority after required review. Never bypass dependencies or mark subjective product validation passed.
The protocol is not a scheduler; later/background work needs separate authorization.
