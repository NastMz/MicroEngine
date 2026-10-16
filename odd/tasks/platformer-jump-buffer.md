# Optional landing jump buffer in the external example
Branch feat/platformer-jump-buffer; baseline553a5d9, no main integration. Authorized task-branch push to origin with configured Git authentication.
Existing serial Release dotnet suites/native harness; memory mirror unavailable (no registered runtime identity). Delivery by coherent local units, no PR/release/force.

## Specs
S1: "La respuesta del personaje al movimiento es inmediata y la velocidad me resulta cómoda. Visualmente puedo distinguir perfectamente al personaje y el suelo." Preserve movement180, impulse-300, gravity900, geometry; record baseline feedback without inventing full-H1 acceptance.
S2: "La única sensación extraña aparece al intentar saltar justo cuando creo que estoy aterrizando. Visualmente parece que el personaje ya tocó el suelo, pero algunas pulsaciones no producen un salto." Reproduce a representative public-input trace; do not assert unobserved native timing or classify this as an engine defect.
S3: "No lo considero un bloqueo ni un fallo confirmado del motor, sino una observación de jugabilidad. Puedes continuar. Evalúa si conviene incorporar jump buffering y, si corresponde, coyote time como opciones configurables del controlador de plataformas."
S4: "No quiero convertir esos comportamientos en reglas obligatorias del motor ni afectar otros modelos de movimiento." Only consumer GameModel/example/test/docs surfaces; parameterless controller keeps buffer disabled, example explicitly opts into100ms. Coyote deferred: actual current game has no ledge/walk-off case. No engine API/dependency/backend/scope change.

## Tasks
T1 S1-S4 inline: ME-016 completed, PC-1 approved after independent jump_buffer_review designPASS; runtimeN/A designgate.
T2 S1-S4 inline: ME-017 optional buffer, consumer pause/reset bridge and public tests; runnable RED/GREEN, independent implementation verification; pending.
T3 S1-S4 inline: feedback/technical evidence/local commits/authorized push; pending.

## Log
L1 User request verbatim:
La respuesta del personaje al movimiento es inmediata y la velocidad me resulta cómoda. Visualmente puedo distinguir perfectamente al personaje y el suelo.
La única sensación extraña aparece al intentar saltar justo cuando creo que estoy aterrizando. Visualmente parece que el personaje ya tocó el suelo, pero algunas pulsaciones no producen un salto. Sospecho que presiono unas milésimas antes de que el motor registre el contacto.
No lo considero un bloqueo ni un fallo confirmado del motor, sino una observación de jugabilidad.
Puedes continuar. Evalúa si conviene incorporar jump buffering y, si corresponde, coyote time como opciones configurables del controlador de plataformas. No quiero convertir esos comportamientos en reglas obligatorias del motor ni afectar otros modelos de movimiento.
L2 Exploration: landing_input_map read-only CodeGraph/scratch trace found model ground-check before contact; press at60Hz tick39 is ignored then model lands, following empty tick remains grounded. Loop queues edges across zero-update frames and consumes once. Not an engine input-loss diagnosis. No ledges; coyote has no present validation case. No diagnostic-skill scaffolding/issue-tracker change needed for a scoped consumer feature; existing public deterministic seam is sufficient.
L3 Route: parent writes inline (no writer trigger). Shared example public model behavior/config changes require independent review. New consumer spec suffices under ADR-0003/0004; no significant engine architecture choice/new ADR. Forecast under400 authored lines before evidence, no PR prepared. Preserve .gitignore/.codegraph and existing76tests. New feature branch only, no main merge.

L4 2026-10-16: jump_buffer_review independent designPASS, no blockers; exact timing/boundary/reset/wrapper/headless tests approved, no engine ADR required. PC-1 accepted, ME-017ready.
