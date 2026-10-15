# MicroEngine contributor entry point

Read [the engineering index](docs/engineering/README.md) before planning or editing.
Repository context must be sufficient without previous chats or agent memory.

## Current authorization and status

- The next version is a greenfield, general-purpose C#/.NET 2D framework.
- The prototype is retired from the active tree and preserved at `legacy-final`; read docs/engineering/history.md.
- Repository transition was approved and completed. No new engine implementation,
  dependencies or CI are authorized without explicit scope and approved contracts.
- ECS, scenes, DI, storage and native renderers are not approved technical choices. H0 test-only dependencies are selected under ADR-0002 delegation.
- An agent recommendation is a proposal, not an accepted decision.

## Working procedure

1. Read the manifesto, requirements, accepted decisions, roadmap, backlog, execution
   protocol and autonomy policy linked from the index.
2. Identify the authorized scope and linked requirement IDs; ask about genuine ambiguity.
3. Find accepted ADRs and approved capability specifications. If none apply, state that.
4. Propose significant architectural changes before implementing them.
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
- Do not commit, publish or perform remote operations without the applicable authorization.
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
evidence rather than assigning executable checks to the owner. Escalate only decisions outside
delegated scope/risk/authorization or owner product acceptance. Pending product acceptance does
not require repeating automated tests and blocks only genuinely dependent work; preserve all
acceptance criteria and coverage and never accept an owner-owned milestone automatically.
H0 construction is authorized within ADR-0002 after the reviewed H0-1 contract/readiness gate. Never infer
accepted contracts from proposals, bypass dependencies or mark manual milestones passed.
The protocol is not a scheduler; later/background work needs separate authorization.
