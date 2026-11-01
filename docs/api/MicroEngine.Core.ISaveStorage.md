# Interface ISaveStorage

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Saving data](../manual/persistence.md)  

Where a [SaveStore](MicroEngine.Core.SaveStore.md) keeps its slots: named texts that are written whole. Failures surface
as `IOException` or `UnauthorizedAccessException`.

```csharp
public interface ISaveStorage
```

## Properties

### <a id="MicroEngine_Core_ISaveStorage_Location"></a> Location

The save folder, or a description of the medium for diagnostics.

```csharp
string Location { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Core_ISaveStorage_Delete_System_String_"></a> Delete\(string\)

Removes the slot; does nothing when it does not exist.

```csharp
void Delete(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_ISaveStorage_Exists_System_String_"></a> Exists\(string\)

Whether the slot has data.

```csharp
bool Exists(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_ISaveStorage_Read_System_String_"></a> Read\(string\)

The slot's text, or null when it does not exist.

```csharp
string? Read(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_ISaveStorage_SetAside_System_String_"></a> SetAside\(string\)

Moves unreadable data out of the slot, keeping a copy; best effort, never throws.

```csharp
void SetAside(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_ISaveStorage_Write_System_String_System_String_"></a> Write\(string, string\)

Replaces the slot atomically.

```csharp
void Write(string slot, string text)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)
