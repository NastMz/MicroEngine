# GRAPHICS-2: 2D presentation, scaling and antialiasing

Objective: establish, with captures and frame times, what a developer can already choose about how a low-resolution
canvas reaches a large window, and implement the general-purpose hooks the evidence shows missing. Issue
NastMz/MicroEngine#110. Branch `feat/graphics-2`. Test runner: `dotnet test MicroEngine.slnx -c Release`.
Evaluation: [`docs/evaluations/graphics-2.md`](../../docs/evaluations/graphics-2.md).

## Specs

- S1 Opportunity: "Neon Rift's low-resolution virtual canvas can look overly pixelated or blurry when enlarged to a
  1080p fullscreen display. Antialiasing is not necessarily the remedy: internal resolution, texture filtering, upscale
  method and reconstruction solve different problems."
- S2 Configuration: "Establish each demo's actual virtual resolution and Presentation configuration; reproduce the
  reported fullscreen/windowed differences."
- S3 Comparison: "Compare nearest/linear scaling, integer/non-integer scaling, suitable AA/upscalers, hybrid-resolution
  world/effects/UI, and visual/performance tradeoffs."
- S4 Existing layers: "Check what the rebuilt Presentation and SDL_GPU layers already allow; favor developer-controlled
  presentation rather than built-in universal filters."
- S5 Evidence: "Use reproducible captures and frame-time evidence while preserving intentionally crisp pixel-art
  styles."
- S6 Done when: "Document feasible developer-facing options and exact missing general-purpose hooks, with a comparison
  on Neon Rift; do not assume AA should be applied."
