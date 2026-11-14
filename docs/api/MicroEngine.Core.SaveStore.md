# Class SaveStore

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.Saves.dll  
Guide: [Saving data](../manual/persistence.md)  

Small JSON persistence for settings and progress. Without a directory or a storage of its own, a store keeps its slots
in the platform's default medium ([DefaultStorage](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_DefaultStorage_System_String_)): files under the user's data folder
([DefaultDirectory](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_DefaultDirectory_System_String_)), and the page's localStorage in browsers. A composition can hand a game another medium
(<code>HostBuilder.Storage</code>); tests pass a temporary directory.
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

```csharp
public sealed class Progress { public int Level { get; set; } = 1; }

var store = new SaveStore("com.example.mygame");
var progress = store.Load<Progress>("progress") ?? new Progress();
progress.Level++;
store.Save("progress", progress);
```

## Remarks

A slot is a name such as "settings" or "progress"; with file storage it becomes <code>&lt;slot&gt;.json</code> in
[Directory](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_Directory). Values are serialized with `JsonSerializer` (public properties).

<p>
A slot is a plain name, never a path, and the same names are valid on every system, so a save can move between
them: letters, digits, spaces and punctuation such as <code>-</code>, <code>_</code> and <code>.</code> ("profile.v2"). Every call
that takes a slot throws `ArgumentException` for a name that is empty or white space, contains
<code>/ \ : * ? " &lt; &gt; |</code> or a control character, ends with a dot or a space (which includes "." and ".."), or
is a Windows device name ("CON", "PRN", "AUX", "NUL", "COM1", "LPT1" and the like, with or without an extension).
</p>
<p>
[Save``1](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_Save__1_System_String___0_) and [Load``1](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_Load__1_System_String_) find the properties of the saved type by reflection.
A game published trimmed or with NativeAOT has no reflection to serialize with: it declares its saved types in a
`JsonSerializerContext` and passes the generated metadata to
[Save``1](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_Save__1_System_String___0_) and [Load``1](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_Load__1_System_String_). Both pairs
write the same text and read each other's slots.
</p>

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

The folder for the slot files; null uses the platform's default medium ([DefaultStorage](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_DefaultStorage_System_String_)).

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

### <a id="MicroEngine_Core_SaveStore__ctor_MicroEngine_Platform_IStorage_"></a> SaveStore\(IStorage\)

Creates a store over any storage of the platform contract ([IStorage](MicroEngine.Platform.IStorage.md)).

```csharp
public SaveStore(IStorage storage)
```

#### Parameters

`storage` [IStorage](MicroEngine.Platform.IStorage.md)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">storage</code> is null.

## Properties

### <a id="MicroEngine_Core_SaveStore_Directory"></a> Directory

The save folder for file storage; otherwise a description of where saves live.

