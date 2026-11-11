// The sound of "Assembly", made by the engine it presents: the score is a MicroEngine Song (the step sequencer games
// use for their music) and every effect is a Synth sound.
//
// Usage: dotnet run --project audio -c Release -- <output-directory>
//
// 150 BPM, two steps per beat: a step is 0.2 s (6 frames at 30 fps), a beat 0.4 s and a bar 1.6 s. Bar N starts at
// 1.6 * N seconds. The score follows the table: single notes while pieces arrive, a run of eighth notes while they
// dock, the full band while a configuration runs, and a break each time it is taken apart.
using System.Text;
using MicroEngine.Audio;
using MicroEngine.Audio.Retro;

string output = args.Length > 0 ? args[0] : "out";
Directory.CreateDirectory(output);

Wav.Write(Path.Combine(output, "score.wav"), Score.Build().Render());
foreach (var (name, clip) in Effects.All()) Wav.Write(Path.Combine(output, name + ".wav"), clip);
Console.WriteLine($"Wrote the score and {Effects.All().Count()} effects to {Path.GetFullPath(output)}");

internal static class Score
{
    private const int Bars = 34;
    private const int StepsPerBar = 8;

    private static readonly Instrument Bell = new(Wave.Sine, 0.22f, 0.002f, 0.4f, 0.1f, 0.35f, 0.6f, 0.002f, 6);
    private static readonly Instrument Pad = new(Wave.Saw, 0.07f, 0.15f, 0.4f, 0.7f, 0.3f, 0.42f, 0.003f, 3.5f, 1300);
    private static readonly Instrument PadHigh = new(Wave.Triangle, 0.11f, 0.15f, 0.4f, 0.7f, 0.3f, 0.6f, 0.003f, 4f, 2400);
    private static readonly Instrument Bass = new(Wave.Triangle, 0.42f, 0.004f, 0.07f, 0.75f, 0.04f, 0.5f);
    private static readonly Instrument Pluck = new(Wave.Square, 0.1f, 0.002f, 0.1f, 0.15f, 0.05f, 0.62f, 0, 5, 3200);
    private static readonly Instrument Lead = new(Wave.Pulse25, 0.16f, 0.008f, 0.1f, 0.62f, 0.08f, 0.46f, 0.005f, 5.5f, 4200);

    private static readonly Dictionary<string, int[]> Chords = new()
    {
        ["Em"] = [4, 7, 11], ["C"] = [0, 4, 7], ["G"] = [7, 11, 14], ["D"] = [2, 6, 9], ["Am"] = [9, 12, 16],
    };

    private static readonly string[] Names = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
    private static string Note(int midi) => Names[midi % 12] + (midi / 12 - 1);

    // One chord per bar. 0-1: the kernel. 2-5: pieces arrive. 6: they dock. 7-11: the first configuration runs.
    // 12: taken apart. 13-14: pieces change. 15: dock. 16-20: the second runs. 21: apart. 22-23: pieces of your own.
    // 24: dock. 25-27: yours. 28: apart. 29: the mark's parts dock. 30-33: the mark.
    private static readonly string[] Harmony =
    [
        "Em", "Em",
        "Em", "C", "G", "D",
        "D",
        "Em", "C", "G", "D", "Em",
        "C",
        "G", "D",
        "D",
        "G", "D", "Em", "C", "G",
        "D",
        "Am", "C",
        "D",
        "C", "D", "Em",
        "D",
        "D",
        "G", "G", "G", "G",
    ];

    private const string Rest = ". . . . . . . .";

