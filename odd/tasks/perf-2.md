# PERF-2: Performance hardening and graphics configuration

Objective: close PERF-1's open items and give games control over their graphics options (backend, renderer, VSync,
frame limit, GPU, window mode, display). Branch `feat/perf-2`, from `feat/perf-1` at 29aca83 (PR NastMz/MicroEngine#19,
not merged). Delivery: commits per work unit, one PR stacked on PERF-1 (as PERF-1, splittable on request), no merge or
release without the owner. Test runner: `dotnet test MicroEngine.slnx -c Release`; SDK: `node sdk/tests/*.mjs`.

## Specs

Quoted fragments are the owner's request (2026-11-02), verbatim.

- S1 "Integrar correctamente `Game.FrameBlend` en las cuatro demos para mejorar la fluidez en monitores de distintas
  frecuencias."
- S2 "Investigar el refresh perdido aproximadamente 1,9 segundos después de abrir una ventana SDL."
- S3 "Investigar y optimizar el tiempo de inicialización del subsistema de gamepads."
- S4 "Reducir las asignaciones de memoria evitables que permanecen en las demos."
- S5 "Corregir el comportamiento de `SceneManager` cuando no existen escenas."
- S6 "Revisar los errores tardíos durante la generación de música en segundo plano."
- S7 "Investigar los cinco fallos ambientales identificados en PERF-1." (the five `no build-machine path` failures of
  `sdk/tests/packages.mjs` on a local pack: MicroEngine, MicroEngine.Sdl, MicroEngine.Raylib, MicroEngine.Desktop,
  MicroEngine.Cli).
- S8 "Resolver la variabilidad de las capturas visuales cuando corresponda."
- S9 "Verificar las correcciones existentes de pacing y captura de audio."
- S10 Graphics configuration: "Backend gráfico." "Selección automática y explícita de renderizador." "Fallback entre
  implementaciones disponibles." "Sincronización vertical configurable." "Límite de FPS independiente de VSync."
  "Frecuencia de actualización de la simulación." "Preferencias de GPU." "Configuración de pantalla y fullscreen."
  "Selección de monitor cuando corresponda." "Estas opciones deben poder utilizarse desde un juego distribuido,
  incluyendo su configuración gráfica durante la ejecución cuando la plataforma lo permita."
- S11 "Permite configurar VSync desde el SDK y evalúa su comportamiento con distintos renderizadores, frecuencias de
  monitor, límites de FPS y modos de actualización." "La sincronización vertical, el límite de fotogramas y la
  frecuencia de simulación deben ser conceptos independientes."
- S12 Multi-monitor: "El equipo de referencia dispone de un monitor principal de 100 Hz y dos secundarios de 75 Hz.
  PERF-1 detectó 75 Hz." "Revisa la identificación de la pantalla utilizada, los cambios de monitor durante la
  ejecución, las frecuencias de actualización, el escalado y la interacción con el frame pacing."
- S13 Multi-GPU: "Investiga la selección de adaptadores, las preferencias de rendimiento y ahorro energético, la
  identificación de la GPU utilizada y las capacidades ofrecidas por los backends existentes."
- S14 Modern APIs: evaluate "Direct3D, Vulkan, Metal, OpenGL, OpenGL ES, SDL_Renderer, SDL_GPU, Raylib, WebGL y WebGPU,
  junto con proyectos y bibliotecas existentes que puedan reutilizarse." "No extrapoles la superioridad de un backend en
  benchmarks 2D a escenarios con shaders, efectos avanzados o futuras capacidades 3D." "Si una evolución requiere una
  implementación considerable, determina si corresponde abordarla aquí o mediante una misión GRAPHICS-1 independiente."
- S15 Cross-platform: "con especial atención a Android y WebAssembly." "Aprovecha los entornos, herramientas, MCPs,
  runners, emuladores, simuladores y dispositivos disponibles según su utilidad para cada investigación." "Analiza
  frame pacing, tiempos de carga, memoria, renderizado, estabilidad y regresiones."
- S16 "Para las correcciones y mejoras relevantes, establece una línea base, implementa los cambios y compara
  resultados." "Ejecuta las pruebas de regresión correspondientes y aprovecha la infraestructura de CI existente."
  "Documenta las decisiones técnicas, mejoras realizadas y pendientes que permanezcan abiertos."

