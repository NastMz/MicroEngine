# Class GraphicsDeviceOptions

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Device creation options.

```csharp
public sealed record GraphicsDeviceOptions : IEquatable<GraphicsDeviceOptions>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GraphicsDeviceOptions](MicroEngine.Graphics.Devices.GraphicsDeviceOptions.md)

#### Implements

[IEquatable<GraphicsDeviceOptions\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceOptions__ctor_MicroEngine_Graphics_Devices_PresentMode_System_String_System_Boolean_System_Boolean_"></a> GraphicsDeviceOptions\(PresentMode, string?, bool, bool\)

Device creation options.

```csharp
public GraphicsDeviceOptions(PresentMode PresentMode = PresentMode.VSync, string? Api = null, bool Debug = false, bool PreferLowPower = false)
```

#### Parameters

`PresentMode` [PresentMode](MicroEngine.Graphics.Devices.PresentMode.md)

Present mode chosen explicitly (never left to the driver); must be in [PresentModes](MicroEngine.Graphics.Devices.GraphicsCapabilities.md#MicroEngine_Graphics_Devices_GraphicsCapabilities_PresentModes) when there is a surface.

`Api` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Preferred underlying API ("direct3d12", "vulkan", ...) or null for the backend default. Check [ApiName](MicroEngine.Graphics.Devices.GraphicsCapabilities.md#MicroEngine_Graphics_Devices_GraphicsCapabilities_ApiName) after creation.

`Debug` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Enable API validation where available.

`PreferLowPower` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Prefer an integrated adapter.

## Properties

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceOptions_Api"></a> Api

Preferred underlying API ("direct3d12", "vulkan", ...) or null for the backend default. Check [ApiName](MicroEngine.Graphics.Devices.GraphicsCapabilities.md#MicroEngine_Graphics_Devices_GraphicsCapabilities_ApiName) after creation.

```csharp
public string? Api { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceOptions_Debug"></a> Debug

Enable API validation where available.

```csharp
public bool Debug { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceOptions_PreferLowPower"></a> PreferLowPower

Prefer an integrated adapter.

```csharp
public bool PreferLowPower { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceOptions_PresentMode"></a> PresentMode

Present mode chosen explicitly (never left to the driver); must be in [PresentModes](MicroEngine.Graphics.Devices.GraphicsCapabilities.md#MicroEngine_Graphics_Devices_GraphicsCapabilities_PresentModes) when there is a surface.

```csharp
public PresentMode PresentMode { get; init; }
```

#### Property Value

 [PresentMode](MicroEngine.Graphics.Devices.PresentMode.md)

## Methods

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceOptions__Clone__"></a> <Clone\>$\(\)

```csharp
public GraphicsDeviceOptions <Clone>$()
```

#### Returns

 [GraphicsDeviceOptions](MicroEngine.Graphics.Devices.GraphicsDeviceOptions.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceOptions_Deconstruct_MicroEngine_Graphics_Devices_PresentMode__System_String__System_Boolean__System_Boolean__"></a> Deconstruct\(out PresentMode, out string?, out bool, out bool\)

```csharp
public void Deconstruct(out PresentMode PresentMode, out string? Api, out bool Debug, out bool PreferLowPower)
```

#### Parameters

`PresentMode` [PresentMode](MicroEngine.Graphics.Devices.PresentMode.md)

`Api` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`Debug` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`PreferLowPower` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceOptions_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceOptions_Equals_MicroEngine_Graphics_Devices_GraphicsDeviceOptions_"></a> Equals\(GraphicsDeviceOptions?\)

```csharp
public bool Equals(GraphicsDeviceOptions? other)
```

#### Parameters

`other` [GraphicsDeviceOptions](MicroEngine.Graphics.Devices.GraphicsDeviceOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceOptions_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceOptions_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceOptions_op_Equality_MicroEngine_Graphics_Devices_GraphicsDeviceOptions_MicroEngine_Graphics_Devices_GraphicsDeviceOptions_"></a> operator ==\(GraphicsDeviceOptions?, GraphicsDeviceOptions?\)

```csharp
public static bool operator ==(GraphicsDeviceOptions? left, GraphicsDeviceOptions? right)
```

#### Parameters

`left` [GraphicsDeviceOptions](MicroEngine.Graphics.Devices.GraphicsDeviceOptions.md)?

`right` [GraphicsDeviceOptions](MicroEngine.Graphics.Devices.GraphicsDeviceOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceOptions_op_Inequality_MicroEngine_Graphics_Devices_GraphicsDeviceOptions_MicroEngine_Graphics_Devices_GraphicsDeviceOptions_"></a> operator \!=\(GraphicsDeviceOptions?, GraphicsDeviceOptions?\)

```csharp
public static bool operator !=(GraphicsDeviceOptions? left, GraphicsDeviceOptions? right)
```

#### Parameters

`left` [GraphicsDeviceOptions](MicroEngine.Graphics.Devices.GraphicsDeviceOptions.md)?

`right` [GraphicsDeviceOptions](MicroEngine.Graphics.Devices.GraphicsDeviceOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
