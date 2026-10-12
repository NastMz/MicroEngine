# MicroEngine contributor entry point

Read [the engineering index](docs/engineering/README.md) before planning or editing.
Repository context must be sufficient without previous chats or agent memory.

## Current authorization and status

- The next version is a greenfield, general-purpose C#/.NET 2D framework.
- The prototype is retired from the active tree and preserved at `legacy-final`; read docs/engineering/history.md.
- Repository transition was approved and completed. No new engine implementation,
  dependencies or CI are authorized without explicit scope and approved contracts.
- ECS, scenes, DI, storage, renderers and dependencies are not approved technical choices.
- An agent recommendation is a proposal, not an accepted decision.

## Working procedure

1. Read the manifesto, requirements, decisions and current roadmap in the index.
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
