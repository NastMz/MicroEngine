# Class BrowserSaveStorage

Namespace: [MicroEngine.Raylib](MicroEngine.Raylib.md)  
Assembly: MicroEngine.Raylib.dll  
Guide: [Backends and platforms](../manual/backends.md)  

Save slots in the page's localStorage under "microengine/&lt;game&gt;/&lt;slot&gt;". The browser's file system
is in memory and vanishes on reload; localStorage writes are synchronous and survive it. The page registers
the "microengine" module (see web/README.md) before the game starts.

```csharp
public sealed class BrowserSaveStorage : ISaveStorage
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BrowserSaveStorage](MicroEngine.Raylib.BrowserSaveStorage.md)

#### Implements

[ISaveStorage](https://github.com/NastMz/MicroEngine/blob/4d9999002843ba740441ef7bb60461c13b2a816e/src/MicroEngine/Core/SaveStore.cs)

## Constructors

### <a id="MicroEngine_Raylib_BrowserSaveStorage__ctor_System_String_"></a> BrowserSaveStorage\(string\)

Save slots in the page's localStorage under "microengine/&lt;game&gt;/&lt;slot&gt;". The browser's file system
is in memory and vanishes on reload; localStorage writes are synchronous and survive it. The page registers
the "microengine" module (see web/README.md) before the game starts.

```csharp
public BrowserSaveStorage(string gameId)
```

#### Parameters

`gameId` [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Raylib_BrowserSaveStorage_Location"></a> Location

"localStorage:" followed by the key prefix of this game's slots.

```csharp
public string Location { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Raylib_BrowserSaveStorage_Delete_System_String_"></a> Delete\(string\)

Removes the slot; does nothing when it does not exist.

```csharp
public void Delete(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Raylib_BrowserSaveStorage_Exists_System_String_"></a> Exists\(string\)

Whether the slot has data.

```csharp
public bool Exists(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_BrowserSaveStorage_Read_System_String_"></a> Read\(string\)

The slot's text, or null when it does not exist.

```csharp
public string? Read(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Raylib_BrowserSaveStorage_SetAside_System_String_"></a> SetAside\(string\)

Moves unreadable data out of the slot, keeping a copy; best effort, never throws.

```csharp
public void SetAside(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Raylib_BrowserSaveStorage_Write_System_String_System_String_"></a> Write\(string, string\)

Replaces the slot atomically.

```csharp
public void Write(string slot, string text)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)
