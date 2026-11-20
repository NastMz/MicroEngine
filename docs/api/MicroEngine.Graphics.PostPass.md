# Class PostPass

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

What an effect works with while it runs ([Apply](MicroEngine.Graphics.PostEffect.md#MicroEngine_Graphics_PostEffect_Apply_MicroEngine_Graphics_PostPass_)): the picture so far, the renderer, and the
chain's own targets and textures, which the chain keeps between frames and releases itself.

```csharp
public sealed class PostPass
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PostPass](MicroEngine.Graphics.PostPass.md)

## Properties

### <a id="MicroEngine_Graphics_PostPass_Graphics"></a> Graphics

The graphics device of the renderer.

```csharp
public IGraphicsDevice Graphics { get; }
```

#### Property Value

 [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

### <a id="MicroEngine_Graphics_PostPass_Height"></a> Height

The picture's height in pixels.

```csharp
public int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_PostPass_Renderer"></a> Renderer

The renderer to draw with. It is drawing into the chain's destination: push a target before drawing.

```csharp
public IRenderer Renderer { get; }
```

#### Property Value

 [IRenderer](MicroEngine.Graphics.IRenderer.md)

### <a id="MicroEngine_Graphics_PostPass_ShadersRun"></a> ShadersRun

Whether materials run their shaders here ([SupportsShaders](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_SupportsShaders)).

```csharp
public bool ShadersRun { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_PostPass_Source"></a> Source

The picture so far: what the effect reads. Premultiplied.

```csharp
public Texture Source { get; }
```

#### Property Value

 [Texture](MicroEngine.Graphics.Texture.md)

### <a id="MicroEngine_Graphics_PostPass_Width"></a> Width

The picture's width in pixels.

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_PostPass_BeginOver"></a> BeginOver\(\)

Starts drawing on top of the current picture: draws made with the returned renderer until [EndOver](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_EndOver)
land on it, in its pixels. For effects that add to the picture or darken it (a glow, a vignette).

```csharp
public IRenderer BeginOver()
```

#### Returns

 [IRenderer](MicroEngine.Graphics.IRenderer.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Already drawing over the picture.

### <a id="MicroEngine_Graphics_PostPass_Copy_MicroEngine_Graphics_Material2D_System_Nullable_MicroEngine_Color__"></a> Copy\(Material2D?, Color?\)

Makes a new picture from the current one: the whole of [Source](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Source) copied, texel for texel, through
<code class="paramref">material</code> (as it is when null) and multiplied by <code class="paramref">tint</code>. The copy becomes
[Source](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Source).

```csharp
public void Copy(Material2D? material = null, Color? tint = null)
```

#### Parameters

`material` [Material2D](MicroEngine.Graphics.Material2D.md)?

`tint` [Color](MicroEngine.Color.md)?

### <a id="MicroEngine_Graphics_PostPass_Draw_MicroEngine_Graphics_Texture_MicroEngine_Graphics_RenderTarget2D_MicroEngine_Graphics_DrawState__System_Nullable_MicroEngine_Color__"></a> Draw\(Texture, RenderTarget2D, in DrawState, Color?\)

Draws the whole of <code class="paramref">source</code> over the whole of <code class="paramref">target</code> with
<code class="paramref">state</code> (its blend state, sampler and material), multiplied by <code class="paramref">tint</code>: one
pass of an effect made of several.

```csharp
public void Draw(Texture source, RenderTarget2D target, in DrawState state, Color? tint = null)
```

#### Parameters

`source` [Texture](MicroEngine.Graphics.Texture.md)

`target` [RenderTarget2D](MicroEngine.Graphics.RenderTarget2D.md)

`state` [DrawState](MicroEngine.Graphics.DrawState.md)

`tint` [Color](MicroEngine.Color.md)?

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

<code class="paramref">source</code> is <code class="paramref">target</code>'s own picture.

### <a id="MicroEngine_Graphics_PostPass_EndOver"></a> EndOver\(\)

Ends the drawing started by [BeginOver](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_BeginOver).

```csharp
public void EndOver()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

[BeginOver](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_BeginOver) was not called.

### <a id="MicroEngine_Graphics_PostPass_Image__1_System_String_System_Int64___0_System_Func___0_MicroEngine_Graphics_PixelImage__MicroEngine_Graphics_TextureFilter_"></a> Image<TState\>\(string, long, TState, Func<TState, PixelImage\>, TextureFilter\)

A texture of the chain's for this effect, by <code class="paramref">name</code>, made from the image
<code class="paramref">create</code> returns for <code class="paramref">state</code>: kept between frames, made again when
<code class="paramref">version</code> differs from the one it was made with (the values that shape the image), released
with the chain. Pass a static function and what it needs as <code class="paramref">state</code>, so nothing is allocated
on the frames that reuse the texture.

```csharp
public Texture Image<TState>(string name, long version, TState state, Func<TState, PixelImage> create, TextureFilter filter = TextureFilter.Linear)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`version` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`state` TState

`create` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TState, [PixelImage](MicroEngine.Graphics.PixelImage.md)\>

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

#### Returns

 [Texture](MicroEngine.Graphics.Texture.md)

#### Type Parameters

`TState` 

### <a id="MicroEngine_Graphics_PostPass_Replace_MicroEngine_Graphics_Texture_MicroEngine_Graphics_TextureFilter_MicroEngine_Graphics_Material2D_System_Nullable_MicroEngine_Color__"></a> Replace\(Texture, TextureFilter, Material2D?, Color?\)

Makes <code class="paramref">picture</code> the new picture: the whole of it stretched over a target of the picture's size,
sampled with <code class="paramref">filter</code>, through <code class="paramref">material</code> and multiplied by
<code class="paramref">tint</code>. An effect that built its result in a smaller target of its own ends with it.

```csharp
public void Replace(Texture picture, TextureFilter filter = TextureFilter.Linear, Material2D? material = null, Color? tint = null)
```

#### Parameters

`picture` [Texture](MicroEngine.Graphics.Texture.md)

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

`material` [Material2D](MicroEngine.Graphics.Material2D.md)?

`tint` [Color](MicroEngine.Color.md)?

### <a id="MicroEngine_Graphics_PostPass_Scratch_System_String_System_Int32_System_Int32_MicroEngine_Graphics_TextureFilter_"></a> Scratch\(string, int, int, TextureFilter\)

A render target of the chain's for this effect, by <code class="paramref">name</code>: kept between frames, made again
when the size asked for changes, released with the chain. It keeps its pixels until the effect draws into it.

```csharp
public RenderTarget2D Scratch(string name, int width, int height, TextureFilter filter = TextureFilter.Linear)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

#### Returns

 [RenderTarget2D](MicroEngine.Graphics.RenderTarget2D.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A size that is not positive.

</div>
