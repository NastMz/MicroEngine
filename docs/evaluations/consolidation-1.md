# CONSOLIDATION-1: cross-cutting audit of the 2D engine

Audit of 2026-11-19, on `main` at 37c264c. It covers every subsystem, not only the open issues, and asks one question
of each: what does a developer outside this repository have to write, connect or maintain to make a game, and which of
that should the engine own. Task record: [`odd/tasks/consolidation-1.md`](../../odd/tasks/consolidation-1.md).

Evidence was read from source unless a line says "docs". Nothing here was measured: this audit makes no performance
claim, and none of its decisions rests on one.

## 1. Method

A capability is judged in this order: the need (which games, which problem), what a developer writes today, what exists
to reuse, what a reasonable integration would look like, where the engine's responsibility ends, how it fits the
existing contracts, the trade-offs, the decision, and what proves it. Performance is a constraint on a design, checked
when a concrete doubt exists.

Six statements are not conclusions by themselves: no demo needs it; the current system is fast enough; it needs
infrastructure; a library can be wired by hand; it changes an API; the game can implement it. Earlier evaluations
closed questions with them. Section 4 lists where.

Interventions use four levels: **conserve**, **extend** (the architecture is right, contracts or capabilities are
missing), **remodel** (responsibilities or APIs are reorganized) and **rebuild**.

## 2. Diagnosis

The engine's individual subsystems are sound. Almost every deficiency found is one of five structural causes, and most
of the closed "defer" decisions are consequences of them rather than independent choices.

### 2.1 A module can register, but cannot live (foundation)

`IEngineModule.Configure(EngineBuilder)` is the only hook: services, frame systems by phase, capabilities, settings and
five window events (`src/MicroEngine.Kernel`). After that everything is hard-coded:

- a scene's coroutines, tweens, world and widgets are fixed members ticked in a fixed order (`Scene.cs:193-212`),
  hashed by a closed list (`Scene.cs:183-191`) and torn down by fixed code (`Scene.cs:339-342`);
- `Game.ComputeStateHash` is a closed list of sections (`Game.cs:635-672`), `Game.Shutdown` is fixed (`Game.cs:604-611`);
- there is no scene enter, exit, push or pop notification for anything outside the scene class itself.

Consequences seen: `samples/EcsSwarm` creates, ticks, hashes and destroys its store by hand in four places; a game adds
a physics world to `PhysicsSystem` by hand; Tiled, Navigation, Box2D and Text.Shaping are plain libraries with no
composition entry; `TrackAchievements` is special-cased inside `Game` (`Game.cs:265-283`). ADR 0059 concluded that an
ECS integration was "glue of five places that reads better in the game" precisely because the engine offers no place
to put it. An ECS module, a state-machine runner, localization and per-scene physics all need the same missing thing:
a system that belongs to a scene and is stepped, hashed and disposed with it.

**Decision: extend.** Scene systems (a per-scene list with begin-step, step, end-step, hash and dispose, in declared
order) and module lifecycle events (started, scene entered and exited, shutdown, a state-hash section).

### 2.2 Two graphics type families and two render paths (foundation)

`MicroEngine.Graphics` holds a façade (`Texture`, `IGraphicsDevice`, `IRenderer`, `Sprite`, in a folder named `Legacy`
that is in fact the primary public API) and a device layer (`Devices.Texture`, `Devices.IGraphicsDevice`,
`Renderer2D`). The bridge is one-way and internal: `DeviceTexture`'s constructor is internal
(`src/MicroEngine/Graphics/DeviceCanvas.cs:68`), and `IRenderer` has no material, state, target or depth parameter.

Consequences seen:

- a render target, a compressed texture or any device texture can never be a `Texture` or a `Sprite`;
- a custom shader takes ten steps and about 45 lines of glue (`samples/Examples/Examples/WaterExample.cs`): a cast, a
  second `Renderer2D`, `Flush()`, shaders loaded from `Assets.Bytes`, a hand-written copy of the sprite vertex shader
  and five manual `Dispose` calls;
- shaders, materials, render targets and `ShapedFont` are created and disposed by hand, while textures, atlases and
  fonts are owned by asset scopes;
