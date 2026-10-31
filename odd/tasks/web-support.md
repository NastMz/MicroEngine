# WEB-1: Web support for the four demos

Objective: turn the WEB-0 feasibility result into reproducible, shared browser builds of Meadow Run, Courtyard Key,
Trail Duel and Neon Rift. Branch `feat/web-support` (from `feat/web-feasibility`; no push, no merge).
Delivery: single local branch, work-unit commits. Test runner: `dotnet test MicroEngine.slnx -c Release`; browser
checks: `web/tests` (puppeteer-core against the installed Chrome and Firefox).

## Specs

- S1 Review WEB-0: "Identifica qué componentes son definitivos, cuáles son experimentales y qué partes necesitan
  generalizarse para soportar las cuatro demos." Keep what worked; "No reescribas la solución ni sustituyas
  tecnologías sin una necesidad demostrada."
- S2 Shared host: "Diseña un mecanismo compartido para iniciar cualquier demo en navegador, manteniendo únicamente las
  diferencias necesarias entre juegos." It selects the game, loads assemblies, prepares assets, initializes the
  engine, runs and ends the lifecycle, gives each game its storage and handles start-up errors. "No dupliques cuatro
  veces la infraestructura del host. Tampoco construyas un framework de plugins o un cargador dinámico complejo".
- S3 Reproducible build: declares .NET, Emscripten and dependency versions; prepares tools "sin depender de
  modificaciones manuales del SDK del sistema"; compiles raylib when needed; builds all four packages or one;
  documents local run; "Detecte dependencias ausentes y produzca errores comprensibles." No public deployment
  pipeline; no generated binaries in Git.
- S4 Functional compatibility per game with its real mechanics (lists in L1), reaching the end of each demo when
  technically possible. "No basta con comprobar que los juegos arrancan". "No modifiques el gameplay para adaptarlo
  artificialmente al navegador."
- S5 Input and browser behaviour: investigate and fix the WEB-0 missed first presses; no input before ready; held and
  released keys; focus loss and regain leaves no stuck keys; browser-reserved keys handled with an alternative;
  working fullscreen where allowed.
- S6 Audio: music and effects in the selected browsers; autoplay restrictions; an unlock mechanism when a gesture is
  required; "Evita situaciones en las que el juego arranque aparentemente bien, pero permanezca silencioso sin
  explicación."
- S7 Persistence: reload, isolation between games, achievements, missing or corrupt data, storage errors and limits,
  achievement catalogue versions, "Ausencia de escrituras que eliminen datos válidos ante fallos de lectura."
  Keep localStorage if sufficient; document browser limits.
- S8 Lifecycle and presentation: no last frame left after quitting; loading screen; ready-to-play state; visible
  start-up errors; end and restart without a page reload if viable; canvas fits without distortion and scales the
  original resolutions; no accidental scroll or interfering shortcuts while focused. "No construyas una interfaz
  elaborada."
- S9 Chromium and Firefox: start, render, input, audio, persistence, scene changes, end, restart in both. Safari and
  mobile documented without claiming support.
- S10 Tests: automated tests where they add value (shared host, game selection, storage, lifecycle); all engine and
  game tests; desktop unchanged; reuse gameplay and capture scripts for web builds; report automated, real browser,
  manual and unverified separately.
- S11 Size and performance: record the four builds' sizes and main components; evaluate reasonable reductions
  (assets, trimming) without sacrificing stability; check different render rates.
- S12 Docs: build requirements, toolchain setup, single and full builds, local run, shared host structure, web
  persistence, verified browsers, limitations, adding a fifth demo. ADR only for relevant decisions.

## Tasks

| ID | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 | S1, S2 | inline | done | `GameSession` (RED then GREEN, bd48545); `web/Host` + `games.props` (b17cb3a) |
| T2 | S3 | inline | done | `toolchain.env`, `setup.sh` from an empty folder in 1m45s, `build.sh` (b17cb3a) |
| T3 | S5, S6, S8 | inline | done | Gesture start, tap fix (af60960, 5a5eb0e), page screens; L4-L6 |
| T4 | S7 | inline | done | `SaveStore.Save` bool + unreadable guard (RED then GREEN, 7b1e294); page storage notices |
| T5 | S4, S10 | inline | done | `web/tests` suites; L7-L8, L10 |
| T6 | S9, S10 | inline | done | Chrome 156 + Firefox 150; L10 |
| T7 | S11 | inline | done | L9 |
| T8 | S12 | inline | done | `web/README.md`, ADR 0008, `docs/evaluations/web-support.md` |

