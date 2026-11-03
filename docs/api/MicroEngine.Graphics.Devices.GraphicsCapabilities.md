# Class GraphicsCapabilities

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

What a device declares. Read after creation: the backend identity is what the device reports, not what was requested.

```csharp
public sealed record GraphicsCapabilities : IEquatable<GraphicsCapabilities>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GraphicsCapabilities](MicroEngine.Graphics.Devices.GraphicsCapabilities.md)

#### Implements

[IEquatable<GraphicsCapabilities\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities__ctor_System_String_MicroEngine_Graphics_Devices_GraphicsTier_MicroEngine_Graphics_Devices_GraphicsFeatures_MicroEngine_Graphics_Devices_ShaderFormat_System_Collections_Generic_IReadOnlyDictionary_MicroEngine_Graphics_Devices_TextureFormat_MicroEngine_Graphics_Devices_TextureUsage__System_Collections_Generic_IReadOnlySet_MicroEngine_Graphics_Devices_BlendState__System_Collections_Generic_IReadOnlySet_MicroEngine_Graphics_Devices_PresentMode__MicroEngine_Graphics_Devices_GraphicsLimits_System_String_System_String_"></a> GraphicsCapabilities\(string, GraphicsTier, GraphicsFeatures, ShaderFormat, IReadOnlyDictionary<TextureFormat, TextureUsage\>, IReadOnlySet<BlendState\>, IReadOnlySet<PresentMode\>, GraphicsLimits, string, string?\)

What a device declares. Read after creation: the backend identity is what the device reports, not what was requested.

```csharp
public GraphicsCapabilities(string Backend, GraphicsTier Tier, GraphicsFeatures Features, ShaderFormat ShaderFormats, IReadOnlyDictionary<TextureFormat, TextureUsage> Formats, IReadOnlySet<BlendState> BuiltInBlendStates, IReadOnlySet<PresentMode> PresentModes, GraphicsLimits Limits, string ApiName, string? Adapter)
```

#### Parameters

`Backend` [string](https://learn.microsoft.com/dotnet/api/system.string)

The factory name ("sdl-renderer", "sdl-gpu", ...); [Device](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md#MicroEngine_Graphics_Devices_GraphicsCapabilityException_Device).

`Tier` [GraphicsTier](MicroEngine.Graphics.Devices.GraphicsTier.md)

Tier.

`Features` [GraphicsFeatures](MicroEngine.Graphics.Devices.GraphicsFeatures.md)

Declared optional features.

`ShaderFormats` [ShaderFormat](MicroEngine.Graphics.Devices.ShaderFormat.md)

Accepted shader formats; [None](MicroEngine.Graphics.Devices.ShaderFormat.md) on tier 0.

`Formats` [IReadOnlyDictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlydictionary\-2)<[TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md), [TextureUsage](MicroEngine.Graphics.Devices.TextureUsage.md)\>

Usages each texture format supports; a format absent from the map supports nothing.

`BuiltInBlendStates` [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<[BlendState](MicroEngine.Graphics.Devices.BlendState.md)\>

Blend states the built-in pipelines support; always contains [Presets](MicroEngine.Graphics.Devices.BlendState.md#MicroEngine_Graphics_Devices_BlendState_Presets).

`PresentModes` [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<[PresentMode](MicroEngine.Graphics.Devices.PresentMode.md)\>

Present modes the surface supports (empty without a surface).

`Limits` [GraphicsLimits](MicroEngine.Graphics.Devices.GraphicsLimits.md)

Limits.

`ApiName` [string](https://learn.microsoft.com/dotnet/api/system.string)

Underlying API as reported after creation ("direct3d11", "direct3d12", "vulkan", ...).

`Adapter` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Adapter name when the API reports one.

## Properties

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_Adapter"></a> Adapter

Adapter name when the API reports one.

```csharp
public string? Adapter { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_ApiName"></a> ApiName

Underlying API as reported after creation ("direct3d11", "direct3d12", "vulkan", ...).

```csharp
public string ApiName { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_Backend"></a> Backend

The factory name ("sdl-renderer", "sdl-gpu", ...); [Device](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md#MicroEngine_Graphics_Devices_GraphicsCapabilityException_Device).

```csharp
public string Backend { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_BuiltInBlendStates"></a> BuiltInBlendStates

Blend states the built-in pipelines support; always contains [Presets](MicroEngine.Graphics.Devices.BlendState.md#MicroEngine_Graphics_Devices_BlendState_Presets).

```csharp
public IReadOnlySet<BlendState> BuiltInBlendStates { get; init; }
```

#### Property Value

 [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<[BlendState](MicroEngine.Graphics.Devices.BlendState.md)\>

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_Features"></a> Features

Declared optional features.

```csharp
public GraphicsFeatures Features { get; init; }
```

#### Property Value

 [GraphicsFeatures](MicroEngine.Graphics.Devices.GraphicsFeatures.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_Formats"></a> Formats

Usages each texture format supports; a format absent from the map supports nothing.

```csharp
public IReadOnlyDictionary<TextureFormat, TextureUsage> Formats { get; init; }
```

#### Property Value

 [IReadOnlyDictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlydictionary\-2)<[TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md), [TextureUsage](MicroEngine.Graphics.Devices.TextureUsage.md)\>

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_Limits"></a> Limits

Limits.

```csharp
public GraphicsLimits Limits { get; init; }
```

#### Property Value

 [GraphicsLimits](MicroEngine.Graphics.Devices.GraphicsLimits.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_PresentModes"></a> PresentModes

Present modes the surface supports (empty without a surface).

```csharp
public IReadOnlySet<PresentMode> PresentModes { get; init; }
```

#### Property Value

 [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<[PresentMode](MicroEngine.Graphics.Devices.PresentMode.md)\>

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_ShaderFormats"></a> ShaderFormats

Accepted shader formats; [None](MicroEngine.Graphics.Devices.ShaderFormat.md) on tier 0.

```csharp
public ShaderFormat ShaderFormats { get; init; }
```

#### Property Value

 [ShaderFormat](MicroEngine.Graphics.Devices.ShaderFormat.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_Tier"></a> Tier

Tier.

```csharp
public GraphicsTier Tier { get; init; }
```

#### Property Value

 [GraphicsTier](MicroEngine.Graphics.Devices.GraphicsTier.md)

## Methods

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities__Clone__"></a> <Clone\>$\(\)

```csharp
public GraphicsCapabilities <Clone>$()
```

#### Returns

 [GraphicsCapabilities](MicroEngine.Graphics.Devices.GraphicsCapabilities.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_Deconstruct_System_String__MicroEngine_Graphics_Devices_GraphicsTier__MicroEngine_Graphics_Devices_GraphicsFeatures__MicroEngine_Graphics_Devices_ShaderFormat__System_Collections_Generic_IReadOnlyDictionary_MicroEngine_Graphics_Devices_TextureFormat_MicroEngine_Graphics_Devices_TextureUsage___System_Collections_Generic_IReadOnlySet_MicroEngine_Graphics_Devices_BlendState___System_Collections_Generic_IReadOnlySet_MicroEngine_Graphics_Devices_PresentMode___MicroEngine_Graphics_Devices_GraphicsLimits__System_String__System_String__"></a> Deconstruct\(out string, out GraphicsTier, out GraphicsFeatures, out ShaderFormat, out IReadOnlyDictionary<TextureFormat, TextureUsage\>, out IReadOnlySet<BlendState\>, out IReadOnlySet<PresentMode\>, out GraphicsLimits, out string, out string?\)

```csharp
public void Deconstruct(out string Backend, out GraphicsTier Tier, out GraphicsFeatures Features, out ShaderFormat ShaderFormats, out IReadOnlyDictionary<TextureFormat, TextureUsage> Formats, out IReadOnlySet<BlendState> BuiltInBlendStates, out IReadOnlySet<PresentMode> PresentModes, out GraphicsLimits Limits, out string ApiName, out string? Adapter)
```

#### Parameters

`Backend` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Tier` [GraphicsTier](MicroEngine.Graphics.Devices.GraphicsTier.md)

`Features` [GraphicsFeatures](MicroEngine.Graphics.Devices.GraphicsFeatures.md)

`ShaderFormats` [ShaderFormat](MicroEngine.Graphics.Devices.ShaderFormat.md)

`Formats` [IReadOnlyDictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlydictionary\-2)<[TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md), [TextureUsage](MicroEngine.Graphics.Devices.TextureUsage.md)\>

`BuiltInBlendStates` [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<[BlendState](MicroEngine.Graphics.Devices.BlendState.md)\>

`PresentModes` [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<[PresentMode](MicroEngine.Graphics.Devices.PresentMode.md)\>

`Limits` [GraphicsLimits](MicroEngine.Graphics.Devices.GraphicsLimits.md)

`ApiName` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Adapter` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_Equals_MicroEngine_Graphics_Devices_GraphicsCapabilities_"></a> Equals\(GraphicsCapabilities?\)

```csharp
public bool Equals(GraphicsCapabilities? other)
```

#### Parameters

`other` [GraphicsCapabilities](MicroEngine.Graphics.Devices.GraphicsCapabilities.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_Has_MicroEngine_Graphics_Devices_GraphicsFeatures_"></a> Has\(GraphicsFeatures\)

True when every flag of <code class="paramref">features</code> is declared.

```csharp
public bool Has(GraphicsFeatures features)
```

#### Parameters

`features` [GraphicsFeatures](MicroEngine.Graphics.Devices.GraphicsFeatures.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_Supports_MicroEngine_Graphics_Devices_TextureFormat_MicroEngine_Graphics_Devices_TextureUsage_"></a> Supports\(TextureFormat, TextureUsage\)

True when <code class="paramref">format</code> supports every flag of <code class="paramref">usage</code>.

```csharp
public bool Supports(TextureFormat format, TextureUsage usage)
```

#### Parameters

`format` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

`usage` [TextureUsage](MicroEngine.Graphics.Devices.TextureUsage.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_op_Equality_MicroEngine_Graphics_Devices_GraphicsCapabilities_MicroEngine_Graphics_Devices_GraphicsCapabilities_"></a> operator ==\(GraphicsCapabilities?, GraphicsCapabilities?\)

```csharp
public static bool operator ==(GraphicsCapabilities? left, GraphicsCapabilities? right)
```

#### Parameters

`left` [GraphicsCapabilities](MicroEngine.Graphics.Devices.GraphicsCapabilities.md)?

`right` [GraphicsCapabilities](MicroEngine.Graphics.Devices.GraphicsCapabilities.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilities_op_Inequality_MicroEngine_Graphics_Devices_GraphicsCapabilities_MicroEngine_Graphics_Devices_GraphicsCapabilities_"></a> operator \!=\(GraphicsCapabilities?, GraphicsCapabilities?\)

```csharp
public static bool operator !=(GraphicsCapabilities? left, GraphicsCapabilities? right)
```

#### Parameters

`left` [GraphicsCapabilities](MicroEngine.Graphics.Devices.GraphicsCapabilities.md)?

`right` [GraphicsCapabilities](MicroEngine.Graphics.Devices.GraphicsCapabilities.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
