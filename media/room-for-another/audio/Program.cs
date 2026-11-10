// The sound of "Room for another", made by the engine it presents: the score is a MicroEngine Song (the step sequencer
// games use for their music) and every effect is a Synth sound.
//
// Usage: dotnet run --project audio -c Release -- <output-directory>
//
// The film runs at 120 BPM: one beat is 0.5 s (15 frames at 30 fps) and one bar is 2 s, so bar N starts at 2 * N seconds.
// The score is assembled the way the picture is: each module that seats in the first bars brings a layer with it.
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
    private const int Bars = 28;
    private const int StepsPerBar = 16;

    private static readonly Instrument Bell = new(Wave.Sine, 0.2f, 0.002f, 0.5f, 0.1f, 0.4f, 0.6f, 0.002f, 6);
    private static readonly Instrument Pad = new(Wave.Saw, 0.075f, 0.18f, 0.5f, 0.7f, 0.4f, 0.42f, 0.003f, 3.5f, 1300);
    private static readonly Instrument PadHigh = new(Wave.Triangle, 0.12f, 0.2f, 0.5f, 0.7f, 0.4f, 0.6f, 0.003f, 4f, 2400);
    private static readonly Instrument Bass = new(Wave.Triangle, 0.42f, 0.004f, 0.08f, 0.75f, 0.05f, 0.5f);
    private static readonly Instrument Arp = new(Wave.Square, 0.06f, 0.002f, 0.09f, 0.2f, 0.05f, 0.66f, 0, 5, 2800);
    private static readonly Instrument Lead = new(Wave.Pulse25, 0.17f, 0.008f, 0.12f, 0.62f, 0.1f, 0.46f, 0.005f, 5.5f, 4200);
    private static readonly Instrument Soar = new(Wave.Triangle, 0.3f, 0.03f, 0.25f, 0.75f, 0.2f, 0.5f, 0.006f, 4.5f, 3200);

    private static readonly Dictionary<string, int[]> Chords = new()
    {
        ["C"] = [0, 4, 7], ["F"] = [5, 9, 12], ["G"] = [7, 11, 14], ["Am"] = [9, 12, 16],
    };

    private static readonly string[] Names = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
    private static string Note(int midi) => Names[midi % 12] + (midi / 12 - 1);

    // One chord per bar. Bars 0-3: the first modules seat. 4-7: the game runs. 8-9, 10-11, 12-13, 14-15: four
    // configurations. 16-19: the board extends. 20-22: the claim. 23: the rise. 24-27: the mark.
    private static readonly string[] Harmony =
    [
        "Am", "Am", "F", "G",
        "Am", "F", "C", "G",
        "Am", "F", "C", "G", "Am", "F", "F", "G",
        "Am", "F", "C", "G",
        "F", "G", "Am",
        "G",
        "C", "C", "C", "C",
    ];

    /// <summary>One bar per entry; a null bar is silent. A bar is 8 tokens (eighth notes, doubled to steps) or 16.</summary>
    private static string Line(Func<int, string?> bar)
    {
        var sb = new StringBuilder();
        for (int b = 0; b < Bars; b++)
        {
            var tokens = (bar(b) ?? ". . . . . . . .").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == StepsPerBar) sb.AppendJoin(' ', tokens).Append(' ');
            else if (tokens.Length == 8) foreach (var t in tokens) sb.Append(t).Append(t == "." ? " . " : " - ");
            else throw new FormatException($"Bar {b} has {tokens.Length} tokens; write 8 or 16.");
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
        // Bars where everything plays.
        static bool Full(int b) => b is (>= 4 and <= 9) or 12 or 13 or (>= 16 and <= 22);
        const string Sixteenths = "0 1 2 1 0 1 2 1 0 1 2 1 0 1 2 1";

        var lead = new Dictionary<int, string>
        {
            [4] = "E5 - A5 - G5 E5 - C5", [5] = "A5 - - G5 F5 - C5 -", [6] = "G5 - E5 - C5 E5 G5 -", [7] = "D5 - - B4 D5 - G5 -",
            // Each configuration answers after its module has seated.
            [8] = ". . . . E5 - A5 -", [9] = "C6 - - A5 F5 - A5 -",
            [12] = ". . . . A5 - C6 -", [13] = "A5 - G5 F5 - - C5 -",
            [16] = "E5 - A5 - C6 B5 - A5", [17] = "C6 - - A5 F5 - A5 -", [18] = "G5 - C6 - E6 - D6 C6", [19] = "D6 - - B5 D6 - G6 -",
        };
        var soar = new Dictionary<int, string>
        {
            [20] = "A5 - - - C6 - - -", [21] = "B5 - - - D6 - - -", [22] = "E6 - - - C6 - A5 -", [23] = "B5 - - - D6 - - -",
            [24] = "C6 - - - - - - -",
        };
        var bell = new Dictionary<int, string>
        {
            [0] = "A4 . . . . . . .", [1] = ". . E5 . . . . .",
            // The module that does not exist yet: no drums, a slow broken chord.
            [14] = "F5 . C6 . A5 . F6 .", [15] = "G5 . D6 . B5 . G6 .",
            [24] = "C5 E5 G5 C6 E6 G6 C7 -",
        };

        return new Song(120, 4) { Echo = 0.22f, EchoDelayBeats = 0.75f, DrumVolume = 0.55f }
            .Track(Bell, Line(b => bell.GetValueOrDefault(b)))
            .Track(Pad, Line(b => b is >= 2 and <= 25 ? Held(Root(b, 3)) : null))
            .Track(PadHigh, Line(b => b is >= 2 and <= 25 ? Held(Tone(b, 4, 1)) : null))
            .Track(Lead, Line(b => lead.GetValueOrDefault(b)))
            .Track(Soar, Line(b => soar.GetValueOrDefault(b)))
            .Track(Bass, Line(b =>
                // The bass arrives with the third module, on the third beat of bar 2.
                b == 2 ? ". . . . " + BassBar(b, "R . O .")
                : b == 3 ? BassBar(b, "R . O R . R O .")
                : b is 10 or 11 ? BassBar(b, "R . . . O . R .")
                : b is 14 or 15 ? Held(Root(b, 2))
                : b == 23 ? BassBar(b, "R R R R R R R R")
                : b == 24 ? Held(Root(b, 1))
                : Full(b) ? BassBar(b, b >= 20 ? "R . O R . R O R" : "R . O R . R O .")
                : null))
            .Track(Arp, Line(b =>
                b == 3 ? Arpeggio(b, 4, "0 . 1 . 2 . 1 .")
                : b is 19 or 23 ? Arpeggio(b, 5, Sixteenths)
                : Full(b) ? Arpeggio(b, 4, Sixteenths)
                : null))
            .Drums(Line(b =>
                // The kick arrives with the first module, on the third beat of bar 0.
                b == 0 ? ". . . . k . k ."
                : b is 1 or 2 or 11 ? "k . k . k . k ."
                : b is 3 or 19 or 23 ? "k . . . k . . . s . s . s s s s"
                : b == 10 ? "k . . . k . . ."
                : b is 8 or 12 ? "k . . . . . . . k . k . s . . ."
                : b is 7 or 22 ? "k . . . s . . . k . k . s . s s"
                : b == 24 ? "k . . . . . . ."
                : Full(b) ? "k . . . s . . . k . k . s . . ."
                : null))
            .Drums(Line(b =>
                // The hats arrive with the second module, on the third beat of bar 1.
                b == 1 ? ". . . . . . . . h . h h h . o ."
                : b is 2 or 3 ? "h . h h h . h h h . h h h . o ."
                : b is 10 or 11 ? ". . h . . . h . . . h . . . h ."
                : Full(b) ? "h . h h h . h h h . h h h . o ."
                : null));
    }
}

