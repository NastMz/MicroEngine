namespace MicroEngine.Audio;

/// <summary>A tracker-style voice: waveform plus an ADSR envelope (seconds, sustain level 0..1).</summary>
public sealed record Instrument(
    Wave Wave,
    float Volume = 0.3f,
    float Attack = 0.005f,
    float Decay = 0.08f,
    float SustainLevel = 0.6f,
    float Release = 0.06f,
    float Pan = 0.5f,
    float VibratoDepth = 0f,
    float VibratoRate = 5f,
    float LowPass = 0f)
{
    /// <summary>A bright 25% pulse with light vibrato, for melodies.</summary>
    public static readonly Instrument Lead = new(Wave.Pulse25, 0.22f, 0.01f, 0.1f, 0.55f, 0.08f, 0.45f, 0.004f, 5.5f, 5000);
    /// <summary>A round triangle wave, for bass lines.</summary>
    public static readonly Instrument Bass = new(Wave.Triangle, 0.42f, 0.004f, 0.05f, 0.8f, 0.04f, 0.5f);
    /// <summary>A soft, filtered saw with slow attack and release, for chords.</summary>
    public static readonly Instrument Pad = new(Wave.Saw, 0.1f, 0.08f, 0.3f, 0.6f, 0.25f, 0.55f, 0.003f, 4f, 1800);
    /// <summary>A short, filtered square, for arpeggios and accompaniment.</summary>
    public static readonly Instrument Pluck = new(Wave.Square, 0.16f, 0.002f, 0.12f, 0.15f, 0.05f, 0.6f, 0, 5, 3500);
    /// <summary>A sine with a long decay, for chimes.</summary>
    public static readonly Instrument Bell = new(Wave.Sine, 0.28f, 0.002f, 0.4f, 0.1f, 0.3f, 0.55f, 0.002f, 6f);
}

/// <summary>
/// A tiny step sequencer. Patterns are whitespace-separated tokens, one per step:
/// a note ("C4", "F#3", "Bb5"), "-" to hold the previous note, "." to rest, "|" is ignored (bar lines).
/// Drum tracks use "k" (kick), "s" (snare), "h" (hat), "o" (open hat) and "." for rests.
/// Shorter tracks loop to the song length, so a four-step drum loop can drive a long melody.
/// </summary>
public sealed class Song
{
    private readonly List<(Instrument Instrument, string[] Steps)> _tracks = [];
    private readonly List<string[]> _drums = [];
    /// <summary>Tempo in beats per minute.</summary>
    public float Bpm { get; }
    /// <summary>Pattern steps per beat (4 by default: each step is a sixteenth note in 4/4).</summary>
    public int StepsPerBeat { get; }
    /// <summary>Volume of the drum tracks (default 0.5).</summary>
    public float DrumVolume { get; set; } = 0.5f;
    /// <summary>Feedback echo mix (0 disables). Adds space to otherwise dry chip voices.</summary>
    public float Echo { get; set; } = 0.18f;
    /// <summary>Delay of the echo in beats (default 0.75).</summary>
    public float EchoDelayBeats { get; set; } = 0.75f;

    /// <summary>Creates an empty song.</summary>
    /// <param name="bpm">Tempo in beats per minute; must be positive.</param>
    /// <param name="stepsPerBeat">Pattern steps per beat; must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bpm"/> or <paramref name="stepsPerBeat"/> is not positive.</exception>
    public Song(float bpm, int stepsPerBeat = 4)
    {
        if (bpm <= 0) throw new ArgumentOutOfRangeException(nameof(bpm));
        if (stepsPerBeat <= 0) throw new ArgumentOutOfRangeException(nameof(stepsPerBeat));
        Bpm = bpm;
        StepsPerBeat = stepsPerBeat;
    }

    /// <summary>Length of one step in seconds.</summary>
    public float StepSeconds => 60f / Bpm / StepsPerBeat;

    /// <summary>Adds a melodic track. The patterns are joined in order into one sequence of steps.</summary>
    /// <param name="instrument">The voice that plays the notes.</param>
    /// <param name="patterns">Note patterns (see the class remarks for the token format).</param>
    /// <returns>This song, for chaining.</returns>
    public Song Track(Instrument instrument, params string[] patterns)
    {
        ArgumentNullException.ThrowIfNull(instrument);
        _tracks.Add((instrument, Tokenize(patterns)));
        return this;
    }

    /// <summary>Adds a drum track of "k", "s", "h", "o" and "." tokens; the patterns are joined in order.</summary>
    /// <returns>This song, for chaining.</returns>
    public Song Drums(params string[] patterns)
    {
        _drums.Add(Tokenize(patterns));
        return this;
    }

    /// <summary>Number of steps in the longest track; the rendered song is this long.</summary>
    public int LengthInSteps => Math.Max(_tracks.Select(t => t.Steps.Length).DefaultIfEmpty(0).Max(), _drums.Select(d => d.Length).DefaultIfEmpty(0).Max());

