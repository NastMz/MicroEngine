# ARCH-1: Architectural renaissance — adversarial audit, industry research, target architecture

Objective: Phase I (done) audited every subsystem against the vision of a generalist, modular, multi-platform
2D/2.5D/3D engine, researched other engines and designed the target architecture; the owner approved it on 2026-11-02
with five amendments (`L4`-`L5`). **Phase II (reconstruction) is under way** under the approved plan in
[`docs/evaluations/arch-1.md`](../../docs/evaluations/arch-1.md) §9-§9.1; this file is its persistent register.
Governance (`L6`): the architect directs, reviews and merges; agents implement every unit through a pull request and
never merge their own; no NuGet publication, public or demo release, production site deployment, visibility,
secrets or permission change. Phase I branch `docs/arch-1` from `feat/asset-1` at 5706285; Phase II branches per unit.

## Specs

Quoted fragments are the owner's messages (2026-11-02), verbatim; the full texts are `L1` (request), `L4` (five
adjustments), `L5` (approval) and `L6` (Phase II protocol).

- S1 Adversarial audit: "Realiza una revisión adversarial de absolutamente toda la arquitectura actual." "Si
  diseñáramos MicroEngine desde cero, con nuestra experiencia actual y las lecciones de la industria, ¿volveríamos a
  tomar esta decisión?" "cada crítica y propuesta deberá respaldarse con evidencia técnica."
- S2 Industry research: "Godot." "Unity." "Unreal Engine." "MonoGame." "SDL y SDL_GPU." "Raylib." "Prioriza código
  fuente, documentos de diseño, decisiones arquitectónicas, propuestas técnicas, issues, pull requests, discusiones de
  mantenedores y experiencias documentadas." "diferencia lo que está públicamente documentado de lo que solo puede
  inferirse."
- S3 Subsystems: "Núcleo y ciclo de vida", "Renderizado y gráficos", "Input", "Audio", "Física", "Entidades, escenas y
  mundo", "Assets", "Plataformas", "SDK y herramientas"; "Identifica también subsistemas ausentes".
- S4 Logical and physical architecture: "Propón la estructura física y lógica que corresponda a la arquitectura
  objetivo, incluso si implica reorganizar completamente la solución." "No confundas crear carpetas con solucionar
  problemas de acoplamiento."
- S5 Implicit assumptions: "Busca sistemáticamente decisiones ocultas que el motor toma en nombre del desarrollador."
  "Los defaults plug-and-play siguen siendo un requisito fundamental."
- S6 Substitution: "Determina si las interfaces actuales representan contratos de sustitución reales o simples
  envoltorios de una implementación concreta."
- S7 Validation infrastructure: "Cuestiona especialmente la idoneidad de las comparaciones pixel-perfect como criterio
  universal de validación entre renderizadores y plataformas diferentes."
- S8 Evidence per finding: "1. Qué existe actualmente. 2. Qué problema representa. 3. Qué evidencia lo demuestra.
  4. Qué han aprendido otros proyectos. 5. Qué alternativas existen. 6. Qué solución propones. 7. Qué consecuencias
  tiene para MicroEngine."
- S9 Target architecture and dossier: "La propuesta debe distinguir claramente qué necesitamos implementar ahora, qué
  interfaces deben quedar preparadas y qué capacidades pueden incorporarse después." Dossier contents as listed in
  section 11 of `L1`.
- S10 Gate: "Al concluir esta fase, presenta el diseño para revisión y aprobación. No comiences la reconstrucción
  estructural de la Fase II sin esa aprobación."
- S11 Approved amendments (`L5`): "1. La nueva planificación de P1–P7, ejecutando cada prototipo antes de fijar el
  contrato o tomar la decisión que debe validar." "2. Un contrato gráfico único y completo, con recursos compartidos,
  capacidades explícitas y validación mediante prototipos de nivel 0 y nivel 1." "3. La eliminación obligatoria del
  adaptador heredado de Entity al finalizar la etapa 5, sin incorporarlo como API pública permanente." "4. Los nuevos
  criterios de cierre por etapa y una estrategia de validación basada en propiedades y tolerancias justificadas."
  "5. La definición de finalización de Fase II, incluyendo una prueba vertical 3D real y una demostración de
  sustitución del sistema físico." From `L4`: "Hay que ejecutar cada uno antes de tomar la decisión irreversible que
  pretende validar." "Mi condición: validar la arquitectura del dispositivo antes de estabilizar sus contratos, no
  después de construir dos backends sobre ellos." "No queremos terminar conservando dos modelos de entidades porque
  resultó cómodo durante la reconstrucción." "las tolerancias visuales deben depender de las propiedades evaluadas, no
  convertirse simplemente en referencias más permisivas." Integrated in dossier §6.3-§6.5, §7, §8, §9, §9.1.
- S12 Physics distinction (`L5`): "En la prueba de física, distingue las capacidades comunes entre el motor cinemático
  de tiles y Box2D de las capacidades específicas de cada implementación. No presupongas equivalencia funcional
  completa entre ambos." Dossier §9.1 table and both scenarios.
- S13 Definition of done (`L4`, `L6` §8): "Exigiría al menos una prueba vertical de renderizado 3D antes de afirmar que
  la arquitectura está preparada. No un juego 3D completo, pero sí una implementación que ejerza los contratos nuevos"
  "Continúa coordinando las etapas de la Fase II hasta cumplir los criterios de finalización establecidos en el
  dossier actualizado, incluidas las pruebas verticales 3D y de sustitución de física." "Entrega la reconstrucción
  integrada en `main`, sin publicar una nueva versión." Dossier §9.1.
- S14 PR flow (`L6` §1-§3): "Todas las modificaciones deberán ser realizadas por agentes delegados." "Cada unidad de
  implementación deberá entregarse mediante una PR." The agent must "Implementar su tarea.", "Ejecutar las pruebas
  correspondientes.", "Documentar los cambios.", "Publicar su rama.", "Abrir la PR.", "Corregir los problemas
  encontrados durante la revisión." "Los agentes implementadores no deben fusionar sus propias PRs."
- S15 Merge authorization and its limits (`L6` §4-§5): "tienes autorización expresa para aprobar y fusionar PRs a
  `main` sin solicitar aprobación individual del propietario." "Antes de fusionar, verifica que los cambios cumplen los
  criterios de aceptación correspondientes y que las pruebas relevantes han sido ejecutadas." "Mantén documentadas las
  incompatibilidades temporales, sus responsables y la etapa en la que deberán resolverse." "No omitas verificaciones
  arbitrariamente para acelerar merges." Not authorized: "Publicar nuevas versiones en NuGet." "Crear releases
  públicas." "Publicar nuevas versiones de las demos." "Desplegar versiones públicas del sitio." "Eliminar el
  repositorio o modificar su visibilidad." "Modificar secretos, credenciales o permisos administrativos." "Si los
  workflows habituales activan publicaciones o despliegues automáticamente después de un merge, coordina previamente
  su desactivación o aislamiento mediante agentes, sin comprometer la ejecución del CI de validación."
- S16 Register (`L6` §6): "Mantén un registro persistente y actualizado con:" "Estado de cada etapa." "Decisiones
  arquitectónicas aprobadas." "Contratos vigentes." "Agentes responsables." "Tareas pendientes." "Dependencias." "PRs
  abiertas y fusionadas." "Resultados de validación." "Problemas conocidos." "Bloqueos." "Próximos pasos." "La
  reconstrucción debe poder continuar coherentemente entre sesiones sin depender de recordar conversaciones extensas."
  This file's `## Register`.

## Tasks

Phase I tasks keep their IDs. Phase II IDs: `G` governance, `S<stage>-…` stage units, `P<n>` prototypes. Every Phase
II task is delegated to an agent and delivered as one pull request (S14); "unassigned" means no agent or branch yet.
A stage's tasks are checked against the stage closure criteria in dossier §9.

