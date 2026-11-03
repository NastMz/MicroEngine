# Class AudioAssetLoaders

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

The audio loaders of the content store (<code>Game.Assets.Sound</code>, <code>Game.Assets.Music</code>): "sound" decodes a PCM WAV file into an [AudioClip](MicroEngine.Audio.AudioClip.md) (".wav"), and
"music" reads a music file's bytes as [EncodedAudio](MicroEngine.Audio.EncodedAudio.md) (".ogg", ".mp3"; a ".wav" asked for as music too).

```csharp
public static class AudioAssetLoaders
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AudioAssetLoaders](MicroEngine.Audio.AudioAssetLoaders.md)

## Fields

### <a id="MicroEngine_Audio_AudioAssetLoaders_Music"></a> Music

The kind of music files ([EncodedAudio](MicroEngine.Audio.EncodedAudio.md)).

```csharp
public const string Music = "music"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Audio_AudioAssetLoaders_Sound"></a> Sound

The kind of decoded sound effects ([AudioClip](MicroEngine.Audio.AudioClip.md)).

```csharp
public const string Sound = "sound"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Audio_AudioAssetLoaders_Register_MicroEngine_Content_AssetLoaderRegistry_"></a> Register\(AssetLoaderRegistry\)

Registers both loaders in <code class="paramref">loaders</code> (<code>Game.Assets</code> has them already).

```csharp
public static void Register(AssetLoaderRegistry loaders)
```

#### Parameters

`loaders` AssetLoaderRegistry

</div>
