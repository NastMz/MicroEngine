# Class ShaderEffect

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

A full-screen pass through a material of the game's own (a fragment shader compiled by <code>microengine shaders</code>,
loaded with <code>Assets.Material</code>): the picture is the draw's texture, in slot 0, and the shader's output is the
new picture. [Full](MicroEngine.Graphics.PostEffectSupport.md) where shaders run; [None](MicroEngine.Graphics.PostEffectSupport.md) elsewhere,
where the picture passes through unchanged (the material's own fallback is not used). The game owns the material.

```csharp
public sealed class ShaderEffect : PostEffect
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PostEffect](MicroEngine.Graphics.PostEffect.md) ← 
[ShaderEffect](MicroEngine.Graphics.ShaderEffect.md)

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

### <a id="MicroEngine_Graphics_ShaderEffect__ctor_MicroEngine_Graphics_Material2D_"></a> ShaderEffect\(Material2D\)

Creates a pass through <code class="paramref">material</code>.

```csharp
public ShaderEffect(Material2D material)
```

#### Parameters

`material` [Material2D](MicroEngine.Graphics.Material2D.md)

## Properties

### <a id="MicroEngine_Graphics_ShaderEffect_Filter"></a> Filter

How the picture is sampled by the shader: nearest (the default) reads each pixel as it is.

```csharp
public TextureFilter Filter { get; set; }
```

#### Property Value

 [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

### <a id="MicroEngine_Graphics_ShaderEffect_Material"></a> Material

The material the picture is drawn through. Set its parameters at any time before the frame is drawn.

```csharp
public Material2D Material { get; }
```

#### Property Value

 [Material2D](MicroEngine.Graphics.Material2D.md)

### <a id="MicroEngine_Graphics_ShaderEffect_Prepare"></a> Prepare

Called just before the pass, to set parameters that depend on the picture (its size in
[Width](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Width) and [Height](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Height)); null when there are none.

```csharp
public Action<Material2D, PostPass>? Prepare { get; set; }
```

#### Property Value

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-2)<[Material2D](MicroEngine.Graphics.Material2D.md), [PostPass](MicroEngine.Graphics.PostPass.md)\>?

## Methods

### <a id="MicroEngine_Graphics_ShaderEffect_Apply_MicroEngine_Graphics_PostPass_"></a> Apply\(PostPass\)

Runs the effect: reads [Source](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Source) and leaves the new picture with [Copy](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Copy_MicroEngine_Graphics_Material2D_System_Nullable_MicroEngine_Color__),
[Replace](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_Replace_MicroEngine_Graphics_Texture_MicroEngine_Graphics_TextureFilter_MicroEngine_Graphics_Material2D_System_Nullable_MicroEngine_Color__) or by drawing over it ([BeginOver](MicroEngine.Graphics.PostPass.md#MicroEngine_Graphics_PostPass_BeginOver)). An effect that does
nothing leaves the picture as it is. Never called for an effect whose support is [None](MicroEngine.Graphics.PostEffectSupport.md).

```csharp
protected override void Apply(PostPass pass)
```

#### Parameters

`pass` [PostPass](MicroEngine.Graphics.PostPass.md)

### <a id="MicroEngine_Graphics_ShaderEffect_SupportOn_MicroEngine_Graphics_IGraphicsDevice_"></a> SupportOn\(IGraphicsDevice\)

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
