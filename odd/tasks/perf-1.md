# PERF-1: Performance engineering, graphics backends and runtime optimization

Objective: a reproducible way to measure MicroEngine and measured fixes for the bottlenecks it finds, frame pacing first.
Branch `feat/perf-1`, from `main` at dc3770d (GAME-POLISH merged). Delivery: commits per work unit, PR for review, no
merge or release without the owner. Test runner: `dotnet test MicroEngine.slnx -c Release`; SDK: `node sdk/tests/*.mjs`.

## Specs

Quoted fragments are the owner's request (2026-11-01), verbatim.

- S1 Method: "Investigar → Reutilizar → Integrar → Desarrollar desde cero cuando sea necesario." "No quiero una
  optimización superficial basada en aumentar el contador de FPS." 2D only: "No implementes 3D durante esta misión,
  pero evita decisiones arquitectónicas que hagan innecesariamente difícil incorporar renderizado 3D en el futuro."
- S2 Every important optimization follows the ten steps (problem, reproduction, baseline, cause, existing solutions,
  implementation, re-measure, compare, functional regressions, keep or revert); "Reporta también resultados negativos".
- S3 Baseline with the four demos plus synthetic benchmarks and stress tests; record hardware, OS, configuration,
  backend, resolution, load and conditions. "No compares resultados obtenidos bajo condiciones incompatibles."
- S4 Frame pacing on Windows: "microtirones ocasionales incluso con un contador cercano a 100 FPS en un equipo potente"
  (Ryzen 5 7600X, RTX 4070, 32 GB, 100 Hz). Frame duration, interval variability, p50/p95/p99/p99.9, update, render,
  present wait, VSync, limiting, fixed/variable timestep, accumulation, GC, JIT, late loading, threads, backends. "No
  atribuyas el problema a VSync, SDL3, GPU integrada, GC o cualquier otra causa sin evidencia." "Distingue claramente
  los tiempos medidos por el motor de los tiempos reales de presentación que requieran herramientas externas."
- S5 Android, iOS and WebAssembly have high priority. "No utilices un emulador como sustituto de mediciones en
  hardware físico." WebGPU researched, "pero no presupongas que podemos reemplazar Raylib o WebGL de forma inmediata."
- S6 Evaluate SDL_Renderer, SDL_GPU, Direct3D 11/12, Vulkan, Metal, OpenGL/ES, raylib, WebGL, WebGPU and existing
  libraries, especially a 2D renderer on SDL_GPU. "No realices una migración masiva de renderer basada únicamente en
  benchmarks sintéticos."
- S7 Automatic graphics API selection: driver detection, preferences, capability checks, fallback on failed start,
  clear diagnostics, explicit developer configuration, log of the chosen implementation. "No asumas que Vulkan o
  Direct3D 12 siempre proporcionan mejor rendimiento". No per-OS game code to choose an API.
- S8 Multi-GPU machines: discovery, effective device, high-performance/power-saving, OS interaction, limits of
  SDL_Renderer/SDL_GPU/raylib. "Evita prometer selección física de GPU cuando una plataforma no permita controlarla."
  "No fuerces siempre la GPU dedicada".
- S9 Current renderer: draw calls, batching, texture/state changes, targets, copies, particles, text, temporary
  allocations, CPU/GPU sync. "No alteres innecesariamente los resultados visuales." No pixel-art-only assumptions.
- S10 Game loop and time: update rate, presentation, fixed timestep, interpolation, pause, input, limiting, slow-frame
  recovery. "No cambies indiscriminadamente las reglas temporales de juegos existentes."
- S11 Headless audio: "El modo headless predeterminado no debe emitir sonido físico ni depender de un dispositivo de
  audio instalado." Tests can capture samples to compare waves, durations, silences, amplitudes; an explicit mode keeps
  listening tests possible; no duplicated audio engine.
- S12 Startup phases (runtime, platform/device, audio, resources, texture upload, scenes, first frame, first input).
  "No introduzcas todavía un pipeline completo de assets: ese trabajo pertenece a ASSET-1."
- S13 Profiling: reuse existing tools; own instrumentation only for metrics the engine must expose consistently; no
  significant cost in normal builds; exportable, comparable results; documented procedure.
- S14 Public SDK kept; no 3D engine, no ECS; breaking public changes justified and documented. Internal records for
  decisions; public docs only when SDK capabilities change; "No publiques informes internos de benchmarking en la
  landing."
- S15 Validation: all tests; specific checks for pacing, loop, rendering, backend changes, fallback, adapters, memory,
  headless audio, startup, packaging, WebAssembly, mobile. "No inventes resultados ni extrapoles benchmarks de
  escritorio a teléfonos." "No reduzcas las comprobaciones de CI para hacer pasar optimizaciones."
- S16 Deliverables 1–16 of the request, ending in a PR ready for review.

## Tasks

| ID | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 | S11 | inline | done | 8f6f076 (AudioOutput, CaptureAudioDevice, --audio, --audio-capture, SdlAudioDevice.TryCreate(driver)) |
| T2 | S4 S13 | inline | done | 8f6f076 (FrameProfiler, --profile, IPlatformDiagnostics) |
| T3 | S12 S13 | inline | done | 8f6f076 + 257f8b4 (gamepads behind the start screen), e51f90e (songs in the background) |
| T4 | S3 | inline | done | 6085dc8 (benchmarks/PerfBench) |
| T5 | S3 S4 | inline | done (Windows only) | docs/evaluations/perf-1.md sections 1, 3 |
| T6 | S4 S10 | inline | done | 8f6f076 (display pacing, FrameBlend), c3653bc (raylib rate); demos do not interpolate yet |
| T7 | S7 S8 | inline | done | 318d70a (fallback, --gpu), 257f8b4 (adapter diagnostics) |
| T8 | S9 | inline | done | df612e3 (SDL renderer state), 018b74c (input allocations), c3653bc (raylib hidden VSync) |
| T9 | S5 S6 S8 | inline | done (desk + Windows); phones and web frame timing pending hardware | docs/evaluations/perf-1.md sections 5, 8, 10 |
| T10 | S14 S16 | inline | in progress | ADR 0023, public docs, PR |

## Log

- L1 (2026-11-01) The owner's request, verbatim: the PERF-1 mission text (sections 1–18 and the final acceptance
  criterion) as pasted in the session; its quoted fragments are in Specs.
- L2 Exploration: `GameLoop` (src/MicroEngine/Core/GameRunner.cs) runs fixed 60 Hz steps (`GameSettings.UpdateRate`
  default, no demo overrides it), up to five per frame, and renders once per frame with no interpolation. The machine
  of this session is the reference machine (Ryzen 5 7600X, RTX 4070 Ti SUPER plus Radeon iGPU, 31 GB, Windows 11),
  display reporting 74 Hz. No Android device or adb on this machine.
- L3 Measurements and findings are in docs/evaluations/perf-1.md (environment, baseline, three stutter causes, driver
  comparison, start, renderer, selection, audio). Tests were written after each implementation, not RED-first; the
  allocation test was checked RED (3,544 bytes per frame) before its fix.
- L4 Risk: item 4 (concurrency: songs render on the thread pool, e51f90e); item 3 is touched by requested behaviour
  changes (hidden/scripted runs silent, platform fallback, display pacing) recorded in docs/migrations.md. RDD is off
  (global), so no native review ran.
- L5 Not available here: Android/iOS hardware, adb, the wasm-tools workload, a visible browser pane for frame timing,
  PresentMon. Pending measurements are listed in the evaluation, section 10.
