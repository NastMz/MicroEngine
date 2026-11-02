# GAME-POLISH: Player experience, application identity and multiplatform quality

Objective: the four demos feel finished on desktops, phones and browsers; every general fix lands once, in the engine or
the SDK tool. Branch `feat/game-polish`, from `main` at d2c7081. Delivery: commits per work unit, PR for review, no
merge or release without the owner. Test runner: `dotnet test MicroEngine.slnx -c Release`; SDK: `node sdk/tests/*.mjs`.

## Specs

Quoted fragments are the owner's request (2026-11-01), verbatim.

- S1 Playable audit of Meadow Run, Courtyard Key, Trail Duel and Neon Rift: "Ejecuta y examina las cuatro demos
  utilizando los medios disponibles. No te limites a leer código o ejecutar tests unitarios." Start, menus, controls
  screens, gameplay, pause, dialogues, scene changes, defeat/victory, save/restore, mobile suspend, resolutions,
  keyboard, gamepad, touch. "Prioriza corregir los problemas encontrados, no simplemente registrarlos." "No declares
  probado manualmente un dispositivo al que no tienes acceso."
- S2 Application identity on Windows, Linux, macOS, Android, iOS and web, from the game's public configuration (SDK-1):
  "Existe un defecto confirmado en Android: una demo puede mostrar su nombre correcto en la información de la
  aplicación y, simultáneamente, aparecer como «MicroEngine» en el launcher, con un icono genérico." "No corrijas
  únicamente Android." "No mantengas nombres, iconos o identificadores genéricos codificados en los hosts." Check the
  final artifacts (manifests, icons, bundles, metadata), not the `.csproj`; never change stable application ids.
- S3 Adaptive action prompts: "Implementa un mecanismo para resolver la representación de una acción según:
  Dispositivo activo. Bindings configurados. Remapeos. Contexto del juego. Plataforma." Text and, where it fits,
  glyphs; updated when the active device changes; applied to dialogues, tutorials, help screens, menus, contextual
  messages and gameplay hints. "No reemplaces simplemente `Z` por `A` en textos concretos."
- S4 Touch control visuals: position, size, layout, opacity, colours, shapes, visual states, custom sprites, press
  feedback, joystick, d-pad and button styles. "Mantén independientes la detección táctil y su representación visual."
  Same actions, finger tracking, multitouch and rules. "No conviertas el sistema en un framework general de UI."
- S5 Dialogues, texts and interfaces: overflow, wrapping, sizes, scaling, alignment, legibility, selection markers,
  menus, contextual messages, touch overlap, transitions, mobile resolutions. "El problema conocido de desbordamiento
  en `DialogueBox` debe investigarse y corregirse". Narrative texts unchanged where possible.
- S6 Startup: Android splash, iOS launch screen, desktop opening, web loading indicator, transition, game identity,
  optional "Made with MicroEngine". "No obligues a mostrar la marca MicroEngine en todos los juegos." "Evita demoras
  artificiales únicamente para mostrar un logotipo." "El splash no debe ocultar indefinidamente errores de
  inicialización."
- S7 Audiovisual polish: fix real defects (animations, transitions, overlaps, scaling, effects, missing feedback,
  duplicated or cut sounds, music restarts, volume jumps, pause/defeat/scene changes) without an art redesign; needs
  for new subsystems are recorded, not built.
- S8 Validation on Windows, Linux, macOS, Android, iOS, desktop and mobile browsers by the means available; real
  packages built by the public CLI; platform references kept: "No actualices capturas de referencia para ocultar
  regresiones."
- S9 Regression tests for prompts, device changes, remapping, control styles, hit testing, control drawing, long
  dialogues, identity and metadata, packaging, splash, navigation and lifecycle; existing demo and SDK tests pass.
- S10 SDK compatibility: 0.4.0-alpha.1 is published; keep compatibility, document breaks, update examples and docs,
  prepare release notes; "No presentes cambios de `main` como si ya estuvieran disponibles en la versión publicada."
  No SDK release.
- S11 Public docs for controls, identity and startup with real examples; editorial separation kept; demo pages match
  each build's instructions.
