# Class ColorGrade

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

Colour grading: brightness, contrast around mid grey, saturation and a tint, applied in that order.
[Full](MicroEngine.Graphics.PostEffectSupport.md) where shaders run. Elsewhere contrast and saturation are left out
([Reduced](MicroEngine.Graphics.PostEffectSupport.md)): the tint and the brightness still apply, made with blending.

```csharp
public sealed class ColorGrade : PostEffect
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PostEffect](MicroEngine.Graphics.PostEffect.md) ← 
[ColorGrade](MicroEngine.Graphics.ColorGrade.md)

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

### <a id="MicroEngine_Graphics_ColorGrade__ctor"></a> ColorGrade\(\)

```csharp
public ColorGrade()
```

## Properties

### <a id="MicroEngine_Graphics_ColorGrade_Brightness"></a> Brightness

Multiplies the colours; 1 changes nothing, 0 is black, 2 twice as bright (clamped at white).

```csharp
public float Brightness { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_ColorGrade_Contrast"></a> Contrast

Spreads the colours away from mid grey (above 1) or towards it (below 1); 1 changes nothing.

```csharp
public float Contrast { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_ColorGrade_Saturation"></a> Saturation

How much colour is kept: 1 changes nothing, 0 is black and white, above 1 is more vivid.

```csharp
public float Saturation { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_ColorGrade_Tint"></a> Tint

A colour every pixel is multiplied by; white changes nothing. Its alpha is ignored.

```csharp
public Color Tint { get; set; }
```

#### Property Value

 [Color](MicroEngine.Color.md)

## Methods

### <a id="MicroEngine_Graphics_ColorGrade_Apply_MicroEngine_Graphics_PostPass_"></a> Apply\(PostPass\)

Runs the effect: reads [Source](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Source) and leaves the new picture with [Copy](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Copy_MicroEngine_Graphics_Material2D_System_Nullable_MicroEngine_Color__),
[Replace](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Replace_MicroEngine_Graphics_Texture_MicroEngine_Graphics_TextureFilter_MicroEngine_Graphics_Material2D_System_Nullable_MicroEngine_Color__) or by drawing over it ([BeginOver](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_BeginOver)). An effect that does
nothing leaves the picture as it is. Never called for an effect whose support is [None](MicroEngine.Graphics.PostEffectSupport.md).

```csharp
protected override void Apply(PostPass pass)
```

#### Parameters

`pass` [PostPass](MicroEngine.Graphics.PostPass.md)

### <a id="MicroEngine_Graphics_ColorGrade_SupportOn_MicroEngine_Graphics_IGraphicsDevice_"></a> SupportOn\(IGraphicsDevice\)

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
