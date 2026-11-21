# Class Scanlines

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

Darkens every other line of the picture, as a television's lines show. Made of a texture and the multiply blend
state, so it is [Full](MicroEngine.Graphics.PostEffectSupport.md) and the same on every device, and it keeps the picture's transparency.

```csharp
public sealed class Scanlines : PostEffect
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PostEffect](MicroEngine.Graphics.PostEffect.md) ← 
[Scanlines](MicroEngine.Graphics.Scanlines.md)

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

### <a id="MicroEngine_Graphics_Scanlines__ctor"></a> Scanlines\(\)

```csharp
public Scanlines()
```

## Fields

### <a id="MicroEngine_Graphics_Scanlines_MaxLines"></a> MaxLines

The largest number of lines of [Lines](MicroEngine.Graphics.Scanlines.md#MicroEngine_Graphics_Scanlines_Lines) and of [Lines](MicroEngine.Graphics.Crt.md#MicroEngine_Graphics_Crt_Lines).

```csharp
public const int MaxLines = 4096
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Properties

### <a id="MicroEngine_Graphics_Scanlines_Lines"></a> Lines

How many lines the picture has: each is a bright row and a dark row. 0 (the default) is half the picture's
height, one row of pixels each. On a picture larger than the game's canvas (the window), give the canvas's height.
Kept within 0 to 4096.

```csharp
public int Lines { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Scanlines_Strength"></a> Strength

How dark the dark lines are: 0 nothing, 1 black. Kept within 0 to 1; a value that is not a number is ignored.

```csharp
public float Strength { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Graphics_Scanlines_Apply_MicroEngine_Graphics_PostPass_"></a> Apply\(PostPass\)

Runs the effect: reads [Source](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Source) and leaves the new picture with [Copy](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Copy_MicroEngine_Graphics_Material2D_System_Nullable_MicroEngine_Color__),
[Replace](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Replace_MicroEngine_Graphics_Texture_MicroEngine_Graphics_TextureFilter_MicroEngine_Graphics_Material2D_System_Nullable_MicroEngine_Color__) or by drawing over it ([BeginOver](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_BeginOver)). An effect that does
nothing leaves the picture as it is. Never called for an effect whose support is [None](MicroEngine.Graphics.PostEffectSupport.md).

```csharp
protected override void Apply(PostPass pass)
```

#### Parameters

`pass` [PostPass](MicroEngine.Graphics.PostPass.md)

### <a id="MicroEngine_Graphics_Scanlines_SupportOn_MicroEngine_Graphics_IGraphicsDevice_"></a> SupportOn\(IGraphicsDevice\)

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