```csharp
public string Directory { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Core_SaveStore_DefaultDirectory_System_String_"></a> DefaultDirectory\(string\)

The save folder used when no directory is given (outside browsers):
<code>%LOCALAPPDATA%\MicroEngine\&lt;gameId&gt;</code> on Windows, <code>~/Library/Application Support/MicroEngine/&lt;gameId&gt;</code>
on macOS and iOS (inside the app's container), the app's private files folder (<code>files/MicroEngine/&lt;gameId&gt;</code>)
on Android, and <code>$XDG_DATA_HOME/MicroEngine/&lt;gameId&gt;</code> (by default <code>~/.local/share</code>) on Linux and
other Unix systems. Always absolute, also when the folder does not exist yet: the first save creates it.

```csharp
public static string DefaultDirectory(string gameId)
```

#### Parameters

`gameId` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_SaveStore_DefaultStorage_System_String_"></a> DefaultStorage\(string\)

The platform's default save medium for a game: the page's localStorage in browsers
([BrowserStorage](MicroEngine.Platform.BrowserStorage.md), keys <code>microengine/&lt;gameId&gt;/&lt;slot&gt;</code>), files in
[DefaultDirectory](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_DefaultDirectory_System_String_) everywhere else. A rule of the platform, not a setting: a host that wants another
medium composes it (<code>HostBuilder.Storage</code>) and the game passes it to
[#ctor](MicroEngine.Core.SaveStore.md).

```csharp
public static IStorage DefaultStorage(string gameId)
```

#### Parameters

`gameId` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [IStorage](MicroEngine.Platform.IStorage.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">gameId</code> is null, empty or white space.

### <a id="MicroEngine_Core_SaveStore_Delete_System_String_"></a> Delete\(string\)

Removes the slot's data; does nothing when it does not exist. A slot that could not be read can be saved again
after it is deleted: deleting is the game's explicit choice to start over.

```csharp
public void Delete(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">slot</code> is not a valid slot name (see the class remarks).

### <a id="MicroEngine_Core_SaveStore_Exists_System_String_"></a> Exists\(string\)

Whether the slot has saved data.

```csharp
public bool Exists(string slot)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">slot</code> is not a valid slot name (see the class remarks).

### <a id="MicroEngine_Core_SaveStore_Load__1_System_String_"></a> Load<T\>\(string\)

Reads and deserializes a slot, finding the properties of <code class="typeparamref">T</code> by reflection.

```csharp
[RequiresUnreferencedCode("Serializes the saved type with reflection, which trimmed and NativeAOT applications do not have. Declare the type in a JsonSerializerContext and pass its JsonTypeInfo<T> to the overload that takes one.")]
[RequiresDynamicCode("Serializes the saved type with reflection, which trimmed and NativeAOT applications do not have. Declare the type in a JsonSerializerContext and pass its JsonTypeInfo<T> to the overload that takes one.")]
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

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The application was published trimmed or with NativeAOT; use [Load``1](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_Load__1_System_String_).

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">slot</code> is not a valid slot name (see the class remarks).

### <a id="MicroEngine_Core_SaveStore_Load__1_System_String_System_Text_Json_Serialization_Metadata_JsonTypeInfo___0__"></a> Load<T\>\(string, JsonTypeInfo<T\>\)

Reads and deserializes a slot with the game's own metadata for <code class="typeparamref">T</code>, without reflection: the
way to load in a game published trimmed or with NativeAOT. It reads the slots [Save``1](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_Save__1_System_String___0_) wrote.

```csharp
public T? Load<T>(string slot, JsonTypeInfo<T> typeInfo) where T : class
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

The slot name.

`typeInfo` [JsonTypeInfo](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo\-1)<T\>

The metadata of <code class="typeparamref">T</code>, from the game's `JsonSerializerContext` (<code>MySaves.Default.Progress</code>).

#### Returns

 T?

The saved value, or null when the slot does not exist, holds invalid JSON (the data is set aside) or could not be read
(the slot is then protected from saving for the life of this store).

#### Type Parameters

`T` 

The saved type.

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">typeInfo</code> is null.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">slot</code> is not a valid slot name (see the class remarks).

### <a id="MicroEngine_Core_SaveStore_Save__1_System_String___0_"></a> Save<T\>\(string, T\)

Writes the whole slot at once, so a crash never leaves a half-written save, finding the properties of
<code class="typeparamref">T</code> by reflection. Returns false when the medium refused the write, or when the slot could
not be read earlier (see the class remarks).

```csharp
[RequiresUnreferencedCode("Serializes the saved type with reflection, which trimmed and NativeAOT applications do not have. Declare the type in a JsonSerializerContext and pass its JsonTypeInfo<T> to the overload that takes one.")]
[RequiresDynamicCode("Serializes the saved type with reflection, which trimmed and NativeAOT applications do not have. Declare the type in a JsonSerializerContext and pass its JsonTypeInfo<T> to the overload that takes one.")]
public bool Save<T>(string slot, T data)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

`data` T

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T` 

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The application was published trimmed or with NativeAOT; use [Save``1](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_Save__1_System_String___0_).

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">slot</code> is not a valid slot name (see the class remarks).

### <a id="MicroEngine_Core_SaveStore_Save__1_System_String___0_System_Text_Json_Serialization_Metadata_JsonTypeInfo___0__"></a> Save<T\>\(string, T, JsonTypeInfo<T\>\)

Writes the whole slot at once with the game's own metadata for <code class="typeparamref">T</code>, without reflection: the
way to save in a game published trimmed or with NativeAOT. The text is the same indented JSON
[Save``1](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_Save__1_System_String___0_) writes, so either call reads the other's slots. Returns false when the medium
refused the write, or when the slot could not be read earlier (see the class remarks).

```csharp
public bool Save<T>(string slot, T data, JsonTypeInfo<T> typeInfo)
```

#### Parameters

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

The slot name.

`data` T

The value to save.

`typeInfo` [JsonTypeInfo](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo\-1)<T\>

The metadata of <code class="typeparamref">T</code>, from the game's `JsonSerializerContext` (<code>MySaves.Default.Progress</code>).

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T` 

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">typeInfo</code> is null.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">slot</code> is not a valid slot name (see the class remarks).

</div>