- S17 (2026-11-02, review before the PR) WebAssembly music freeze: "Investiga su causa real, implementa una solución y
  comprueba que la generación o incorporación de recursos musicales no produzca interrupciones perceptibles durante el
  gameplay." "Conserva las mejoras ya obtenidas en los tiempos de carga, pero prioriza la fluidez sostenida sobre la
  velocidad de arranque."
- S18 "Valida visualmente la implementación de `Game.FrameBlend` en las cuatro demos." (characters and enemies, cameras,
  animations and particles, scene transitions, consistency across update and presentation rates). "Las pruebas
  automatizadas y las capturas deben complementar la validación visual, no sustituirla."
- S19 "Valida el comportamiento del motor con los tres monitores disponibles: 100 Hz, 74,97 Hz y 60 Hz." "Comprueba el
  movimiento de ventanas entre pantallas, los cambios de frecuencia, la sincronización vertical y los límites de FPS."
  "una presentación lo más uniforme posible, independientemente del monitor utilizado."
- S20 GameInput: "La compatibilidad funcional con dispositivos de entrada tiene mayor importancia que ahorrar ese
  segundo." "Comprueba el funcionamiento de los mandos con los recursos de prueba disponibles, incluyendo conexión,
  desconexión y reconocimiento de dispositivos." "No sacrifiques capacidades funcionales únicamente para mejorar el
  tiempo de inicio."
- S21 Alpha APIs: "No introduzcas capas de compatibilidad, adaptadores obsoletos ni complejidad adicional únicamente para
  conservar comportamientos antiguos." "Actualiza los consumidores, las plantillas, los ejemplos, las pruebas y la
  documentación según corresponda."
- S22 Sustained performance: frame-time stability, gameplay micro-stutter, allocations and GC, main-thread work, audio
  and resource generation, simulation/render/presentation interaction, platforms and backends. "Los tiempos de arranque
  son secundarios, salvo cuando exista un problema realmente significativo."
- S23 Delivery: verify engine, SDK and demo suites; update the report; coherent commits; "Publica la rama y abre el PR
  para activar el CI." "Revisa los resultados del CI y corrige los fallos atribuibles a esta rama." "No hagas merge ni
  publiques nuevas versiones sin autorización."

## Tasks

| ID | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 | S9 S1 | inline | done | 1b2aae9 (FrameBlend 1 in fixed-step runs); pacing and capture fixes re-checked by their tests and on hardware |
| T2 | S5 S6 | inline | done | e4f0dae (empty scene stack; songs validated in AddMusic), tests checked RED first |
| T3 | S1 | delegated (4 parallel writers, one per demo, own worktrees: Writer trigger a) | done | b759ee3, fcc1f19, 63c93ae, ca1cbdb; captures byte-identical per demo |
| T4 | S4 S8 | delegated (same writers) + inline (engine) | done | 870bbbe, a6ed56a, 836918a, 5133812 (demos); abdc34c, edddd0e (engine); S8: capture variance was saved achievement progress, none with a fresh save folder; 138/138 shared captures identical to e4f0dae |
| T5 | S2 S3 | inline | done | PresentMon: DWM promotes the window to independent flip ~0.3 s after its first frame on DISPLAY1; 51cece6 (GameInput off: gamepad start 1,070 -> 110 ms) |
| T6 | S7 | inline | done | ddc67b7: the five failures were a local pack without -p:MicroEngineRelease=true; with it every package check passes |
| T7 | S10 S11 | inline | done | 92550f9 (GraphicsSettings, launcher options, Game.Screen, frame limit), ebf5fe6 (docs, ADR 0024); measured in docs/evaluations/perf-2.md section 4 |
| T8 | S12 S13 | inline | done | 2f4263c (pace only while frames arrive at the reported rate); evaluation sections 5-6 |
| T9 | S14 | inline | done | evaluation section 7; GRAPHICS-1 decided as a separate mission (ADR 0024) |
| T10 | S15 | inline | done | 291d6a1, f720501 (songs render in slices in browsers); Android emulator start, memory, stability; evaluation section 8 |
| T11 | S16 | inline | done | docs/evaluations/perf-2.md; all suites pass (native included); PR pending the owner |
| T12 | S17 | inline | pending | |
| T13 | S20 | inline | pending | |
| T14 | S18 | inline | pending | |
| T15 | S19 | inline | pending | |
| T16 | S21 | inline | pending | |
| T17 | S22 | inline | pending | |
| T18 | S23 | inline | pending | |

## Log

