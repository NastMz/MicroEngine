using MicroEngine.Audio;
using MicroEngine.Audio.Retro;
using MicroEngine.Core;

namespace TrailDuel.Sound;

/// <summary>
/// The soundtrack: every track is an original eight-bar tune on chip voices (eighth-note steps, two per
/// beat), rendered once at load. The four jingles are recorded CC0 files (see CREDITS.md).
/// A recorded soundtrack was tried and dropped: it measured 3-10 dB quieter with far less bass drive,
/// and played as calm rather than adventurous.
/// </summary>
internal static class MusicBank
{
    private static readonly Instrument SoftLead = new(Wave.Pulse25, 0.2f, 0.01f, 0.12f, 0.5f, 0.1f, 0.45f, 0.005f, 5f, 4200);
    private static readonly Instrument Flute = new(Wave.Triangle, 0.32f, 0.03f, 0.15f, 0.7f, 0.12f, 0.45f, 0.006f, 5f);
    private static readonly Instrument Harp = new(Wave.Pulse12, 0.11f, 0.002f, 0.18f, 0.12f, 0.1f, 0.62f, 0, 5, 3200);
    private static readonly Instrument Brass = new(Wave.Saw, 0.15f, 0.02f, 0.1f, 0.65f, 0.08f, 0.5f, 0.004f, 6f, 2600);
    private static readonly Instrument Sub = new(Wave.Triangle, 0.45f, 0.004f, 0.06f, 0.75f, 0.04f, 0.5f);

    /// <summary>Short fanfares played over the music as sound effects, from <c>Content/jingles</c>.</summary>
    public static readonly string[] Jingles = ["heal", "levelup", "item", "catch"];

    public static void Register(AudioMixer audio, ContentFolder content)
    {
        foreach (var jingle in Jingles) audio.AddSoundFile("jingle:" + jingle, content.PathOf($"jingles/{jingle}.wav"));
        audio.AddMusic("title", Title());
        audio.AddMusic("town", Town());
        audio.AddMusic("route", Route());
        audio.AddMusic("forest", Forest());
        audio.AddMusic("town2", Brookhaven());
        audio.AddMusic("shrine", Shrine());
        audio.AddMusic("hall", Hall());
        audio.AddMusic("battle", WildBattle());
        audio.AddMusic("trainer", TrainerBattle());
        audio.AddMusic("master", MasterBattle());
        audio.AddMusic("victory", Victory());
        audio.AddMusic("evolution", Evolution());
        audio.AddMusic("credits", Credits());
    }

    private static string Arp(params string[] chords) => string.Join(" ", chords.Select(c =>
    {
        var n = c.Split(',');
        return $"{n[0]} {n[1]} {n[2]} {n[1]} {n[0]} {n[1]} {n[2]} {n[1]}";
    }));

    private static Song Title() => new Song(100, 2) { Echo = 0.22f }
        .Track(Brass,
            "E5 - - - D5 - C5 - | G5 - - - - - E5 - | F5 - E5 - D5 - C5 - | D5 - - - - - - - ",
            "E5 - - - D5 - C5 - | G5 - - - A5 - G5 - | F5 - E5 - D5 - E5 - | C5 - - - - - - - ")
        .Track(Harp, Arp("C4,E4,G4", "C4,E4,G4", "F4,A4,C5", "G4,B4,D5", "C4,E4,G4", "A4,C5,E5", "F4,A4,C5", "C4,E4,G4"))
        .Track(Sub,
            "C3 . G2 . C3 . G2 . | E3 . C3 . E3 . C3 . | F2 . C3 . F2 . C3 . | G2 . D3 . G2 . D3 . ",
            "C3 . G2 . C3 . G2 . | A2 . E3 . A2 . E3 . | F2 . C3 . G2 . D3 . | C3 . G2 . C3 - - - ")
        .Drums("k . h . s . h . k . h k s . h o");

