# Class Bloom

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

Bloom: the bright parts of the picture, blurred, added back over it, so light sources glow.
[Full](MicroEngine.Graphics.PostEffectSupport.md) where shaders run (what is brighter than [Threshold](MicroEngine.Graphics.Bloom.md#MicroEngine_Graphics_Bloom_Threshold), with a
Gaussian blur). Elsewhere ([Reduced](MicroEngine.Graphics.PostEffectSupport.md)) the bright parts are picked by multiplying the
picture by itself, without an exact threshold, and blurred by halving: a softer, wider glow.

```csharp
public sealed class Bloom : PostEffect
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PostEffect](MicroEngine.Graphics.PostEffect.md) ← 
[Bloom](MicroEngine.Graphics.Bloom.md)

#### Inherited Members

[PostEffect.SupportOn\(IGraphicsDevice\)](MicroEngine.Graphics.PostEffect.md\#MicroEngine\_Graphics\_PostEffect\_SupportOn\_MicroEngine\_Graphics\_IGraphicsDevice\_), 
[PostEffect.Enabled](MicroEngine.Graphics.PostEffect.md\#MicroEngine\_Graphics\_PostEffect\_Enabled), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Constructors

### <a id="MicroEngine_Graphics_Bloom__ctor"></a> Bloom\(\)

```csharp
public Bloom()
```

## Properties

### <a id="MicroEngine_Graphics_Bloom_Radius"></a> Radius

How far the glow reaches, in pixels of the picture.

```csharp
public float Radius { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_Bloom_Strength"></a> Strength

How much of the glow is added: 1 adds it once, 0 nothing, 2 twice.

```csharp
public float Strength { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_Bloom_Threshold"></a> Threshold

How bright a pixel's brightest channel must be to glow, 0..1; 0 makes everything glow.

```csharp
public float Threshold { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Graphics_Bloom_Apply_MicroEngine_Graphics_PostPass_"></a> Apply\(PostPass\)

Runs the effect: reads [Source](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Source) and leaves the new picture with [Copy](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Copy_MicroEngine_Graphics_Material2D_System_Nullable_MicroEngine_Color__),
[Replace](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Replace_MicroEngine_Graphics_Texture_MicroEngine_Graphics_TextureFilter_MicroEngine_Graphics_Material2D_System_Nullable_MicroEngine_Color__) or by drawing over it ([BeginOver](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_BeginOver)). An effect that does
nothing leaves the picture as it is. Never called for an effect whose support is [None](MicroEngine.Graphics.PostEffectSupport.md).

```csharp
protected override void Apply(PostPass pass)
```

#### Parameters

`pass` [PostPass](MicroEngine.Graphics.PostPass.md)

### <a id="MicroEngine_Graphics_Bloom_SupportOn_MicroEngine_Graphics_IGraphicsDevice_"></a> SupportOn\(IGraphicsDevice\)

What the effect draws with the textures of <code class="paramref">graphics</code>, with its current values: an effect with
a shader is [Full](MicroEngine.Graphics.PostEffectSupport.md) where shaders run ([SupportsShaders](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_SupportsShaders))
and, elsewhere, [Reduced](MicroEngine.Graphics.PostEffectSupport.md) or [None](MicroEngine.Graphics.PostEffectSupport.md) as its type states.

```csharp
public override PostEffectSupport SupportOn(IGraphicsDevice graphics)
```

#### Parameters

`graphics` [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

#### Returns

 [PostEffectSupport](MicroEngine.Graphics.PostEffectSupport.md)

</div>
