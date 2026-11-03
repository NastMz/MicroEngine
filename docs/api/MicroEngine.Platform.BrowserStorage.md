# Class BrowserStorage

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Platform](MicroEngine.Platform.md)  
Assembly: MicroEngine.Platform.dll  
Guide: [The platform host](../manual/backends.md#the-platform-host)  

Save slots in the page's localStorage under "microengine/&lt;game&gt;/&lt;slot&gt;": the default save medium in
browsers. The browser's file system is in memory and vanishes on reload; localStorage writes are synchronous and
survive it. The page registers the "microengine" JavaScript module (its <code>storage.get</code>, <code>storage.set</code> and
<code>storage.remove</code> functions) before the game starts; the browser host's page does.

```csharp
public sealed class BrowserStorage : IStorage
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BrowserStorage](MicroEngine.Platform.BrowserStorage.md)

#### Implements

[IStorage](MicroEngine.Platform.IStorage.md)

## Constructors

### <a id="MicroEngine_Platform_BrowserStorage__ctor_System_String_"></a> BrowserStorage\(string\)

The slots of the game <code class="paramref">gameId</code>.

```csharp
public BrowserStorage(string gameId)
```

#### Parameters

`gameId` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">gameId</code> is null, empty or white space.

## Properties

### <a id="MicroEngine_Platform_BrowserStorage_Location"></a> Location

"localStorage:" followed by the key prefix of this game's slots.

```csharp
public string Location { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Platform_BrowserStorage_Delete_System_String_"></a> Delete\(string\)

Removes the slot; does nothing when it does not exist.

```csharp
public void Delete(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Platform_BrowserStorage_Exists_System_String_"></a> Exists\(string\)

Whether the slot has data.

```csharp
public bool Exists(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Platform_BrowserStorage_Read_System_String_"></a> Read\(string\)

The slot's text, or null when it does not exist.

```csharp
public string? Read(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Platform_BrowserStorage_SetAside_System_String_"></a> SetAside\(string\)

Moves unreadable data out of the slot, keeping a copy; best effort, never throws.

```csharp
public void SetAside(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Platform_BrowserStorage_Write_System_String_System_String_"></a> Write\(string, string\)

Replaces the slot atomically.

```csharp
public void Write(string slot, string text)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>
