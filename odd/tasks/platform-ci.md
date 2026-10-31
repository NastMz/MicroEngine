# PLATFORM-2: Cross-platform validation infrastructure and efficient CI

Objective: run only the checks a change needs, and gather reproducible evidence for macOS, Android and iOS.
Branch `feat/platform-ci` (from `origin/main` after PLATFORM-1, #12). No push of this branch, no PR, no merge; runner
experiments go through the throwaway branch `experiment/platform-2` (owner approval, L2).
Test runners: `dotnet test MicroEngine.slnx -c Release`; `node --test .github/scripts` for the change planner.

## Specs

- S1 Audit: "Duración de cada job. Dependencias entre jobs. Compilaciones redundantes. Cachés disponibles. Pruebas que
  realmente necesitan ejecutarse. Coste estimado de los runners. Qué cambios deben invalidar los artefactos
  existentes." "No elimines pruebas importantes para reducir tiempos. Prioriza evitar ejecuciones innecesarias."
- S2 Tiers: "CI rápido: pruebas unitarias, simulación headless y validaciones pertinentes al cambio." "CI de
  integración: compilaciones, pruebas gráficas nativas, navegador y regresiones por plataforma." "CI multiplataforma
  completo: emuladores, simuladores, pruebas de compatibilidad y verificaciones costosas." "El pipeline debe
  identificar qué componentes cambiaron y ejecutar solamente las verificaciones necesarias." Reuse the content keys
  where appropriate. Full validation by `workflow_dispatch` and, if convenient, a schedule. "Los fallos de plataformas
  experimentales deben quedar visibles y diagnosticados sin bloquear automáticamente la publicación de plataformas
  estables."
- S3 macOS: "Investiga el fallo exacto de GLFW al crear un contexto OpenGL." Runner limits vs context configuration vs
  backend incompatibility. Reproducible alternatives "sin modificar el motor a ciegas". "No introduzcas parches
  invasivos en GLFW ni sustituyas Raylib únicamente para satisfacer el CI." Keep managed and headless tests on macOS.
  Document what is automatic and what needs real hardware.
- S4 Android feasibility: evaluate .NET for Android, raylib with the NDK, current bindings, lifecycle, assets, touch and
  gamepad, audio, persistence, APK. A minimal prototype with the real engine if viable; install and run it on an
  emulator in GitHub Actions. "No declares soporte Android completo por conseguir compilar un APK."
- S5 iOS feasibility: toolchain, bindings, graphics init, lifecycle, assets and storage, audio and input, AOT and
  interop limits, simulator install and run, on a macOS runner. "No asumas que el soporte Android de raylib implica
  automáticamente soporte iOS." Keep evidence if the simulator cannot run the backend.
- S6 Architecture: same gameplay code everywhere; Android/iOS specifics in hosts or platform adapters; "No dupliques
  los juegos." No speculative universal architecture; no SDL3 or Box2D; document raylib's structural limits exactly.
- S7 Validation: existing Windows, Linux and WebAssembly tests keep passing. For mobile, separate: toolchain
  configured, project compiled, package generated, app installed, app started, rendering checked, input and audio
  checked, real game run. "No equipares una compilación exitosa con compatibilidad funcional."
- S8 Delivery: CI improvements and viable experiments; expensive cross-platform checks out of the publishing path
  until stable; docs and roadmap updated with verified results; commits on a separate branch, no push or merge.
  Report: CI before/after, execution matrix by change type, macOS/Android/iOS results, prototypes, blockers,
  backend recommendations.

## Tasks

| ID | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 | S1 | inline | done | docs/evaluations/platform-2.md section 1 (run data 2026-10-31) |
| T2 | S2 | inline | done | 248a982; planner tests 16/16; actionlint clean; layout suite measured 86 s |
| T3 | S3 | inline | done | 999fcbc; OpenGL probe in runs 1–8 (only a non-accelerated renderer) |
| T4 | S4, S6, S7 | inline | done | 142c99e, e1e1c22; local emulator and run 41211812862/41213988975: all levels pass |
| T5 | S5, S6, S7 | inline | done | 999fcbc…0776abe; run 41213988975: 4/4 runs match desktop |
| T6 | S7, S8 | inline | done | 282 .NET + 25 site tests pass; 138 Windows captures byte-identical to main; docs commit |

- T1 Audit the current pipeline from recorded runs (docs/evaluations/ci-audit.md).
- T2 Change planner (`.github/scripts/changes.mjs`, tested), tiered `pipeline.yml`, separate `platforms.yml`.
- T3 macOS: OpenGL pixel-format probe on the runner; result and alternatives documented.
- T4 Android: NDK raylib build, .NET for Android host running a real game, emulator run in Actions.
- T5 iOS: toolchain, simulator build and launch of the real engine; raylib's iOS gap documented.
- T6 Regression run of existing suites; docs, roadmap, ADR; final report.

Routes are inline: no unit runs in parallel with another, and no writer trigger applies.

## Log

- L1 (2026-10-31) Original request: PLATFORM-2 brief "Infraestructura de validación multiplataforma y CI eficiente"
  (sections 1–8 as pasted by the owner; quoted verbatim in the specs above where wording matters).
- L2 (2026-10-31) Owner answer to "may I push experiment branches?": "Push experiment branch" — only a throwaway
  `experiment/platform-2` branch with workflows triggered by pushes to it; `feat/platform-ci` stays unpushed.
- L3 Exploration: GLFW in raylib 6.0 always adds `NSOpenGLPFAAccelerated` (`src/external/glfw/src/nsgl_context.m:207`);
  the macOS runner log shows `GLFW: Error: 65545 Description: NSGL: Failed to find a suitable pixel format` for all
  15 scripts (run 41201969390). raylib 6.0 has Android (`rcore_android.c`, `android_main` calls `main`) but no iOS
  platform; `rcore_memory.c` is a windowless software renderer (rlsw). Local machine: .NET 10.0.403 with android and
  ios workloads, Android SDK with emulator and an x86_64 API 36.1 image, no NDK.
- L4 (2026-10-31) T2: pipeline tiers by changed path; Site no longer waits for .NET jobs, Deploy checks every result.
  Docs-only browser checks reduced to the layout suite (86 s locally instead of 221 s in CI).
- L5 (2026-10-31) T4: Android prototype on the local emulator: NativeActivity + raylib android_main + JNI main(). Found:
  Raylib-cs glibc library packaged for android-x64 (excluded); raylib GetCurrentMonitor detaches the JVM thread
  (guarded in RaylibPlatform); `adb shell` splits `;` in extras (quoted). Captures 13/13 within the Linux reference,
  11 byte-identical to Windows.
- L6 (2026-10-31) Mistake: `git commit -a` put the owner's uncommitted site/.gitignore edit (2 ignore lines) into an
  experiment-branch commit that was pushed; reverted there with a new commit (no force-push), local edit restored.
  Later a broken `&&` chain committed it on feat/platform-ci; that local commit was undone before any push (push was
  refused), and the branch's upstream (origin/main) was removed. Only explicit paths are committed since.
- L7 (2026-10-31) T3/T5 runs 1–8 on experiment/platform-2 (table in the evaluation). macOS: only renderer 0x01020400
  (Apple Software Renderer, not accelerated); GLFW requires accelerated. Software raylib (PLATFORM_MEMORY + rlsw):
  138 captures, all blank (rlReadTexturePixels has no software branch). iOS: raylib has no iOS platform; headless host
  matches desktop 4/4. Android in CI: all levels pass in runs 5, 7, 8; raylib CloseAudioDevice race aborted 3 of ~37
  scripted runs.
- L8 Regression: .NET 282 tests and site 25 tests pass on Windows; 138 Windows desktop captures byte-identical between
  origin/main and this branch (25 of them were already outside the Linux reference's tolerance at the base).
- Next: owner decisions (raylib patch for the audio race; whether to keep experiment/platform-2; pushing this branch).
