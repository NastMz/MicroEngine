# Class SaveStore

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Saving data](../manual/persistence.md)  

Small JSON persistence for settings and progress. Files live under the user's data folder
([DefaultDirectory](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_DefaultDirectory_System_String_)) unless a directory is supplied (tests use a temporary one) or a launcher
chose another medium through [DefaultStorage](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_DefaultStorage) (the browser keeps saves in localStorage).
Corrupt or missing data yields null instead of crashing the game; corrupt data is set aside
("&lt;slot&gt;.corrupt.json" for files) so the next save cannot destroy it. A slot that exists but could not be
read (locked file, blocked browser storage) is never written by this store, because the game is then
running with fresh progress that would replace the real one. Failed writes return false instead of
throwing, so a full disk or browser storage quota leaves the game running with its progress in memory.

```csharp
public sealed class SaveStore
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SaveStore](MicroEngine.Core.SaveStore.md)

## Examples

<pre><code class="lang-csharp">public sealed class Progress { public int Level { get; set; } = 1; }

var store = new SaveStore("com.example.mygame");
var progress = store.Load&lt;Progress&gt;("progress") ?? new Progress();
progress.Level++;
store.Save("progress", progress);</code></pre>

## Remarks

A slot is a name such as "settings" or "progress"; with file storage it becomes <code>&lt;slot&gt;.json</code> in
[Directory](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_Directory). Values are serialized with `JsonSerializer` (public properties).

## Constructors

### <a id="MicroEngine_Core_SaveStore__ctor_System_String_System_String_"></a> SaveStore\(string, string?\)

Creates a store for a game.

```csharp
public SaveStore(string gameId, string? directory = null)
```

#### Parameters

`gameId` [string](https://learn.microsoft.com/dotnet/api/system.string)

The game's id, which names its save folder; must not be empty.

`directory` [string](https://learn.microsoft.com/dotnet/api/system.string)?

The folder for the slot files; null uses [DefaultStorage](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_DefaultStorage) when a launcher set it, otherwise [DefaultDirectory](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_DefaultDirectory_System_String_).

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">gameId</code> is null, empty or white space.

### <a id="MicroEngine_Core_SaveStore__ctor_MicroEngine_Core_ISaveStorage_"></a> SaveStore\(ISaveStorage\)

Creates a store over a custom storage medium.

```csharp
public SaveStore(ISaveStorage storage)
```

#### Parameters

`storage` [ISaveStorage](MicroEngine.Core.ISaveStorage.md)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">storage</code> is null.

## Properties

### <a id="MicroEngine_Core_SaveStore_DefaultStorage"></a> DefaultStorage

Storage for stores created without a directory, chosen by the launcher before the game is built
(null: files under local application data). Called with the game id.

```csharp
public static Func<string, ISaveStorage>? DefaultStorage { get; set; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [ISaveStorage](MicroEngine.Core.ISaveStorage.md)\>?

### <a id="MicroEngine_Core_SaveStore_Directory"></a> Directory

The save folder for file storage; otherwise a description of where saves live.

```csharp
public string Directory { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Core_SaveStore_DefaultDirectory_System_String_"></a> DefaultDirectory\(string\)

The save folder used when neither a directory nor [DefaultStorage](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_DefaultStorage) is given:
<code>%LOCALAPPDATA%\MicroEngine\&lt;gameId&gt;</code> on Windows, <code>~/Library/Application Support/MicroEngine/&lt;gameId&gt;</code>
on macOS, and <code>$XDG_DATA_HOME/MicroEngine/&lt;gameId&gt;</code> (by default <code>~/.local/share</code>) on Linux and other
Unix systems. Always absolute, also when the folder does not exist yet: the first save creates it.

```csharp
public static string DefaultDirectory(string gameId)
```

#### Parameters

`gameId` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_SaveStore_Delete_System_String_"></a> Delete\(string\)

Removes the slot's data; does nothing when it does not exist.

```csharp
public void Delete(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_SaveStore_Exists_System_String_"></a> Exists\(string\)

Whether the slot has saved data.

```csharp
public bool Exists(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_SaveStore_Load__1_System_String_"></a> Load<T\>\(string\)

Reads and deserializes a slot.

```csharp
public T? Load<T>(string slot) where T : class
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

The slot name.

#### Returns

 T?

The saved value, or null when the slot does not exist, holds invalid JSON (the data is set aside) or could not be read
(the slot is then protected from [Save``1](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_Save__1_System_String___0_) for the life of this store).

#### Type Parameters

`T` 

The saved type.

### <a id="MicroEngine_Core_SaveStore_Save__1_System_String___0_"></a> Save<T\>\(string, T\)

Writes the whole slot at once, so a crash never leaves a half-written save. Returns false when the
medium refused the write, or when the slot could not be read earlier (see the class remarks).

```csharp
public bool Save<T>(string slot, T data)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

`data` T

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T`
