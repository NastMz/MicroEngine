# Technical autonomy and first graphical horizon
Branch: codex/h0-lifecycle; baseline2201eaf5542c95d30c9f7a67109c51efeade6b85.
Delivery: local coherent work units; no remote/PR/main integration. Test runner: dotnet test serial Release; native smoke when available. Memory mirror unavailable (no registered runtime identity).

## Specs
S1: "autonomía técnica por defecto e intervención humana por excepción". "Actualiza AGENTS.md, la política de autonomía, el procedimiento de ejecución, las plantillas y el backlog para reflejar este modelo."
S2: "Crear, revisar, aprobar y sustituir ADRs técnicos dentro de la visión del proyecto." "Definir y evolucionar contratos públicos." "Elegir dependencias, herramientas y backends apropiados."
S3: "Reevalúa ADR-0003 bajo la nueva autoridad delegada. Si cumple los criterios técnicos y supera revisión independiente, puedes aprobarla, preparar ME-008 e implementarla."
S4: "Las capacidades nuevas deben tener especificaciones y criterios de aceptación verificables." "No se considera completada una tarea sin evidencia suficiente." "Las pruebas automatizadas y las regresiones deben ejecutarse cuando corresponda."
S5: "Después, continúa de forma autónoma con las tareas disponibles y refina progresivamente el siguiente horizonte del roadmap, sin anticipar innecesariamente capacidades lejanas."
S6: "No publiques al remoto ni integres en main sin autorización." "Evita modificaciones destructivas de trabajo ajeno." "No alteres la visión del producto para justificar decisiones técnicas."
S7: "Sea imprescindible evaluar manualmente una experiencia subjetiva, como jugabilidad, ergonomía o presentación." Technical milestone acceptance is delegated; subjective product acceptance is not.

## Tasks
T1 S1/S2/S6: inline completed, independent technical_authority_review PASS after original blocker correction/recheck; runtime N/A.
T2 S2/S3/S4: inline completed, technical_authority_review design PASS, ADR-0003/H1-R1 accepted and ME-008 completed; runtime evidence pending implementation.
T3 S3/S4: inline fixed-step/input/presentation general capability with public tests; pending.
T4 S3/S4/S7: inline private native adapter and external tracer with tests/smoke; pending.
T5 S5/S6/S7: inline evidence, next bounded horizon and local ledger; pending.

## Log
L1 — Owner request2026-10-15: broad technical delegation, ADR/contract/dependency/backend/project/refactoring/task/horizon approval, objective technical acceptance and local commits; independent reviews/specifications/tests/evidence/docs mandatory. Human exceptions remain strategic product changes, destructive/external/irreversible actions, subjective experience, reasonably unresolvable blockers or no useful roadmap-aligned work. No remote or main integration. Above S clauses quote the actionable instruction; full authority captured in autonomy policy.
L2 — Baseline clean for task except pre-existing .gitignore changes and generated .codegraph; preserve both. RDD global off confirmed with elevated read-only query after sandbox access denial. Source H0 mapped with CodeGraph; no changes planned to H0 contracts. Mapping trigger delegated one read-only authority mapper. Parent writes inline; no writer trigger. Forecast exceeds400 authored lines across independently reviewable local units; no PR prepared or remote delivery strategy selected, do not force arbitrary splits by file type.

L3 — Policy review: technical_authority_review PASS after bounded correction of active old authority claims; historical dated evidence preserved. Runtime RED N/A for documentary policy; independent structural72links zero missing, diff-check pass. Local commit follows; RDD off unchanged.

L4 — Governance commit81d2d2d; independent design four original blockers corrected in one batch, targeted PASS. ADR-0003 and H1-R1 approved by delegated technical authority. ME-009 ready, ME-010 pending managed proof; no native installation yet.
