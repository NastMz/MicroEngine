using System.Text;
using MicroEngine.Audio;
using MicroEngine.Audio.Retro;

namespace MeadowRun.Audio;

/// <summary>
/// The soundtrack. Melodies are written by hand in eighth notes (two steps per beat); bass lines and
/// arpeggios are generated from chord symbols so harmony stays consistent with the melody.
/// </summary>
internal static class MusicBank
{
    private static readonly Instrument Lead = new(Wave.Pulse25, 0.2f, 0.006f, 0.09f, 0.62f, 0.07f, 0.44f, 0.005f, 5.5f, 4600);
    private static readonly Instrument SoftLead = new(Wave.Triangle, 0.34f, 0.02f, 0.2f, 0.7f, 0.15f, 0.48f, 0.006f, 4.5f, 3000);
    private static readonly Instrument Bass = new(Wave.Triangle, 0.4f, 0.004f, 0.06f, 0.75f, 0.04f, 0.5f);
    private static readonly Instrument Arp = new(Wave.Square, 0.075f, 0.002f, 0.08f, 0.2f, 0.05f, 0.62f, 0, 5, 2600);
    private static readonly Instrument Bell = new(Wave.Sine, 0.22f, 0.002f, 0.35f, 0.12f, 0.3f, 0.58f, 0.002f, 6);
    private static readonly Instrument Pad = new(Wave.Saw, 0.07f, 0.12f, 0.4f, 0.65f, 0.3f, 0.4f, 0.003f, 3.5f, 1400);
    private static readonly Instrument Grit = new(Wave.Square, 0.16f, 0.003f, 0.05f, 0.5f, 0.04f, 0.4f, 0, 5, 2200);

    public static IEnumerable<(string Name, Song Song, bool Loop)> All()
    {
        yield return ("title", Title(), true);
        yield return ("map", Map(), true);
        yield return ("meadow", Meadow(), true);
        yield return ("caves", Caves(), true);
        yield return ("sky", Sky(), true);
        yield return ("castle", Castle(), true);
        yield return ("boss", Boss(), true);
        yield return ("clear", Clear(), false);
        yield return ("victory", Victory(), false);
        yield return ("gameover", GameOver(), false);
        yield return ("credits", Credits(), true);
    }

    private static readonly Dictionary<string, int[]> Chords = new()
    {
        ["C"] = [0, 4, 7], ["Dm"] = [2, 5, 9], ["Em"] = [4, 7, 11], ["F"] = [5, 9, 12], ["G"] = [7, 11, 14],
        ["Am"] = [9, 12, 16], ["E"] = [4, 8, 11], ["Bb"] = [10, 14, 17], ["A"] = [9, 13, 16], ["D"] = [2, 6, 9],
        ["B"] = [11, 15, 18], ["Gm"] = [7, 10, 14],
    };

    private static readonly string[] Names = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
    private static string Note(int midi) => Names[midi % 12] + (midi / 12 - 1);

    /// <summary>One bar (8 eighths) per chord: a broken-chord arpeggio starting at <paramref name="octave"/>.</summary>
    private static string ArpLine(string chords, int octave, string shape = "0 1 2 1 0 1 2 1")
    {
        var sb = new StringBuilder();
        int[] order = shape.Split(' ').Select(int.Parse).ToArray();
        foreach (var chord in chords.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var tones = Chords[chord];
            foreach (int i in order) sb.Append(Note((octave + 1) * 12 + tones[i])).Append(' ');
            sb.Append("| ");
        }
        return sb.ToString();
    }

    /// <summary>Bass bar per chord from a pattern of R (root), O (octave), F (fifth), '-' hold, '.' rest.</summary>
    private static string BassLine(string chords, int octave, string pattern = "R . O . R R O .")
    {
        var sb = new StringBuilder();
        foreach (var chord in chords.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            int root = (octave + 1) * 12 + Chords[chord][0];
            foreach (var t in pattern.Split(' '))
                sb.Append(t switch { "R" => Note(root), "O" => Note(root + 12), "F" => Note(root + 7), _ => t }).Append(' ');
            sb.Append("| ");
        }
        return sb.ToString();
    }

    private static string Repeat(string s, int times) => string.Join(" ", Enumerable.Repeat(s, times));