- S7 Judge by an external developer (owner's standing rule for research issues, as TILE-1 S6): research issues end in
  implementation; the bar is whether an external developer could build that game today with the published SDK. Only
  additive public API; anything that breaks existing public API or changes the demos' default visuals is described
  under "Needs owner decision" instead of implemented.
- S8 Neon Rift's default (owner, L5): "Mantener el aspecto predeterminado de Neon Rift por ahora."
- S9 Real captures (owner, L5): "Generar capturas REALES del renderer comparando nearest, smooth y prescale/sharp a
  1366×768 y 1920×1080, con el mismo frame, para evaluar visualmente el resultado."
- S10 Fit (owner, L5): "Corregir `Presentation.Fit` si es un ajuste localizado, con pruebas de dimensiones impares,
  centrado y bordes."
- S11 Prescale everywhere (owner, L5): "Completar prescale en headless y raylib."
- S12 Criterion (owner, L5): "corregir problemas reales dentro del lote cuando el alcance sea razonable. No ampliar
  innecesariamente la arquitectura ni crear issues preventivos. Si algo exige un rediseño considerable, presenta
  evidencia, alcance y alternativas antes de implementarlo. Distingue expresamente los fallos demostrados, las
  hipótesis y las diferencias perceptibles para el usuario."

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Map each demo's canvas and presentation, and the compose path of every backend | S2 S4 | inline | done | evaluation §1, §2 |
| T2 Captures and frame times of the scaling options on Neon Rift | S1 S3 S5 | inline | done | evaluation §3; sheets and profiles inspected (L3) |
| T3 `Presentation.Prescale`, `PrescaleFor`, `CanvasPresenter`, the SDL platform, tests | S6 S7 | inline | done | `f01e2f1`; RED: the tests did not compile without the API; `CanvasPresenterTests` 9/9, `SdlPrescaleTests` 1/1 |
| T4 `RIFT_PRESENT` on Neon Rift for the comparison | S6 | inline | done | `ac20064`; RED: `sharp` gave the default; `LaunchOptionsTests` 4/4 |
| T5 Evaluation, release notes, architecture table, API reference | S6 | inline | done | this branch |
| T6 Pin Neon Rift's default presentation | S8 | inline | done | already pinned by `LaunchOptionsTests` (`ac20064`); the default's presented picture equals nearest at both sizes (L7) |
| T7 `Presentation.Fit` returns whole-pixel frames (own commit) | S10 S12 | inline | done | `c40ef78`; RED: 4 of the 5 new `Fit` tests failed (the whole-factor one passed before and after) |
| T8 Prescale in `DeviceCanvas.Compose`, the headless `ScreenCapture.Compose` and raylib; manual row; release note | S11 | inline | done | `35f31da`; RED: device canvas `E6001A` for red, headless blended, raylib 170 for 212 |
| T9 Captures of the presented image from the real device, three options at two window sizes, same frame | S9 S12 | inline | done | `a36bfde` (readback and its test); pictures outside the repository (L7) |
| T10 Evaluation updated: demonstrated failures, hypotheses, perceptible differences | S12 | inline | done | evaluation §5 |

## Log

- L1 (2026-11-13) Issue #110, verbatim:

  > ## Opportunity
  > Neon Rift's low-resolution virtual canvas can look overly pixelated or blurry when enlarged to a 1080p fullscreen
  > display. Antialiasing is not necessarily the remedy: internal resolution, texture filtering, upscale method and
  > reconstruction solve different problems.
  >
  > ## Investigate
  > - Establish each demo's actual virtual resolution and Presentation configuration; reproduce the reported
  >   fullscreen/windowed differences.
  > - Compare nearest/linear scaling, integer/non-integer scaling, suitable AA/upscalers, hybrid-resolution
  >   world/effects/UI, and visual/performance tradeoffs.
  > - Check what the rebuilt Presentation and SDL_GPU layers already allow; favor developer-controlled presentation
  >   rather than built-in universal filters.
  > - Use reproducible captures and frame-time evidence while preserving intentionally crisp pixel-art styles.
  >
  > ## Done when
  > Document feasible developer-facing options and exact missing general-purpose hooks, with a comparison on Neon Rift;
  > do not assume AA should be applied.
  >
  > **Status:** Research candidate · **Timing:** after release; related to GRAPHICS-1.
- L2 (2026-11-13) Mapping: every demo uses `Presentation.PixelArt`; Neon Rift is 480 x 270 (4x exact at 1080p), the
  other three 320 x 180 (6x exact). Neon Rift goes fractional with nearest at 1366 x 768 (2.84x): pixels two or three
  window pixels wide. Rounding, upscale sampler, texture sampler, canvas size and the window-resolution UI view already
  exist; the 2D batcher has no custom shader and targets have no sample count. Missing: even pixels at a scale that is
  not whole. Decision: a prescale field on `Presentation`, two passes without a shader, so it runs on both devices.
- L3 (2026-11-13) Evidence looked at: crops of the HUD and a drone at 1366 x 768 and 1920 x 1080 from the reference
  device (nearest uneven, linear blurred at every scale, prescale even with one blended pixel; identical to nearest at
  4x, checked byte for byte) and from the real Direct3D 12 window at 1366 x 768 captured from the screen (same result).
  Frame interval p50: 1.48 ms nearest, 2.07 ms with the extra pass at 1366 x 768; no difference at 1080p.
- L4 (2026-11-13) The edit surface of this change excludes `src/MicroEngine/**` and `docs/manual/**`, so
  `DeviceCanvas.Compose`, the headless `ScreenCapture.Compose`, the raylib backend and the manual's presentation table
  are untouched: the SDL platform composes through `CanvasPresenter` only when a prescale is set, and the default path
  is the one it had. Listed in the evaluation §4 for the owner, with the other hooks not implemented (a canvas rendered
  above its size, a shader on the 2D or present pass, snapping fractional frames, Neon Rift's default). No ADR: one
  additive presentation field. `AGENTS.md` not edited (its record list is one line every parallel branch would touch).
- L5 (2026-11-13) Owner's request, verbatim:

  ```
  GRAPHICS-2

  * Mantener el aspecto predeterminado de Neon Rift por ahora.
  * Generar capturas REALES del renderer comparando nearest, smooth y prescale/sharp a 1366×768 y 1920×1080, con el mismo frame, para evaluar visualmente el resultado.
  * Corregir `Presentation.Fit` si es un ajuste localizado, con pruebas de dimensiones impares, centrado y bordes.
  * Completar prescale en headless y raylib.

  Criterio general: corregir problemas reales dentro del lote cuando el alcance sea razonable. No ampliar innecesariamente la arquitectura ni crear issues preventivos. Si algo exige un rediseño considerable, presenta evidencia, alcance y alternativas antes de implementarlo. Distingue expresamente los fallos demostrados, las hipótesis y las diferencias perceptibles para el usuario.
  ```
- L6 (2026-11-13) `Fit`: its consumers (SDL and raylib frame and pointer mapping, headless `WorldFrame`, touch layout)
  use the frame as a continuous rectangle, so the fix is local: the size in whole pixels, the position rule as it
  was. Demonstrated on the reference device: a 1244.44-wide frame painted 1244 columns with 60 and 62 of letterbox;
  no blending at the edge (the earlier note in the evaluation was wrong). Prescale: `DeviceCanvas.Compose` delegates
  to `CanvasPresenter` only when `Prescale != 1`, `ScreenCapture.Compose` takes the presentation; the rows presented
  without a prescale were recorded before the change and pinned.
- L7 (2026-11-13) Real captures: `SdlGpuDevice.ReadPresented` (internal) reads the surface texture `Present` copies
  to the swapchain. Eight runs of Neon Rift stage 1 under the autopilot, deterministic, seed 1, update 750, hidden
  borderless window: canvas SHA-256 `A023601C12E93E34` in all. Default = nearest at both sizes, sharp = nearest at
  1920 x 1080 byte for byte; at 1366 x 768 nearest has strokes of 2 and 3 pixels, smooth is soft, sharp is even with
  a one-pixel soft edge. All pictures and 4x crops were looked at. Not captured: the SDL renderer device (the
  desktop default), a raylib window, motion. The release note keeps to the presentation entry (the `Fit` change is
  written there); a line under Fixed and `docs/migrations.md` are outside this change's surface.
