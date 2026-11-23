# Class MemorySaveStorage

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.Saves.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Slots kept in memory: empty when created, gone with the object. It is where a run nobody plays keeps its saves (a
scripted run, a run in deterministic time, a game stepped by a test), so such a run never reads or writes the
player's saves and two of them start from the same state. A test that plays a game twice over the same saves
hands both the same instance (<code>RunOptions.Saves</code>, <code>Game.InitializeForTest</code>). It takes the names the file
storage takes (a plain name, see [SaveStore](MicroEngine.Core.SaveStore.md)), so a slot that works here works on a disk.

```csharp
public sealed class MemorySaveStorage : ISaveStorage, IStorage
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MemorySaveStorage](MicroEngine.Core.MemorySaveStorage.md)

#### Implements

[ISaveStorage](MicroEngine.Core.ISaveStorage.md), 
[IStorage](MicroEngine.Platform.IStorage.md)

## Remarks

It keeps no time of writing ([LastWrite](MicroEngine.Platform.SlotInfo.md#MicroEngine_Platform_SlotInfo_LastWrite) is null): a run that must be the same every
time cannot show the clock's. Not safe for use from several threads at once.

## Constructors

### <a id="MicroEngine_Core_MemorySaveStorage__ctor"></a> MemorySaveStorage\(\)

```csharp
public MemorySaveStorage()
```

## Properties

### <a id="MicroEngine_Core_MemorySaveStorage_Location"></a> Location

"memory": there is no folder.

```csharp
public string Location { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Core_MemorySaveStorage_Delete_System_String_"></a> Delete\(string\)

Removes the slot; does nothing when it does not exist.

```csharp
public void Delete(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_MemorySaveStorage_Exists_System_String_"></a> Exists\(string\)

Whether the slot has data.

```csharp
public bool Exists(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_MemorySaveStorage_List"></a> List\(\)

The slots that have text, with its size in UTF-8 bytes (what a file would hold) and no time of writing.

```csharp
public IReadOnlyList<SlotInfo> List()
```

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[SlotInfo](MicroEngine.Platform.SlotInfo.md)\>

### <a id="MicroEngine_Core_MemorySaveStorage_Read_System_String_"></a> Read\(string\)

The slot's text, or null when it does not exist.

```csharp
public string? Read(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_MemorySaveStorage_ReadSetAside_System_String_"></a> ReadSetAside\(string\)

The text [SetAside](MicroEngine.Core.MemorySaveStorage.md#MicroEngine_Core_MemorySaveStorage_SetAside_System_String_) took out of <code class="paramref">slot</code>, or null when nothing was set aside.

```csharp
public string? ReadSetAside(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_MemorySaveStorage_SetAside_System_String_"></a> SetAside\(string\)

Takes the slot's text out of the slot and keeps it ([ReadSetAside](MicroEngine.Core.MemorySaveStorage.md#MicroEngine_Core_MemorySaveStorage_ReadSetAside_System_String_)), replacing an earlier copy.

```csharp
public void SetAside(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_MemorySaveStorage_Write_System_String_System_String_"></a> Write\(string, string\)

Replaces the slot atomically.

```csharp
public void Write(string slot, string text)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">text</code> is null.

</div>
