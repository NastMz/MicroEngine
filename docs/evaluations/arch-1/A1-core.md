# A1 — Core, lifecycle, composition, assets, structure and SDK surface

Phase I adversarial audit, domain A1. Code audited: branch `docs/arch-1` at `5706285` (includes the unmerged ASSET-1
work). Read-only on the repository. Experiments live outside it, in
`<scratch>\exp-core\`:

| Artifact | What it is |
| --- | --- |
| `exp-core\nsdeps.mjs`, `exp-core\nsdeps.txt` | Namespace dependency extractor (type names declared per namespace, matched in other namespaces' files after stripping comments and strings) and its output |
| `exp-core\stub\` (`Stub.csproj`, `Program.cs`) | Throwaway console project referencing the built `src\MicroEngine\bin\Debug\net10.0\MicroEngine.dll` (binary reference, so the repository was not rebuilt): stub backend, composition, headless clock, asset identity and public-surface experiments |
| `exp-core\stub-output.txt` | Output of all five experiments (E1–E5) |

Industry notes in this report are brief and limited to facts I know reliably; the industry-research team owns depth.

---

## 1. Inventory

### 1.1 Physical structure

| Project / package | Contents | Dependencies |
| --- | --- | --- |
| `src/MicroEngine` (package `MicroEngine`) | Everything backend-independent: `Core` (17 files), `Graphics` (17), `Input` (5), `Audio` (4), `Scenes` (2), `UI` (3), `World` (3), `Math` (3, root namespace `MicroEngine`), `build/MicroEngine.targets` (identity) | StbImageSharp 2.30.15, FontStashSharp 1.3.10 (+ StbTrueTypeSharp, FontStashSharp.Base, Cyotek.Drawing.BitmapFont) — `src/MicroEngine/MicroEngine.csproj:12-15` |
| `src/MicroEngine.Sdl` | `SdlPlatform` (854 lines: window, SDL_Renderer, graphics device, input, touch, lifecycle), `SdlAudioDevice` | ppy.SDL3-CS, NVorbis — `MicroEngine.Sdl.csproj` |
| `src/MicroEngine.Raylib` | `RaylibPlatform` (535), audio, touch, `BrowserLauncher`, `BrowserSaveStorage` | Raylib-cs |
| `src/MicroEngine.Desktop` | `DesktopLauncher` (50 lines): official catalog `sdl, raylib, sdl-software, headless` | Core + Sdl + Raylib (`MicroEngine.Desktop.csproj`) |
| `src/MicroEngine.Cli` | `microengine` .NET tool (1468 lines) + host templates: `Hosts/web` (raylib WASM), `Hosts/android`, `Hosts/ios`, `Hosts/mobile/MobileRun.cs` | — |
| `templates/MicroEngine.Templates` | One `dotnet new` game: `Program.cs` → `DesktopLauncher.Run(new MainGame(), args)`, identity in MSBuild properties incl. `MicroEngineGameFactory` (a C# expression) | `MicroEngine.Desktop` |
| `sdk/` | `version.json`, `version.mjs`, `tests/` (external consumer check) | — |

Single version for all packages: `Directory.Build.props` `MicroEngineVersion=0.4.0-alpha.2`.

### 1.2 Logical core (what `Core` actually holds)

| Type | Role |
| --- | --- |
| `Game` (abstract) | Base class *and* composition root *and* service locator: owns `Screen`, `Input`, `Touch`, `Prompts`, `Scenes`, `Assets`, `Audio`, `Font`, `Achievements`, `AchievementToasts`, `Random`; fixed `Initialize`/`Step`/`Render`/`Suspend`/`Shutdown` order (`Game.cs:132-207`) |
| `GameSettings` | Title, virtual canvas, window scale, `Scaling`, `TextureFilter?`, `UpdateRate`, colours, `Graphics` (`Platform.cs:18-44`) |
| `GraphicsSettings`, `GameScreen`, `IDisplayControl` | Start-time and run-time graphics options (ADR 0024) |
| `GameLoop`, `GameRunner`, `RunOptions`, `RunResult`, `InputScript`, `MonotonicClock` | Fixed-step loop, scripted input, captures (`GameRunner.cs`) |
| `GameSession` | Page-driven wrapper for browsers (`GameSession.cs`) |
| `GameLauncher`, `LaunchArguments` | CLI parsing, platform selection with fallback, console reporting, audio capture, profiling (`GameLauncher.cs`) |
| `IPlatform`, `IPlatformDiagnostics` | Backend contract (`Platform.cs:47-114`) |
| `PlatformProfile`, `PlatformCatalog`, `PlatformRequest`, `PlatformParts`, `ComposedPlatform`, `CompositeInput` | Backend selection and part replacement (ADR 0016) |
| `HeadlessPlatform` (+ `HeadlessGraphicsDevice`, `HeadlessRenderer`, `HeadlessInput`, `HeadlessTouchScreen`) | Test platform, shipped in the core package |
| `AssetCache`, `AssetBatch`, `AssetLoadException`, `ContentFolder` | ASSET-1 cache with scopes; physical content folder with case checking |
| `SaveStore`, `ISaveStorage`, `FileSaveStorage` | JSON slots |
| `Achievement`, `AchievementTracker` | Achievements with persistence |
| `AppIdentity`, `StartScreen`, `WindowSizing`, `FrameTimer`, `FrameProfiler` | Identity from assembly metadata, splash, window size rules, precise sleep, profiling |

### 1.3 Public API surface (experiment E5, reflection over the built assembly)

| Namespace | Exported types | Public members (declared) |
| --- | ---: | ---: |
| `MicroEngine` | 4 | 61 |
| `MicroEngine.Audio` | 12 | 139 |
| `MicroEngine.Core` | 55 | 570 |
| `MicroEngine.Graphics` | 33 | 320 |
| `MicroEngine.Input` | 37 | 409 |
| `MicroEngine.Scenes` | 9 | 66 |
| `MicroEngine.UI` | 8 | 90 |
| `MicroEngine.World` | 15 | 88 |
| **Total** | **173** | **≈1743** |

Public static mutable state: `ContentFolder.DefaultRoot`, `SaveStore.DefaultStorage` (E5). Internal static mutable
state: `AudioMixer.RenderSongsOnThreads`, `AudioMixer.SliceBudget` (`Audio/Audio.cs:213-215`); in backends
`BrowserTouch._hidden` and other `BrowserTouch`/`BrowserLifecycle` statics (`MicroEngine.Raylib/BrowserTouch.cs:45`,
`Hosts/web/Program.cs`). Public test hooks on the game base class: `Game.InitializeForTest`, `Game.StepForTest`.

### 1.4 Backend contract size (experiment E2)

| Contract | Members | Without default implementation |
| --- | ---: | ---: |
| `IPlatform` | 16 | 11 |
| `IRenderer` | 13 | 13 |
| `IAudioDevice` | 11 | 11 |
| `IInputSource` | 7 | 3 |
| `IGraphicsDevice` | 3 | 2 |
| `Texture` (abstract class) | — | 3 protected abstract |
| optional `ITouchScreen` / `IOverlayCanvas` / `IDisplayControl` / `IPlatformDiagnostics` / `ISaveStorage` | 8 / 6 / 7 / 1 / 6 | 6 / 5 / 7 / 1 / 6 |

A stub backend of 32 required members (reusing `SilentAudioDevice`) compiled at the first attempt and ran a game for
10 updates with no error (`stub-output.txt`, E2). Writing one is syntactically cheap; see F11 for what the contract does
not say.

---

## 2. Namespace dependency map

Extracted by `nsdeps.mjs` (lexical: a type name declared in namespace B that appears in a file of namespace A). Two
false positives removed by inspection: `Scenes.Wait` matched the method name `Wait` and `Audio.Wave` matched
`MathX.Wave`, so `Core→Scenes` via `Wait`, `MicroEngine→Audio` and `UI→Audio` are not real. Counts are files×types.

```
                      +-------------------- MicroEngine (Color, RectF, MathX, Ease) <-- everyone
                      |
   UI ----------> Core <----------> Scenes
   |  ^            | |  \             |
   |  +------------+ |   \            v
   |   (Game draws   |    +------> Graphics <---- Input (CanvasScaling, PixelImage)
   |    toasts)      |    |            ^           ^
   v                 v    v            |           |
 Input           Audio   Input        World -------+ (no)   World -> Graphics only