    private static string Line(Func<int, string?> bar)
    {
        var sb = new StringBuilder();
        for (int b = 0; b < Bars; b++)
        {
            var tokens = (bar(b) ?? Rest).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != StepsPerBar) throw new FormatException($"Bar {b} has {tokens.Length} tokens; write {StepsPerBar}.");
            sb.AppendJoin(' ', tokens).Append(' ');
        }
        return sb.ToString();
    }

    private static string Root(int bar, int octave) => Note((octave + 1) * 12 + Chords[Harmony[bar]][0]);
    private static string Tone(int bar, int octave, int index) => Note((octave + 1) * 12 + Chords[Harmony[bar]][index]);
    private static string Held(string note) => note + " - - - - - - -";
    private static string Arpeggio(int bar, int octave, string shape)
        => string.Join(' ', shape.Split(' ').Select(s => s == "." ? "." : Tone(bar, octave, int.Parse(s))));
    private static string BassBar(int bar, string pattern)
        => string.Join(' ', pattern.Split(' ').Select(t => t switch { "R" => Root(bar, 1), "O" => Root(bar, 2), _ => t }));

    public static Song Build()
    {
        static bool Runs(int b) => b is (>= 7 and <= 11) or (>= 16 and <= 20);
        static bool Docks(int b) => b is 6 or 15 or 24 or 29;
        static bool Arrives(int b) => b is (>= 2 and <= 5) or 13 or 14 or 22 or 23;
        static bool Apart(int b) => b is 12 or 21 or 28;
        static bool Yours(int b) => b is >= 25 and <= 27;

        var lead = new Dictionary<int, string>
        {
            // The first configuration: a line in E minor.
            [7] = "E5 - G5 B5 - - A5 G5", [8] = "E5 - - G5 C6 - B5 -", [9] = "D6 - B5 - G5 B5 D6 -", [10] = "A5 - - F#5 A5 - D6 -",
            [11] = "E6 - B5 - G5 - E5 -",
            // The second: another line, in G: a different game, not the first one changed.
            [16] = "G5 B5 D6 - B5 - G5 -", [17] = "A5 - F#5 A5 D6 - - -", [18] = "B5 - G5 - E5 G5 B5 -", [19] = "C6 - - E6 G6 - E6 -",
            [20] = "D6 - B5 - G5 - - -",
        };
        var bell = new Dictionary<int, string>
        {
            [0] = "E5 . . . . . . .", [1] = ". . . . B4 . . .",
            // Yours: slow, open, nothing decided yet.
            [25] = "E5 . G5 . C6 . . .", [26] = "F#5 . A5 . D6 . . .", [27] = "G5 . B5 . E6 . . .",
            [30] = "G5 B5 D6 G6 B6 - - -",
        };

        return new Song(150, 2) { Echo = 0.2f, EchoDelayBeats = 0.75f, DrumVolume = 0.55f }
            .Track(Bell, Line(b => bell.GetValueOrDefault(b)))
            .Track(Pad, Line(b => Runs(b) || Yours(b) || b is 30 or 31 ? Held(Root(b, 3)) : null))
            .Track(PadHigh, Line(b => Runs(b) || Yours(b) || b is 30 or 31 ? Held(Tone(b, 4, 1)) : null))
            .Track(Lead, Line(b => lead.GetValueOrDefault(b)))
            // A piece arrives every two beats: one plucked note each, climbing the chord.
            .Track(Pluck, Line(b =>
                Arrives(b) ? Arpeggio(b, 4, "0 . . . 2 . . .")
                // Docking: one note for each piece that lands, an eighth apart, from the third step of the bar.
                : Docks(b) ? ". . " + Arpeggio(b, 4, "0 1 2 0 1 2")
                : Runs(b) ? Arpeggio(b, 4, "0 1 2 1 0 1 2 1")
                : null))
            .Track(Bass, Line(b =>
                Runs(b) ? BassBar(b, "R . O R . R O .")
                : Arrives(b) ? BassBar(b, "R . . . . . . .")
                : Yours(b) ? Held(Root(b, 2))
                : b == 30 ? Held(Root(b, 1))
                : null))
            .Drums(Line(b =>
                b is 0 or 1 ? ". . . . k . . ."
                : Arrives(b) ? "k . . . k . . ."
                : Docks(b) ? "k . s s s s s s"
                : Runs(b) ? (b is 11 or 20 ? "k . s . k k s s" : "k . s . k k s .")
                : Apart(b) ? "k . . . . . . ."
                : b == 30 ? "k . . . . . . ."
                : null))
            .Drums(Line(b =>
                Runs(b) ? "h h h h h h h o"
                : Arrives(b) ? ". . h . . . h ."
                : null));
    }
}