    private static Song Town() => new Song(108, 2) { Echo = 0.2f }
        .Track(Flute,
            "B4 - G4 - A4 B4 D5 - | C5 - B4 - A4 - - - | G4 - E4 - F#4 G4 A4 - | B4 - - - - - . . ",
            "B4 - G4 - A4 B4 D5 - | E5 - D5 - C5 - B4 - | A4 - B4 - A4 - F#4 - | G4 - - - - - . . ")
        .Track(Harp, Arp("G4,B4,D5", "C4,E4,G4", "E4,G4,B4", "D4,F#4,A4", "G4,B4,D5", "C4,E4,G4", "D4,F#4,A4", "G4,B4,D5"))
        .Track(Sub,
            "G2 . D3 . G2 . D3 . | C3 . G2 . C3 . G2 . | E2 . B2 . E2 . B2 . | D3 . A2 . D3 . A2 . ",
            "G2 . D3 . G2 . D3 . | C3 . G2 . C3 . G2 . | D3 . A2 . D3 . F#2 . | G2 . D3 . G2 - - - ")
        .Drums("k . h h s . h . k . h . s . h h");

    private static Song Route() => new Song(132, 2) { Echo = 0.16f }
        .Track(SoftLead,
            "A4 - D5 - F#5 - E5 D5 | E5 - - - A4 - - - | B4 - D5 - G5 - F#5 E5 | F#5 - - - - - - - ",
            "A4 - D5 - F#5 - A5 - | G5 - F#5 - E5 - D5 - | E5 - F#5 - E5 - C#5 - | D5 - - - - - . . ")
        .Track(Harp, Arp("D4,F#4,A4", "A3,C#4,E4", "G4,B4,D5", "D4,F#4,A4", "D4,F#4,A4", "G4,B4,D5", "A3,C#4,E4", "D4,F#4,A4"))
        .Track(Sub,
            "D3 . D3 A2 D3 . D3 A2 | A2 . A2 E3 A2 . A2 E3 | G2 . G2 D3 G2 . G2 D3 | D3 . D3 A2 D3 . D3 A2 ",
            "D3 . D3 A2 D3 . D3 A2 | G2 . G2 D3 G2 . G2 D3 | A2 . A2 E3 A2 . C#3 E3 | D3 . A2 . D3 - - - ")
        .Drums("k . h . s . h k k . h . s . h h");

    private static Song Forest() => new Song(92, 2) { Echo = 0.32f, EchoDelayBeats = 1f }
        .Track(Flute,
            "A4 - - C5 E5 - D5 C5 | B4 - - - G4 - - - | A4 - - C5 E5 - G5 F5 | E5 - - - - - - - ",
            "F5 - E5 - D5 - C5 - | B4 - C5 - D5 - E5 - | C5 - B4 - A4 - G#4 - | A4 - - - - - . . ")
        .Track(Harp, Arp("A3,C4,E4", "G3,B3,D4", "A3,C4,E4", "E3,G#3,B3", "F3,A3,C4", "G3,B3,D4", "E3,G#3,B3", "A3,C4,E4"))
        .Track(Sub,
            "A2 - - - . . A2 . | G2 - - - . . G2 . | A2 - - - . . A2 . | E2 - - - . . E2 . ",
            "F2 - - - . . F2 . | G2 - - - . . G2 . | E2 - - - . . E2 . | A2 - - - . . . . ")
        .Drums("k . . h . . s . . . h . k . h .");

    private static Song Brookhaven() => new Song(116, 2) { Echo = 0.2f }
        .Track(SoftLead,
            "C5 - A4 - F4 - A4 - | Bb4 - D5 - C5 - - - | A4 - C5 - F5 - E5 D5 | C5 - - - - - - - ",
            "D5 - Bb4 - G4 - Bb4 - | C5 - A4 - F4 - A4 - | G4 - A4 - Bb4 - E4 - | F4 - - - - - . . ")
        .Track(Harp, Arp("F4,A4,C5", "Bb3,D4,F4", "F4,A4,C5", "C4,E4,G4", "G3,Bb3,D4", "F4,A4,C5", "C4,E4,G4", "F4,A4,C5"))
        .Track(Sub,
            "F2 . C3 . F2 . C3 . | Bb2 . F2 . Bb2 . F2 . | F2 . C3 . F2 . C3 . | C3 . G2 . C3 . G2 . ",
            "G2 . D3 . G2 . D3 . | F2 . C3 . F2 . C3 . | C3 . G2 . C3 . E2 . | F2 . C3 . F2 - - - ")
        .Drums("k . h . s h h . k k h . s . h .");

