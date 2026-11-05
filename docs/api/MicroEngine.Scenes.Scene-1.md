# Class Scene<TResult\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

A scene that ends by returning a value to whoever pushed it (a dialogue's choice, a shop's purchase, a menu's
answer): push it with [Push``1](MicroEngine.Scenes.SceneManager.md#MicroEngine_Scenes_SceneManager_Push__1_MicroEngine_Scenes_Scene___0__System_Action___0__MicroEngine_Scenes_Transition_) and
end it with [Finish](MicroEngine.Scenes.md).

```csharp
public abstract class Scene<TResult> : Scene, IStateHashed
```

#### Type Parameters

`TResult` 

What the scene returns.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Scene](MicroEngine.Scenes.Scene.md) ← 
[Scene<TResult\>](MicroEngine.Scenes.Scene\-1.md)

#### Implements

[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

#### Inherited Members

[Scene.Enter\(\)](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Enter), 
[Scene.Exit\(\)](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Exit), 
[Scene.Resume\(\)](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Resume), 
[Scene.Describe\(\)](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Describe), 
[Scene.HashState\(StateHasher\)](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_HashState\_MicroEngine\_Kernel\_StateHasher\_), 
[Scene.Update\(float\)](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Update\_System\_Single\_), 
[Scene.Draw\(IRenderer\)](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Draw\_MicroEngine\_Graphics\_IRenderer\_), 
[Scene.DrawUi\(IRenderer\)](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_DrawUi\_MicroEngine\_Graphics\_IRenderer\_), 
[Scene.UiContains\(Vector2\)](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_UiContains\_System\_Numerics\_Vector2\_), 
[Scene.Game](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Game), 
[Scene.Input](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Input), 
[Scene.Audio](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Audio), 
[Scene.Font](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Font), 
[Scene.Assets](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Assets), 
[Scene.Coroutines](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Coroutines), 
[Scene.Tweens](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Tweens), 
[Scene.Age](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Age), 
[Scene.EngineStep](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_EngineStep), 
[Scene.IsOverlay](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_IsOverlay), 
[Scene.BlocksUpdateBelow](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_BlocksUpdateBelow), 
[Scene.Objects](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Objects), 
[Scene.TouchActions](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_TouchActions), 
[Scene.WorldCamera](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_WorldCamera), 
[Scene.Widgets](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Widgets), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Constructors

### <a id="MicroEngine_Scenes_Scene_1__ctor"></a> Scene\(\)

```csharp
protected Scene()
```

## Methods

### <a id="MicroEngine_Scenes_Scene_1_Finish__0_MicroEngine_Scenes_Transition_"></a> Finish\(TResult, Transition?\)

Pops this scene, deferred like [Pop](MicroEngine.Scenes.SceneManager.md#MicroEngine_Scenes_SceneManager_Pop_MicroEngine_Scenes_Transition_), and hands <code class="paramref">result</code> to the callback
it was pushed with, after its [Exit](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Exit) and before the scene below receives
[Resume](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Resume). Only the first call counts; nothing happens if the scene is no longer on top when the
change applies. A scene removed any other way (popped, replaced, cleared) returns nothing.

```csharp
protected void Finish(TResult result, Transition? transition = null)
```

#### Parameters

`result` TResult

The value handed to the callback.

`transition` [Transition](MicroEngine.Scenes.Transition.md)?

An optional transition for the pop.

</div>