- L1 (2026-11-02) The owner's request, verbatim: the PERF-2 mission text (sections 1–8) as pasted in the session; its
  quoted fragments are in Specs.
- L2 Environment checked: displays DISPLAY3 primary 1920x1080 at 100 Hz (LG FHD), DISPLAY1 left at 75 Hz, DISPLAY2
  right at 60 Hz (not 75 as stated in S12; reported to the owner); RTX 4070 Ti SUPER plus Radeon iGPU. `adb devices`
  lists no device; one emulator image (Medium_Phone_API_36.1). PresentMon 2.6.0, adb and the wasm-tools workload are now
  installed.
- L3 Found while exploring: PERF-1 documented `FrameBlend` as 0 in fixed-step runs "so captures stay exact", but with
  the documented `Lerp(previous, current, FrameBlend)` a blend of 0 draws the previous update: adopting it would shift
  every capture by one step. Fixed in T1.
- L4 T3/T4 routing: the four demos are independent units with disjoint edit surfaces (games/<Name>, tests/<Name>.Tests),
  each several minutes of work: four writers ran in parallel in worktrees under C:/Proyectos/MicroEngine-worktrees,
  their commits cherry-picked. Each kept every scripted capture byte-identical (Meadow 43/44 with the 44th varying by
  itself: saved achievement progress; Courtyard 43/43; Trail 118/118; Neon 107/107). Allocations per frame: Meadow level
  2,816 -> 384 B (with the engine fixes), Neon stage 14,176 -> ~1,010 B, Courtyard road 2,270 -> 469 B, Trail route
  22,361 -> 286 B.
- L5 Gamepad start: sampling the main thread during SDL_InitSubSystem(GAMEPAD) showed GameInputRedist.dll
  GameInputInitialize sleeping ~950 ms (GameInputSvc installed but stopped here). SDL starts GameInput unless both
  SDL_JOYSTICK_GAMEINPUT and SDL_JOYSTICK_GAMEINPUT_RAW are 0. Not tested with a physical controller (none connected).
- L6 Missed refresh (S2): PresentMon 2.6 runs without elevation here. Windowed SDL D3D11 presents as Composed: Flip
  (display latency ~29.5 ms at 100 Hz). On DISPLAY1 (75 Hz) DWM promotes the window to Hardware Composed: Independent
  Flip ~0.3 s after the first frame; 1-3 frames are shown 20-42 ms at that moment, and the engine sees one 26.5 ms frame.
  PERF-1's "1.9 s after the window appears" = ~1.5 s of gamepad start before the first frame + 0.3 s. Borderless full
  screen reaches independent flip from the third frame (latency 19.7 ms); SDL's exclusive mode stays composed here.
- L7 Multi-monitor (run): a window composed by the desktop runs at the 100 Hz primary's rate on the 60 Hz display, and
  a window moved from the primary to the 75 Hz display keeps 100 Hz (re-applying VSync did not change it); full screen
  and windows promoted to independent flip run at their display's rate. Counted in the reported display's periods, drawn
  time alternated 16.7 / 3.3 ms; the loop now paces only while the median of the last 16 frames matches (2f4263c).
- L8 WebAssembly (run, headed Chrome, 100 Hz): frame pacing perfect in all four demos and frame work 0.6-1 ms, but Play to
  playing took 5.0-8.7 s in the three demos with synthesized music (Neon Rift, music from files: 1.0 s): every song
  rendered inside Load on the single thread. Songs now render in slices from AudioMixer.Update. Mono AOT for wasm
  crashed on SDL3-CS.dll with the pinned toolchain (not adopted).
- L9 S7: the "five environmental failures" are packages.mjs's no-build-machine-path check after a local pack without
  -p:MicroEngineRelease=true (ContinuousIntegrationBuild maps paths only in CI or with that flag).
- L10 Android: emulator only (no phone connected); Meadow Run cold start 0.96-1.55 s (am start -W), engine start to title
  2.15 s, PSS 159 MB, no fatal error in a 20 s run. Not a phone measurement.
- L11 (2026-11-02) The owner's review before the PR, verbatim: the seven-point text pasted in the session ("Antes de
  abrir el PR, quiero resolver y validar los siguientes aspectos." ... "No queremos sacrificar compatibilidad o calidad
  arquitectónica para obtener mejoras marginales en los tiempos de arranque."); quoted fragments in S17-S23. It reverses
  the GameInput decision of 51cece6 and authorizes pushing the branch and opening the PR.
