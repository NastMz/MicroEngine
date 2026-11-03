# Namespace MicroEngine.Audio

<div data-pagefind-weight="0.3">

Sound effects and music: the one mixer every backend plays through (voices with handles and priorities, buses, music streams, exact-frame scheduling), the sound bank and music games play through, the audio device contract, offline rendering for tests, WAV and OGG decoding, synthesis.

Guide: [Audio](../manual/audio.md)  

### Classes

 [AudioBus](MicroEngine.Audio.AudioBus.md)

A named mix stage: the sounds and buses under it are scaled by its [Volume](MicroEngine.Audio.AudioBus.md#MicroEngine_Audio_AudioBus_Volume). Buses form a tree under
"master": a bus is created under a parent that already exists and is never moved, so there is no cycle.

 [AudioClip](MicroEngine.Audio.AudioClip.md)

Uncompressed audio: interleaved float samples in -1..1.

 [AudioEngineSettings](MicroEngine.Audio.AudioEngineSettings.md)

Settings of an [AudioEngine](MicroEngine.Audio.AudioEngine.md).

 [AudioEngine](MicroEngine.Audio.AudioEngine.md)

The engine's one mixer: game code plays sounds here on the main thread, a sink renders them. Every backend plays
through it, so voices, priorities, stealing, buses and fades behave the same everywhere, and an offline sink renders
exactly what a device would play.

 [AudioMixer](MicroEngine.Audio.AudioMixer.md)

Game-facing audio: a sound bank keyed by name plus music with fades.
Games register synthesized clips once and then call [Play](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Play_System_String_System_Single_System_Single_System_Single_System_Single_) by name.

 [CaptureAudioDevice](MicroEngine.Audio.CaptureAudioDevice.md)

An audio device that plays nothing aloud and records what would have been heard: the engine's own mixer
([MixerAudioDevice](MicroEngine.Audio.MixerAudioDevice.md)) rendering into an offline sink, one game step per [Update](MicroEngine.Audio.CaptureAudioDevice.md#MicroEngine_Audio_CaptureAudioDevice_Update) (the game calls
it once per update), so the same run always produces the same samples. Tests compare its [Output](MicroEngine.Audio.CaptureAudioDevice.md#MicroEngine_Audio_CaptureAudioDevice_Output) (waves,
durations, silences, amplitudes); the launcher's <code>--audio-capture FILE</code> writes it as a WAV.

 [EncodedAudio](MicroEngine.Audio.EncodedAudio.md)

The bytes of a music file loaded from content (the "music" asset kind) and its format ("wav", "ogg", "mp3"): music is
decoded while it plays rather than all at once.

 [Instrument](MicroEngine.Audio.Instrument.md)

A tracker-style voice: waveform plus an ADSR envelope (seconds, sustain level 0..1).

 [MixerAudioDevice](MicroEngine.Audio.MixerAudioDevice.md)

An [IAudioDevice](MicroEngine.Audio.IAudioDevice.md) over the engine's one mixer ([AudioEngine](MicroEngine.Audio.AudioEngine.md)) and a PCM sink: every backend
device is one of these with its own sink, so voices, priorities, stealing, music and fades behave the same
everywhere. Music decodes in managed code: WAV, OGG Vorbis (streamed, decoded ahead outside the simulation step), and
formats a backend adds ([AddDecoder](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_AddDecoder_System_String_System_Func_System_Byte___MicroEngine_Audio_IAudioStream__)).

 [MusicHandle](MicroEngine.Audio.MusicHandle.md)

An opaque handle to loaded, loopable music.

 [OfflineAudioSink](MicroEngine.Audio.OfflineAudioSink.md)

The test sink: renders into memory with no device and no clock. Each [Pump](MicroEngine.Audio.OfflineAudioSink.md#MicroEngine_Audio_OfflineAudioSink_Pump) renders exactly one
simulation step of frames (the step's share of the rate, so 44100 Hz at 60 steps alternates 735-frame steps exactly),
in blocks of at most the block size, so the output depends only on what the game played, never on time.

 [SfxParams](MicroEngine.Audio.SfxParams.md)

Parameters for a retro sound effect (in the spirit of sfxr). Every game sound in the
samples is described with one of these, so audio is original, tweakable and license-free.

 [SilentAudioDevice](MicroEngine.Audio.SilentAudioDevice.md)

Silent device used headlessly and as a fallback when no audio hardware is present.

 [Song](MicroEngine.Audio.Song.md)

A tiny step sequencer. Patterns are whitespace-separated tokens, one per step:
a note ("C4", "F#3", "Bb5"), "-" to hold the previous note, "." to rest, "|" is ignored (bar lines).
Drum tracks use "k" (kick), "s" (snare), "h" (hat), "o" (open hat) and "." for rests.
Shorter tracks loop to the song length, so a four-step drum loop can drive a long melody.

 [SoundHandle](MicroEngine.Audio.SoundHandle.md)

An opaque handle to a loaded sound effect.

 [Synth](MicroEngine.Audio.Synth.md)

Deterministic waveform synthesis for sound effects and music voices.

### Structs

 [PlayParameters](MicroEngine.Audio.PlayParameters.md)

How a sound plays.

 [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

A sound playing in an [AudioEngine](MicroEngine.Audio.AudioEngine.md): a voice slot and the generation of the sound that took it. Once the
sound ends, is stopped or is stolen by the voice limit, the handle is stale and calls with it do nothing.
<code>default</code> is [None](MicroEngine.Audio.VoiceHandle.md#MicroEngine_Audio_VoiceHandle_None), which [Play](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Play_MicroEngine_Audio_AudioClip_MicroEngine_Audio_PlayParameters__) also
returns for a sound the voice policy refused.

### Interfaces

 [IAudioDevice](MicroEngine.Audio.IAudioDevice.md)

Backend audio device. Implementations must tolerate missing hardware by becoming silent.

 [IAudioStream](MicroEngine.Audio.IAudioStream.md)

Streamed audio (music decoded while it plays). The engine reads it only in [Pump](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Pump), on the main
thread outside the simulation step, ahead of the sink.

### Enums

 [Wave](MicroEngine.Audio.Wave.md)

Oscillator waveforms for [SfxParams](MicroEngine.Audio.SfxParams.md) and [Instrument](MicroEngine.Audio.Instrument.md): <code>Square</code>, <code>Triangle</code>, <code>Saw</code>,
<code>Sine</code>, <code>Noise</code> (a random value held for each cycle, so its frequency sets the colour of the noise), and the
thinner pulse waves <code>Pulse25</code> and <code>Pulse12</code> (25% and 12.5% duty cycle).

</div>