```

| Edge | Evidence (type@file) |
| --- | --- |
| Core → Graphics (37) | `IGraphicsDevice`, `Texture`, `SpriteAtlas`, `ScalableFont`, `ImageDecoder`@AssetCache.cs; `Sprite`@Achievements.cs; `PixelImage`@AppIdentity.cs |
| Core → Input (31) | `InputMap`, `VirtualControls`, `ActionPrompts`, `TouchLayout`, `ITouchScreen`@Game.cs; `Key`@AppIdentity.cs |
| Core → Audio (6) | `AudioMixer`@Game.cs, `IAudioDevice`@Platform.cs, `CaptureAudioDevice`@GameLauncher.cs |
| Core → Scenes (real: 1) | `SceneManager`@Game.cs |
| Core → UI (2) | `AchievementToasts`, `UiTheme`@Game.cs |
| Scenes → Core | `Game`, `AssetCache`@Scene.cs |
| UI → Core | `Game`, `Achievement`, `AchievementTracker`@AchievementViews.cs |
| UI → Scenes, Input, Graphics | `Scene`@AchievementViews.cs; `ActionPrompts`, `Key`, `PadButton`@PromptText.cs; `IRenderer`, `IFont`@Widgets.cs |
| Input → Graphics | `CanvasScaling`@Touch.cs, `PixelImage`@TouchSkin.cs |
| World → Graphics | `IRenderer`, `Camera2D`, `Sprite`, `Texture`@TileMap.cs, Entities.cs |
| Graphics → (root only) | Graphics depends on nothing but `MicroEngine` |

**Cycles:** `Core ↔ Scenes` (`Game` holds `SceneManager`; `Scene.Game`), `Core ↔ UI` (`Game.Render` draws
`AchievementToasts`; `AchievementsScene` reads `Game`). **Shape:** `Core` is not a kernel at the bottom of the graph;
it is the *top* of the graph — it depends on every other namespace except World, and the names "Core" and
"MicroEngine" (math) invert the usual meaning. `Graphics` is the actual leaf. The only namespaces that could become
separate assemblies today without breaking a cycle are `MicroEngine` (math), `Graphics`, `World` and `Audio`.

---

## 3. Findings

### Summary table

| # | Finding | Classification | Severity |
| --- | --- | --- | --- |
| F1 | Composed platforms silently drop optional capabilities (display pacing, precise wait, run-time display control, diagnostics) | Confirmed problem | High |
| F2 | `Game` is a fixed god object / service locator; no module or system registration; fixed step order; Core↔Scenes, Core↔UI cycles | Architectural risk | High |
| F3 | The frame lifecycle hard-wires one 2D immediate-mode `IRenderer` into `Game` and `Scene`; no transform/matrix/3D vocabulary anywhere | Architectural risk (3D vision) | High |
| F4 | Time model (fixed step vs real time, deterministic clock) is implemented by each backend, not the loop; headless clock hard-codes 60 Hz | Confirmed problem (bug + misplaced responsibility) | Medium |
| F5 | Loop policy is hard-coded (max 5 steps, 0.25 s clamp, no variable-rate update phase, input only sampled per fixed step, interpolation left to every game) | Design limitation | Medium |
| F6 | Three composition roots (desktop launcher, browser launcher, mobile runner) configured partly through process-wide statics and environment variables | Confirmed problem | High |
| F7 | Asset system: concrete closed cache, no asset identity/handles, audio outside it, physical files only, no import stage | Architectural risk + missing capability | High |
| F8 | Hidden defaults and coupled options beyond ASSET-1's filter coupling | Confirmed problem | Medium |
| F9 | One assembly for every module, with third-party decoders mandatory; namespaces are not boundaries | Design limitation | Medium |
| F10 | Public surface leaks: test hooks, statics, CLI/console in core, presentation and hotkey policy in the platform contract | Confirmed problem | Low–Medium |
| F11 | `IPlatform` is easy to implement but its semantics are implicit; no conformance kit; window+renderer+device+audio+input in one contract | Architectural risk | Medium |
| F12 | No engine events (resize, focus, display, device), no logging abstraction | Missing capability | Medium |
| F13 | Threading model implicit (single game thread, ad-hoc `Task.Run`), no main-thread contract | Architectural risk (latent) | Low |
| F14 | Virtual canvas fixed at start; no runtime resolution change, no aspect-adaptive layout | Design limitation | Medium |
| F15 | Single-player assumptions live in the platform contract (`IInputSource` = one pad, one pointer) | Architectural risk (cross-domain: Input) | High |
| F16 | Achievements, toasts and saves wired into `Game` | Design limitation | Low |
| F17 | Fixed-step determinism with scripted input, asset scopes, managed decoders, profile catalog | Legitimate specialization (keep, re-home) | — |

### F1 — Composed platforms silently drop optional capabilities

1. **Exists.** `PlatformProfile.Create` wraps the backend in `ComposedPlatform` whenever a host replaces audio or adds
   input (`PlatformProfiles.cs:131-140`). `ComposedPlatform` (`:197-231`) forwards the 14 members it implements.
   Optional capabilities are discovered by type tests (`platform as IDisplayControl` in `GraphicsSettings.cs:103`,
   `is IPlatformDiagnostics` in `GameLauncher.cs:207` and `GraphicsSettings.cs:150`) and by default interface members
   (`IPlatform.DisplayRate => 0`, `IPlatform.Wait` → `FrameTimer.Delay`, `Platform.cs:77,94-98`).
2. **Problem.** The wrapper does not implement `DisplayRate`, `Wait`, `IDisplayControl` or `IPlatformDiagnostics`, so
   C# silently binds the defaults: display-paced timing turns off, the backend's precise timer is replaced by the
   generic sleep, `Game.Screen.CanChange` becomes false (a graphics menu stops working), and diagnostics disappear.
   This is triggered by an ordinary, documented feature: `--audio-capture` (`GameLauncher.cs:86-88`) and any host
   that passes `PlatformRequest.Audio`/`ExtraInput` (ADR 0016's headline feature). No test asserts these through a
   composed platform (`PlatformCompositionTests.cs` has no `DisplayRate`/`IDisplayControl` assertion).
3. **Evidence.** Experiment E1 (`stub-output.txt`):
   ```
   direct                 type=StubPlatform     DisplayRate=144 IDisplayControl=True  IPlatformDiagnostics=True  Wait->backend=True
   composed (ExtraInput)  type=ComposedPlatform DisplayRate=  0 IDisplayControl=False IPlatformDiagnostics=False Wait->backend=False
   Game.Screen.CanChange through composed platform: False
   ```
4. **Industry.** Capability-by-cast plus decorators is a known fragility (the "decorator hides interfaces" problem in
   COM `QueryInterface` aggregation, and in .NET stream wrappers). Engines that compose subsystems expose capabilities
   through an explicit query (Godot's `DisplayServer::has_feature`, SDL's per-object property bags) rather than through
   the static type of the object.
5. **Alternatives.** (a) Forward every member and optional interface in `ComposedPlatform` (fixes today, breaks again at
   the next optional interface); (b) replace type tests with an explicit capability query on the platform; (c) stop
   wrapping: let the request's parts be injected into the backend's construction so no decorator exists.
6. **Proposal.** (b) + (c): subsystems are constructed by the host and handed to the loop separately (see target §4);
   optional features are obtained by `TryGetCapability<T>()`, which a composition forwards generically. As an
   immediate fix in the current code, forward the four members and add a test that composes every official profile and
   compares its capabilities to the uncomposed one.
7. **Consequences.** Removes a class of silent regressions every time an optional interface is added (ADR 0024 already
   added two). Breaks custom backends that rely on type tests (Alpha: acceptable).

### F2 — `Game` is a fixed god object; no extension point for subsystems

1. **Exists.** `Game` creates and owns every service in a fixed order (`Game.cs:134-152`), and `Step` runs a fixed
   sequence: scenes, audio, toasts, achievement flush, input edge reset, asset "playing" flag (`Game.cs:159-173`).
   `Render` is clear → scenes → toasts (`:175-180`). Extension is limited to overriding `CreateInput`,
   `CreateTouchLayout`, `ContentRoot` and `Load`. `Scene` re-exposes `Game`'s services (`Scene.cs:32-40`). ADR 0016
   rejected a DI container ("Nothing needed more than a catalog and a request").
2. **Problem.** A physics world, an ECS scheduler, a network session, an animation system, a 3D renderer or a debug
   overlay has no place in the frame except inside a scene's `Update`/`Draw`. There is no pre/post-step phase, no
   ordering, no lifecycle (init/suspend/shutdown) for a subsystem the engine did not anticipate, and no way to replace
   a built-in one (the mixer, the touch controls and the default bitmap font are always created). The service set *is*
   the public API of `Game`, so adding a subsystem changes the base class. The two namespace cycles (§2) are a direct
   result: `Game` must know `SceneManager` and `AchievementToasts`.
3. **Evidence.** `Game.cs:132-207`; namespace cycles `Core↔Scenes`, `Core↔UI` (`nsdeps.txt`); `Game.Initialize` always
   adds `VirtualControls` as an input source and always builds `BitmapFont.CreateDefault` (`Game.cs:138-148`) even for an
   HD desktop game.
4. **Industry.** MonoGame's `Game` kept `Components` and a `GameServiceContainer` precisely for this. Godot moved
   engine subsystems behind *servers* registered at startup; Bevy and Unity's player loop expose ordered phases into which
   systems are inserted. The common lesson: a small kernel with a typed service registry and a phased scheduler, not a
   full DI container.
5. **Alternatives.** (a) Keep `Game`, add `Components`/`Services` à la MonoGame; (b) a host builder with modules
   (`IEngineModule`) and an ordered phase schedule, `Game` becoming one module/facade; (c) adopt
   Microsoft.Extensions.DependencyInjection (heavy, reflection, trimming/AOT friction on WASM/iOS).
6. **Proposal.** (b), with a minimal typed registry (no reflection, AOT-safe). `Game` stays as the friendly facade
   over the default module set so the plug-and-play path keeps its one-line `Program.cs`.
7. **Consequences.** Breaks `Game` internals and every service property's lifetime story; scenes keep their accessors
   through the facade. Unblocks physics, ECS, 3D and debugging tools as packages.

### F3 — The frame lifecycle hard-wires a 2D immediate renderer

1. **Exists.** `GameLoop.Render` calls `platform.BeginFrame()` → one `IRenderer` → `Game.Render(renderer)` →
   `Scene.Draw(IRenderer)` (abstract, `Scene.cs:77`) → `EndFrame()` (`GameRunner.cs:367-372`). `IRenderer` is 2D
   immediate mode with a translation-only camera ("cameras do not combine", `IGraphics.cs:139-142`; `Camera2D` has only
   `Position`/shake/bounds). `Color` is 8-bit sRGB (`Math/Color.cs:10`). There are **zero** uses of `Matrix3x2`,
   `Matrix4x4`, `Vector3` or `Quaternion` in `src/MicroEngine` (grep count 0).
2. **Problem.** The lifecycle assumes one render target per frame, one draw pass, one 2D vocabulary, and that the
   platform's frame *is* the renderer. 3D needs views/cameras with projection, depth, multiple passes, render targets,
   materials and a scene-to-renderer extraction step; 2.5D needs at least a transform hierarchy with matrices. None of
   it can be added without changing `Scene`'s abstract signature, the platform's `BeginFrame` contract and `Game.Render`.
   This is the domain's main 3D blocker; rendering-specific detail belongs to the rendering auditor.
3. **Evidence.** Files above; grep for matrix/vector3 types: 0 matches.
4. **Industry.** Godot 4 separated `RenderingServer` (backend) from scene nodes; Unity's SRP introduced a frame
   pipeline object between the player loop and the backend; MonoGame kept `SpriteBatch` as one *client* of
   `GraphicsDevice` rather than the device itself — which is what made 3D possible there.
5. **Alternatives.** (a) Keep `IRenderer` as the only path and add a separate 3D API later (two incompatible frame
   models); (b) introduce a frame/render-pipeline abstraction now, with today's 2D batch as its first client;
   (c) full render graph now (premature).
6. **Proposal.** (b): `Draw(RenderContext)` where the context provides views and command recorders; the 2D immediate API
   becomes a recorder over it (`ctx.Canvas2D`). Math vocabulary (`Transform2D`/`Transform3D` over System.Numerics
   matrices, linear float colour) enters the kernel now.
7. **Consequences.** Every scene's `Draw` signature changes once (mechanical). Requires coordination with the rendering
   domain on what a "view" and "render target" are.

### F4 — The time model is implemented by backends; headless hard-codes 60 Hz

1. **Exists.** Deterministic time is requested from the backend (`PlatformRequest.FixedStep`), and each backend implements
   it: `SdlPlatform.Now => _fixedStep ? _frames / UpdateRate : ticks` and `RealTime => !_fixedStep`
   (`SdlPlatform.cs:222-224`), the same in `RaylibPlatform.cs:115-117`, while `HeadlessPlatform.Now => _frames / 60.0`
   (`HeadlessPlatform.cs:40`). The launcher decides `FixedStep = script is not null || hidden` (`GameLauncher.cs:79`).
2. **Problem.** (i) Bug: headless time ignores `UpdateRate`. (ii) Clock policy is duplicated in three places and must be
   re-implemented by every new backend, although it is pure loop policy. (iii) "Hidden window" silently implies
   deterministic time (and, through `AudioOutput.Automatic`, no audio): three independent concerns coupled.
3. **Evidence.** E3: `UpdateRate=120: updates=120 Game.Time=1.000s platform.Now=2.000s (frames/60)`. `platform.Now` feeds
   the touch-controls delta (`GameRunner.cs:251`) and the frame limiter, so headless touch timing runs at half speed
   for a 120 Hz game.
4. **Industry.** Fixed-step loops (Fiedler's "Fix Your Timestep", used by most engines) keep the clock in the loop and
   ask the platform only for a monotonic time source.
5. **Alternatives.** Fix the constant only; or move the policy.
6. **Proposal.** `IPlatform` exposes only a monotonic clock; a `TimeStepPolicy` (`RealTime`, `Deterministic`) chosen by
   `RunOptions` lives in the loop; "hidden", "deterministic" and "audio output" become separate options.
7. **Consequences.** Backends shrink; scripted runs on any backend share one clock implementation.

### F5 — Loop policy is hard-coded; no variable-rate phase

1. **Exists.** `UpdateRate` is configurable (`Platform.cs:34`), but max 5 steps per frame, the 0.25 s elapsed clamp and the
   spiral reset `_accumulator = Math.Min(_accumulator, _step)` are literals (`GameRunner.cs:246,258,274`). Input is sampled
   once per fixed step (`:262`). `FrameBlend` is exposed (`Game.cs:73`), but interpolation is manual per game
   (`Vector2.Lerp(previous, position, FrameBlend)`) because there is no engine transform to interpolate.
2. **Problem.** There is no per-frame variable-rate phase for things that should run at display rate (camera smoothing,
   UI animation, render-side interpolation, input polling for latency on 144 Hz displays); with `UpdateRate=60` on a
   144 Hz display, 84 of 144 frames show no new input. A 3D game typically wants physics at a fixed rate and the
   camera/animation at frame rate. Hard-coded catch-up limits cannot be tuned for heavy simulations or slow devices.
3. **Evidence.** Lines cited; `Scene` has only `Update(float dt)` (fixed) and `Draw` (`Scene.cs:74-77`).
4. **Industry.** Unity (`Update` + `FixedUpdate` + `maximumDeltaTime`), Godot (`_process` + `_physics_process`,
   `max_physics_steps_per_frame` configurable, physics interpolation added in 4.3 after years of requests).
5. **Alternatives.** Expose the constants in `GameSettings`; add a `FrameUpdate(float dt)` phase; engine-side transform
   interpolation once a transform type exists.
6. **Proposal.** `TimeSettings { FixedRate, MaxStepsPerFrame, MaxFrameDelta }`; scheduler phases
   `FixedUpdate` and `Update(frame)`; interpolation as an opt-in service over the engine transform (later).
7. **Consequences.** Additive for games; determinism unchanged (fixed phase stays authoritative).

### F6 — Three composition roots, partly configured through globals

1. **Exists.**
   - Desktop: `DesktopLauncher.Run` → `GameLauncher.Run` → `PlatformCatalog`/`PlatformProfile`/`PlatformRequest`, with
     preference fallback and `MICROENGINE_PLATFORM` (`GameLauncher.cs:42-160`), and it sets the static
     `ContentFolder.DefaultRoot` (`DesktopLauncher.cs`).
   - Browser: `BrowserLauncher.CreateSession` sets the static `SaveStore.DefaultStorage` and `BrowserTouch.Vibrator`,
     then constructs `new RaylibPlatform(game.Settings, new DesktopOptions())` directly — no profile, no request, no
     graphics overrides, no part replacement (`MicroEngine.Raylib/BrowserLauncher.cs`). Page events go to statics
     (`BrowserTouch.Pointer`, `BrowserLifecycle.Hidden`, `Hosts/web/Program.cs`).
   - Mobile: `MobileRun.Run` applies `MICROENGINE_*` environment variables to the process
     (`Hosts/mobile/MobileRun.cs`: `Environment.SetEnvironmentVariable`), its own `RunOptions`, and a platform factory.
   - The game's entry differs per target: desktop runs the developer's `Program.cs`; web/mobile run a generated
     `GameEntry.Create()` from the MSBuild property `MicroEngineGameFactory` (a C# expression string).
2. **Problem.** Composition that a developer writes in `Program.cs` (a `configure` callback, a custom catalog, a replaced
   audio device) never reaches web or phone builds; replaceable parts are unavailable in the browser; process-wide
   statics make two games in one process (an editor, a test runner in parallel, a launcher of demos) interfere.
3. **Evidence.** Files above; E5 lists the two public settable statics.
4. **Industry.** .NET's own hosting moved to one builder per app (`HostApplicationBuilder`) precisely to unify entry
   points; MonoGame's per-platform `Game` subclasses were a long-standing pain point.
5. **Alternatives.** Keep per-target launchers but share one `EngineBuilder` the game configures once.
6. **Proposal.** The game exposes one `static void Configure(EngineBuilder b)` (or the template's `Program.cs` builds an
   `EngineBuilder` that hosts reuse); every host (desktop, web, phone, test) calls the same configuration and adds only
   its platform module. Content roots and storage become per-engine services, not statics.
7. **Consequences.** Removes the MSBuild C# expression; changes the host templates in `MicroEngine.Cli`.

### F7 — Asset system: identity, references, kinds, sources, import

1. **Exists.** `AssetCache` (sealed, `AssetCache.cs:65`) keyed by `(path, kind string, filter, pivot, options)`
   (`:75`); kinds are fixed: texture, image, atlas, folder atlas, font, bytes, text. Scopes with parent lookup, no reference
   counting (ADR 0025). `Preload` infers the kind from the extension (`.json`/`.xml` → atlas, `:333-335`), decodes on
   `Task.Run` (`:340`) and uploads within a frame budget. `ContentFolder` reads the physical file system (`File.*`,
   `Directory.*`) with case checks. Audio files are **not** in the cache: `AudioMixer.AddSoundFile(name, path)` reads a raw
   path (`Audio/Audio.cs:308-315`); games pass `content.PathOf(...)`, and one sample builds a fresh `new ContentFolder()`
   (`samples/Examples/Examples/SoundExample.cs:21`), ignoring `Game.ContentRoot`. `IAudioDevice` has no unload.
2. **Problems.**
   - No asset identity: callers hold live objects. `Unload(path)` disposes objects other code still holds (E4:
     `held texture after Unload: IsDisposed=True`), so lifetime errors surface as `ObjectDisposedException` at draw time.
   - Sibling scopes load the same file twice (E4: `sibling scopes share one texture: False`), so two levels or a level and
     an overlay scene duplicate textures; the same file with a different filter is a separate texture (by design, but it
     means "filter" is part of the asset's identity rather than a sampler state).
   - Closed set of kinds: a game or package cannot register a loader (a tilemap format, a model, a shader, a localisation
     table) or a different cache policy; audio, songs and fonts each have their own lifetime story.
   - Physical files only: no virtual file system, so archives, APK assets read in place, HTTP/IndexedDB in browsers, mods
     or patch overlays are impossible without unpacking; `ContentFolder.DefaultRoot` is a static.
   - No import stage: ADR 0025 rightly rejected an MGCB-like compiler *for today's formats*, but there is also no seam
     for one: 3D models (glTF), compressed GPU textures, mipmaps, shader compilation (SDL_shadercross) and SDF fonts all
     need a build-time step plus metadata (import settings) next to the source file.
   - No hot reload, no dependency tracking (an atlas JSON and its PNG are two files but one asset), no async API other
     than the batch.
3. **Evidence.** E4 output; files and lines above; ADR 0025 "Consequences" (no GPU formats or mipmaps until GRAPHICS-1).
4. **Industry.** Godot: `ResourceLoader` with registered format loaders, resources identified by path *and* UID
   (added in 4.x to survive renames), import step writing `.import` metadata and cached converted files. Unity:
   GUID-based `AssetDatabase` + Addressables (reference counting). Bevy: `Handle<T>` (strong/weak), `AssetServer` with
   pluggable `AssetLoader`s and `AssetReader` sources (file, embedded, HTTP). MonoGame: `ContentManager` returns live
   objects and disposes per manager — the model MicroEngine copied, and its documented pain point is exactly shared assets
   across managers.
5. **Alternatives.** (a) Keep live objects, add sibling sharing via the root cache; (b) handles with generation counters
   resolved through the cache; (c) full GUID database (editor-era feature, premature).
6. **Proposal.** (b) now, path-based identity with an optional stable id later: `Asset<T>` handle (struct: id +
   generation), `IAssetLoader<T>` registry keyed by type and extension, `IContentSource` (folder, zip/pak, APK, browser
   fetch), scopes become *owners* of handle references in a shared store (sharing across siblings, release when the last
   owner releases). Audio clips and music become asset kinds. Reserve an `IAssetImporter` seam and `*.meta` sidecar
   format for build-time conversion; implement only for GPU textures/shaders when GRAPHICS-1 needs them.
7. **Consequences.** Changes every `Assets.*` call site to return handles or keeps a convenience `Get` that resolves;
   enables 3D models/materials/shaders as packages without touching the core.

### F8 — Hidden defaults and coupled options

ASSET-1's `DefaultTextureFilter => TextureFilter ?? (Scaling == Smooth ? Linear : Nearest)` (`Platform.cs:32`) is not
isolated. Inventory:

| Default / coupling | Where | Effect | Classification |
| --- | --- | --- | --- |
| `CaptureScale` derived from `Scaling` (3 for pixel art, 1 for smooth) | `PlatformProfiles.cs:43` | Capture resolution depends on art style | Coupled option |
| `FixedStep = script != null \|\| hidden` | `GameLauncher.cs:79` | A hidden window changes the time model | Coupled option (F4) |
| `OpensOwnAudio`: `Automatic` → no device when hidden or fixed-step | `PlatformProfiles.cs:52-57` | Test mode decides audio | Coupled option |
| Seed: `Environment.TickCount` unless a script is given, then 1 | `GameLauncher.cs:380` | Randomness depends on whether a script exists | Hidden default |
| Max 5 steps, 0.25 s clamp | `GameRunner.cs:246,258` | Not configurable | Hidden default (F5) |
| Headless clock 60 Hz | `HeadlessPlatform.cs:40` | Ignores `UpdateRate` | Bug (F4) |
| F11 / Alt+Enter toggle full screen, inside each backend's event pump, and the key still reaches the game | `SdlPlatform.cs:365-367`, `RaylibPlatform.cs:148` | Not configurable; a game binding F11 gets both | Backend policy in the contract |
| Start screen only in real time; gamepads start after it | `GameRunner.cs:141`, `SdlPlatform.cs:172` | Device start order coupled to splash | Coupled option |
| Default 320×180 canvas, `Scale = 4`, `Scaling = Pixel` | `Platform.cs:18-26` | Pixel art is the default identity; `Scale` is meaningless for a 1920×1080 canvas | Pixel-art heritage |
| Fractional scaling only when whole factors "would waste a fifth" | ADR/architecture, backends | Magic 20% threshold | Hidden default |
| `VirtualControls` always created and added as an input source; `BitmapFont` always created | `Game.cs:138-148` | Every game pays for touch and the pixel font | Hidden default |
| Achievements flush every 1 s of simulated time | `Game.cs:166-170` | Writes saves at a fixed cadence | Hidden default |
| Asset `SlowLoadMilliseconds = 4`, `FrameBudgetMilliseconds = 4`, `BackgroundDecoding = !browser` | `AssetCache.cs:147-156` | Reasonable, but hitch warnings go to `Console.Error` | Hidden default (F12) |
| Songs rendered on threads unless browser (internal static) | `Audio.cs:213` | Not configurable per engine | Static policy |
| Headless excluded from automatic fallbacks | `GameLauncher.cs:164-171` | Good default, undocumented in API | Documented in code only |
| Default touch screen size headless = canvas × 4 | `HeadlessPlatform.cs` ctor | Test geometry assumption | Hidden default |

Proposal: every default lives in one documented settings object per subsystem with explicit values (no `null` meaning
"derived from another option"); derived defaults are computed in one visible "profile" (`GamePresets.PixelArt`,
`GamePresets.HighResolution2D`, later `Presets.ThreeD`) that the template picks explicitly. Classification: confirmed
problem, medium.

### F9 — One assembly for every module; namespaces are not boundaries

1. **Exists.** `MicroEngine.dll` holds kernel, 2D graphics model, text, image decoding, input + touch skins, audio
   synthesis and mixing, scenes + transitions, tile world + entities, UI widgets, achievements, saves, headless test
   platform and the CLI launcher: 173 public types (E5). Its package forces StbImageSharp and FontStashSharp on every
   game, including fully procedural ones (ADR 0025 "Consequences").
2. **Problem.** No module can be replaced or omitted at the package level; nothing enforces that `World` does not depend
   on `Core`, or that the kernel does not depend on UI (it does: §2). The SDK's promise of replaceable subsystems is
   enforced only by review. WASM and iOS (AOT, trimming) pay for unused code.
3. **Evidence.** §1.1, §2, E5.
4. **Industry.** MonoGame (Framework + Content.Pipeline + platform packages), Bevy (crate per subsystem behind feature
   flags), Stride (assembly per subsystem) — all enforce boundaries with the build graph.
5. **Alternatives.** (a) Keep one assembly, add architecture tests (NetArchTest-style) on namespaces; (b) split into
   assemblies by subsystem; (c) hybrid: few assemblies for stable boundaries, namespaces inside.
6. **Proposal.** (c): split where a replacement or an omission is real (kernel, graphics abstraction, 2D toolkit, input,
   audio, assets core, image/text decoders, scenes/world, UI, extras, testing), see §4.3; plus an architecture test that
   fails on forbidden references.
7. **Consequences.** More packages to version (they already share one version: cheap). Templates reference a meta
   package (`MicroEngine.Desktop`-like) so plug-and-play is unchanged.

### F10 — Public surface leaks

- `Game.InitializeForTest`, `Game.StepForTest` are public on the base class every developer derives from.
- `ContentFolder.DefaultRoot`, `SaveStore.DefaultStorage` are public mutable statics (F6).
- `HeadlessPlatform` and its fakes ship in the runtime core package (should be `MicroEngine.Testing`).
- `GameLauncher`/`LaunchArguments` (CLI parsing, `Console` output, `Environment` reads) live in the core namespace
  (moved there by ADR 0016 for reuse; a hosting assembly is the right home).
- `IPlatform` carries presentation details of today's 2D canvas: `SaveScreenshot` (of "the virtual frame"),
  `ToggleFullscreen` (also in `IDisplayControl`), `Letterbox` colour; and the hotkey policy (F8).
- `IGraphicsDevice.DefaultFilter` is engine state stored in the backend.
Classification: confirmed, low–medium.

### F11 — `IPlatform`: real contract or wrapper?

1. **Exists.** One interface (11 required members) plus `IRenderer` (13), `IGraphicsDevice` (2), `Texture` (3), `IInputSource`
   (3), `IAudioDevice` (11). ADR 0016 keeps window, renderer, graphics device and presentation together and allows
   replacing only audio and adding input.
2. **Assessment.** It *is* a real substitution contract at the level of the 2D feature set: the core references no backend
   type, the stub backend works (E2), and three implementations exist. But the contract's semantics are implicit and live
   in backend code: virtual-canvas scaling and letterboxing rules, capture scale, drawing the touch overlay after the frame
   and outside captures, the deterministic clock (F4), `OpensOwnAudio`, hotkeys, `DisplayRate` honesty (ADR 0024's pacing
   depends on it), `Wait` returning false for page-driven hosts. There is no conformance suite a third-party backend can
   run (`PlatformEquivalenceTests` compares the official backends with each other). Splitting window/graphics/audio/input
   was rejected because "it would promise combinations that cannot work" — true for window+renderer, not for audio,
   input, storage, file system or clock, which are bundled anyway.
3. **Cost of a new backend.** Syntactically ~32 members, a few hundred lines (E2 stub: ~80 lines). Semantically: the
   canvas/letterbox/scaling logic (SDL ~850 lines, raylib ~535 including it), the touch overlay canvas, display pacing, the
   fixed-step clock and suspension. A capability-based renderer (GRAPHICS-1) would multiply this unless graphics is split
   from window/events.
4. **Proposal.** Split `IPlatform` into `IPlatformHost` (events, lifecycle, clock, windows/surfaces), `IGraphicsBackend`
   created *from* a surface (keeps the "same context" constraint explicit through the type system instead of forbidding
   replacement), `IAudioBackend`, `IInputBackend`, `IFileSystem`/`IStorage`. Move canvas scaling, letterboxing, capture and
   overlay composition into a backend-independent presenter. Ship `MicroEngine.Testing.Conformance`: a test suite any
   backend runs.
Classification: architectural risk, medium.

### F12 — No engine events, no logging abstraction

`Game` gets `OnSuspended`/`OnResumed` only. There is no window resize, focus lost/gained, display change, DPI change, device
connected/disconnected, quit-requested-with-veto, low-memory or locale change event; the game must poll (`Input.LastDevice`,
`Screen`). Only a handful of C# events exist (`AchievementTracker.Unlocked`, UI widget events). Diagnostics are
`Console.Error.WriteLine` (`AssetCache.cs:471`, `GameLauncher.cs`), so a phone, browser or editor cannot route them.
Proposal: `EngineEvents` (typed, synchronous, dispatched on the game thread at a defined phase) and an `ILog` sink with a
console default. Missing capability, medium.

### F13 — Implicit threading model

The loop, scenes, assets and audio mixer run on one thread; `AssetCache.Preload` and song rendering use `Task.Run`
(`AssetCache.cs:340`, `Audio.cs:274`); `ContentFolder` uses a `ConcurrentDictionary` because workers check names
(`ContentFolder.cs` listing comment); SDL's background callback sets an `Interlocked` flag (`SdlPlatform.cs:254`). Nothing
documents or asserts which APIs are game-thread-only (e.g. `Texture.Update`, `AssetCache.*` are not thread-safe;
`Dispose` of a scope while a decode task runs drops it silently). Proposal: declare a game-thread affinity contract, a
`MainThread.Post` dispatcher, and a small job abstraction (worker pool vs. inline on WASM) used by assets and audio.
Latent risk, low now, required before multithreaded rendering or physics.

### F14 — Fixed virtual canvas

`GameSettings` is "read once when the game starts; keep it constant" (`Game.cs:33`); backends size their render target from
it. A game cannot change resolution at run time, react to aspect ratio (only letterbox), or render UI at native resolution
over a low-resolution world. That is the pixel-art model (legitimate as a preset) but it is the *only* model. With F3, the
target is views with their own resolution policy (fixed virtual, scaled, native). Design limitation, medium; rendering
domain owns the detail.

### F15 — Single-player assumptions in the platform contract

`IPlatform.Input` is one `IInputSource`, which has one `IsPadDown(PadButton)`, one `LeftStick`, `PadConnected`, `PadStyle`
and one `Pointer` (`Input.cs:54-75`); `Game.Input` is one `InputMap`; `CompositeInput` merges sources by "any source has it
down" (`PlatformProfiles.cs:233+`), which cannot express two players. Local multiplayer requires breaking the backend
contract, not adding a feature. Cross-domain (Input auditor owns the redesign); flagged here because it is in `IPlatform`.
Architectural risk, high.

### F16 — Achievements, toasts and saves wired into `Game`

`UseAchievements` builds a tracker and `AchievementToasts`; `Game.Step` updates and flushes them; `Game.Render` draws the
toasts with the bitmap font (`Game.cs:118-125,163-170,179`). These are reasonable *optional* features, but their coupling to
the base class creates the Core↔UI cycle and gives no seam for a platform achievement service (Steam, Game Center,
Play Games). Proposal: an optional module that subscribes to scheduler phases and renders in an overlay layer; achievements
behind an `IAchievementService` with a local default. Low.

### F17 — Legitimate specializations to keep (re-homed)

- **Fixed-step simulation with action-level scripted input and captures** (`GameRunner.cs`, `InputScript`): the right
  foundation for deterministic tests and replays. Keep; move the clock into the loop (F4).
- **Scoped asset lifetimes** (ADR 0025): good mental model; keep scopes as *owners* over shared handles (F7).
- **Managed decoders identical on every platform**: keep as default loaders, behind the loader registry (F7, F9).
- **Profile catalog with explicit failure messages and preference fallback** (ADR 0016/0024): keep as the platform-module
  selection mechanism.
- **Suspension handling** (no simulation, no time catch-up): correct; keep as an engine event.

---

## 4. Proposed target for this domain

### 4.1 Principles

1. The kernel is at the bottom of the dependency graph and knows no subsystem by type.
2. One composition model for every host and test; no process-wide statics.
3. Subsystems are modules with explicit lifecycle and scheduled phases; built-in ones use the same mechanism as user ones.
4. Capabilities are queried explicitly, never discovered by casting the platform object.
5. Every default is a named value in a settings object or an explicit preset; no option derives silently from another.
6. Assets are identified, owned and loaded through registries; the file system is a source, not a given.

### 4.2 Contracts (sketch)

```csharp
// ---- Kernel: composition and lifecycle (MicroEngine.Core)
public interface IEngineModule
{
    void Configure(EngineBuilder builder);        // register services, phases, loaders
}

