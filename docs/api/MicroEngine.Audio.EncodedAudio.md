# Class EncodedAudio

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

The bytes of a music file loaded from content (the "music" asset kind) and its format ("wav", "ogg", "mp3"): music is
decoded while it plays rather than all at once.

```csharp
public sealed class EncodedAudio
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EncodedAudio](MicroEngine.Audio.EncodedAudio.md)

## Constructors

### <a id="MicroEngine_Audio_EncodedAudio__ctor_System_Byte___System_String_"></a> EncodedAudio\(byte\[\], string\)

Wraps a music file's bytes without copying them.

```csharp
public EncodedAudio(byte[] data, string format)
```

#### Parameters

`data` [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

The whole file.

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)

Its format, the file extension without the dot ("ogg"); compared without regard to case.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">format</code> is null, empty or white space.

## Properties

### <a id="MicroEngine_Audio_EncodedAudio_Data"></a> Data

The file's bytes. The array is not copied.

```csharp
public byte[] Data { get; }
```

#### Property Value

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

### <a id="MicroEngine_Audio_EncodedAudio_Format"></a> Format

The format, lower case without the dot: "wav", "ogg", "mp3".

```csharp
public string Format { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>
