# Enum AudioLoadMode

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

How a music file is kept once registered ([AddMusic](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_AddMusic_System_String_MicroEngine_Audio_AudioClip_System_Boolean_)).

```csharp
public enum AudioLoadMode
```

## Fields

`Decoded = 1` 

The file is decoded whole when it is registered: about ten megabytes per minute of stereo at 44.1 kHz, nothing
to decode while it plays, and it can change pitch, seek to any sample at once and play many times over at no
cost. For short pieces and ones played often.



`Streamed = 0` 

The file's bytes are kept and decoded a little ahead of what plays: the least memory, a small cost in every
frame while it plays, and its own pitch. The default for music.

</div>
