# Class Game

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Base class for a game. Subclasses declare [Settings](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Settings), build content in
[Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load) and push the first scene. The runner owns the loop.

```csharp
public abstract class Game
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Game](MicroEngine.Core.Game.md)

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

## Constructors

### <a id="MicroEngine_Core_Game__ctor"></a> Game\(\)

```csharp
protected Game()
```

## Properties

### <a id="MicroEngine_Core_Game_AchievementToasts"></a> AchievementToasts

Unlock banners drawn above every scene (null without achievements).

```csharp
public AchievementToasts? AchievementToasts { get; }
```

#### Property Value

 [AchievementToasts](MicroEngine.UI.AchievementToasts.md)?

### <a id="MicroEngine_Core_Game_Achievements"></a> Achievements

The game's achievements; available after [UseAchievements](MicroEngine.Core.Game.md#MicroEngine_Core_Game_UseAchievements_System_Collections_Generic_IEnumerable_MicroEngine_Core_Achievement__MicroEngine_Core_SaveStore_MicroEngine_UI_UiTheme_).

```csharp
public AchievementTracker Achievements { get; }
```

#### Property Value

 [AchievementTracker](MicroEngine.Core.AchievementTracker.md)

### <a id="MicroEngine_Core_Game_Audio"></a> Audio

The mixer that loads and plays sound effects and music. Created before [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load).

```csharp
public AudioMixer Audio { get; }
```

#### Property Value

 [AudioMixer](MicroEngine.Audio.AudioMixer.md)

### <a id="MicroEngine_Core_Game_Font"></a> Font

The built-in pixel font, created before [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load) and disposed by the engine when the game shuts down.

```csharp
public BitmapFont Font { get; }
```

#### Property Value

 [BitmapFont](MicroEngine.Graphics.BitmapFont.md)

### <a id="MicroEngine_Core_Game_Graphics"></a> Graphics

Creates textures from images ([PixelImage](MicroEngine.Graphics.PixelImage.md), atlases, fonts). Available from [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load) on; throws `InvalidOperationException` before.

```csharp
public IGraphicsDevice Graphics { get; }
```

#### Property Value

 [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

### <a id="MicroEngine_Core_Game_HasAchievements"></a> HasAchievements

Whether [UseAchievements](MicroEngine.Core.Game.md#MicroEngine_Core_Game_UseAchievements_System_Collections_Generic_IEnumerable_MicroEngine_Core_Achievement__MicroEngine_Core_SaveStore_MicroEngine_UI_UiTheme_) was called, so [Achievements](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Achievements) can be read without throwing.

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

The input actions, built by [CreateInput](MicroEngine.Core.Game.md#MicroEngine_Core_Game_CreateInput) before [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load) and sampled once per update.

```csharp
public InputMap Input { get; }
```

#### Property Value

 [InputMap](MicroEngine.Input.InputMap.md)

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

Deterministic per-run randomness; seeded by the launcher.

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

### <a id="MicroEngine_Core_Game_Touch"></a> Touch

The on-screen touch controls ([CreateTouchLayout](MicroEngine.Core.Game.md#MicroEngine_Core_Game_CreateTouchLayout)), drawn and read wherever the platform has a touch screen.

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

### <a id="MicroEngine_Core_Game_Width"></a> Width

Width of the virtual canvas in pixels ([VirtualWidth](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_VirtualWidth)).

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Core_Game_CreateInput"></a> CreateInput\(\)

Override to change bindings; the default suits most games.

```csharp
protected virtual InputMap CreateInput()
```

#### Returns

 [InputMap](MicroEngine.Input.InputMap.md)

### <a id="MicroEngine_Core_Game_CreateTouchLayout"></a> CreateTouchLayout\(\)

Override to choose the on-screen controls for touch screens: which d-pad, joystick and buttons press which actions,
and where. The default ([CreateDefault](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_CreateDefault)) matches [CreateDefault](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_CreateDefault).

```csharp
protected virtual TouchLayout CreateTouchLayout()
```

#### Returns

 [TouchLayout](MicroEngine.Input.TouchLayout.md)

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

### <a id="MicroEngine_Core_Game_Unload"></a> Unload\(\)

Called once before the platform is disposed.

```csharp
protected virtual void Unload()
```

### <a id="MicroEngine_Core_Game_UseAchievements_System_Collections_Generic_IEnumerable_MicroEngine_Core_Achievement__MicroEngine_Core_SaveStore_MicroEngine_UI_UiTheme_"></a> UseAchievements\(IEnumerable<Achievement\>, SaveStore, UiTheme?\)

Turns on achievements, usually from [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load). Progress persists in the "achievements" slot
of <code class="paramref">store</code> (the game's own save folder) and every unlock shows a banner styled with
<code class="paramref">theme</code>. Report progress through [Achievements](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Achievements).

```csharp
protected AchievementTracker UseAchievements(IEnumerable<Achievement> catalog, SaveStore store, UiTheme? theme = null)
```

#### Parameters

`catalog` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[Achievement](MicroEngine.Core.Achievement.md)\>

`store` [SaveStore](MicroEngine.Core.SaveStore.md)

`theme` [UiTheme](MicroEngine.UI.UiTheme.md)?

#### Returns

 [AchievementTracker](MicroEngine.Core.AchievementTracker.md)

</div>
