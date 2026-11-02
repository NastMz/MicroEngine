# A3 — Simulation-side domains: input, audio, world/entities/scenes, physics, UI

Phase I adversarial audit, MicroEngine branch `docs/arch-1` (includes the unmerged ASSET-1 work). Read-only on the
repository. Experiments live in
`<scratch>\exp-sim\`
(`lib\` holds a copy of `src/MicroEngine/bin/Debug/net10.0/MicroEngine.dll`, built 2026-11-02 15:27, newer than the
last commit to the audited files; `probe\` is a console project that references it).

Scope: `src/MicroEngine/{Input,Audio,World,Scenes,UI}`, the SDL and raylib implementations of `IInputSource`,
`IAudioDevice`, `ITouchScreen`, and how the four demos and the samples use them. The vision is a generalist, modular,
multi-platform engine for 2D, 2.5D and 3D, with plug-and-play defaults that are coherent, documented and replaceable.

---

## 1. Inventory

| Area | Types (file) | Size | Notes |
| --- | --- | --- | --- |
| Input contract | `Key`, `PadButton`, `PadAxis`, `InputDevice`, `GamepadStyle`, `PointerState`, `IInputSource`, `IActionSource`, `ActionBindings`, `InputMap` (`Input/Input.cs`) | 412 | One polled snapshot interface; one global action map |
| Touch | `ITouchScreen`, `TouchEvent`, `ITouchOverlay`, `IOverlayCanvas`, `FrameLayout` (`Input/Touch.cs`); `TouchLayout`, `TouchControl` records, `VirtualControls` (`Input/VirtualControls.cs`); `ITouchSkin`, `DefaultTouchSkin` (`Input/TouchSkin.cs`) | 104 + 795 + 273 | Engine-owned on-screen controls (ADR 0020) |
| Prompts | `ActionPrompts`, `ActionPrompt` (`Input/ActionPrompts.cs`), `PromptText` (`UI/PromptText.cs`) | 381 + 144 | Device-aware "Press {confirm}" text |
| Backend input | `SdlInput` (`Sdl/SdlInput.cs`), `SdlTouchScreen`, `RaylibInput` (`Raylib/RaylibPlatform.cs:434+`), `RaylibTouchScreen`, `BrowserTouch`, `HeadlessInput`, `CompositeInput` (`Core/PlatformProfiles.cs:232`) | | |
| Audio contract | `AudioClip`, `SoundHandle`, `MusicHandle`, `IAudioDevice`, `SilentAudioDevice`, `AudioMixer` (`Audio/Audio.cs`) | 448 | Mixer is a name→handle bank plus one music channel |
| Synthesis | `Synth`, `SfxParams`, `Wave` (`Audio/Synth.cs`); `Song`, `Instrument` (`Audio/Song.cs`) | 142 + 247 | Offline render to PCM |
| Audio test device | `CaptureAudioDevice` (`Audio/CaptureAudioDevice.cs`) | 164 | A managed software mixer used as the test oracle |
| Backend audio | `SdlAudioDevice` (16 SDL streams, NVorbis), `RaylibAudioDevice` (miniaudio via raylib, 4 aliases per sound) | 302 + ~200 | |
| World | `TileFlags`, `TileLayer`, `ICollisionGrid`, `TileGrid`, `Autotile`, `TileRenderer` (`World/TileMap.cs`); `TileMotion`, `MoveResult` (`World/TileMotion.cs`); `Entity`, `EntityList<T>` (`World/Entities.cs`) | 174 + 126 + 213 | |
| Scenes | `Scene`, `SceneManager`, `Transition` + 4 built-ins (`Scenes/Scene.cs`); `CoroutineRunner`, `Wait`, `Tweener` (`Scenes/Coroutines.cs`) | 322 + 146 | Stack of scenes, per-scene coroutines/tweens |
| UI | `Typewriter`, `UiTheme`, `PanelStyle`, `DialogueBox`, `SelectionList` (`UI/Widgets.cs`); `AchievementToasts`, `AchievementsScene` (`UI/AchievementViews.cs`) | 274 + 174 | Immediate, action-driven widgets |
| Physics | — | 0 | No physics contract, no body, no collider, no contact event, no broadphase anywhere in `src/` (grep for physics/rigidbody/collider/box2d: no hit) |

Composition (`Core/Game.cs:36-55, 134-151`): `Game` hard-wires exactly one `InputMap` (`CreateInput`), one
`VirtualControls` (`CreateTouchLayout`) added as an action source, one `ActionPrompts`, one `AudioMixer` over
`platform.Audio`, one `SceneManager`, one `AssetCache`, one `BitmapFont`. `Scene` exposes them through `Game`
(`Scenes/Scene.cs:32-40`). There is no service registry; replacing any of these means replacing `Game`.

Loop ordering (`Core/GameRunner.cs:251-274`, `Core/Game.cs:153-174`): per frame `UpdateTouch` (drains
`ITouchScreen.ReadTouches`, `Input/VirtualControls.cs:482`); per fixed step `Input.Sample(platform.Input)` →
`Scenes.Update` → `Audio.Update` (fades, device streaming refill) → achievements → `Input.EndUpdate`.

---

## 2. Findings table

Classes: **CP** confirmed problem, **AR** architectural risk, **DL** design limitation, **LS** legitimate
specialization, **MC** missing capability, **NE** needs experiment.

| ID | Finding | Class | Severity |
| --- | --- | --- | --- |
| IN-1 | The input model is one player: one `InputMap`, one gamepad, all devices merged | CP / DL | **High** |
| IN-2 | `IInputSource` is a polled snapshot of a fixed, tiny device vocabulary; no events, no text, no wheel, no rumble | MC | **High** |
| IN-3 | Raw touch is consumed by the on-screen controls; games cannot read gameplay touches or gestures | MC | **High** (mobile) |
| IN-4 | No input consumption or priority between scenes, overlays and UI | DL | Medium |
| IN-5 | Key prompts and bindings assume a QWERTY layout | CP | Medium |
| IN-6 | Window shortcuts (F11, Alt+Enter) are hard-coded in both backends | CP | Low |
| IN-7 | Rebinding exists (`Unbind`/`GetBindings`) but has no persistence, no "listen for next input", and actions are untyped strings | DL | Medium |
| AU-1 | Mixing lives in three different places with three different semantics (SDL: 16 global voices; raylib: 4 per sound; capture oracle: unlimited) | CP | **High** |
| AU-2 | `IAudioDevice` is fire-and-forget: no voice handle, stop, loop SFX, per-instance control, buses, effects | MC | **High** |
| AU-3 | Exactly one music channel; no layering, crossfade between two tracks, ambience or streaming SFX | DL | Medium |
| AU-4 | Audio is outside the asset lifecycle: nothing is ever unloaded | CP | Medium |
| AU-5 | No positional/spatial audio; both demos that need it compute pan themselves, differently | MC | Medium (2D) / High (3D) |
| AU-6 | Streaming is fed from the simulation step on the main thread with a 0.25 s buffer | AR | Medium |
| AU-7 | `AudioMixer.Play` "not within one frame" actually dedups per fixed step | CP | Low |
| WO-1 | No physics contract: no body, collider, contact, query or step; demos write O(n²) overlap loops | MC | **Critical** for the vision |
| WO-2 | `TileMotion` is a platformer kinematic mover documented as "swept AABB"; it tunnels through thin extra solids and ignores solids that move into the body | CP / LS | **High** |
| WO-3 | `Entity` is a 2D sprite object (Vector2 + RectF + `Draw(IRenderer)` + float depth): no transform, hierarchy, identity, components or serialization | DL | **High** |
| WO-4 | Interpolation between fixed steps is left to games; three demos reimplement it; ADR 0023 is stale about it | CP | **High** |
| WO-5 | `EntityList` throws when an entity clears the list during `Update`; removal is O(n²) worst case | CP | Low |
| SC-1 | Scenes are a flat stack with service-locator access to `Game`; no world/scene tree, no scene result passing, no additive scenes | DL | Medium |
| SC-2 | A scene that did not step still receives the global `FrameBlend`; every interpolating scene must track this itself | CP | Medium (part of WO-4) |
| SC-3 | Coroutines and tweens are tied to scenes and to the fixed step; no unscaled/real time, no time scale, no hitstop | DL | Medium |
| UI-1 | UI widgets are action-driven only: no pointer/touch hit testing, no layout, no focus model, no text input | MC | Medium |
| UI-2 | UI sizing is the pixel-art unit of the bitmap font (`UiUnit(font)`) | LS / DL | Low |
| DM-1 | Demos already build a simulation/presentation split (sinks, `Tick`/`Render`, `PlayerInput`) that the engine does not provide | CP (missing capability made visible) | **High** |

---

## 3. Findings in detail

### IN-1 — One player, one gamepad, all devices merged (CP/DL, High)

1. **What exists.** `Game` owns a single `InputMap` (`Core/Game.cs:40, 139`). `IInputSource.IsPadDown(PadButton)`,
   `LeftStick`, `GetPadAxis`, `PadConnected`, `PadStyle` take no device index (`Input/Input.cs:54-75`). `SdlInput` opens
   "one gamepad (the first connected; the next one takes over when it is unplugged)" (`Sdl/SdlInput.cs:8, 36-53`).
   `RaylibInput` reads gamepad `0` only (`Raylib/RaylibPlatform.cs:484, 510, 524`). Hosts that add input sources get a
   `CompositeInput` that ORs keys/buttons and keeps the largest stick (`Core/PlatformProfiles.cs:232-285`).
   `InputMap.Sample` folds keyboard, pad, sticks, axes and every `IActionSource` into one action state
   (`Input/Input.cs:229-286`).
2. **Problem.** Local multiplayer (couch co-op, versus, hot-seat with two pads) is impossible without bypassing the
   engine: there is no device identity to assign to a player, no second pad, and no way to know which device pressed an
   action except the global `LastDevice`. `InputMap.CreateDefault()` binds both WASD and arrows to the same actions
   (`Input/Input.cs:403-411`), so even split-keyboard needs a second map, which `Game` cannot hold.
3. **Evidence.** Experiment E5 (`exp-sim\probe\Program.cs`): keyboard holding Left and gamepad holding DPadRight in the
   same sample give `left=True right=True Axis=0 LastDevice=Gamepad` — two people cancel each other out. No demo has two
   players; MeadowRun (`games/MeadowRun/Gameplay/PlayerTuning.cs`, built in `games/MeadowRun/Scenes/PlayScene.cs:66-68`)
   and NeonRift (`games/NeonRift/Gameplay/Player.cs`) introduce their own `PlayerInput` struct to decouple rules from
   the global map.
4. **Industry.** Unity's new Input System moved from a global `Input` class to `PlayerInput` + `InputUser` pairing
   devices to users; Godot passes a `device` id on every `InputEvent` and leaves player assignment to the game (a known
   pain point in its proposals); Unreal's Enhanced Input is per `LocalPlayer`. SDL3 exposes stable `SDL_JoystickID`s and
   player indices.
5. **Alternatives.** (a) Keep one map, add device index to bindings (Godot style). (b) `InputMap` per `PlayerSlot`, with
   a `DeviceAssigner` policy (Unity style). (c) Leave it to games (current state; rejected: it needs backend changes).
6. **Proposal.** Devices get identity (`DeviceId`, kind, name, style, connected/disconnected events). `IInputSource`
   becomes a device registry that reports per-device state. Action maps are instantiated per player
   (`InputUser`), each with a device set; a default `SinglePlayerAssignment` gives player 1 every device, which keeps
   today's plug-and-play behaviour. See the contract sketch in §7.
7. **Consequences.** Breaking change to `IInputSource` and to `Scene.Input`; `VirtualControls` becomes a device
   belonging to a user; `ActionPrompts` must be per user. Prepare now even if only one player ships, because the
   device vocabulary is a backend contract that is expensive to widen later.

### IN-2 — Polled fixed vocabulary; no events, text, wheel, rumble (MC, High)

1. **What exists.** `Key` has 66 members: letters, digits, arrows, a few modifiers, F1–F5, F11, F12
   (`Input/Input.cs:6-13`): no F6–F10, numpad, punctuation, Alt, RightControl, Home/End, Delete. `PointerState` is
   position + three buttons (`Input/Input.cs:51`), no wheel. No text-input or IME API anywhere in `src/` (grep for
   `TEXT_INPUT`/`SDL_StartTextInput`: none). The only haptics is phone vibration on `ITouchScreen`
   (`Input/Touch.cs`); no gamepad rumble, gyro, touchpad, battery. Input is a per-sample poll; SDL key events are only used
   to latch taps (`Sdl/SdlInput.cs:22-32`).
2. **Problem.** Name entry, chat, debug consoles, editor tooling, rebinding to any key, strategy/mouse-heavy games and
   accessibility remappers all need either more keys, text events or wheel. A polled `bool IsKeyDown(Key)` also cannot
   express ordering within a frame (combo inputs, double taps faster than a step), which the engine already works around
   with an 8-frame latch.
3. **Evidence.** As cited; `ActionPrompts.KeyName` (`Input/ActionPrompts.cs:309+`) can only name the same small set.
4. **Industry.** Every mature engine pairs an event stream (ordered, timestamped, with text/IME composition events) with
   derived polled state: SDL3 itself, Godot `_input`/`_unhandled_input`, Unity's event-based Input System, MonoGame added
   `TextInput` after years of complaints.
5. **Alternatives.** Grow the enum only (cheap, keeps the poll model, still no text); full event stream + derived state.
6. **Proposal.** Backend contract becomes `IInputBackend.Drain(List<InputEvent>)` with typed events (key with
   scancode + keycode, text, composition, pointer with id/button/wheel, gamepad button/axis, device added/removed);
   the core builds polled state from it, so `InputMap` keeps its API. `Key` covers the full USB HID usage table.
7. **Consequences.** Both backends and the headless source change; scripted input (`InputScript`) can replay events,
   which is more faithful than forcing actions.

### IN-3 — Touch is owned by on-screen controls (MC, High for mobile)

1. **What exists.** `ITouchScreen.ReadTouches(List<TouchEvent>)` (`Input/Touch.cs:23+`) is drained once per frame by
   `VirtualControls.Update` (`Input/VirtualControls.cs:482`). SDL's touch→mouse synthesis is turned off
   (`Sdl/SdlPlatform.cs:90-91`), so touches never appear as `Pointer` either.
2. **Problem.** A game cannot implement tap-to-move, drag-and-drop, swipe, pinch-zoom, a tappable menu or a touch-first
   puzzle game: the touches it would need were already consumed and are not forwarded. ADR 0020 solved "virtual gamepad
   on phones" well, but only that.
3. **Evidence.** As cited; `samples/Examples/Examples/CameraExample.cs:50-52` uses `Input.Pointer` for world picking,
   which never fires on a phone. No demo uses gameplay touch.
4. **Industry.** Godot: touches are `InputEventScreenTouch/Drag` delivered to UI first, then to `_unhandled_input`; Unity:
   `Touchscreen` device + `EnhancedTouch` + gesture packages; UI systems sit before gameplay in the routing.
5. **Proposal.** Touches become pointer events (with pointer id and kind) in the event stream of IN-2; `VirtualControls`
   becomes one consumer in a routing chain (UI → on-screen controls → gameplay), each marking events handled. A small
   gesture recogniser (tap, long press, swipe, pinch) is an optional module.
7. **Consequences.** `ITouchScreen` loses the overlay/drawing responsibilities that belong to the renderer side
   (cross-domain with A1/A2).

### IN-4 — No input consumption between scenes and UI (DL, Medium)

1. `Scene.Input => Game.Input` (`Scenes/Scene.cs:34`); `SceneManager.Update` ticks every scene from the first
   non-blocking overlay up (`Scenes/Scene.cs:180-182`), and all of them read the same map. `Input.Reset()` on every
   push/pop (`Scenes/Scene.cs:142, 154`) is the only arbitration: global, and it hides a held key's `Pressed` from both.
2. A non-blocking overlay (HUD menu, chat, toast with buttons) and the scene under it both see `Pressed("confirm")`.
   Games avoid this by always blocking (no demo sets `BlocksUpdateBelow = false`).
4. Godot's `set_input_as_handled` / `_unhandled_input`, Unity's action map enable/disable and UI event consumption.
6. Per-scene (or per-layer) action-map contexts with priority and `Consume(action)`; the touch-context idea of
   `Scene.TouchActions` (`Scenes/Scene.cs:71`) generalised to all devices.

### IN-5 — QWERTY assumptions (CP, Medium)

`SdlInput` maps `Key` to positional scancodes (`Sdl/SdlInput.cs:55-73`) — correct for gameplay — but `ActionPrompts.KeyName`
(`Input/ActionPrompts.cs:309`) names keys by the enum, so an AZERTY player is told "Press A" for the key labelled Q, and
"WASD" defaults are ZQSD on that layout. Fix: keep scancodes for bindings, ask the backend for the layout's label
(`SDL_GetKeyFromScancode` + `SDL_GetKeyName`; `KeyboardEvent.key` in browsers) for prompts.

### IN-6 — Hard-coded window shortcuts (CP, Low)

`Sdl/SdlPlatform.cs:364-368` and `Raylib/RaylibPlatform.cs:147` toggle fullscreen on F11 and Alt+Enter before the game
sees the key. A game that binds F11 cannot opt out. Make it a default action ("toggle-fullscreen") in the default map,
handled by a removable engine behaviour.

### IN-7 — Rebinding without persistence or capture (DL, Medium)

`Unbind`/`GetBindings` exist (`Input/Input.cs:196-205`); there is no serialisable binding format, no "wait for the next
input" helper and actions are `string` (typos are silent: `Down("jmup")` is just false, `Input/Input.cs:319`). Propose
an `InputActionSet` description (data, serialisable through `SaveStore`), optional source-generated typed action ids,
and a rebinding helper fed by the event stream.

### AU-1 — Three mixers with three semantics (CP, High)

1. **What exists.** SDL: 16 `SDL_AudioStream` voices shared by every sound, round-robin, each `Play` clears the next
   voice whatever it is playing (`Sdl/SdlAudioDevice.cs:15, 139-159`); panning copies the whole clip into a pooled
   buffer on every play (`:145-149, 165-176`). raylib: 4 voices *per sound* via `LoadSoundAlias`, unlimited overall
   (`Raylib/RaylibAudioDevice.cs:15, 56-57, 92-101`). `CaptureAudioDevice` — the device tests use as oracle — mixes in
   managed code with an unlimited voice list (`Audio/CaptureAudioDevice.cs:77-83, 110-150`).
2. **Problem.** The same game sounds different per backend (17 overlapping explosions: the first is cut on SDL, kept on
   raylib), and tests cannot detect voice stealing because the oracle never steals. The engine already owns a correct
   managed mixer (the capture device) but uses it only for tests. Every future feature (buses, ducking, effects, 3D)
   would have to be written three times.
3. **Evidence.** Code as cited; the capture device's own remark says it reproduces requests "not the exact resampling or
   timing of a sound card" (`CaptureAudioDevice.cs` header).
4. **Industry.** FNA/MonoGame moved to FAudio (an engine-side XAudio2 mixer) for identical behaviour on every platform;
   Godot mixes in its own `AudioServer` and treats drivers as PCM sinks; SDL3 itself recommends `SDL_mixer` or own
   mixing on top of streams; miniaudio's `ma_engine` is a whole mixer with sound groups and spatialisation.
5. **Alternatives.** (a) Engine-side managed mixer; device = `IAudioOutput` that pulls interleaved float frames on its
   own thread. (b) Adopt miniaudio (`ma_engine`) as the single mixer on all platforms, including WebAssembly (raylib
   already embeds it). (c) Keep per-backend mixing and specify the semantics (rejected: three implementations of every
   feature).
6. **Proposal.** (a) as default, with (b) as an evaluated alternative implementation (needs a latency/CPU experiment on
   Android and browser — NE). One `AudioEngine` in the core: voices with priority and stealing policy, buses, effects
   chain, streaming sources, resampling. Backends shrink to `IAudioOutput { Format; Start(IAudioRenderCallback) }`. The
   capture device becomes "the same mixer with a file sink", so tests exercise the real mixing.
7. **Consequences.** `IAudioDevice` is replaced; the browser output needs an AudioWorklet or ScriptProcessor bridge
   (cross-domain with platforms/web). FMOD/Wwise integration becomes "replace `AudioEngine`", which needs the
   game-facing API to be an interface, not `AudioMixer`.

### AU-2 — Fire-and-forget API (MC, High)

`IAudioDevice.Play(SoundHandle, volume, pitch, pan)` returns `void` (`Audio/Audio.cs:138`); `AudioMixer.Play` likewise
(`:327-334`). There is no way to stop a sound, loop an engine hum, fade a single instance, change pitch of a playing
car engine, or know when a sound ended. Propose `SoundInstance` handles (generation-checked struct) with `Stop`,
`Volume`, `Pitch`, `Pan`/`Position`, `Loop`, `IsPlaying`, and a `Bus` per instance.

### AU-3 — One music channel (DL, Medium)

`PlayMusic` replaces the current track (`Audio/Audio.cs:140`); fades are serial (fade-out, then start,
`Audio/Audio.cs:351-368, 434-445`). No simultaneous crossfade, stems, ambience bed, or streaming of long SFX/voice.
Music becomes just a streaming source on the "music" bus in the AU-1 design.

### AU-4 — Audio outside the asset lifecycle (CP, Medium)

`IAudioDevice` has no unload; `SdlAudioDevice` keeps every clip and every music closure (`_sounds`, `_music`,
`Sdl/SdlAudioDevice.cs:20-21, 100-136`) for the life of the device. `AssetCache` (ADR 0025) has no audio
(`grep -i audio src/MicroEngine/Core/AssetCache.cs`: no hit) and `AddSoundFile`/`AddMusicFile` read
`File.ReadAllBytes(path)` directly (`Audio/Audio.cs:308, 315`). A game with per-level music grows memory without bound
and audio does not participate in scope release or hitch reporting. Cross-domain with the assets auditor.

### AU-5 — No positional audio; demos do it themselves (MC)

MeadowRun: `pan = clamp(0.5 + (x - camera.Center.X)/480, 0.15, 0.85)` (`games/MeadowRun/Gameplay/Stage.cs:288-292`).
NeonRift: `pan = clamp((x - view.Left)/view.Width, 0.1, 0.9)` (`games/NeonRift/Gameplay/Stage.cs:339`). Two formulas,
no distance attenuation, no listener. Propose an `AudioListener` (position, orientation; 2D uses z = 0) and per-instance
emitter position with attenuation curves; HRTF/3D panning is a later implementation of the same contract.

### AU-6 — Streaming fed from the simulation step (AR, Medium)

`AudioMixer.Update` is called from `Game.Step` (`Core/Game.cs:164`), which calls the device's `Update`; SDL refills to a
0.25 s target, decoding Vorbis on the main thread (`Sdl/SdlAudioDevice.cs:219-234, 286-298`). A load hitch longer than
~250 ms (ADR 0025's cache only *reports* such hitches) underruns music; the five-step cap
(`Core/GameRunner.cs:258, 274`) means at most five refills per frame anyway. Needs an experiment (NE) to measure on
Android; the AU-1 pull-thread design removes the risk.

### AU-7 — Dedup "per frame" is per step (CP, Low)

`_frame` increments in `Update` (`Audio/Audio.cs:432`), called per fixed step, and is stored cast to `int`
(`:331`); the doc says "within one frame" (`:325`). Harmless today; wrong documentation of an implicit policy that should
be a configurable voice-limiting rule in AU-1.

### WO-1 — No physics contract (MC, Critical for the vision)

1. **What exists.** Nothing named physics. Collision is `TileMotion` (kinematic box vs grid) plus each game's own
   `Bounds.Intersects` loops: 24 call sites in the demos, e.g. `games/MeadowRun/Gameplay/Stage.Rules.cs:183, 189, 223`,
   `games/NeonRift/Gameplay/Stage.cs:214`. `EntityList.Overlapping` is a linear scan (`World/Entities.cs:104`) and no demo
   uses it. TrailDuel ignores the World namespace entirely and writes its own grid occupancy
   (`games/TrailDuel/World/MapRuntime.cs:53, 96-106`). CourtyardKey implements its own `ICollisionGrid`
   (`games/CourtyardKey/World/Room.cs:66`).
2. **Problem.** A generalist engine needs a place where a physics engine plugs in: bodies tied to entity transforms,
   collision layers/masks, queries (raycast, overlap, shape cast), contact/trigger events and a step in the fixed
   update. Today Box2D or Jolt would have to be wired by each game, with no transform to sync to (WO-3) and no
   step hook other than calling it from `Scene.Update`.
4. **Industry.** Godot separates `PhysicsServer2D/3D` (replaceable: Jolt replaced GodotPhysics3D as an option in 4.4) from
   nodes; Unity has separate 2D (Box2D) and 3D (PhysX) worlds with a shared component shape; Bevy has no built-in physics
   and relies on Rapier/Avian plugins against its transform. Lesson: the contract is "bodies, shapes, queries, events,
   step, interpolation", the implementation is swappable, and 2D and 3D are distinct worlds.
5. **Alternatives.** (a) Ship Box2D v3 (C, MIT, deterministic, has C# bindings) as the default 2D world and Jolt
   (JoltPhysicsSharp) later for 3D, behind `IPhysicsWorld2D/3D`. (b) Write a managed 2D engine (rejected: reinvention).
   (c) Keep kinematic helpers only (rejected for the vision; fine for a tile-platformer module).
6. **Proposal.** A `MicroEngine.Physics` contract assembly (no implementation in the core): world, body handle, shape,
   filter, query, contact events, fixed-step hook, transform sync. `MicroEngine.Physics.Box2D` as the first
   implementation. `TileMotion` survives as a *kinematic character controller for tile worlds*, in a 2D gameplay module,
   and can read the same `ICollisionGrid`. See §7.
7. **Consequences.** Needs WO-3 (a transform) and the fixed-step phase list (cross-domain with core/loop). Determinism of
   scripted tests depends on the physics implementation being deterministic for a given platform (Box2D v3 claims
   cross-platform determinism; NE).

### WO-2 — `TileMotion`: a platformer mover documented as swept AABB (CP/LS, High)

1. **What exists.** `TileMotion.Move` resolves X then Y in sub-steps of `TileSize / 2` with discrete overlap tests
   (`World/TileMotion.cs:40-68`); extra `solids` that the box already overlaps before a sub-step are skipped
   (`:112-118`); one-way tiles are a platformer rule (`:106-107`). `docs/architecture.md:78` calls it "swept AABB".
2. **Problem.** It is not swept: the sub-step size depends on the grid's tile size, not on the obstacle. Extra solids
   thinner than a sub-step minus the body height are tunnelled; a platform moving up into a resting body is ignored and
   the body ends embedded. It is a legitimate specialisation (tile platformer kinematics) placed in a generic `World`
   namespace and presented as general collision.
3. **Evidence.** Experiment E1: a 2×2 body moving 15.9 px down through a 4 px solid at y 100..104 ends at y = 105.9,
   `HitFloor=False` (tunnelled). E2: a 10×10 body resting at bottom 90, platform raised to top 88: after a 2 px fall
   the body's bottom is 92 (embedded 4 px), `HitFloor=False`, `IsOnGround=False`. E3 (tiles only) behaves correctly
   (`y=78, HitFloor=True`).
4. **Industry.** Celeste/TowerFall-style "actors and solids" handle moving solids by having the *solid* push/carry
   actors; Godot's `move_and_slide` sweeps shapes against the physics server; Box2D uses TOI for bullets.
6. **Proposal.** Rename and relocate (`Gameplay2D.TileCharacterMover`), correct the docs, add a real swept test against
   extra rectangles (or move "solids" to a moving-solid model that pushes actors), and keep it physics-engine
   independent as a kinematic helper.
7. **Consequences.** MeadowRun (`games/MeadowRun/Gameplay/Actors/Actor.cs` `MoveBody`), NeonRift
   (`games/NeonRift/Gameplay/Actor.cs:137-141`) and CourtyardKey use it; their tuning must be preserved by regression
   playthroughs.

### WO-3 — `Entity` is a 2D sprite object (DL, High)

1. `Entity` = `Vector2 Position`, `RectF Hitbox` default 16×16, `float Depth`, `Alive`, `Update(dt)`,
   `Draw(IRenderer)`, `Removed()` (`World/Entities.cs:14-31`). `EntityList<T>` sorts by `Depth` and draws, optionally
   culling with `Camera2D` (`World/Entities.cs` `Draw`).
2. No transform (rotation, scale, z), no parent/child hierarchy, no stable identity (handles), no way to find an entity
   by id, no components, no serialisation, no lifecycle beyond `Removed`, and simulation and drawing live on the same
   object (`Draw(IRenderer)`), so a headless simulation must avoid calling `Draw` and a 3D renderer cannot consume it.
   The 16×16 default hitbox is a pixel-art tile assumption.
3. Demos override it: MeadowRun's `Actor` seals `Update`, adds `Velocity`, `PreviousPosition`, sleeping and a separate
   `Render(IRenderer, Assets, float)` "so the stage can run headless in tests"
   (`games/MeadowRun/Gameplay/Actors/Actor.cs` header and body); NeonRift and CourtyardKey add their own previous
   position and draw position (`games/NeonRift/Gameplay/Actor.cs:13-31`, `games/CourtyardKey/Entities/Actor.cs:38-54`);
   TrailDuel's `Actor` does not derive from `Entity` at all (`games/TrailDuel/World/Actor.cs:13`).
4. ADR 0003 rejected ECS for "tens to hundreds of objects" — reasonable for those games, but the decision bundled
   "no ECS" with "no transform, no hierarchy, no separation of state and presentation", which are orthogonal. Godot
   (nodes, no ECS) and Unity (GameObject + Transform) show a non-ECS model can still have a transform hierarchy;
   Bevy/Flecs show ECS can be the substrate. The common denominator every engine has is a `Transform` with a parent.
6. Proposal: a minimal *world model* independent of rendering: `EntityId` (generation-checked), `Transform`
   (position, rotation, scale in 3D with 2D helpers), optional parent, a small component/attachment mechanism (typed
   attachments on an entity, not mandatory ECS), and presentation as separate attachments read by renderers
   (sprite, mesh, light). Keep a simple `Entity` subclass API as the plug-and-play default on top. Decide ECS vs
   node/attachment after a prototype (NE) — the contract that must exist either way is ids + transforms + attachments.
7. Breaks `Entity`/`EntityList`; demos migrate their actor bases (they already wrap them).

### WO-4 / SC-2 — Interpolation is reimplemented by every game (CP, High)

1. `Game.FrameBlend` is global (`Core/Game.cs:76`). ADR 0023 decides "Interpolation is the game's choice … the engine
   does not own the positions games draw" and says "The four demos have not yet" adopted it
   (`docs/decisions/0023-frame-pacing-and-measurement.md:29-34, 52-53`).
2. That statement is stale: MeadowRun has `Motion.Between`, `StepBlend`, `StageMotion`
   (`games/MeadowRun/Scenes/Motion.cs:14-70`), NeonRift has the same idea as `DrawBlend`
   (`games/NeonRift/Scenes/DrawBlend.cs`) plus `Actor.DrawPosition` (`games/NeonRift/Gameplay/Actor.cs:16-31`),
   CourtyardKey has `DrawPositionAt(blend)` (`games/CourtyardKey/Entities/Actor.cs:38-54`). All three had to solve the
   same engine defect: a scene frozen under an overlay or in a transition still receives a `FrameBlend` < 1, so the
   game must remember whether *this scene* stepped this update (SC-2). Each also invented "snap" for teleports.
3. Root cause: the engine owns no transforms (WO-3), so it cannot interpolate.
4. Godot 4.3+ added built-in physics interpolation after years of every game doing it; Unity has
   `Rigidbody.interpolation`; Gaffer on Games' "Fix your timestep" is the reference pattern.
6. With a transform (WO-3): the engine keeps previous/current transform per entity, interpolates for presentation only,
   exposes `Teleport()` to snap, and a per-scene "stepped this update" flag so frozen scenes draw blend = 1.
7. Removes the duplicated helpers (`Motion.cs` alone is 113 lines, `DrawBlend.cs` 23, plus the actor fields) across
   three demos; ADR 0023 needs amending.

### WO-5 — `EntityList` edge cases (CP, Low)

Experiment E4: an entity calling `list.Clear()` in its `Update` throws
`InvalidOperationException: Collection was modified` (`World/Entities.cs:65, 106`). `Flush` removes with `RemoveAt`
inside a loop (`:77`), O(n²) when many die at once (bullet hell, particle-like entities). Low today; relevant if
`EntityList` stays the default container.

### SC-1 — Scene stack, not a world (DL, Medium)

1. `SceneManager` is a flat stack with deferred `Push/Pop/Replace/Reset`, overlays, and a transition that swaps at 0.5
   (`Scenes/Scene.cs:89-201`). Scenes reach every service through `Game` (`:32-40`).
2. Limits: no scene result/return value (TrailDuel passes callbacks and `done` flags:
   `games/TrailDuel/Scenes/OverworldScene.Scripts.cs:258, 306, 339, 365`); no additive/streamed scenes (a level plus a
   persistent HUD world, or 3D world + 2D UI); a scene is not a container of entities — each demo keeps its own `Stage`
   (MeadowRun, NeonRift) or `PlayScene` partials (CourtyardKey) as the real world; transitions are drawn into the
   virtual canvas only; `Game` acts as a service locator with a fixed set of services.
3. The stack is a good default for menus/pause/dialogue and should stay (LS). What is missing is a *world* concept
   below it (WO-3) and typed results (`Push<TResult>` returning a task/awaitable).
6. Keep the stack as "screen flow"; add a `World` (entities, transforms, physics world, audio listener, camera(s)) that
   scenes may own or share; add `SceneManager.Push<T>(Scene<T>) -> SceneResult<T>`.

### SC-3 — Time is the fixed step only (DL, Medium)

Coroutines (`Scenes/Coroutines.cs`), tweens and `Scene.Age` advance with the step `dt`; there is no time scale,
unscaled time or per-world clock. Demos build hitstop themselves (`games/MeadowRun/Gameplay/Stage.cs:285`
`Freeze(seconds)`). Coroutines are used by only two demos (CourtyardKey, TrailDuel). Propose clocks (`GameClock`,
`WorldClock` with scale/pause) that coroutines, tweens, audio and physics read.

### UI-1 — Action-driven widgets only (MC, Medium)

`SelectionList.Update(InputMap, up, down, confirm)` and `DialogueBox.Update(dt, InputMap, confirm)` read actions
(`UI/Widgets.cs:144-149, 239-248`); `AchievementsScene` too (`UI/AchievementViews.cs:107-115`). No hit testing,
pointer hover/click, touch tap, focus navigation graph, layout (anchors, stacks), scrolling, text field, or
localisation/bidi/text shaping. On a phone, menus are operated through the virtual d-pad. Fine as "retro widgets";
insufficient as the engine's UI. Proposal: an input-routing-aware UI layer (IN-3/IN-4) with focus + pointer, layout
primitives, and the current widgets reimplemented on it; heavy UI (retained, styled) as an optional module or an
integration (e.g. a Myra-like toolkit) behind the same input routing.

### UI-2 — Pixel-art sizing unit (LS/DL, Low)

`DialogueBox.UiUnit(font)` derives sizes from the bitmap font (`UI/Widgets.cs:171, 262`). Coherent for pixel art; for
HD/3D UI the unit should come from a UI scale (density, canvas) setting, documented and replaceable.

### DM-1 — The demos already built the architecture the engine lacks (High)

See §5. The recurring pattern — rules in a headless `Stage`, presentation fed through sinks, a `PlayerInput` struct,
previous/current positions, sleeping actors — is a simulation/presentation split that the engine should own.

---

## 4. Implicit assumptions

1. **One player.** One `InputMap`, one pad, merged devices (`Core/Game.cs:40`, `Sdl/SdlInput.cs:8`,
   `Raylib/RaylibPlatform.cs:484`, `Core/PlatformProfiles.cs:232`).
2. **One touch purpose.** All touches belong to on-screen controls (`Input/VirtualControls.cs:482`,
   `Sdl/SdlPlatform.cs:90-91`).
3. **QWERTY labels.** `ActionPrompts.KeyName` (`Input/ActionPrompts.cs:309`).
4. **Engine owns F11/Alt+Enter** (`Sdl/SdlPlatform.cs:364-368`, `Raylib/RaylibPlatform.cs:147`).
5. **Default action names** `left/right/up/down/confirm/cancel/action/pause` are an engine vocabulary shared by
   `InputMap.CreateDefault`, `TouchLayout.CreateDefault`, `SelectionList.Update` defaults and `AchievementsScene`
   (`Input/Input.cs:403-411`, `UI/Widgets.cs:239`). Renaming an action in a game silently breaks engine widgets.
6. **Voice limits are a backend accident**: 16 global (SDL) vs 4 per sound (raylib) vs unlimited (capture).
7. **Stereo pan is the only spatialisation**, linear balance law, 0..1.
8. **Output 48 kHz stereo float** (SDL, `Sdl/SdlAudioDevice.cs:16, 68`); synthesis at 44.1 kHz
   (`Audio/Audio.cs:7`); no surround, no device choice, no device-change handling.
9. **Volume ≤ 1.** Gain clamped to 0..1 everywhere (`Sdl/SdlAudioDevice.cs:153`), so no boosting quiet assets.
10. **One music at a time** (`Audio/Audio.cs:140`).
11. **Sounds live forever** (no unload in `IAudioDevice`).
12. **2D world.** `Entity.Position : Vector2`, `RectF` hitbox, float depth sort (`World/Entities.cs:14-20`).
13. **16×16 default hitbox** — pixel-art tile size baked into `Entity` (`World/Entities.cs:16`).
14. **Collision = tiles + AABB**, X before Y, sub-step = half a tile, one-way = platformer
    (`World/TileMotion.cs:40-68, 106`).
15. **Out-of-bounds is solid by default** (`TileGrid.OutsideFlags = Solid`, `World/TileMap.cs:72`).
16. **Entities draw themselves** with `IRenderer` (`World/Entities.cs:31`).
17. **Scenes freeze what is below** by default (`Scenes/Scene.cs:51`) and input is reset on every push/pop
    (`Scenes/Scene.cs:142, 154`).
18. **Time = fixed step**, 60 Hz default, max five steps per frame; no time scale.
19. **UI unit = bitmap font pixel** (`UI/Widgets.cs:171`).
20. **Game is the only composition root** with a fixed set of services (`Core/Game.cs:134-151`).

---

## 5. What the demos reimplement (missing engine capability)

| Capability | MeadowRun | NeonRift | CourtyardKey | TrailDuel | Engine today |
| --- | --- | --- | --- | --- | --- |
| Previous/current position + draw interpolation + snap | `Scenes/Motion.cs` `Motion.Between`, `StepBlend`, `StageMotion` | `Scenes/DrawBlend.cs`, `Gameplay/Actor.cs:16-31` | `Entities/Actor.cs:38-54` | — | Global `FrameBlend` only |
| "Did this scene step this update" | `StepBlend` | `DrawBlend` | — | — | none (SC-2) |
| Actor base with `Velocity` | `Gameplay/Actors/Actor.cs` | `Gameplay/Actor.cs:13` | `Entities/Actor.cs` | own `World/Actor.cs` | none |
| Gravity + tile body move | `Actor.MoveBody` | `Actor` at `:137-141` | yes | own grid stepping | `TileMotion` (no body) |
| Pairwise overlap/hit tests | `Stage.Rules.cs:183-223` | `Stage.cs:214` | combat partial | grid occupancy | linear `Overlapping`, unused |
| Positional pan from camera | `Stage.cs:288-292` | `Stage.cs:339` | — | — | none |
| Sim → presentation sinks (sound/music/events) | `Stage.SoundSink/MusicSink/StompSink` (`PlayScene.cs:31-36`) | `Stage.SoundSink` (`Stage.cs:341`) | via `PlayScene` | battle event `Present(...)` | none |
| Player command struct decoupled from `InputMap` | `PlayerInput` | `PlayerInput` (+ `Autopilot` bot) | — | — | none |
| Sleep/activate by camera proximity | `Actor.Awake`, `Stage.IsNearView` | — | room-based | map-based | none |
| Hitstop / freeze frames | `Stage.Freeze` (`:285`) | — | — | — | none |
| Scene result passing | — | — | — | callbacks + `done` flags | none |
| Own collision grid | `TileGrid` | `TileGrid` | `RoomGrid : ICollisionGrid` | `MapRuntime` | `ICollisionGrid` (good seam) |
| Sound/music banks (content) | `SoundBank`, `MusicBank` | `Sounds` | `SoundBank`, `MusicBank` | `SoundBank`, `MusicBank` | name registry (fine: content is game-owned, LS) |

Leaks of demo behaviour into the core (candidates): one-way tiles and the X-then-Y platformer resolution in the generic
`World` namespace (WO-2); the default action vocabulary used by engine UI (assumption 5); the 16×16 hitbox; the
retro `Synth`/`Song` in the core `Audio` namespace (legitimate as a module, not as core — it is the only music
authoring path that renders on threads inside `AudioMixer`, `Audio/Audio.cs:265-276`, coupling the mixer to the
sequencer).

Good seams worth keeping: `IActionSource` (network peers, bots, accessibility), `ICollisionGrid`, the deferred
scene-change queue, the engine-owned `TouchLayout`, `CaptureAudioDevice` as a deterministic audio oracle.

---

## 6. Experiments

`exp-sim\probe\Program.cs` against the copied DLL (`dotnet run`):

```
E1 thin solid: start y=90 delta=15.9 -> y=105,899994 hitFloor=False (solid spans y 100..104)
E2 platform overlap: box bottom 92 platform top 88 hitFloor=False onGround=False
E3 tile: y=78 hitFloor=True (tile spans 80..96)
E4 Clear during Update: InvalidOperationException: Collection was modified; enumeration operation may not execute.
E5 left=True right=True Axis=0 LastDevice=Gamepad
```

Open experiments (NE): managed mixer vs miniaudio `ma_engine` latency/CPU on Android and WebAssembly (AU-1/AU-6);
Box2D v3 determinism across x64/ARM64 for scripted playthroughs (WO-1); ECS vs attachment-entity prototype at 10k
entities with transforms and interpolation (WO-3).

---

## 7. Proposed target for this domain

Principles: contracts in small assemblies with no backend references; the core supplies the default implementation;
2D is a configuration of a 3D-capable transform, not a separate type system; presentation reads simulation, never the
other way.

### 7.1 Input

```csharp
public readonly record struct DeviceId(int Value);
public enum DeviceKind { Keyboard, Mouse, Gamepad, Touchscreen, Pen, Virtual, Other }
public sealed record InputDeviceInfo(DeviceId Id, DeviceKind Kind, string Name, GamepadStyle Style, int? PlayerIndexHint);

