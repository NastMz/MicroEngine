# Namespace MicroEngine.Audio.Effects

<div data-pagefind-weight="0.3">

### Classes

 [BiquadFilter](MicroEngine.Audio.Effects.BiquadFilter.md)

A second-order filter (12 dB per octave): the base of [LowPass](MicroEngine.Audio.Effects.LowPass.md) and [HighPass](MicroEngine.Audio.Effects.HighPass.md). A change of
[Cutoff](MicroEngine.Audio.Effects.BiquadFilter.md#MicroEngine_Audio_Effects_BiquadFilter_Cutoff) or [Resonance](MicroEngine.Audio.Effects.BiquadFilter.md#MicroEngine_Audio_Effects_BiquadFilter_Resonance) glides over a fiftieth of a second instead of clicking.

 [Compressor](MicroEngine.Audio.Effects.Compressor.md)

Turns a bus down while it, or another bus, is loud. Over [Threshold](MicroEngine.Audio.Effects.Compressor.md#MicroEngine_Audio_Effects_Compressor_Threshold), each decibel of level gives only
1/[Ratio](MicroEngine.Audio.Effects.Compressor.md#MicroEngine_Audio_Effects_Compressor_Ratio) of a decibel out. With [Sidechain](MicroEngine.Audio.Effects.Compressor.md#MicroEngine_Audio_Effects_Compressor_Sidechain) naming another bus, it is that bus's level that
counts: music turned down while a voice line plays (ducking).

 [Delay](MicroEngine.Audio.Effects.Delay.md)

An echo: the bus is heard again [Time](MicroEngine.Audio.Effects.Delay.md#MicroEngine_Audio_Effects_Delay_Time) seconds later, and each repeat again, [Feedback](MicroEngine.Audio.Effects.Delay.md#MicroEngine_Audio_Effects_Delay_Feedback)
times as loud.

 [HighPass](MicroEngine.Audio.Effects.HighPass.md)

Lets through what is above [Cutoff](MicroEngine.Audio.Effects.BiquadFilter.md#MicroEngine_Audio_Effects_BiquadFilter_Cutoff) and cuts what is below: a thin sound (a radio, a
telephone), or a rumble removed.

 [Limiter](MicroEngine.Audio.Effects.Limiter.md)

Keeps a mix within a ceiling: when a frame would go over [Threshold](MicroEngine.Audio.Effects.Limiter.md#MicroEngine_Audio_Effects_Limiter_Threshold), the gain drops at once to what
keeps it there, then recovers over [Release](MicroEngine.Audio.Effects.Limiter.md#MicroEngine_Audio_Effects_Limiter_Release). It looks at nothing ahead, so it adds no delay; and while
the mix stays within the threshold (and the gain has recovered) every sample passes exactly as it is.

 [LowPass](MicroEngine.Audio.Effects.LowPass.md)

Lets through what is below [Cutoff](MicroEngine.Audio.Effects.BiquadFilter.md#MicroEngine_Audio_Effects_BiquadFilter_Cutoff) and cuts what is above: a muffled sound (music behind
a pause menu, under water, behind a wall).

 [Reverb](MicroEngine.Audio.Effects.Reverb.md)

A room: the bus is heard with the reflections of a space around it, from a small room to a hall
([RoomSize](MicroEngine.Audio.Effects.Reverb.md#MicroEngine_Audio_Effects_Reverb_RoomSize)). The design is Freeverb's (Jezar at Dreampoint, public domain): eight comb filters and four
all-pass filters for each side.

</div>
