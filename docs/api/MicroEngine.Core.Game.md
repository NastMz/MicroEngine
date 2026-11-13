# Class Game

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Base class for a game. Subclasses declare [Settings](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Settings), build content in
[Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load) and push the first scene. The runner owns the loop.

```csharp
public abstract class Game : IEngineModule, IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Game](MicroEngine.Core.Game.md)

#### Implements

[IEngineModule](MicroEngine.Kernel.IEngineModule.md), 
[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

## Examples

```csharp
public sealed class MyGame : Game
{
    public override GameSettings Settings { get; } = new("My Game", 320, 180);

    protected override void Load() => Scenes.Push(new TitleScene());
}
```

## Remarks

The engine services ([Graphics](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Graphics), [Audio](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Audio), [Input](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Input), [Scenes](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Scenes),
[Font](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Font)) exist from [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load) on; reading them in the constructor fails. The simulation advances in
fixed steps of <code>1 / UpdateRate</code> seconds; drawing happens once per frame.

<p>
A game is an engine module ([IEngineModule](MicroEngine.Kernel.IEngineModule.md)): its services are registered in the engine's
[Services](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Services) and its per-frame work runs as named systems of the frame [Schedule](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Schedule). Override
[Configure](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Configure_MicroEngine_Kernel_EngineBuilder_) to add systems, replace services or change the [TimeSettings](MicroEngine.Kernel.TimeSettings.md).
</p>

## Constructors

### <a id="MicroEngine_Core_Game__ctor"></a> Game\(\)

```csharp
protected Game()
```

## Properties

### <a id="MicroEngine_Core_Game_Achievements"></a> Achievements

The game's achievements; available after [TrackAchievements](MicroEngine.Core.Game.md#MicroEngine_Core_Game_TrackAchievements_System_Collections_Generic_IEnumerable_MicroEngine_Core_Achievement__MicroEngine_Core_SaveStore_).

```csharp
public AchievementTracker Achievements { get; }
```

#### Property Value

 [AchievementTracker](MicroEngine.Core.AchievementTracker.md)

### <a id="MicroEngine_Core_Game_Assets"></a> Assets

The game's content files: textures, atlases and fonts loaded from <code>Content/</code>, shared and released by the
engine ([AssetCache](MicroEngine.Core.AssetCache.md)). Created before [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load); everything it loaded is disposed after
[Unload](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Unload).

```csharp
public AssetCache Assets { get; }
```

#### Property Value

 [AssetCache](MicroEngine.Core.AssetCache.md)

### <a id="MicroEngine_Core_Game_Audio"></a> Audio

The mixer that loads and plays sound effects and music. Created before [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load).

```csharp
public AudioMixer Audio { get; }
```

#### Property Value

 [AudioMixer](MicroEngine.Audio.AudioMixer.md)

### <a id="MicroEngine_Core_Game_ContentRoot"></a> ContentRoot

A content folder of the game's own for [Assets](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Assets), which then wins over the composition's content. Null
(the default) reads the content the composition names ([Content](MicroEngine.Hosting.HostBuilder.md#MicroEngine_Hosting_HostBuilder_Content): a macOS bundle's
resources, a phone's unpacked files, a zip archive), or else the <code>Content</code> folder next to the executable
([DefaultRoot](MicroEngine.Core.ContentFolder.md#MicroEngine_Core_ContentFolder_DefaultRoot)). Tests override it to point at the game's sources.

```csharp
protected virtual string? ContentRoot { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_Game_DebugDraw"></a> DebugDraw

Lines, shapes, text and physics worlds any code can ask to see, drawn above the scenes at the end of the frame
([DebugDraw](MicroEngine.Core.DebugDraw.md)). Calls do nothing until [Enabled](MicroEngine.Core.DebugDraw.md#MicroEngine_Core_DebugDraw_Enabled) is set.

```csharp
public DebugDraw DebugDraw { get; }
```

#### Property Value

 [DebugDraw](MicroEngine.Core.DebugDraw.md)

### <a id="MicroEngine_Core_Game_Diagnostics"></a> Diagnostics

The live measurements of the run: the last frame's times, allocations, graphics work and audio voices, and the time
of each frame system. Off until [Enabled](MicroEngine.Core.GameDiagnostics.md#MicroEngine_Core_GameDiagnostics_Enabled) is set or a tool listens to the engine's meter.

```csharp
public GameDiagnostics Diagnostics { get; }
```

#### Property Value

 [GameDiagnostics](MicroEngine.Core.GameDiagnostics.md)

### <a id="MicroEngine_Core_Game_Font"></a> Font

The built-in pixel font, created before [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load) and disposed by the engine when the game shuts down.

```csharp
public BitmapFont Font { get; }
```

#### Property Value

 [BitmapFont](MicroEngine.Graphics.BitmapFont.md)

### <a id="MicroEngine_Core_Game_Frame"></a> Frame

The time of the frame being run ([FrameContext](MicroEngine.Kernel.FrameContext.md)): its delta, the fixed step, the blend and the frame number.

```csharp
public FrameContext Frame { get; }
```

#### Property Value

 [FrameContext](MicroEngine.Kernel.FrameContext.md)

### <a id="MicroEngine_Core_Game_FrameBlend"></a> FrameBlend

How far the frame being drawn is between the last update and the next one, from 0 to 1: the part of a step of
real time that has passed but not been simulated yet. Updates run at a fixed rate and frames at the display's, so
on a display whose rate is not a multiple of [UpdateRate](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_UpdateRate) (75, 100, 144 Hz...) some
frames show no new update; drawing moving things at
<code>Vector2.Lerp(previousPosition, position, FrameBlend)</code> makes their motion even (drawn up to one update behind,
like every interpolating loop). Always 1 when time advances one step per frame (headless, scripted runs), so
captures show exactly the last update.

```csharp
public float FrameBlend { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Core_Game_Graphics"></a> Graphics

Creates textures from images ([PixelImage](MicroEngine.Graphics.PixelImage.md), atlases, fonts). Available from [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load) on; throws `InvalidOperationException` before.

```csharp
public IGraphicsDevice Graphics { get; }
```

#### Property Value

 [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

### <a id="MicroEngine_Core_Game_HasAchievements"></a> HasAchievements

Whether [TrackAchievements](MicroEngine.Core.Game.md#MicroEngine_Core_Game_TrackAchievements_System_Collections_Generic_IEnumerable_MicroEngine_Core_Achievement__MicroEngine_Core_SaveStore_) was called, so [Achievements](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Achievements) can be read without throwing.

```csharp
public bool HasAchievements { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_Game_Height"></a> Height

Height of the virtual canvas in pixels ([VirtualHeight](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_VirtualHeight)).

```csharp
public int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_Game_Input"></a> Input

The input actions of player 1, built by [CreateInput](MicroEngine.Core.Game.md#MicroEngine_Core_Game_CreateInput) before [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load) and sampled once per
update. With no setup player 1 owns every device, so a single-player game reads only this.

```csharp
public InputMap Input { get; }
```

#### Property Value

 [InputMap](MicroEngine.Input.InputMap.md)

### <a id="MicroEngine_Core_Game_InputSystem"></a> InputSystem

The engine's input ([InputSystem](MicroEngine.Input.InputSystem.md)): the local players ([Players](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Players);
player 1's map is [Input](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Input)), the devices and their events, the input layers events pass through before
gameplay ([Layers](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Layers); the on-screen controls are one), this update's events no layer took
([Events](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Events)) and the text channel ([Text](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Text)). Created before
[Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load).

```csharp
public InputSystem InputSystem { get; }
```

#### Property Value

 [InputSystem](MicroEngine.Input.InputSystem.md)

### <a id="MicroEngine_Core_Game_Lifecycle"></a> Lifecycle

Engine events: suspension, resumption, quit requests (with veto) and, where the platform reports them, resizing and focus.

```csharp
public IEngineLifecycle Lifecycle { get; }
```

#### Property Value

 [IEngineLifecycle](MicroEngine.Kernel.IEngineLifecycle.md)

### <a id="MicroEngine_Core_Game_Log"></a> Log

Where the engine's diagnostics go (the [ILog](MicroEngine.Kernel.ILog.md) service; standard error by default).

```csharp
public ILog Log { get; }
```

#### Property Value

 [ILog](MicroEngine.Kernel.ILog.md)

### <a id="MicroEngine_Core_Game_Platform"></a> Platform

The platform running the game (window, audio, input). Throws `InvalidOperationException` before the game starts.

```csharp
protected IPlatform Platform { get; }
```

#### Property Value

 [IPlatform](MicroEngine.Core.IPlatform.md)

### <a id="MicroEngine_Core_Game_Prompts"></a> Prompts

How to name each action to the player on the device in use (<code>Prompts.Format("Press {confirm}")</code>), from the
bindings of [Input](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Input) and the controls of [Touch](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Touch). Created with them, before [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load).

```csharp
public ActionPrompts Prompts { get; }
```

#### Property Value

 [ActionPrompts](MicroEngine.Input.ActionPrompts.md)

### <a id="MicroEngine_Core_Game_QuitRequested"></a> QuitRequested

True after [Quit](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Quit); the loop stops at the end of the current frame.

```csharp
public bool QuitRequested { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_Game_Random"></a> Random

Deterministic per-run randomness, seeded by the launcher ([Seed](MicroEngine.Core.RunOptions.md#MicroEngine_Core_RunOptions_Seed)): a
[SeededRandom](MicroEngine.Kernel.SeededRandom.md), which draws exactly what <code>new Random(seed)</code> draws and whose state is part of the
per-step state hash ([ComputeStateHash](MicroEngine.Core.Game.md#MicroEngine_Core_Game_ComputeStateHash_System_Boolean_)).

```csharp
public Random Random { get; }
```

#### Property Value

 [Random](https://learn.microsoft.com/dotnet/api/system.random)

### <a id="MicroEngine_Core_Game_Scenes"></a> Scenes

The scene stack. Push the first scene from [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load).

```csharp
public SceneManager Scenes { get; }
```

#### Property Value

 [SceneManager](MicroEngine.Scenes.SceneManager.md)

### <a id="MicroEngine_Core_Game_Schedule"></a> Schedule

The frame's systems by phase, the built-in ones named <code>engine.*</code>; fixed once the game starts.

```csharp
public FrameSchedule Schedule { get; }
```

#### Property Value

 [FrameSchedule](MicroEngine.Kernel.FrameSchedule.md)

### <a id="MicroEngine_Core_Game_Screen"></a> Screen

The screen while the game runs: displays, VSync, frame limit, window mode, and the graphics API and GPU in use
([GraphicsSettings](MicroEngine.Core.GraphicsSettings.md) for the options it starts with). Available from <code>Load</code>.

```csharp
public GameScreen Screen { get; }
```

#### Property Value

 [GameScreen](MicroEngine.Core.GameScreen.md)

### <a id="MicroEngine_Core_Game_Services"></a> Services

The engine's services: every built-in service ([InputMap](MicroEngine.Input.InputMap.md), [AudioMixer](MicroEngine.Audio.AudioMixer.md),
[SceneManager](MicroEngine.Scenes.SceneManager.md), [AssetCache](MicroEngine.Core.AssetCache.md), [VirtualControls](MicroEngine.Input.VirtualControls.md), [ActionPrompts](MicroEngine.Input.ActionPrompts.md),
[BitmapFont](MicroEngine.Graphics.BitmapFont.md), [GameScreen](MicroEngine.Core.GameScreen.md), [IPlatform](MicroEngine.Core.IPlatform.md), `Random`, this
game) and those the game's [Configure](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Configure_MicroEngine_Kernel_EngineBuilder_) registered, found by the type they were registered as.
Available from [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load) on.

```csharp
public ServiceRegistry Services { get; }
```

#### Property Value

 [ServiceRegistry](MicroEngine.Kernel.ServiceRegistry.md)

### <a id="MicroEngine_Core_Game_Settings"></a> Settings

The title, virtual canvas size, window scale, update rate and background colour of the game. Read once when the game starts; keep it constant.

```csharp
public abstract GameSettings Settings { get; }
```

#### Property Value

 [GameSettings](MicroEngine.Core.GameSettings.md)

### <a id="MicroEngine_Core_Game_Time"></a> Time

Seconds of simulated time since start.

```csharp
public float Time { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Core_Game_TimeSettings"></a> TimeSettings

The time policy of the run: fixed rate, catch-up limits and whether time follows the wall clock or advances one step per frame.

```csharp
public TimeSettings TimeSettings { get; }
```

#### Property Value

 [TimeSettings](MicroEngine.Kernel.TimeSettings.md)

### <a id="MicroEngine_Core_Game_Touch"></a> Touch

The on-screen touch controls ([CreateTouchLayout](MicroEngine.Core.Game.md#MicroEngine_Core_Game_CreateTouchLayout)), available from [Configure](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Configure_MicroEngine_Kernel_EngineBuilder_) on. They are
an opt-in module: active, drawn and read on a touch-first screen ([TouchFirst](MicroEngine.Input.ITouchScreen.md#MicroEngine_Input_ITouchScreen_TouchFirst): phones,
tablets, touch-only browsers) or once the game sets their [Layout](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_Layout) (in
[Configure](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Configure_MicroEngine_Kernel_EngineBuilder_), [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load) or a scene), on any platform with a touch screen; otherwise inactive,
with no frame system, input layer, action source or overlay ([IsActive](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_IsActive)). Controls
activated after the engine was built join the game at the start of the next frame.

```csharp
public VirtualControls Touch { get; }
```

#### Property Value

 [VirtualControls](MicroEngine.Input.VirtualControls.md)

### <a id="MicroEngine_Core_Game_UpdateCount"></a> UpdateCount

Number of fixed updates simulated since start (0 during [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load)).

```csharp
public long UpdateCount { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Core_Game_Views"></a> Views

The world view and the UI view, and the mapping between window pixels, canvas pixels and UI units, with the
pointer in each ([GameViews](MicroEngine.Core.GameViews.md)).

```csharp
public GameViews Views { get; }
```

#### Property Value

 [GameViews](MicroEngine.Core.GameViews.md)

### <a id="MicroEngine_Core_Game_Width"></a> Width

Width of the virtual canvas in pixels ([VirtualWidth](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_VirtualWidth)).

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Core_Game_ComputeStateHash_System_Boolean_"></a> ComputeStateHash\(bool\)

Hashes the simulation state now ([StateHasher](MicroEngine.Kernel.StateHasher.md)), in sections: <code>engine.time</code> (update count and
time), <code>engine.random</code> ([Random](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Random)), <code>engine.input</code> ([Input](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Input) after sampling, then the other players and the pairings when there are any),
<code>engine.audio</code> (the music playing or paused and the volumes), <code>engine.assets</code> (how many assets are
loaded), <code>engine.achievements</code> (when on), <code>engine.scenes</code> (the stack and the transition), one
<code>scene[i]:Type</code> per scene from the bottom (its age, coroutines and tweens, then [HashState](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_HashState_MicroEngine_Kernel_StateHasher_))
and <code>game</code> ([HashState](MicroEngine.Core.Game.md#MicroEngine_Core_Game_HashState_MicroEngine_Kernel_StateHasher_)). The loop records it after every fixed step of a run given a
[StateTrace](MicroEngine.Core.RunOptions.md#MicroEngine_Core_RunOptions_StateTrace); drawing never changes it.

```csharp
public StepHash ComputeStateHash(bool sections = false)
```

#### Parameters

`sections` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the result carries each section's hash, to name the state that differs.

#### Returns

 [StepHash](MicroEngine.Kernel.StepHash.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The game has not started.

### <a id="MicroEngine_Core_Game_Configure_MicroEngine_Kernel_EngineBuilder_"></a> Configure\(EngineBuilder\)

Called once when the game starts, after the built-in services and systems were registered and before
[Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load): add frame systems (<code>builder.Schedule.Add</code>), register or replace services
(<code>builder.Services</code>), edit settings such as [TimeSettings](MicroEngine.Kernel.TimeSettings.md) (<code>builder.Configure</code>) and
subscribe to <code>builder.Lifecycle</code>. A run's own choices ([TimeMode](MicroEngine.Core.RunOptions.md#MicroEngine_Core_RunOptions_TimeMode)) are applied after
it. The default does nothing.

```csharp
public virtual void Configure(EngineBuilder builder)
```

#### Parameters

`builder` [EngineBuilder](MicroEngine.Kernel.EngineBuilder.md)

### <a id="MicroEngine_Core_Game_CreateCamera"></a> CreateCamera\(\)

A camera the size of the canvas whose defaults follow the game's presentation
([Presentation](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_Presentation)): snapped to whole pixels for pixel art, smooth for
high-resolution 2D.

```csharp
public Camera2D CreateCamera()
```

#### Returns

 [Camera2D](MicroEngine.Graphics.Camera2D.md)

### <a id="MicroEngine_Core_Game_CreateInput"></a> CreateInput\(\)

Override to change bindings; the default suits most games.

```csharp
protected virtual InputMap CreateInput()
```

#### Returns

 [InputMap](MicroEngine.Input.InputMap.md)

### <a id="MicroEngine_Core_Game_CreateTouchLayout"></a> CreateTouchLayout\(\)

Override to choose the on-screen controls for touch screens: which d-pad, joystick and buttons press which actions,
and where. The default ([CreateDefault](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_CreateDefault_MicroEngine_Input_ActionNames_)) matches [CreateDefault](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_CreateDefault_MicroEngine_Input_ActionNames_) and is
used on touch-first screens only. A layout of the game's own, returned without calling the base method, is the
game setting a layout: it activates the controls on any platform with a touch screen ([Touch](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Touch)).

```csharp
protected virtual TouchLayout CreateTouchLayout()
```

#### Returns

 [TouchLayout](MicroEngine.Input.TouchLayout.md)

### <a id="MicroEngine_Core_Game_DrawAboveScenes_MicroEngine_Graphics_IRenderer_"></a> DrawAboveScenes\(IRenderer\)

Draws above every scene, in the canvas, once the scenes and their transition are drawn and before the scenes'
interfaces: what belongs to the game rather than to one scene, such as an unlock banner or a notice that stays
while scenes change. Advance what it shows from a frame system (<code>builder.Schedule.Add</code> in
[Configure](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Configure_MicroEngine_Kernel_EngineBuilder_)). The default draws nothing.

```csharp
protected virtual void DrawAboveScenes(IRenderer canvas)
```

#### Parameters

`canvas` [IRenderer](MicroEngine.Graphics.IRenderer.md)

The canvas renderer, in the game's virtual pixels.

### <a id="MicroEngine_Core_Game_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the game's own simulation state to the per-step state hash ([ComputeStateHash](MicroEngine.Core.Game.md#MicroEngine_Core_Game_ComputeStateHash_System_Boolean_)), after the
engine's sections, in a section named <code>game</code>: the fields of this class that decide what happens next
(progress, a session's lives and score...). Leave out what only drawing or the platform uses (caches written while
drawing, textures, save locations). The default adds nothing; scenes add their own state
([HashState](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_HashState_MicroEngine_Kernel_StateHasher_)).

```csharp
public virtual void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Core_Game_InitializeForTest_MicroEngine_Core_IPlatform_System_Int32_"></a> InitializeForTest\(IPlatform, int\)

Test hook: initializes against a platform without running the loop.

```csharp
public void InitializeForTest(IPlatform platform, int seed = 1)
```

#### Parameters

`platform` [IPlatform](MicroEngine.Core.IPlatform.md)

`seed` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_Game_Load"></a> Load\(\)

Called once after the platform exists. Create atlases, sounds and push the first scene.

```csharp
protected abstract void Load()
```

### <a id="MicroEngine_Core_Game_OnResumed"></a> OnResumed\(\)

Called when a suspended game comes back, before its next update.

```csharp
protected virtual void OnResumed()
```

### <a id="MicroEngine_Core_Game_OnSuspended"></a> OnSuspended\(\)

Called when the platform suspends the game (sent to the background), after music paused and achievements were saved.

```csharp
protected virtual void OnSuspended()
```

### <a id="MicroEngine_Core_Game_Quit"></a> Quit\(\)

Asks the loop to stop. The current frame finishes, then every scene exits and [Unload](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Unload) runs.

```csharp
public void Quit()
```

### <a id="MicroEngine_Core_Game_StepForTest"></a> StepForTest\(\)

Test hook: advances one fixed step after sampling input.

```csharp
public void StepForTest()
```

#### Remarks

Runs the phases that come before drawing for one fixed step, with a frame that stands for exactly one step:
[BeginFrame](MicroEngine.Kernel.FramePhase.md), [Input](MicroEngine.Kernel.FramePhase.md), the four fixed phases,
[Update](MicroEngine.Kernel.FramePhase.md) and [LateUpdate](MicroEngine.Kernel.FramePhase.md). The loop's clock does not move.

### <a id="MicroEngine_Core_Game_TrackAchievements_System_Collections_Generic_IEnumerable_MicroEngine_Core_Achievement__MicroEngine_Core_SaveStore_"></a> TrackAchievements\(IEnumerable<Achievement\>, SaveStore\)

Turns achievements on: call it from the game's constructor or from [Configure](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Configure_MicroEngine_Kernel_EngineBuilder_), before the engine is
composed. It adds the achievements module (an [AchievementTracker](MicroEngine.Core.AchievementTracker.md) service over
<code class="paramref">catalog</code> and the <code>engine.achievements</code> system that writes progress) and the saves module
when the game has none. Progress persists in the "achievements" slot of <code class="paramref">store</code>. Report progress
through [Achievements](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Achievements). The game shows unlocks its own way: subscribe to
[Unlocked](MicroEngine.Core.AchievementTracker.md#MicroEngine_Core_AchievementTracker_Unlocked) on [Achievements](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Achievements), and draw a banner above every scene from
[DrawAboveScenes](MicroEngine.Core.Game.md#MicroEngine_Core_Game_DrawAboveScenes_MicroEngine_Graphics_IRenderer_). A game that never calls it has no achievement service, system or file.

```csharp
protected AchievementTracker TrackAchievements(IEnumerable<Achievement> catalog, SaveStore store)
```

#### Parameters

`catalog` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[Achievement](MicroEngine.Core.Achievement.md)\>

`store` [SaveStore](MicroEngine.Core.SaveStore.md)

#### Returns

 [AchievementTracker](MicroEngine.Core.AchievementTracker.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The engine was already composed (called from [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load) or later).

### <a id="MicroEngine_Core_Game_TryGetCapability__1___0__"></a> TryGetCapability<T\>\(out T?\)

An optional feature, if there is one: one the engine declared ([Capabilities](MicroEngine.Kernel.EngineBuilder.md#MicroEngine_Kernel_EngineBuilder_Capabilities)), else one
the platform offers ([TryGetCapability``1](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_TryGetCapability__1___0__): its [ITouchScreen](MicroEngine.Input.ITouchScreen.md), its
[IDisplayControl](MicroEngine.Core.IDisplayControl.md), its [IPlatformDiagnostics](MicroEngine.Core.IPlatformDiagnostics.md)...). False before the game starts.

```csharp
public bool TryGetCapability<T>(out T? capability) where T : class
```

#### Parameters

`capability` T?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T` 

### <a id="MicroEngine_Core_Game_Unload"></a> Unload\(\)

Called once before the platform is disposed.

```csharp
protected virtual void Unload()
```

</div>