public abstract record InputEvent(DeviceId Device, double Time);
public sealed record KeyEvent(DeviceId Device, double Time, Scancode Code, Keycode Label, bool Down, bool Repeat) : InputEvent(Device, Time);
public sealed record TextEvent(DeviceId Device, double Time, string Text, bool Composing) : InputEvent(Device, Time);
public sealed record PointerEvent(DeviceId Device, double Time, long PointerId, PointerPhase Phase, Vector2 Position, PointerButtons Buttons, Vector2 Wheel) : InputEvent(Device, Time);
public sealed record GamepadEvent(DeviceId Device, double Time, GamepadControl Control, float Value) : InputEvent(Device, Time);
public sealed record DeviceEvent(DeviceId Device, double Time, InputDeviceInfo Info, bool Connected) : InputEvent(Device, Time);

/// Backend contract: replaces IInputSource + ITouchScreen.ReadTouches.
public interface IInputBackend
{
    IReadOnlyList<InputDeviceInfo> Devices { get; }
    void Drain(List<InputEvent> into);              // ordered, timestamped
    void SetTextInput(bool enabled, RectF? caret);  // IME
    bool Rumble(DeviceId pad, float low, float high, int ms) => false;
    string LabelOf(Scancode code) => code.ToString(); // layout-aware prompts
}

