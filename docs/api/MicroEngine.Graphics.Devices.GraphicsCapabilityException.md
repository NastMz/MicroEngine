# Class GraphicsCapabilityException

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

The one typed error for a missing capability: names the operation, the device and what it lacks. Thrown at
creation or recording time, never deferred to a draw.

```csharp
public sealed class GraphicsCapabilityException : NotSupportedException, ISerializable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Exception](https://learn.microsoft.com/dotnet/api/system.exception) ← 
[SystemException](https://learn.microsoft.com/dotnet/api/system.systemexception) ← 
[NotSupportedException](https://learn.microsoft.com/dotnet/api/system.notsupportedexception) ← 
[GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md)

#### Implements

[ISerializable](https://learn.microsoft.com/dotnet/api/system.runtime.serialization.iserializable)

#### Inherited Members

[Exception.GetBaseException\(\)](https://learn.microsoft.com/dotnet/api/system.exception.getbaseexception), 
[Exception.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.exception.tostring), 
[Exception.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.exception.gettype), 
[Exception.TargetSite](https://learn.microsoft.com/dotnet/api/system.exception.targetsite), 
[Exception.Message](https://learn.microsoft.com/dotnet/api/system.exception.message), 
[Exception.Data](https://learn.microsoft.com/dotnet/api/system.exception.data), 
[Exception.InnerException](https://learn.microsoft.com/dotnet/api/system.exception.innerexception), 
[Exception.HelpLink](https://learn.microsoft.com/dotnet/api/system.exception.helplink), 
[Exception.Source](https://learn.microsoft.com/dotnet/api/system.exception.source), 
[Exception.HResult](https://learn.microsoft.com/dotnet/api/system.exception.hresult), 
[Exception.StackTrace](https://learn.microsoft.com/dotnet/api/system.exception.stacktrace), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Constructors

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilityException__ctor_System_String_System_String_MicroEngine_Graphics_Devices_GraphicsFeatures_System_String_"></a> GraphicsCapabilityException\(string, string, GraphicsFeatures, string?\)

Creates the error.

```csharp
public GraphicsCapabilityException(string operation, string device, GraphicsFeatures missing, string? requirement = null)
```

#### Parameters

`operation` [string](https://learn.microsoft.com/dotnet/api/system.string)

The operation, e.g. "CreatePipeline(depth)".

`device` [string](https://learn.microsoft.com/dotnet/api/system.string)

The backend name ([Backend](MicroEngine.Graphics.Devices.GraphicsCapabilities.md#MicroEngine_Graphics_Devices_GraphicsCapabilities_Backend)).

`missing` [GraphicsFeatures](MicroEngine.Graphics.Devices.GraphicsFeatures.md)

The undeclared feature, or [None](MicroEngine.Graphics.Devices.GraphicsFeatures.md) when the requirement is not a feature (a format, shader format or present mode: see <code class="paramref">requirement</code>).

`requirement` [string](https://learn.microsoft.com/dotnet/api/system.string)?

What exactly is missing, e.g. "TextureFormat.D24UnormS8 as DepthStencil".

## Properties

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilityException_Device"></a> Device

The backend name.

```csharp
public string Device { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilityException_Missing"></a> Missing

The undeclared feature, or [None](MicroEngine.Graphics.Devices.GraphicsFeatures.md) for a non-feature requirement.

```csharp
public GraphicsFeatures Missing { get; }
```

#### Property Value

 [GraphicsFeatures](MicroEngine.Graphics.Devices.GraphicsFeatures.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilityException_Operation"></a> Operation

The refused operation.

```csharp
public string Operation { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_Devices_GraphicsCapabilityException_Requirement"></a> Requirement

The non-feature requirement (format and usage, shader format, present mode), when there is one.

```csharp
public string? Requirement { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

</div>
