using MicroEngine.Audio;
using MicroEngine.Audio.Retro;

namespace CourtyardKey.Audio;

/// <summary>
/// The five looping themes. All share the "key motif" (a rising root-third-fifth-octave, then a
/// falling step), so the title, the vale and the ending feel like one story.
/// Melodies are written in eighth notes (two steps per beat), bars separated by "|".
/// </summary>
public static class MusicBank
{
    public static void Register(AudioMixer audio)
    {
        audio.AddMusic("title", Title());
        audio.AddMusic("overworld", Overworld());
        audio.AddMusic("dungeon", Dungeon());
        audio.AddMusic("boss", Boss());
        audio.AddMusic("ending", Ending());
    }

    private static readonly Dictionary<string, string[]> Chords = new()
    {
        ["C"] = ["C", "E", "G"], ["D"] = ["D", "F#", "A"], ["E"] = ["E", "G#", "B"], ["F"] = ["F", "A", "C"],
        ["G"] = ["G", "B", "D"], ["A"] = ["A", "C#", "E"], ["B"] = ["B", "D#", "F#"],
        ["Am"] = ["A", "C", "E"], ["Bm"] = ["B", "D", "F#"], ["Dm"] = ["D", "F", "A"], ["Em"] = ["E", "G", "B"],
        ["F#m"] = ["F#", "A", "C#"],
    };

    /// <summary>Root-fifth bass, one chord per half bar (4 eighth steps each).</summary>
    private static string Bass(string pattern, int octave, params string[] chords)
    {
        var parts = new List<string>();
        foreach (var name in chords)
        {
            var c = Chords[name];
            string root = c[0] + octave, fifth = c[2] + (Fifth(c[0], c[2]) ? octave : octave + 1);
            parts.Add(string.Join(" ", pattern.Split(' ').Select(t => t == "R" ? root : t == "F" ? fifth : t)));
        }
        return string.Join(" | ", parts);
    }

    /// <summary>True when the fifth's letter is above the root's within the same octave (C..B ordering).</summary>
    private static bool Fifth(string root, string fifth) => "CDEFGAB".IndexOf(fifth[0]) > "CDEFGAB".IndexOf(root[0]);

    /// <summary>Broken-chord arpeggio over each half-bar chord.</summary>
    private static string Arp(int octave, params string[] chords)
    {
        var parts = new List<string>();
        foreach (var name in chords)
        {
            var c = Chords[name];
            string N(int i) => c[i] + (Fifth(c[0], c[i]) || i == 0 ? octave : octave + 1);
            parts.Add($"{N(0)} {N(1)} {N(2)} {N(1)}");
        }
        return string.Join(" ", parts);
    }

    private static Instrument Lead(float volume = 0.2f) => new(Wave.Pulse25, volume, 0.008f, 0.12f, 0.55f, 0.1f, 0.45f, 0.005f, 5.5f, 4200);
    private static Instrument Soft(float volume = 0.12f) => new(Wave.Triangle, volume, 0.01f, 0.2f, 0.5f, 0.15f, 0.6f, 0.003f, 4f);
    private static Instrument Arpeggio(float volume = 0.08f) => new(Wave.Pulse12, volume, 0.002f, 0.08f, 0.2f, 0.05f, 0.62f, 0, 5, 3200);

    private static Song Title()
    {
        var song = new Song(84, 2) { Echo = 0.3f, EchoDelayBeats = 1f, DrumVolume = 0.25f };
        song.Track(new Instrument(Wave.Sine, 0.3f, 0.004f, 0.5f, 0.2f, 0.4f, 0.45f, 0.004f, 5f),
            "G4 - B4 - D5 - G5 - | F#5 - - - D5 - - - | E5 - - - C5 - E5 - | D5 - - - - - - - |",
            "G4 - B4 - D5 - G5 - | A5 - - - B5 - A5 - | G5 - F#5 - E5 - F#5 - | G5 - - - - - - - |");
        song.Track(Instrument.Pad with { Volume = 0.07f },
            "B3 - - - - - - - | A3 - - - - - - - | G3 - - - - - - - | F#3 - - - - - - - |",
            "B3 - - - - - - - | A3 - - - - - - - | C4 - - - D4 - - - | B3 - - - - - - - |");
        song.Track(Soft(0.2f), Bass("R - - - F - - -", 2, "G", "D", "C", "D", "G", "D", "C", "G"));
        song.Track(Arpeggio(0.05f), Arp(4, "G", "G", "D", "D", "C", "C", "D", "D", "G", "G", "D", "D", "C", "D", "G", "G"));
        song.Drums(". . . . . . h . | . . . . . . h . | . . . . . . h . | . . . . . . h o");
        return song;
    }