/// Core: one per player. Default: one user owning every device.
public sealed class InputUser
{
    public int Index { get; }
    public IReadOnlySet<DeviceId> Devices { get; }
    public ActionState Actions { get; }             // today's InputMap API: Down/Pressed/Value/Vector
}
public interface IDeviceAssignment { void OnDevice(DeviceEvent e, IList<InputUser> users); }

/// Routing: UI → on-screen controls → gameplay; each layer may consume.
public interface IInputLayer { int Priority { get; } bool Handle(InputEvent e, InputUser? user); }
```

### 7.2 Audio

```csharp
/// Backend contract: a PCM sink that pulls on its own thread.
public interface IAudioOutput : IDisposable
{
    AudioFormat Format { get; }                     // rate, channels (float32)
    void Start(IAudioRender render);                // render.Render(Span<float> interleaved)
    event Action<AudioDeviceChange>? DeviceChanged;
}

/// Game-facing contract; default: the engine's managed mixer. FMOD/miniaudio replace this whole interface.
public interface IAudioEngine
{
    AudioBus Master { get; }
    AudioBus Bus(string name);                      // "music", "sfx", "ui", "voice" by default
    SoundInstance Play(AudioAsset sound, in PlayParams p);  // returns a handle
    AudioListener Listener { get; }                 // position/orientation; 2D uses z = 0
    void Update(in AudioClock clock);
}
public readonly struct SoundInstance { /* generation-checked */ public bool IsPlaying { get; } public void Stop(float fade = 0); public float Volume { set; } public float Pitch { set; } public Vector3 Position { set; } }
public sealed record PlayParams(float Volume = 1, float Pitch = 1, bool Loop = false, AudioBus? Bus = null, Vector3? Position = null, int Priority = 0);
public interface IAudioEffect { void Process(Span<float> frames, int channels, int rate); }
```

`AudioAsset` comes from the asset pipeline (decoded or streamed), so audio unload follows asset scopes. `Synth` and
`Song` move to an optional `MicroEngine.Audio.Retro` module that produces `AudioAsset`s.

### 7.3 World, transforms, physics

```csharp
public readonly record struct EntityId(int Index, int Generation);
public struct Transform { public Vector3 Position; public Quaternion Rotation; public Vector3 Scale; }
public interface IWorld
{
    EntityId Create(EntityId? parent = null);
    void Destroy(EntityId id);                      // deferred, safe during iteration
    ref Transform Local(EntityId id);
    Matrix4x4 WorldMatrix(EntityId id);
    T? Get<T>(EntityId id) where T : class;         // attachments (not mandatory ECS)
    void Attach<T>(EntityId id, T attachment) where T : class;
    void Teleport(EntityId id);                     // no interpolation this step
    Transform Interpolated(EntityId id, float blend); // presentation only
    WorldClock Clock { get; }                       // scale, pause, hitstop
}

