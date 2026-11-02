# R2 — Industry research for the MicroEngine Phase I architecture audit

Date: 2026-11-02. Scope: engine core and composition, entities and scenes, physics, assets, audio, input.
Method: primary sources (engine docs, release posts, PRs, maintainer posts) fetched or searched on the date above.
Every material claim carries a URL in brackets referring to the numbered source list at the end.
Labels: **[fact]** = stated by the cited source; **[inference]** = my reading or extrapolation; **[unverified]** =
reported only by secondary sources or not confirmed against a primary source.

MicroEngine baseline used for comparison (from `docs/architecture.md`, ADRs 0003, 0016, 0020, 0025, read-only):
one core assembly; `GameLoop` fixed 60 Hz, at most five steps per frame, render once per frame with no interpolation
of state between steps; scene stack; plain `Entity` / `EntityList<T>` with deferred add/remove and depth-sorted draw;
no transform hierarchy; `TileMotion` swept AABB; `InputMap` of named actions with `IActionSource`; engine-side PCM
mixer and synth over an `IAudioDevice`; `AssetCache` with scopes, raw files decoded in managed code at load;
`IPlatform` with `PlatformProfile`/`PlatformCatalog`, replaceable audio and input parts, no DI container.

---

## 1. Engine core and composition

### 1.1 Godot: servers below, nodes above, MainLoop at the top

- **[fact]** Godot keeps core subsystems (rendering, physics, audio) as data-oriented "servers"; nodes are the
  user-facing interface to data those servers process. The article was written by Juan Linietsky in February 2021 [1].
- **[fact]** Servers are addressed through opaque RIDs that are allocated and freed manually; an RID is not a
  reference, so the owning resource must be kept alive. Older docs warned that servers may run asynchronously and
  polling them can stall [2].
- **[fact]** `MainLoop` is an abstract base class; `SceneTree` is the default implementation and a project can supply
  its own (`application/run/main_loop_type` or `-s script`) [3].
- **[fact]** Extension has two tiers: C++ modules compiled into the engine (deep access, rebuild required) and
  GDExtension shared libraries loaded at run time (no engine rebuild, "not as deep" access; forward but not backward
  compatible across minor versions; experimental in 4.4-era docs) [4].
- **[fact]** Godot's own priorities page admits that "there is still a lot of legacy code" after the 4.0 rewrite, and
  that many nodes "make it difficult for them to take advantage of multiple threads" [5].
- **Lesson [inference]:** the server/node split is what allowed Godot to swap physics backends (section 3) without
  touching the node API. The cost is a two-layer API and an RID lifetime model that users must learn when they go
  below nodes.

### 1.2 Unity: GameObject/MonoBehaviour, then DOTS, then "ECS for All"

- **[fact]** Entities 1.0 reached "Released" status in June 2023 for 2022 LTS, designed to coexist with GameObjects
  through conversion/baking workflows [6].
- **[fact]** In September 2024 Unity wrote that "many Unity creators have been unable to enjoy these benefits due to
  the advanced nature of ECS", and that transform data must be copied between GameObjects and Entities; the plan is
  GameObjects backed by entities, a unified transform, and removal of `Entities.ForEach` and Aspects [7].
- **[fact]** In December 2025 Unity changed course to deliver incrementally through Unity 6.x; the key step is
  replacing the integer InstanceID with an `EntityId` shared by GameObjects and Entities; by Unity 6.6 all ECS packages
  are core packages and the focus is still identifier alignment [8][9]. The public roadmap lists "ECS for All" as
  "Planned" [10].
- **Lesson [inference]:** two object models in one engine produced a decade-long reconciliation (DOTS announced
  2018; unification still in progress in late 2026). The expensive part is not the ECS itself but the *identity* and
  *transform* duplication between the two worlds — exactly what Unity is now unifying first.

### 1.3 Unreal: modules, UObject, subsystems, Mass

- **[fact]** Unreal subsystems are auto-instanced objects whose lifetime follows a parent: Engine, Editor,
  GameInstance, World or LocalPlayer [11][12].
- **[fact]** MassEntity is a separate data-oriented framework (fragments = data, stateless processors with
  `EntityQuery`, archetypes; composition changes deferred through `MassCommandBuffer`) that lives beside actors rather
  than replacing them [13]. Third-party samples still describe it as experimental [14] **[unverified as to current
  status]**.
- **Lesson [inference]:** lifetime-scoped subsystems are a cheap, discoverable alternative to a DI container:
  "where does this service live and when does it die" is answered by its base class.

### 1.4 Bevy: everything is a plugin over an ECS with schedules

