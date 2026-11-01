namespace MicroEngine.Audio;

/// <summary>
/// Oscillator waveforms for <see cref="SfxParams"/> and <see cref="Instrument"/>: <c>Square</c>, <c>Triangle</c>, <c>Saw</c>,
/// <c>Sine</c>, <c>Noise</c> (a random value held for each cycle, so its frequency sets the colour of the noise), and the
/// thinner pulse waves <c>Pulse25</c> and <c>Pulse12</c> (25% and 12.5% duty cycle).
/// </summary>
public enum Wave { Square, Triangle, Saw, Sine, Noise, Pulse25, Pulse12 }

/// <summary>
/// Parameters for a retro sound effect (in the spirit of sfxr). Every game sound in the
/// samples is described with one of these, so audio is original, tweakable and license-free.
/// </summary>
public sealed record SfxParams
{
    /// <summary>The oscillator waveform (square by default).</summary>
    public Wave Wave { get; init; } = Wave.Square;
    /// <summary>Start frequency in Hz.</summary>
    public float Frequency { get; init; } = 440f;
    /// <summary>Frequency at the end of the sound (exponential slide). Null keeps it constant.</summary>
    public float? EndFrequency { get; init; }
    /// <summary>Seconds to rise from silence to full volume.</summary>
    public float Attack { get; init; } = 0.005f;
    /// <summary>Seconds held at full volume after the attack.</summary>
    public float Sustain { get; init; } = 0.08f;
    /// <summary>Seconds to fade to silence after the sustain. The sound lasts <c>Attack + Sustain + Decay</c> seconds.</summary>
    public float Decay { get; init; } = 0.12f;
    /// <summary>Peak amplitude, 0..1 (default 0.5).</summary>
    public float Volume { get; init; } = 0.5f;
    /// <summary>Vibrato depth as a fraction of the frequency (0 disables; 0.02 is a light wobble).</summary>
    public float VibratoDepth { get; init; }
    /// <summary>Vibrato speed in Hz (default 8).</summary>
    public float VibratoRate { get; init; } = 8f;
    /// <summary>Optional arpeggio: frequency multipliers stepped every <see cref="ArpeggioStep"/> seconds.</summary>
    public float[]? Arpeggio { get; init; }
    /// <summary>Seconds each <see cref="Arpeggio"/> multiplier lasts (default 0.05).</summary>
    public float ArpeggioStep { get; init; } = 0.05f;
    /// <summary>Simple one-pole low-pass in Hz (0 disables). Softens harsh square/noise sounds.</summary>
    public float LowPass { get; init; }
    /// <summary>Bit-crush to N levels (0 disables) for crunchy hits.</summary>
    public int Crush { get; init; }
}

