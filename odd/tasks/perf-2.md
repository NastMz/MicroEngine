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

## Tasks

| ID | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 | S9 S1 | inline | pending | FrameBlend is 1 in fixed-step runs (Lerp at 0 drew the previous state); pacing and capture re-checked |
| T2 | S5 S6 | inline | pending | |
| T3 | S1 | inline | pending | four demos draw between updates; fixed-step captures unchanged |
| T4 | S4 S8 | inline | pending | |
| T5 | S2 S3 | inline | pending | PresentMon, gamepad start |
| T6 | S7 | inline | pending | |
| T7 | S10 S11 | inline | pending | graphics options API, VSync, frame limit, window mode, display, runtime changes |
| T8 | S12 S13 | inline | pending | |
| T9 | S14 | inline | pending | evaluation, GRAPHICS-1 decision |
| T10 | S15 | inline | pending | Android (emulator only: no device connected), WebAssembly |
| T11 | S16 | inline | pending | evaluation, ADR, docs, checks, PR |

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
