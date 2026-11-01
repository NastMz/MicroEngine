# PLATFORM-3: Multiplatform backend architecture

Objective: official, evidence-selected stacks for Windows, Linux, macOS, Android, iOS and the browser, behind contracts
that let a host replace a platform or one of its subsystems without touching game logic. Branch `feat/platform-3`
(from `main`). Delivery: `ask-on-risk`. Test runner: `dotnet test MicroEngine.slnx -c Release`.

## Specs

- S1 Six platforms: "MicroEngine debe soportar: Windows. Linux. macOS. Android. iOS. Navegador mediante WebAssembly."
  "Ninguna de estas plataformas es opcional." "No declares una plataforma terminada simplemente porque compila o pasa
  pruebas headless." "El objetivo es ejecutar videojuegos reales con renderizado, input, audio, carga de recursos y un
  ciclo de vida funcional."
- S2 Technology selection by evidence: "Construye pruebas mínimas y reproducibles de las alternativas relevantes."
  "Reutiliza las evidencias de PLATFORM-1 y PLATFORM-2." "No mantengas una dependencia por inercia." Compare by the
  criteria of section 7 (compatibility, features, performance, stability, maintenance, bindings, integration, native
  dependencies, licence, diagnostics, adapter cost, replaceability). Reuse libraries before writing infrastructure.
- S3 Profiles: official profiles per platform, any organisation "técnicamente justificada"; "No debes crear esas clases
  por anticipación." A profile may combine libraries per subsystem.
- S4 `IPlatform` evolution: "Parte de los contratos existentes." "No reemplaces interfaces funcionales simplemente para
  introducir un patrón arquitectónico nuevo." Relation kept: "Game → GameRunner / GameSession → IPlatform →
  subsistemas → bibliotecas y APIs externas". "La selección de plataforma corresponde al host o a la configuración de
  ejecución, no a las reglas del videojuego."
- S5 Composition: replace rendering, graphics device, audio, input, window/events, clock, lifecycle and OS services
  where viable; real couplings "explícitamente mediante contratos, capacidades o grupos de componentes compatibles".
  "Evita prometer combinaciones técnicamente imposibles." No DI container "únicamente por anticipación".
- S6 Selection: "Utilizar automáticamente la plataforma oficial recomendada. Seleccionar un perfil alternativo
  disponible. Configurar componentes compatibles. Registrar implementaciones personalizadas." "No necesitamos hot
  swapping de backends durante la ejecución del juego." "Evita acoplar `Game` a nombres de sistemas operativos o
  tecnologías de terceros."
- S7 Extensibility proof: "1. Ejecutar un mismo juego utilizando dos implementaciones reales de `IPlatform` o dos
  perfiles tecnológicamente distintos" "2. Sustituir al menos un subsistema mediante una implementación alternativa
  real y compatible." "3. Comprobar que no es necesario reescribir escenas, reglas o lógica del juego." "4. Validar
  errores claros al utilizar configuraciones incompatibles." "Los mocks pueden complementar las pruebas, pero no son
  prueba suficiente de intercambiabilidad real."
- S8 Optional alternatives: "La parte C puede concluir legítimamente con cero implementaciones adicionales."
- S9 Game validation: the four games keep their behaviour on the official platforms (rules, scenes, rendering and
  scaling, audio, input, persistence, resources, start/stop, pause and lifecycle changes); tolerances for legitimate
  GPU differences; deterministic results stay equivalent; "Los problemas específicos de un juego no deben corregirse
  introduciendo excepciones artificiales en el motor."
- S10 CI: distinguish build, unit, headless, native rendering, browser, Android emulator, iOS simulator, real hardware;
  "No confundas soporte de compilación con soporte funcional." Backend-only changes must not rebuild unrelated
  implementations. "No rediseñes innecesariamente la infraestructura CI/CD ya implementada."
- S11 Completion and honesty: "Si una plataforma requiere hardware físico para una validación que no puede realizarse en
  GitHub Actions, identifica exactamente la evidencia obtenida y la pendiente. No inventes resultados".
- S12 Deliverables: the 14 items of section 17 (inventory, comparison, prototypes, decision, implementation, contract
  evolution, selection, substitution tests, matrix, game validation, CI, docs, ADRs, limitations).

## Tasks

| ID | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 | S4 S5 S6 | inline | done | 39a260e; RED: compile errors, GREEN: 106 engine tests |
| T2 | S1 S2 S3 | inline | done | 93ebf12; 138 captures on SDL, D3D11/GL/Vulkan identical |
| T3 | S6 S7 | inline | done | 93ebf12; `--platform`, `--list-platforms` run |
| T4 | S7 S9 | inline | done | 93ebf12, fd29e65; `PlatformEquivalenceTests` 5/5 Windows, 4/5 Linux (no audio backend in WSL) |
| T5 | S1 S9 | inline | done | a89ce95; emulator: 7/7 levels for Meadow Run and Neon Rift |
| T6 | S1 S9 | inline | done | bd1ff48 and fixes; simulator: 2 games, 5/5 levels (run 41253809116) |
| T7 | S1 S10 | inline | done | dccaed1 and fixes; macOS and platform jobs green (run 41253809116), Native Linux green (run 41248136986) |
| T8 | S10 S11 S12 | inline | done | docs commit; site build and tests pass |

- T1 Core contracts: platform profiles, catalog, request with subsystem overrides, composite input, lifecycle
  (suspend/resume) in `GameLoop`; tests.