- no device declares `Mipmaps`, there is no post-processing helper and no hook at window resolution.

The rejected KTX work (PR #172) needed its own renderer for exactly this reason: `Assets.Texture` reaches the device
only through `CreateTexture(PixelImage)`. The same split forced GRAPHICS-1's materials onto a side path. One cause, two
rejected or awkward results.

**Decision: remodel.** One texture type over the device texture, state and material on the normal draw path, and
shaders, materials and render targets as assets with scope ownership. Post-processing, mipmaps and compressed textures
then become extensions on top instead of parallel paths.

### 2.3 The content seam stops at loading (foundation for content)

`AssetLoaderRegistry` is a good extension point and Tiled uses it. Beyond it: `IAssetImporter` is declared and never
called (`AssetLoaders.cs:140`); `Preload` rejects audio and custom kinds (`AssetCache.cs:319-336`); there is no reload;
packaging copies `Content/**` unvalidated. All four demos slice sheets with hand-typed tables
(`games/NeonRift/Art/Assets.cs:36-65`), hard-code frame rates, and refer to content by strings that fail at run time.
`microengine shaders --check` already shows the shape of an optional offline step whose output goes into `Content/`.

**Decision: extend**, on top of 2.2. Section 3 has the scope.

### 2.4 Closed representations

Three subsystems chose a representation that cannot carry what the backend or the device already provides:

- **Input.** A binding is five fixed lists: keys, pad buttons, left-stick direction, pad axes, chords
  (`InputMap.cs:55-62`). Mouse buttons, wheel and pointer motion have no place in it, in its JSON or in
  `ListenForPress`. The documented reason is "no game uses it" (`docs/manual/input.md:794`). Also: the right stick has
  no radial dead zone or direction binding, pointer buttons have no edges, there is no pointer delta or cursor lock.
- **Audio.** Every voice is added straight into the output buffer with flattened bus gains
  (`AudioEngine.cs:752-843`). A bus is a volume. No effect, filter or limiter can be inserted by anyone, and the stated
  reason is "nothing measured asks for them" (`docs/evaluations/audio-1.md:128`). The voice and bus API works only
  when the device is a `MixerAudioDevice`; any other `IAudioDevice` silently returns no voice. There are no parameter
  ramps, no playback position, no public decoder registration, and `.mp3` music loads everywhere but decodes only on
  raylib.
- **Physics.** The contract is thin against Box2D 3.2: no joints (the `Joints` flag exists, no API uses it, the native
  layer has no joint call), no forces, impulses, angular velocity or damping, queries limited to closest ray and box
  overlap, and no way to reach the native world. A rope, a vehicle or a physics puzzle cannot be built on the
  published API. No evaluation ever weighed this: the contract was simply not extended.

**Decision: remodel** the binding representation and the mixer's render graph; **extend** the physics contract.

### 2.5 The engine's contracts are not what its games use

Every demo moves characters with the static `TileMotion` helper and its own gravity, grounded and jump logic (five
copies), never through `IPhysicsWorld2D`; every demo constructs `new SaveStore(id, dir)` and bypasses the host's
storage; two demos and a sample hand-write save versions; eight state machines are hand-rolled; none uses a tool
atlas. The contracts were validated by samples and benchmarks while the games kept their older private code, which is
how "no demo needs it" stayed true for capabilities the demos would use if they were written today.

**Decision:** modernizing the demos and samples is part of proving each intervention, not a later cosmetic pass.

### 2.6 Substitution is uneven

Services are found by exact concrete type and the built-ins are sealed (`AssetCache`, `AudioMixer`, `InputMap`), so
`Services.Replace` swaps an instance, never an implementation. Substitution really happens at five different seams:
platform profiles, `Services.Replace`, physics by `new`, saves through the host, and a second host-side registry.

This is narrower than it looks. The seams that matter are below the façades and exist: platform, device, audio sink,
storage, content source, physics world, asset loader. **Decision: conserve** the façades as concrete types; **extend**
the seams where a capability needs one (audio effects, decoders, the ECS store, state-machine persistence) and document
one table of what is replaceable and where. A blanket interface over every façade would add indirection without a
consumer.

## 3. Decisions by area

| Area | Level | What changes |
| --- | --- | --- |
| Kernel, scenes | Extend | Scene systems; module lifecycle events; state-hash sections; timers (2.1) |
| Graphics resources | Remodel | One texture type; state and material on `IRenderer`; shader, material and render-target assets (2.2) |
| Rendering capabilities | Extend | Post-process chain and present-stage hook; mipmaps; built-in sprite vertex shader; uniforms by name; draw depth |
| Content (#113) | Extend | Asset manifest and generated names; `.aseprite` and sheet descriptions; offline atlas packing through `microengine content`; validation; preload for every kind; reload during development; platform variants |
| State machines (#171) | Extend | A runner integrated with the clock, hashing, saves and animation; library adoption decided on evidence |
| ECS (#151) | Extend | Optional `MicroEngine.Ecs` over a mature library, as a scene system; ADR 0059 superseded if the modelling comparison supports it |
| Input | Remodel | One control representation covering pointer buttons, wheel and motion; bindings format v2; pointer edges, delta, cursor control; per-stick dead zones |
| Audio | Remodel | Per-bus submix; effect contract with built-in filter, reverb, compressor and limiter; ramps; position and loop points; decoder registry; listener and emitters |
| Physics | Extend | Dynamics API, joints, shape and point queries, a character controller, tile geometry on Box2D, an explicit native escape hatch |
| Navigation | Extend | Path follower, steering and separation, clearance, reachability, time-sliced search |
| Persistence | Extend | Versioned saves with registered migrations, slot listing, settings store |
| Localization | New | String tables, language change, localized prompts, fonts and assets per language |
| UI, cameras, particles, tweens, Tiled, saves' safety, diagnostics | Conserve | Re-read and found sound |

Not recommended now, each because nothing in the audit showed a developer blocked by it: an event bus, networking,
multiple render targets, stencil, MSAA, a lighting system, SDF text, a 2D navmesh, hex grids, a rebinding screen, a
visual editor, cloud saves.

## 4. Earlier decisions reopened

| Decision | Reason given | Status |
| --- | --- | --- |
| ADR 0059, no first-party ECS | store cost, "glue of five places" | Reopened (#151); modelling never evaluated |
| ADR 0060 / PR #172, KTX side path | load time and memory | Rejected by the owner; replaced by 2.2 and 2.3 |
| ADR 0055:40, ANIM-1 §5, state machines are game rules | "a second way to write `switch`" | Reopened (#171) |
| INPUT-1, mouse not bindable | "no game uses it" | Reopened |
| AUDIO-1, no effects, listener, ducking, crossfade | "nothing measured asks for them" | Reopened |
| NAV-1 / ADR 0056, behaviour stays in games | "No measured need" | Reopened for following, steering, clearance |
| ADR 0031, no joint API | none; never evaluated | Reopened |
| ADR 0057, no post-processing stack, effects as examples | "no scenario asked for one" | Reopened |
| ADR 0025, no content step | measured on demo-sized images | Reopened (#113) |
| SAVE-1, migrations left to the game | "a game upgrades its own" | Reopened |
| Localization | never evaluated | New |

Re-read and kept: no skeletal runtime (licence), UI as primitives (ADR 0050), Box2D optional with tiles as default
(ADR 0051), Tiled through DotTiled (ADR 0052), no shaders in browsers (no SDL_GPU on the web), the spatial index
beside the world (ADR 0058).

## 5. Order

Two foundations come first because other work lands on them:

1. **MODULE-1** (2.1): before state machines, ECS, localization and physics-per-scene.
2. **RENDER-1** (2.2): before content processing and rendering capabilities.

Then the capabilities, smallest dependency first: state machines, input, saves, audio, content, ECS, physics,
navigation, localization, rendering capabilities. The demos and samples are migrated as each lands, and a final pass
removes what is left of the private infrastructure.

## 6. Not verified

Android and iOS behaviour of anything above; Metal; whether `sdl-gpu` runs on phones (docs do not say); every library
named in the task record until its own evaluation checks version, licence, trimming and browser support.
