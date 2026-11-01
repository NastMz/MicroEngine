# DOCS-1: documentation audit, SDK gaps and validation

Internal record of the developer-documentation mission ([`odd/tasks/docs-1.md`](../../odd/tasks/docs-1.md)). Branch
`docs/developer-manual`, from `main` at db5fa1b, which equals the published SDK 0.4.0-alpha.1 (`git diff
sdk-v0.4.0-alpha.1..main -- src templates` changes one template README line).

## Audit of the existing pages

| Page | Finding | Action |
| --- | --- | --- |
| `making-a-game.md` | Obsolete: project references to a repository clone, "package-based setup is planned", browser packages "not available" | Replaced by the tutorial; the file stays (the published template's README links it) as a map to the new pages |
| `guide/getting-microengine.md` | Correct but thin; mixed install, first game, repository work | Rewritten as Install the SDK; Your first game and Updating the SDK are new pages |
| `guide/getting-started.md`, `guide/project-structure.md` | Internal, unpublished; said "no NuGet package yet" | Stale sentence corrected; still unpublished |
| `examples/embedding.md` | Obsolete: "producing browser packages is not available" | Publishing on the web: building with `microengine package web`, hosting, `--dev` |
| `examples/testing.md` | CI evidence (Xvfb, 138 captures, browser byte comparisons) on a public page; stale browser sentence | Engine checks moved to `docs/ci.md`; page starts with a game's own test project (quoted, tested) |
| `reference/configuration.md` | Obsolete browser-package section | Corrected (`--dev`, `MicroEngineGameFactory`) |
| `manual/input.md` | A table of measurements from `web/tests/mobile.mjs` and headless runs (test evidence) | Replaced by a functional guide to how much of the frame stays visible |
| `manual/audio.md`, `manual/assets.md`, intro, README, architecture | Wrong: OGG and MP3 music "streamed by the backend"; `RaylibAudioDevice` named as the desktop device | SDL (default) streams WAV and OGG only and throws `NotSupportedException` for MP3; raylib also MP3 |
| `manual/rendering.md` | Correct; text and collision only touched; "the raylib backend renders..." while SDL is the default | Both platforms named; links to the new Text and Tile maps pages |
| `manual/lifecycle.md`, `scenes.md`, `ui.md`, `persistence.md`, `achievements.md`, `backends.md` | Correct and deep; examples taken from the demos | Kept; links updated; extension examples added in a new page |
| `examples/hello-engine.md` | Correct code; project setup implied the repository | Quotes its package-based project file |
| `examples/patterns.md`, `reference/contracts.md`, `components.md`, `limitations.md` | Correct | Kept; broken anchor in limitations fixed |
| Navigation | Reference and examples mixed beginners and maintainers; no tutorial, no API reference, no how-to | Ten groups as learning paths; API reference generated |
| Undocumented capabilities | Tile collision (`TileMotion`, one-way platforms, extra solids), `EntityList`, custom transitions, platform profiles, `IActionSource` defaults, the font's symbols, local tool pinning | Tile maps and collision, Extending the engine, Text, Updating the SDK |
| Documented but missing | MP3 music on the default platform | Corrected (see gaps) |

## SDK gaps found

Defects (reported, not fixed: outside the documentation's scope; each is documented where a developer meets it):

1. **MP3 music only on raylib.** `SdlAudioDevice.LoadMusicStream` accepts `wav` and `ogg` and throws for `mp3`
   (`src/MicroEngine.Sdl/SdlAudioDevice.cs`), while raylib decodes MP3. A game that uses MP3 works with
   `--platform raylib` and in browsers and fails on the default platform and on phones.
2. **The template's scene comment** says the game's actions are bound in `MainGame.CreateInput`, which the template does
   not override (published 0.4.0-alpha.1 template, `Scenes/PlayScene.cs`).
3. `Animator.Speed` below 0 makes `Frame` throw `IndexOutOfRangeException` (`Graphics/Animation.cs`).
4. `CoroutineRunner.StopAll` drops coroutines that have not run yet without cancelling their handles; `IsFinished`
   stays false (`Scenes/Coroutines.cs`).
5. A scene change requested while a transition plays loses its own transition and applies at once, before the playing
   transition swaps: `Push(A, fade)` then `Push(B, fade)` puts B below A (`Scenes/Scene.cs`).
6. `SfxParams.Arpeggio` plays its steps once and then holds the last one; the code suggests it was meant to loop
   (`Audio/Synth.cs`).
7. `Synth.Render` produces a NaN sample when `Attack + Sustain + Decay` is 0 and `EndFrequency` is set.
8. `Synth.Layer` assumes mono 44.1 kHz clips and ignores `SampleRate` and `Channels`.
9. `AudioMixer.PlayMusic(name, fadeSeconds: 0)` still fades the new track in over about 0.25 s.
10. `ContentFolder.PathOf`'s "outside the content folder" check is a prefix match (`../ContentX/file` passes);
    `Exists` does not check it.
11. `SaveStore.Delete` does not clear a slot's "could not be read" mark, so `Save` keeps refusing that slot; `Delete`
    lets I/O exceptions through while `Save` returns false.
12. `GameLauncher` reports an invalid number by its value ("abc needs a positive integer") instead of the option.
13. A press and release within one frame reports `Pressed` and `Down` but never `Released` (`Input/Input.cs`).
14. `DialogueBox` silently drops wrapped lines beyond `Lines`; `SelectionList.SetOptions` accepts an empty list and
    `Index` is not range-checked.
15. `ParticleSystem.Draw` with `Additive` resets the blend mode to `Alpha` instead of restoring the previous one;
    `EntityList.Clear` does not call `Removed()`.

Capabilities a developer will look for and not find (functional limits, documented as such):

- One built-in font, no font loading (TTF or bitmap sheets), no rich text or right-to-left text.
- Menus and widgets answer keyboard, gamepad and touch actions but not mouse clicks; the mouse cannot be bound to
  actions.
- No slopes, circles or spatial index in collision; no map-editor importers.
- No Android App Bundles; the tool builds APKs.
- No hook for shaders or render targets in `IRenderer`.
- `SaveStore.DefaultDirectory` cannot be redirected on Windows (it uses the known folder, not an environment
  variable), so a game must take a save directory itself to keep tests and scripted runs away from the player's saves;
  the tutorial's game does.
- `microengine run --help` lists only the official platform names, although a game's own profiles also work with
  `--platform` (verified with the examples' `sdl-counted`).

Found and fixed during the work (tooling, not engine):

- The CI planner read only the `ProjectReference` items written in each project file. Once the samples reference the
  published packages and get the engine's source through `samples/Directory.Build.targets`, an engine change would not
  have run their tests. `.github/scripts/projects.mjs` now adds references a `Directory.Build.targets` declares.
- The landing page's code panels drew comments at 3.9:1 contrast; raised to 6:1.
- DocFX writes `<example>` code as raw HTML, which widened API pages on phones; converted to fenced blocks.

## What was built

- Learning paths: Introduction, Getting started, Tutorial (overview and 13 steps), How-to and examples, Engine manual,
  Platforms and distribution, Advanced topics, API reference (147 generated pages), credits, legal.
- Samples written as a developer's project (package reference to 0.4.0-alpha.1), built against the engine's source in
  the repository: Gem Garden in four checkpoints, 14 focused examples with extension code, Hello Engine.
- Quoted code: 102 blocks on the public pages quote a sample file or region; the site build fails when one differs.
- API reference: DocFX 2.81 (`.config/dotnet-tools.json`) over the four engine assemblies; 615 public members gained
  XML comments (comments only, no code line changed); `--check` keeps the committed pages in step.

## Validation (2026-11-01, Windows 11, .NET SDK 10.0.403, Node 26)

| Check | Result |
| --- | --- |
| `dotnet test MicroEngine.slnx -c Release` | 378 passed, 5 skipped (platform-specific), 0 failed; includes Samples.Tests (9) |
| `npm test` in `site/` | 42 passed (quoted code, API page discovery, sidebar paths, link checker included) |
| `node --test .github/scripts/*.test.mjs` | 41 passed |
| `astro build` + `check-dist` (`SITE_DEMOS=none`) | 213 pages, 204 documentation pages, internal links and anchors resolved, no engineering material |
| `node site/scripts/snippets.mjs` | 102 quoted blocks, none out of date |
| `node site/scripts/api-reference.mjs --check` | 147 pages up to date |
| `node sdk/tests/docs.mjs` (nuget.org only, outside the repository) | the six samples build against 0.4.0-alpha.1 and play their scripts |
| `node sdk/tests/external.mjs --public --targets windows,linux` | 34 checks passed |
| Onboarding from scratch (isolated NuGet cache and .NET home; only public pages) | 14 steps passed: tool install, `new`, step 2 code taken from the page, the finished game, `build`, `package windows`, `package linux`, the testing page's own test project |
| Packages | Windows ZIP unpacked and played its script (3910 updates); Linux tar.gz layout and desktop entry inspected |
| Responsive | all 204 documentation pages at 375 px: no horizontal overflow (after the API code fix) |
| Accessibility | axe-core 4.10 WCAG 2 A/AA on the landing, introduction, tutorial, how-to, examples, manual and API pages: no violations |
| Search | Pagefind index built; guides rank first for task queries after weighting API pages down |
| Captures | every sample screen captured and looked at |

Not verified here: web, Android, macOS and iOS packages of the tutorial's game (this machine has none of their
workloads); their instructions follow the CLI's own documented and CI-tested behaviour. Linux packages were built, not
run.