    private static Song Shrine() => new Song(84, 2) { Echo = 0.35f, EchoDelayBeats = 1f }
        .Track(Instrument.Bell,
            "E5 - G5 - C6 - - - | B5 - G5 - D5 - - - | A5 - F5 - C5 - - - | G5 - - - - - - - ",
            "E5 - G5 - C6 - - - | D6 - B5 - G5 - - - | F5 - A5 - G5 - E5 - | C5 - - - - - - - ")
        .Track(Instrument.Pad,
            "C4 - - - - - - - | G3 - - - - - - - | F3 - - - - - - - | G3 - - - - - - - ",
            "C4 - - - - - - - | G3 - - - - - - - | F3 - - - G3 - - - | C4 - - - - - - - ")
        .Track(Sub, "C3 - - - - - - - | G2 - - - - - - - | F2 - - - - - - - | G2 - - - - - - - ");

    private static Song Hall() => new Song(104, 2) { Echo = 0.25f }
        .Track(Brass,
            "D4 - D4 F4 A4 - - - | Bb4 - A4 - G4 - - - | F4 - G4 - A4 - D5 - | C#5 - - - - - - - ",
            "D5 - C5 - Bb4 - A4 - | G4 - A4 - Bb4 - - - | A4 - G4 - F4 - E4 - | D4 - - - - - - - ")
        .Track(Sub,
            "D2 . D2 . D2 . D2 . | G2 . G2 . G2 . G2 . | F2 . F2 . D2 . D2 . | A2 . A2 . A2 . A2 . ",
            "Bb2 . Bb2 . F2 . F2 . | G2 . G2 . G2 . G2 . | A2 . A2 . A2 . A2 . | D2 . D2 . D2 . A2 . ")
        .Drums("s . s s s . s . k . s . k . s s");

    private static Song WildBattle() => new Song(160, 2) { Echo = 0.12f }
        .Track(SoftLead,
            "E5 - E5 D5 E5 - G5 - | F#5 - E5 - D5 - B4 - | C5 - C5 B4 C5 - E5 - | D5 - - - B4 - - - ",
            "E5 - E5 D5 E5 - G5 - | A5 - G5 - F#5 - E5 - | D5 - B4 - C5 - D5 - | E5 - - - - - B4 - ")
        .Track(Sub,
            "E2 E3 E2 E3 E2 E3 E2 E3 | D2 D3 D2 D3 D2 D3 D2 D3 | C2 C3 C2 C3 C2 C3 C2 C3 | B1 B2 B1 B2 B1 B2 B1 B2 ",
            "E2 E3 E2 E3 E2 E3 E2 E3 | C2 C3 C2 C3 D2 D3 D2 D3 | B1 B2 B1 B2 C2 C3 D2 D3 | E2 E3 E2 E3 B1 B2 B1 B2 ")
        .Track(Harp, Arp("E4,G4,B4", "D4,F#4,A4", "C4,E4,G4", "B3,D#4,F#4", "E4,G4,B4", "C4,E4,G4", "B3,D#4,F#4", "E4,G4,B4"))
        .Drums("k h s h k k s h");

    private static Song TrainerBattle() => new Song(158, 2) { Echo = 0.12f }
        .Track(Brass,
            "E5 - B4 - E5 F#5 G5 - | F#5 - D5 - B4 - - - | C5 - G4 - C5 D5 E5 - | D5 - - - B4 - A4 - ",
            "G4 - B4 - E5 - G5 - | F#5 - E5 - D5 - F#5 - | E5 - D#5 - E5 - B4 - | E5 - - - - - - - ")
        .Track(Sub,
            "E2 E2 E3 E2 E2 E3 E2 E3 | D2 D2 D3 D2 D2 D3 D2 D3 | C2 C2 C3 C2 C2 C3 C2 C3 | B1 B1 B2 B1 B1 B2 B1 B2 ",
            "E2 E2 E3 E2 E2 E3 E2 E3 | D2 D2 D3 D2 D2 D3 D2 D3 | B1 B1 B2 B1 B1 B2 B1 B2 | E2 E2 E3 E2 B1 B2 B1 B2 ")
        .Track(Harp, Arp("E4,G4,B4", "B3,D4,F#4", "C4,E4,G4", "D4,F#4,A4", "E4,G4,B4", "D4,F#4,A4", "B3,D#4,F#4", "E4,G4,B4"))
        .Drums("k h s h k h s s");

