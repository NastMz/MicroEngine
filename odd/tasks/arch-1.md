# ARCH-1: Architectural renaissance — adversarial audit, industry research, target architecture

Objective: Phase I (done) audited every subsystem against the vision of a generalist, modular, multi-platform
2D/2.5D/3D engine, researched other engines and designed the target architecture; the owner approved it on 2026-11-02
with five amendments (`L4`-`L5`). **Phase II (reconstruction) is complete on `main` (2026-11-05, c0ae530) and
unpublished**, under the approved plan in [`docs/evaluations/arch-1.md`](../../docs/evaluations/arch-1.md)
§9-§9.1; its final report is dossier §12. This file is its persistent register.
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

Phase I tasks keep their IDs. Phase II IDs: `G` governance, `S<stage>-…` stage units, `P<n>` prototypes, `X<n>`
cross-stage fixes. Every Phase
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
| T7 | S11, S16 | delegated: registrar agent, branch `docs/arch-1-register-2` | done | [#41](https://github.com/NastMz/MicroEngine/pull/41), 3aa486e | directives 4-9 applied: dossier §6.4 (graphics contract v1 summary + link, `IAudioSink` v1, physics revisions), §9 verdicts (graphics spike, P5, P6, browser determinism), §5-§11; this register |
| T8 | S11, S16 | delegated: registrar agent, branch `docs/arch-1-register-3` | done | [#55](https://github.com/NastMz/MicroEngine/pull/55), f08a3e3 | directives 9-12 applied: stages 0-2 closed, P7 verdict; dossier §6.4 (contract states, frozen `IWorld` v1 sketch, stage 2 notes), §8 (T0 OS precision), §9 (P7 verdict, stage 3 units, progress), §10 (two risks), §11; this register |
| T9 | S11, S16 | delegated: registrar agent, branch `docs/arch-1-register-4` | done | [#64](https://github.com/NastMz/MicroEngine/pull/64), 251f1e2 | directives 12-14 and #54-#61 applied: stage 3 closed, stages 4 and 6 started; dossier §6.3-§6.4 (implemented states), §9 (stage units, progress), §10 (risks), §11; this register |
| T10 | S11, S13, S16 | delegated: closing registrar agent, branch `docs/arch-1-close` | PR open | the closing PR | #62-#89 registered with merge commits and evidence; stages 4, 5 and 6 checked against their criteria; dossier §12 (final report, §9.1 item 4); ADRs 0030 and 0038 amended; migration notes: upgrade checklist; release-notes draft; SDL in the notices' zlib text |
| G1 | S15 | delegated: another agent, branch `fix/phase-ii-deploy-freeze` | done | [#22](https://github.com/NastMz/MicroEngine/pull/22), 9a8020e | automatic public publication frozen for Phase II (`.github/phase-freeze`); validation CI keeps running |
| S0-D1 | S11 | delegated: stage 0 defects agent, branch `fix/arch-1-stage0-defects` | done | [#24](https://github.com/NastMz/MicroEngine/pull/24), 59fae72 | `ComposedPlatform` forwards every `IPlatform` member and exactly the backend's optional interfaces; tests RED first (since deleted by 1B) |
| S0-D2 | S11 | delegated: stage 0 CI agent, branch `fix/arch-1-stage0-ci` | done | [#27](https://github.com/NastMz/MicroEngine/pull/27), a89d031 | Windows WARP reference drift (eight entries of `sdl-windows-ci.json`) and the Debian 11 glibc check rule (`packaging/targets.mjs`, `glibcRefusal`) |
| S0-D3 | S11 | delegated: stage 0 validation agent, branch `feat/arch-1-stage0-validation` | done | [#30](https://github.com/NastMz/MicroEngine/pull/30), 5e1f8de | property checks on scripted captures (`tests/MeadowRun.Tests/CaptureProperties.cs`) with a sensitivity test; the fingerprint stays a pre-filter |
| S0-D4 | S11 | delegated: stage 0 defects agent | done | [#24](https://github.com/NastMz/MicroEngine/pull/24), 59fae72 | `HeadlessPlatform.UpdateRate`; `Now` equals `Game.Time` at any rate. Limitation: `new HeadlessPlatform()` without settings still runs at 60 |
| S0-D9 | S11 | delegated: stage 0 defects agent | done | [#24](https://github.com/NastMz/MicroEngine/pull/24), 59fae72 | one store per game, shared holds, disposal with the last holder; ADR 0025 amended (745221e) |
| S0-D11 | S11 | delegated: stage 0 defects agent; CI agent for `docs/ci.md` | partly done | [#24](https://github.com/NastMz/MicroEngine/pull/24), [#27](https://github.com/NastMz/MicroEngine/pull/27) | done in #24: `EntityList` clear during update, glyph filter docs match the code, `AssetGalleryTests` 30 s deadline; `docs/ci.md` drift fixed in #27; frozen-overlay blend resolved by stage 3 (3a-2; dossier §6.5 Scene row); QWERTY prompts fixed by 2b ([#49](https://github.com/NastMz/MicroEngine/pull/49): prompts from the keyboard layout on SDL; raylib keeps US names) |
| S0-C1 | S11, S15 | delegated: stage 0 CI agent | done | [#27](https://github.com/NastMz/MicroEngine/pull/27), a89d031 | `validate.yml` nightly on `main` (03:17 UTC), publishes nothing |
| S0-C2 | S11 | delegated: stage 0 CI agent | done | [#27](https://github.com/NastMz/MicroEngine/pull/27), a89d031 | `web/tests/run.mjs` per game in `checks.yml` (Chrome under Xvfb) (D10); browser-vs-desktop frame comparison reported, not gating |
| S0-C3 | S11 | delegated: stage 0 CI agent | done | [#27](https://github.com/NastMz/MicroEngine/pull/27), a89d031 | `SdlKeyboardTests` and `SdlAudioTests` in the Native Linux job (D10) |
| S0-V1 | S11 | delegated: stage 0 validation agent, branch `feat/arch-1-stage0-validation` | done | [#30](https://github.com/NastMz/MicroEngine/pull/30), 5e1f8de | written rendering rules; `RendererConformance` (specification-tests pattern) on headless reference, every SDL driver and raylib; SDL and raylib brought to the rules (D5, stage 0 part) |
| S0-V2 | S11 | delegated: stage 0 validation agent | done | [#30](https://github.com/NastMz/MicroEngine/pull/30), 5e1f8de | recording renderer (backend-neutral command text) + CPU `ReferenceRasterizer` (`HeadlessPlatform.Rasterize`) |
| S0-V3 | S11 | delegated: stage 0 validation agent | done | [#30](https://github.com/NastMz/MicroEngine/pull/30), 5e1f8de | property-based checks beside the fingerprint (dossier §8 tolerance policy); exact cross-backend RGB equality now an informative report |
| S0-V4 | S11 | delegated, branches `fix/arch-1-gallery-deterministic-clock`, `fix/arch-1-gallery-scripted-hitch` | done | [#31](https://github.com/NastMz/MicroEngine/pull/31), 0ed8bcd; [#35](https://github.com/NastMz/MicroEngine/pull/35), 894eb73 | #31: no wall-clock times in scripted gallery runs; #35: the scripted tour loads its atlas before play (no hitch) and `samples/AssetGallery` selects the `sdk` check (planner fix after #31 broke `sdk/tests/external.mjs` on `main`) |
| S0-W1 | S11 | delegated, branch `fix/arch-1-browser-short-press-check` | done | [#36](https://github.com/NastMz/MicroEngine/pull/36), 495175a | the browser short-press check reads traced frames in one call (harness race, not an engine defect) |
| S0-W2 | S11 | delegated, branch `docs/arch-1-browser-determinism` | done | [#39](https://github.com/NastMz/MicroEngine/pull/39), 7270a74 | `experiments/arch-1/browser-determinism`: findings R1-R4, T0 mechanism proposal; robust browser launch in `site/tests` |
| T0 | S11 | delegated: T0 agent (directive 9), branch `feat/arch-1-t0-state-hash` | done | [#42](https://github.com/NastMz/MicroEngine/pull/42), 4dc5ada | per-step state hash: `StateHasher`, `IStateHashed`, `SeededRandom`, `Scene`/`Game.HashState`, `RunOptions.StateTrace`, `StateTrace.FirstDivergence`; drawing never changes the hash (R3); two deterministic runs per demo scenario and the gallery tour give identical traces; gate on Linux for every change and on Windows/macOS when engine or shared inputs change (documented precision of the dossier's "every OS"); cross-platform reported, not gating; closed stage 0. Follow-up (stage 5): starting points for the touch, boss, tour and catch scripts |
| P1 | S11 | delegated: prototype agent, `experiments/arch-1/p1-sdl-gpu-driver` | concluded: verdict accepted | [#26](https://github.com/NastMz/MicroEngine/pull/26), 479d3af | tier-0 SDL keeps SDL's default driver (dossier §9 P1 verdict); stage 2a entry gate met |
| P3 | S11 | delegated: prototype agent, `experiments/arch-1/p3-sdl-gpu-3d` | concluded: verdict accepted | [#25](https://github.com/NastMz/MicroEngine/pull/25), a5bad10 | 142/142 checks; graphics contract revised in 16 points (dossier §6.4, §9 P3 verdict) |
| P4 | S11 | delegated: prototype agent, `experiments/arch-1/p4-sdl3-web` | concluded: verdict accepted | [#28](https://github.com/NastMz/MicroEngine/pull/28), 3ef39c0 | SDL3 on .NET wasm; no integrated backends; SDL the single host; raylib web retired in stage 5 (dossier §9 P4 verdict) |
| S1-1A | S11 | delegated: stage 1 kernel agent, branches `feat/arch-1-stage1-kernel`, `feat/arch-1-stage1-kernel-loop` | done | [#33](https://github.com/NastMz/MicroEngine/pull/33), fa2d0c9; [#34](https://github.com/NastMz/MicroEngine/pull/34), 41094a0 | `MicroEngine.Kernel` (builder, typed registry, schedule, capabilities, log, math/colour types), loop-owned time and `Game` façade; architecture test; ADRs 0026, 0027 (directive 4) |
| S1-1B | S11 | delegated: stage 1 platform agent, branch `feat/arch-1-stage1-platform` | done | [#37](https://github.com/NastMz/MicroEngine/pull/37), da2df7f | `MicroEngine.Platform` (`IPlatformHost`, `ISurface` as frozen in graphics contract v1, `IStorage`, `IContentSource`); capability query everywhere; `ComposedPlatform` deleted; `FixedStep` removed; resize/focus raised by SDL, raylib, headless; platform conformance suite; ADR 0037 (directive 6) |
| S1-1C | S11 | delegated: stage 1 hosting agent (directive 9), branch `feat/arch-1-stage1-hosting` | done | [#43](https://github.com/NastMz/MicroEngine/pull/43), 67d340e | `MicroEngine.Hosting` + `GameHost`: one launch path for desktop, browser, Android, iOS and tests; `Configure` entry point (`MicroEngineGameFactory` deprecated); no statics (`ContentFolder.DefaultRoot` a read-only rule, `SaveStore.DefaultStorage` a method, `BrowserPage`); R1 fixed (browser scripted runs deterministic, byte-identical across two runs); `--state-trace`; raylib stays the web host; ADR 0038. Follow-ups: GPU/window/audio options carried as CLI words until modules split; phone `--env` read from the process environment by demos (stage 5); no web/phone trace export (declined) |
| S1-2 | S11 | delegated: graphics spike agent, `experiments/arch-1/graphics-spike` | concluded: contract frozen | [#32](https://github.com/NastMz/MicroEngine/pull/32), 1cd5c50 | tier-0 SDL_Renderer + tier-1 pure SDL_GPU adapters driving one batcher; verdict FREEZE WITH CHANGES; graphics contract v1 frozen (directive 5); SDL `3.5.0-ea7a2da` recorded |
| S2a | S11 | delegated: stage 2a graphics agent (directive 10), branches `feat/arch-1-stage2a-graphics-1`..`-3` (chained) | done | [#45](https://github.com/NastMz/MicroEngine/pull/45), 85bf978; [#48](https://github.com/NastMz/MicroEngine/pull/48), 1711c34; [#52](https://github.com/NastMz/MicroEngine/pull/52), 046a02c | #45 contract v1 in `MicroEngine.Graphics` (`Version = 1`, `Textured`), reference device, conformance suite, counters, ADR 0028; #48 tier-0 SDL_Renderer and raylib devices, conformance on every SDL driver and raylib, `Presentation` + presets, ADR 0029; #52 `MicroEngine.Graphics2D` batcher, `IRenderer` façade over the device, old SDL/raylib renderers deleted, `Camera2D` zoom/rotation, pixel snap per preset (`CameraPixelSnap`), premultiplied-alpha rule changes for translucent `Multiply`/`Clear`, raylib canvas darkening gone, SDL vs raylib captures within 3/255. Follow-ups: F11 action (S2a-F11, #54), views hook, `Aspect.Expand` and font pages following `DefaultSampler` (S2a-V, #61); open: tier-1 adapter port (stage 6a) |
| S2a-F11 | S11 | delegated, branch `feat/arch-1-fullscreen-action` | done | [#54](https://github.com/NastMz/MicroEngine/pull/54), 4b1d769 | engine action `fullscreen` (`ActionNames.Fullscreen`), F11 and Alt+Enter by default through `KeyChord`; system `engine.fullscreen`; the SDL and raylib backends no longer hard-wire the hotkeys; chord precedence is general to the input map (a key pressed while a modifier of one of its chords is held belongs to the chord until released) |
| S2a-V | S11 | delegated: graphics agent (directive 13), branch `feat/arch-1-views` | done | [#61](https://github.com/NastMz/MicroEngine/pull/61), 4ff6857 | views hook: UI view at screen resolution composited over the world view; `Scene.DrawUi`, `Scene.UiContains`, `Scene.WorldCamera`, `Game.Views`, `IScreenView`, `Presentation.UiScale`, `AspectMode.Expand`, font pages follow `DefaultSampler`. Carried to 4a by the architect: captures composite the UI view; `UiScale` default = surface density |
| X1 | S11 | delegated, branch `fix/arch-1-allocation-tests` | done | [#58](https://github.com/NastMz/MicroEngine/pull/58), 75b191f | root cause: background-GC allocation-buffer accounting (a one-off jump of about 8 KB on the measuring thread); `AllocationAssert` (warm-up, then at least one round of 0 bytes); frame loop judged by the median frame. Follow-ups: audio feeder thread unmeasured; frame loop 240 B per frame vs the 256 B limit; timing flake `Loads_during_play_are_counted_and_slow_ones_reported_once` |
| S2b | S11 | delegated: stage 2b input agent (directive 10), branches `feat/arch-1-stage2b-input`, `-input-1`, `-input-2` (chained) | done | [#46](https://github.com/NastMz/MicroEngine/pull/46), c2f5f97; [#49](https://github.com/NastMz/MicroEngine/pull/49), d897a09; [#51](https://github.com/NastMz/MicroEngine/pull/51), 7c7a157 | #46 `MicroEngine.Input` model (device events, players, layers, text, rebinding, action names, taps expire by updates), ADR 0032 with `IInputBackend` in `MicroEngine.Input` as an open point, API reference lists a namespace's types in name order (d5c93d9); #49 SDL and raylib event backends, every gamepad, layout key labels, input backend conformance; #51 scripted device events, widgets consume presses, two-player test |
| P5 | S11 | delegated: prototype agent, `experiments/arch-1/p5-audio-sinks` | concluded: verdict accepted | [#38](https://github.com/NastMz/MicroEngine/pull/38), cd6cde5 | `IAudioSink` v1 frozen (directive 7); feeder-thread default on SDL3 platforms, AudioWorklet sink in browsers, offline sink in tests |
| S2c | S11 | delegated: stage 2c audio and content agent (directive 10), branches `feat/arch-1-stage2c-audio-content-1`..`-3` | done | [#44](https://github.com/NastMz/MicroEngine/pull/44), 09f9564; [#47](https://github.com/NastMz/MicroEngine/pull/47), 50502e3; [#50](https://github.com/NastMz/MicroEngine/pull/50), f1f04e0 | #44 `MicroEngine.Content` (store, `Asset<T>`, loader registry, zip and listed sources, audio bytes as kinds), ADR 0034; #47 `MicroEngine.Audio`: one managed mixer over `IAudioSink` v1 (in `MicroEngine.Platform`), SDL feeder / raylib feeder / AudioWorklet / offline sinks, `VoiceHandle`, voice policy (D8), R2 fixed, hidden page suspends audio, NVorbis in the `MicroEngine` package + notices, ADR 0033; #50 sounds and music as assets, `Synth`/`Song` in a `Retro` folder (namespace move deferred to stage 5). Risk: streamed music decoded 0.5 s ahead |
| P6 | S11, S12 | delegated: prototype agent, `experiments/arch-1/p6-box2d` | concluded: verdict accepted | [#40](https://github.com/NastMz/MicroEngine/pull/40), 5dda331 | native Box2D v3.2.0 behind the engine's flat C layer; physics contract revised in eight points (directive 8) |
| P7 | S11 | delegated: prototype agent, `experiments/arch-1/p7-entities` | concluded: verdict accepted | [#53](https://github.com/NastMz/MicroEngine/pull/53), d2191cc | Courtyard Key port: bit-identical traces on 11 scripts × 2 storages, 47 game tests, 12 helpers removed; `IWorld` v1 frozen with corrections (directive 11); gate met |
| S3a | S11, S12 | delegated: stage 3a agent (directive 12), branches `feat/arch-1-stage3a-scene-1`..`-3` (chained) | done | [#56](https://github.com/NastMz/MicroEngine/pull/56), 9b7022c; [#57](https://github.com/NastMz/MicroEngine/pull/57), 4705135; [#59](https://github.com/NastMz/MicroEngine/pull/59), d5515b2 | #56 `MicroEngine.Scene` (namespace `MicroEngine.Scenes`, so the `Scene` class is not hidden): `EntityId`, `Transform`, `IWorld`/`SceneWorld` per P7 + directive 11, conformance suite (38 tests: every v1 rule of P7's 44 cases on one storage, plus new rules), T0 hashing, ADR 0030; #57 scenes own worlds (`Scene.Objects`, not `Scene.World`, which broke two consumers), engine interpolation and `Teleport`, world-owned blend from per-scene step tracking (D11/SC-2 fixed), world clocks (SC-3), scene results (SC-1); the views hook was not possible there (moved to S2a-V); #59 transitional adapter `MicroEngine.Migration.Entities` (non-packable, `[Obsolete]` `MEMIG001`, demos only, architecture test), `PlatformerExample` without `Entity`, docs label `Entity` "removed in the next SDK version", ADR 0003 amended by 0030. Closed stage 3a |
| S3b | S11, S12 | delegated: stage 3b agent (directive 12), branch `feat/arch-1-stage3b-physics-1` | done | [#60](https://github.com/NastMz/MicroEngine/pull/60), e39d8bb | `MicroEngine.Physics` (`IPhysicsWorld2D`, `PhysicsContract.Version = 1`, `PhysicsCapabilities`, `PhysicsCapabilityException`, P6 revisions a-h, `PhysicsModule` in `FramePhase.Physics`), common conformance suite with a `Scale` override, capability suites; `MicroEngine.Physics.Tiles` (exact swept mover, D6 fixed with no T0 trace change, `TileGeometry`, `OneWayPlatforms`, `MovingSolids`); tile types moved with type forwards from `MicroEngine`; ADR 0031. Follow-ups: no broad phase (0.75 ms/step for 200 boxes; stage 6b); `MoveResult` name clash between `MicroEngine.World` and `MicroEngine.Physics` (stage 5 decides `TileMotion`'s future). Closed stage 3 |
| S4a | S11 | delegated: stage 4a agent (directive 14), chained PRs | done | [#66](https://github.com/NastMz/MicroEngine/pull/66), 3fee335; [#70](https://github.com/NastMz/MicroEngine/pull/70), faca750; [#74](https://github.com/NastMz/MicroEngine/pull/74), 5af531a; [#75](https://github.com/NastMz/MicroEngine/pull/75), 608f470 | 4a-0 #66 the 2D façade model moves to `MicroEngine.Graphics2D` with forwards (27 forwarding tests, RED first); 4a-1 #70 `MicroEngine.Text` (fonts, FontStashSharp, the font loader under kind "font"); 4a-2 #74 `MicroEngine.UI` (widget tree, highest input layer, renamable widget actions, text field with IME, captures composite the UI view; ADR 0044); #75 `UiScale` defaults to the surface density (`float?`, `UiScaleFor`) |
| S4b | S11 | delegated: stage 4b agent (directive 14) | done | [#65](https://github.com/NastMz/MicroEngine/pull/65), 427fa4e; [#68](https://github.com/NastMz/MicroEngine/pull/68), 70b385e; [#76](https://github.com/NastMz/MicroEngine/pull/76), 80e7bf6 | #65 `MicroEngine.Saves`, `SavesModule`, `AchievementsModule`, achievements declared before composition (ADR 0006 amended); #68 `MicroEngine.Achievements` (architecture edge `Achievements -> Saves` added); #76 `MicroEngine.Input.Touch` as an opt-in module (active on touch-first screens or with a game layout; architecture row added; ADR 0045); T0 traces byte-identical on 16 scripts (#76) |
| S5a | S11 | delegated (directive 15 §5a), branch `feat/arch-1-stage5a-api` | done | [#78](https://github.com/NastMz/MicroEngine/pull/78), 9cce0ef | `Synth`, `SfxParams`, `Wave`, `Song`, `Instrument` to `MicroEngine.Audio.Retro`; `CanvasScaling`, `GameSettings.Scaling`, `EffectivePresentation` removed (never published; ADR 0029 amended); `TileMoveResult` (ADR 0031 amended); `docs.mjs` required build against the source-built SDK, nuget.org build report-only until the next release; 78/78 deterministic traces identical |
| S5b | S11 | delegated (directive 15 §5b), branches `feat/arch-1-stage5b-sdl-web`, `-2` | done | [#80](https://github.com/NastMz/MicroEngine/pull/80), 1130603; [#81](https://github.com/NastMz/MicroEngine/pull/81), 2996563 | #80 SDL 3 browser host behind `--web-backend sdl`, SDL built from source by `sdl-web.yml`, every CI-verifiable gate passed (run 41981413614); #81 SDL is the browser platform, raylib web retired, ADR 0047 |
| S5c | S11, S13 | delegated: one agent per demo | done | [#83](https://github.com/NastMz/MicroEngine/pull/83), 44a32d6; [#84](https://github.com/NastMz/MicroEngine/pull/84), d597a23; [#85](https://github.com/NastMz/MicroEngine/pull/85), 049f948; [#86](https://github.com/NastMz/MicroEngine/pull/86), 4cbe45e | Neon Rift, Trail Duel, Courtyard Key, Meadow Run on `Scene.Objects`; per-script trace comparisons (game sections identical, except 40 steps of Courtyard Key `gallery.script`: a pause inside a hitstop now consumes one step); captures byte-identical (67, 118, 43, 44); launch options through `HostEnvironment`; T0 starting points declared |
| S5d | S11, S13 | delegated (directive 16), three PRs | done | [#89](https://github.com/NastMz/MicroEngine/pull/89), c0ae530 (5d-1); [#88](https://github.com/NastMz/MicroEngine/pull/88), 6833daa (5d-2); [#87](https://github.com/NastMz/MicroEngine/pull/87), 3efd395 (5d-3) | 5d-1: `Scene.EngineStep`, `Camera2D.DrawPosition`, `Objects` refused while drawing, one tap rule for every device, phone `--env` as host variables, **the entity adapter deleted** and refused by the architecture test; 5d-2: API reference links for moved types, `docs/ci.md`, the architecture diagram and module table, the decisions index; 5d-3: frame loop 208 → 0 B per frame (limit < 24 B), audio feeder allocation counter |
| X2 | S11 | delegated, branch `fix/arch-1-short-press-check-2` | done | [#79](https://github.com/NastMz/MicroEngine/pull/79), 3682cfb | the browser short-press check missed presses read by a catch-up frame (the trace listed actions after the frame's last update; harness, not engine); development `Probe()` records actions per update; new gating catch-up check 80/80 (4/80 before) |
| P2 | S11, S13 | delegated: inside stage 6a (directive 13) | concluded | [#71](https://github.com/NastMz/MicroEngine/pull/71), 9973599 | palette swap (exact) and full-screen effect (±1/255) on tier 1 with shaders compiled offline by `microengine shaders`; typed error on tier 0; toolchain fixed (ADR 0042): DXC restored on first use, compiling needs Windows, MSL generated but not executed |
| S6-1 | S13 | delegated: stage 6a agent (directive 13), chained PRs | done | [#62](https://github.com/NastMz/MicroEngine/pull/62), f94afbb; [#71](https://github.com/NastMz/MicroEngine/pull/71), 9973599; [#72](https://github.com/NastMz/MicroEngine/pull/72), 87e7de9; [#77](https://github.com/NastMz/MicroEngine/pull/77), 4350249; [#82](https://github.com/NastMz/MicroEngine/pull/82), ee57db0 | #62 tier-1 `SdlGpuDevice`, conformance on D3D12, Vulkan and lavapipe (ADR 0041); #71 shader toolchain with P2 (ADR 0042); #72 glTF mesh kind, `Camera3D`, `Renderer3D`, Slice3D, E12 on desktop (ADR 0043); #77 device chosen through composition (`--graphics sdl-gpu`), Slice3D an ordinary game, run in CI on lavapipe (ADR 0046); #82 NVIDIA driver crash analysed. Evidence map: dossier §12.4 |
| S6-2 | S12, S13 | delegated: stage 6b agent (directive 13), chained PRs | done | [#63](https://github.com/NastMz/MicroEngine/pull/63), 9d159de; [#67](https://github.com/NastMz/MicroEngine/pull/67), 12ebdf2; [#69](https://github.com/NastMz/MicroEngine/pull/69), 7bab790; [#73](https://github.com/NastMz/MicroEngine/pull/73), 308d0f0 | #63 Box2D v3.2.0 natives for eight runtimes (`box2d.yml`, checksums, determinism comparison); #67 `Box2DPhysicsWorld2D`, capability suites, optional package never published; #69 tile grid broad phase (results identical to a full scan); #73 substitution scenarios 1 and 2, ADR 0031 amended. Shared and specific capabilities: dossier §12.5 |
| S6-3 | S13 | delegated: closing registrar (T10) | PR open | the closing PR | final report: dossier §12 (§9.1 item 4), integrated in `main` when merged; no release |

Route evidence: T1/T2 fire the mapping trigger (the audit spans the whole repository and external sources, far beyond
the inline evidence budget); the owner explicitly asked for delegated, parallel, domain-specialised agents. Phase II:
every unit is delegated by the owner's protocol (S14).

## Register

Updated by agents at each merge, gate and stage closure (S16). Dates are ISO; PR numbers link to the pull request.
Last update: 2026-11-05 (T10, closing Phase II).

### Stage status

| Stage | Status | Gates still open | Closure criteria met (dossier §9 a-e) |
| --- | --- | --- | --- |
| 0. Repair and measure | **closed** (directive 10): defects #24, CI #27, validation tooling #30, gallery #31 and #35, browser harness #36, T0 #42; P1, P3, P4 concluded | — | closed by the architect (directives 10, 12); (d): ADR 0035 reserved, not written (T10 check) |
| 1. Kernel and composition | **closed** (directive 12): 1A #33, #34; 1B #37; 1C #43 (ADR 0038) | — | closed by the architect (directive 12); (d): ADR 0036 reserved, not written; the architecture test it would record is in force (T10 check) |
| 2a. Graphics foundation | **closed** (directive 12): #45, #48, #52 (ADRs 0028, 0029); follow-ups landed: F11 action #54, views hook with `Aspect.Expand` and font pages #61 | — (tier-1 port → stage 6a) | closed by the architect (directive 12) |
| 2b. Input | **closed** (directive 12): #46, #49, #51 (ADR 0032) | — (open point: `IInputBackend` location) | closed by the architect (directive 12) |
| 2c. Audio and content | **closed** (directive 12): #44, #47, #50 (ADRs 0033, 0034) | — | closed by the architect (directive 12) |
| 3. World and physics | **closed** (directive 13): 3a #56, #57, #59 (ADR 0030); 3b #60 (ADR 0031) | — | closed by the architect (directive 13) |
| 4. UI and modules | **closed** (2026-11-05): 4a-0 #66, 4a-1 #70, 4a-2 #74 and #75 (ADR 0044); 4b #65, #68, #76 (ADR 0006 amended, ADR 0045) | — | (a) suites green on each PR; (b) the demos (achievements, saves, touch tests), the UI widget and capture tests, Slice3D's UI-view label; (c) rows for Text, UI, Saves, Achievements, Input.Touch; (d) merged; (e) T10 |
| 5. Consumers | **closed** (2026-11-05) with one content item open: 5a #78; 5b #80, #81 (ADR 0047); 5c #83-#86; 5d #87, #88, #89; X2 #79 | the public release notes (`docs/guide/release-notes.md`, "Next release") do not list stages 2-6: drafted in `docs/evaluations/arch-1/release-notes-draft.md` (outside T10's edit surfaces) | (a) green on each PR; (b) the four demos, the gallery, the samples, the template; (c) green, the adapter refused; (d) 0029 and 0031 amended, 0047, 0030 and 0038 amended (T10); (e) T10. Adapter deleted (#89); exclusion list never used |
| 6. Phase II completion | **closed** (2026-11-05): 6a #62, #71, #72, #77, #82 (ADRs 0041, 0042, 0043, 0046); 6b #63, #67, #69, #73 (ADR 0031 amended); final report dossier §12 (T10) | — (lavapipe proven; Box2D tested on Windows, Linux, macOS and in Chrome; iOS and Android arm64 build only, directive 13) | §9.1 item 2 met (§12.4), item 3 met (§12.5), item 1 met with open items (ADRs 0035/0036, release-notes page, owner validation), item 4 by T10 |

Prototypes: P1-P7 concluded (P2 inside stage 6a, #71).

### Approved decisions

Approved by the owner on 2026-11-02 (dossier §7, §11). Each ADR is written and merged in the stage that implements
it; "written" records the merged ADR file.

| ADR | Decision | Stage | Written |
| --- | --- | --- | --- |
| 0026 | Kernel with modules, typed registry, phased schedule; `Game` façade | 1 | yes: [0026](../../docs/decisions/0026-kernel-modules-and-schedule.md) (#33) |
| 0027 | The loop owns time; engine interpolation with teleport; world clocks | 1, 3 | yes: [0027](../../docs/decisions/0027-loop-owned-time.md) (#34) for loop-owned time; interpolation and world clocks are stage 3 |
| 0028 | One complete graphics device contract with capabilities, built-in pipelines, typed capability error; tier 1 pure SDL_GPU (P3); frozen as v1 (directive 5) | 1 (spike), 2a | yes: [0028](../../docs/decisions/0028-graphics-device-contract.md) (#45) |
| 0029 | `Presentation` replaces `CanvasScaling`; pixel art is a preset | 2a | yes: [0029](../../docs/decisions/0029-presentation-replaces-canvas-scaling.md) (#48) |
| 0030 | One object model; `IWorld` v1 as frozen by P7 (directive 11); transitional `Entity` adapter deleted at the end of stage 5 | 3a, 5 | yes: [0030](../../docs/decisions/0030-one-object-model.md) (#56); ADR 0003 marked as amended by it (#59); amended by T10: the adapter is deleted (#89) |
| 0031 | Physics common contract + declared capabilities; tile backend first; native Box2D after P6, with P6's revisions (directive 8) | 3b, 6 | yes: [0031](../../docs/decisions/0031-physics-contract.md) (#60); amended in 6b (#73: Box2D backend, natives, 128-world limit, substitution, broad phase) and 5a (#78: `TileMoveResult`) |
| 0032 | Input as device events with ids, players, layers | 2b | yes: [0032](../../docs/decisions/0032-input-devices-players-layers.md) (#46); open point: `IInputBackend` lives in `MicroEngine.Input`, not `MicroEngine.Platform` |
| 0033 | One managed mixer; backends are PCM sinks; default SDL sink a feeder thread (directive 7) | 2c | yes: [0033](../../docs/decisions/0033-one-mixer-over-pcm-sinks.md) (#47) |
| 0034 | Content handles, loader registry, sources; optional build-time import | 2c | yes: [0034](../../docs/decisions/0034-content-handles-loaders-sources.md) (#44) |
| 0035 | Validation by contract: declared properties, justified tolerances, closure criteria, exclusion list | 0 | **no**: reserved (decisions index, #88); the decision is applied (§8, conformance suites, T0) |
| 0036 | Assemblies by boundary, architecture test; few packages, one version | 1 | **no**: reserved (decisions index, #88); the architecture test exists since #33 |
| 0037 | Platform host contract with capabilities, explicit options and no time coupling (amends 0016, 0027); numbered after the reserved 0028-0036 (directive 6) | 1 | yes: [0037](../../docs/decisions/0037-platform-host-contract.md) (#37) |
| 0038 | One composition path (`MicroEngine.Hosting`, `GameHost`, `Configure` entry point, no process-wide statics) (directive 9) | 1 | yes: [0038](../../docs/decisions/0038-one-composition-path.md) (#43); amended by T10: phone `--env` pairs as host variables (#89), SDL browser host (#81) |
| 0039, 0040 | — | — | unused numbers (decisions index, #88) |
| 0041 | Tier-1 graphics device on pure SDL_GPU, conformance on D3D12, Vulkan and lavapipe | 6a | yes: [0041](../../docs/decisions/0041-tier1-device-on-sdl-gpu.md) (#62) |
| 0042 | Offline shader toolchain (`microengine shaders`, `CompiledShader`); DXC restored on first use, not redistributed | 6a | yes: [0042](../../docs/decisions/0042-offline-shader-toolchain.md) (#71) |
| 0043 | Minimal 3D path: glTF meshes as an asset kind, `Camera3D`, one material, `Renderer3D` | 6a | yes: [0043](../../docs/decisions/0043-minimal-3d-path.md) (#72) |
| 0044 | UI module: widgets as the highest input layer, renamable widget actions, captures of what the player sees | 4a | yes: [0044](../../docs/decisions/0044-ui-module.md) (#74, #75) |
| 0045 | Touch controls as an opt-in module in `MicroEngine.Input.Touch` | 4b | yes: [0045](../../docs/decisions/0045-touch-controls-module.md) (#76) |
| 0046 | Tier-1 device chosen through the composition path; `DeviceRenderer.Flush`/`Target` | 6a | yes: [0046](../../docs/decisions/0046-tier1-device-selection.md) (#77; crash analysis #82) |
| 0047 | SDL 3 is the browser platform; raylib leaves the web (amends 0007, 0008, 0017) | 5b | yes: [0047](../../docs/decisions/0047-sdl-browser-platform.md) (#81) |

Existing ADRs amended in Phase II: 0025 (asset pipeline), for scopes that share assets (#24, 745221e), and for the
asset store that now holds those semantics (#44); 0006 (achievements and saves as modules, #65, #68); 0029
(`CanvasScaling` removed, #78). The full list of amended ADRs is in dossier §12.6.

Architect decisions on prototype evidence (directives 2-16 of 2026-11-02 to 2026-11-05; dossier §9 prototype
verdicts, §11, §12). Directives 15 and 16 are recorded from the pull requests that cite them (#78, #80, #81, #87);
their full text was not given to the registrar.

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
| Directive 10 | Stage 2 as three parallel units with disjoint assemblies (2a `MicroEngine.Graphics` + `Graphics2D`, 2b `MicroEngine.Input`, 2c `MicroEngine.Audio` + `MicroEngine.Content`); consumers kept working through the existing façades; the exclusion list is added centrally by the architect only if a unit needs it (none did); ADRs 0028, 0029, 0032, 0033, 0034 | §6.4, §9, §11 |
| P7 (directive 11) | `IWorld` v1 frozen with corrections: world-owned blend (no blend arguments), attach-order iteration stable under removal, dense per-type storage, no struct attachments in v1, `Children(parent)` with a 2D fast path, world clock advancing with the world's step, concrete `SceneWorld`; `Entity` adapter per P7's mapping | §6.4, §9, §10, §11 |
| Directive 12 | Stage 3 as 3a (`MicroEngine.Scene`, chained 3a-1 world, 3a-2 scenes own worlds, 3a-3 `Entity` adapter; ADR 0030) and 3b (`MicroEngine.Physics` + `Physics.Tiles` after 3a-1 merges; ADR 0031); Box2D backend in stage 6; stages 0-2 closed | §9, §11 |
| Directive 13 | Stages 0-3 closed; views hook as a graphics unit before stage 4 (UI view at screen resolution, scene-level hook, pointer per view, `Aspect.Expand`, font pages following `DefaultSampler`); stage 6 as 6a (3D vertical slice: tier-1 SDL_GPU device, offline shader toolchain with P2, minimal glTF, `Camera3D`, lavapipe CI job, E12 measured on desktop) and 6b (Box2D from pinned source, CI-built natives for eight RIDs in an unpublished optional package, mobile build-only unless reachable; substitution scenarios 1 and 2; tile broad phase) | §9, §10, §11 |
| Directive 14 | Stage 4 as 4a (`MicroEngine.Text`, then `MicroEngine.UI`, chained; from the #61 review: captures composite the UI view over the world view, `UiScale` defaults to the surface density) and 4b (`Saves`, `Achievements`, `Input.Touch` as opt-in modules, type forwards, demos green, T0 traces unchanged); UI ADR at the next free number | §9, §11 |
| Directive 15 | Stage 5 opens with two units: 5a settles the public names before the demos migrate (`Audio.Retro`, `Presentation` only, `TileMoveResult`, the docs-check switch); 5b makes SDL 3 a browser host behind an option with CI gates, then the default with raylib web retired once every CI-verifiable gate passes | §12.3 |
| Stage 5c | One migration unit per demo off the entity adapter, each with a per-script state-trace comparison | §12.3 |
| Directive 16 | Stage 5d: 5d-1 the engine gaps the demo migrations found and the adapter's deletion; 5d-2 documentation and CI pages; 5d-3 the frame loop's and the audio feeder's allocations | §12.3 |
| Architect reviews | `Achievements -> Saves` edge (#68); the `Input.Touch` row and activation rule (#76); `UiScale` default granted as a separate unit (#75); 6a-4 follow-up after #72 (#77); WinDbg analysis with the owner's approval (#82) | §12 |

### Contracts in force

A contract is listed as frozen only after its gate passes and the freezing PR merges; later changes are versioned
revisions with evidence.

| Contract | Freeze gate | Status | Implemented in |
| --- | --- | --- | --- |
| Kernel (`EngineBuilder`, `ServiceRegistry`, `FrameSchedule`, `TimeSettings`, lifecycle events, `ILog`, capabilities) | stage 1A | in force on `main` (#33, #34; ADRs 0026, 0027) | `src/MicroEngine.Kernel` |
| Kernel per-step state hash (`StateHasher`, `IStateHashed`, `StateTrace`) | T0 | in force on `main` (#42) | `src/MicroEngine.Kernel`, `src/MicroEngine` |
| Platform host (`IPlatformHost`, `ISurface`, `IStorage`, `IContentSource`, page-driven rules) | P4 concluded; stage 1B | in force on `main` per ADR 0037 (#37) | `src/MicroEngine.Platform` |
| Hosting (`GameHost`, `HostBuilder`, `Configure` entry point) | stage 1C | in force on `main` per ADR 0038 (#43) | `src/MicroEngine.Hosting`, `src/MicroEngine` |
| Graphics device contract v1 | graphics contract spike (S1-2) | **frozen** (directive 5; #32); in force on `main` (#45, ADR 0028): `GraphicsContract.Version = 1`, `Textured`; the XML documentation is the contract text; no change through stage 6 | `src/MicroEngine.Graphics` (namespace `MicroEngine.Graphics.Devices`); tier 0 in `src/MicroEngine.Sdl`, `src/MicroEngine.Raylib` (#48); tier 1 `src/MicroEngine.Graphics.SdlGpu` (#62, ADR 0041; `sdl-gpu`, #77) |
| `Presentation`, `IRenderer2D`, `Camera2D` | stage 2a | in force on `main` (#48, #52; ADR 0029) | `src/MicroEngine.Graphics`, `src/MicroEngine.Graphics2D` |
| Input (device events, players, layers, `IInputBackend`, key chords) | stage 2b | in force on `main` (#46, #49, #51; ADR 0032); key chords and the `fullscreen` engine action (#54); one tap rule for every device (#89); `IInputBackend` kept in `MicroEngine.Input` | `src/MicroEngine.Input` |
| Views (`IScreenView`, `Game.Views`, `Scene.DrawUi`/`UiContains`/`WorldCamera`, `Presentation.UiScale`, `AspectMode.Expand`) | directive 13 | in force on `main` (#61); captures composite the UI view (#74); `UiScale` defaults to the surface density (#75) | `src/MicroEngine` (`Core/GameViews.cs`, scenes), `src/MicroEngine.Graphics` (`Presentation`) |
| `IAudioSink` v1 | P5 | **frozen** (directive 7; #38); in force on `main` (#47) | `src/MicroEngine.Platform/Audio/IAudioSink.cs`; mixer in `src/MicroEngine.Audio` (ADR 0033) |
| Content (`AssetStore`, `Asset<T>`, `IAssetLoader<T>`, sources) | stage 2c | in force on `main` (#44, #50; ADR 0034) | `src/MicroEngine.Content` |
| `IPhysicsWorld2D` common contract and `PhysicsCapabilities` | P6 concluded; written in stage 3b | in force on `main` (#60; ADR 0031): `PhysicsContract.Version = 1`, P6's eight revisions; the XML documentation is the contract text; two backends pass the common suite (#60, #67) | `src/MicroEngine.Physics`, `src/MicroEngine.Physics.Tiles`; `src/MicroEngine.Physics.Box2D` (#67, unpublished) |
| `IWorld` v1 | P7 concluded | **frozen** (directive 11; #53); in force on `main` (#56, #57; ADR 0030); the four demos on it (#83-#86); adapter deleted (#89) | `src/MicroEngine.Scene` (namespace `MicroEngine.Scenes`); `Scene.Objects`, `Scene.EngineStep` in `src/MicroEngine` |
| Tier-1 shader toolchain (`CompiledShader`, `.shader` version 1, `microengine shaders`) | P2 concluded (#71) | in force on `main` (ADR 0042) | `src/MicroEngine.Graphics` (`CompiledShader`), `src/MicroEngine.Cli/Shaders.cs` |
| Text, UI | stage 4a | in force on `main` (#70; #74, #75; ADR 0044) | `src/MicroEngine.Text`, `src/MicroEngine.UI` |
| Opt-in modules: saves, achievements, touch controls | stage 4b | in force on `main` (#65, #68, #76; ADRs 0006 amended, 0045) | `src/MicroEngine.Saves`, `src/MicroEngine.Achievements`, `src/MicroEngine.Input.Touch` |
| Tier-1 device selection (`GraphicsSettings.Device`, `--graphics`, `PlatformProfile.GraphicsDevices`, `DeviceRenderer.Flush`/`Target`) | stage 6a | in force on `main` (#77; ADR 0046) | `src/MicroEngine`, `src/MicroEngine.Sdl` |
| 3D path (`MeshAssets`, `Camera3D`, `Renderer3D`) | stage 6a | on `main`, repository only, not packed (#72; ADR 0043) | `src/MicroEngine.Graphics3D` |
| Browser platform (SDL 3 `BrowserLauncher`, `BrowserPage`, `SdlPlatform.BrowserProfile`) | P4 gates (stage 5b) | in force on `main` (#80, #81; ADR 0047) | `src/MicroEngine.Sdl`, `src/MicroEngine.Cli/Hosts/web` |

### Open PRs

| PR | Task | Branch | Owner | Notes |
| --- | --- | --- | --- | --- |
| the closing PR | T10, S6-3 | `docs/arch-1-close` | closing registrar agent | this register, dossier §12, ADR amendments, migration notes |

No other pull request is open (`gh pr list --state all`, 2026-11-05).

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
| [#41](https://github.com/NastMz/MicroEngine/pull/41) | T7 | 3aa486e | 2026-11-03 02:29 | register and dossier: directives 4-9 |
| [#42](https://github.com/NastMz/MicroEngine/pull/42) | T0 | 4dc5ada | 2026-11-03 02:47 | per-step state hash and trace divergence; closes stage 0 |
| [#43](https://github.com/NastMz/MicroEngine/pull/43) | S1-1C | 67d340e | 2026-11-03 04:59 | `MicroEngine.Hosting`, `GameHost`, `Configure` entry point, R1 fixed, `--state-trace`; ADR 0038; closes stage 1 |
| [#44](https://github.com/NastMz/MicroEngine/pull/44) | S2c (2c-1) | 09f9564 | 2026-11-03 05:53 | `MicroEngine.Content`: store, handles, loaders, sources; ADR 0034 |
| [#47](https://github.com/NastMz/MicroEngine/pull/47) | S2c (2c-2) | 50502e3 | 2026-11-03 06:15 | `MicroEngine.Audio`: one mixer over `IAudioSink` v1, default sinks, R2; ADR 0033 |
| [#50](https://github.com/NastMz/MicroEngine/pull/50) | S2c (2c-3) | f1f04e0 | 2026-11-03 06:25 | sounds and music as asset kinds; retro synthesizer folder |
| [#46](https://github.com/NastMz/MicroEngine/pull/46) | S2b (part 1) | c2f5f97 | 2026-11-03 07:10 | `MicroEngine.Input`: device events, players, layers, saved bindings; ADR 0032 |
| [#49](https://github.com/NastMz/MicroEngine/pull/49) | S2b (part 2) | d897a09 | 2026-11-03 07:19 | SDL and raylib event backends, every gamepad, layout labels |
| [#51](https://github.com/NastMz/MicroEngine/pull/51) | S2b (part 3) | 7c7a157 | 2026-11-03 07:28 | scripted device events, consuming widgets, two-player scenario |
| [#45](https://github.com/NastMz/MicroEngine/pull/45) | S2a (2a-1) | 85bf978 | 2026-11-03 07:46 | graphics device contract v1 in the engine, reference device, conformance suite; ADR 0028 |
| [#48](https://github.com/NastMz/MicroEngine/pull/48) | S2a (2a-2) | 1711c34 | 2026-11-03 08:00 | SDL and raylib tier-0 devices, `Presentation` and presets; ADR 0029 |
| [#53](https://github.com/NastMz/MicroEngine/pull/53) | P7 | d2191cc | 2026-11-03 08:01 | `experiments/arch-1/p7-entities` |
| [#52](https://github.com/NastMz/MicroEngine/pull/52) | S2a (2a-3) | 046a02c | 2026-11-03 08:14 | `Graphics2D` batcher, `IRenderer` façade over the device, `Camera2D` zoom and rotation; closes stage 2 |
| [#55](https://github.com/NastMz/MicroEngine/pull/55) | T8 | f08a3e3 | 2026-11-03 09:54 | register and dossier: directives 9-12, stages 0-2 closed, P7 verdict |
| [#56](https://github.com/NastMz/MicroEngine/pull/56) | S3a (3a-1) | 9b7022c | 2026-11-03 10:16 | `MicroEngine.Scene`: `IWorld`, `SceneWorld`, conformance suite, T0 hashing; ADR 0030 |
| [#54](https://github.com/NastMz/MicroEngine/pull/54) | S2a-F11 | 4b1d769 | 2026-11-03 10:17 | full-screen toggle as an engine action the game can rebind; key chords |
| [#58](https://github.com/NastMz/MicroEngine/pull/58) | X1 | 75b191f | 2026-11-03 10:31 | allocation checks measure their own thread after warm-up |
| [#60](https://github.com/NastMz/MicroEngine/pull/60) | S3b | e39d8bb | 2026-11-03 11:06 | `MicroEngine.Physics` and `MicroEngine.Physics.Tiles`; D6 fixed; ADR 0031 |
| [#57](https://github.com/NastMz/MicroEngine/pull/57) | S3a (3a-2) | 4705135 | 2026-11-03 11:35 | scenes own worlds, step tracking, world clocks, scene results |
| [#59](https://github.com/NastMz/MicroEngine/pull/59) | S3a (3a-3) | d5515b2 | 2026-11-03 11:46 | `Entity`/`EntityList` move to the transitional adapter; the demos use it; closes stage 3 |
| [#61](https://github.com/NastMz/MicroEngine/pull/61) | S2a-V | 4ff6857 | 2026-11-03 12:16 | UI view at screen resolution and the scene view hook; `Aspect.Expand`; font pages follow `DefaultSampler` |
| [#64](https://github.com/NastMz/MicroEngine/pull/64) | T9 | 251f1e2 | 2026-11-03 12:31 | register and dossier: stage 3 closed, stages 4 and 6 started |
| [#66](https://github.com/NastMz/MicroEngine/pull/66) | S4a (4a-0) | 3fee335 | 2026-11-03 12:55 | the 2D façade model moves into `MicroEngine.Graphics2D` |
| [#65](https://github.com/NastMz/MicroEngine/pull/65) | S4b (4b-1) | 427fa4e | 2026-11-05 03:01 | saves and achievements as opt-in modules |
| [#63](https://github.com/NastMz/MicroEngine/pull/63) | S6-2 (6b-1) | 9d159de | 2026-11-05 03:16 | Box2D v3.2.0 natives for every runtime |
| [#67](https://github.com/NastMz/MicroEngine/pull/67) | S6-2 (6b-2) | 12ebdf2 | 2026-11-05 03:44 | Box2D backend of the 2D physics contract |
| [#70](https://github.com/NastMz/MicroEngine/pull/70) | S4a (4a-1) | faca750 | 2026-11-05 03:44 | `MicroEngine.Text` |
| [#69](https://github.com/NastMz/MicroEngine/pull/69) | S6-2 (6b-3) | 7bab790 | 2026-11-05 03:59 | grid broad phase for the tile backend |
| [#62](https://github.com/NastMz/MicroEngine/pull/62) | S6-1 (6a-1) | f94afbb | 2026-11-05 04:06 | tier-1 SDL_GPU device, conformance on D3D12, Vulkan, lavapipe; ADR 0041 |
| [#73](https://github.com/NastMz/MicroEngine/pull/73) | S6-2 (6b-4) | 308d0f0 | 2026-11-05 04:11 | physics substitution demonstration; ADR 0031 amended |
| [#71](https://github.com/NastMz/MicroEngine/pull/71) | S6-1 (6a-2), P2 | 9973599 | 2026-11-05 04:19 | offline shader toolchain and P2 effects; ADR 0042 |
| [#68](https://github.com/NastMz/MicroEngine/pull/68) | S4b (4b-2) | 70b385e | 2026-11-05 04:22 | `MicroEngine.Achievements` |
| [#72](https://github.com/NastMz/MicroEngine/pull/72) | S6-1 (6a-3) | 87e7de9 | 2026-11-05 04:35 | 3D vertical slice on the tier-1 device; ADR 0043 |
| [#74](https://github.com/NastMz/MicroEngine/pull/74) | S4a (4a-2) | 5af531a | 2026-11-05 04:51 | `MicroEngine.UI`, captures of what the player sees; ADR 0044 |
| [#75](https://github.com/NastMz/MicroEngine/pull/75) | S4a (4a-2 b) | 608f470 | 2026-11-05 05:03 | `UiScale` follows the surface density |
| [#76](https://github.com/NastMz/MicroEngine/pull/76) | S4b (4b-3) | 80e7bf6 | 2026-11-05 05:15 | `MicroEngine.Input.Touch` opt-in module; ADR 0045; closes stage 4 |
| [#77](https://github.com/NastMz/MicroEngine/pull/77) | S6-1 (6a-4) | 4350249 | 2026-11-05 11:30 | tier-1 device through composition, Slice3D as a game; ADR 0046 |
| [#79](https://github.com/NastMz/MicroEngine/pull/79) | X2 | 3682cfb | 2026-11-05 11:30 | short presses never lost between frames in the browser check |
| [#82](https://github.com/NastMz/MicroEngine/pull/82) | S6-1 (6a-5) | ee57db0 | 2026-11-05 11:43 | NVIDIA driver fault behind the Vulkan test-host crash; closes stage 6 implementation |
| [#78](https://github.com/NastMz/MicroEngine/pull/78) | S5a | 9cce0ef | 2026-11-05 11:49 | API cleanup: `Audio.Retro`, `Presentation` only, `TileMoveResult` |
| [#83](https://github.com/NastMz/MicroEngine/pull/83) | S5c | 44a32d6 | 2026-11-05 12:21 | Neon Rift off the entity adapter |
| [#84](https://github.com/NastMz/MicroEngine/pull/84) | S5c | d597a23 | 2026-11-05 12:23 | Trail Duel on the scene world |
| [#85](https://github.com/NastMz/MicroEngine/pull/85) | S5c | 049f948 | 2026-11-05 12:28 | Courtyard Key off the entity adapter |
| [#86](https://github.com/NastMz/MicroEngine/pull/86) | S5c | 4cbe45e | 2026-11-05 12:29 | Meadow Run off the entity adapter |
| [#80](https://github.com/NastMz/MicroEngine/pull/80) | S5b (part 1) | 1130603 | 2026-11-05 12:37 | SDL 3 browser host behind `--web-backend sdl` |
| [#81](https://github.com/NastMz/MicroEngine/pull/81) | S5b (part 2) | 2996563 | 2026-11-05 13:00 | SDL 3 is the browser platform; raylib web retired; ADR 0047 |
| [#87](https://github.com/NastMz/MicroEngine/pull/87) | S5d (5d-3) | 3efd395 | 2026-11-05 13:14 | frame loop and audio feeder allocations |
| [#88](https://github.com/NastMz/MicroEngine/pull/88) | S5d (5d-2) | 6833daa | 2026-11-05 13:16 | API reference links, CI and guide pages match the code |
| [#89](https://github.com/NastMz/MicroEngine/pull/89) | S5d (5d-1) | c0ae530 | 2026-11-05 13:51 | engine gaps closed; the entity adapter deleted; closes stage 5 |

Times are UTC merge times (`gh pr view N --json mergedAt`; hours shown from #41). Every pull request from #27 to #89
is merged; none is closed unmerged; every commit on `main`'s first-parent history after #61 is one of these merges
(`git log --first-parent 4ff6857..c0ae530`). Chained PRs (#48 on #45, #52 on #48, #49 on #46, #51 on #49, #57 on #56, #59 on
#57) were retargeted to `main` and merged there.

### Temporary incompatibilities

Mirror of the checked-in exclusion list (dossier §9). Each entry: consumer, reason, owner, resolving stage. Stage 5
empties it.

| Consumer | Reason | Owner | Resolved by stage |
| --- | --- | --- | --- |
| — (empty at the end of Phase II: no exclusion-list mechanism was ever needed. Stages 1-4 kept every consumer building through type forwards and façades; stage 5 migrated the four demos (#83-#86), the gallery and samples (#78) and deleted the adapter (#89), with the documented samples built against the source-built SDK and reported against nuget.org 0.4.0-alpha.2 (`sdk/tests/docs.mjs`, #78)) | | | |

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
| #42 (T0) | `dotnet test MicroEngine.slnx -c Release` all passed (MicroEngine.Tests 409 + 1 skipped); new hash/trace tests (kernel 13, engine 10, games 8/7/12/9, gallery 3); RED first; an injected unseeded `Random.Shared` caught at update 40 in `scene[0]:Noisy`; no R2 divergence headless; scripts skipped (start not stated): Meadow Run `boss`, `tour`, every game's `touch` |
| #43 (1C) | `dotnet test` all green (MicroEngine.Tests 421 + 1 skipped; new `GameHostTests` 12, `MicroEngine.Cli.Tests` 2); browser `run.mjs repeat` 4/4 scenarios byte-identical (13/13 captures), RED on `main`'s package (smoke 3/6, showcase 0/1); 13/13 browser captures identical to desktop raylib (Meadow Run, Chrome, Windows); `packages.mjs`, `external.mjs --targets windows,web`, `docs.mjs` passed; Android/iOS not built locally |
| #44 (2c-1) | `dotnet test` all green (MicroEngine.Tests 433 + 1 skipped); `AssetStoreTests` 11; existing `AssetCacheTests` unchanged and green |
| #47 (2c-2) | `dotnet test` all green (MicroEngine.Tests 449 + 1 skipped); `AudioEngineTests` 14 (exact start frame, block sizes 64-4096 identical, voice policy, zero-allocation render); R2 as a T0 trace slow vs fast machine identical; browser `platform audio` 18 passed (Chrome), `audio` 6 passed (Chrome, Firefox); one-off failure of `Measuring_text_allocates_nothing` |
| #50 (2c-3) | `dotnet test` all green (MicroEngine.Tests 452 + 1 skipped); `AudioAssetTests`; snippets 104 blocks match |
| #46 (2b-1) | `dotnet test` all pass (MicroEngine.Tests 455); RED on a scratch copy of `main` for touch routing; `InputEventTests` 27; state hash unchanged for one player (traces and all 15 scenario captures unchanged) |
| #49 (2b-2) | `dotnet test` all pass (MicroEngine.Tests 458 + 1 skipped, Desktop 192 + 14 skipped); input conformance on headless, SDL (virtual joysticks), raylib (driven tests skipped: GLFW cannot be driven); one-off failure of `Measuring_text_allocates_nothing` |
| #51 (2b-3) | `dotnet test` all pass (MicroEngine.Tests 476 + 1 skipped); two-player scenario reproducible; demos' scripted captures unchanged with widget consumption |
| #45 (2a-1) | Graphics.Tests 43/43; RED by mutation of the reference device (top-left rule, scissor, filters); `dotnet test` all green; `packages.mjs`, `external.mjs --targets windows`, `docs.mjs` passed |
| #48 (2a-2) | Graphics 54, Desktop 425 + 10 skipped; device conformance 7 SDL configurations × 31 = 217/217, raylib 30/31 (minimise skipped); SDL `software` diagnostic with named tolerances (coverage ≤ 40/1,595, measured 33; blend ±2/255, measured 1.2) |
| #52 (2a-3) | Graphics 67, MicroEngine.Tests 448 + 1, Desktop 425 + 10; `RendererConformance` R1-R10 on the façade over every SDL driver and raylib; `web/tests/desktop.mjs` 138 captures: SDL within 3/255 of `main`, SDL vs raylib no pixel differs by more than 3 (on `main` up to 228 in up to 2,331 pixels); CI references unchanged (Native Linux and browser engine checks passed) |
| #54 (F11 action) | `FullscreenActionTests` 9 (4 fail with the engine system removed); SDL conformance check fails with `origin/main`'s `SdlPlatform.cs` swapped in; `dotnet test` all pass (MicroEngine.Tests 520 + 1 skipped, Graphics 68); `api-reference.mjs --check` up to date |
| #56 (3a-1) | 38 conformance tests RED first (did not compile); `dotnet test` all green (MicroEngine.Tests 550 + 1 skipped); `packages.mjs` 51 checks and `external.mjs --targets windows` 35 checks passed; `docs.mjs` not run (no sample changed) |
| #57 (3a-2) | 13 new scene-world tests RED first (did not compile); `dotnet test` all green (MicroEngine.Tests 563 + 1 skipped); 36 demo scripts traced on 046a02c and the branch: 35 byte-identical, `MeadowRun/smoke.script` differs only in `engine.achievements`, which also differs between two base runs |
| #58 (allocation checks) | `AllocationAssertTests` 6 (RED first: did not compile); stress 120/120 runs of `MicroEngine.Tests` green (before: 4/48 and 5/48 failed); an injected escaping allocation failed every check; `dotnet test` all green |
| #60 (3b) | three D6 `TileMotion` tests RED on `origin/main`, two preservation tests green on both; physics tests 64 (common 22, capability 14, tile backend 6, module 3, world/façade 19); `dotnet test` all pass (MicroEngine.Tests 619 + 1 skipped); 30 demo state traces byte-identical; `packages.mjs` passed; `PhysicsBench` 200 boxes: tile world 0.75 ms/step (30 B/step), `TileMotion` 0.08 ms/step (0 B); `docs.mjs`, `external.mjs` and the site build not run |
| #59 (3a-3) | `PlatformerExample` test RED first; Courtyard Key scroll test RED against the first adapter; the 11 adapter tests were written first but not run RED; `dotnet test` all green (MicroEngine.Tests 578 + 1 skipped, Migration.Entities 11, Samples 21); traces 35/36 byte-identical (the same `smoke.script` exception); `packages.mjs`, `external.mjs --targets windows` and `docs.mjs` (six documented samples on nuget.org 0.4.0-alpha.2) passed; `WorldBench` 0 B on every path |
| #61 (views hook) | `ViewsTests` (UI at window resolution over a world at canvas resolution, `UiScale` 2, `Expand`, pointer per view, fallback without a UI view); with the UI composite disabled 3 view tests fail; `dotnet test` all green (MicroEngine.Tests 636 + 1 skipped, Graphics 70, Samples 21, Desktop 440 + 15 skipped); demos' T0 trace tests unchanged; `packages.mjs`, `external.mjs --targets windows`, `docs.mjs` passed |
| #66 (4a-0) | `Graphics2DForwardingTests` 27 (26 RED before the forwards); `dotnet test` green (MicroEngine.Tests 663 + 1); games' T0 traces unchanged; `packages.mjs`, `external.mjs --targets windows`, `docs.mjs` passed |
| #65 (4b-1) | RED first (saves assembly test; 4 opt-in tests); `dotnet test` green (MicroEngine.Tests 643 + 1 skipped); snippets pass; site build and `packages.mjs` left to CI |
| #63 (6b-1) | layer self-test 22/22 and state hash `93f3a79ebf294b23` on win-x64, browser-wasm, linux-x64 (container), android-x64 (emulator); android-arm64 built only; macOS/iOS in CI; planner tests written after the rules (no RED) |
| #67 (6b-2) | Box2D tests 89 passed on Windows and 89 in Chrome (wasm interpreter); 89 skipped without the library, 89 failed with `MICROENGINE_BOX2D_REQUIRED=1`; tests written alongside, not first |
| #70 (4a-1) | architecture test RED first; `dotnet test` green except the machine-specific gamepad test (fails on the base too); `packages.mjs`, `docs.mjs` passed |
| #69 (6b-3) | `TileBroadPhaseTests` RED first; a 300-step scripted history identical for a full scan and cells of 16, 64, 300; PhysicsBench 200 boxes 0.74-0.83 → 0.30-0.31 ms, 800 boxes 10.0 → 1.3-1.7 ms |
| #62 (6a-1) | `Gpu` category 120/120 on NVIDIA (D3D12, Vulkan) and AMD; mutation 9/9 failed; `dotnet test` green (Desktop 608 + 15 skipped, MicroEngine.Tests 629 + 1); lavapipe proven by the PR's CI run |
| #73 (6b-4) | Box2D tests 101 (89 + 12 substitution) on Windows and in Chrome; sample: both backends reach the goal at step 653 with 2 of 3 coins (tiles (618, 121), Box2D (617.84, 120.84)); no RED (properties the backends already had). T10 checked CI run 41965084699: every Box2D job green (eight natives, tests on win-x64, linux-x64, osx-arm64, Chrome, package, determinism) |
| #71 (6a-2, P2) | Graphics 82/82; Cli 8 + 1 skipped; Desktop `Gpu` 126/126 (NVIDIA D3D12 + Vulkan); stale-shader check RED by editing `color.frag.hlsl` |
| #68 (4b-2) | architecture test RED first; focused 49 pass; `dotnet test` green except the machine-specific gamepad test |
| #72 (6a-3) | Graphics3D 8/8; `Slice3DTests` 6/6 on D3D12 NVIDIA, Vulkan NVIDIA, Vulkan AMD; depth-test mutation fails; captures looked at (D3D12, Vulkan); E12 desktop +1.06 ms (D3D12), +0.97 ms (Vulkan) |
| #74 (4a-2) | widget and capture tests RED first (did not compile); MicroEngine.Tests 693 + 1; composite equals the reference device's presented frame pixel for pixel; demo tests and T0 traces unchanged; `docs.mjs`, `packages.mjs` passed |
| #75 (4a-2 b) | RED first (did not compile); `UiScale` unset → density 1 or 2, explicit override, invalid density → 1; density-2 capture maps each UI unit to 2×2 pixels |
| #76 (4b-3) | architecture and behaviour tests RED first; T0 traces byte-identical on 16 scripts (base 70b385e vs branch, clean builds); `docs.mjs` passed |
| #77 (6a-4) | `DeviceCanvasTests`, `GraphicsSettingsTests`, `TierOneGameTests`; full suite green except the gamepad test. T10 checked CI run 41973605483: lavapipe `llvmpipe (LLVM 20.1.2)`, Mesa 25.2.8; `Gpu` 138 tests: 94 passed, 44 inconclusive (Direct3D 12 class, minimise); `slice.script` ran as `platform: sdl (SDL sdl-gpu vulkan)` |
| #79 (X2) | catch-up check 4/80 before, 80/80 after; whole platform suite 66/66 in 20/20 runs (62-64 of 66 before); `mobile.mjs` 272/272; release package plays with no trace hooks |
| #82 (6a-5) | docs only: full dump analysed with cdb (WinDbg); faulting thread in `nvoglv64.dll` 32.0.16.1088, use-after-free; 1 crash in 40 runs of the 138-test `Gpu` category |
| #78 (5a) | `RenamedApiTests` 4 RED first; presets reproduce the old Pixel/Smooth rule over 13 × 3 × 5 cases; 78/78 deterministic traces identical (the 2 differing are the deliberately unseeded `Noisy` scene); `docs.mjs` required checks pass, nuget.org report shows the Retro break as expected; `packages.mjs`, `external.mjs` passed |
| #83 (5c Neon Rift) | NeonRift.Tests 56; 7 scripts, 11 670 steps bit-identical with the world's hash masked; 67 captures byte-identical; whole solution 1890 tests, 0 failed |
| #84 (5c Trail Duel) | TrailDuel.Tests 118; 8 scripts: every game and engine section identical on every step; 118 captures identical; two save folders give identical traces |
| #85 (5c Courtyard Key) | CourtyardKey.Tests 52; 12 scripts: game sections identical except `gallery.script` `play.progress` from update 321 (hitstop and pause); 43 captures byte-identical |
| #86 (5c Meadow Run) | MeadowRun.Tests 70; 27 runs with game-owned state identical at every step; 44 captures byte-identical; code 5887 → 5877 lines |
| #80 (5b-1) | gate table on CI run 41981413614 (four demos, Chrome and Firefox, emulated phones and pads, layout, suspension): all passed; first run 41978009538 failed three gates, each fixed |
| #81 (5b-2) | CI run 42022955265: CI result passed; Trail Duel's first attempt failed one repeat comparison (`master-battle.script`, typewriter one character behind), passed on re-run (Known problems); packages about 190 KB smaller with SDL |
| #87 (5d-3) | `FrameGarbageTests` RED (3 failing) and the frame-loop check RED at 208 B; after: 0 B per median frame over 300 and 1,000,000 frames; feeder counter mutation 720-744 B per window caught; PerfBench 86,976 → 720 B per frame, 33 → 0 collections |
| #88 (5d-2) | `api-reference.mjs --check` 444 pages up to date; 6873 links between API pages, 0 broken; snippets 104 blocks; site build 501 pages with `check-dist` |
| #89 (5d-1) | RED observed for each item; 37 traced runs byte-identical before and after; `dotnet test` green except the gamepad test (MicroEngine.Tests 733 after merging main); `packages.mjs`, `external.mjs --targets windows`, `docs.mjs` passed; `mobile.mjs` stall check syntax-checked only; phone hosts not built locally |
| `main` CI (T10 check, 2026-11-05) | runs on f94afbb (#62) and 80e7bf6 (#76) failed on Trail Duel's browser short-press check, the flake #79 fixed; 3efd395 (#87) failed only on an HTTP 502 uploading a demo artifact; every other run after #64 passed or was superseded; c0ae530 (#89) still running when T10 was written |
| P7 (#53) | `Scene.Tests` 44 passed (42/44 RED against a stub); `CourtyardKey.Port.Tests` 71 passed (47 game tests, 22 trace equivalence runs, swap-remove divergence at update 248 of `gameover.script`); frame time ±5 %, allocation ±0.1 %; dense storage 4.7× faster lookups than per-entity dictionaries; desktop CoreCLR only |

### Known problems

Confirmed defects from the audit (dossier §3.1), all scheduled in stage 0 except D5-D8, which the redesign resolves (D5, D7, D8 resolved in stage 2):

| ID | Summary | Task | Status |
| --- | --- | --- | --- |
| D1 | `ComposedPlatform` drops display pacing, timer, graphics menu, diagnostics | S0-D1 | fixed (#24); `ComposedPlatform` deleted in 1B (#37) |
| D2 | Release-gating package checks red on `main`; not run daily | S0-D2, S0-C1 | fixed (#27) |
| D3 | Visual fingerprint passes real regressions, fails invisible shifts | S0-D3, S0-V3 | fixed (#30): property checks beside the fingerprint |
| D4 | Headless clock hard-codes 60 Hz | S0-D4 | fixed (#24); time loop-owned since 1A (#34) |
| D5 | Backends disagree (clear vs clip, sub-pixel rows, disposed textures, capture alpha) | S0-V1, S2a | fixed: stage 0 part (#30); devices on one contract (#48) and both platforms drawing through them (#52): SDL vs raylib within 3/255 on 138 captures |
| D6 | `TileMotion` tunnels and embeds | S3b | fixed (#60): exact swept mover; `TileMotion` a façade over it; no T0 trace changed |
| D7 | Straight alpha fringes | S2a | fixed: premultiplied contract and no-dark-fringe test (#45); straight sources premultiplied at upload (#52) |
| D8 | Three mixers with three voice policies | S2c | fixed (#47): one mixer, one voice policy on every backend |
| D9 | `AssetCache.Unload` disposes shared textures; duplicate loads | S0-D9 | fixed (#24) |
| D10 | Browser suite not in CI; no SDL keyboard/audio-output test | S0-C2, S0-C3 | fixed (#27) |
| D11 | Smaller defects (`EntityList` clear, font filter docs, flaky wait, `docs/ci.md` drift, frozen-overlay blend, QWERTY prompts) | S0-D11, S2b, S3a | fixed: five by #24, #27 and #49 (QWERTY prompts); the frozen-overlay blend by #57 (per-scene step tracking) |

Found in Phase II:

| ID | Summary | Handling | Owner | Status |
| --- | --- | --- | --- | --- |
| K-P1-1 | SDL_Renderer's `gpu` driver on D3D12 ignores VSync off in desktop full screen (P1: 10 ms frames on a 100 Hz display) | Pure SDL_GPU with an explicit present mode runs unthrottled with Immediate (graphics spike); tier 0 keeps SDL's default driver. Not reported upstream (owner item) | S1-2 | closed for tier 1 (directive 5) |
| K-P4-1 | .NET wasm interpreter mis-calls P/Invokes with 64-bit enum parameters, `delegate* unmanaged` parameters (P4) and 8-byte structs passed by value (P6) | Worked around: engine-owned `ulong` declarations for the SDL browser host (`SdlWeb`, #80); the Box2D layer passes only scalars and pointers (#63, #67). Not reported upstream (owner item) | S5b, S6-2 | worked around; upstream report open (owner) |
| R1 (determinism) | Browser scripted runs are real time (`BrowserLauncher` builds `new DesktopOptions()`): steps per frame and draw blend from the wall clock | One launcher model decides the time mode; browser test proves byte-identical captures across two runs | S1-1C | fixed (#43) |
| R2 (determinism) | Music renders in wall-clock slices inside `Game.Step` in browsers; `CurrentMusic` depends on CPU speed | Logical music state independent of rendering; render outside the step | S2c | fixed (#47) |
| R3 (determinism) | Scene caches written while drawing | T0 hashes explicit contributions only; a test proves drawing never changes the hash | T0 | fixed by design (#42) |
| R4 (determinism) | Cross-platform last-bit float differences (FMA in `Vector2.Lerp`, per-libm `MathF`) | Cross-platform identity reported, not gated; deterministic engine math later | — | report only |
| Tap expiry | `RaylibInput.CollectTaps` expires an unread tap after 8 drawn frames, not after an update: at about 1000 frames per second 22 of 40 presses lost on Meadow Run (CI draws at 60, not affected) | Taps expire by updates or time | S2b | fixed (#46 model, #49 raylib backend; raylib still polls pads per frame) |
| raylib canvas alpha | raylib's canvas alpha follows OpenGL's default blend (0xBF after a 50 % draw) and the frame is presented with alpha blending over the letterbox, which may darken translucent areas on screen (not in captures) (#30) | New raylib tier-0 device on the frozen contract | S2a | fixed (#52: opaque composite) |
| Browser vs desktop frames | Meadow Run's level and map, Neon Rift's stage 1 and single Courtyard Key frames differ between browser and desktop (Meadow Run's camera about 3 px off) (#27) | Reported in the CI job summary, not a gate (pixel identity across backends is not a contract); cause R1, fixed by #43 (Meadow Run: 13/13 browser captures identical to desktop raylib locally) | S1-1C (R1) | report only |
| R-SDL-3.5 | `ppy.SDL3-CS` 2026.1002.1 bundles SDL 3.5.0 (`ea7a2da`), a development series; P1, P3, P4, P5 and the graphics spike ran on it | Exact version recorded by the spike; binding updates re-run the conformance suites; dossier §10 | — | open (risk) |
| R-SW-RAST | lavapipe/WARP for SDL_GPU unproven; SDL's software renderer fails the coverage rule (18/240 pixels) | lavapipe runs the tier-1 suites and the slice in CI (#62, #72, #77; run 41973605483); WARP still unreachable through SDL_GPU; `sdl-software` stays a diagnostic fallback | S6-1 | closed for lavapipe |
| R-E12 | Tier 1's per-frame surface copy (E12) unmeasured on mobile | Measured on desktop (#72: +1.06 ms D3D12, +0.97 ms Vulkan at 1920×1080, upper bounds); no phone reachable | S6-1 | desktop measured; mobile open |
| R-ETC2 | No ETC2 in SDL_GPU 3.5; ASTC unsupported on the measured desktop GPUs: Android compressed textures have no path | Per-format capability; decision in 2c/6 | S2c, S6-1 | open (risk) |
| R-AUDIO-DEV | Audio sinks unverified on iOS and real phones (Android emulator only), Safari and real browser audio devices | Recorded in dossier §10 | S2c | open (risk) |
| R-BOX2D-NATIVE | Box2D natives unverified on Linux, macOS, iOS and Android arm64 | CI builds eight runtimes; self-test, tests and the determinism hash on Windows, Linux, macOS arm64 (and x64 under Rosetta) and in Chrome (#63, #67, #73); iOS and Android arm64 build only (directive 13); iOS static linking unverified | S6-2 | closed for desktop and browser; mobile build only (owner item) |
| P1-gallery | The asset gallery's captures embedded wall-clock load times | S0-V4 | — | fixed (#31, #35) |
| F11 action | Backends toggled full screen on F11/Alt+Enter hard-wired; directive 10 asked for a rebindable engine action (#52) | `fullscreen` engine action, key chords, chord precedence general to the input map | S2a-F11 | fixed (#54); the toggle happens in the first update after the press |
| Views hook | World view + screen-resolution UI view were possible (`DeviceCanvas.Compose`) but games had no scene-level drawing hook (#52); 3a-2 could not build it (#57) | UI view at screen resolution and the scene hook (directive 13) | S2a-V | fixed (#61) |
| 2a follow-ups | `Aspect.Expand` missing; font pages did not follow `DefaultSampler` (#48, #52) | Done in the views unit | S2a-V | fixed (#61) |
| UI view in captures | Captures recorded the world view only (#61) | Captures composite the UI view; `CaptureWorld` keeps the world-only capture | S4a | fixed (#74) |
| `UiScale` default | `Presentation.UiScale` defaulted to 1 (#61) | `float?`, null = the surface density; headless 1 | S4a | fixed (#75) |
| Tier-1 adapter | The spike's tier-1 adapter was not ported to the engine contract (#45) | `SdlGpuDevice` | S6-1 | fixed (#62) |
| Tile broad phase | The tile backend scanned every body (0.75 ms per step for 200 boxes, #60) | Spatial grid, results identical to a full scan | S6-2 | fixed (#69) |
| `MoveResult` name clash | `MoveResult` in both `MicroEngine.World` and `MicroEngine.Physics` (#60) | The tile mover's result is `TileMoveResult`; `TileMotion` stays a stateless convenience | S5a | fixed (#78) |
| Legacy `Entity` mentions | Two reference pages listed `Entity` without the removal label (#59) | Both labelled "removed in the next SDK version" (checked by T10: `docs/reference/components.md`, `configuration.md`) | S5 | fixed |
| `smoke.script` achievements | Meadow Run's `smoke.script` state trace differs between two runs of the same build in `engine.achievements`: saved achievements persist in the user's save folder (#57, #59) | Pre-existing determinism gap of that script; not caused by stage 3 | — (unassigned) | open |
| Audio feeder allocations | The feeder thread's render loop was not measured (#58) | `AudioEngine.RenderAllocations` (internal) counts render-thread bytes; the SDL feeder allocates nothing per render | S5d | fixed (#87) |
| Frame loop allocation margin | 240 B per frame against a 256 B limit (#58) | Three sources removed (headless command text, boxed enumerators, scope copies); median frame 0 B, limit < 24 B | S5d | fixed (#87) |
| Timing flake | `Loads_during_play_are_counted_and_slow_ones_reported_once` reported 2 slow loads instead of 1 once in 60 contended runs (#58) | Timing-based, not an allocation check | — (unassigned) | open |
| `Input.Touch` assembly row | The architecture table had no `MicroEngine.Input.Touch` row | Row added: `Input.Touch -> Input, Graphics2D, Text, Graphics, Content, Kernel, Platform` | S4b | resolved (#76) |
| Mobile validation wording | Dossier §9 said Box2D natives are "validated on Linux, macOS, iOS and Android arm64"; directive 13 makes mobile build-only | Dossier §9 stage 6 row corrected and §12 records what ran where | T10 | resolved (T10) |
| Input backend location | `IInputBackend` lives in `MicroEngine.Input`, not `MicroEngine.Platform` as §6.3 sketched | Kept there through Phase II (the architecture test and #88's diagram record it) | — | accepted deviation |
| Hosting options | `MicroEngine.Hosting` carries GPU, window and audio options as command-line words (#43) | The graphics device is typed (`GraphicsSettings.Device`, `LaunchRequest.Graphics`, #77); the other options stay words | later | follow-up (partly done) |
| Phone `--env` | Phone developer variables were exported to the process environment (#43) | The demos read through `HostEnvironment` (#83-#86); phone hosts pass the pairs as `HostEnvironment.Variables` (#89); ADR 0038 amended (T10) | S5d | fixed (#89) |
| Trace export | No state-trace export from web or phone runs (#43) | — | — | declined |
| T0 coverage | T0 skipped scripts with no stated start (touch, Meadow Run `boss` and `tour`, Trail Duel `catch`) (#42) | Starting points declared and traced (#83-#86); Courtyard Key, Meadow Run and Trail Duel fail on a script without one | S5c | fixed |
| `Audio.Retro` namespace | `Synth`/`Song` in a `Retro` folder with namespace `MicroEngine.Audio` (#50) | Moved to `MicroEngine.Audio.Retro` with a migration note | S5a | fixed (#78) |
| `VoiceHandle` naming | The implemented handle is `VoiceHandle(int Index, uint Generation)`; the dossier sketch and directive 10 say `SoundHandle` (#47) | Recorded in dossier §6.4 (the code is authoritative) | — | recorded |
| R-MUSIC-BUFFER | Streamed music is decoded only 0.5 s ahead by `Pump` (#47); a longer main-thread stall starves it | Recorded in dossier §10 | S2c | open (risk) |
| Flaky allocation tests | `Measuring_text_allocates_nothing` (and similar allocation-counter tests) failed once in #47 and #49 runs and passed on rerun | Root cause: background-GC allocation-buffer accounting; `AllocationAssert` requires one 0-byte round after warm-up | X1 | fixed (#58) |
| R-P7-SCOPE | `IWorld` measured on desktop CoreCLR only; iteration order latent behaviour | Conformance pins the order (#56); each demo's traces compared before and after its migration (#83-#86); wasm and Android costs still unmeasured | S5 | open (risk: wasm/Android cost) |
| NVIDIA Vulkan crash | Intermittent crash of the test host on the development machine (1 in 40 full `Gpu` runs) | Analysed with WinDbg (#82): use-after-free on a thread of `nvoglv64.dll` 32.0.16.1088 when one process creates and destroys many Vulkan devices; no SDL or engine frame on the stack; never in CI. Documented in ADR 0046 (Consequences) and `docs/manual/rendering.md` (limitations) | S6-1 | closed (environmental driver defect); NVIDIA report is an owner item |
| Trail Duel typewriter capture | One CI browser `repeat` run (#81, run 42022955265): capture `b_596` of `master-battle.script` showed the typewriter one step behind ("Em" vs "Emb"); 80 other captures byte-identical; state-trace tests pass; not reproduced in 23 local headless Chrome runs | Suspected, unproven: the capture read-back with WebGL on Linux/llvmpipe (`SdlRendererDevice.ReadPixelsCore`). Proof: log the step number with the read-back pixels in development builds, or capture twice | — (unassigned) | open |
| Browser short-press check | The gating check failed intermittently on loaded runners (main runs on f94afbb and 80e7bf6) | Harness: the trace listed actions after the frame's last update; development `Probe()` records them per update; catch-up check added | X2 | fixed (#79) |
| Key vs touch taps | A key tap was down in one update of a catch-up frame, a touch tap in every update (#79) | One rule: a press shorter than a frame is down in exactly one update for every device (`VirtualControls.EndUpdate`) | S5d | fixed (#89) |
| SDL licence notice | Native (desktop, Android, iOS) packages carry no `licenses/SDL-LICENSE.txt`; the `MicroEngine.Sdl` package packs `SDL3.a` without SDL's licence; `site/scripts/demos.mjs` and `packaging/targets.mjs` do not require it; the notices' zlib text did not name SDL | zlib text fixed by T10 (`THIRD-PARTY-NOTICES.md`); the rest are code changes before the next release (dossier §12.7 c; `docs/releasing.md`) | — (unassigned) | open (before the next release) |
| Release notes page | `docs/guide/release-notes.md` "Next release" lists none of stages 2-6 (worlds, physics, UI, SDL browsers, `sdl-gpu`, entity removal) | Drafted in `docs/evaluations/arch-1/release-notes-draft.md`; merge it in the release pull request (`docs/releasing.md`). Outside T10's edit surfaces | — (owner / release PR) | open |
| ADRs 0035, 0036 | The plan's two reserved decisions (validation by contract; assemblies by boundary) were never written, although stages 0 and 1 were closed (criterion d) | Write them, or record that the plan (§7, §8) stands as their record | — (architect) | open |
| Demo artifact upload | `main` CI on 3efd395 (#87) failed: `TrailDuel-….tar.gz` upload to `demo-artifacts` returned HTTP 502 (run 42034089258) | Infrastructure; the next run on 6833daa rebuilds | — | infrastructure, not a defect |
| Gamepad test on development machines | `GamepadTests.Pads_connect_are_recognised_press_and_hand_over_when_unplugged` fails locally when a real pad is attached (#68-#89) | Environmental; green in CI on clean runners | — | known, environmental |
| `DialogueBox`, `PromptText` in the façade | They depend on `ActionPrompts` (#74) | Needs a prompt-formatting interface in UI | — | follow-up |
| Owner validation items | Real phones and tablets, Safari, real gamepads, haptics, high-density screens, long sessions (#80, #81); iOS static linking of natives (#63, #67); Metal/MSL shaders (#71) | Not verifiable in CI; never marked passed by an agent | owner | open (owner) |

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
3. NVIDIA: the use-after-free in `nvoglv64.dll` 32.0.16.1088 (#82; ADR 0046 Consequences).
4. Validation on real devices (not verifiable in CI): Android and iOS phones and tablets, Safari on macOS and iOS,
   real gamepads (vendor mappings, rumble), haptics on a real Android browser, high-density screens, long sessions;
   iOS static-library linking of the Box2D natives; Metal and MSL shaders on Apple; E12 and audio sinks on phones.
5. Lifting the publication freeze (`.github/phase-freeze`) and releasing the next SDK version, a breaking Alpha:
   the release-notes draft, the SDL licence items and ADRs 0035/0036 are open (Known problems).

### Process lessons

- PR #31 merged with only the Linux .NET checks: the CI planner treated `samples/` as a .NET input only, so the `sdk`
  check (`sdk/tests/external.mjs`, whose gallery tour #31 broke by waiting for a load during play) did not run, and
  `main` regressed (found while verifying #34, which reported the same failure on `main` at 1cd5c50). #35 fixed the gallery and the planner: `samples/AssetGallery/**` now selects
  `dotnet` and `sdk` and is an input of the `sdk` evidence key. Before merging, confirm that the checks a change needs
  actually ran, not only that the CI result is green.
- Evidence reuse on retargeted PRs (architect's check during stage 2): when a chained PR (#48, #52, #49, #51) was
  retargeted to `main`, the jobs the planner skipped reused passed runs whose inputs were identical, so the reuse
  rule (ADR 0013, once per content) held across retargeting; a skipped job is evidence only when its inputs match.
- Allocation-counter tests were flaky under load (`Measuring_text_allocates_nothing` failed once in the #47 and #49
  runs). The cause was the runtime's per-thread count, not the code under test (#58): reproduce a flake outside the
  test framework before adding tolerance. #58 added none; a check now needs one 0-byte round after warm-up.
- A unit's directive can ask for something whose prerequisite does not exist yet: 3a-2 could not build the views hook
  (no UI view on `main`); the agent reported it instead of widening its edit surface, and the architect made it a
  separate unit (directive 13).

- Final registration (T10): the PR descriptions were the evidence, but CI facts were checked separately. The lavapipe
  run, the Box2D jobs and the two `main` failures were all read from the Actions logs. Two `main` runs failed on a
  flake (#79) and one on infrastructure, so a green PR check does not prove `main` stayed green. Read the runs after
  each merge.
- Directives 15 and 16 are known only from the PRs that cite them. Directives given outside the repository should be
  added to the register's Log when they are issued, as L4-L6 were.

### Blocks

- None for Phase II's implementation. The definition of done is met except the open items below, which need
  the owner or the architect: the release-notes page (drafted), ADRs 0035 and 0036, the SDL licence items before
  the next release, and the owner's device validation.

### Next steps

1. Review and merge the closing PR (T10). Its CI must be green.
2. The architect decides on ADRs 0035 and 0036 (write them, or record that the plan stands as their record).
3. Before the next SDK release: ship SDL's licence file in the native packages and the `MicroEngine.Sdl` package;
   require it in `site/scripts/demos.mjs` when the next demo release is built on SDL; merge the release-notes draft
   in the release pull request (`node sdk/version.mjs release <version>`).
4. The owner validates on real devices (Items for the owner, 4), lifts the freeze and decides the release.
5. Open follow-ups stay listed under Known problems: the Trail Duel typewriter capture, Meadow Run's `smoke.script`
   achievements, the timing flake, `DialogueBox`/`PromptText`, the typed hosting options, wasm/Android `IWorld` cost.

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
- L10 (2026-11-03) T8 brought the dossier and this register up to date with `main` and the architect's directives
  9-12. Merged since L9 (all of #41-#53; none closed unmerged; `gh pr list --state all`): #41 (T7, 3aa486e), #42 (T0,
  4dc5ada), #43 (1C, 67d340e; ADR 0038), #44 (2c-1, 09f9564; ADR 0034), #47 (2c-2, 50502e3; ADR 0033), #50 (2c-3,
  f1f04e0), #46 (2b-1, c2f5f97; ADR 0032), #49 (2b-2, d897a09), #51 (2b-3, 7c7a157), #45 (2a-1, 85bf978; ADR 0028),
  #48 (2a-2, 1711c34; ADR 0029), #53 (P7, d2191cc), #52 (2a-3, 046a02c). Stages 0, 1 and 2 closed (directives 10,
  12); stage 3 started (3a; 3b after 3a-1). T0's gate is Linux on every change and Windows/macOS when the engine or
  shared inputs change: recorded as a precision of the dossier's "every OS" (§8). The discrepancy noted in L9 is
  resolved: the contract now lives in `src/MicroEngine.Graphics` with `Textured` and `GraphicsContract.Version = 1`
  (#45). P7 verdict (directive 11) written in dossier §9; §6.4's Scene sketch replaced by `IWorld` v1 as frozen
  (world-owned blend, no blend arguments, attach-order iteration, `Children`, `SceneWorld`, no struct attachments);
  §6.4's contract table and 2D/input/audio parts note what stage 2 implemented (`VoiceHandle`, `IInputBackend` in
  `MicroEngine.Input`, `Presentation` fields). Fixed and closed in Known problems: D5, D7, D8, D11 QWERTY prompts, R1,
  R2, R3, tap expiry, raylib canvas alpha. New entries: F11 action (PR #54 open), views hook (3a-2), `Aspect.Expand`
  and font pages (follow-ups), tier-1 adapter port (stage 6), `IInputBackend` location (open point), hosting options as
  CLI words, phone `--env` (stage 5), no web/phone trace export (declined), T0 script starting points (stage 5),
  `Audio.Retro` namespace (stage 5), `VoiceHandle` naming, 0.5 s music buffer risk, flaky allocation tests (fix in
  progress), P7 measurement scope. Process notes: evidence reuse held on retargeted PRs (architect's check);
  allocation-test flakiness. The exclusion list stays empty. Not independently re-verified by T8: the evidence-reuse
  observation (taken from the architect's brief). Next: stage 3a-1, then 3a-2/3a-3 and 3b.
- L11 (2026-11-03) T9 brought the dossier and this register up to date with `main` (4ff6857) and the architect's
  directives 12-14. Merged since L10 (all of #54-#61; none closed unmerged; `gh pr list --state all`): #55 (T8,
  f08a3e3), #56 (3a-1, 9b7022c; ADR 0030), #54 (full-screen action, 4b1d769), #58 (allocation checks, 75b191f), #60
  (3b, e39d8bb; ADR 0031), #57 (3a-2, 4705135), #59 (3a-3, d5515b2), #61 (views hook, 4ff6857). Open: #62 (6a-1) and
  #63 (6b-1). Stage 3 closed (directive 13); stage 4 started as 4a and 4b (directive 14); stage 6 started as 6a and
  6b (directive 13). Checked on `main`: `ActionNames.Fullscreen` = `"fullscreen"` and `KeyChord` in
  `src/MicroEngine.Input`; `src/MicroEngine.Scene` declares namespace `MicroEngine.Scenes`; `MEMIG001` and
  `IsPackable` false in `src/MicroEngine.Migration.Entities`; `PhysicsConformance.Scale`; the tile types forwarded
  from `MicroEngine` (`src/MicroEngine/World/TileForwards.cs`); `IScreenView`, `Presentation.UiScale` (default 1) and
  `AspectMode.Expand`; ADRs 0030 and 0031 present. New tasks: T9, S2a-V (views hook, #61), X1 (allocation checks,
  #58), S4a, S4b; S3a, S3b, S2a-F11 done; S6-1, S6-2, P2 in progress. Known problems fixed: D6, D11 (all), F11 action,
  views hook, the 2a follow-ups, flaky allocation tests. New entries: UI view in captures and `UiScale` default
  (carried to 4a), tile broad phase (6b), `MoveResult` name clash (stage 5), legacy `Entity` mentions in two reference
  pages, Meadow Run `smoke.script` achievements persisting between runs, audio feeder allocations, frame loop
  allocation margin, a timing flake. Dossier: §6.3 adapter references, §6.4 states of `IWorld` and the physics
  contract, §9 stage 4 and 6 units, P2 verdict cell, progress block, §10 (five rows updated, two added), §11
  (directives 13-14). Conflicts and discrepancies for the architect, not resolved here: (1) directive 14 makes
  `Input.Touch` an assembly "references per the architecture table", but the table has no `MicroEngine.Input.Touch`
  row (dossier §6.3 puts `Input.Touch` inside `MicroEngine.Input`); (2) dossier §9 (stage 6) says Box2D natives are
  validated on iOS and Android arm64 and §10 measured E12 in the slice, while directive 13 makes mobile build-only for
  Box2D and leaves E12 on mobile unverified unless reachable; (3) directive 12 described a 44-rule conformance suite,
  #56 delivered 38 tests (P7's 44 cases were 22 rules over three storage designs; v1 has one); (4) dossier §6.3
  listed the adapter's references as Scene and Graphics2D, #59 added the core `MicroEngine` (recorded); (5) #57 and
  #59 traced 36 demo scripts, #60 reports 30 (as reported; not reconciled). Not re-run by T9: any test or benchmark;
  the results above are the PRs' own reports. Next: 4a-1 and 4b; review #62 and #63.
- L12 (2026-11-05) T10 closed the register for Phase II (`main` at c0ae530). Merged since L11, all through pull
  requests and none closed unmerged (`gh pr list --state all`; `git log --first-parent 4ff6857..c0ae530` holds only
  their merges): #64 (T9, 251f1e2), #66 (4a-0, 3fee335), #65 (4b-1, 427fa4e), #63 (6b-1, 9d159de), #67 (6b-2,
  12ebdf2), #70 (4a-1, faca750), #69 (6b-3, 7bab790), #62 (6a-1, f94afbb), #73 (6b-4, 308d0f0), #71 (6a-2,
  9973599), #68 (4b-2, 70b385e), #72 (6a-3, 87e7de9), #74 (4a-2, 5af531a), #75 (4a-2 b, 608f470), #76 (4b-3,
  80e7bf6), #77 (6a-4, 4350249), #79 (X2, 3682cfb), #82 (6a-5, ee57db0), #78 (5a, 9cce0ef), #83-#86 (5c, 44a32d6,
  d597a23, 049f948, 4cbe45e), #80 (5b-1, 1130603), #81 (5b-2, 2996563), #87 (5d-3, 3efd395), #88 (5d-2, 6833daa),
  #89 (5d-1, c0ae530). Stages 4, 5 and 6 closed against the §9 criteria. These were checked on `main` and in CI,
  not only taken from the PRs:
  - `src/MicroEngine.Migration.Entities` is absent, and `No_project_references_the_removed_entity_adapter` refuses it.
  - The template uses `Configure`.
  - The phone launch passes `--env` pairs through `HostEnvironment.Variables`, with no `SetEnvironmentVariable` in
    the hosts.
  - Declared capabilities: tiles `TileGeometry | OneWayPlatforms | MovingSolids`; Box2D the eight solver
    capabilities plus `OneWayPlatforms`.
  - Lavapipe in run 41973605483, and the Box2D jobs in run 41965084699.
  - The two failing `main` runs are the #79 flake, and #87's run failed on an HTTP 502.
  §9.1: item 2 met (dossier §12.4, the 3D slice in #62, #71, #72, #77); item 3 met (§12.5, the substitution in #73,
  with shared and specific capabilities separated); item 1 met with open items; item 4 is dossier §12. Open items:
  - ADRs 0035 and 0036 are not written (stages 0 and 1, criterion d).
  - `docs/guide/release-notes.md` lacks stages 2-6: drafted in `docs/evaluations/arch-1/release-notes-draft.md`,
    outside T10's surfaces.
  - The SDL licence file is missing from native packages and `MicroEngine.Sdl`.
  - Owner device validation.
  T10 also:
  - amended ADR 0030 (the adapter deleted) and ADR 0038 (phone `--env` as host variables; SDL browser host), and
    updated the index;
  - opened "Next release" in `docs/migrations.md` with a twelve-item upgrade checklist, and corrected two stale
    entries (fonts, `EntityList`);
  - named SDL in the notices' zlib text;
  - added the release note to `docs/releasing.md`;
  - corrected the dossier §9 stage 6 wording (mobile build only) and added §12.
  Not re-run by T10: any test or benchmark; the figures are the PRs' own. The Trail Duel typewriter details (`b_596`,
  23 local runs) come from the architect's brief. Next: review and merge T10; the owner decides the freeze and the
  release.