- **[fact]** Plugin groups (`DefaultPlugins`) can be built and individual plugins disabled (`.disable::<T>()`) or
  replaced (`set`), keeping their slot for ordering; disabling does not remove the code from the binary — for that
  you drop cargo features or depend on sub-crates [15][16].
- **[fact]** Fixed-step work runs in `FixedUpdate`, driven by `RunFixedMainLoop` zero or more times per frame from
  `Time<Virtual>` and an overstep; interpolation systems belong in `AfterFixedMainLoop` [17].
- **[fact]** Bevy released 0.13–0.20 between February 2024 and October 2026 [18]; it is still pre-1.0 with breaking
  changes every release (see migration notes in 0.15 and 0.16 below).
- **Lesson [inference]:** Bevy demonstrates that "plugin = unit of replacement" scales, but its replaceability is
  paid for with a uniform ECS everywhere and continuous API churn.

### 1.5 .NET engines: MonoGame/FNA, Stride; others (Flax, O3DE)

- **[fact]** MonoGame (XNA model): `Game` owns a `GameComponentCollection` that drives `Update`/`Draw`;
  `Game.Services` is a `GameServiceContainer` where providers register by interface and consumers look up by type
  (`IGraphicsDeviceService` is the canonical example) [19][20].
- **[fact]** Stride is a .NET engine with an ECS-shaped model: `Entity`, `EntityComponent`, `EntityProcessor`
  (processors attached with `DefaultEntityComponentProcessor`, react to component add/remove) [21].
- **[fact]** O3DE is composed of "Gems" (modules with a system component); Gems communicate through EBus request
  and publish/subscribe interfaces; enabled Gems are listed per project in `enabled_gems.cmake` [22][23].
- Flax: not researched in depth in this pass (no primary source fetched) — **gap**.
- **Lesson [inference]:** the XNA service locator is the minimal form of replaceable subsystems; its known weakness
  is that lookups are untyped and failures are late (null at run time). MicroEngine's `PlatformProfile` +
  `PlatformParts` already gives stronger, earlier errors (ADR 0016).

### 1.6 Game loop

- **[fact]** The canonical pattern: accumulate real time, consume it in fixed `dt` steps, render a blend of previous
  and current state using `alpha = accumulator / dt`; guard against the spiral of death by capping steps or clamping
  frame time [24].
- **[fact]** Godot added 2D physics interpolation only in 4.3 (August 2024, forward-ported from 3.6), explicitly to
  fix position/camera jitter in 2D games; 3D followed later [25][26]. Teleports need an explicit
  `reset_physics_interpolation`, otherwise objects "streak" [27].
- **[fact]** Unity interpolation delays the pose by one physics step, trading latency for smoothness; it is off by
  default and enabling it hands the transform to physics [28].
- **Lesson [inference]:** every major engine that started with fixed-step-without-interpolation eventually added
  interpolation, and every one needed a "teleport/reset" escape hatch. MicroEngine's 60 Hz step on 120/144 Hz
  displays has the same jitter exposure.

---

## 2. Entities and scenes

- **[fact]** Godot's position: ECS pays off for "tens of thousands" of objects (city builders, large strategy);
  that is the exception; recommended escape hatches are culling, spreading work, the Servers API, compute, or a
  third-party ECS [1].
- **[fact]** The flecs author's FAQ: archetype storage (flecs, Bevy, Unity DOTS) favours iteration; sparse sets
  (EnTT) favour add/remove; a home-made basic ECS "will not outperform established implementations"; ECS fits
  poorly with tree/spatial structures; hierarchies are a known hard case (parent references are flexible but slow
  for transforms; depth sorting needs re-sorting; per-depth archetype subtrees fragment tables) [29].
- **[fact]** Bevy rebuilt its hierarchy in 0.16 as "relationships" (`ChildOf` / `Children`), removed the
  `bevy_hierarchy` crate, and made recursive despawn an opt-in hook [30].
- **[fact]** Bevy 0.15 made gamepads entities [31]; Bevy's scene notation (BSN) appears in 0.19 per a third-party
  site that also says no first-party `.bsn` loader ships yet [32] **[unverified]**.
- **[fact]** Unity's ECS-for-All plan prioritises a unified transform and mixed hierarchies [7].
- **Lessons [inference]:**
  - Hierarchies and identity are the parts every engine rebuilt (Bevy hierarchy → relationships; Unity InstanceID →
    EntityId; Godot node paths → UIDs for resources, section 4). Getting a stable entity identity early is cheap;
    retrofitting it is not.
  - For MicroEngine's scale (tens to hundreds of objects, ADR 0003) the evidence supports *not* adopting an ECS in
    the core. The open question is whether a 2.5D/3D vision needs a **transform hierarchy**; every 3D engine studied
    has one, and none of the ECS ones found it easy.

