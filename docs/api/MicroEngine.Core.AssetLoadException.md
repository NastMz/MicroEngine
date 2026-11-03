# Class AssetLoadException

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

A content file that could not be loaded: missing, unreadable, in an unsupported format, or named with the wrong letter case.

```csharp
public sealed class AssetLoadException : Exception, ISerializable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Exception](https://learn.microsoft.com/dotnet/api/system.exception) ← 
[AssetLoadException](MicroEngine.Core.AssetLoadException.md)

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

### <a id="MicroEngine_Core_AssetLoadException__ctor_System_String_System_String_System_String_System_Exception_"></a> AssetLoadException\(string, string, string, Exception?\)

Records which asset failed and why.

```csharp
public AssetLoadException(string assetPath, string kind, string message, Exception? inner = null)
```

#### Parameters

`assetPath` [string](https://learn.microsoft.com/dotnet/api/system.string)

`kind` [string](https://learn.microsoft.com/dotnet/api/system.string)

`message` [string](https://learn.microsoft.com/dotnet/api/system.string)

`inner` [Exception](https://learn.microsoft.com/dotnet/api/system.exception)?

## Properties

### <a id="MicroEngine_Core_AssetLoadException_AssetPath"></a> AssetPath

The path the game asked for, relative to the content source.

```csharp
public string AssetPath { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_AssetLoadException_Kind"></a> Kind

What the game asked for it as ("texture", "atlas", "font", "sound"...): the loader's kind.

```csharp
public string Kind { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>
