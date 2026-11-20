# Class PostProcess

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

A chain of full-screen effects over a picture: what was drawn between [Begin](MicroEngine.Graphics.PostProcess.md#MicroEngine_Graphics_PostProcess_Begin_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_IGraphicsDevice_System_Nullable_MicroEngine_Color__) and [End](MicroEngine.Graphics.PostProcess.md#MicroEngine_Graphics_PostProcess_End_MicroEngine_Graphics_IRenderer_System_Boolean_)
(or a picture given to [Apply](MicroEngine.Graphics.PostProcess.md#MicroEngine_Graphics_PostProcess_Apply_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_IGraphicsDevice_MicroEngine_Graphics_Texture_MicroEngine_RectF_System_Boolean_)) goes through every enabled effect in order, and the result is drawn
where the picture would have been. The chain owns the render targets, textures and built-in materials its effects
need, sized to the picture, and releases them with [Dispose](MicroEngine.Graphics.PostProcess.md#MicroEngine_Graphics_PostProcess_Dispose). A scene has one for its own picture
(<code>Scene.PostProcess</code>), and a game has one for the canvas and one for the window (<code>Game.PostProcess</code>,
<code>Game.ScreenPostProcess</code>); the engine begins, ends and releases those.

```csharp
public sealed class PostProcess : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PostProcess](MicroEngine.Graphics.PostProcess.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Examples

```csharp
// In a scene's Enter: the scene's picture glows and darkens towards the corners.
PostProcess.Add(new Bloom { Threshold = 0.6f, Strength = 1.2f });
PostProcess.Add(new Vignette { Strength = 0.6f });
```

## Remarks

Effects are made of the drawing API (render targets, states, materials), so a chain runs on every platform. Where
the graphics device runs no shaders (SDL's default device, raylib, browser builds, the headless platform), each
effect draws what its [SupportOn](MicroEngine.Graphics.PostEffect.md#MicroEngine_Graphics_PostEffect_SupportOn_MicroEngine_Graphics_IGraphicsDevice_) says: [Vignette](MicroEngine.Graphics.Vignette.md) and [Scanlines](MicroEngine.Graphics.Scanlines.md) are the
same everywhere, the others have a stated simpler form or are left out. A chain with no enabled effect does
nothing and costs nothing: the picture is drawn straight to where it goes. Post-processing only draws; it is not
part of a game's simulation state.

## Constructors

### <a id="MicroEngine_Graphics_PostProcess__ctor"></a> PostProcess\(\)

Creates an empty chain.

```csharp
public PostProcess()
```

## Properties

### <a id="MicroEngine_Graphics_PostProcess_Effects"></a> Effects

The effects, in the order they run.

```csharp
public IReadOnlyList<PostEffect> Effects { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[PostEffect](MicroEngine.Graphics.PostEffect.md)\>

### <a id="MicroEngine_Graphics_PostProcess_Enabled"></a> Enabled

Whether the chain runs at all; false draws the picture as it is, whatever effects the chain holds.

```csharp
public bool Enabled { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_PostProcess_IsActive"></a> IsActive

Whether the chain would change a picture now: it is enabled and holds at least one enabled effect.

```csharp
public bool IsActive { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_PostProcess_SizeInBytes"></a> SizeInBytes

What the chain holds for its effects now, in bytes of device memory (targets and textures).

```csharp
public long SizeInBytes { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

## Methods

### <a id="MicroEngine_Graphics_PostProcess_Add_MicroEngine_Graphics_PostEffect_"></a> Add\(PostEffect\)

Adds <code class="paramref">effect</code> at the end of the chain and returns the chain.

```csharp
public PostProcess Add(PostEffect effect)
```

#### Parameters

`effect` [PostEffect](MicroEngine.Graphics.PostEffect.md)

#### Returns

 [PostProcess](MicroEngine.Graphics.PostProcess.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The chain already holds this effect.

### <a id="MicroEngine_Graphics_PostProcess_Apply_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_IGraphicsDevice_MicroEngine_Graphics_Texture_MicroEngine_RectF_System_Boolean_"></a> Apply\(IRenderer, IGraphicsDevice, Texture, RectF, bool\)

Runs the effects on an existing <code class="paramref">picture</code> (a render target's texture, or any texture) and draws
the result into <code class="paramref">destination</code> of what the renderer is drawing into. An inactive chain draws
the picture itself there.

```csharp
public void Apply(IRenderer renderer, IGraphicsDevice graphics, Texture picture, RectF destination, bool opaque = false)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

The renderer to draw with.

`graphics` [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

The graphics device of the renderer.

`picture` [Texture](MicroEngine.Graphics.Texture.md)

The picture; it is not changed.

`destination` [RectF](MicroEngine.RectF.md)

Where the result lands, in the renderer's coordinates without a camera.

`opaque` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the result replaces what is there (true) or is drawn over it with its transparency.

### <a id="MicroEngine_Graphics_PostProcess_Begin_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_IGraphicsDevice_System_Nullable_MicroEngine_Color__"></a> Begin\(IRenderer, IGraphicsDevice, Color?\)

Starts the chain's picture: draws made with <code class="paramref">renderer</code> until [End](MicroEngine.Graphics.PostProcess.md#MicroEngine_Graphics_PostProcess_End_MicroEngine_Graphics_IRenderer_System_Boolean_) go into a target
of the renderer's size (its current target's), which starts as <code class="paramref">clear</code> (transparent when null).
Like any pushed target, the picture starts with no camera, no clip and no state. An inactive chain
([IsActive](MicroEngine.Graphics.PostProcess.md#MicroEngine_Graphics_PostProcess_IsActive) false) pushes nothing: the draws go where they would without it.

```csharp
public bool Begin(IRenderer renderer, IGraphicsDevice graphics, Color? clear = null)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

The renderer the picture is drawn with, and the result drawn into.

`graphics` [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

The graphics device of the renderer (<code>Game.Graphics</code>).

`clear` [Color](MicroEngine.Color.md)?

What the picture starts as; null is transparent, so the result is drawn over what is under it.

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether a picture was started.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The chain's picture is already being drawn.

### <a id="MicroEngine_Graphics_PostProcess_Clear"></a> Clear\(\)

Removes every effect and releases what the chain made for them.

```csharp
public void Clear()
```

### <a id="MicroEngine_Graphics_PostProcess_Dispose"></a> Dispose\(\)

Releases the render targets, textures and materials the chain made. The effects stay, and the chain makes what
it needs again the next time it runs, so a released chain can be used again. Safe to call more than once.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Graphics_PostProcess_End_MicroEngine_Graphics_IRenderer_System_Boolean_"></a> End\(IRenderer, bool\)

Ends the picture started by [Begin](MicroEngine.Graphics.PostProcess.md#MicroEngine_Graphics_PostProcess_Begin_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_IGraphicsDevice_System_Nullable_MicroEngine_Color__), runs the effects on it and draws the result over the whole of
what the renderer was drawing into, with normal transparency (or replacing what is there with
<code class="paramref">opaque</code>). After a [Begin](MicroEngine.Graphics.PostProcess.md#MicroEngine_Graphics_PostProcess_Begin_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_IGraphicsDevice_System_Nullable_MicroEngine_Color__) that started nothing it does nothing.

```csharp
public void End(IRenderer renderer, bool opaque = false)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`opaque` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

A camera or a state pushed in the picture was not popped.

### <a id="MicroEngine_Graphics_PostProcess_Get__1"></a> Get<T\>\(\)

The first effect of type <code class="typeparamref">T</code>, or null: to change its values while the game runs.

```csharp
public T? Get<T>() where T : PostEffect
```

#### Returns

 T?

#### Type Parameters

`T` 

### <a id="MicroEngine_Graphics_PostProcess_Insert_System_Int32_MicroEngine_Graphics_PostEffect_"></a> Insert\(int, PostEffect\)

Adds <code class="paramref">effect</code> at <code class="paramref">index</code> (0 runs first) and returns the chain.

```csharp
public PostProcess Insert(int index, PostEffect effect)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`effect` [PostEffect](MicroEngine.Graphics.PostEffect.md)

#### Returns

 [PostProcess](MicroEngine.Graphics.PostProcess.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The chain already holds this effect.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">index</code> is not a position of the chain.

### <a id="MicroEngine_Graphics_PostProcess_Remove_MicroEngine_Graphics_PostEffect_"></a> Remove\(PostEffect\)

Removes <code class="paramref">effect</code> and releases what the chain made for it. Returns whether the chain held it.

```csharp
public bool Remove(PostEffect effect)
```

#### Parameters

`effect` [PostEffect](MicroEngine.Graphics.PostEffect.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_PostProcess_SupportOn_MicroEngine_Graphics_IGraphicsDevice_"></a> SupportOn\(IGraphicsDevice\)

What each effect of the chain does with the textures of <code class="paramref">graphics</code>, in the chain's order
([SupportOn](MicroEngine.Graphics.PostEffect.md#MicroEngine_Graphics_PostEffect_SupportOn_MicroEngine_Graphics_IGraphicsDevice_)): for a settings screen, or to say what a device leaves out.

```csharp
public IReadOnlyList<(PostEffect Effect, PostEffectSupport Support)> SupportOn(IGraphicsDevice graphics)
```

#### Parameters

`graphics` [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<\([PostEffect](MicroEngine.Graphics.PostEffect.md) [Effect](https://learn.microsoft.com/dotnet/api/system.valuetuple\-microengine.graphics.posteffect,microengine.graphics.posteffectsupport\-.effect), [PostEffectSupport](MicroEngine.Graphics.PostEffectSupport.md) [Support](https://learn.microsoft.com/dotnet/api/system.valuetuple\-microengine.graphics.posteffect,microengine.graphics.posteffectsupport\-.support)\)\>

</div>