---

## 3. Physics

### 3.1 Box2D v3

- **[fact]** v3.0 (August 2024, 18+ months of work) is a rewrite in portable C17: C API of enums, structs and
  functions; ids instead of pointers (fewer double frees and orphans); Soft Step solver (soft constraints +
  substepping), more stable than v2.4 for mass ratios and long chains; graph colouring enabling an AVX2 contact
  solver; persistent islands; hybrid speculative/TOI continuous collision; pulley and gear joints removed [33].
- **[fact]** Multithreaded determinism in v3; cross-platform determinism requires disabling FMA and using Box2D's own
  trig approximations [34]; the FAQ states cross-platform determinism "as of version 3.1" as a first-class feature,
  with a snapshot system for rollback [35].
- **[fact]** v3.1 reworked sensors to be independent of body type and sleep, run at end of step, multithreaded and
  deterministic; contact events became disabled by default; events are buffered arrays read after the step [36][37].
- **[fact]** The rewrite has a cost: v3 "uses more complex algorithms than v2.4 and needs more testing" [38].
- Box3D: secondary sources report a 3D sibling from Erin Catto with cross-platform determinism (secondary posts only)
  **[unverified]**.

### 3.2 Jolt and Godot

- **[fact]** Jolt (MIT) is multi-core friendly, allows queries concurrent with simulation, is deterministic (with
  documented limits), offers optional double precision, and ships in Horizon Forbidden West and Death Stranding 2;
  3D only [39].
- **[fact]** Godot's priorities page said the Jolt *extension* "is not optimal" and that "numerous features ...
  can't be implemented in Godot due to the current way the system works"; the plan was Jolt as default 3D engine,
  with expected "compatibility issues" [5].
- **[fact]** Godot 4.4 integrated Jolt as an experimental, opt-in module (`physics/3d/physics_engine`) [40]; the
  docs list behavioural differences (margins actually used, Baumgarte strength, kinematic contact reporting, joint
  soft limits unsupported, ray-cast face index off by default) [41].
- **[fact]** Press coverage of Godot 4.6 (January 2026) reports Jolt as the default for *new* 3D projects, existing
  projects unchanged [42] **[secondary; official 4.6 notes not fetched]**.
- **[fact]** Godot has no comparable 2D replacement: community Rapier2D and Box2D extensions exist; the Box2D addon
  paused waiting for Box2D v3 and lists missing thread safety and determinism [43][44].
- **Lesson [inference]:** Godot's in-house physics did not fail for lack of effort; the server abstraction let it
  adopt Jolt, but the abstraction's shape (Godot's own semantics) still leaked: Jolt behaves differently behind the
  same nodes. A physics abstraction designed around one engine's semantics does not make engines interchangeable.

### 3.3 Rapier, Bepu, PhysX/Chaos; binding patterns

- **[fact]** Rapier: local determinism by default; cross-platform determinism with `enhanced-determinism`
  (IEEE 754-2008, WASM included), incompatible with `simd8`, compatible with `parallel` [45].
- **[fact]** Bevy integrations: bevy_rapier maintains a separate physics world and synchronises "a ton of data"
  each frame; Avian (successor of bevy_xpbd) stores physics state in Bevy's ECS and now runs in `FixedPostUpdate`;
  its own FAQ recommends rapier when maturity matters [46].
- **[fact]** Unity binds physics to transforms and, when interpolation is on, physics owns the transform; manual
  transform edits need `Physics.SyncTransforms` [28].
- **[fact]** FMOD and physics share the same rule of thumb: update attributes once per frame before the engine's
  own update (see 5.3).
- PhysX/Chaos: not researched in this pass — **gap** (out of reach for a .NET 2D engine in any case [inference]).

### 3.4 .NET bindings — status

| Library | Kind | Status (evidence) |
| --- | --- | --- |
| Box2D.NET (ikpil) | Pure C# port, MIT | Active repo (691 commits), samples on .NET 10; README says NuGet support "in the future"; which Box2D version it tracks is **not stated** on the README [47]. NuGet profile shows an older 2024 package [48] |
| JoltPhysicsSharp (amerkoleci) | P/Invoke bindings + native package | 2.18.x on NuGet (Sept 2025), net8/net9, separate native and debug native packages [49] |
| BepuPhysics2 | Pure C# 3D, Apache-2.0 | Stable 2.4.0 dates from Feb 2022; 2.5.0 betas through April 2026; targets .NET 8; author calls the API "highly nonidiomatic"; no determinism claim in README [50][51] |

**Lesson [inference]:** for 2D, Box2D v3 semantics (ids, buffered events, sensors, determinism) are the state of the
art, but the .NET options are either a port whose upstream version must be verified or a native binding that adds
per-platform native libraries (including WebAssembly, which MicroEngine ships through raylib). That is a packaging
risk, not only an API choice.

