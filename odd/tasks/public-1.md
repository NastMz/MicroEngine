# PUBLIC-1: readiness for a public repository

Objective: prepare MicroEngine to become a public GitHub repository under a restrictive source-available model, with
safe, selective CI on standard hosted runners. Visibility is **not** changed, no final licence is published, no
production release is made. Branch `feat/public-1` (from `feat/ci-cost` dbeaf67, INFRA-1), worktree
`MicroEngine-worktrees/public-1`. No push, PR or merge without the owner.
Test runners: `node --test .github/scripts/*.test.mjs`, `npm test --prefix site`, `actionlint`. Delivery: ask-on-risk.

## Specs

- S1 Audit (section 2): "Archivos actuales." "Historial de Git." "Ramas y tags." "GitHub Releases y artefactos."
  "Workflows y sus logs." "ADRs y evaluaciones internas." "Documentación." "Archivos de configuración." "Metadatos de
  commits." "Direcciones de correo y posibles datos personales." "Credenciales, tokens o secretos." "Recursos
  gráficos, musicales y otras dependencias." "Código y contenido con licencias de terceros." Classify publishable /
  withdraw / needs authorisation. "No reescribas el historial ni elimines archivos históricos sin autorización."
  Credentials: "documenta el riesgo y el procedimiento de rotación; no reproduzcas sus valores en el informe."
- S2 Licensing research (section 3): compare OSI open source, free software, source-available, proprietary with public
  code, permissive, copyleft, SDK binary licences; study existing licences; evaluate a source licence, an SDK/runtime
  licence and separate asset licences; contributions. "No presentes el resultado como una licencia OSI-approved si
  restringe redistribución o determinados usos." "Prepara una propuesta jurídica para revisión, pero no la declares
  legalmente aprobada."
- S3 MIT history (section 4): when MIT applied, which implementation, what was published, rights possibly granted,
  implications, alternatives including a clean-history repository. "No asumas automáticamente que cambiar un archivo
  `LICENSE` revoca permisos anteriores."
- S4 Contributions (section 5): README, CONTRIBUTING, CODE_OF_CONDUCT, SECURITY, issue and PR templates, contribution
  licensing, CLA evaluation. "No debemos afirmar que aceptamos contribuciones abiertas si la licencia propuesta hace
  jurídicamente ambiguo ese proceso." "No crees obligaciones jurídicas definitivas sin revisión."
- S5 Public CI (sections 6, 7): selection from "un grafo real de afectación, no de comparaciones superficiales entre
  nombres de carpetas"; "Conserva un comportamiento conservador frente a cambios desconocidos." "Un cambio exclusivo
  de documentación no debe reconstruir las cuatro demos." Fork PR safety: "No expongas secretos a código no
  confiable." "Evita utilizar runners self-hosted para PRs públicos no confiables." Android/iOS stay experimental.
- S6 Releases and deployment (section 8): manual, versioned, reproducible; reuse approved packages; invalidation when
  the core changes; engine vs demos vs site versioning.
- S7 Public documentation plan for DOCS-2 (section 9), without rewriting the 29 pages.
- S8 Security and publication checklist with objective conditions (section 10); blockers are reported, not hidden.
- S9 Deliverables (section 12): one report with the 15 listed sections.
- S10 Constraints (section 11): "No hagas push ni merge sin autorización." "No cambies el repositorio a público. No
  publiques una nueva licencia definitiva. No crees releases de producción." Reuse INFRA-1; do not implement INFRA-2's
  local-first proposals.

## Tasks

| ID | Specs | Route | Status | Commit |
| --- | --- | --- | --- | --- |
| T1 | S5 | inline | done | d0ce2b9: evidence only from this repository's runs; fork-safe demo builds |
| T2 | S5 | inline | done | 9ec307c: project-graph planner (tests and games per change), macOS informative on public runners |
| T3 | S1, S8 | inline | done | 7b06d00: `public-audit.mjs` reproducible pre-publication audit |
| T4 | S4 | inline | done | a1ddacf: SECURITY, templates, contribution status, code of conduct fixes |
| T5 | S2, S3, S4 | inline | done | 8a21583: licensing proposal with contributor agreement (CLA) terms |
| T6 | S5, S6 | inline | done | 3e04bac: ADR 0014, docs/ci.md public section |
| T7 | S1-S9 | inline | done | `docs/evaluations/public-1.md` report |

Commits are recorded in `## Log`.

## Log

- L1 (2026-10-31) User request: "MicroEngine — PUBLIC-1: Preparación integral para repositorio público" (full text in
  session; specs above quote it).
- L2 Audit facts (2026-10-31): 446 commits on all refs; 68 commits of the current implementation carry the MIT grant
  text (1de4c1c..616a039, 2026-10-29/30), all pushed and on `origin/main`; 250 old-implementation MIT commits in
  `reference/pre-rebuild`, 11 more on other old branches. Four Mixkit audio files exist in old history without a
  notice. No secret values found in history. Unused `NUGET_API_KEY` secret; no NuGet package was ever published.
- L3 Finding: a fork pull request could not pass CI on a game change (the demo store needs `contents: write`, which
  fork tokens never have) and evidence lookup accepted artifacts from any run. Fixed in T1.
- L4 Commits: T1 d0ce2b9, T2 9ec307c, T3 7b06d00, T4 a1ddacf, T5 8a21583, T6 3e04bac, T7 (report, this commit).
  Checks: planner/graph/evidence/cost/audit tests 40 passed; site tests 31 passed; actionlint clean;
  `dotnet-test.sh` with two projects 140 passed. RED observed for the fork evidence rule, the overlay store and
  `planScope`; `projects.mjs` tests were written alongside it. No hosted workflow was run.
- L5 Blockers left for the owner: counsel on the 68 MIT-tree commits (or option C), the Mixkit files in history,
  the code of conduct contact, the site's "source not distributed" page at publication time.
