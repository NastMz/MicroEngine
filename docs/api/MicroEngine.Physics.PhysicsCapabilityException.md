# Class PhysicsCapabilityException

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

A request needed a physics capability the backend does not declare ([Capabilities](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Capabilities)). It
is thrown before anything changes: the world is as it was.

```csharp
public sealed class PhysicsCapabilityException : NotSupportedException, ISerializable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Exception](https://learn.microsoft.com/dotnet/api/system.exception) ← 
[SystemException](https://learn.microsoft.com/dotnet/api/system.systemexception) ← 
[NotSupportedException](https://learn.microsoft.com/dotnet/api/system.notsupportedexception) ← 
[PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

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

### <a id="MicroEngine_Physics_PhysicsCapabilityException__ctor_System_String_System_String_MicroEngine_Physics_PhysicsCapabilities_"></a> PhysicsCapabilityException\(string, string, PhysicsCapabilities\)

Creates the error.

```csharp
public PhysicsCapabilityException(string operation, string backend, PhysicsCapabilities missing)
```

#### Parameters

`operation` [string](https://learn.microsoft.com/dotnet/api/system.string)

The refused operation, e.g. "CreateBody(Dynamic)".

`backend` [string](https://learn.microsoft.com/dotnet/api/system.string)

The backend name ([Backend](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Backend)).

`missing` [PhysicsCapabilities](MicroEngine.Physics.PhysicsCapabilities.md)

The undeclared capabilities the request needs.

## Properties

### <a id="MicroEngine_Physics_PhysicsCapabilityException_Backend"></a> Backend

The backend name.

```csharp
public string Backend { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Physics_PhysicsCapabilityException_Missing"></a> Missing

The undeclared capabilities the request needs.

```csharp
public PhysicsCapabilities Missing { get; }
```

#### Property Value

 [PhysicsCapabilities](MicroEngine.Physics.PhysicsCapabilities.md)

### <a id="MicroEngine_Physics_PhysicsCapabilityException_Operation"></a> Operation

The refused operation.

```csharp
public string Operation { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Physics_PhysicsCapabilityException_ThrowIfMissing_MicroEngine_Physics_PhysicsCapabilities_MicroEngine_Physics_PhysicsCapabilities_System_String_System_String_"></a> ThrowIfMissing\(PhysicsCapabilities, PhysicsCapabilities, string, string\)

Throws when <code class="paramref">required</code> holds a flag <code class="paramref">declared</code> lacks: the check every backend
runs before acting on a request.

```csharp
public static void ThrowIfMissing(PhysicsCapabilities declared, PhysicsCapabilities required, string operation, string backend)
```

#### Parameters

`declared` [PhysicsCapabilities](MicroEngine.Physics.PhysicsCapabilities.md)

`required` [PhysicsCapabilities](MicroEngine.Physics.PhysicsCapabilities.md)

`operation` [string](https://learn.microsoft.com/dotnet/api/system.string)

`backend` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

A required capability is not declared.

</div>
