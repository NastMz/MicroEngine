# DEVOPS-1: CI/CD architecture and release workflow modernization

Objective: one coherent delivery strategy for the SDK, the demo distribution and the site, with fewer manual steps and
no weaker guarantees. Branch `feat/devops-1` from `main` at 56e9fb5. Delivery: single PR, work-unit commits.
Test runner: `node --test` (planner and release tools), `actionlint` for workflows, `node sdk/tests/docs.mjs`.

## Specs

- S1 Diagnose the current architecture end to end (workflows, scripts, NuGet, GitHub releases, demos, Vercel, docs,
  versions, caches, triggers, protections), with a diagram of the existing flow and the human steps. "No presupongas
  que debemos conservar la estructura actual. Tampoco presupongas que debemos reemplazarla completamente."
- S2 Evaluate real alternatives, including "mantener workflows separados con mejor coordinación o introducir un
  orquestador"; justify the operational complexity; prefer native GitHub Actions mechanisms.
- S3 Versioning: distinguish "Versión del código que se está preparando", "Versión pública del SDK", "Versión de las
  distribuciones de juegos", "Revisión desplegada del sitio", each with a source of truth. Keep the six packages on one
  `MicroEngineVersion` (ADR 0019) unless there is a convincing reason.
- S4 Documentation: active version references synchronised or generated; "Las notas históricas deben permanecer
  intactas"; documented samples keep building against the right public packages; no blind text replacement.
- S5 Flows A–H resolved: code change without publication; new SDK version (prepare, validate, approve, publish, verify
  nuget.org, update docs); docs/landing only; demo change (unchanged artifacts kept); engine change affecting SDK and
  demos; failed publication (resume without duplicates, site never pointing to missing artifacts); partial publication
  (nuget.org has the version, a later step failed); urgent hotfix without unrelated distribution work.
- S6 Guarantees kept or improved: artifact integrity, reuse of tested packages, `main` protection, tests before
  publishing, human approval, Trusted Publishing (OIDC), least-privilege permissions, secrets, duplicate prevention,
  downloads/site consistency, recovery, traceability. "No recompiles durante la publicación un artefacto diferente del
  que fue aprobado." No atomic cross-service rollback promises.
- S7 CI optimisation based on measurements, without removing essential validations.
- S8 Implement the chosen architecture; prepare and document any action needing credentials or protected settings.
- S9 Regression tests and dry runs for the release flows; no real publication.
- S10 Deliverables 1–14 (diagnosis, diagrams, problems, alternatives, target, implementation, versioning, docs
  strategy, test results, before/after, operations guide, risks, PR ready for review). No merge, no release.

## Tasks

| ID | Specs | Route | Status | Commit |
| --- | --- | --- | --- | --- |
| T1 | S3, S4, S9 | inline | pending | |
| T2 | S5, S6, S7, S9 | inline | pending | |
| T3 | S5, S6, S8, S9 | inline | pending | |
| T4 | S1, S2, S10 | inline | pending | |

- T1 Version model and tool: `sdk/version.json` (documented SDK version), `sdk/version.mjs` (status, check, release),
  tests, history markers, `docs.mjs` against the documented version with a local feed before publication.
- T2 SDK checks inside daily CI: an `sdk` check in the planner and evidence, `sdk.yml` called by `ci.yml` and gated by
  CI result; no duplicate run on `main`.
- T3 SDK release as one resumable run after CI on `main`; production site held while the documented SDK is not on
  nuget.org; demo release preflight and default version.
- T4 ADR, evaluation (diagnosis, diagrams, alternatives, measurements, before/after), operations guide, docs/ci.md,
  AGENTS.md; PR.

Route evidence: no writer trigger (units depend on one another and share `docs/ci.md`); Risk item 5 (CI, release,
deploy) → one independent verifier at the feature's end. RDD is off globally (`gentle-ai review mode status`).

## Log

- L1 2026-11-01 user request: "MicroEngine — DEVOPS-1 CI/CD Architecture & Release Workflow Modernization" (full brief
  in the session; its sections 1–10 are quoted in S1–S10). "Trabaja en una rama dedicada a partir de `main`. […] No
  hagas merge, no publiques nuevas versiones y no alteres producción sin autorización."
- L2 Findings that shape the design (measured 2026-11-01, runs quoted in docs/evaluations/devops-1.md): SDK
  0.4.0-alpha.2 took two PRs, three workflow dispatches, one approval, a local command and an 18-file edit; `sdk.yml`
  ran twice on the merge commit (the push run was cancelled by the release's call through the shared concurrency
  group); `sdk.yml` is not part of the required check; `sdk-release` refuses a version already on nuget.org, so a
  publish that pushed the packages and then failed cannot be resumed by the workflow.