public sealed class EngineBuilder
{
    public ServiceRegistry Services { get; }
    public FrameSchedule Schedule { get; }
    public EngineBuilder Add<TModule>(TModule module) where TModule : IEngineModule;
    public EngineBuilder Configure<TSettings>(Action<TSettings> edit) where TSettings : class, new();
    public Engine Build();
}

public sealed class ServiceRegistry                 // typed, no reflection, AOT/WASM safe
{
    public void Add<T>(T instance) where T : class;
    public void Replace<T>(T instance) where T : class;
    public T Get<T>() where T : class;
    public bool TryGet<T>([NotNullWhen(true)] out T? service) where T : class;
}

public enum FramePhase { BeginFrame, PreFixed, FixedUpdate, PostFixed, Update, LateUpdate, Render, Present, EndFrame }

public sealed class FrameSchedule
{
    public void Add(FramePhase phase, string name, Action<FrameContext> run, string? after = null, string? before = null);
}

public readonly record struct FrameContext(double FrameDelta, double FixedDelta, float Blend, long FixedStep, long Frame);

public sealed record TimeSettings
{
    public int FixedRate { get; init; } = 60;
    public int MaxStepsPerFrame { get; init; } = 5;
    public double MaxFrameDelta { get; init; } = 0.25;
    public TimeMode Mode { get; init; } = TimeMode.RealTime;   // Deterministic for scripted runs: loop-owned clock
}

