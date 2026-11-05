using MicroEngine.Audio;
using MicroEngine.Audio.Retro;

namespace MeadowRun.Audio;

/// <summary>Every sound effect in Meadow Run, synthesized at load time.</summary>
internal static class SoundBank
{
    private static SfxParams Tone(Wave wave, float from, float? to, float sustain, float decay, float volume = 0.35f, float lowPass = 0)
        => new() { Wave = wave, Frequency = from, EndFrequency = to, Sustain = sustain, Decay = decay, Volume = volume, LowPass = lowPass };

    private static AudioClip R(SfxParams p) => Synth.Render(p);

    /// <summary>A short melodic phrase rendered from (note, start, length) triples.</summary>
    private static AudioClip Phrase(Wave wave, float volume, params (string Note, float Start, float Length)[] notes)
        => Synth.Layer(notes.Select(n => (R(new SfxParams
        {
            Wave = wave, Frequency = Synth.NoteFrequency(n.Note), Sustain = n.Length * 0.6f, Decay = n.Length * 0.6f,
            Volume = volume, LowPass = 4200, VibratoDepth = 0.004f, VibratoRate = 6,
        }), n.Start)).ToArray());

    public static IEnumerable<(string Name, AudioClip Clip)> All()
    {
        yield return ("jump", R(Tone(Wave.Square, 300, 640, 0.03f, 0.12f, 0.28f, 3800) with { Attack = 0.002f }));
        yield return ("land", R(Tone(Wave.Noise, 220, 70, 0.01f, 0.07f, 0.5f, 900)));
        yield return ("skid", R(Tone(Wave.Noise, 1400, 700, 0.04f, 0.06f, 0.3f, 2600) with { Crush = 10 }));
        yield return ("drop", R(Tone(Wave.Triangle, 500, 220, 0.02f, 0.08f, 0.35f)));
        yield return ("coin", R(new SfxParams { Wave = Wave.Square, Frequency = 988, Arpeggio = [1f, 1.335f], ArpeggioStep = 0.055f, Sustain = 0.1f, Decay = 0.28f, Volume = 0.26f, LowPass = 5200 }));
        yield return ("stomp", Synth.Layer((R(Tone(Wave.Square, 260, 90, 0.02f, 0.1f, 0.35f, 2500)), 0), (R(Tone(Wave.Noise, 700, 200, 0.01f, 0.08f, 0.35f, 2000)), 0)));
        yield return ("bump", Synth.Layer((R(Tone(Wave.Triangle, 160, 90, 0.02f, 0.09f, 0.6f)), 0), (R(Tone(Wave.Noise, 400, 150, 0.01f, 0.05f, 0.25f, 1200)), 0)));
        yield return ("break", Synth.Layer((R(Tone(Wave.Noise, 900, 120, 0.03f, 0.28f, 0.6f, 3000) with { Crush = 8 }), 0), (R(Tone(Wave.Triangle, 180, 60, 0.02f, 0.15f, 0.5f)), 0)));
        yield return ("thud", R(Tone(Wave.Triangle, 120, 60, 0.02f, 0.08f, 0.5f)));
        yield return ("sprout", R(new SfxParams { Wave = Wave.Square, Frequency = 392, Arpeggio = [1f, 1.26f, 1.5f, 2f, 2.52f], ArpeggioStep = 0.05f, Sustain = 0.22f, Decay = 0.1f, Volume = 0.22f, LowPass = 3500 }));
        yield return ("powerup", R(new SfxParams { Wave = Wave.Pulse25, Frequency = 523, Arpeggio = [1f, 1.25f, 1.5f, 2f, 1.5f, 2f, 2.5f, 3f], ArpeggioStep = 0.045f, Sustain = 0.34f, Decay = 0.15f, Volume = 0.24f, VibratoDepth = 0.01f, VibratoRate = 12 }));
        yield return ("powerdown", R(new SfxParams { Wave = Wave.Square, Frequency = 700, EndFrequency = 220, Arpeggio = [1f, 0.84f, 0.7f, 0.6f], ArpeggioStep = 0.07f, Sustain = 0.22f, Decay = 0.12f, Volume = 0.26f, LowPass = 3000 }));
        yield return ("death", Phrase(Wave.Square, 0.24f, ("B4", 0, 0.12f), ("F5", 0.12f, 0.12f), ("F5", 0.3f, 0.1f), ("F5", 0.42f, 0.14f), ("E5", 0.6f, 0.14f), ("D5", 0.78f, 0.14f), ("C5", 0.96f, 0.5f)));
        yield return ("hurry", Phrase(Wave.Pulse25, 0.22f, ("E6", 0, 0.07f), ("F6", 0.08f, 0.07f), ("E6", 0.16f, 0.07f), ("F6", 0.24f, 0.07f), ("E6", 0.4f, 0.07f), ("F6", 0.48f, 0.07f), ("E6", 0.56f, 0.07f), ("F6", 0.64f, 0.07f)));
        yield return ("throw", Synth.Layer((R(Tone(Wave.Noise, 2000, 800, 0.02f, 0.06f, 0.2f, 4000)), 0), (R(Tone(Wave.Square, 700, 1400, 0.02f, 0.05f, 0.18f, 3000)), 0)));
        yield return ("knock", R(new SfxParams { Wave = Wave.Square, Frequency = 520, Arpeggio = [1f, 1.5f, 0.75f], ArpeggioStep = 0.04f, Sustain = 0.06f, Decay = 0.1f, Volume = 0.3f, LowPass = 3000 }));
        yield return ("spring", R(new SfxParams { Wave = Wave.Triangle, Frequency = 180, EndFrequency = 760, Sustain = 0.12f, Decay = 0.18f, Volume = 0.55f, VibratoDepth = 0.12f, VibratoRate = 22 }));
        yield return ("checkpoint", Phrase(Wave.Sine, 0.35f, ("C6", 0, 0.12f), ("E6", 0.1f, 0.12f), ("G6", 0.2f, 0.12f), ("C7", 0.3f, 0.4f)));
        yield return ("flag", R(new SfxParams { Wave = Wave.Square, Frequency = 1300, EndFrequency = 260, Sustain = 0.55f, Decay = 0.15f, Volume = 0.2f, LowPass = 3200, VibratoDepth = 0.03f, VibratoRate = 30 }));
        yield return ("tick", R(Tone(Wave.Square, 1760, null, 0.01f, 0.02f, 0.18f, 5000)));
        yield return ("gate", Synth.Layer((R(Tone(Wave.Noise, 300, 60, 0.05f, 0.3f, 0.7f, 900) with { Crush = 6 }), 0), (R(Tone(Wave.Triangle, 90, 40, 0.05f, 0.25f, 0.6f)), 0)));
        yield return ("roar", Synth.Layer((R(new SfxParams { Wave = Wave.Saw, Frequency = 140, EndFrequency = 70, Sustain = 0.7f, Decay = 0.4f, Volume = 0.45f, LowPass = 900, VibratoDepth = 0.08f, VibratoRate = 9 }), 0), (R(Tone(Wave.Noise, 500, 200, 0.6f, 0.4f, 0.35f, 1200)), 0)));
        yield return ("paw", Synth.Layer((R(Tone(Wave.Noise, 380, 200, 0.02f, 0.06f, 0.35f, 1400)), 0), (R(Tone(Wave.Noise, 380, 200, 0.02f, 0.06f, 0.35f, 1400)), 0.18f), (R(Tone(Wave.Noise, 380, 200, 0.02f, 0.06f, 0.35f, 1400)), 0.36f)));
        yield return ("charge", R(new SfxParams { Wave = Wave.Saw, Frequency = 90, EndFrequency = 260, Sustain = 0.3f, Decay = 0.2f, Volume = 0.35f, LowPass = 1400 }));
        yield return ("leap", R(Tone(Wave.Triangle, 140, 420, 0.1f, 0.15f, 0.55f)));
        yield return ("slam", Synth.Layer((R(Tone(Wave.Noise, 260, 40, 0.08f, 0.45f, 0.8f, 1000) with { Crush = 6 }), 0), (R(Tone(Wave.Triangle, 90, 35, 0.1f, 0.35f, 0.7f)), 0)));
        yield return ("burrow", R(Tone(Wave.Noise, 500, 90, 0.35f, 0.25f, 0.5f, 1300)));
        yield return ("erupt", Synth.Layer((R(Tone(Wave.Noise, 120, 700, 0.1f, 0.3f, 0.7f, 2000)), 0), (R(Tone(Wave.Square, 100, 300, 0.1f, 0.2f, 0.3f, 1200)), 0)));
        yield return ("bosshit", Synth.Layer((R(Tone(Wave.Square, 340, 90, 0.05f, 0.25f, 0.4f, 2400) with { Crush = 8 }), 0), (R(Tone(Wave.Noise, 900, 200, 0.02f, 0.2f, 0.4f, 2400)), 0)));
        yield return ("chip", R(Tone(Wave.Square, 900, 600, 0.01f, 0.05f, 0.25f, 3000)));
        yield return ("bossdown", Synth.Layer((R(new SfxParams { Wave = Wave.Saw, Frequency = 300, EndFrequency = 40, Sustain = 1.0f, Decay = 0.6f, Volume = 0.4f, LowPass = 1400, VibratoDepth = 0.1f, VibratoRate = 7 }), 0), (R(Tone(Wave.Noise, 600, 80, 0.8f, 0.6f, 0.4f, 1800)), 0.1f)));
        yield return ("clank", Synth.Layer((R(Tone(Wave.Square, 1400, 1300, 0.01f, 0.12f, 0.2f, 6000)), 0), (R(Tone(Wave.Square, 2110, 2050, 0.01f, 0.1f, 0.14f, 6000)), 0)));
        yield return ("crumble", R(Tone(Wave.Noise, 600, 150, 0.04f, 0.2f, 0.5f, 1500) with { Crush = 8 }));
        yield return ("hop", R(Tone(Wave.Triangle, 260, 520, 0.03f, 0.08f, 0.4f)));
        yield return ("swell", R(Tone(Wave.Sine, 220, 440, 0.2f, 0.15f, 0.3f)));
        yield return ("spit", R(Tone(Wave.Noise, 1600, 400, 0.02f, 0.08f, 0.35f, 3000)));
        yield return ("sizzle", R(Tone(Wave.Noise, 3000, 1500, 0.2f, 0.5f, 0.4f, 3500)));
        yield return ("sunstone", Phrase(Wave.Sine, 0.32f, ("C6", 0, 0.2f), ("G6", 0.12f, 0.2f), ("E6", 0.24f, 0.2f), ("C7", 0.36f, 0.2f), ("G7", 0.5f, 0.7f)));
        yield return ("oneup", Phrase(Wave.Square, 0.2f, ("G5", 0, 0.08f), ("C6", 0.08f, 0.08f), ("E6", 0.16f, 0.08f), ("G6", 0.24f, 0.08f), ("E6", 0.32f, 0.08f), ("C7", 0.4f, 0.22f)));
        yield return ("menu_move", R(Tone(Wave.Square, 880, null, 0.015f, 0.03f, 0.2f, 4000)));
        yield return ("menu_select", R(new SfxParams { Wave = Wave.Square, Frequency = 660, Arpeggio = [1f, 1.5f, 2f], ArpeggioStep = 0.04f, Sustain = 0.08f, Decay = 0.12f, Volume = 0.22f, LowPass = 4500 }));
        yield return ("menu_back", R(Tone(Wave.Square, 600, 300, 0.03f, 0.07f, 0.2f, 3500)));
        yield return ("pause", Phrase(Wave.Pulse25, 0.2f, ("E6", 0, 0.06f), ("C6", 0.07f, 0.06f), ("E6", 0.14f, 0.06f), ("C6", 0.21f, 0.12f)));
        yield return ("step", R(Tone(Wave.Noise, 300, 200, 0.005f, 0.03f, 0.25f, 1200)));
        yield return ("unlock", Phrase(Wave.Pulse25, 0.22f, ("C5", 0, 0.1f), ("G5", 0.1f, 0.1f), ("C6", 0.2f, 0.1f), ("E6", 0.3f, 0.1f), ("G6", 0.4f, 0.3f)));
        yield return ("blip", R(Tone(Wave.Square, 1200, null, 0.005f, 0.015f, 0.1f, 4000)));
    }
}