---

## 4. Assets

- **[fact] Godot:** a `.import` file next to each source stores import settings and must be committed; generated
  output goes to `.godot/imported/` (not committed); reimport on MD5 change; only imported resources ship unless
  "Keep File" [52]. Before 4.0 references were paths only and moves broke them; UIDs were added in 4.0 and extended to
  scripts/shaders in 4.4 via `.uid` sidecars; a central database was rejected (does not move with files, merge
  conflicts, breaks add-ons), checksums rejected (identical files collide) [53]. The priorities page still names
  importer performance as a problem [5].
- **[fact] Unity:** every asset and folder gets a `.meta` with a GUID and import settings; losing a `.meta` breaks
  references silently (materials lose textures, components lose scripts) [54]. Addressables use reference-counted
  `AsyncOperationHandle`s that must be released explicitly, even after failure [55].
- **[fact] Unreal:** Asset Manager with Primary/Secondary assets, `TSoftObjectPtr`, asset bundles and cook rules;
  a soft reference alone does not cook its target, a frequent "works in editor, null in shipping" failure [56].
- **[fact] Bevy Assets v2 (2023):** motivated by no preprocessing, no per-asset settings, no dependency waiting, no
  multiple sources; added `.meta` sidecars, a processor (`LoadAndSave`), three modes (unprocessed default, processed,
  processed_dev with hot reload), pluggable async readers, typed handles and dependency events; the author left
  identity as path-only (rename = rescan/reprocess, UUIDs deferred) and listed many follow-ups [57][58].
- **[fact] MonoGame/FNA:** XNB output requires the pipeline tool, which hinders modding; raw loading via
  `Texture2D.FromStream` is possible and recommended against only for cross-platform reasons in older threads [59];
  FNA's community largely loads raw assets [60] **[secondary; FNA's own post not fetched]**.
- **[fact] glTF in .NET:** SharpGLTF (Core / Runtime / Toolkit) reads and writes glTF 2.0; the README has described
  the API as still subject to change [61] **[current release status unverified]**. Godot imports `.blend` by having
  Blender export glTF [5].
- **Lessons [inference]:**
  - Path identity is the default everyone starts with and every editor-based engine moved away from (Godot UIDs,
    Unity GUIDs, Bevy deferred). The pressure comes from *editors and moves*; a code-first engine with paths in
    source code and compile-time listing (ADR 0025) has much less of it.
  - Build-time processing earns its cost when output format differs from input (GPU texture formats, shader
    compilation, mesh optimisation). For 3D this becomes true; ADR 0025's "files ship as they are" is correct for 2D
    and should be framed as a *mode*, like Bevy's `unprocessed` default, not a permanent rule.

---

## 5. Audio

- **[fact] miniaudio:** single-file C; low-level device API with a data callback; optional high-level `ma_engine`
  (node graph, sound groups, spatialisation with four attenuation models, no HRTF yet, resource manager with
  ref-counted or paged streaming and async jobs); WAV/FLAC/MP3 built in, custom decoders (Vorbis/Opus examples);
  backends from WASAPI to AAudio and Web Audio, plus a null backend [62].
- **[fact] SoLoud:** zlib licence; voices, filters, mixing buses, 3D positional audio, virtual voices; backends
  include miniaudio, SDL and a null backend for manual mixing; no artist tools; newest docs dated 2020 [63]
  **[maintenance status unverified]**.
- **[fact] Godot AudioServer:** buses route only right-to-left (no loops), effects in sequence, buses referenced by
  name (rename falls back to Master), silent buses auto-disabled; on the web, effects do not work in default
  "Sample" playback mode [64].
- **[fact] FMOD:** set listener and event 3D attributes once per frame, all before `Studio::System::update`; cache
  parameter handles rather than names [65].
- **[fact] SDL3:** the primary audio model is no longer a callback; apps bind `SDL_AudioStream`s to devices, and
  `SDL_OpenAudioDeviceStream` opens a device and stream together (device starts paused) [66].
- OpenAL Soft and Unity's audio stack: not researched in this pass — **gap**.
- **Lessons [inference]:**
  - Mixer ownership on the engine side (MicroEngine today) matches miniaudio's engine/device split and SDL3's stream
    model: the device is a sink. This keeps behaviour identical across backends and testable headless.
  - Godot's web caveat shows the cost of having two playback paths (engine mix vs. browser-native samples): features
    differ per platform. MicroEngine already streams OGG/MP3 through the device; any future bus/effect feature must
    state what happens on the device-streamed path.
  - Middleware (FMOD/Wwise) integration needs: per-frame attribute update ordered before the middleware update, and a
    way to turn off the engine mixer. That is a replaceable-audio-part case ADR 0016 already anticipates.