/// <summary>Deterministic waveform synthesis for sound effects and music voices.</summary>
public static class Synth
{
    /// <summary>Synthesizes a mono sound effect. The same parameters always produce the same samples.</summary>
    /// <param name="p">The sound description.</param>
    /// <param name="sampleRate">Samples per second of the result.</param>
    /// <returns>A mono clip, ready for <see cref="AudioMixer.AddSound"/>.</returns>
    public static AudioClip Render(SfxParams p, int sampleRate = AudioClip.DefaultSampleRate)
    {
        ArgumentNullException.ThrowIfNull(p);
        float duration = p.Attack + p.Sustain + p.Decay;
        int count = Math.Max(1, (int)(duration * sampleRate));
        var samples = new float[count];
        var rng = new Random(91);
        double phase = 0;
        float noise = 0, filtered = 0;
        float alpha = p.LowPass > 0 ? 1f - MathF.Exp(-MathF.Tau * p.LowPass / sampleRate) : 1f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)sampleRate;
            float progress = t / duration;
            float freq = p.EndFrequency is { } end ? p.Frequency * MathF.Pow(end / p.Frequency, progress) : p.Frequency;
            if (p.Arpeggio is { Length: > 0 } arp) freq *= arp[Math.Min(arp.Length - 1, (int)(t / p.ArpeggioStep)) % arp.Length];
            if (p.VibratoDepth > 0) freq *= 1 + MathF.Sin(t * p.VibratoRate * MathF.Tau) * p.VibratoDepth;
            double previous = phase;
            phase = (phase + freq / sampleRate) % 1.0;
            if (phase < previous) noise = (float)(rng.NextDouble() * 2 - 1);
            float s = Oscillator(p.Wave, (float)phase, noise);
            s *= Envelope(t, p.Attack, p.Sustain, p.Decay) * p.Volume;
            filtered += (s - filtered) * alpha;
            s = filtered;
            if (p.Crush > 1) s = MathF.Round(s * p.Crush) / p.Crush;
            samples[i] = s;
        }
        return new AudioClip(samples, sampleRate);
    }

    /// <summary>The value of <paramref name="wave"/> at <paramref name="phase"/>, in -1..1.</summary>
    /// <param name="wave">The waveform.</param>
    /// <param name="phase">Position in the cycle, 0..1.</param>
    /// <param name="noise">The value returned for <see cref="Wave.Noise"/>.</param>
    public static float Oscillator(Wave wave, float phase, float noise) => wave switch
    {
        Wave.Square => phase < 0.5f ? 1f : -1f,
        Wave.Pulse25 => phase < 0.25f ? 1f : -1f,
        Wave.Pulse12 => phase < 0.125f ? 1f : -1f,
        Wave.Triangle => 1f - 4f * MathF.Abs(phase - 0.5f),
        Wave.Saw => 2f * phase - 1f,
        Wave.Sine => MathF.Sin(phase * MathF.Tau),
        Wave.Noise => noise,
        _ => 0f,
    };

    /// <summary>A linear attack-sustain-decay envelope: 0..1 at <paramref name="t"/> seconds.</summary>
    public static float Envelope(float t, float attack, float sustain, float decay)
    {
        if (t < attack) return attack <= 0 ? 1 : t / attack;
        t -= attack;
        if (t < sustain) return 1f;
        t -= sustain;
        return decay <= 0 ? 0 : MathF.Max(0, 1f - t / decay);
    }

    /// <summary>Mixes several clips (mono) starting at given offsets in seconds.</summary>
    public static AudioClip Layer(params (AudioClip Clip, float Offset)[] parts)
    {
        int rate = AudioClip.DefaultSampleRate;
        int length = parts.Max(p => (int)(p.Offset * rate) + p.Clip.Samples.Length);
        var mix = new float[length];
        foreach (var (clip, offset) in parts)
        {
            int start = (int)(offset * rate);
            for (int i = 0; i < clip.Samples.Length; i++) mix[start + i] += clip.Samples[i];
        }
        for (int i = 0; i < mix.Length; i++) mix[i] = MathF.Tanh(mix[i]);
        return new AudioClip(mix, rate);
    }

    /// <summary>Frequency of a note name such as "C4", "F#3" or "Bb5" (A4 = 440 Hz).</summary>
    public static float NoteFrequency(string note)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(note);
        int semitone = char.ToUpperInvariant(note[0]) switch
        {
            'C' => 0, 'D' => 2, 'E' => 4, 'F' => 5, 'G' => 7, 'A' => 9, 'B' => 11,
            _ => throw new FormatException($"Invalid note '{note}'."),
        };
        int i = 1;
        if (i < note.Length && note[i] == '#') { semitone++; i++; }
        else if (i < note.Length && note[i] == 'b') { semitone--; i++; }
        if (!int.TryParse(note.AsSpan(i), out int octave)) throw new FormatException($"Invalid note '{note}'.");
        int midi = (octave + 1) * 12 + semitone;
        return 440f * MathF.Pow(2f, (midi - 69) / 12f);
    }
}
