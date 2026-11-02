# Namespace MicroEngine.Audio

<div data-pagefind-weight="0.3">

Sound effects and music: synthesis, WAV decoding, the mixer games play sounds through, and the audio device contract.

Guide: [Audio](../manual/audio.md)  

### Classes

 [AudioClip](MicroEngine.Audio.AudioClip.md)

Uncompressed audio: interleaved float samples in -1..1.

 [AudioMixer](MicroEngine.Audio.AudioMixer.md)

Game-facing audio: a sound bank keyed by name plus music with fades.
Games register synthesized clips once and then call [Play](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Play_System_String_System_Single_System_Single_System_Single_System_Single_) by name.

 [CaptureAudioDevice](MicroEngine.Audio.CaptureAudioDevice.md)

An audio device that plays nothing aloud and records what would have been heard: it mixes sound effects and music
into stereo samples, advancing one game step per [Update](MicroEngine.Audio.CaptureAudioDevice.md#MicroEngine_Audio_CaptureAudioDevice_Update) (the game calls it once per update), so the same
run always produces the same samples. Tests compare its [Output](MicroEngine.Audio.CaptureAudioDevice.md#MicroEngine_Audio_CaptureAudioDevice_Output) (waves, durations, silences, amplitudes);
the launcher's <code>--audio-capture FILE</code> writes it as a WAV.

 [Instrument](MicroEngine.Audio.Instrument.md)

A tracker-style voice: waveform plus an ADSR envelope (seconds, sustain level 0..1).

 [MusicHandle](MicroEngine.Audio.MusicHandle.md)

An opaque handle to loaded, loopable music.

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

### Interfaces

 [IAudioDevice](MicroEngine.Audio.IAudioDevice.md)

Backend audio device. Implementations must tolerate missing hardware by becoming silent.

### Enums

 [Wave](MicroEngine.Audio.Wave.md)

Oscillator waveforms for [SfxParams](MicroEngine.Audio.SfxParams.md) and [Instrument](MicroEngine.Audio.Instrument.md): <code>Square</code>, <code>Triangle</code>, <code>Saw</code>,
<code>Sine</code>, <code>Noise</code> (a random value held for each cycle, so its frequency sets the colour of the noise), and the
thinner pulse waves <code>Pulse25</code> and <code>Pulse12</code> (25% and 12.5% duty cycle).

</div>
