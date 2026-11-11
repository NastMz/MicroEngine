// The sound of "Every step", made by the engine it presents: the score is a MicroEngine Song (the step sequencer games
// use for their music) and every effect is a Synth sound.
//
// Usage: dotnet run --project audio -c Release -- <audio-directory> <score-json>
//
// The film is the score seen: the picture is a tape of steps and each cell that lights is a note of this song. So
// besides the WAV files this program writes the steps of every voice as JSON, and the film draws from that file.
//
// 100 BPM, two steps per beat: a step is 0.3 s (9 frames at 30 fps), a beat 0.6 s and a bar 2.4 s. Bar N starts at
// 2.4 * N seconds.
using System.Text;
using System.Text.Json;
using MicroEngine.Audio;
using MicroEngine.Audio.Retro;

string output = args.Length > 0 ? args[0] : "out";
string json = args.Length > 1 ? args[1] : Path.Combine(output, "score.json");
Directory.CreateDirectory(output);

var (song, voices) = Score.Build();
Wav.Write(Path.Combine(output, "score.wav"), song.Render());
foreach (var (name, clip) in Effects.All()) Wav.Write(Path.Combine(output, name + ".wav"), clip);
File.WriteAllText(json, JsonSerializer.Serialize(new { bpm = Score.Bpm, stepsPerBeat = 2, stepsPerBar = Score.StepsPerBar, bars = Score.Bars, voices },
    new JsonSerializerOptions { WriteIndented = false }));
Console.WriteLine($"Wrote the score, {Effects.All().Count()} effects and {voices.Count} voices of steps.");

/// <summary>One voice as the film draws it: per step, 0 for silence, 1 for a note starting, 2 for a note held.</summary>
internal sealed record Voice(string Name, int[] Steps);

internal static class Score
{
    public const int Bpm = 100;
    public const int Bars = 23;
    public const int StepsPerBar = 8;

    private static readonly Instrument Bell = new(Wave.Sine, 0.22f, 0.002f, 0.6f, 0.1f, 0.5f, 0.6f, 0.002f, 6);
    private static readonly Instrument Pad = new(Wave.Saw, 0.07f, 0.25f, 0.6f, 0.7f, 0.5f, 0.42f, 0.003f, 3.5f, 1200);
    private static readonly Instrument PadHigh = new(Wave.Triangle, 0.11f, 0.25f, 0.6f, 0.7f, 0.5f, 0.6f, 0.003f, 4f, 2200);
    private static readonly Instrument Bass = new(Wave.Triangle, 0.42f, 0.004f, 0.1f, 0.75f, 0.06f, 0.5f);
    private static readonly Instrument Arp = new(Wave.Square, 0.065f, 0.002f, 0.12f, 0.2f, 0.06f, 0.66f, 0, 5, 2600);
    private static readonly Instrument Lead = new(Wave.Pulse25, 0.16f, 0.01f, 0.14f, 0.62f, 0.12f, 0.46f, 0.005f, 5f, 4000);

    private static readonly Dictionary<string, int[]> Chords = new()
    {
        ["Dm"] = [2, 5, 9], ["Bb"] = [10, 14, 17], ["F"] = [5, 9, 12], ["C"] = [0, 4, 7],
    };

    private static readonly string[] Names = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
    private static string Note(int midi) => Names[midi % 12] + (midi / 12 - 1);

