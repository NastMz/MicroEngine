# Class Crt

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

The picture as a cathode-ray tube shows it: the glass bulges ([Curvature](MicroEngine.Graphics.Crt.md#MicroEngine_Graphics_Crt_Curvature)), the lines of the picture
show ([Scanlines](MicroEngine.Graphics.Crt.md#MicroEngine_Graphics_Crt_Scanlines)) and so do the phosphor's coloured stripes ([Mask](MicroEngine.Graphics.Crt.md#MicroEngine_Graphics_Crt_Mask)).
[Full](MicroEngine.Graphics.PostEffectSupport.md) where shaders run. Elsewhere only the lines are drawn
([Reduced](MicroEngine.Graphics.PostEffectSupport.md)): the picture stays flat and has no stripes.

```csharp
public sealed class Crt : PostEffect
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PostEffect](MicroEngine.Graphics.PostEffect.md) ← 
[Crt](MicroEngine.Graphics.Crt.md)

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

### <a id="MicroEngine_Graphics_Crt__ctor"></a> Crt\(\)

```csharp
public Crt()
```

## Properties

### <a id="MicroEngine_Graphics_Crt_Curvature"></a> Curvature

How much the glass bulges: 0 is flat, 0.1 a gentle curve. What falls outside the curved picture is black. Kept within 0 to 1; a value that is not a number is ignored.

```csharp
public float Curvature { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_Crt_Lines"></a> Lines

How many lines the picture has. 0 (the default) is half the picture's height. On a picture larger than the game's
canvas (the window), give the canvas's height, so each canvas row is one line.
Kept within 0 to 4096.

```csharp
public int Lines { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Crt_Mask"></a> Mask

How visible the red, green and blue stripes are: 0 nothing, 1 full. They are three pixels of the picture wide. Kept within 0 to 1; a value that is not a number is ignored.

```csharp
public float Mask { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_Crt_Scanlines"></a> Scanlines

How dark the picture is between two of its lines: 0 nothing, 1 black. Kept within 0 to 1; a value that is not a number is ignored.

```csharp
public float Scanlines { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Graphics_Crt_Apply_MicroEngine_Graphics_PostPass_"></a> Apply\(PostPass\)

Runs the effect: reads [Source](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Source) and leaves the new picture with [Copy](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Copy_MicroEngine_Graphics_Material2D_System_Nullable_MicroEngine_Color__),
[Replace](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Replace_MicroEngine_Graphics_Texture_MicroEngine_Graphics_TextureFilter_MicroEngine_Graphics_Material2D_System_Nullable_MicroEngine_Color__) or by drawing over it ([BeginOver](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_BeginOver)). An effect that does
nothing leaves the picture as it is. Never called for an effect whose support is [None](MicroEngine.Graphics.PostEffectSupport.md).

```csharp
protected override void Apply(PostPass pass)
```

#### Parameters

`pass` [PostPass](MicroEngine.Graphics.PostPass.md)

### <a id="MicroEngine_Graphics_Crt_SupportOn_MicroEngine_Graphics_IGraphicsDevice_"></a> SupportOn\(IGraphicsDevice\)

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