public interface IPhysicsWorld2D
{
    BodyId CreateBody(EntityId owner, in BodyDef def);
    ShapeId AddShape(BodyId body, in ShapeDef shape);   // box, circle, capsule, polygon, chain; layer/mask; sensor
    bool RayCast(Vector2 from, Vector2 to, QueryFilter filter, out RayHit hit);
    int Overlap(in Shape2D shape, Vector2 at, QueryFilter filter, Span<ShapeId> results);
    void Step(float dt);                                // called in the fixed-step physics phase
    void ReadContacts(List<ContactEvent> began, List<ContactEvent> ended);
}
// IPhysicsWorld3D mirrors it with Vector3/Quaternion. Default 2D impl: Box2D v3; 3D later: Jolt.
// TileCharacterMover (today's TileMotion) lives in MicroEngine.Gameplay2D and reads ICollisionGrid.
```

### 7.4 Scenes and UI

- `SceneManager` stays as screen flow; scenes may own or share an `IWorld`; `Push<TResult>` returns an awaitable result.
- Per-scene `stepped` tracking in the engine, so frozen scenes present blend = 1.
- UI layer registers as the highest-priority `IInputLayer`; widgets get focus + pointer; sizes from a UI scale setting.

### 7.5 Now / prepare / later

| Now (Phase II foundation) | Prepare (contracts, default minimal impl) | Later |
| --- | --- | --- |
| Device identity + event stream backend contract; one default user owning all devices | Multiple `InputUser`s, device assignment policies, rebinding persistence | Gesture module, gyro/touchpad, accessibility remappers |
| Pointer events from touch; input routing layers with consumption | Text input / IME in SDL and browser | Full retained UI module |
| Engine-side mixer behind `IAudioOutput`; voice handles; buses; audio in asset scopes | Listener + 2D attenuation; effects interface | HRTF/3D spatialisation, FMOD/Wwise adapters, miniaudio alternative |
| `EntityId` + `Transform` + interpolation + teleport + world clock | Attachments; physics contract assemblies; Box2D v3 adapter | Jolt 3D, scene serialisation, ECS backend if the prototype wins |
| Fix/relocate `TileMotion` (docs, moving solids, thin solids) | `TileCharacterMover` in Gameplay2D | — |
| Amend ADR 0003, 0020, 0023 | | |

---

## 8. Cross-domain questions

1. **Core/loop:** who defines the fixed-step phase list (input → gameplay → physics → transform sync → audio) and the
   clocks? This domain needs named phases, not `Scene.Update`.
2. **Rendering:** if entities stop drawing themselves, what is the presentation contract (sprite/mesh attachments read
   by a renderer, render layers instead of float `Depth`)? `ITouchScreen` currently draws through `IOverlayCanvas`;
   should the touch overlay become an ordinary UI/render layer?
3. **Assets:** audio must enter `AssetCache` (decoded vs streamed policy, unload by scope); who owns decoders (NVorbis
   today in the SDL backend only, raylib decodes its own formats — another per-backend divergence)?
4. **Platforms/web:** the managed-mixer design needs an AudioWorklet bridge in the browser and an output thread on
   Android/iOS; is a pull callback viable on the raylib/emscripten host?
5. **SDK/composition:** with replaceable `IAudioEngine`, `IPhysicsWorld2D`, `IInputBackend`, `Game` can no longer be a
   fixed service locator — what is the composition root (builder, modules) and how do defaults stay plug-and-play?
6. **Testing:** scripted input should replay events (not forced actions) once IN-2 lands; the audio oracle should be the
   real mixer with a file sink; physics determinism becomes part of the validation contract.