    // One chord per bar. Bars 0-1: one step. 2-3: the tape. 4-6: inside a step. 7-9: the same steps twice.
    // 10-15: everything that happens in them. 16-18: back to one step. 19-22: the mark.
    private static readonly string[] Harmony =
    [
        "Dm", "Dm", "Bb", "C",
        "Dm", "Bb", "F",
        "Dm", "Bb", "C",
        "Dm", "Bb", "F", "C", "Dm", "Bb",
        "Bb", "C", "C",
        "F", "F", "F", "F",
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

    public static (Song Song, List<Voice> Voices) Build()
    {
        static bool Twice(int b) => b is >= 7 and <= 9;
        static bool Full(int b) => b is >= 10 and <= 15;

        var lead = new Dictionary<int, string>
        {
            [10] = "D5 - F5 A5 - - G5 F5", [11] = "D5 - - F5 A#5 - A5 -", [12] = "C6 - A5 - F5 A5 C6 -", [13] = "G5 - - E5 G5 - C6 -",
            [14] = "D6 - A5 - F5 - A5 D6", [15] = "F6 - D6 - A#5 - D6 -",
        };
        var bell = new Dictionary<int, string>
        {
            // Inside one step: three notes for its three parts, slower first, then once per beat pair.
            [4] = "D5 . F5 . A5 . . .", [5] = "D5 . F5 . A#5 . . .", [6] = "C5 F5 A5 . C5 F5 A5 .",
            [19] = "F5 A5 C6 F6 A6 - - -",
        };

        // Every voice is written once here, and both the song and the picture are made from the same lines.
        var kick = Line(b =>
            b is 0 or 1 ? "k . k . k . k ."
            : b is 2 or 3 ? "k . k . k . k ."
            : b is 4 or 5 or 6 ? null
            : Twice(b) ? "k . . . k . . k"
            : Full(b) ? "k . . k . . k ."
            : b == 16 ? "k . . . k . . ."
            : b is 17 or 18 ? "k . k . k . k ."
            : b == 19 ? "k . . . . . . ."
            : null);
        var snare = Line(b =>
            Twice(b) ? ". . s . . . s ."
            : Full(b) ? (b is 13 or 15 ? ". . s . . . s s" : ". . s . . . s .")
            : b == 16 ? ". . s . . . s ."
            : null);
        var hat = Line(b =>
            b is 2 or 3 ? ". h . h . h . h"
            : Twice(b) ? "h h . h h h . h"
            : Full(b) ? "h h h h h h h o"
            : b == 16 ? ". h . h . h . h"
            : null);
        var bass = Line(b =>
            b == 3 ? BassBar(b, "R . . . R . . .")
            : Twice(b) ? BassBar(b, "R . . O . . R .")
            : Full(b) ? BassBar(b, "R . O R . R O .")
            : b == 16 ? BassBar(b, "R . . . O . . .")
            : b == 19 ? Held(Root(b, 1))
            : null);
        var arp = Line(b =>
            Twice(b) ? Arpeggio(b, 4, "0 . 1 . 2 . 1 .")
            : Full(b) ? Arpeggio(b, 4, b >= 14 ? "0 1 2 1 2 1 0 1" : "0 1 2 1 0 1 2 1")
            : null);
        var pad = Line(b => b is (>= 4 and <= 15) or (>= 19 and <= 20) ? Held(Root(b, 3)) : null);
        var padHigh = Line(b => b is (>= 4 and <= 15) or (>= 19 and <= 20) ? Held(Tone(b, 4, 1)) : null);
        var leadLine = Line(b => lead.GetValueOrDefault(b));
        var bellLine = Line(b => bell.GetValueOrDefault(b));

        var song = new Song(Bpm, 2) { Echo = 0.24f, EchoDelayBeats = 0.75f, DrumVolume = 0.55f }
            .Track(Bell, bellLine).Track(Pad, pad).Track(PadHigh, padHigh).Track(Lead, leadLine)
            .Track(Bass, bass).Track(Arp, arp)
            .Drums(Merge(kick, snare)).Drums(hat);

        var voices = new List<Voice>
        {
            new("kick", Steps(kick)), new("snare", Steps(snare)), new("hat", Steps(hat)), new("bass", Steps(bass)),
            new("arp", Steps(arp)), new("pad", Steps(pad)), new("lead", Steps(leadLine)), new("bell", Steps(bellLine)),
        };
        return (song, voices);
    }

    /// <summary>Two drum lines as one track: a step takes whichever line plays on it.</summary>
    private static string Merge(string a, string b)
    {
        var x = a.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var y = b.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', x.Select((t, i) => t != "." ? t : y[i]));
    }

    private static int[] Steps(string line)
        => line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(t => t == "." ? 0 : t == "-" ? 2 : 1).ToArray();
}

internal static class Effects
{
    private static SfxParams Tone(Wave wave, float from, float? to, float sustain, float decay, float volume = 0.35f, float lowPass = 0)
        => new() { Wave = wave, Frequency = from, EndFrequency = to, Sustain = sustain, Decay = decay, Volume = volume, LowPass = lowPass };

    private static AudioClip R(SfxParams p) => Synth.Render(p);

    public static IEnumerable<(string Name, AudioClip Clip)> All()
    {
        // A step opening into its parts, and closing again.
        yield return ("open", R(Tone(Wave.Triangle, 180, 520, 0.08f, 0.22f, 0.4f)));
        yield return ("close", R(Tone(Wave.Triangle, 520, 180, 0.06f, 0.2f, 0.36f)));
        // The tape splitting in two, and the plane turning into depth.
        yield return ("split", Synth.Layer(
            (R(new SfxParams { Wave = Wave.Noise, Frequency = 2600, EndFrequency = 400, Attack = 0.03f, Sustain = 0.15f, Decay = 0.35f, Volume = 0.24f, LowPass = 3000 }), 0),
            (R(Tone(Wave.Triangle, 240, 120, 0.08f, 0.3f, 0.35f)), 0)));
        yield return ("turn", Synth.Layer(
            (R(new SfxParams { Wave = Wave.Noise, Frequency = 240, EndFrequency = 3200, Attack = 0.7f, Sustain = 0.2f, Decay = 0.3f, Volume = 0.26f, LowPass = 3400 }), 0),
            (R(Tone(Wave.Triangle, 70, 200, 0.7f, 0.4f, 0.4f)), 0)));
        // Rising noise into the mark.
        yield return ("riser", R(new SfxParams { Wave = Wave.Noise, Frequency = 220, EndFrequency = 6000, Attack = 2.1f, Sustain = 0.2f, Decay = 0.05f, Volume = 0.32f, LowPass = 5500 }));
        // The board closing over the step: the one heavy hit of the film.
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
