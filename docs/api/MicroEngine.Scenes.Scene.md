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

[AchievementsScene](MicroEngine.UI.AchievementsScene.md), 
[Scene<TResult\>](MicroEngine.Scenes.Scene\-1.md)

#### Implements

IStateHashed

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

 AudioMixer

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

 InputMap

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

 IWorld

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

Draws the scene once per frame into the virtual canvas. Scenes below an overlay are drawn first.

```csharp
public abstract void Draw(IRenderer renderer)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

The renderer of the current frame.

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

`hash` StateHasher

The hasher of the step; a section started here lasts until the engine starts the next one.

### <a id="MicroEngine_Scenes_Scene_Resume"></a> Resume\(\)

Called when a scene pushed above this one is popped.

```csharp
public virtual void Resume()
```

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
