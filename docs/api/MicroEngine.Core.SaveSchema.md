# Class SaveSchema

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.Saves.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

What every [SaveSchema`1](MicroEngine.Core.md) shares.

```csharp
public static class SaveSchema
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SaveSchema](MicroEngine.Core.SaveSchema.md)

## Fields

### <a id="MicroEngine_Core_SaveSchema_FirstVersion"></a> FirstVersion

The version of a save that has no version property: the saves written before the type had a schema.

```csharp
public const int FirstVersion = 1
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_SaveSchema_VersionProperty"></a> VersionProperty

The name of the property that holds a save's version, the first one of the saved JSON object: <code>"$version"</code>.
The store writes and removes it; the saved type never sees it and must not have a property serialized under
this name.

```csharp
public const string VersionProperty = "$version"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>
