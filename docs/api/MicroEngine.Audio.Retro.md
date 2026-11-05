# Namespace MicroEngine.Audio.Retro

<div data-pagefind-weight="0.3">

Retro sound in code: sound effects described by parameters (in the spirit of sfxr), the synthesizer that renders them, instruments and waveforms, and the tiny step sequencer behind `Song`.

Guide: [Synthesized sound effects](../manual/audio.md#synthesized-sound-effects)  

### Classes

 [Instrument](MicroEngine.Audio.Retro.Instrument.md)

A tracker-style voice: waveform plus an ADSR envelope (seconds, sustain level 0..1).

 [SfxParams](MicroEngine.Audio.Retro.SfxParams.md)

Parameters for a retro sound effect (in the spirit of sfxr). Every game sound in the
samples is described with one of these, so audio is original, tweakable and license-free.

 [Song](MicroEngine.Audio.Retro.Song.md)

A tiny step sequencer. Patterns are whitespace-separated tokens, one per step:
a note ("C4", "F#3", "Bb5"), "-" to hold the previous note, "." to rest, "|" is ignored (bar lines).
Drum tracks use "k" (kick), "s" (snare), "h" (hat), "o" (open hat) and "." for rests.
Shorter tracks loop to the song length, so a four-step drum loop can drive a long melody.

 [Synth](MicroEngine.Audio.Retro.Synth.md)

Deterministic waveform synthesis for sound effects and music voices.

### Enums

 [Wave](MicroEngine.Audio.Retro.Wave.md)

Oscillator waveforms for [SfxParams](MicroEngine.Audio.Retro.SfxParams.md) and [Instrument](MicroEngine.Audio.Retro.Instrument.md): <code>Square</code>, <code>Triangle</code>, <code>Saw</code>,
<code>Sine</code>, <code>Noise</code> (a random value held for each cycle, so its frequency sets the colour of the noise), and the
thinner pulse waves <code>Pulse25</code> and <code>Pulse12</code> (25% and 12.5% duty cycle).

</div>
