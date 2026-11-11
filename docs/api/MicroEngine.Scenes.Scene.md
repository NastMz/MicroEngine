# Class Scene

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

A screen of the game (title, level, battle, pause menu...). Scenes live on a stack:
an overlay scene (dialogue, pause) draws on top of the scene below and can freeze it.
Each scene owns its coroutines and tweens, which stop when it leaves the stack.

```csharp
public abstract class Scene : IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Scene](MicroEngine.Scenes.Scene.md)

#### Derived

[Scene<TResult\>](MicroEngine.Scenes.Scene\-1.md)

#### Implements

[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

## Examples

```csharp
public sealed class TitleScene : Scene
{
    public override void Update(float dt)
    {
        if (Input.Pressed("confirm")) Game.Scenes.Replace(new LevelScene(), Transition.Fade());
    }

    public override void Draw(IRenderer renderer) =>
        Font.DrawCentered(renderer, "PRESS START", new Vector2(Game.Width / 2f, Game.Height / 2f), Color.White);
}
```

## Constructors

### <a id="MicroEngine_Scenes_Scene__ctor"></a> Scene\(\)

```csharp
protected Scene()
```

## Properties

### <a id="MicroEngine_Scenes_Scene_Age"></a> Age

Seconds this scene has been updated.

```csharp
public float Age { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_Scene_Assets"></a> Assets

The game's content files ([Assets](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Assets)).

```csharp
public AssetCache Assets { get; }
```

#### Property Value

 [AssetCache](MicroEngine.Core.AssetCache.md)

### <a id="MicroEngine_Scenes_Scene_Audio"></a> Audio

The game's audio mixer ([Audio](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Audio)).

```csharp
public AudioMixer Audio { get; }
```

#### Property Value

 [AudioMixer](MicroEngine.Audio.AudioMixer.md)

### <a id="MicroEngine_Scenes_Scene_BlocksUpdateBelow"></a> BlocksUpdateBelow

When true (default), scenes below an overlay are not updated.

```csharp
public virtual bool BlocksUpdateBelow { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_Scene_Coroutines"></a> Coroutines

Coroutines owned by this scene. They advance before [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) while the scene is updated and stop when it leaves the stack.

```csharp
public CoroutineRunner Coroutines { get; }
```

#### Property Value

 [CoroutineRunner](MicroEngine.Scenes.CoroutineRunner.md)

### <a id="MicroEngine_Scenes_Scene_EngineStep"></a> EngineStep

The engine's step in seconds (<code>1 / UpdateRate</code>): the step of the current update before the world's clock
scales or freezes it. [Age](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Age), [Coroutines](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Coroutines) and [Tweens](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Tweens) advance by it, and it
is what [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) receives when the scene has no [Objects](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Objects). Use it for what keeps moving
through a hitstop or slow motion (play time, camera shake, particles, a pause button) while the rules advance by
[Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_)'s step. Known from [Enter](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Enter) on.

```csharp
public float EngineStep { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Read before the scene is on a running game's stack and was updated.

### <a id="MicroEngine_Scenes_Scene_Font"></a> Font

The game's built-in font ([Font](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Font)).

```csharp
public BitmapFont Font { get; }
```

#### Property Value

 [BitmapFont](MicroEngine.Graphics.BitmapFont.md)

### <a id="MicroEngine_Scenes_Scene_Game"></a> Game

The game this scene belongs to. Set when the scene is pushed; throws `InvalidOperationException` before that (for example in the scene's constructor).

```csharp
public Game Game { get; }
```

#### Property Value

 [Game](MicroEngine.Core.Game.md)

### <a id="MicroEngine_Scenes_Scene_Input"></a> Input

The game's input actions ([Input](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Input)).

```csharp
public InputMap Input { get; }
```

#### Property Value

 [InputMap](MicroEngine.Input.InputMap.md)

### <a id="MicroEngine_Scenes_Scene_IsOverlay"></a> IsOverlay

When true, the scene below is drawn first.

```csharp
public virtual bool IsOverlay { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_Scene_Objects"></a> Objects

The scene's objects ([IWorld](MicroEngine.Scenes.IWorld.md)), created the first time it is read. While the scene is updated the
engine steps it with the scene: each step starts before the scene's coroutines, tweens and [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_)
(which then receives the step on the world's [Clock](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_Clock)) and ends after them, releasing what was
destroyed. Before [Draw](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Draw_MicroEngine_Graphics_IRenderer_) the engine sets its [Blend](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_Blend): the frame's blend when the
scene was updated in the latest step, 1 when it was not (frozen under an overlay, in a transition), so drawing
through [DrawPosition2D](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_DrawPosition2D_MicroEngine_Scenes_EntityId_) and [WorldMatrix](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_WorldMatrix_MicroEngine_Scenes_EntityId_) is smooth while it plays and
still while it is frozen. Its state is part of the scene's state hash.

```csharp
public IWorld Objects { get; }
```

#### Property Value

 [IWorld](MicroEngine.Scenes.IWorld.md)

#### Remarks

Create it outside drawing: read it first from [Enter](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Enter), [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) or the scene's constructor.
A world created by drawing would be stepped and hashed only in runs that drew a frame, so the state hash would
depend on the display; the first read from [Draw](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Draw_MicroEngine_Graphics_IRenderer_) or [DrawUi](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_DrawUi_MicroEngine_Graphics_IRenderer_) is refused instead.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Read for the first time while the game draws.

### <a id="MicroEngine_Scenes_Scene_TouchActions"></a> TouchActions

The actions this scene needs from the on-screen touch controls while it is on top, or null for all of them (the
default). A dialogue returns <code>["confirm", "cancel"]</code>: the movement control disappears from over its text and
the buttons that confirm and cancel stay. Read every frame, so a scene may change it with its own state.

```csharp
public virtual IReadOnlyCollection<string>? TouchActions { get; }
```

#### Property Value

 [IReadOnlyCollection](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlycollection\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

### <a id="MicroEngine_Scenes_Scene_Tweens"></a> Tweens

Tweens owned by this scene. They advance before [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) while the scene is updated and are cleared when it leaves the stack.

```csharp
public Tweener Tweens { get; }
```

#### Property Value

 [Tweener](MicroEngine.Scenes.Tweener.md)

### <a id="MicroEngine_Scenes_Scene_Widgets"></a> Widgets

The scene's widgets ([UiRoot](MicroEngine.UI.UiRoot.md)), created the first time it is read (from [Enter](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Enter) on; the
scene must be on the stack). Set its [Content](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Content) and the engine does the rest: it lays the
widgets out in UI units in the UI view and draws them after [DrawUi](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_DrawUi_MicroEngine_Graphics_IRenderer_), updates them before
[Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) (so the actions they act on are consumed before the scene reads them), and, while the scene is
on top, routes input to them as the highest input layer: a press on a widget, and the keyboard while a text field
has the focus, never reach gameplay, and the pointer over them is on the UI ([PointerOnUi](MicroEngine.Core.GameViews.md#MicroEngine_Core_GameViews_PointerOnUi)).
The widgets use the game's font and the map's action names until the game sets others. A scene that never reads
it has no UI and pays nothing.

```csharp
public UiRoot Widgets { get; }
```

#### Property Value

 [UiRoot](MicroEngine.UI.UiRoot.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The scene is not attached to a game yet (read from its constructor).

### <a id="MicroEngine_Scenes_Scene_WorldCamera"></a> WorldCamera

The camera of the scene's world view: when set, the engine pushes it before [Draw](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Draw_MicroEngine_Graphics_IRenderer_) and pops it after,
so [Draw](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Draw_MicroEngine_Graphics_IRenderer_) draws in world coordinates. Null (the default) draws in canvas pixels.

```csharp
public Camera2D? WorldCamera { get; set; }
```

#### Property Value

 [Camera2D](MicroEngine.Graphics.Camera2D.md)?

## Methods

### <a id="MicroEngine_Scenes_Scene_Describe"></a> Describe\(\)

A short description of the scene's state for development probes and tests (the web host's trace, for example
"x=120 y=88 grounded"); null by default. Never shown to players.

```csharp
public virtual string? Describe()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Scenes_Scene_Draw_MicroEngine_Graphics_IRenderer_"></a> Draw\(IRenderer\)

Draws the scene's world once per frame into the canvas (the world view). Scenes below an overlay are drawn first.
With a [WorldCamera](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_WorldCamera), the engine pushes it around this call.

```csharp
public abstract void Draw(IRenderer renderer)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

The renderer of the current frame.

### <a id="MicroEngine_Scenes_Scene_DrawUi_MicroEngine_Graphics_IRenderer_"></a> DrawUi\(IRenderer\)

Draws the scene's interface once per frame in the UI view, after every scene's world: in UI units at the window's
resolution ([UiSize](MicroEngine.Core.GameViews.md#MicroEngine_Core_GameViews_UiSize)), composited over the world, so text and shapes stay sharp
whatever the canvas's resolution. On a platform without a UI view it is drawn in the canvas instead. Does nothing by
default.

```csharp
public virtual void DrawUi(IRenderer ui)
```

#### Parameters

`ui` [IRenderer](MicroEngine.Graphics.IRenderer.md)

The UI view's renderer: <code>ui.Width</code> and <code>ui.Height</code> are its size in UI units.

### <a id="MicroEngine_Scenes_Scene_Enter"></a> Enter\(\)

Called when the scene is pushed onto the stack, after [Game](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Game) is set and input was reset.

```csharp
public virtual void Enter()
```

### <a id="MicroEngine_Scenes_Scene_Exit"></a> Exit\(\)

Called when the scene leaves the stack (popped, replaced or cleared), after its coroutines and tweens stopped.

```csharp
public virtual void Exit()
```

### <a id="MicroEngine_Scenes_Scene_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the scene's simulation state to the per-step state hash ([ComputeStateHash](MicroEngine.Core.Game.md#MicroEngine_Core_Game_ComputeStateHash_System_Boolean_)): the
fields that decide what happens next (positions, velocities, timers, health, the state of its own generators), in
a fixed order. Leave out what [Draw](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Draw_MicroEngine_Graphics_IRenderer_) writes (text and layout caches, positions held for blending,
particles that are only drawn): the hash must be the same whether or not a frame was drawn. The engine adds the
scene's type, [Age](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Age), coroutines and tweens itself, in the scene's section
(<code>scene[index]:Type</code>). The default adds nothing.

```csharp
public virtual void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

The hasher of the step; a section started here lasts until the engine starts the next one.

### <a id="MicroEngine_Scenes_Scene_Resume"></a> Resume\(\)

Called when a scene pushed above this one is popped.

```csharp
public virtual void Resume()
```

### <a id="MicroEngine_Scenes_Scene_UiContains_System_Numerics_Vector2_"></a> UiContains\(Vector2\)

Whether <code class="paramref">uiPoint</code> (UI units) lies on this scene's interface, so the pointer belongs to the UI view
there and not to the world ([PointerOnUi](MicroEngine.Core.GameViews.md#MicroEngine_Core_GameViews_PointerOnUi)). False by default.

```csharp
public virtual bool UiContains(Vector2 uiPoint)
```

#### Parameters

`uiPoint` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_Scene_Update_System_Single_"></a> Update\(float\)

Advances the scene by one fixed step. Not called while a blocking overlay is above, nor during the covering half of a transition.

```csharp
public abstract void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

Step length in seconds: <code>1 / UpdateRate</code>, or, once the scene has [Objects](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Objects), the step on the
world's clock (the same at scale 1; scaled by [Scale](MicroEngine.Scenes.WorldClock.md#MicroEngine_Scenes_WorldClock_Scale); 0 while it is frozen).

</div>
