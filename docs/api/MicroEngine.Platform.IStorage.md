# Interface IStorage

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Platform](MicroEngine.Platform.md)  
Assembly: MicroEngine.Platform.dll  
Guide: [The platform host](../manual/backends.md#the-platform-host)  

Where a game keeps what it saves: named slots of text that are written whole (files on desktops and phones, the
page's localStorage in browsers, a cloud service). Failures surface as `IOException` or
`UnauthorizedAccessException`.

```csharp
public interface IStorage
```

## Remarks

Which names a slot can have is the storage's to say, because only the storage knows what a name becomes: a file
storage takes plain file names and keeps every slot inside its folder, a key-value storage may take any text. A
storage refuses a name it cannot hold with `ArgumentException` from every member that takes a slot;
that is a mistake in the calling code, not a failure of the medium. A plain name (letters, digits, <code>-</code>,
<code>_</code>, a dot inside the name) is valid in every storage the engine provides.

## Properties

### <a id="MicroEngine_Platform_IStorage_Location"></a> Location

The save folder, or a description of the medium for diagnostics.

```csharp
string Location { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Platform_IStorage_Delete_System_String_"></a> Delete\(string\)

Removes the slot; does nothing when it does not exist.

```csharp
void Delete(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Platform_IStorage_Exists_System_String_"></a> Exists\(string\)

Whether the slot has data.

```csharp
bool Exists(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Platform_IStorage_Read_System_String_"></a> Read\(string\)

The slot's text, or null when it does not exist.

```csharp
string? Read(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Platform_IStorage_SetAside_System_String_"></a> SetAside\(string\)

Moves unreadable data out of the slot, keeping a copy; best effort, never throws for a failure of the medium.

```csharp
void SetAside(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Platform_IStorage_Write_System_String_System_String_"></a> Write\(string, string\)

Replaces the slot atomically.

```csharp
void Write(string slot, string text)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>