---

## 6. Input

- **[fact] SDL3:** the gamepad API sits over joysticks and reports positions (face buttons, shoulders) through a
  mapping database (`SDL_AddGamepadMapping*`, env var for unknown devices); offers button labels per gamepad type,
  player index, gyro/accelerometer sensors, rumble (incl. triggers), LEDs; hotplugging expected [67]. Text input is
  per window and no longer on by default (`SDL_StartTextInput(window)`), `SDL_SetTextInputArea` with cursor offset
  places IME candidates, hints merged into `SDL_HINT_IME_NATIVE_UI` [68].
- **[fact] Unity Input System:** rewritten because the old manager predated today's devices; the new one supports
  any number/mix of devices and device-change notifications, and moves device discovery to C# [69][70]. Its own team
  acknowledged "room to make things simpler" (2020) [71]; users reported basic tasks harder than before (2022) [72]
  **[user posts]**. Unity 6 now creates a project-wide action asset with default Move/Look/Jump bindings — a defaults
  answer to onboarding cost [73].
- **[fact] Unity local multiplayer:** `PlayerInputManager` joins players on a button press from an unpaired device,
  on a join action, or manually; optional split-screen; per-player UI needs extra modules [74].
- **[fact] Godot:** events propagate `_input` → GUI → `_shortcut_input` → `_unhandled_key_input` →
  `_unhandled_input` → picking, deepest node first; `set_input_as_handled` stops it; InputMap actions can be changed
  at run time but are not saved automatically [75]. InputMap has no per-player concept; users clone actions per
  device, and keyboard and first pad can both be device 0 [76] **[forum posts]**.
- **[fact] Bevy:** gamepads became entities in 0.15 [31]; leafwing-input-manager gives each player entity its own
  `InputMap`/`ActionState`; gamepads must be assigned explicitly (`set_gamepad`), otherwise a map listens to all pads;
  its example joins players by button combination and removes them on disconnect [77].
- **Lessons [inference]:**
  - Actions-over-devices (MicroEngine ADR 0020) is the consensus design; the rewrites happened where engines exposed
    devices first (Unity old manager) or had no user dimension (Godot).
  - The missing dimension in MicroEngine's `InputMap` is *the player*: a single global map works until local
    multiplayer, where every engine studied needed "one map per user + device pairing + join policy".
  - Unity's complexity complaints suggest keeping the simple path a one-liner and putting pairing, schemes and
    rebinding persistence behind optional APIs.
  - IME/text input is a separate channel from actions in every engine studied; it needs a text event stream and a
    caret rectangle, not more actions.

---

## 7. Transferable recommendations for MicroEngine

Format: **Adopt** (do now), **Prepare** (shape the API so it can be added without breaking), **Avoid**; with the
tradeoff. All are recommendations [inference] from the evidence above.

### 7.1 Engine core and composition
- **Adopt:** render-time interpolation support in `GameLoop`: expose `alpha` (accumulator / step) to `Draw` and a
  documented "teleport/reset" call, following [24][25][27]. *Tradeoff:* games must keep previous state (or the engine
  keeps previous positions for engine-owned objects); one step of visual latency, as Unity documents [28]; headless
  runs stay unaffected (alpha = 0 or 1).
- **Adopt:** keep the `PlatformProfile`/`PlatformParts` model and extend it by *lifetime-scoped* engine services
  (game-scoped, scene-scoped) in the Unreal-subsystem spirit [11] rather than a DI container or XNA-style untyped
  service lookup [19]. *Tradeoff:* less flexible than a container; much easier to read and to fail early.
- **Prepare:** split the core into assemblies along the seams that would be swapped (physics, audio mixing, 2D
  rendering model, UI) with the Bevy rule "a plugin is the unit of replacement and must be removable from the
  binary", not only disabled [15]. *Tradeoff:* more packages to version (SDK release cost, ADR 0022).
