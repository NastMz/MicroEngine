# Class FileSaveStorage

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.Saves.dll  
Guide: [Saving data](../manual/persistence.md)  

One JSON file per slot in a directory (desktop and tests).

```csharp
public sealed class FileSaveStorage : ISaveStorage, IStorage
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FileSaveStorage](MicroEngine.Core.FileSaveStorage.md)

#### Implements

[ISaveStorage](MicroEngine.Core.ISaveStorage.md), 
[IStorage](MicroEngine.Platform.IStorage.md)

## Constructors

### <a id="MicroEngine_Core_FileSaveStorage__ctor_System_String_"></a> FileSaveStorage\(string\)

One JSON file per slot in a directory (desktop and tests).

```csharp
public FileSaveStorage(string directory)
```

#### Parameters

`directory` [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Core_FileSaveStorage_Location"></a> Location

The directory that holds the slot files.

```csharp
public string Location { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Core_FileSaveStorage_Delete_System_String_"></a> Delete\(string\)

Removes the slot; does nothing when it does not exist.

```csharp
public void Delete(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_FileSaveStorage_Exists_System_String_"></a> Exists\(string\)

Whether the slot has data.

```csharp
public bool Exists(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_FileSaveStorage_Read_System_String_"></a> Read\(string\)

The slot's text, or null when it does not exist.

```csharp
public string? Read(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_FileSaveStorage_SetAside_System_String_"></a> SetAside\(string\)

Renames <code>&lt;slot&gt;.json</code> to <code>&lt;slot&gt;.corrupt.json</code>, replacing an earlier copy; leaves the file in place if that fails.

```csharp
public void SetAside(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_FileSaveStorage_Write_System_String_System_String_"></a> Write\(string, string\)

Writes atomically (temp file + move).

```csharp
public void Write(string slot, string text)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>
