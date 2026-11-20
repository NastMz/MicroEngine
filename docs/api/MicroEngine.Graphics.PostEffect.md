# Class PostEffect

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

One full-screen effect of a [PostProcess](MicroEngine.Graphics.PostProcess.md) chain: it receives the picture so far and leaves a new one.
The ready effects are [ColorGrade](MicroEngine.Graphics.ColorGrade.md), [Blur](MicroEngine.Graphics.Blur.md), [Bloom](MicroEngine.Graphics.Bloom.md), [Vignette](MicroEngine.Graphics.Vignette.md),
[Scanlines](MicroEngine.Graphics.Scanlines.md), [Crt](MicroEngine.Graphics.Crt.md) and [ShaderEffect](MicroEngine.Graphics.ShaderEffect.md) (a material of the game's own); a game
derives its own from this class. Pictures are premultiplied, like the canvas, and an effect keeps their alpha
unless changing it is its purpose.

```csharp
public abstract class PostEffect
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PostEffect](MicroEngine.Graphics.PostEffect.md)

#### Derived

[Bloom](MicroEngine.Graphics.Bloom.md), 
[Blur](MicroEngine.Graphics.Blur.md), 
[ColorGrade](MicroEngine.Graphics.ColorGrade.md), 
[Crt](MicroEngine.Graphics.Crt.md), 
[Scanlines](MicroEngine.Graphics.Scanlines.md), 
[ShaderEffect](MicroEngine.Graphics.ShaderEffect.md), 
[Vignette](MicroEngine.Graphics.Vignette.md)

## Constructors

### <a id="MicroEngine_Graphics_PostEffect__ctor"></a> PostEffect\(\)

```csharp
protected PostEffect()
```

## Properties

### <a id="MicroEngine_Graphics_PostEffect_Enabled"></a> Enabled

Whether the chain runs the effect. A disabled effect costs nothing.

```csharp
public bool Enabled { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Graphics_PostEffect_Apply_MicroEngine_Graphics_PostPass_"></a> Apply\(PostPass\)

Runs the effect: reads [Source](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Source) and leaves the new picture with [Copy](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Copy_MicroEngine_Graphics_Material2D_System_Nullable_MicroEngine_Color__),
[Replace](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Replace_MicroEngine_Graphics_Texture_MicroEngine_Graphics_TextureFilter_MicroEngine_Graphics_Material2D_System_Nullable_MicroEngine_Color__) or by drawing over it ([BeginOver](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_BeginOver)). An effect that does
nothing leaves the picture as it is. Never called for an effect whose support is [None](MicroEngine.Graphics.PostEffectSupport.md).

```csharp
protected abstract void Apply(PostPass pass)
```

#### Parameters

`pass` [PostPass](MicroEngine.Graphics.PostPass.md)

### <a id="MicroEngine_Graphics_PostEffect_SupportOn_MicroEngine_Graphics_IGraphicsDevice_"></a> SupportOn\(IGraphicsDevice\)

What the effect draws with the textures of <code class="paramref">graphics</code>, with its current values: an effect with
a shader is [Full](MicroEngine.Graphics.PostEffectSupport.md) where shaders run ([SupportsShaders](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_SupportsShaders))
and, elsewhere, [Reduced](MicroEngine.Graphics.PostEffectSupport.md) or [None](MicroEngine.Graphics.PostEffectSupport.md) as its type states.

```csharp
public abstract PostEffectSupport SupportOn(IGraphicsDevice graphics)
```

#### Parameters

`graphics` [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

#### Returns

 [PostEffectSupport](MicroEngine.Graphics.PostEffectSupport.md)

</div>