- **Avoid:** a second object model beside `Entity` (Unity's two-world cost, [7][8]). If a data-oriented layer is
  ever needed, make it internal to a subsystem (Godot servers [1], Mass beside actors [13]), not a parallel
  gameplay model.

### 7.2 Entities and scenes
- **Adopt:** a stable entity/object identity (an id type, not references or list indices) before serialization or
  prefabs exist; every engine retrofitted this at great cost [8][30][53].
- **Prepare:** an optional transform hierarchy (parent, local/world transform, dirty propagation) designed so 2D
  games without it pay nothing; settle hierarchy-despawn semantics up front (Bevy made it opt-in [30]).
  *Tradeoff:* a hierarchy is the biggest step towards 2.5D/3D and the most frequently rebuilt part elsewhere.
- **Avoid:** a core ECS. Evidence from Godot [1] and the flecs FAQ [29] says the benefit appears at tens of thousands
  of objects and a home-made ECS rarely beats established ones; MicroEngine's target scale is hundreds (ADR 0003).

### 7.3 Physics
- **Adopt:** keep `TileMotion` as the default kinematic path for platformers; it is deterministic and simple.
- **Prepare:** a physics *module* boundary shaped after Box2D v3 semantics — body/shape ids, step with substeps
  inside the fixed step, events read as buffered arrays after the step, sensors separate from contacts [33][36] —
  so Box2D (via Box2D.NET or a native binding) or a 3D engine (Jolt [39], Bepu [50]) can sit behind it.
  *Tradeoff:* Godot shows the abstraction leaks backend semantics [41]; define the contract by observable behaviour
  and test it per backend rather than promising interchangeability.
- **Prepare:** decide the binding direction: engine entities *own* transforms and physics writes back after the step
  (avoids the bevy_rapier double-world sync cost [46] for small worlds), with interpolation from 7.1.
- **Avoid:** writing a general rigid-body solver in-house (Godot's experience [5]); and adopting a native physics
  dependency before checking WebAssembly packaging and determinism flags (FMA off, trig [34]) on every target.
  Verify Box2D.NET's upstream version before relying on it [47].

### 7.4 Assets
- **Adopt:** keep raw files and path identity for 2D (ADR 0025) — the evidence for GUIDs comes from editor-driven
  moves [53][54], which a code-first engine does not have.
- **Prepare:** an optional *processed mode* (Bevy's unprocessed/processed/processed_dev [57]) for 3D, where glTF
  (SharpGLTF [61]) and GPU texture formats make build-time work worth it; keep loader settings in code or a
  sidecar, never in a central database (Godot rejected it [53]). Add typed handles with load-state and dependency
  completion before async/streaming.
- **Avoid:** mandatory compiled formats (XNB friction for modding and tooling [59]); explicit reference counting
  exposed to games (Addressables leaks when a handle is not released [55]) — MicroEngine's scopes are the safer
  default; and "editor works, build fails" divergence (Unreal cook [56]) — keep the build's content check (ADR 0025).

### 7.5 Audio
- **Adopt:** keep the engine-owned mixer with the device as a sink (matches miniaudio and SDL3 streams [62][66]);
  add named buses with right-to-left (acyclic) routing and fall-back-to-master semantics like Godot [64].
- **Prepare:** a listener + emitter position API (2D pan/attenuation now, 3D later) updated once per fixed step and
  pushed before the mixer update — the FMOD ordering rule [65]; and an "engine mixer off" mode so a middleware or
  miniaudio-based `IAudioDevice` can replace mixing via the existing replaceable audio part (ADR 0016).
- **Avoid:** two playback paths with different feature sets without documenting it (Godot web caveat [64]); state per
  feature whether device-streamed music participates.

### 7.6 Input
- **Adopt:** keep actions-only (ADR 0020); add SDL3 gamepad metadata the core can use: gamepad type and button labels
  for prompts, player index/LED, mapping-database loading [67].
- **Prepare:** a *player/user* dimension: `InputMap` per player with explicit device pairing and a join policy
  (unpaired-device press / join action / manual), mirroring Unity's `PlayerInputManager` [74] and leafwing's
  per-entity maps [77]; keep the single global map as the default so single-player code is unchanged. Add rebinding
  persistence through `SaveStore` (Godot leaves this to every game [75]).
- **Prepare:** a text-input channel separate from actions: start/stop per window, text and composition events,
  caret rectangle for IME (SDL3 [68]); needed for name entry and chat, and on phones for the soft keyboard.
- **Avoid:** Unity-style mandatory assets/schemes for the simple path [71][72]; and implicit "listen to all pads"
  in multiplayer (leafwing's pitfall [77]) or keyboard-and-pad sharing device 0 (Godot pitfall [76]).

---

## Gaps in this pass
Flax engine, PhysX/Chaos, OpenAL Soft, Unity audio internals, Unity Package Manager modularisation history,
official Godot 4.6 release notes (Jolt default reported only by press), Box2D.NET upstream version, BSN status,
current SharpGLTF release label. Threading/job systems were covered only through Box2D (enkiTS, graph colouring) and
Jolt; no general job-system survey.

---

## Sources

1. Godot — Why isn't Godot an ECS-based game engine? (J. Linietsky, 2021-02-26): https://godotengine.org/article/why-isnt-godot-ecs-based-game-engine/
2. Godot docs — Optimization using Servers: https://docs.godotengine.org/en/stable/tutorials/performance/using_servers.html
3. Godot docs — MainLoop class: https://docs.godotengine.org/es/stable/classes/class_mainloop.html
4. Godot docs — What is GDExtension: https://docs.godotengine.org/de/4.4/_sources/tutorials/scripting/gdextension/what_is_gdextension.rst.txt
5. Godot — Priorities: https://godotengine.org/priorities/
6. Unity — DOTS development status, November 2023: https://discussions.unity.com/t/dots-development-status-and-next-milestones-november-2023/933505
7. Unity — DOTS development status and milestones + ECS for all (September 2024): https://discussions.unity.com/t/dots-development-status-and-milestones-ecs-for-all-september-2024/1519286
8. Unity — ECS Development Status, December 2025: https://discussions.unity.com/t/ecs-development-status-december-2025/1699284/1
9. Unity — ECS Development Status for Unity 6.6: https://discussions.unity.com/t/ecs-development-status-for-unity-6-6/1724480
10. Unity roadmap — ECS for All: https://activation.unity3d.com/roadmap/2700-ecs-for-all
11. Unreal API — USubsystem: https://dev.epicgames.com/documentation/unreal-engine/API/Runtime/Engine/USubsystem
12. Game Developer — Unreal-style singletons with subsystems: https://gamedeveloper.com/game-platforms/unreal-style-singletons-with-subsystems
13. Unreal docs — Overview of Mass Entity: https://dev.epicgames.com/documentation/unreal-engine/overview-of-mass-entity-in-unreal-engine
14. MassCommunitySample: https://github.com/getnamo/MassCommunitySample
15. Bevy Cheatbook — Plugins (0.13): https://Bevy-cheatbook.github.io/programming/plugins.html
16. Bevy — PluginGroupBuilder: https://dev-docs.bevyengine.org/bevy/app/struct.PluginGroupBuilder.html
17. Bevy — RunFixedMainLoopSystem: https://docs.rs/bevy/latest/bevy/app/enum.RunFixedMainLoopSystem.html
18. Bevy news (release list): https://bevy.org/news/
19. MonoGame — What is the Game Loop: https://docs.monogame.net/articles/getting_to_know/whatis/game_loop/
20. MonoGame API — Game: https://docs.monogame.net/api/Microsoft.Xna.Framework.Game.html
21. Stride docs — ECS usage: https://doc.stride3d.net/4.2/en/manual/engine/entity-component-system/usage.html
22. O3DE — Code Gems: https://docs.o3de.org/docs/user-guide/programming/gems/code-gems/
23. O3DE — Key concepts: https://docs.o3de.org/docs/welcome-guide/key-concepts/
24. G. Fiedler — Fix Your Timestep!: https://gafferongames.com/post/fix_your_timestep/
25. Godot 4.3 dev 6 snapshot: https://godotengine.org/article/dev-snapshot-godot-4-3-dev-6/
26. Godot 4.3 beta 1 / release: https://godotengine.org/article/dev-snapshot-godot-4-3-beta-1/ , https://godotengine.org/releases/4.3/
27. Godot docs — Using physics interpolation: https://docs.godotengine.org/en/4.4/tutorials/physics/interpolation/using_physics_interpolation.html
28. Unity manual — Rigidbody interpolation: https://docs.unity3d.com/Manual/rigidbody-interpolation.html
29. S. Mertens — ECS FAQ: https://github.com/SanderMertens/ecs-faq
30. Bevy — relationships PR #17398 / hierarchy: https://doc.qu1x.dev/bevy_trackball/src/bevy_ecs/hierarchy.rs.html ; https://lib.rs/crates/bevy_121
31. Bevy 0.15 release: https://bevy.org/news/bevy-0-15/
32. Tainted Coders — BSN (secondary): https://taintedcoders.com/bevy/bsn
33. E. Catto — Releasing Box2D 3.0: https://box2d.org/posts/2024/08/releasing-box2d-3.0
34. E. Catto — Determinism: https://box2d.org/posts/2024/08/determinism/
35. Box2D FAQ: https://box2d.org/documentation/md_questions.html
36. E. Catto — Box2D 3.1: https://box2d.org/posts/2025/04/box2d-3.1
37. Box2D v3.1 release notes: https://box2d.org/documentation/md_release__notes__v310.html
38. Box2D repository: https://github.com/erincatto/box2d
39. Jolt Physics repository: https://github.com/jrouwe/JoltPhysics
40. devclass — Godot 4.4 released (Jolt, .NET 8): https://devclass.com/2025/03/05/godot-4-4-released-open-source-game-engine-adds-jolt-physics-net-8-and-more/
41. Godot docs — Using Jolt Physics: https://docs.godotengine.org/en/stable/tutorials/physics/using_jolt_physics.html
42. GameFromScratch — Godot 4.6 released (secondary): https://gamefromscratch.com/godot-4-6-released/
43. godot-rapier-physics: https://github.com/appsinacup/godot-rapier-physics
44. godot-box2d README: https://github.com/appsinacup/godot-box2d/blob/main/README.md
45. Rapier — Determinism: https://rapier.rs/docs/user_guides/rust/determinism
46. Avian docs / README: https://docs.rs/avian2d ; https://raw.githubusercontent.com/avianphysics/avian/main/README.md ; https://github.com/Jondolf/avian/pull/457
47. Box2D.NET: https://github.com/ikpil/Box2D.NET
48. NuGet profile ikpil: https://packages.nuget.org/profiles/ikpil
49. JoltPhysicsSharp on NuGet: https://www-0.nuget.org/packages/JoltPhysicsSharp
50. bepuphysics2: https://github.com/bepu/bepuphysics2
51. BepuPhysics on NuGet: https://www.nuget.org/packages/BepuPhysics
52. Godot docs — Import process: https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/import_process.html
53. Godot — UID changes coming to Godot 4.4: https://godotengine.org/article/uid-changes-coming-to-godot-4-4/
54. Unity manual — Asset metadata: https://docs.unity.com/en-us/engine/current/manual/assets-and-media/import-assets/asset-metadata
55. Unity Addressables — AsyncOperationHandle: https://docs.unity3d.com/Packages/com.unity.addressables@2.9/manual/AddressableAssetsAsyncOperationHandle.html
56. Unreal — Asset management / async loading: https://dev.epicgames.com/documentation/unreal-engine/asynchronous-asset-loading-in-unreal-engine ; https://dev.epicgames.com/documentation/unreal-engine/asset-management-in-unreal-engine?lang=ja
57. Bevy Asset V2 (description carried in PR #8503 page): https://github.com/bevyengine/bevy/pull/8503
58. Bevy Asset V2 follow-up tracking issue #9714: https://github.com/bevyengine/bevy/issues/9714
59. MonoGame community — Modable games and XNB: https://community.monogame.net/t/modable-games-and-xnb/2028 ; https://community.monogame.net/t/is-there-any-alternative-to-load-content-in-monogame/12877
60. Habr — MonoGame/FNA raw assets (secondary): https://habr.com/ru/articles/1039344/
61. SharpGLTF: https://github.com/vpenades/SharpGLTF
62. miniaudio manual: https://miniaud.io/docs/manual/index.html
63. SoLoud: https://solhsa.com/soloud/
64. Godot docs — Audio buses: https://docs.godotengine.org/en/stable/tutorials/audio/audio_buses.html
65. FMOD — Studio API 3D events: https://fmod.com/docs/2.03/api/studio-api-3d-events.html ; Unity integration examples: https://www.fmod.com/resources/documentation-unity?page=examples-basic.html&version=2.02
66. SDL3 — SDL_OpenAudioDeviceStream / migration: https://wiki.libsdl.org/SDL3/SDL_OpenAudioDeviceStream ; https://wiki.libsdl.org/SDL3/README-migration/raw
67. SDL3 — CategoryGamepad: https://wiki.libsdl.org/SDL3/CategoryGamepad
68. SDL3 — SDL_StartTextInput: https://wiki.libsdl.org/SDL3/SDL_StartTextInput/raw ; imgui SDL3 issue #7754: https://upd.dev/ocornut/imgui/issues/7754
69. Unity blog — Introducing the new Input System: https://blog.unity.com/de/engine-platform/introducing-the-new-input-system
70. Unity forum — Input System Update: https://discussions.unity.com/t/input-system-update/685526
71. Unity forum — Why I didn't end up using the new input system (2020): https://forum.unity.com/threads/feedback-why-i-didnt-end-up-using-the-new-input-system.829482/
72. Unity forum — First impressions (2022): https://forum.unity.com/threads/feedback-first-impressions.1221033/
73. Unity Input System — Project-wide actions: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/about-project-wide-actions.html
74. Unity Input System — PlayerInputManager: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/PlayerInputManager.html
75. Godot docs — Using InputEvent: https://docs.godotengine.org/en/stable/tutorials/inputs/inputevent.html
76. Godot forum — local multiplayer input threads: https://forum.godotengine.org/t/split-screen-player-devices/42321 ; https://forum.godotengine.org/t/help-local-multiplayer-device-conflict/128696
77. leafwing-input-manager: https://github.com/leafwing-studios/leafwing-input-manager ; https://docs.rs/crate/leafwing-input-manager/latest/source/examples/register_gamepads.rs