    private const string MeadowA1 = "E5 G5 C6 - B5 G5 A5 - | G5 - D5 E5 F5 - E5 D5 | C5 E5 A5 - G5 E5 C5 - | D5 - F5 A5 G5 - - . |";
    private const string MeadowA2 = "E5 G5 C6 - B5 G5 A5 - | G5 - D5 E5 F5 - E5 D5 | C5 E5 A5 G5 F5 D5 B4 G4 | C5 - - - . . G4 . |";
    private const string MeadowB = "A5 - C6 A5 G5 - E5 - | F5 G5 A5 - G5 - D5 - | E5 - G5 E5 D5 C5 A4 - | B4 C5 D5 - G5 - - . | "
        + "A5 - C6 A5 G5 - E5 - | F5 G5 A5 - C6 - B5 - | C6 - G5 E5 F5 - D5 - | C5 - - - . . . . |";
    private const string MeadowChords = "C G Am G C G Am C F Dm Am G F F C C";

    /// <summary>Sunny Meadow: bouncy and bright, the game's main theme.</summary>
    private static Song Meadow() => new Song(144, 2) { Echo = 0.14f }
        .Track(Lead, MeadowA1, MeadowA2, MeadowB)
        .Track(Bass, BassLine(MeadowChords, 2))
        .Track(Arp, ArpLine(MeadowChords, 4))
        .Drums("k . s . k k s .")
        .Drums("h h h h h h h o");

    private static Song Title()
    {
        const string chords = "C G Dm G C F C C";
        return new Song(112, 2) { Echo = 0.24f }
            .Track(Bell, "E5 G5 C6 - B5 G5 A5 - | G5 - - - . . D5 . | F5 A5 D6 - C6 A5 B5 - | G5 - - - . . . . | "
                + "E5 G5 C6 - D6 C6 B5 A5 | A5 - C6 - F5 - A5 - | G5 - E5 - D5 - E5 - | C5 - - - . . . . |")
            .Track(Pad, BassLine(chords, 3, "R - - - - - - -"))
            .Track(Bass, BassLine(chords, 2, "R . . R . . O ."))
            .Track(Arp, ArpLine(chords, 4, "0 1 2 1 2 1 0 1"))
            .Drums("k . . . s . . . | k . k . s . . h");
    }

    private static Song Map()
    {
        const string chords = "G Em C D G Em C G";
        return new Song(116, 2) { Echo = 0.2f }
            .Track(SoftLead, "G4 B4 D5 - B4 - G4 - | E5 - D5 B4 G4 - - . | E4 G4 C5 - E5 - D5 C5 | A4 - - - . . D5 . | "
                + "G5 - F#5 - E5 D5 B4 - | E5 - G5 - B4 - - . | C5 - E5 - D5 - A4 - | G4 - - - . . . . |")
            .Track(Bass, BassLine(chords, 2, "R . F . O . F ."))
            .Track(Arp, ArpLine(chords, 4, "0 2 1 2 0 2 1 2"))
            .Drums("k . h . s . h h");
    }

    /// <summary>Crystal Caves: slow, echoing bell arpeggios over a held bass.</summary>
    private static Song Caves()
    {
        const string chords = "Am F G E Am F Dm E";
        return new Song(92, 2) { Echo = 0.4f, EchoDelayBeats = 1.5f }
            .Track(SoftLead, "A5 - - - G5 - E5 - | F5 - - - E5 - C5 - | D5 - - - E5 - G5 - | E5 - - - - - . . | "
                + "A5 - - - B5 - C6 - | C6 - B5 - A5 - F5 - | F5 - - - E5 - D5 - | E5 - - - G#5 - - . |")
            .Track(Bell, ArpLine(chords, 4, "0 1 2 0 2 1 2 1"))
            .Track(Bass, BassLine(chords, 2, "R - - - - - F -"))
            .Track(Pad, BassLine(chords, 3, "R - - - - - - -"))
            .Drums("k . . . . . h . | . . k . . . h .");
    }

    /// <summary>Cloud Heights: airy and wide, a soaring melody with gentle drums.</summary>
    private static Song Sky()
    {
        const string chords = "F Bb C F Dm Bb C C";
        return new Song(120, 2) { Echo = 0.3f, EchoDelayBeats = 1f }
            .Track(Lead with { Wave = Wave.Pulse12, Volume = 0.17f, VibratoDepth = 0.008f },
                "F5 - A5 - C6 - A5 - | A#5 - - - A5 G5 F5 - | G5 - A#5 - D6 - C6 A#5 | A5 - - - - - . . | "
                + "D6 - C6 A5 F5 - A5 - | A#5 - D6 - F6 - D6 - | C6 - A#5 - A5 - G5 - | C6 - - - - - . . |")
            .Track(Bass, BassLine(chords, 2, "R . . O . . R ."))
            .Track(Arp, ArpLine(chords, 4, "0 1 2 1 0 2 1 2"))
            .Track(Pad, BassLine(chords, 4, "R - - - - - - -"))
            .Drums("k . h . s . h . | k . h k s . h h");
    }

