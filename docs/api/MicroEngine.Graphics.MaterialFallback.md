# Struct MaterialFallback

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

What a material's draws become on a graphics device that cannot run its shader (one without
[Shaders](MicroEngine.Graphics.Devices.GraphicsFeatures.md): SDL's default device and its software renderer, raylib, browser builds and
the headless platform). There, every draw made under the material is drawn the usual way, without the shader, with
this fallback applied; the game's code is the same on every device.

```csharp
public readonly struct MaterialFallback : IEquatable<MaterialFallback>
```

#### Implements

[IEquatable<MaterialFallback\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_MaterialFallback__ctor_System_Nullable_MicroEngine_Color__MicroEngine_Graphics_Devices_BlendState_System_Boolean_"></a> MaterialFallback\(Color?, BlendState?, bool\)

What a material's draws become on a graphics device that cannot run its shader (one without
[Shaders](MicroEngine.Graphics.Devices.GraphicsFeatures.md): SDL's default device and its software renderer, raylib, browser builds and
the headless platform). There, every draw made under the material is drawn the usual way, without the shader, with
this fallback applied; the game's code is the same on every device.

```csharp
public MaterialFallback(Color? Tint = null, BlendState? Blend = null, bool Hidden = false)
```

#### Parameters

`Tint` [Color](MicroEngine.Color.md)?

A colour every draw is multiplied by; null leaves the draws as they are.

`Blend` [BlendState](MicroEngine.Graphics.Devices.BlendState.md)?

A blend state the draws use instead of the state's; null keeps the state's.

`Hidden` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the draws are left out altogether: for an effect that means nothing without its shader (a glow drawn over the picture).

## Properties

### <a id="MicroEngine_Graphics_MaterialFallback_Blend"></a> Blend

A blend state the draws use instead of the state's; null keeps the state's.

```csharp
public BlendState? Blend { get; init; }
```

#### Property Value

 [BlendState](MicroEngine.Graphics.Devices.BlendState.md)?

### <a id="MicroEngine_Graphics_MaterialFallback_Hidden"></a> Hidden

Whether the draws are left out altogether: for an effect that means nothing without its shader (a glow drawn over the picture).

```csharp
public bool Hidden { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_MaterialFallback_Hide"></a> Hide

Nothing is drawn where the shader cannot run.

```csharp
public static MaterialFallback Hide { get; }
```

#### Property Value

 [MaterialFallback](MicroEngine.Graphics.MaterialFallback.md)

### <a id="MicroEngine_Graphics_MaterialFallback_Plain"></a> Plain

The draws as they are, without the shader. The default.

```csharp
public static MaterialFallback Plain { get; }
```

#### Property Value

 [MaterialFallback](MicroEngine.Graphics.MaterialFallback.md)

### <a id="MicroEngine_Graphics_MaterialFallback_Tint"></a> Tint

A colour every draw is multiplied by; null leaves the draws as they are.

```csharp
public Color? Tint { get; init; }
```

#### Property Value

 [Color](MicroEngine.Color.md)?

## Methods

### <a id="MicroEngine_Graphics_MaterialFallback_Deconstruct_System_Nullable_MicroEngine_Color___MicroEngine_Graphics_Devices_BlendState__System_Boolean__"></a> Deconstruct\(out Color?, out BlendState?, out bool\)

```csharp
public void Deconstruct(out Color? Tint, out BlendState? Blend, out bool Hidden)
```

#### Parameters

`Tint` [Color](MicroEngine.Color.md)?

`Blend` [BlendState](MicroEngine.Graphics.Devices.BlendState.md)?

`Hidden` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_MaterialFallback_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_MaterialFallback_Equals_MicroEngine_Graphics_MaterialFallback_"></a> Equals\(MaterialFallback\)

```csharp
public bool Equals(MaterialFallback other)
```

#### Parameters

`other` [MaterialFallback](MicroEngine.Graphics.MaterialFallback.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_MaterialFallback_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_MaterialFallback_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_MaterialFallback_op_Equality_MicroEngine_Graphics_MaterialFallback_MicroEngine_Graphics_MaterialFallback_"></a> operator ==\(MaterialFallback, MaterialFallback\)

```csharp
public static bool operator ==(MaterialFallback left, MaterialFallback right)
```

#### Parameters

`left` [MaterialFallback](MicroEngine.Graphics.MaterialFallback.md)

`right` [MaterialFallback](MicroEngine.Graphics.MaterialFallback.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_MaterialFallback_op_Inequality_MicroEngine_Graphics_MaterialFallback_MicroEngine_Graphics_MaterialFallback_"></a> operator \!=\(MaterialFallback, MaterialFallback\)

```csharp
public static bool operator !=(MaterialFallback left, MaterialFallback right)
```

#### Parameters

`left` [MaterialFallback](MicroEngine.Graphics.MaterialFallback.md)

`right` [MaterialFallback](MicroEngine.Graphics.MaterialFallback.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