    private static Song MasterBattle() => new Song(168, 2) { Echo = 0.14f }
        .Track(Brass,
            "A4 - C5 - E5 - A5 - | G5 - E5 - D5 - C5 - | B4 - D5 - G5 - B5 - | A5 - - - E5 - - - ",
            "F5 - E5 - D5 - C5 - | D5 - C5 - B4 - A4 - | G#4 - B4 - E5 - G#5 - | A5 - - - - - - - ")
        .Track(SoftLead,
            "E4 - A4 - C5 - E5 - | D5 - C5 - B4 - A4 - | G4 - B4 - D5 - G5 - | E5 - - - C5 - - - ",
            "D5 - C5 - A4 - A4 - | B4 - A4 - G4 - F4 - | E4 - G#4 - B4 - E5 - | E5 - - - - - - - ")
        .Track(Sub,
            "A1 A2 A1 A2 A1 A2 A1 A2 | G1 G2 G1 G2 G1 G2 G1 G2 | G1 G2 G1 G2 G1 G2 G1 G2 | A1 A2 A1 A2 A1 A2 A1 A2 ",
            "F1 F2 F1 F2 F1 F2 F1 F2 | D2 D3 D2 D3 D2 D3 D2 D3 | E1 E2 E1 E2 E1 E2 E1 E2 | A1 A2 A1 A2 E1 E2 G#1 G#2 ")
        .Drums("k h s k k h s h", "k h s k k s s s");

    private static Song Victory() => new Song(120, 2) { Echo = 0.2f }
        .Track(Brass,
            "G4 C5 E5 G5 - - E5 G5 | A5 - - - G5 - - - | F5 - E5 - D5 - E5 - | C5 - - - - - - - ",
            "E5 - C5 - D5 - G4 - | C5 - E5 - G5 - - - | F5 - A5 - G5 - E5 - | C5 - - - . . . . ")
        .Track(Harp, Arp("C4,E4,G4", "F4,A4,C5", "G4,B4,D5", "C4,E4,G4", "C4,E4,G4", "C4,E4,G4", "F4,A4,C5", "C4,E4,G4"))
        .Track(Sub,
            "C3 . C3 . C3 . C3 . | F2 . F2 . F2 . F2 . | G2 . G2 . G2 . G2 . | C3 . G2 . C3 . G2 . ",
            "C3 . G2 . G2 . G2 . | C3 . C3 . E3 . E3 . | F2 . F2 . G2 . G2 . | C3 . G2 . C3 . . . ")
        .Drums("k . h . s . h h");

    private static Song Evolution() => new Song(96, 4) { Echo = 0.3f }
        .Track(Harp,
            "C5 E5 G5 C6 B5 G5 E5 D5 C5 E5 G5 C6 B5 G5 E5 D5 | F5 A5 C6 F6 E6 C6 A5 G5 F5 A5 C6 F6 E6 C6 A5 G5 ",
            "D5 F5 A5 D6 C6 A5 F5 E5 D5 F5 A5 D6 C6 A5 F5 E5 | G5 B5 D6 G6 F6 D6 B5 A5 G5 B5 D6 G6 F6 D6 B5 A5 ")
        .Track(Instrument.Pad, "C4 - - - - - - - - - - - - - - - | F4 - - - - - - - - - - - - - - - | D4 - - - - - - - - - - - - - - - | G4 - - - - - - - - - - - - - - - ");

    private static Song Credits() => new Song(100, 2) { Echo = 0.25f }
        .Track(Flute,
            "D5 - B4 - G4 - B4 - | C5 - E5 - D5 - - - | B4 - G4 - E4 - G4 - | A4 - - - - - - - ",
            "D5 - B4 - G4 - B4 - | C5 - E5 - G5 - - - | F#5 - E5 - D5 - A4 - | G4 - - - - - - - ")
        .Track(Harp, Arp("G4,B4,D5", "C4,E4,G4", "E4,G4,B4", "D4,F#4,A4", "G4,B4,D5", "C4,E4,G4", "D4,F#4,A4", "G4,B4,D5"))
        .Track(Instrument.Pad,
            "G3 - - - - - - - | C4 - - - - - - - | E3 - - - - - - - | D3 - - - - - - - ",
            "G3 - - - - - - - | C4 - - - - - - - | D4 - - - - - - - | G3 - - - - - - - ")
        .Track(Sub,
            "G2 . . . D3 . . . | C3 . . . G2 . . . | E2 . . . B2 . . . | D3 . . . A2 . . . ",
            "G2 . . . D3 . . . | C3 . . . G2 . . . | D3 . . . A2 . . . | G2 . . . D3 . . . ")
        .Drums("k . h . s . h .");
}