public interface IEngineLifecycle                      // engine events, game thread, defined phase
{
    event Action<SurfaceResized>? Resized;
    event Action<bool>? FocusChanged;
    event Action? Suspending;  event Action? Resumed;
    event Action<QuitRequest>? QuitRequested;          // can veto
}

public interface ILog { void Write(LogLevel level, string category, string message); }

// ---- Platform split (abstractions in MicroEngine.Platform, implementations in backend packages)
public interface IPlatformHost : IDisposable
{
    IMonotonicClock Clock { get; }
    bool PumpEvents(IEngineEventSink sink);            // returns false when the app must stop
    ISurface? MainSurface { get; }
    bool TryGetCapability<T>([NotNullWhen(true)] out T? capability) where T : class; // IDisplayControl, ITouchScreen, IHaptics...
    bool CanBlock { get; }                             // page-driven hosts: false
    void Wait(double seconds);
}

public interface IGraphicsBackend : IDisposable        // created for a surface: the "shared context" rule in the type system
{
    GraphicsCapabilities Capabilities { get; }
    IGraphicsDevice Device { get; }                    // resources: textures, render targets, later buffers/shaders
    void BeginFrame(ISurface surface);
    void Submit(RenderFrame frame);                    // views + command lists; the 2D batch is one producer
    void Present();
}
public interface IGraphicsBackendFactory { string Name { get; } bool CanCreate(ISurface s, out string? why); IGraphicsBackend Create(ISurface s); }
public interface IAudioBackend : IDisposable { /* audio domain */ }
public interface IInputBackend { /* input domain: devices with ids, per-player assignment */ }
public interface IFileSystem { IContentSource Content { get; } IStorage Saves { get; } }

