# Interface IAssetImporter

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

Reserved: a build-time step that turns a source file into the file a loader reads (a texture compressed for the GPU,
a model converted from glTF, a font baked into a distance field), driven by settings kept next to the source file.
The engine does not call importers yet; the interface fixes where that step will plug in so loaders never need to
change when it arrives. Content stays usable without any importer: loaders read source files directly.

```csharp
public interface IAssetImporter
```

## Properties

### <a id="MicroEngine_Content_IAssetImporter_Name"></a> Name

A name for the import step, for diagnostics and settings files.

```csharp
string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Content_IAssetImporter_SourceExtensions"></a> SourceExtensions

The source file extensions it imports (lower case, with the dot).

```csharp
IReadOnlyList<string> SourceExtensions { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

## Methods

### <a id="MicroEngine_Content_IAssetImporter_Import_System_String_System_IO_Stream_System_Collections_Generic_IReadOnlyDictionary_System_String_System_String__System_IO_Stream_"></a> Import\(string, Stream, IReadOnlyDictionary<string, string\>, Stream\)

Writes the imported form of <code class="paramref">source</code> to <code class="paramref">output</code>.

```csharp
void Import(string sourcePath, Stream source, IReadOnlyDictionary<string, string> settings, Stream output)
```

#### Parameters

`sourcePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

The source file's content path.

`source` [Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)

The source file.

`settings` [IReadOnlyDictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlydictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [string](https://learn.microsoft.com/dotnet/api/system.string)\>

The import settings kept next to the source, by name.

`output` [Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)

Where the imported file is written.

</div>
