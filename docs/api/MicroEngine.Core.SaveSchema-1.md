# Class SaveSchema<T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.Saves.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

How a saved type changes between releases of a game: the version this build writes and one step for each earlier
version, which turns a save of that version into the next. [Load``1](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_Load__1_System_String_)
runs the steps a save needs, in order, and [Save``1](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_Save__1_System_String___0_) stamps the
current version.

```csharp
public sealed class SaveSchema<T> where T : class
```

#### Type Parameters

`T` 

The saved type.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SaveSchema<T\>](MicroEngine.Core.SaveSchema\-1.md)

## Examples

```csharp
public static readonly SaveSchema<Profile> Schema = new SaveSchema<Profile>(3, MySaves.Default.Profile)
    .Migrate(1, save => save["Lives"] = 3)                                             // 1 -> 2: lives arrived
    .Migrate(2, save => { save["Gold"] = save["Coins"]?.DeepClone(); save.Remove("Coins"); }); // 2 -> 3: a rename

var loaded = Saves.Load("profile", Schema);
var profile = loaded.Value ?? new Profile();
Saves.Save("profile", profile, Schema);
```

## Remarks

<p>
Versions are whole numbers from 1. A save with no version (written before the type had a schema, or by
[Save``1](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_Save__1_System_String___0_)) is version 1, so a game adopts a schema at version 1 with no steps and
its players' saves keep loading. A release that renames, removes or reinterprets a saved property raises the
version by one and adds the step from the previous one; adding a property needs neither.
</p>
<p>
A step receives the save as a `JsonObject`, without its version property, and changes it in place. It
is an ordinary method: test it by itself with a JSON literal. Never edit a step that has shipped; saves of that
version are on players' machines. A step that throws, for any reason, leaves the slot as it is
([MigrationFailed](MicroEngine.Core.SaveLoadStatus.md)).
</p>
<p>
The saved value must serialize to a JSON object. With [#ctor](MicroEngine.Core.md) nothing
here uses reflection: steps work on JSON nodes and the type is read and written with the game's metadata, so it
works in games published trimmed or with NativeAOT. A schema has no state besides its declaration: build it once
and keep it in a static field.
</p>

## Constructors

### <a id="MicroEngine_Core_SaveSchema_1__ctor_System_Int32_System_Text_Json_Serialization_Metadata_JsonTypeInfo__0__"></a> SaveSchema\(int, JsonTypeInfo<T\>\)

A schema that reads and writes <code class="typeparamref">T</code> with the game's own metadata, without reflection.

```csharp
public SaveSchema(int version, JsonTypeInfo<T> typeInfo)
```

#### Parameters

`version` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The version this build writes: 1 for a type that has never changed.

`typeInfo` [JsonTypeInfo](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo\-1)<T\>

The metadata of <code class="typeparamref">T</code>, from the game's `JsonSerializerContext` (<code>MySaves.Default.Profile</code>).

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">version</code> is below 1.

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">typeInfo</code> is null.

### <a id="MicroEngine_Core_SaveSchema_1__ctor_System_Int32_"></a> SaveSchema\(int\)

A schema that finds the properties of <code class="typeparamref">T</code> by reflection, like
[Save``1](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_Save__1_System_String___0_): for games that are not published trimmed or with NativeAOT.

```csharp
[RequiresUnreferencedCode("Serializes the saved type with reflection, which trimmed and NativeAOT applications do not have. Declare the type in a JsonSerializerContext and pass its JsonTypeInfo<T> to the constructor that takes one.")]
[RequiresDynamicCode("Serializes the saved type with reflection, which trimmed and NativeAOT applications do not have. Declare the type in a JsonSerializerContext and pass its JsonTypeInfo<T> to the constructor that takes one.")]
public SaveSchema(int version)
```

#### Parameters

`version` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The version this build writes: 1 for a type that has never changed.

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">version</code> is below 1.

## Properties

### <a id="MicroEngine_Core_SaveSchema_1_Version"></a> Version

The version this build writes.

```csharp
public int Version { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Core_SaveSchema_1_Migrate_System_Int32_System_Action_System_Text_Json_Nodes_JsonObject__"></a> Migrate\(int, Action<JsonObject\>\)

Declares the step that turns a save of version <code class="paramref">from</code> into version <code class="paramref">from</code> + 1.
Every version from 1 to [Version](MicroEngine.Core.md) - 1 needs one; a schema with a step missing throws
`InvalidOperationException` when it is used.

```csharp
public SaveSchema<T> Migrate(int from, Action<JsonObject> step)
```

#### Parameters

`from` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The version the step receives.

`step` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[JsonObject](https://learn.microsoft.com/dotnet/api/system.text.json.nodes.jsonobject)\>

Changes the save in place. The version property is not in it.

#### Returns

 [SaveSchema](MicroEngine.Core.SaveSchema\-1.md)<T\>

This schema, for chaining.

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">from</code> is below 1, or is not below [Version](MicroEngine.Core.md).

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The version already has a step.

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">step</code> is null.

### <a id="MicroEngine_Core_SaveSchema_1_Read_System_String_"></a> Read\(string\)

Reads a save from its text: checks its version, runs the steps it needs and builds the value. It touches no
storage, so it is also how a test loads the text an earlier release wrote. It never returns
[Missing](MicroEngine.Core.SaveLoadStatus.md) or [Unreadable](MicroEngine.Core.SaveLoadStatus.md), which are the storage's.

```csharp
public SaveLoadResult<T> Read(string text)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [SaveLoadResult](MicroEngine.Core.SaveLoadResult\-1.md)<T\>

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">text</code> is null.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

A version below [Version](MicroEngine.Core.md) has no step.

### <a id="MicroEngine_Core_SaveSchema_1_Write__0_"></a> Write\(T\)

The text of a save: the value as indented JSON, as [Save``1](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_Save__1_System_String___0_) writes it, with the
current version as its first property (after the serializer's own, such as <code>"$type"</code>).

```csharp
public string Write(T value)
```

#### Parameters

`value` T

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">value</code> is null.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

A version below [Version](MicroEngine.Core.md) has no step; <code class="typeparamref">T</code> does not serialize to a JSON object;
or it has a property serialized as [VersionProperty](MicroEngine.Core.SaveSchema.md#MicroEngine_Core_SaveSchema_VersionProperty).

</div>