internal static class Effects
{
    private static SfxParams Tone(Wave wave, float from, float? to, float sustain, float decay, float volume = 0.35f, float lowPass = 0)
        => new() { Wave = wave, Frequency = from, EndFrequency = to, Sustain = sustain, Decay = decay, Volume = volume, LowPass = lowPass };

    private static AudioClip R(SfxParams p) => Synth.Render(p);

    /// <summary>A piece docking: a low body, a short metallic contact and a puff of noise.</summary>
    private static AudioClip Dock(float pitch) => Synth.Layer(
        (R(Tone(Wave.Triangle, 150 * pitch, 70 * pitch, 0.02f, 0.12f, 0.6f)), 0),
        (R(Tone(Wave.Square, 1400 * pitch, 1300 * pitch, 0.008f, 0.09f, 0.12f, 6000)), 0),
        (R(Tone(Wave.Noise, 500, 160, 0.008f, 0.06f, 0.3f, 1400)), 0));

    public static IEnumerable<(string Name, AudioClip Clip)> All()
    {
        yield return ("dock-a", Dock(1f));
        yield return ("dock-b", Dock(1.12f));
        yield return ("dock-c", Dock(0.9f));
        // The kernel set down on the table.
        yield return ("land", Synth.Layer(
            (R(Tone(Wave.Triangle, 120, 45, 0.06f, 0.4f, 0.7f)), 0),
            (R(Tone(Wave.Noise, 300, 80, 0.03f, 0.2f, 0.4f, 1000)), 0)));
        // A configuration coming apart: every piece springs back along its axis at once.
        yield return ("apart", Synth.Layer(
            (R(Tone(Wave.Triangle, 170, 460, 0.05f, 0.18f, 0.42f)), 0),
            (R(new SfxParams { Wave = Wave.Noise, Frequency = 600, EndFrequency = 2600, Attack = 0.02f, Sustain = 0.08f, Decay = 0.2f, Volume = 0.2f, LowPass = 3000 }), 0)));
        // What a configuration is for, appearing above it.
        yield return ("emerge", R(new SfxParams { Wave = Wave.Square, Frequency = 660, Arpeggio = [1f, 1.5f, 2f], ArpeggioStep = 0.05f, Sustain = 0.08f, Decay = 0.16f, Volume = 0.15f, LowPass = 4200 }));
        // Rising noise into the mark.
        yield return ("riser", R(new SfxParams { Wave = Wave.Noise, Frequency = 300, EndFrequency = 5200, Attack = 1.35f, Sustain = 0.2f, Decay = 0.05f, Volume = 0.3f, LowPass = 5000 }));
        // The mark's module docking: the one heavy hit of the film.
        yield return ("impact", Synth.Layer(
            (R(Tone(Wave.Triangle, 110, 34, 0.12f, 0.9f, 0.8f)), 0),
            (R(Tone(Wave.Noise, 300, 40, 0.06f, 0.5f, 0.7f, 1000) with { Crush = 6 }), 0),
            (R(Tone(Wave.Square, 1400, 1300, 0.01f, 0.14f, 0.14f, 6000)), 0)));
    }
}

internal static class Wav
{
    /// <summary>Writes a clip as 16-bit PCM.</summary>
    public static void Write(string path, AudioClip clip)
    {
        using var file = new BinaryWriter(File.Create(path));
        int bytes = clip.Samples.Length * 2;
        file.Write("RIFF"u8); file.Write(36 + bytes); file.Write("WAVE"u8);
        file.Write("fmt "u8); file.Write(16); file.Write((short)1); file.Write((short)clip.Channels);
        file.Write(clip.SampleRate); file.Write(clip.SampleRate * clip.Channels * 2); file.Write((short)(clip.Channels * 2)); file.Write((short)16);
        file.Write("data"u8); file.Write(bytes);
        foreach (float sample in clip.Samples) file.Write((short)Math.Round(Math.Clamp(sample, -1f, 1f) * 32767));
    }
}