    private static Song Overworld()
    {
        var song = new Song(118, 2) { Echo = 0.16f, DrumVolume = 0.42f };
        song.Track(Lead(),
            "D5 - - A4 D5 E5 F#5 G5 | A5 - - - F#5 - D5 - | G5 - F#5 E5 D5 - E5 F#5 | E5 - - - - - . . |",
            "D5 - - A4 D5 E5 F#5 G5 | A5 - - - B5 - A5 - | G5 F#5 E5 - D5 C#5 E5 - | D5 - - - - - . . |",
            "B4 - D5 - G5 - F#5 E5 | F#5 - - - D5 - A4 - | G4 - B4 - E5 - D5 C#5 | D5 - - - A4 - - - |",
            "B4 - D5 - G5 - A5 B5 | A5 - - - F#5 - D5 - | E5 - G5 F#5 E5 - C#5 - | D5 - - - - - . . |");
        song.Track(Soft(0.11f),
            "F#4 - - - A4 - - - | F#4 - - - A4 - - - | B4 - - - B4 - - - | C#5 - - - A4 - - - |",
            "F#4 - - - A4 - - - | F#4 - - - D5 - - - | B4 - - - A4 - - - | F#4 - - - - - - - |",
            "G4 - - - B4 - - - | A4 - - - F#4 - - - | E4 - - - G4 - - - | F#4 - - - - - - - |",
            "G4 - - - B4 - - - | A4 - - - F#4 - - - | G4 - - - E4 - - - | F#4 - - - - - - - |");
        song.Track(Instrument.Bass with { Volume = 0.36f }, Bass("R . F . R . F R", 2,
            "D", "D", "G", "A", "D", "D", "G", "D", "G", "D", "Em", "D", "G", "D", "A", "D"));
        song.Drums("k . h . s . h h | k . h k s . h . | k . h . s . h h | k . h k s . o .");
        return song;
    }

    private static Song Dungeon()
    {
        var song = new Song(92, 2) { Echo = 0.32f, EchoDelayBeats = 1.5f, DrumVolume = 0.22f };
        song.Track(new Instrument(Wave.Sine, 0.26f, 0.004f, 0.35f, 0.25f, 0.3f, 0.4f, 0.006f, 4.5f),
            "A4 - - - C5 - B4 - | A4 - E4 - - - - - | F4 - - - A4 - G4 - | E4 - - - - - - - |",
            "A4 - - - C5 - D5 - | E5 - - - D5 C5 B4 - | C5 - B4 - G#4 - B4 - | A4 - - - - - - - |");
        song.Track(Instrument.Pad with { Volume = 0.06f },
            "E4 - - - - - - - | C4 - - - - - - - | A3 - - - - - - - | G#3 - - - - - - - |",
            "E4 - - - - - - - | G4 - - - - - - - | F4 - - - E4 - - - | E4 - - - - - - - |");
        song.Track(Soft(0.24f), Bass("R . . R F . R .", 2, "Am", "Am", "F", "E", "Am", "C", "Dm", "Am"));
        song.Track(Arpeggio(0.04f), Arp(4, "Am", "Am", "Am", "Am", "F", "F", "E", "E", "Am", "Am", "C", "C", "Dm", "E", "Am", "Am"));
        song.Drums(". . h . . . h . | . . h . . h h . | . . h . . . h . | . . h . o . . .");
        return song;
    }

    private static Song Boss()
    {
        var song = new Song(152, 2) { Echo = 0.12f, DrumVolume = 0.55f };
        song.Track(Lead(0.22f),
            "E5 - E5 D5 E5 - G5 - | F#5 - E5 - D5 - B4 - | C5 - C5 B4 C5 - E5 - | D#5 - - - B4 - - - |",
            "E5 - E5 D5 E5 - G5 - | A5 - G5 - F#5 - E5 - | F#5 - G5 - A5 - B5 - | B5 - - - D#5 - - - |");
        song.Track(new Instrument(Wave.Saw, 0.08f, 0.005f, 0.1f, 0.5f, 0.05f, 0.6f, 0, 5, 2000),
            "B4 - B4 - B4 - B4 - | A4 - A4 - G4 - G4 - | G4 - G4 - G4 - G4 - | F#4 - F#4 - F#4 - F#4 - |",
            "B4 - B4 - B4 - B4 - | C5 - C5 - C5 - C5 - | A4 - A4 - B4 - B4 - | D#5 - D#5 - F#4 - F#4 - |");
        song.Track(Instrument.Bass with { Volume = 0.42f }, Bass("R R F R", 2, "Em", "Em", "Em", "Em", "C", "C", "B", "B", "Em", "Em", "Am", "Am", "D", "D", "B", "B"));
        song.Drums("k h s h k k s h | k h s h k k s o");
        return song;
    }

    private static Song Ending()
    {
        var song = new Song(100, 2) { Echo = 0.24f, EchoDelayBeats = 1f, DrumVolume = 0.35f };
        song.Track(Lead(0.18f),
            "C5 - E5 - G5 - C6 - | B5 - - - G5 - - - | A5 - - - F5 - A5 - | G5 - - - - - - - |",
            "C5 - E5 - G5 - C6 - | D6 - - - E6 - D6 - | C6 - B5 - A5 - B5 - | C6 - - - - - - - |");
        song.Track(new Instrument(Wave.Sine, 0.16f, 0.004f, 0.4f, 0.2f, 0.3f, 0.6f),
            "E5 - - - G5 - - - | D5 - - - D5 - - - | C5 - - - C5 - - - | E5 - - - D5 - - - |",
            "E5 - - - G5 - - - | B5 - - - G5 - - - | A5 - - - G5 - F5 - | E5 - - - - - - - |");
        song.Track(Instrument.Bass with { Volume = 0.34f }, Bass("R . F .", 2, "C", "C", "G", "G", "F", "F", "C", "G", "C", "C", "G", "G", "F", "G", "C", "C"));
        song.Track(Arpeggio(0.05f), Arp(4, "C", "C", "G", "G", "F", "F", "C", "G", "C", "C", "G", "G", "F", "G", "C", "C"));
        song.Drums("k . h . s . h . | k . h . s . h h");
        return song;
    }
}