    /// <summary>Ember Castle: a driving minor ostinato with a tense lead.</summary>
    private static Song Castle()
    {
        const string chords = "Dm Dm Bb A Dm Dm Bb A";
        return new Song(150, 2) { Echo = 0.16f }
            .Track(Lead, "D5 - - A4 D5 - E5 - | F5 - E5 - D5 - C#5 - | D5 - - A4 A#4 - G4 - | A4 - - - - - . . | "
                + "F5 - - D5 F5 - G5 - | A5 - G5 - F5 - E5 - | F5 - E5 D5 A#4 - D5 - | C#5 - - - E5 - A5 - |")
            .Track(Grit, BassLine(chords, 2, "R R O R R R F R"))
            .Track(Bass, BassLine(chords, 1, "R - - - R - - -"))
            .Drums("k h s h k k s h");
    }

    /// <summary>The Mole King: fast, aggressive, with a syncopated bass.</summary>
    private static Song Boss()
    {
        const string chords = "Em Em C D Em Em C B";
        return new Song(170, 2) { Echo = 0.12f }
            .Track(Lead, "E5 - G5 - F#5 - E5 - | B5 - - - A5 G5 F#5 - | E5 - G5 - A5 - B5 - | C6 - B5 - A5 - F#5 D5 | "
                + "E5 - G5 - F#5 - E5 - | B5 - - - D6 C6 B5 - | C6 - B5 - A5 - G5 - | F#5 - - - D#5 - F#5 - |")
            .Track(Grit, BassLine(chords, 2, "R O R O R O F O"))
            .Track(Arp, ArpLine(chords, 5, "2 1 0 1 2 1 0 1"))
            .Drums("k h s h k s s h | k h s h k k s o");
    }

    private static Song Clear() => new Song(150, 2) { Echo = 0.2f }
        .Track(Lead, "C5 E5 G5 C6 - - G5 - | A5 - B5 - C6 - - - | . . . . . . . .")
        .Track(Bass, "C3 . . . G2 . . . | F2 . G2 . C3 - - - | . . . . . . . .")
        .Track(Arp, "E4 G4 C5 E5 - - . . | F4 A4 G4 B4 C5 - - - | . . . . . . . .")
        .Drums("k . . . k . . . | k . k . s . . . | . . . . . . . .");

    private static Song Victory()
    {
        const string chords = "C Am F G C F G C";
        return new Song(126, 2) { Echo = 0.25f }
            .Track(Lead, "G5 - - E5 G5 - C6 - | B5 - A5 - E5 - - - | F5 - - D5 F5 - A5 - | B5 - - - G5 - - - | "
                + "C6 - - G5 E5 - G5 - | A5 - - F5 A5 - C6 - | B5 - D6 - B5 - G5 - | C6 - - - - - - - |")
            .Track(Bass, BassLine(chords, 2))
            .Track(Arp, ArpLine(chords, 4))
            .Drums("k . s . k k s .");
    }

    private static Song GameOver() => new Song(84, 2) { Echo = 0.3f }
        .Track(SoftLead, "C5 - B4 - A#4 - A4 - | G#4 - - - G4 - - - | . . . . . . . .")
        .Track(Bass, "C3 - - - F2 - - - | C#3 - - - C3 - - - | . . . . . . . .");

    private static Song Credits()
    {
        return new Song(100, 2) { Echo = 0.3f, EchoDelayBeats = 1f }
            .Track(Bell, MeadowA1, MeadowA2, MeadowB)
            .Track(Pad, BassLine(MeadowChords, 3, "R - - - - - - -"))
            .Track(Bass, BassLine(MeadowChords, 2, "R . . . O . . ."))
            .Track(Arp with { Volume = 0.05f }, ArpLine(MeadowChords, 4, "0 1 2 1 2 1 0 1"))
            .Drums(Repeat("k . . . s . . . | k . . k s . . h", 1));
    }
}