// ---- Assets (MicroEngine.Assets)
public readonly record struct AssetPath(string Value);              // normalised, case-checked
public readonly record struct Asset<T>(uint Id, uint Generation) where T : class;

public interface IContentSource                                     // folder, zip/pak, APK, browser fetch, overlay
{
    bool Exists(AssetPath path);
    Stream Open(AssetPath path);
    IReadOnlyList<AssetPath> List(AssetPath folder);
}

public interface IAssetLoader<T> where T : class
{
    IReadOnlyList<string> Extensions { get; }
    ValueTask<T> LoadAsync(AssetLoadContext context, CancellationToken cancel);  // decode may run on a worker;
                                                                                 // context.OnGameThread(...) for uploads
    // dependencies (atlas -> image) are declared through context.Depend(path)
}

public interface IAssetStore
{
    Asset<T> Load<T>(AssetPath path, IAssetOwner owner, object? options = null) where T : class;
    ValueTask<Asset<T>> LoadAsync<T>(AssetPath path, IAssetOwner owner, object? options = null, CancellationToken c = default) where T : class;
    T Get<T>(Asset<T> handle) where T : class;                       // throws a clear error on a stale generation
    AssetState StateOf<T>(Asset<T> handle) where T : class;
    void Release(IAssetOwner owner);                                 // scopes are owners; shared across siblings
    event Action<AssetPath>? Reloaded;                               // dev-only content watcher
}

