# Class AudioDecoders

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

The audio formats the engine decodes, by file extension: one list for every way a file reaches the mixer (sound
effects decoded whole, music streamed while it plays, the "sound" and "music" asset kinds) and for every platform.
"wav" (PCM), "ogg" (Vorbis) and "mp3" are built in and decoded in managed code; [Add](MicroEngine.Audio.AudioDecoders.md#MicroEngine_Audio_AudioDecoders_Add_System_String_System_Func_System_Byte___MicroEngine_Audio_IAudioStream__) registers
another, with no change to the mixer.

```csharp
public static class AudioDecoders
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AudioDecoders](MicroEngine.Audio.AudioDecoders.md)

## Examples

```csharp
AudioDecoders.Add("raw", bytes => new RawStream(bytes));     // from now on "sfx/hit.raw" loads like any sound
Audio.AddSoundFile("hit", "Content/sfx/hit.raw");
```

## Remarks

The list is the process's: a decoder is a function over a file's bytes with no state of the game in it, and files
are decoded from the asset loaders' worker threads as well as from the game's. Register formats when the game
starts, before it loads content. Reads are safe from any thread.

## Properties

### <a id="MicroEngine_Audio_AudioDecoders_Formats"></a> Formats

The formats that can be decoded, lower case without the dot, in alphabetical order.

```csharp
public static IReadOnlyList<string> Formats { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

## Methods

### <a id="MicroEngine_Audio_AudioDecoders_Add_System_String_System_Func_System_Byte___MicroEngine_Audio_IAudioStream__"></a> Add\(string, Func<byte\[\], IAudioStream\>\)

Registers a format, replacing a decoder already registered for it (a built-in one too).

```csharp
public static void Add(string format, Func<byte[], IAudioStream> open)
```

#### Parameters

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)

The file extension, with or without its dot; compared without regard to case.

`open` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\], [IAudioStream](MicroEngine.Audio.IAudioStream.md)\>

Opens a stream over a whole file's bytes; it throws `InvalidDataException` for bytes it cannot
decode. It is called once to check a file when it is registered, once for each time the file plays as a stream,
and from any thread. A stream that also implements [ISeekableAudioStream](MicroEngine.Audio.ISeekableAudioStream.md) can be sought and loop
over a region.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">format</code> is null, empty or white space.

### <a id="MicroEngine_Audio_AudioDecoders_Decode_System_Byte___System_String_"></a> Decode\(byte\[\], string\)

Decodes a whole file into memory: a sound effect, or music that should not be decoded while it plays. The clip
carries the loop region the file names, if any.

```csharp
public static AudioClip Decode(byte[] encoded, string format)
```

#### Parameters

`encoded` [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [AudioClip](MicroEngine.Audio.AudioClip.md)

#### Exceptions

 [NotSupportedException](https://learn.microsoft.com/dotnet/api/system.notsupportedexception)

No decoder is registered for the format, or the WAV is not PCM.

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The bytes cannot be decoded as that format.

### <a id="MicroEngine_Audio_AudioDecoders_Open_System_Byte___System_String_"></a> Open\(byte\[\], string\)

Opens a stream that decodes <code class="paramref">encoded</code>, a whole file of <code class="paramref">format</code>, as it is read.

```csharp
public static IAudioStream Open(byte[] encoded, string format)
```

#### Parameters

`encoded` [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [IAudioStream](MicroEngine.Audio.IAudioStream.md)

#### Exceptions

 [NotSupportedException](https://learn.microsoft.com/dotnet/api/system.notsupportedexception)

No decoder is registered for the format.

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The bytes cannot be decoded as that format, or have other than 1 or 2 channels.

### <a id="MicroEngine_Audio_AudioDecoders_Remove_System_String_"></a> Remove\(string\)

Takes a format out of the list; false when it was not in it. Files of that format already loaded keep playing.

```csharp
public static bool Remove(string format)
```

#### Parameters

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioDecoders_Supports_System_String_"></a> Supports\(string?\)

Whether <code class="paramref">format</code> (a file extension, with or without its dot, in any case) can be decoded.

```csharp
public static bool Supports(string? format)
```

#### Parameters

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