- T2 `MicroEngine.Sdl`: SDL3 backend (window, SDL_Renderer into a virtual target, textures, keyboard, gamepad, touch,
  audio through SDL audio streams, OGG through NVorbis, captures).
- T3 Backend-neutral desktop launcher (`--platform`), default profile per system; games' hosts use it.
- T4 Substitution and equivalence: same scripts on two platforms, audio substitution, incompatible-configuration errors.
- T5 Android host on SDL3 (`SDLActivity`), touch, lifecycle, content files from the APK; emulator run.
- T6 iOS host on SDL3 (UIKit, Metal), simulator run with captures.
- T7 macOS native SDL run; CI jobs for the new profiles.
- T8 Compatibility matrix, evaluation, ADRs, developer docs, limitations.

## Log

- L1 (2026-10-31) Original request: PLATFORM-3 brief "MicroEngine — PLATFORM-3: Multiplatform Backend Architecture"
  (sections 1–18 as pasted by the owner; quoted verbatim in the specs above where wording matters).
- L2 Exploration: core has no raylib reference; `IPlatform` bundles graphics, audio, input, clock, frame and window
  calls; `RaylibPlatform` serves desktop, browser and the experimental Android host; iOS runs headless (raylib 6.0
  has no iOS platform); macOS runners have no accelerated OpenGL (GLFW requires it). `ppy.SDL3-CS` 2026.1002.1 (MIT,
  SDL 3.5.0) ships SDL3 for win-x64/x86/arm64, linux-x64/x86/arm64/arm, osx-x64/arm64, android (arm, arm64, x86,
  x64, with `SDL3-CS.aar`) and an iOS xcframework (device and simulator). NVorbis 0.10.5 (MIT) decodes OGG in managed
  code. Content formats in games: PNG, WAV, OGG. Local: .NET 10.0.403, android and ios workloads, Android SDK with
  emulator, no Mac.
- L3 Prototype results (details in `docs/evaluations/platform-3.md`): SDL vs raylib on Windows, 15 scenarios, RGB
  identical in 11, the rest differ at half-pixel and rotated translucent sprites (worst cell 7); SDL Direct3D 11,
  OpenGL and Vulkan pixel-identical; SDL software renderer worst cell 15. Decision: SDL default on the five native
  systems, raylib for browsers and as a desktop alternative, `sdl-software` official alternative.
- L4 Android on SDL (local emulator API 36.1): first run failed lifecycle and touch. Causes: SDL turned the window to
  portrait (fixed: orientation hint); background/foreground events reached the loop together (fixed: event watch and
  a latch); `adb input tap` presses and releases within one frame (fixed: tap latch in `TouchPad`, test added).
  Then 7/7 levels for Meadow Run and Neon Rift; captures pixel-identical to Windows SDL.
- L5 `desktop.mjs` already used `--platform` to label references; the new option is `--backend` to keep that meaning.
- L6 Linux (WSL2 Ubuntu 24.04, private .NET and Node): with llvmpipe forced, raylib reproduces `linux-x64.json`
  138/138; SDL needs its own reference (`sdl-linux-x64.json`, 112/138 within the GPU reference's tolerance). Found and
  fixed a test bug (unavailable render driver failed in required mode). SDL audio: no backend in this WSL image.
- L7 Browser: dev packages for Meadow Run and Neon Rift built from the branch; Chrome replayed 7 scripts, 25/25
  captures byte-identical to desktop raylib. Packages carry about 150 KB (Brotli) of unused SDL code.
- L8 Open: macOS and iOS runs and the new CI jobs need GitHub runners; this branch is not pushed (owner decision).
- L9 (2026-10-31) Owner: "Push experiment branch". Pushed `experiment/platform-3` (throwaway). Validate run 41248136986:
  Linux and Windows .NET and Native Linux (SDL and raylib references) passed; macOS hung 45 min in window tests
  (Cocoa main thread), Android failed (`set -o pipefail` under sh), iOS `DllNotFoundException: SDL3`. Fixed.
- L10 Owner: iOS ran 30 min. Cause: SDL keeps the app alive after the game returns; each launch hit its 300 s limit.
  Fixed (scripted runs exit). macOS SDL: 138 captures, 25 beyond the GPU reference (half-pixel HUD, rotated sprites);
  recorded `sdl-macos-arm64.json`. Platform run 41253809116: macOS, Android and iOS all green; macOS reproduced its
  reference 138/138; iOS Meadow Run 13/13 and Neon Rift 12/12 within the SDL reference, audio, lifecycle.
- L11 (2026-11-01) Owner: "el repo ya es público ... no limites las pipelines por ese tema ... todo lo que esté
  perdiendo tiempo necesariamente hay que optimizarlo, no solo en iOS, sino también en Mac, en Windows, en Linux, en
  Android, en navegador web". Measured every job, then: parallel scenario runs in `desktop.mjs`, parallel browsers and
  Chrome layout pages in `browser.mjs`, one Android and iOS job per game, pinned Xcode, event-driven iOS waits.
  Validation 41257531549 failed the site (Firefox BiDi lost concurrent contexts: Firefox back to one page) and showed
  the iOS boot during the build slowed it (reverted). Run 41258648571 green: site checks 186 s → 115 s, iOS about
  8.5 min per game (was 18 min for both serially); macOS 197 s → 98 s, Android 404 s → 258/316 s (run 41257531549).
  Windows and Linux .NET tests (about 70-80 s, mostly the build) and cached web builds (about 2 min) left as they are.