| ID | Specs | Route | Status | PR | Evidence |
| --- | --- | --- | --- | --- | --- |
| T1 | S1, S3-S6, S8 | delegated: 4 domain auditors (core/SDK, rendering/platform, input/audio/world, infrastructure) | done | — (in the Phase I PR) | annexes A1-A4, experiments in `docs/evaluations/arch-1/experiments/` |
| T2 | S2 | delegated: 3 researchers (rendering, engine core/physics/assets/audio/input, validation/packaging) | done | — (in the Phase I PR) | annexes R1-R3 (230+ sources) |
| T3 | S1-S9 | inline: cross-check, resolve contradictions, write `docs/evaluations/arch-1.md` | done | — (in the Phase I PR) | dossier; D1 and D4 spot-checked against source |
| T4 | S10 | inline: present for approval | done: approved 2026-11-02 with five amendments | — | `L4`, `L5` |
| T5 | S11-S13, S16 | delegated: documentation agent, branch `docs/arch-1-plan` | done | [#23](https://github.com/NastMz/MicroEngine/pull/23), 986adc6 | dossier amended (§6.3-§6.5, §7-§11, §9.1), annex notes, this register |
| T6 | S11, S16 | delegated: documentation agent, branch `docs/arch-1-prototype-verdicts` | done | [#29](https://github.com/NastMz/MicroEngine/pull/29), 32006ca | dossier §9 prototype verdicts; §6.4 graphics contract revised per directive 2 (16 points); directive 3 decisions in §5, §6.1-§6.5, §7, §9, §10, §11; this register |
| T7 | S11, S16 | delegated: registrar agent, branch `docs/arch-1-register-2` | PR open | [#41](https://github.com/NastMz/MicroEngine/pull/41) | directives 4-9 applied: dossier §6.4 (graphics contract v1 summary + link, `IAudioSink` v1, physics revisions), §9 verdicts (graphics spike, P5, P6, browser determinism), §5-§11; this register |
| G1 | S15 | delegated: another agent, branch `fix/phase-ii-deploy-freeze` | done | [#22](https://github.com/NastMz/MicroEngine/pull/22), 9a8020e | automatic public publication frozen for Phase II (`.github/phase-freeze`); validation CI keeps running |
| S0-D1 | S11 | delegated: stage 0 defects agent, branch `fix/arch-1-stage0-defects` | done | [#24](https://github.com/NastMz/MicroEngine/pull/24), 59fae72 | `ComposedPlatform` forwards every `IPlatform` member and exactly the backend's optional interfaces; tests RED first (since deleted by 1B) |
| S0-D2 | S11 | delegated: stage 0 CI agent, branch `fix/arch-1-stage0-ci` | done | [#27](https://github.com/NastMz/MicroEngine/pull/27), a89d031 | Windows WARP reference drift (eight entries of `sdl-windows-ci.json`) and the Debian 11 glibc check rule (`packaging/targets.mjs`, `glibcRefusal`) |
| S0-D3 | S11 | delegated: stage 0 validation agent, branch `feat/arch-1-stage0-validation` | done | [#30](https://github.com/NastMz/MicroEngine/pull/30), 5e1f8de | property checks on scripted captures (`tests/MeadowRun.Tests/CaptureProperties.cs`) with a sensitivity test; the fingerprint stays a pre-filter |
| S0-D4 | S11 | delegated: stage 0 defects agent | done | [#24](https://github.com/NastMz/MicroEngine/pull/24), 59fae72 | `HeadlessPlatform.UpdateRate`; `Now` equals `Game.Time` at any rate. Limitation: `new HeadlessPlatform()` without settings still runs at 60 |
| S0-D9 | S11 | delegated: stage 0 defects agent | done | [#24](https://github.com/NastMz/MicroEngine/pull/24), 59fae72 | one store per game, shared holds, disposal with the last holder; ADR 0025 amended (745221e) |
| S0-D11 | S11 | delegated: stage 0 defects agent; CI agent for `docs/ci.md` | partly done | [#24](https://github.com/NastMz/MicroEngine/pull/24), [#27](https://github.com/NastMz/MicroEngine/pull/27) | done in #24: `EntityList` clear during update, glyph filter docs match the code, `AssetGalleryTests` 30 s deadline; `docs/ci.md` drift fixed in #27; frozen-overlay blend resolved by stage 3 (dossier §6.5 Scene row); QWERTY prompts open, unassigned |
| S0-C1 | S11, S15 | delegated: stage 0 CI agent | done | [#27](https://github.com/NastMz/MicroEngine/pull/27), a89d031 | `validate.yml` nightly on `main` (03:17 UTC), publishes nothing |
| S0-C2 | S11 | delegated: stage 0 CI agent | done | [#27](https://github.com/NastMz/MicroEngine/pull/27), a89d031 | `web/tests/run.mjs` per game in `checks.yml` (Chrome under Xvfb) (D10); browser-vs-desktop frame comparison reported, not gating |
| S0-C3 | S11 | delegated: stage 0 CI agent | done | [#27](https://github.com/NastMz/MicroEngine/pull/27), a89d031 | `SdlKeyboardTests` and `SdlAudioTests` in the Native Linux job (D10) |
| S0-V1 | S11 | delegated: stage 0 validation agent, branch `feat/arch-1-stage0-validation` | done | [#30](https://github.com/NastMz/MicroEngine/pull/30), 5e1f8de | written rendering rules; `RendererConformance` (specification-tests pattern) on headless reference, every SDL driver and raylib; SDL and raylib brought to the rules (D5, stage 0 part) |
| S0-V2 | S11 | delegated: stage 0 validation agent | done | [#30](https://github.com/NastMz/MicroEngine/pull/30), 5e1f8de | recording renderer (backend-neutral command text) + CPU `ReferenceRasterizer` (`HeadlessPlatform.Rasterize`) |
| S0-V3 | S11 | delegated: stage 0 validation agent | done | [#30](https://github.com/NastMz/MicroEngine/pull/30), 5e1f8de | property-based checks beside the fingerprint (dossier §8 tolerance policy); exact cross-backend RGB equality now an informative report |
| S0-V4 | S11 | delegated, branches `fix/arch-1-gallery-deterministic-clock`, `fix/arch-1-gallery-scripted-hitch` | done | [#31](https://github.com/NastMz/MicroEngine/pull/31), 0ed8bcd; [#35](https://github.com/NastMz/MicroEngine/pull/35), 894eb73 | #31: no wall-clock times in scripted gallery runs; #35: the scripted tour loads its atlas before play (no hitch) and `samples/AssetGallery` selects the `sdk` check (planner fix after #31 broke `sdk/tests/external.mjs` on `main`) |
| S0-W1 | S11 | delegated, branch `fix/arch-1-browser-short-press-check` | done | [#36](https://github.com/NastMz/MicroEngine/pull/36), 495175a | the browser short-press check reads traced frames in one call (harness race, not an engine defect) |
| S0-W2 | S11 | delegated, branch `docs/arch-1-browser-determinism` | done | [#39](https://github.com/NastMz/MicroEngine/pull/39), 7270a74 | `experiments/arch-1/browser-determinism`: findings R1-R4, T0 mechanism proposal; robust browser launch in `site/tests` |
| T0 | S11 | delegated: T0 agent (directive 9) | in progress (no PR yet) | — | per-step state hash: `StateHasher`, `IStateHashed`, `Scene`/`Game.HashState`, `RunOptions.StateTrace`, `StateTrace.FirstDivergence`; drawing never changes the hash (R3); two deterministic runs per demo and the gallery give identical traces (every OS); cross-platform reported, not gating; closes stage 0 |
| P1 | S11 | delegated: prototype agent, `experiments/arch-1/p1-sdl-gpu-driver` | concluded: verdict accepted | [#26](https://github.com/NastMz/MicroEngine/pull/26), 479d3af | tier-0 SDL keeps SDL's default driver (dossier §9 P1 verdict); stage 2a entry gate met |
| P3 | S11 | delegated: prototype agent, `experiments/arch-1/p3-sdl-gpu-3d` | concluded: verdict accepted | [#25](https://github.com/NastMz/MicroEngine/pull/25), a5bad10 | 142/142 checks; graphics contract revised in 16 points (dossier §6.4, §9 P3 verdict) |
| P4 | S11 | delegated: prototype agent, `experiments/arch-1/p4-sdl3-web` | concluded: verdict accepted | [#28](https://github.com/NastMz/MicroEngine/pull/28), 3ef39c0 | SDL3 on .NET wasm; no integrated backends; SDL the single host; raylib web retired in stage 5 (dossier §9 P4 verdict) |
| S1-1A | S11 | delegated: stage 1 kernel agent, branches `feat/arch-1-stage1-kernel`, `feat/arch-1-stage1-kernel-loop` | done | [#33](https://github.com/NastMz/MicroEngine/pull/33), fa2d0c9; [#34](https://github.com/NastMz/MicroEngine/pull/34), 41094a0 | `MicroEngine.Kernel` (builder, typed registry, schedule, capabilities, log, math/colour types), loop-owned time and `Game` façade; architecture test; ADRs 0026, 0027 (directive 4) |
| S1-1B | S11 | delegated: stage 1 platform agent, branch `feat/arch-1-stage1-platform` | done | [#37](https://github.com/NastMz/MicroEngine/pull/37), da2df7f | `MicroEngine.Platform` (`IPlatformHost`, `ISurface` as frozen in graphics contract v1, `IStorage`, `IContentSource`); capability query everywhere; `ComposedPlatform` deleted; `FixedStep` removed; resize/focus raised by SDL, raylib, headless; platform conformance suite; ADR 0037 (directive 6) |
| S1-1C | S11 | delegated: stage 1 hosting agent (directive 9) | in progress (no PR yet) | — | `MicroEngine.Hosting`: one launcher model for desktop, browser, Android, iOS and tests; `Configure` entry point replacing `MicroEngineGameFactory`; no statics (`ContentFolder.DefaultRoot`, `SaveStore.DefaultStorage`); R1 (browser scripted runs honour the time mode, byte-identical captures across two runs); `--state-trace` flag; raylib stays the web host |
| S1-2 | S11 | delegated: graphics spike agent, `experiments/arch-1/graphics-spike` | concluded: contract frozen | [#32](https://github.com/NastMz/MicroEngine/pull/32), 1cd5c50 | tier-0 SDL_Renderer + tier-1 pure SDL_GPU adapters driving one batcher; verdict FREEZE WITH CHANGES; graphics contract v1 frozen (directive 5); SDL `3.5.0-ea7a2da` recorded |
| S2a | S11 | delegated (unassigned) | pending | — | graphics foundation implementing the frozen contract v1 in engine assemblies (built-in `Textured` pipeline); decides raylib desktop's future; raylib translucent-canvas darkening |
| S2b | S11 | delegated (unassigned) | pending | — | input; taps expire by updates, not by drawn frames |
| P5 | S11 | delegated: prototype agent, `experiments/arch-1/p5-audio-sinks` | concluded: verdict accepted | [#38](https://github.com/NastMz/MicroEngine/pull/38), cd6cde5 | `IAudioSink` v1 frozen (directive 7); feeder-thread default on SDL3 platforms, AudioWorklet sink in browsers, offline sink in tests |
| S2c | S11 | delegated (unassigned) | pending | — | audio and content; implements `IAudioSink` v1 and its default sinks; R2 (music rendering off the wall clock); hidden-page audio suspension |
| P6 | S11, S12 | delegated: prototype agent, `experiments/arch-1/p6-box2d` | concluded: verdict accepted | [#40](https://github.com/NastMz/MicroEngine/pull/40), 5dda331 | native Box2D v3.2.0 behind the engine's flat C layer; physics contract revised in eight points (directive 8) |
| P7 | S11 | delegated (unassigned), `experiments/` or draft PR | pending | — | gate: `IWorld` contract of stage 3 frozen |
| S3 | S11, S12 | delegated (unassigned) | pending | — | world, physics common contract (with P6's revisions) + capabilities, `Physics.Tiles`, transitional `Entity` adapter |
| S4 | S11 | delegated (unassigned) | pending | — | UI and modules |
| S5 | S11 | delegated (unassigned) | pending | — | consumers migrated; `Entity` adapter deleted; exclusion list empty; SDL browser launcher replaces raylib on the web behind the P4 gates (dossier §9) |
| P2 | S11, S13 | delegated (unassigned), `experiments/` or draft PR | pending | — | gate: tier-1 shader toolchain fixed (inside the 3D vertical slice); input from P3: HLSL, DXC package, SPIRV-Cross; risks: Windows-only DXC, unverified MSL |
| S6-1 | S13 | delegated (unassigned) | pending | — | 3D vertical slice on tier-1 pure SDL_GPU (dossier §9.1 item 2); CI check proven on Linux lavapipe first; measures E12's surface copy on mobile |
| S6-2 | S12, S13 | delegated (unassigned) | pending | — | native Box2D v3 backend, CI-built natives and optional package `MicroEngine.Physics.Box2D` (not published), and the physics substitution demonstration (§9.1 item 3) |
| S6-3 | S13 | delegated (unassigned) | pending | — | final report, integrated in `main`, no release (§9.1 item 4) |

Route evidence: T1/T2 fire the mapping trigger (the audit spans the whole repository and external sources, far beyond
the inline evidence budget); the owner explicitly asked for delegated, parallel, domain-specialised agents. Phase II:
every unit is delegated by the owner's protocol (S14).

## Register

Updated by agents at each merge, gate and stage closure (S16). Dates are ISO; PR numbers link to the pull request.
Last update: 2026-11-03 (T7).

### Stage status

| Stage | Status | Gates still open | Closure criteria met (dossier §9 a-e) |
| --- | --- | --- | --- |
| 0. Repair and measure | in progress: every unit merged (defects #24, CI #27, validation tooling #30, gallery #31 and #35, browser harness #36) except T0 (per-step state hash, directive 9), in progress; P1, P3, P4 concluded | T0 | closes when T0 merges and (a)-(e) are checked |
| 1. Kernel and composition | in progress: 1A merged (#33, #34), 1B merged (#37), 1C (hosting) in progress | — (graphics contract frozen, directive 5; platform host contract written, ADR 0037) | closes when 1C merges and (a)-(e) are checked |
| 2a. Graphics foundation | not started | — (P1 entry gate met; graphics contract v1 frozen) | — |
| 2b. Input | not started | — | — |
| 2c. Audio and content | not started | — (P5 concluded: `IAudioSink` v1 frozen) | — |
| 3. World and physics | not started | P7 (`IWorld`); P6 concluded (physics contract revisions in the dossier) | — |
| 4. UI and modules | not started | — | — |
| 5. Consumers | not started | `Entity` adapter deleted; exclusion list empty; raylib web retirement gates (P4) | — |
| 6. Phase II completion | not started | P2 (shader toolchain); lavapipe proven for SDL_GPU; Box2D natives validated on every target | — |

Prototypes: P1, P3, P4, P5, P6 concluded; P2, P7 open.

### Approved decisions

Approved by the owner on 2026-11-02 (dossier §7, §11). Each ADR is written and merged in the stage that implements
it; "written" records the merged ADR file.

| ADR | Decision | Stage | Written |
| --- | --- | --- | --- |
| 0026 | Kernel with modules, typed registry, phased schedule; `Game` façade | 1 | yes: [0026](../../docs/decisions/0026-kernel-modules-and-schedule.md) (#33) |
| 0027 | The loop owns time; engine interpolation with teleport; world clocks | 1, 3 | yes: [0027](../../docs/decisions/0027-loop-owned-time.md) (#34) for loop-owned time; interpolation and world clocks are stage 3 |
| 0028 | One complete graphics device contract with capabilities, built-in pipelines, typed capability error; tier 1 pure SDL_GPU (P3); frozen as v1 (directive 5) | 1 (spike), 2a | no (2a) |
| 0029 | `Presentation` replaces `CanvasScaling`; pixel art is a preset | 2a | no |
| 0030 | One object model; transitional `Entity` adapter deleted at the end of stage 5 | 3 | no |
| 0031 | Physics common contract + declared capabilities; tile backend first; native Box2D after P6, with P6's revisions (directive 8) | 3, 6 | no |
| 0032 | Input as device events with ids, players, layers | 2b | no |
| 0033 | One managed mixer; backends are PCM sinks; default SDL sink a feeder thread (directive 7) | 2c | no |
| 0034 | Content handles, loader registry, sources; optional build-time import | 2c | no |
| 0035 | Validation by contract: declared properties, justified tolerances, closure criteria, exclusion list | 0 | no |
| 0036 | Assemblies by boundary, architecture test; few packages, one version | 1 | no (the architecture test exists since #33) |
| 0037 | Platform host contract with capabilities, explicit options and no time coupling (amends 0016, 0027); numbered after the reserved 0028-0036 (directive 6) | 1 | yes: [0037](../../docs/decisions/0037-platform-host-contract.md) (#37) |

Existing ADR amended in Phase II: 0025 (asset pipeline), for scopes that share assets (#24, 745221e).

Architect decisions on prototype evidence (directives 2-9 of 2026-11-02 and 2026-11-03; dossier §9 prototype
verdicts, §11):

| Source | Decision | Applied in dossier |
| --- | --- | --- |
| P3 | Graphics contract revised in 16 points before its freeze: tier 1 pure SDL_GPU with its own built-in 2D pipelines; buffers on every tier; `TryAcquireSurfaceTarget`; fenced readback of offscreen targets; optional `DeviceLost`; pass attachments with load/store; target formats in pipelines; `UpdateBuffer`/`UpdateTexture`; stage-qualified `SetUniforms`; `Draw`/`DrawIndexed` + `IndexSize`; depth formats and `Supports(format, usage)`; creation-time shader validation; `ApiName` checked after creation; numeric conventions; shader toolchain input to P2; risks recorded | §6.4, §9, §10 |
| P1 | Tier-0 SDL keeps its default SDL_Renderer driver (Direct3D 11 on Windows; platform default elsewhere); only Windows measured | §5, §6.3, §6.5, §9 |
| P4 | No integrated-backend concept; SDL the single host for six platforms; raylib web retired in stage 5 behind gates; raylib desktop kept as the second tier-0 implementation until stage 2a decides; wasm P/Invoke workarounds in the engine's own declarations; page-driven host rules in the platform contract | §5, §6.1-§6.5, §9, §10 |
| Directive 4 | Stage 1 as three sequential units: 1A kernel (Kernel assembly bundled in the `MicroEngine` package, ADRs 0026/0027), 1B platform split, 1C hosting; a consumer that cannot keep building is reported, never dropped silently | §9 |
| Graphics spike (directive 5) | Graphics device contract v1 frozen: `experiments/arch-1/graphics-spike/Contract` with E1-E14; `Sprite` + `TexturedGeometry` → `Textured`; E12 surface copy accepted (mobile cost measured in the 3D slice); SDL software renderer a non-conformant diagnostic fallback; K-P1-1 closed for tier 1 | §6.4, §7, §8, §9, §10 |
| Directive 6 | 1B scope: `MicroEngine.Platform` with `ISurface` as frozen; capabilities by query; `ComposedPlatform` deleted; `FixedStep` removed from platform requests; resize/focus raised; platform ADR numbered after the reserved range | §6.3, §6.5, §7 |
| P5 (directive 7) | `IAudioSink` v1 frozen (pull, `SinkThread`/`HostPump`, `LatencyFrames`, `Start`, `Pump`); default sinks: SDL3 feeder thread (60 ms), browser AudioWorklet (100 ms, no SDL audio on the web), offline sink in tests; sounds start at step + delay ≥ latency; hidden-page suspension; ADR 0033 wording | §5, §6.3-§6.5, §7, §9, §10 |
| P6 (directive 8) | Native Box2D v3.2.0 behind the engine's flat C layer; physics contract revisions (a-h: skin tolerance in `Move`, kinematic contacts from `MoveResult`, exact `Overlap`, sensor filter rule, no managed callbacks during `Step`, `OneWayPlatforms`, determinism promise per backend build, box as the common character shape); natives in an optional package designed in stage 6, nothing published | §5, §6.3-§6.5, §7, §9, §9.1, §10 |
| Directive 9 | 1C: one composition path in `MicroEngine.Hosting`, `Configure` entry point, no statics, R1 fixed; T0: per-step state hash, run-to-run gating, cross-platform reported; browser determinism owners R1 → 1C, R2 → 2c, R3 → T0 design, R4 report-only | §6.5, §8, §9, §10 |

### Contracts in force

A contract is listed as frozen only after its gate passes and the freezing PR merges; later changes are versioned
revisions with evidence.

| Contract | Freeze gate | Status | Implemented in |
| --- | --- | --- | --- |
| Kernel (`EngineBuilder`, `ServiceRegistry`, `FrameSchedule`, `TimeSettings`, lifecycle events, `ILog`, capabilities) | stage 1A | in force on `main` (#33, #34; ADRs 0026, 0027) | `src/MicroEngine.Kernel` |
| Platform host (`IPlatformHost`, `ISurface`, `IStorage`, `IContentSource`, page-driven rules) | P4 concluded; stage 1B | in force on `main` per ADR 0037 (#37); hosting consolidation is 1C | `src/MicroEngine.Platform` |
| Graphics device contract v1 | graphics contract spike (S1-2) | **frozen** (directive 5; #32): `experiments/arch-1/graphics-spike/Contract` + E1-E14 + `Textured` | stage 2a (engine assemblies) |
| `IAudioSink` v1 | P5 | **frozen** (directive 7; #38): dossier §6.4 | stage 2c |
| `IPhysicsWorld2D` common contract and `PhysicsCapabilities` | P6 concluded; written in stage 3 | draft, revised by P6 in eight points (dossier §6.4, §9.1) | stage 3 (tiles), stage 6 (Box2D) |
| `IWorld` | P7 concluded; stage 3 | draft | stage 3 |
| Tier-1 shader toolchain | P2 concluded; inside the 3D vertical slice | draft (P3 input: HLSL, DXC, SPIRV-Cross) | stage 6 |

### Open PRs

| PR | Task | Branch | Owner | Notes |
| --- | --- | --- | --- | --- |
| [#41](https://github.com/NastMz/MicroEngine/pull/41) | T7 | `docs/arch-1-register-2` | registrar agent | this register and dossier update |
| — (not yet opened) | S1-1C | — | stage 1 hosting agent | in progress |
| — (not yet opened) | T0 | — | T0 agent | in progress |

### Merged PRs

| PR | Task | Merge commit | Merged | Content |
| --- | --- | --- | --- | --- |
| [#22](https://github.com/NastMz/MicroEngine/pull/22) | G1 | 9a8020e | 2026-11-02 | publication freeze for Phase II |
| [#23](https://github.com/NastMz/MicroEngine/pull/23) | T5 | 986adc6 | 2026-11-02 | dossier, annexes, approved amendments, this register |
| [#25](https://github.com/NastMz/MicroEngine/pull/25) | P3 | a5bad10 | 2026-11-02 | `experiments/arch-1/p3-sdl-gpu-3d` |
| [#24](https://github.com/NastMz/MicroEngine/pull/24) | S0-D1, S0-D4, S0-D9, S0-D11 (subset) | 59fae72 | 2026-11-02 | stage 0 defects + ADR 0025 amendment |
| [#26](https://github.com/NastMz/MicroEngine/pull/26) | P1 | 479d3af | 2026-11-02 | `experiments/arch-1/p1-sdl-gpu-driver` |
| [#28](https://github.com/NastMz/MicroEngine/pull/28) | P4 | 3ef39c0 | 2026-11-02 | `experiments/arch-1/p4-sdl3-web` |
| [#29](https://github.com/NastMz/MicroEngine/pull/29) | T6 | 32006ca | 2026-11-02 | prototype verdicts (P1, P3, P4) and the revised graphics contract in the dossier |
| [#30](https://github.com/NastMz/MicroEngine/pull/30) | S0-D3, S0-V1, S0-V2, S0-V3 | 5e1f8de | 2026-11-02 | written rendering rules, reference rasterizer, renderer conformance suite, capture properties |
| [#27](https://github.com/NastMz/MicroEngine/pull/27) | S0-D2, S0-C1, S0-C2, S0-C3, S0-D11 (`docs/ci.md`) | a89d031 | 2026-11-02 | release checks green, nightly validate, browser and SDL native suites in CI |
| [#31](https://github.com/NastMz/MicroEngine/pull/31) | S0-V4 | 0ed8bcd | 2026-11-02 | the asset gallery shows no wall-clock times in scripted runs |
| [#32](https://github.com/NastMz/MicroEngine/pull/32) | S1-2 | 1cd5c50 | 2026-11-02 | `experiments/arch-1/graphics-spike` (tier 0 SDL_Renderer, tier 1 SDL_GPU) |
| [#35](https://github.com/NastMz/MicroEngine/pull/35) | S0-V4 (follow-up) | 894eb73 | 2026-11-03 | the scripted gallery tour loads before play; `samples/AssetGallery` selects the `sdk` check |
| [#33](https://github.com/NastMz/MicroEngine/pull/33) | S1-1A (i) | fa2d0c9 | 2026-11-03 | `MicroEngine.Kernel`: builder, services, schedule, capabilities, log; ADR 0026 |
| [#34](https://github.com/NastMz/MicroEngine/pull/34) | S1-1A (ii) | 41094a0 | 2026-11-03 | loop-owned time and the `Game` façade; ADR 0027 |
| [#38](https://github.com/NastMz/MicroEngine/pull/38) | P5 | cd6cde5 | 2026-11-03 | `experiments/arch-1/p5-audio-sinks` |
| [#36](https://github.com/NastMz/MicroEngine/pull/36) | S0-W1 | 495175a | 2026-11-03 | browser short-press check reads traced frames in one call |
| [#39](https://github.com/NastMz/MicroEngine/pull/39) | S0-W2 | 7270a74 | 2026-11-03 | robust browser launch; `experiments/arch-1/browser-determinism` |
| [#40](https://github.com/NastMz/MicroEngine/pull/40) | P6 | 5dda331 | 2026-11-03 | `experiments/arch-1/p6-box2d` |
| [#37](https://github.com/NastMz/MicroEngine/pull/37) | S1-1B | da2df7f | 2026-11-03 | `MicroEngine.Platform`: host and surface contracts, capability query, no hidden time coupling; ADR 0037 |

Times are UTC merge times (`gh pr view N --json mergedAt`). Every pull request from #27 to #40 is merged; none is
closed unmerged.

### Temporary incompatibilities

Mirror of the checked-in exclusion list (dossier §9). Each entry: consumer, reason, owner, resolving stage. Stage 5
empties it.

| Consumer | Reason | Owner | Resolved by stage |
| --- | --- | --- | --- |
| — (empty; stages 1A and 1B kept every demo, the gallery and the samples building and tested) | | | |

### Validation results

Phase I evidence: dossier experiments (`docs/evaluations/arch-1/experiments/`). Phase II (as reported in each PR,
Windows unless stated):

| Source | Result |
| --- | --- |
| #24 (stage 0 defects) | 14 new or changed tests RED first; focused 63 passed; `dotnet test MicroEngine.slnx -c Release` all passed (295 + 1 skipped in MicroEngine.Tests); API reference, snippets and site build checks passed |
| P3 (#25) | 142/142 checks on D3D12 and Vulkan, NVIDIA and AMD; D3D12 vs Vulkan 0 differing pixels; NVIDIA vs AMD ±1 only (named quantization source); lavapipe/WARP not run |
| P1 (#26) | `gpu` driver: 138/138 demo captures exact on NVIDIA (D3D12, Vulkan); start-up 2006 → 2227/2326 ms; renderer creation 163 → 401/573 ms; peak commit 173 → 222/358 MB; D3D12 full screen waits for VSync when off; gallery captures non-deterministic (wall clock); Windows only |
| P4 (#28) | Meadow Run and Courtyard Key on `SdlPlatform` in Chrome and Firefox; Brotli download 2544 vs 2696 KB (trimmed); click to playing 727 vs 864 ms; equal frame time; two interop defects worked around |
| #27 (stage 0 CI) | 83 script tests passed (RED first for the glibc rule and planner changes); Windows captures of validate run 41515138843 12/12, 13/13, 26/26 pixel-identical against the updated WARP reference; final CI run 41542118967 success. Browser vs desktop frames measured, not gated: Trail Duel 87/87 identical; Meadow Run, Neon Rift and Courtyard Key frames differ (cause: browser determinism R1) |
| #30 (stage 0 validation) | 23 conformance tests RED on `main` code first; conformance 15/15 on headless reference, SDL default, direct3d11, direct3d12, opengl, vulkan, gpu, software and raylib; `dotnet test MicroEngine.slnx -c Release` all green (MicroEngine.Tests 314 + 1 skipped) |
| Graphics spike (#32) | conformance 125/125 (NVIDIA, five device configurations) and 50/50 (tier 1 on AMD); scenario 84/84 property checks; 2D scene byte-identical across tier 0 (D3D11) and tier 1 (D3D12, Vulkan) on NVIDIA and AMD; mutation testing caught 4/4 injected defects; ad hoc exceptions E1-E14 all resolved; SDL software renderer 18/240 coverage-rule violations |
| #33, #34 (stage 1A) | `dotnet test MicroEngine.slnx -c Release` all green (MicroEngine.Tests 356 then 371 passed, 1 skipped); `sdk/tests/packages.mjs` passed; `external.mjs` gallery check failed identically on `main` (the #31 regression, fixed by #35) |
| #35 | RED first (`TheScriptedTourReportsNoHitch`, planner tests); `dotnet test MicroEngine.slnx -c Release` all passed; `node sdk/tests/external.mjs` 46/46 PASS including the gallery tour |
| #37 (stage 1B) | `dotnet test MicroEngine.slnx -c Release` all green (MicroEngine.Tests 385 + 1 skipped; Desktop native 177 + 9 skipped); platform conformance headless 11/11, SDL 11/11, raylib 7/11 (4 skipped: GLFW cannot focus or close a hidden window from code; a desktop raylib window never suspends); packages and `external.mjs --targets windows` passed; mobile and web hosts not built locally (CI covers them) |
| P5 (#38) | Windows feeder (60 ms): nothing lost through 100 and 300 ms stalls and 53-60 ms collections, 64-71 ms latency; SDL get callback lost 23-73 ms on 50-90 ms collections; browser worklet (100 ms target) lost 7-17 ms on a 100 ms stall, nothing at 150 ms; SDL audio on the web dropped on every stall; offline output bit-identical on CoreCLR, Mono Android and the wasm interpreter; iOS not run |
| #39 (browser determinism) | with scripted runs fixed-step (F1, scratch copy): 138/138 captures byte-identical between two browser runs and browser vs desktop (one Windows machine), against 123/138 and 113/138 without; per-step state identical except R2 (music slices) and R4 (last-bit float differences) |
| P6 (#40) | 500 boxes per step: native v3.2.0 0.092 ms (Windows), 0.196 ms (Chrome interpreter), 0.794 ms (Android x86_64 emulator) vs Box2D.NET 0.563 / 23.2 / 6.42 ms; raw bindings abort on the wasm interpreter (8-byte structs by value); same native build bit-identical on Windows, Chrome and Android x86_64; Linux, macOS, iOS, arm64 not run |

### Known problems

Confirmed defects from the audit (dossier §3.1), all scheduled in stage 0 except D5-D8, which the redesign resolves:

| ID | Summary | Task | Status |
| --- | --- | --- | --- |
| D1 | `ComposedPlatform` drops display pacing, timer, graphics menu, diagnostics | S0-D1 | fixed (#24); `ComposedPlatform` deleted in 1B (#37) |
| D2 | Release-gating package checks red on `main`; not run daily | S0-D2, S0-C1 | fixed (#27) |
| D3 | Visual fingerprint passes real regressions, fails invisible shifts | S0-D3, S0-V3 | fixed (#30): property checks beside the fingerprint |
| D4 | Headless clock hard-codes 60 Hz | S0-D4 | fixed (#24); time loop-owned since 1A (#34) |
| D5 | Backends disagree (clear vs clip, sub-pixel rows, disposed textures, capture alpha) | S0-V1, S2a | stage 0 part fixed (#30: SDL and raylib meet the written rules); the rest with the new devices in 2a |
| D6 | `TileMotion` tunnels and embeds | S3 | open |
| D7 | Straight alpha fringes | S2a | open |
| D8 | Three mixers with three voice policies | S2c | open |
| D9 | `AssetCache.Unload` disposes shared textures; duplicate loads | S0-D9 | fixed (#24) |
| D10 | Browser suite not in CI; no SDL keyboard/audio-output test | S0-C2, S0-C3 | fixed (#27) |
| D11 | Smaller defects (`EntityList` clear, font filter docs, flaky wait, `docs/ci.md` drift, frozen-overlay blend, QWERTY prompts) | S0-D11 | four fixed (#24, #27); frozen-overlay blend by stage 3; QWERTY prompts open |

Found in Phase II:

| ID | Summary | Handling | Owner | Status |
| --- | --- | --- | --- | --- |
| K-P1-1 | SDL_Renderer's `gpu` driver on D3D12 ignores VSync off in desktop full screen (P1: 10 ms frames on a 100 Hz display) | Pure SDL_GPU with an explicit present mode runs unthrottled with Immediate (graphics spike); tier 0 keeps SDL's default driver. Not reported upstream (owner item) | S1-2 | closed for tier 1 (directive 5) |
| K-P4-1 | .NET wasm interpreter mis-calls P/Invokes with 64-bit enum parameters, `delegate* unmanaged` parameters (P4) and 8-byte structs passed by value (P6) | The engine's own P/Invoke declarations use `ulong` / `nint` for the affected SDL functions (SDL browser launcher, stage 5); the Box2D C layer passes only scalars and pointers (stage 6). Not reported upstream (owner item) | S5, S6-2 | open |
| R1 (determinism) | Browser scripted runs are real time (`BrowserLauncher` builds `new DesktopOptions()`): steps per frame and draw blend from the wall clock | One launcher model decides the time mode; browser test proves byte-identical captures across two runs | S1-1C | open (in progress) |
| R2 (determinism) | Music renders in wall-clock slices inside `Game.Step` in browsers; `CurrentMusic` depends on CPU speed | Logical music state independent of rendering; render outside the step | S2c | open |
| R3 (determinism) | Scene caches written while drawing | T0 hashes explicit contributions only; a test proves drawing never changes the hash | T0 | open (design) |
| R4 (determinism) | Cross-platform last-bit float differences (FMA in `Vector2.Lerp`, per-libm `MathF`) | Cross-platform identity reported, not gated; deterministic engine math later | — | report only |
| Tap expiry | `RaylibInput.CollectTaps` expires an unread tap after 8 drawn frames, not after an update: at about 1000 frames per second 22 of 40 presses lost on Meadow Run (CI draws at 60, not affected) | Taps expire by updates or time | S2b | open |
| raylib canvas alpha | raylib's canvas alpha follows OpenGL's default blend (0xBF after a 50 % draw) and the frame is presented with alpha blending over the letterbox, which may darken translucent areas on screen (not in captures) (#30) | New raylib tier-0 device on the frozen contract | S2a | open |
| Browser vs desktop frames | Meadow Run's level and map, Neon Rift's stage 1 and single Courtyard Key frames differ between browser and desktop (Meadow Run's camera about 3 px off) (#27) | Reported in the CI job summary, not a gate (pixel identity across backends is not a contract); cause R1 | S1-1C (R1) | report only |
| R-SDL-3.5 | `ppy.SDL3-CS` 2026.1002.1 bundles SDL 3.5.0 (`ea7a2da`), a development series; P1, P3, P4, P5 and the graphics spike ran on it | Exact version recorded by the spike; binding updates re-run the conformance suites; dossier §10 | — | open (risk) |
| R-SW-RAST | lavapipe/WARP for SDL_GPU unproven; SDL's software renderer fails the coverage rule (18/240 pixels) | The 3D slice's CI check must be proven on Linux lavapipe; `sdl-software` is a non-conformant diagnostic fallback (dossier §9.1, §10) | S6-1 | open (risk) |
| R-E12 | Tier 1's per-frame surface copy (E12) unmeasured on mobile | Measured in the 3D vertical slice | S6-1 | open (risk) |
| R-ETC2 | No ETC2 in SDL_GPU 3.5; ASTC unsupported on the measured desktop GPUs: Android compressed textures have no path | Per-format capability; decision in 2c/6 | S2c, S6-1 | open (risk) |
| R-AUDIO-DEV | Audio sinks unverified on iOS and real phones (Android emulator only), Safari and real browser audio devices | Recorded in dossier §10 | S2c | open (risk) |
| R-BOX2D-NATIVE | Box2D natives unverified on Linux, macOS, iOS and Android arm64; six native builds to own; wasm archives tied to the workload's Emscripten | Stage 6 designs and validates CI-built natives on every target | S6-2 | open (risk) |
| P1-gallery | The asset gallery's captures embedded wall-clock load times | S0-V4 | — | fixed (#31, #35) |

### Items for the owner

Outward publication is not authorised for agents or the architect (S15); these are listed for the owner to decide:

1. SDL upstream: SDL_Renderer's `gpu` driver on D3D12 ignores VSync off in desktop full screen (K-P1-1; evidence:
   `experiments/arch-1/p1-sdl-gpu-driver/results/fullscreen-summary.md`, SDL 3.5.0 `ea7a2da`). The engine no longer
   depends on it (closed for tier 1), but the upstream defect stands.
2. dotnet/runtime upstream: the wasm interpreter's P/Invoke defects with 64-bit enum parameters ("function signature
   mismatch"), `delegate* unmanaged` parameters (`mono_wasm_get_interp_to_native_trampoline`) (K-P4-1; evidence:
   `experiments/arch-1/p4-sdl3-web/README.md`, "Blockers found") and 8-byte structs passed by value (abort at
   `aot-runtime-wasm.c:188` in `mono_wasm_get_interp_to_native_trampoline`; evidence:
   `experiments/arch-1/p6-box2d/README.md`, "Raw binding probe").

### Process lessons

- PR #31 merged with only the Linux .NET checks: the CI planner treated `samples/` as a .NET input only, so the `sdk`
  check (`sdk/tests/external.mjs`, whose gallery tour #31 broke by waiting for a load during play) did not run, and
  `main` regressed (found while verifying #34, which reported the same failure on `main` at 1cd5c50). #35 fixed the gallery and the planner: `samples/AssetGallery/**` now selects
  `dotnet` and `sdk` and is an input of the `sdk` evidence key. Before merging, confirm that the checks a change needs
  actually ran, not only that the CI result is green.

### Blocks

- None. Stage 0 closes with T0; stage 1 closes with 1C; stages 2a, 2b and 2c can start once stage 1 closes (dossier
  §9 dependencies).

### Next steps

1. Review and merge 1C (hosting, R1 with its browser test, `--state-trace` flag) and T0 (per-step state hash, run-to-run
   traces for each demo and the gallery); then close stages 0 and 1 against criteria (a)-(e).
2. Stage 2a implements the frozen graphics contract v1 in engine assemblies (with the `Textured` merge and a version
   marker in the contract text), decides raylib desktop's future and fixes raylib's canvas alpha; 2b input (tap
   expiry by updates); 2c audio and content on `IAudioSink` v1 (R2, hidden-page suspension). 2a, 2b, 2c run in
   parallel.
3. Schedule P7 before stage 3's `IWorld` freeze; stage 3 writes the physics contract with P6's revisions; P2 inside
   the 3D vertical slice (stage 6).
4. Owner items above stay listed until the owner decides.

## Log

- L1 (2026-11-02) Owner's request, verbatim:

  > MicroEngine — Renacimiento Arquitectónico
  > Auditoría adversarial, investigación industrial, rediseño y reconstrucción fundacional
  >
  > Misión
  > MicroEngine ha alcanzado un punto de madurez en el que necesitamos reevaluar sus fundamentos arquitectónicos.
  > El proyecto nació resolviendo necesidades concretas de videojuegos 2D, especialmente pixel art. Sin embargo, nuestra visión es desarrollar un motor generalista, multiplataforma, extensible y modular, capaz de soportar videojuegos 2D, 2.5D y 3D.
  > No queremos que las decisiones tomadas durante las primeras implementaciones condicionen incorrectamente los próximos años de desarrollo.
  > Esta misión supone una revisión fundacional completa del motor, su SDK, sus herramientas, sus proyectos, sus demos y su infraestructura de validación.
  > Tu función es actuar como arquitecto principal, investigador técnico y orquestador de agentes especializados.
  > No debes abordar esta investigación como una tarea individual secuencial. Organiza y delega el trabajo entre los agentes y subagentes que consideres necesarios, con especialización por dominios, revisión cruzada e integración arquitectónica centralizada.
  > El trabajo se divide en dos fases principales, con una revisión y aprobación del diseño entre ambas.
  >
  > FASE I — AUDITORÍA ADVERSARIAL Y REDISEÑO
  >
  > 1. Auditoría crítica sin protección de la arquitectura existente
  > Realiza una revisión adversarial de absolutamente toda la arquitectura actual.
  > Adopta deliberadamente la perspectiva de un auditor que intenta encontrar las debilidades estructurales del sistema.
  > Ningún componente debe considerarse correcto simplemente porque:
  > * Funciona actualmente.
  > * Tiene pruebas automatizadas.
  > * Está bien implementado.
  > * Ya lo utilizan las cuatro demos.
  > * Cambiarlo sería costoso.
  > * Su diseño es sencillo.
  > * Fue una decisión arquitectónica anterior.
  > * Preservarlo evitaría romper compatibilidad.
  > Evalúa cada decisión considerando el motor que queremos construir, no únicamente los juegos que tenemos hoy.
  > La pregunta fundamental es:
  > Si diseñáramos MicroEngine desde cero, con nuestra experiencia actual y las lecciones de la industria, ¿volveríamos a tomar esta decisión?
  > Si la respuesta es negativa, identifica la alternativa apropiada.
  > La demolición de componentes existentes no constituye una restricción.
  > No obstante, cada crítica y propuesta deberá respaldarse con evidencia técnica. La intención adversarial es detectar errores de diseño, no inventarlos.
  >
  > 2. Investigación profunda de la industria
  > La auditoría no debe basarse exclusivamente en criterios propios.
  > Investiga cómo los motores y frameworks existentes han abordado problemas equivalentes, incluyendo:
  > * Godot.
  > * Unity.
  > * Unreal Engine.
  > * MonoGame.
  > * SDL y SDL_GPU.
  > * Raylib.
  > * Otros motores, frameworks, middleware y proyectos abiertos relevantes que descubras.
  > Estudia su arquitectura, implementación y evolución histórica.
  > Prioriza código fuente, documentos de diseño, decisiones arquitectónicas, propuestas técnicas, issues, pull requests, discusiones de mantenedores y experiencias documentadas.
  > Investiga especialmente:
  > * Qué problemas intentaron resolver.
  > * Qué diseños eligieron inicialmente.
  > * Qué errores descubrieron.
  > * Qué decisiones tuvieron que revertir.
  > * Qué subsistemas reconstruyeron.
  > * Qué limitaciones permanecen.
  > * Qué complejidades resultaron inevitables.
  > * Qué soluciones han demostrado funcionar.
  > * Qué lecciones podemos trasladar a MicroEngine.
  > No utilices otros motores como autoridad incuestionable.
  > Distingue sus requisitos particulares de las necesidades generales de un motor.
  > Cuando estudies implementaciones propietarias, diferencia lo que está públicamente documentado de lo que solo puede inferirse.
  > El objetivo es aprovechar conocimiento acumulado en la industria, no copiar superficialmente una arquitectura.
  >
  > 3. Auditoría integral de subsistemas
  > Revisa todos los subsistemas actuales y los límites entre ellos.
  > Incluye, como mínimo:
  > Núcleo y ciclo de vida
  > Inicialización, servicios, composición, dependencias, eventos, game loop, estado, configuración, extensibilidad, threading y gestión de recursos.
  > Renderizado y gráficos
  > Arquitectura de renderizado, SDL_Renderer, SDL_GPU, Raylib, APIs gráficas, dispositivos, shaders, materiales, texturas, primitivas, fuentes, cámaras, transformaciones, render targets, filtrado, antialiasing, resolución, escalado, renderizado 2D y futura incorporación de 3D.
  > Input
  > Teclado, ratón, gamepads, dispositivos simultáneos, múltiples jugadores, mapeo de acciones, touch, gestos, eventos, prioridades, ciclo de vida y abstracciones entre plataformas.
  > Audio
  > Dispositivos, mezcla, reproducción, streaming, síntesis, efectos, audio posicional, espacialización 3D, recursos y extensibilidad de backends.
  > Física
  > Revisa la ausencia o presencia real de contratos y subsistemas físicos propios del motor.
  > Investiga la integración de motores de física 2D y 3D, sus modelos de simulación, colisiones, transformaciones, eventos y relación con entidades y escenas.
  > Estudia implementaciones existentes como Box2D, Jolt y otras alternativas apropiadas.
  > Entidades, escenas y mundo
  > Escenas, jerarquías, entidades, componentes, transformaciones, sistemas, objetos de juego, ECS, ciclo de vida, serialización y coordinación con física, gráficos y audio.
  > No presupongas que ECS es obligatorio ni que el diseño actual debe conservarse.
  > Assets
  > Pipeline de importación, procesamiento, formatos, fuentes, imágenes, atlas, modelos 3D, materiales, shaders, cachés, referencias, streaming, empaquetado y distribución.
  > Plataformas
  > Ventanas, gráficos, audio, input, almacenamiento, archivos, ciclo de vida, dispositivos y adaptación a Windows, Linux, macOS, Android, iOS y web.
  > SDK y herramientas
  > API pública, configuración, defaults, composición de subsistemas, CLI, paquetes NuGet, plantillas, proyectos consumidores, extensibilidad y experiencia de desarrollo.
  > Identifica también subsistemas ausentes o responsabilidades no contempladas que resulten arquitectónicamente relevantes para la visión futura.
  >
  > 4. Arquitectura lógica y física
  > Reevalúa completamente la organización del código.
  > No asumas que las carpetas actuales `Core`, `Graphics`, `Assets` y equivalentes representan límites arquitectónicos adecuados.
  > Investiga:
  > * División de responsabilidades.
  > * Proyectos y assemblies.
  > * Namespaces.
  > * Dependencias entre módulos.
  > * Contratos públicos e internos.
  > * Separación entre abstracciones e implementaciones.
  > * Organización por características, dominios o capas.
  > * Ciclo de vida y composición de servicios.
  > * Dependencias opcionales.
  > * Límites de paquetes.
  > * Capacidad de sustituir implementaciones.
  > * Independencia de plataformas.
  > * Preparación para 2D y 3D.
  > Propón la estructura física y lógica que corresponda a la arquitectura objetivo, incluso si implica reorganizar completamente la solución.
  > No confundas crear carpetas con solucionar problemas de acoplamiento.
  >
  > 5. Supuestos implícitos y comportamientos mágicos
  > Busca sistemáticamente decisiones ocultas que el motor toma en nombre del desarrollador.
  > El problema identificado en ASSET-1, donde una opción de antialiasing determina implícitamente el filtrado de textura, es un ejemplo de lo que debemos investigar en todo el proyecto.
  > Identifica:
  > * Defaults implícitos.
  > * Configuraciones acopladas indebidamente.
  > * Restricciones heredadas del pixel art.
  > * Supuestos de un solo jugador.
  > * Supuestos exclusivamente 2D.
  > * Dependencias directas de backends.
  > * Comportamientos específicos de una demo incorporados al núcleo.
  > * Limitaciones injustificadas de formatos o resoluciones.
  > * Responsabilidades que deberían pertenecer a subsistemas independientes.
  > Los defaults plug-and-play siguen siendo un requisito fundamental.
  > La cuestión es que deben corresponder a decisiones coherentes, documentadas y sustituibles cuando corresponda.
  >
  > 6. Capacidad real de sustitución
  > Evalúa si MicroEngine cumple realmente su promesa de subsistemas reemplazables.
  > Investiga qué ocurre cuando un desarrollador desea sustituir:
  > * Renderer.
  > * API gráfica.
  > * Backend de ventanas.
  > * Motor físico.
  > * Sistema de audio.
  > * Gestor de recursos.
  > * Implementación de input.
  > * Otros subsistemas relevantes.
  > Determina si las interfaces actuales representan contratos de sustitución reales o simples envoltorios de una implementación concreta.
  > Analiza costes de integración, capacidades compartidas, características opcionales, compatibilidad entre módulos y gestión de fallos.
  >
  > 7. Auditoría de infraestructura y validación
  > Incluye toda la infraestructura que sostiene MicroEngine.
  > Reevalúa:
  > * Pruebas unitarias.
  > * Pruebas de integración.
  > * Pruebas funcionales.
  > * Pruebas de plataformas.
  > * Pruebas visuales.
  > * Comparación de imágenes.
  > * Validación temporal.
  > * Rendimiento.
  > * Pruebas de audio.
  > * Pruebas de input.
  > * CI/CD.
  > * Empaquetado y publicación.
  > * Demos como consumidores del SDK.
  > * Documentación y ejemplos.
  > Cuestiona especialmente la idoneidad de las comparaciones pixel-perfect como criterio universal de validación entre renderizadores y plataformas diferentes.
  > Investiga enfoques de comparación perceptual, tolerancias justificadas, invariantes geométricos, pruebas temporales y otros mecanismos existentes.
  > No conserves un método de validación únicamente porque ya está automatizado.
  > La infraestructura debe demostrar que los subsistemas cumplen sus contratos, no que producen accidentalmente una salida idéntica bajo todas las condiciones.
  >
  > 8. Investigación y pruebas arquitectónicas
  > No limites la auditoría a leer código y redactar opiniones.
  > Cuando sea necesario, desarrolla prototipos aislados, pruebas de concepto, modelos de dependencias, experimentos y comparativas.
  > Investiga código existente de otros motores para comprender cómo resolvieron problemas similares.
  > Distingue entre:
  > * Problemas confirmados.
  > * Riesgos arquitectónicos.
  > * Limitaciones de diseño.
  > * Especializaciones legítimas.
  > * Capacidades faltantes.
  > * Decisiones que requieren experimentación adicional.
  > Para cada hallazgo relevante, explica:
  > 1. Qué existe actualmente.
  > 2. Qué problema representa.
  > 3. Qué evidencia lo demuestra.
  > 4. Qué han aprendido otros proyectos.
  > 5. Qué alternativas existen.
  > 6. Qué solución propones.
  > 7. Qué consecuencias tiene para MicroEngine.
  >
  > 9. Orquestación de agentes
  > Utiliza los agentes especializados disponibles de manera activa.
  > Distribuye investigaciones y revisiones por dominios cuando resulte útil.
  > Cada agente debe producir hallazgos verificables, referencias y recomendaciones técnicas.
  > Realiza revisiones cruzadas entre dominios relacionados.
  > La responsabilidad de integrar los resultados y resolver contradicciones corresponde al arquitecto principal.
  > Evita que cada agente diseñe su subsistema ignorando las dependencias con los demás.
  > Organiza la investigación de manera que el trabajo pueda avanzar en paralelo y permita descubrir problemas no previstos inicialmente.
  >
  > 10. Diseño de la arquitectura objetivo
  > A partir de la auditoría, produce una propuesta arquitectónica integral.
  > Debe contemplar:
  > * Visión y principios arquitectónicos.
  > * Organización lógica.
  > * Organización física.
  > * Núcleo y composición de subsistemas.
  > * Contratos e interfaces.
  > * Extensibilidad.
  > * Separación entre plataforma y motor.
  > * Renderizado 2D y futura incorporación de 3D.
  > * Física 2D y 3D.
  > * Entidades y escenas.
  > * Audio.
  > * Input.
  > * Assets.
  > * SDK y herramientas.
  > * Estrategia de testing y CI.
  > * Dependencias externas.
  > * Capacidades opcionales.
  > * Estrategia multiplataforma.
  > La propuesta debe distinguir claramente qué necesitamos implementar ahora, qué interfaces deben quedar preparadas y qué capacidades pueden incorporarse después.
  > Pensar a largo plazo no implica implementar inmediatamente todas las funcionalidades de un motor 3D completo.
  > Sí implica evitar decisiones que hagan inviable o excesivamente costosa su incorporación posterior.
  >
  > 11. Resultado de la Fase I
  > Entrega un dossier arquitectónico completo que incluya:
  > * Inventario del sistema actual.
  > * Informe adversarial por subsistema.
  > * Investigación de la industria con fuentes verificables.
  > * Lecciones históricas aplicables.
  > * Mapa de dependencias y acoplamientos.
  > * Problemas y riesgos clasificados.
  > * Arquitectura objetivo.
  > * Propuesta de estructura de solución, proyectos y paquetes.
  > * Contratos fundamentales propuestos.
  > * Decisiones arquitectónicas y alternativas descartadas.
  > * Prototipos y evidencia experimental.
  > * Nueva estrategia de validación.
  > * Plan de reconstrucción organizado en etapas.
  > * Dependencias entre trabajos.
  > * Estrategia de integración y pruebas.
  > * Riesgos técnicos y costes arquitectónicos.
  > Al concluir esta fase, presenta el diseño para revisión y aprobación. No comiences la reconstrucción estructural de la Fase II sin esa aprobación.
  >
  > FASE II — RECONSTRUCCIÓN FUNDACIONAL
  > Esta fase comenzará después de aprobar la arquitectura objetivo.
  >
  > 12. Reconstrucción sin obligación de preservar la Alpha anterior
  > Implementa el diseño aprobado, incluso cuando requiera sustituir componentes, romper contratos públicos, reorganizar proyectos o modificar profundamente la infraestructura.
  > No conserves implementaciones incompatibles con la arquitectura objetivo únicamente por su antigüedad o por evitar cambios.
  > MicroEngine continúa en Alpha.
  > La estabilidad de APIs anteriores no tiene prioridad sobre la corrección arquitectónica del sistema futuro.
  >
  > 13. Ejecución coordinada
  > Divide la reconstrucción en trabajos especializados y ejecutables.
  > Utiliza subagentes para implementar subsistemas independientes cuando sea apropiado.
  > Establece contratos compartidos antes de desarrollar implementaciones que dependan de ellos.
  > Integra las piezas progresivamente y comprueba su interacción real.
  > Mantén una responsabilidad centralizada sobre la coherencia del motor.
  >
  > 14. Reconstrucción de herramientas e infraestructura
  > Actualiza junto con el motor:
  > * SDK y paquetes.
  > * CLI.
  > * Plantillas.
  > * Documentación.
  > * Benchmarks.
  > * Pruebas automatizadas.
  > * Pruebas visuales.
  > * CI/CD.
  > * Distribución multiplataforma.
  > * Demos y proyectos consumidores.
  > No asumas que la infraestructura anterior debe permanecer intacta.
  > Reconstrúyela donde el nuevo diseño lo requiera.
  >
  > 15. Validación de la nueva arquitectura
  > Utiliza las cuatro demos existentes, Asset Gallery y nuevos escenarios de integración para validar las capacidades del motor.
  > Estos proyectos deben actuar como consumidores y pruebas de la arquitectura, no como restricciones que determinen su diseño.
  > Evalúa tanto capacidades actuales como escenarios representativos de evolución futura.
  > Comprueba la modularidad real, la sustitución de subsistemas, el comportamiento multiplataforma y la coherencia de la experiencia del desarrollador.
  >
  > 16. Resultado de la Fase II
  > Entrega una arquitectura implementada, documentada, integrada y validada.
  > Organiza la reconstrucción en PRs o unidades de integración coherentes cuando sea apropiado.
  > Documenta decisiones, resultados, limitaciones y trabajos futuros.
  > No publiques versiones ni fusiones cambios a `main` sin autorización.
  >
  > PRINCIPIOS RECTORES
  > Visión antes que legado. La arquitectura se evalúa contra la visión futura de MicroEngine, no contra sus primeras demos.
  > Evidencia antes que opinión. Las decisiones relevantes deben estar fundamentadas en código, pruebas, experiencias de la industria o investigación técnica verificable.
  > Reutilización antes que reinvención. Investiga las implementaciones existentes, comprende sus decisiones internas y aprovecha el conocimiento acumulado.
  > Modularidad real. Los subsistemas deben tener responsabilidades y límites claros, con capacidad efectiva de sustitución.
  > Defaults sin encierro. MicroEngine debe funcionar con configuraciones predeterminadas apropiadas sin impedir personalización avanzada.
  > Complejidad justificada. No evites una arquitectura necesaria por ser compleja, pero tampoco reproduzcas complejidad ajena sin comprender su propósito.
  > Libertad para reconstruir. Ningún componente, carpeta, contrato o tecnología existente está protegido de una sustitución fundamentada.
  > Coherencia global. Las soluciones especializadas deben formar parte de una arquitectura integral.
  > Validación significativa. Las pruebas deben demostrar propiedades y contratos relevantes, no únicamente reproducir resultados históricos.
  > Preparación para 3D. El trabajo actual debe sostener una evolución real hacia videojuegos 2D, 2.5D y 3D.
  >
  > Instrucción final
  > No abordes esta misión como una refactorización incremental ordinaria.
  > Esta es una evaluación fundacional de MicroEngine, respaldada por investigación industrial y dirigida a definir la arquitectura sobre la que continuará evolucionando el proyecto.
  > Actúa como arquitecto principal y orquestador.
  > Investiga exhaustivamente, cuestiona lo existente, distribuye el trabajo, contrasta resultados, resuelve contradicciones y produce una arquitectura objetivo concreta.
  > Comienza por la Fase I. La Fase II queda condicionada a la aprobación del diseño arquitectónico.

- L2 (2026-11-02) Seven agents launched in parallel (T1, T2); reports land in the session scratchpad and are
  integrated into the dossier.
- L3 (2026-11-02) All seven reports received and kept as annexes under `docs/evaluations/arch-1/`. Contradictions
  resolved in the dossier (section 5): graphics contract vs implementation order, physics timing, entity model,
  package granularity, mixer, raylib's place, asset processing. Confirmed defects independent of the redesign listed
  as D1-D11 (stage 0). Next: the owner's review of sections 6, 7, 9 and 11; Phase II does not start without it.

- L4 (2026-11-02) The owner's five adjustments to the Phase I proposal, verbatim:

  > Cinco ajustes que que quiero que evalues de la propuesta antes de iniciar la reconstrucción
  > 1. Adelantar los prototipos que determinan contratos fundamentales
  > El plan coloca P1–P7 después de presentar las etapas, pero algunos experimentos pueden cambiar interfaces que se construirán antes.
  > Prototipo	Cuándo debe resolverse	Motivo
  > P5 — Salidas PCM web/móvil	Antes de cerrar la etapa 2c	Puede modificar IAudioSink
  > P7 — Entidades y componentes	Antes de cerrar el contrato de la etapa 3	Debe validar la ergonomía real de IWorld
  > P3 — GPU y composición 3D	Antes de congelar el contrato gráfico	Puede revelar limitaciones del dispositivo propuesto
  > P4 — SDL3 en navegador	Antes de consolidar definitivamente los hosts	Puede afectar la separación entre plataforma y dispositivo
  >
  > No hace falta ejecutar los siete experimentos antes de empezar. Hay que ejecutar cada uno antes de tomar la decisión irreversible que pretende validar. arch-1.md
  >
  > 2. Resolver una diferencia entre los dos contratos gráficos propuestos
  > En el anexo A2, IGraphicsDevice contempla buffers, shaders, pipelines y listas de comandos. En el contrato integrado del dossier, la interfaz expuesta es más reducida y no define expresamente todos esos mecanismos.
  >
  > Esa diferencia importa porque estamos diseñando la base gráfica para 2D y 3D.
  >
  > No necesitamos implementar todas las capacidades ahora, pero sí decidir si pertenecerán al mismo contrato, a extensiones consultables o a interfaces especializadas.
  >
  > Mi condición: validar la arquitectura del dispositivo antes de estabilizar sus contratos, no después de construir dos backends sobre ellos.
  > 3. No preservar Entity como compatibilidad por principio
  > La etapa 3 propone mantener Entity como una capa de compatibilidad temporal.
  >
  > Eso puede ser práctico para migrar las demos, pero no debe convertirse en una restricción del nuevo diseño.
  >
  > Aceptaría esa capa únicamente como herramienta transitoria, con una decisión explícita sobre su eliminación. No queremos terminar conservando dos modelos de entidades porque resultó cómodo durante la reconstrucción.
  > 4. Revisar las dependencias de la estrategia de testing
  > La nueva estrategia T0–T4 está mucho mejor fundamentada que las comparaciones actuales. Distingue determinismo, conformidad, comandos, apariencia y pruebas de dispositivos reales.
  >
  > Sin embargo, exigir que todas las demos estén funcionales al final de cada etapa podría imponer trabajo de adaptación repetitivo. La migración definitiva de consumidores está prevista en la etapa 5.
  >
  > Yo permitiría consumidores temporales o adaptadores durante la reconstrucción, siempre que cada etapa tenga pruebas significativas de sus propios contratos. No obligaría a migrar cuatro juegos completos varias veces para demostrar que el núcleo compila.
  >
  > Además, las tolerancias visuales deben depender de las propiedades evaluadas, no convertirse simplemente en referencias más permisivas. arch-1.md
  >
  > 5. Aclarar qué significa terminar la Fase II
  > La etapa 6 incluye SDL_GPU, shaders, una malla 3D, Box2D y la decisión sobre el host web, pero está marcada como «separadamente aprobada».
  >
  > Eso crea dos posibles criterios de finalización:
  > - Terminar la reconstrucción estructural hasta la etapa 5.
  > - Demostrar además que las nuevas abstracciones soportan capacidades avanzadas mediante los experimentos y prototipos de la etapa 6.
  > Para mí, la arquitectura no debería declararse validada para 3D solamente porque sus interfaces contienen Vector3, Quaternion y capacidades gráficas opcionales.
  >
  > Exigiría al menos una prueba vertical de renderizado 3D antes de afirmar que la arquitectura está preparada. No un juego 3D completo, pero sí una implementación que ejerza los contratos nuevos

- L5 (2026-11-02) The owner's approval, verbatim:

  > Apruebo los cinco ajustes propuestos para ARCH-1.
  > Actualiza el dossier, los anexos y el plan de reconstrucción donde corresponda, incorporando:
  >
  > 1. La nueva planificación de P1–P7, ejecutando cada prototipo antes de fijar el contrato o tomar la decisión que debe validar.
  > 2. Un contrato gráfico único y completo, con recursos compartidos, capacidades explícitas y validación mediante prototipos de nivel 0 y nivel 1.
  > 3. La eliminación obligatoria del adaptador heredado de Entity al finalizar la etapa 5, sin incorporarlo como API pública permanente.
  > 4. Los nuevos criterios de cierre por etapa y una estrategia de validación basada en propiedades y tolerancias justificadas.
  > 5. La definición de finalización de Fase II, incluyendo una prueba vertical 3D real y una demostración de sustitución del sistema físico.
  >
  > En la prueba de física, distingue las capacidades comunes entre el motor cinemático de tiles y Box2D de las capacidades específicas de cada implementación. No presupongas equivalencia funcional completa entre ambos.
  > Una vez actualizado y coherente el dossier, considero aprobada la arquitectura objetivo para iniciar la Fase II bajo este plan.

- L6 (2026-11-02) The owner's Phase II protocol, verbatim:

  > MicroEngine — Fase II: Protocolo de dirección y ejecución autónoma
  > Incorporamos las siguientes reglas obligatorias a la reconstrucción arquitectónica aprobada.
  > Estas instrucciones modifican expresamente el modelo de trabajo y las autorizaciones anteriores.
  > 1. Tu responsabilidad exclusiva: arquitecto y orquestador
  > Durante toda la Fase II, actuarás exclusivamente como arquitecto principal, director técnico, coordinador de agentes y responsable de integración.
  > No debes implementar ni modificar directamente código del motor, pruebas, herramientas, configuraciones, documentación o infraestructura.
  > Todas las modificaciones deberán ser realizadas por agentes delegados.
  > Tu responsabilidad consiste en mantener el contexto global del proyecto, planificar las etapas, definir contratos compartidos, distribuir tareas, gestionar dependencias, resolver contradicciones, revisar entregables y tomar decisiones de integración.
  > No te conviertas en implementador cuando un agente encuentre dificultades. Delega también las correcciones, investigaciones complementarias y tareas de integración.
  > Puedes consultar código, ejecutar verificaciones y utilizar las herramientas necesarias para supervisar el trabajo, pero no asumir directamente las tareas de implementación.
  > 2. Organización mediante agentes especializados
  > Utiliza tantos agentes y subagentes como resulte apropiado.
  > Paraleliza trabajos independientes para mejorar la velocidad de ejecución.
  > Cada agente debe recibir un alcance concreto, los contratos relevantes, sus dependencias y criterios de aceptación.
  > Evita que varios agentes modifiquen simultáneamente los mismos contratos fundamentales sin coordinación.
  > Cuando aparezcan incompatibilidades entre propuestas, resuélvelas desde la dirección arquitectónica y devuelve instrucciones claras a los agentes correspondientes.
  > Prioriza mantener una visión integral del sistema durante toda la reconstrucción.
  > 3. Flujo obligatorio mediante pull requests
  > Cada unidad de implementación deberá entregarse mediante una PR.
  > El agente responsable debe:
  >
  > * Implementar su tarea.
  > * Ejecutar las pruebas correspondientes.
  > * Documentar los cambios.
  > * Publicar su rama.
  > * Abrir la PR.
  > * Corregir los problemas encontrados durante la revisión.
  >
  > Tú serás responsable de revisar cada PR.
  > Evalúa su conformidad con la arquitectura aprobada, los contratos compartidos, las dependencias y los criterios de aceptación de la etapa.
  > Puedes aprobar, rechazar o solicitar correcciones.
  > Los agentes implementadores no deben fusionar sus propias PRs.
  > 4. Autorización excepcional para integrar a main
  > Durante esta Fase II tienes autorización expresa para aprobar y fusionar PRs a `main` sin solicitar aprobación individual del propietario.
  > Esta autorización sustituye temporalmente la restricción anterior que exigía autorización para cada merge.
  > Puedes realizar integraciones sucesivas conforme avancen las etapas y sus dependencias.
  > No necesitas detener la reconstrucción esperando una respuesta del propietario después de cada PR.
  > Antes de fusionar, verifica que los cambios cumplen los criterios de aceptación correspondientes y que las pruebas relevantes han sido ejecutadas.
  > Cuando una etapa permita expresamente dejar consumidores antiguos temporalmente incompatibles, evalúa el resultado según los contratos y pruebas de esa etapa, no según la obligación de que todas las demos funcionen continuamente.
  > Mantén documentadas las incompatibilidades temporales, sus responsables y la etapa en la que deberán resolverse.
  > No omitas verificaciones arbitrariamente para acelerar merges.
  > 5. Límites de la autorización
  > La autonomía concedida incluye:
  >
  > * Crear ramas.
  > * Delegar implementaciones.
  > * Abrir y revisar PRs.
  > * Solicitar y coordinar correcciones.
  > * Aprobar PRs.
  > * Fusionar cambios a `main`.
  > * Actualizar la arquitectura y documentación mediante agentes.
  > * Gestionar las dependencias entre etapas.
  >
  > No incluye autorización para:
  >
  > * Publicar nuevas versiones en NuGet.
  > * Crear releases públicas.
  > * Publicar nuevas versiones de las demos.
  > * Desplegar versiones públicas del sitio.
  > * Eliminar el repositorio o modificar su visibilidad.
  > * Modificar secretos, credenciales o permisos administrativos.
  >
  > La publicación y release final quedan reservadas al propietario.
  > Si los workflows habituales activan publicaciones o despliegues automáticamente después de un merge, coordina previamente su desactivación o aislamiento mediante agentes, sin comprometer la ejecución del CI de validación.
  > 6. Gestión del contexto y continuidad
  > Esta misión es extensa. Debes evitar que la coordinación dependa exclusivamente de tu ventana de contexto.
  > Mantén un registro persistente y actualizado con:
  >
  > * Estado de cada etapa.
  > * Decisiones arquitectónicas aprobadas.
  > * Contratos vigentes.
  > * Agentes responsables.
  > * Tareas pendientes.
  > * Dependencias.
  > * PRs abiertas y fusionadas.
  > * Resultados de validación.
  > * Problemas conocidos.
  > * Bloqueos.
  > * Próximos pasos.
  >
  > Actualiza ese registro mediante agentes cuando corresponda.
  > La reconstrucción debe poder continuar coherentemente entre sesiones sin depender de recordar conversaciones extensas.
  > 7. Control de calidad arquitectónica
  > No sacrifiques coherencia global por velocidad.
  > Cada integración debe acercar MicroEngine a la arquitectura objetivo aprobada.
  > Evita introducir nuevamente los acoplamientos, comportamientos implícitos y limitaciones identificados durante la auditoría adversarial.
  > Si una investigación o prototipo demuestra que una decisión aprobada debe cambiar, revisa el diseño, documenta la evidencia y coordina la modificación correspondiente.
  > No mantengas una decisión incorrecta únicamente porque ya fue aprobada anteriormente.
  > 8. Finalización de la reconstrucción
  > Continúa coordinando las etapas de la Fase II hasta cumplir los criterios de finalización establecidos en el dossier actualizado, incluidas las pruebas verticales 3D y de sustitución de física.
  > Una vez terminada la reconstrucción:
  >
  > * Presenta el estado final del motor.
  > * Resume las decisiones arquitectónicas efectivamente implementadas.
  > * Documenta las pruebas y plataformas verificadas.
  > * Identifica las capacidades pendientes.
  > * Confirma el estado de los paquetes, demos, CLI, documentación y CI.
  > * Entrega la reconstrucción integrada en `main`, sin publicar una nueva versión.
  >
  > El propietario realizará la validación personal y autorizará posteriormente las publicaciones y releases pertinentes.
  > Regla central: tú diriges, coordinas, revisas y fusionas. Tus agentes investigan, implementan, prueban y corrigen. La arquitectura y su coherencia son tu responsabilidad; la ejecución técnica se distribuye.

- L7 (2026-11-02) T5: the architect's directive 1 integrated the five amendments into the dossier in place:
  prototypes scheduled in the stage where they must conclude, each with its gate, P1/P3/P4 starting with stage 0
  (§9); the complete single `IGraphicsDevice` contract with built-in pipelines and `GraphicsCapabilityException`,
  frozen after a stage 1 spike on SDL_Renderer (tier 0) and SDL_GPU (tier 1, P3) (§6.4, ADR 0028 row); the `Entity`
  adapter as a transitional `[Obsolete]` non-packable project deleted at the end of stage 5 (§6.3, §6.5, ADR 0030
  row); stage closure criteria, the exclusion list and the tolerance policy (§8, §9); the Phase II definition of done
  with the 3D vertical slice and the physics common-contract/capability split (§9.1, ADR 0031 row); risks and
  "decisions taken" (§10, §11). Annexes A2 and A3 carry a superseded-points note. This file became the Phase II
  register (S11-S16, Phase II tasks, `## Register`). Next: G1 merges, then this PR; stage 0 and P1/P3/P4 start.
- L8 (2026-11-02) Merged since L7: #22 (G1, 9a8020e), #23 (T5, 986adc6), #25 (P3, a5bad10), #24 (stage 0 defects
  D1, D4, D9, D11 subset + ADR 0025 amendment, 59fae72), #26 (P1, 479d3af), #28 (P4, 3ef39c0). Open: #27 (stage 0 CI
  unit); the validation tooling unit (`feat/arch-1-stage0-validation`) is in progress with no PR yet. Prototypes P1,
  P3 and P4 concluded; the architect's directives 2 (P3: sixteen graphics contract revisions) and 3 (P1 and P4
  verdicts and decisions) were applied by T6 in the dossier: §9 prototype verdicts with evidence links and a Verdict
  column; §6.4 graphics contract revised (`TryAcquireSurfaceTarget`, fenced readback, optional `DeviceLost`, pass
  attachments with load/store, `UpdateBuffer`/`UpdateTexture`, stage-qualified `SetUniforms`, `Draw`/`DrawIndexed` +
  `IndexSize`, depth formats and `Supports(format, usage)`, buffers on every tier, creation-time shader validation,
  conventions); no integrated-backend concept, SDL the single host for six platforms, raylib web retired in stage 5
  behind gates, raylib desktop kept pending stage 2a, wasm P/Invoke workarounds, page-driven host rules (§5,
  §6.1-§6.5, §9, §10, §11); new risks: SDL 3.5.0 development series, D3D12 full-screen VSync (K-P1-1), wasm
  interpreter defects (K-P4-1), lavapipe/WARP for SDL_GPU unproven. New task S0-V4 (asset gallery deterministic
  clock). Owner items: SDL D3D12 VSync issue and dotnet/runtime wasm interpreter defects, not filed upstream. Next:
  stage 0 closes when the CI and validation PRs merge; then stage 1 (S1-1, then the S1-2 spike); P5, P6, P7 before
  their gates.
- L9 (2026-11-03) T7 brought the dossier and this register up to date with `main` and the architect's directives 4-9.
  Merged since L8 (all of #27-#40; none closed unmerged): #29 (T6, 32006ca), #30 (stage 0 validation tooling,
  5e1f8de), #27 (stage 0 CI, a89d031), #31 (gallery without wall-clock times, 0ed8bcd), #32 (graphics contract spike,
  1cd5c50), #35 (gallery loads before play; planner maps `samples/AssetGallery` to the `sdk` check, 894eb73), #33 and
  #34 (stage 1A kernel and loop-owned time, fa2d0c9, 41094a0; ADRs 0026, 0027), #38 (P5, cd6cde5), #36 (browser
  short-press harness, 495175a), #39 (browser determinism, 7270a74), #40 (P6, 5dda331), #37 (stage 1B platform host,
  da2df7f; ADR 0037). Decisions applied in the dossier: directive 4 (stage 1 as 1A, 1B, 1C); directive 5 (graphics
  device contract v1 frozen: the spike's `Contract/` with E1-E14, `Sprite` + `TexturedGeometry` → `Textured`, E12
  accepted, SDL software renderer non-conformant diagnostic, K-P1-1 closed for tier 1; §6.4 graphics part replaced by
  a summary and a link); directive 6 (1B scope; platform ADR 0037 after the reserved range); directive 7 (`IAudioSink`
  v1 frozen, feeder-thread default, AudioWorklet sink in browsers, ADR 0033 wording); directive 8 (native Box2D v3.2.0
  behind a flat C layer; physics contract revisions a-h in §6.4, §7, §9.1); directive 9 (1C hosting and T0 state hash;
  determinism owners R1 → 1C, R2 → 2c, R3 → T0 design, R4 report-only). Also recorded: tap expiry by updates → 2b;
  raylib translucent-canvas darkening → 2a (#30); browser-vs-desktop pixel differences report-only (#27); risks for
  iOS and real-device audio, Box2D natives on Linux/macOS/iOS/arm64, SDL 3.5.0, E12 copy cost on mobile, no ETC2 in
  SDL_GPU; owner item extended with the wasm interpreter's 8-byte struct by value defect; process lesson from #31.
  Stage status: stage 0 merged except T0 (in progress); stage 1A and 1B merged, 1C in progress; P1, P3, P4, P5, P6
  concluded; P2, P7 open; exclusion list empty. Discrepancies noted for the architect: the frozen contract text in
  `experiments/arch-1/graphics-spike/Contract` still declares `BuiltInPipeline { Sprite, TexturedGeometry }` and
  carries no version marker (the `Textured` merge and "v1" live in directive 5 and the dossier; stage 2a applies them
  when it moves the contract into engine assemblies). Next: merge 1C and T0, close stages 0 and 1; then 2a, 2b, 2c in
  parallel.