public interface IAssetImporter                                       // build-time seam (later): glTF, GPU textures, shaders, SDF fonts
{
    bool Handles(string sourceExtension);
    void Import(ImportContext context);                               // reads source + .meta settings, writes runtime files
}

// ---- Game facade keeps plug-and-play
public abstract class Game : IEngineModule
{
    public virtual void Configure(EngineBuilder builder) { }          // optional: add modules, replace services
    protected abstract void Load();
    // Services, Input, Audio, Assets, Scenes... resolved from the registry for convenience
}
```

### 4.3 Physical structure (proposal for the integration architect)

| Package | Contents | Depends on |
| --- | --- | --- |
| `MicroEngine.Core` | Kernel: builder, registry, schedule, time, events, log, math (`Color`/linear colour, `RectF`, transforms over `System.Numerics`), capability query | — |
| `MicroEngine.Platform` | `IPlatformHost`, surfaces, file system/content sources, storage abstractions | Core |
| `MicroEngine.Graphics` | Device/resource abstraction, frame/views, `GraphicsCapabilities` | Core |
| `MicroEngine.Graphics2D` | Sprites, atlases, camera 2D, canvas presets (pixel / HD), text rendering client | Graphics, Assets |
| `MicroEngine.Assets` | Store, handles, loader registry, content sources (folder, zip) | Core, Platform |
| `MicroEngine.Assets.Images`, `MicroEngine.Text` | StbImageSharp loaders; FontStashSharp fonts | Assets, Graphics |
| `MicroEngine.Input`, `MicroEngine.Input.Touch` | Actions, devices, players; virtual controls | Core, Platform (+ Graphics2D for touch drawing) |
| `MicroEngine.Audio` (+ `.Synth`) | Mixer, clips as assets; synthesis | Core, Assets |
| `MicroEngine.Scenes`, `MicroEngine.World2D` | Scene stack/transitions; tiles, entities | Core, Graphics2D |
| `MicroEngine.UI`, `MicroEngine.Achievements`, `MicroEngine.Saves` | Optional modules | as needed |
| `MicroEngine.Hosting` | One launcher model, CLI arguments, profile catalog | Core, Platform |
| `MicroEngine.Testing` | Headless host, fakes, input scripts, capture, backend conformance suite | everything abstract |
| `MicroEngine.Backend.Sdl`, `.Raylib`, later `.SdlGpu` | Platform host + graphics/audio/input backends | abstractions |
| `MicroEngine.Desktop` (meta) | Default modules + SDL (+ raylib optional) | — |

An architecture test (in `MicroEngine.Testing` or the repo's tests) asserts the allowed edges.

### 4.4 Now / prepare / later

| Now (foundation, Phase II early) | Prepare (interfaces in place, minimal implementation) | Later |
| --- | --- | --- |
| Kernel builder + typed registry + phased schedule; `Game` as facade/module | `IGraphicsBackend` created from a surface; `RenderFrame` with views (2D batch only client) | Render graph, 3D renderer, materials |
| Loop-owned clock and `TimeSettings` (fixes F4/F5); variable `Update` phase | Engine transform types + optional interpolation service | Physics interpolation with a physics module |
| Explicit capability query; remove `ComposedPlatform` (fixes F1) | `IInputBackend` with device ids and players (input domain) | Network input sources |
| One composition path for desktop/web/phone/test; no statics (F6) | `IContentSource` beyond folders (zip/pak, browser fetch) | Mods/patch overlays, streaming |
| Asset handles, owners, loader registry; audio as assets (F7) | `IAssetImporter` + `.meta` format; GPU texture import when GRAPHICS-1 lands | glTF models, shader pipeline, SDF fonts, GUID ids, hot reload in editor |
| Settings objects + explicit presets (F8); remove derived nulls | `EngineEvents` complete set; `ILog` routing on phones/web | Editor tooling |
| Package split + architecture test (F9); `MicroEngine.Testing` with conformance suite (F11) | Job abstraction (workers / inline on WASM), main-thread dispatcher (F13) | Multithreaded render submission |

---

## 5. Open questions for cross-domain integration

1. **Rendering:** what is the unit the core schedules — a `RenderFrame` of views and command lists, or a renderer object per
   view? Who owns the virtual canvas policy (pixel/smooth/native) once views exist? Is screenshot capture a presenter
   function or a backend one?
2. **Rendering/platform:** can SDL_GPU, SDL_Renderer and raylib each be created from a shared `ISurface` abstraction, or does
   raylib's window-owns-context model force "platform host + graphics" to stay fused for that backend only?
3. **Input:** device identity and player assignment model, so `IPlatform.Input`'s single-pad shape (F15) can be replaced; are
   input events (text input, IME) needed in the kernel event pump?
4. **Audio:** are clips/music/streams asset kinds of the shared store (proposal), and does the audio backend need a
   release/unload contract (absent today)?
5. **Physics/world:** which phases does a physics module need (`PreFixed`/`FixedUpdate`/`PostFixed` + interpolation)? Which
   transform type is shared between physics, scenes and rendering?
6. **Infrastructure:** does the validation team want the deterministic clock and input scripts in `MicroEngine.Testing`, and a
   backend conformance suite as the cross-backend contract test (instead of pixel equality)?
7. **SDK/tooling:** acceptable replacement for `MicroEngineGameFactory` (a `Configure` entry point the generated hosts call)?