internal static class Effects
{
    private static SfxParams Tone(Wave wave, float from, float? to, float sustain, float decay, float volume = 0.35f, float lowPass = 0)
        => new() { Wave = wave, Frequency = from, EndFrequency = to, Sustain = sustain, Decay = decay, Volume = volume, LowPass = lowPass };

    private static AudioClip R(SfxParams p) => Synth.Render(p);

    /// <summary>A module seating in its socket: a low body, a short metallic contact and a puff of noise.</summary>
    private static AudioClip Seat(float pitch) => Synth.Layer(
        (R(Tone(Wave.Triangle, 150 * pitch, 70 * pitch, 0.02f, 0.12f, 0.6f)), 0),
        (R(Tone(Wave.Square, 1400 * pitch, 1300 * pitch, 0.008f, 0.09f, 0.12f, 6000)), 0),
        (R(Tone(Wave.Noise, 500, 160, 0.008f, 0.06f, 0.3f, 1400)), 0));

    public static IEnumerable<(string Name, AudioClip Clip)> All()
    {
        yield return ("seat-a", Seat(1f));
        yield return ("seat-b", Seat(1.12f));
        yield return ("seat-c", Seat(0.9f));
        // A line of test output arriving on the screen, in two takes.
        yield return ("tick-a", R(Tone(Wave.Square, 1760, null, 0.008f, 0.02f, 0.14f, 5000)));
        yield return ("tick-b", R(Tone(Wave.Square, 1568, null, 0.008f, 0.02f, 0.14f, 5000)));
        // A screen coming on.
        yield return ("screen-on", R(new SfxParams { Wave = Wave.Square, Frequency = 660, Arpeggio = [1f, 1.5f], ArpeggioStep = 0.05f, Sustain = 0.05f, Decay = 0.12f, Volume = 0.14f, LowPass = 4200 }));
        // The module that does not exist yet drifting toward its socket: a soft swell, no contact.
        yield return ("glide", R(new SfxParams { Wave = Wave.Sine, Frequency = 196, EndFrequency = 294, Attack = 0.5f, Sustain = 0.3f, Decay = 0.5f, Volume = 0.3f }));
        // The board growing.
        yield return ("grow", Synth.Layer(
            (R(new SfxParams { Wave = Wave.Noise, Frequency = 240, EndFrequency = 2400, Attack = 0.5f, Sustain = 0.2f, Decay = 0.3f, Volume = 0.26f, LowPass = 2600 }), 0),
            (R(Tone(Wave.Triangle, 70, 180, 0.6f, 0.3f, 0.4f)), 0)));
        // Rising noise into a cut.
        yield return ("riser", R(new SfxParams { Wave = Wave.Noise, Frequency = 300, EndFrequency = 5200, Attack = 1.5f, Sustain = 0.2f, Decay = 0.05f, Volume = 0.3f, LowPass = 5000 }));
        yield return ("riser-long", R(new SfxParams { Wave = Wave.Noise, Frequency = 220, EndFrequency = 6000, Attack = 1.75f, Sustain = 0.2f, Decay = 0.05f, Volume = 0.34f, LowPass = 5500 }));
        // The module of the mark landing: the one heavy hit of the film.
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
