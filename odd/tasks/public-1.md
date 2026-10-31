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

- S11 (L6, supersedes S2's model) Engine under MIT: "Aplicar la licencia MIT oficial al código del motor." "La licencia
  MIT debe utilizar su texto estándar sin modificaciones." "No añadas restricciones adicionales a MIT."
- S12 Demos reserved: "no concedemos autorización general para redistribuir o comercializar los juegos completos, ni
  para vender versiones prácticamente idénticas con otro nombre." "Sus códigos pueden consultarse públicamente para
  aprender cómo funciona MicroEngine." "Delimita cuidadosamente qué archivos de `games/` quedan bajo derechos
  reservados, cuáles pertenecen a terceros y cuáles tienen otras licencias." "No apliques indiscriminadamente un aviso
  de copyright propio a recursos de terceros." Samples: "Los ejemplos pequeños, como HelloEngine, deben ser
  reutilizables bajo MIT."
- S13 Structure: "Implementa una estructura inequívoca para que ningún desarrollador interprete que el MIT de la raíz
  autoriza automáticamente la redistribución comercial de los cuatro juegos." "Comprueba que los avisos no se
  contradigan."
- S14 History: compare A (publish existing) and B (clean-history repository); "Determina si realmente necesitamos la
  alternativa B teniendo en cuenta que hemos elegido MIT." "No reescribas historial ni crees repositorios nuevos sin
  autorización."
- S15 Contributions under MIT: "No impongas automáticamente un CLA complejo si no existe una necesidad concreta."
  "Mantén la revisión y aceptación de cambios bajo control de los mantenedores."
- S16 Public CI: keep INFRA-1/PUBLIC-1; "El objetivo ya no es minimizar el consumo de minutos gratuitos a cualquier
  coste, sino conseguir un CI rápido, confiable y escalable."
- S17 Public docs: "Actualizar todas las referencias que todavía indiquen que MicroEngine es propietario o que su
  código fuente es privado." "La documentación no debe presentar las demos como software MIT cuando no lo son."
- S18 Final audit and delivery: run the auditor; "Distingue bloqueos reales de advertencias aceptables." "No cambies
  todavía la visibilidad del repositorio. No hagas push ni merge sin autorización."

## Tasks

| ID | Specs | Route | Status | Commit |
| --- | --- | --- | --- | --- |
| T1 | S5 | inline | done | d0ce2b9: evidence only from this repository's runs; fork-safe demo builds |
| T2 | S5 | inline | done | 9ec307c: project-graph planner (tests and games per change), macOS informative on public runners |
| T3 | S1, S8 | inline | done | 7b06d00: `public-audit.mjs` reproducible pre-publication audit |
| T4 | S4 | inline | done | a1ddacf: SECURITY, templates, contribution status, code of conduct fixes |
| T5 | S2, S3, S4 | inline | done | 8a21583: licensing proposal with contributor agreement (CLA) terms |
| T6 | S5, S6 | inline | done | 3e04bac: ADR 0014, docs/ci.md public section |
| T7 | S1-S9 | inline | done | 767878d: `docs/evaluations/public-1.md` report |
| T8 | S11-S13 | inline | done | c472e23: MIT `LICENSE`, `games/LICENSE`, notices in packages, ADR 0015 |
| T9 | S17 | inline | done | 3804c47: site, docs and check-dist for MIT engine and reserved demos |
| T10 | S15 | inline | done | e668acd: contributions under MIT (inbound = outbound, no CLA) |
| T11 | S16 | inline | done | 39fff89: public CI: every changed demo key built on public runners |
| T12 | S14, S18 | inline | done | audit rerun, history options, report and final checklist |

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
- L6 (2026-10-31) User decision, verbatim title: "MicroEngine — PUBLIC-1: Finalización de licencias y preparación de
  publicación" (full text in session): "MicroEngine será un proyecto open source bajo MIT, con demos protegidas
  mediante licencias independientes. No queremos una licencia propietaria personalizada, source-available,
  PolyForm, Defold ni un contrato restrictivo propio." Supersedes S2's licensing model and the CLA proposal.
- L7 Mixkit: their published terms (mixkit.co/llm-info, read 2026-10-31) forbid making items available to third
  parties and reselling them unaltered; 24 commits on `origin/main`'s ancestry contain the four files. No commit from
  the rebuild (1de4c1c) onward does.
- L8 Found while applying MIT: all four demos first appear in the 68 MIT-tree commits (9c6f14d, 9e231da, 08bd6a8,
  e57d28a), so option B (public history from `71115bc`) stays recommended even with an MIT engine.
- L9 Checks (round 2): script tests 41 passed; site tests 31 passed; actionlint clean; `SITE_DEMOS=none npm run build`
  passed; layout browser suite passed in Chrome and Firefox (a 320 px overflow from a long button label was fixed);
  `public-audit.mjs` exit 0 after excluding its own fixtures (61f24f3). Play and touch suites not run: they need
  the demo packages, whose keys all changed with the licence files.
- L10 Blockers for the owner: authorise option B; code of conduct contact; repository settings P6-P11.
- L11 (2026-10-31) User request: "reconsideres la recomendación de crear un repositorio nuevo. Mi opción preferida es
  conservar el repositorio actual de GitHub, reconstruir su historial Git desde un punto limpio y hacer force push"
  (full text in session); "No ejecutes force push, no elimines referencias remotas y no cambies la visibilidad
  todavía."
- L12 Evidence: 14 merged PRs, owner-authored, all greenfield; 43 refs (27 branches, 2 tags, 14 refs/pull/N/head),
  every one reaches the 24 Mixkit commits. PR #14 is merged into main (d0b59ef, tree equal to dbeaf67). The cut needs
  two grafts: 71115bc as root and 1d4facd without its first parent 87be497 (an ancestor of 71115bc). Simulation in an
  isolated bare clone: trees of main and feat/public-1 identical, 60 commits, one root, 0 Mixkit, 0 MIT without
  games/LICENSE, pack 42.8 to 14.1 MiB, tests scripts 41 / site 31 / .NET 282 passed, audit exit 0. Only blocker:
  refs/pull/* are read-only and need GitHub Support. Report sections 3 and 15 now recommend the rebuild in this
  repository.
