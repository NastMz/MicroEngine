# Class Vignette

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

Darkens the picture towards its corners (or multiplies it towards [Color](MicroEngine.Graphics.Vignette.md#MicroEngine_Graphics_Vignette_Color)). Made of a texture and the
multiply blend state, so it is [Full](MicroEngine.Graphics.PostEffectSupport.md) and the same on every device, and it keeps the
picture's transparency.

```csharp
public sealed class Vignette : PostEffect
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PostEffect](MicroEngine.Graphics.PostEffect.md) ← 
[Vignette](MicroEngine.Graphics.Vignette.md)

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

### <a id="MicroEngine_Graphics_Vignette__ctor"></a> Vignette\(\)

```csharp
public Vignette()
```

## Properties

### <a id="MicroEngine_Graphics_Vignette_Color"></a> Color

The colour the corners are multiplied by; black (the default) darkens, another colour tints. Its alpha is ignored.

```csharp
public Color Color { get; set; }
```

#### Property Value

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_Vignette_Radius"></a> Radius

Where the darkening starts, from the centre (0) to the corners (1).

```csharp
public float Radius { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_Vignette_Softness"></a> Softness

How gradually it darkens after [Radius](MicroEngine.Graphics.Vignette.md#MicroEngine_Graphics_Vignette_Radius), as a share of the way to the corners; 0 is a hard edge.

```csharp
public float Softness { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_Vignette_Strength"></a> Strength

How strong the darkening is at the corners: 0 nothing, 1 the corners are the picture times [Color](MicroEngine.Graphics.Vignette.md#MicroEngine_Graphics_Vignette_Color) (black, by default).

```csharp
public float Strength { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Graphics_Vignette_Apply_MicroEngine_Graphics_PostPass_"></a> Apply\(PostPass\)

Runs the effect: reads [Source](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Source) and leaves the new picture with [Copy](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Copy_MicroEngine_Graphics_Material2D_System_Nullable_MicroEngine_Color__),
[Replace](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Replace_MicroEngine_Graphics_Texture_MicroEngine_Graphics_TextureFilter_MicroEngine_Graphics_Material2D_System_Nullable_MicroEngine_Color__) or by drawing over it ([BeginOver](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_BeginOver)). An effect that does
nothing leaves the picture as it is. Never called for an effect whose support is [None](MicroEngine.Graphics.PostEffectSupport.md).

```csharp
protected override void Apply(PostPass pass)
```

#### Parameters

`pass` [PostPass](MicroEngine.Graphics.PostPass.md)

### <a id="MicroEngine_Graphics_Vignette_SupportOn_MicroEngine_Graphics_IGraphicsDevice_"></a> SupportOn\(IGraphicsDevice\)

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