Routes are inline: the units depend on each other (host, then page, then validation) and none has a writer reason.

## Log

- L1 (2026-10-30) Original request: "MicroEngine — WEB-1: Compatibilidad web de las cuatro demos" (full brief pasted in
  the session that opened this branch). Objective quoted: "Conseguir que Meadow Run, Courtyard Key, Trail Duel y Neon
  Rift puedan compilarse, ejecutarse y jugarse completamente en navegadores modernos, utilizando la misma
  implementación de gameplay que sus versiones de escritorio." Per-game checks: Meadow Run "movimiento, saltos,
  colisiones, enemigos, monedas, transformaciones, transiciones, guardado y logros"; Courtyard Key "exploración,
  combate, enemigos, objetos, navegación por salas, interfaz, guardado y logros"; Trail Duel "exploración, encuentros,
  combate por turnos, captura, evolución, menús, guardado y logros"; Neon Rift "movimiento, disparos, enemigos, fases,
  pausa, progresión, audio, guardado y logros". Constraints: no game rewrite, no gameplay duplication, no engine
  switch, no unnecessary infrastructure, no accounts or cloud, no landing, no monorepo reorganisation, no desktop
  breakage. "No hagas push ni fusiones."
- L2 WEB-0 review. Definitive: `GameLoop`, `ISaveStorage`/`BrowserSaveStorage`, `BrowserLauncher`, the
  `OperatingSystem.IsBrowser()` guards, rebuilt raylib, content manifest + preload. Experimental: `NeonRift.Web` (one
  host per game), the hand-installed SDK in `~/.dotnet-wasm`, the README setup steps, `experiments/web/RaylibProbe`.
  To generalise: game selection, page glue, toolchain pinning. Found from source: emscripten's GLFW already releases
  held keys on window blur; raylib's F11/Alt+Enter call `ToggleBorderlessWindowed`, which on the web requests
  fullscreen outside a user gesture; raylib sizes the canvas to `window.innerWidth/innerHeight` (so an iframe is the
  embedding unit); GLFW registers its key listeners on `window` (capture) during `InitWindow`.
- L3 Shared host: `-p:Game=<Id>` + `web/games.props` generate `GameEntry`, `content.txt`, `game.json`; per-game
  `obj/<Game>` needed `DefaultItemExcludes` for `obj/**;bin/**` (CS0101 duplicate `GameEntry` otherwise).
- L4 Missed first presses (WEB-0) reproduced: a press and release between two frames was never seen, because
  raylib's `IsKeyDown` is the state at frame start. Fixed with raylib's pressed-key queue; then found that frames
  with no simulation step (display faster than 60 Hz) dropped the press before any step read it; presses now stay
  down until sampled. Before the page waited for a gesture, keys pressed while loading could also reach the game.
- L5 Page: start inside a click/key gesture (audio `running` under blocking autoplay in Chrome and Firefox),
  end screen instead of the last frame, restart without reload (raylib re-initialises on the same canvas),
  postMessage states for an embedding page. The Claude app's browser pane never enters fullscreen (no event, no
  error); checked in Chrome/Firefox instead.
- L6 Storage: a full quota made the page's availability probe (a write) report "blocked"; the probe now only reads.
- L7 Platform suite: 117/117 checks in Chrome 156 and Firefox 150 (four games). Script suite: 15 scenarios x 2
  browsers run to their quit; 138 captures per browser byte-identical to desktop raylib (`desktop.mjs`).
  `stage1.script` needs `RIFT_START=stage` (first run used the title, still identical, but not meaningful).
- L8 Autopilot without `RIFT_GOD` died in every stage in both browsers; the desktop test uses God mode, as does the
  README example, so the browser suite does too.
- L9 Sizes (Brotli transfer): framework 2.7 MB per game; `InvariantGlobalization` cuts it to 1.7 MB; `TrimMode=full`
  saves nothing more (partial trimming already trims the framework). Neon Rift content 10.2 MB, of which OGG music
  7.4 MB is incompressible. WEB-0's "2.8 MB content" counted only files with `.br` copies (corrected in its report).
- L10 Final validation after `InvariantGlobalization`: platform 125/125 (with fullscreen), framerate 24/24 (100/50/25
  fps), scripts 30/30, keyboard replay 12/12, autopilot 6/6 (stages 1-3 to EndingScene in both browsers), audio
  10/10 (output level and suspended-sound button), desktop comparison 276/276 captures, desktop main vs branch
  138/138, `dotnet test` 279/279. One Firefox scripts run made under concurrent load diverged in three games;
  rerun alone it matched twice (cause not found; documented).