    private static string[] Tokenize(string[] patterns) => patterns
        .SelectMany(p => p.Split((char[])[' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
        .Where(t => t != "|")
        .ToArray();

    /// <summary>
    /// Throws what <see cref="Render"/> would throw for the song's content, without rendering it (a few microseconds):
    /// songs that render in the background report their mistakes where they are registered.
    /// </summary>
    internal void Validate()
    {
        if (LengthInSteps == 0) throw new InvalidOperationException("Song has no steps.");
        foreach (var (_, track) in _tracks)
            foreach (var token in track)
                if (token is not ("." or "-")) Synth.NoteFrequency(token);
        foreach (var drum in _drums)
            foreach (var token in drum)
                if (token is not ("." or "-" or "k" or "s" or "h" or "o")) throw new FormatException($"Unknown drum '{token}'.");
    }

    /// <summary>Renders the whole song to stereo PCM, sized to loop seamlessly (echo tails wrap around).</summary>
    /// <exception cref="InvalidOperationException">The song has no steps.</exception>
    /// <exception cref="FormatException">A track has an invalid note or a drum track an unknown drum.</exception>
    public AudioClip Render(int sampleRate = AudioClip.DefaultSampleRate)
    {
        int steps = LengthInSteps;
        if (steps == 0) throw new InvalidOperationException("Song has no steps.");
        double stepSamples = StepSeconds * sampleRate;
        int total = (int)Math.Round(steps * stepSamples);
        var left = new float[total];
        var right = new float[total];
        foreach (var (instrument, track) in _tracks) RenderTrack(instrument, track, steps, stepSamples, sampleRate, left, right);
        var rng = new Random(5);
        foreach (var drum in _drums)
            for (int s = 0; s < steps; s++)
            {
                var token = drum[s % drum.Length];
                if (token is "." or "-") continue;
                RenderDrum(token, (int)Math.Round(s * stepSamples), sampleRate, left, right, rng);
            }
        if (Echo > 0)
        {
            int delay = (int)(EchoDelayBeats * 60f / Bpm * sampleRate);
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < total; i++)
                {
                    int j = (i - delay + total) % total;
                    // Ping-pong: each side echoes the other for width.
                    float l = left[i], r = right[i];
                    left[i] = l + right[j] * Echo * 0.5f;
                    right[i] = r + left[j] * Echo * 0.5f;
                }
        }
        var interleaved = new float[total * 2];
        for (int i = 0; i < total; i++)
        {
            interleaved[i * 2] = MathF.Tanh(left[i] * 1.1f) * 0.9f;
            interleaved[i * 2 + 1] = MathF.Tanh(right[i] * 1.1f) * 0.9f;
        }
        return new AudioClip(interleaved, sampleRate, 2);
    }

    private static void RenderTrack(Instrument ins, string[] track, int steps, double stepSamples, int rate, float[] left, float[] right)
    {
        int total = left.Length;
        int s = 0;
        while (s < steps)
        {
            var token = track[s % track.Length];
            if (token is "." or "-") { s++; continue; }
            float freq = Synth.NoteFrequency(token);
            int length = 1;
            while (s + length < steps && track[(s + length) % track.Length] == "-") length++;
            int start = (int)Math.Round(s * stepSamples);
            int held = (int)Math.Round(length * stepSamples);
            int release = (int)(ins.Release * rate);
            double phase = 0;
            float filtered = 0, noise = 0;
            float alpha = ins.LowPass > 0 ? 1f - MathF.Exp(-MathF.Tau * ins.LowPass / rate) : 1f;
            var rng = new Random(s);
            float Sustained(float t) => t < ins.Attack
                ? t / ins.Attack
                : MathF.Max(ins.SustainLevel, 1 - (t - ins.Attack) / MathF.Max(ins.Decay, 1e-4f) * (1 - ins.SustainLevel));
            float atRelease = Sustained(held / (float)rate);
            for (int i = 0; i < held + release; i++)
            {
                float t = i / (float)rate;
                float env = i < held ? Sustained(t) : atRelease * (1 - (i - held) / (float)Math.Max(release, 1));
                float f = freq * (1 + MathF.Sin(t * ins.VibratoRate * MathF.Tau) * ins.VibratoDepth * MathF.Min(1, t * 4));
                double prev = phase;
                phase = (phase + f / rate) % 1.0;
                if (phase < prev) noise = (float)(rng.NextDouble() * 2 - 1);
                float v = Synth.Oscillator(ins.Wave, (float)phase, noise) * env * ins.Volume;
                filtered += (v - filtered) * alpha;
                int index = (start + i) % total;
                left[index] += filtered * (1 - ins.Pan) * 2 * 0.7f;
                right[index] += filtered * ins.Pan * 2 * 0.7f;
            }
            s += length;
        }
    }

    private void RenderDrum(string token, int start, int rate, float[] left, float[] right, Random rng)
    {
        int total = left.Length;
        float duration = token switch { "k" => 0.16f, "s" => 0.14f, "h" => 0.035f, "o" => 0.12f, _ => throw new FormatException($"Unknown drum '{token}'.") };
        int count = (int)(duration * rate);
        double phase = 0;
        float lp = 0;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float env = MathF.Pow(1 - t / duration, 2);
            float v;
            switch (token)
            {
                case "k":
                    float f = 40 + 110 * MathF.Exp(-t * 40);
                    phase = (phase + f / rate) % 1.0;
                    v = MathF.Sin((float)phase * MathF.Tau) * env * 1.2f;
                    break;
                case "s":
                    phase = (phase + 185.0 / rate) % 1.0;
                    v = ((float)(rng.NextDouble() * 2 - 1) * 0.7f + MathF.Sin((float)phase * MathF.Tau) * 0.4f) * env * 0.7f;
                    break;
                default:
                    float n = (float)(rng.NextDouble() * 2 - 1);
                    lp += (n - lp) * 0.3f;
                    v = (n - lp) * env * 0.45f;
                    break;
            }
            v *= DrumVolume;
            int index = (start + i) % total;
            left[index] += v;
            right[index] += v;
        }
    }
}