- S12 Exclusions: ECS, local or online multiplayer, a new demo, physics, advanced asset pipeline, external fonts, a
  general UI framework, new renderers, deep performance work (performance findings go to PERF-1).
- S13 Deliverables: the 15 items of the request's section 16, ending with a PR ready for review.

## Tasks

| Task | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 Audit: run every demo script, inspect captures, packages and identity | S1 S2 S8 | inline | done | L3, L4 |
| T2 Identity: adaptive Android icon, artifact checks on every target, web manifest | S2 S9 | inline | in progress | 7c976f1; emulator L5 |
| T3 Action prompts in the core, glyphs in UI, demos use them | S3 S9 | inline | in progress | 357c584 core; demos pending |
| T4 Touch control styles and skins, demo styles | S4 S9 | inline | in progress | 4d7a65e core; demos pending |
| T5 DialogueBox pagination and prompt tokens, menu and text fixes | S5 S9 | inline | in progress | 357c584 DialogueBox, Wrap |
| T6 Startup: Android splash, iOS launch screen, web loading, branding option | S6 S9 | inline | in progress | 7c976f1; emulator L6; iOS needs CI |
| T7 Audiovisual and demo fixes found by the audit | S1 S7 | inline | pending | |
| T8 Docs, release notes, evaluation, full tests, PR | S10 S11 S13 | inline | pending | |

Route evidence: no Writer trigger (units share `src/MicroEngine/Input` and `UI`, and each is understood); work stays
inline following this record.

## Log

- L1 2026-11-01 Owner request "MicroEngine — GAME-POLISH" (sections 1–16), quoted in the Specs above.
- L2 Android identity, first evidence: the APKs in `dist/packages` (engine 0.3.0, before SDK-1) have
  `launchable-activity label='MicroEngine' icon=''` while `application-label:'Meadow Run'` (aapt badging): the defect
  as reported. A fresh `microengine package android` from main and the published v0.4.0 APK both have the right
  launcher name and icon. What remains: the icon is a legacy square PNG at one density, no adaptive icon, so Android 8+
  launchers shrink it onto a plate; and the SDK check only read `application-label`, the field that was right while
  the launcher was wrong.
- L3 Audit, desktop captures of all 36 demo scripts (first pass read the developer's own saves; rerun with isolated
  save folders and each script's debug start). Found: Courtyard Key's Controls panel overflows (key caps run into
  their labels, the last row past the border); achievement toasts cover the boss bar and the room tag; hardcoded keys
  in Courtyard Key (item-get dialogue "Press Z", HUD slots "Z"/"C", "Esc: back", "press Z" at the end), Meadow Run (map
  footer "Z: play X: title", controls screen), Trail Duel ("PRESS Z TO START", bag and summary hints, controls), Neon
  Rift (controls screen). `DialogueBox` drops lines beyond `Lines` (documented as "not shown"). `BitmapFont.Wrap`
  leaves a word wider than the line overflowing.
- L4 Engine: `ActionPrompts` (device, bindings, remapping, touch layout, gamepad family; `Format`, `TextAll`,
  `Directions`), `PromptText` glyphs, `DialogueBox` paging and prompt tokens, long-word wrapping (357c584); touch
  `TouchStyle`, `ITouchSkin`, `IOverlayCanvas.Image` (4d7a65e). 199 engine tests pass.
- L5 Android identity on the emulator (Medium_Phone_API_36.1, Android 16): the installed Courtyard Key and Trail Duel
  (pre-SDK-1 builds) were listed as "MicroEngine" with Android's generic icon, Neon Rift (SDK-1 build) with its icon
  shrunk on a white plate: the reported defect. The four packages from this branch installed as updates over them
  (same ids, same key) and the drawer shows each game's name with its art filling the launcher mask; Recents shows
  "Meadow Run" with its icon.
- L6 Startup on the emulator (screen recording, 6 fps): first build showed system splash -> ~0.5 s black (SDL surface)
  -> engine start screen with black bands -> title. After holding Android 12's launch screen until the engine's
  first frame and letterboxing in the start-screen colour: launch screen -> start screen (same colour, full screen)
  -> title, no black frame. The start screen lasts as long as loading does (about 3.5 s on the emulator).
