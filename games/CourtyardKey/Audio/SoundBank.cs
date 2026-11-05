using MicroEngine.Audio;
using MicroEngine.Audio.Retro;

namespace CourtyardKey.Audio;

/// <summary>Every sound effect, synthesized at load. Names are the keys the game plays by.</summary>
public static class SoundBank
{
    public static readonly string[] Names =
    [
        "sword", "hit", "shield", "enemy_die", "hurt", "heart", "gem", "key", "chest", "unlock", "shutter",
        "push", "switch", "boomerang", "blip", "menu_move", "menu_ok", "menu_back", "cut", "pot", "spit",
        "hop", "flap", "roar", "stomp", "charge", "boss_hurt", "boss_die", "stairs", "low_health", "pause",
        "splash", "death", "deflect", "fanfare", "fanfare_big", "secret", "save", "gameover", "victory", "door_slam",
    ];

    public static void Register(AudioMixer audio)
    {
        static AudioClip R(SfxParams p) => Synth.Render(p);

        audio.AddSound("sword", Synth.Layer(
            (R(new SfxParams { Wave = Wave.Noise, Frequency = 5200, EndFrequency = 900, Attack = 0.002f, Sustain = 0.02f, Decay = 0.09f, Volume = 0.32f, LowPass = 5200 }), 0),
            (R(new SfxParams { Wave = Wave.Pulse25, Frequency = 980, EndFrequency = 560, Sustain = 0.01f, Decay = 0.06f, Volume = 0.12f, LowPass = 4000 }), 0.01f)));
        audio.AddSound("hit", Synth.Layer(
            (R(new SfxParams { Wave = Wave.Square, Frequency = 420, EndFrequency = 110, Sustain = 0.02f, Decay = 0.1f, Volume = 0.36f, Crush = 7 }), 0),
            (R(new SfxParams { Wave = Wave.Noise, Frequency = 2400, EndFrequency = 400, Sustain = 0.01f, Decay = 0.08f, Volume = 0.25f }), 0)));
        audio.AddSound("shield", Synth.Layer(
            (R(new SfxParams { Wave = Wave.Square, Frequency = 1650, Arpeggio = [1f, 1.33f], ArpeggioStep = 0.03f, Sustain = 0.02f, Decay = 0.16f, Volume = 0.22f, LowPass = 6000 }), 0),
            (R(new SfxParams { Wave = Wave.Noise, Frequency = 6000, Sustain = 0.005f, Decay = 0.05f, Volume = 0.2f }), 0)));
        audio.AddSound("deflect", R(new SfxParams { Wave = Wave.Triangle, Frequency = 1400, EndFrequency = 2200, Sustain = 0.02f, Decay = 0.12f, Volume = 0.4f }));
        audio.AddSound("enemy_die", Synth.Layer(
            (R(new SfxParams { Wave = Wave.Noise, Frequency = 1400, EndFrequency = 120, Sustain = 0.05f, Decay = 0.32f, Volume = 0.35f, LowPass = 3000 }), 0),
            (R(new SfxParams { Wave = Wave.Square, Frequency = 520, EndFrequency = 70, Sustain = 0.02f, Decay = 0.2f, Volume = 0.2f, Crush = 6 }), 0.02f)));
        audio.AddSound("hurt", R(new SfxParams { Wave = Wave.Saw, Frequency = 360, EndFrequency = 140, Sustain = 0.05f, Decay = 0.2f, Volume = 0.34f, VibratoDepth = 0.08f, VibratoRate = 30, LowPass = 2600 }));
        audio.AddSound("heart", R(new SfxParams { Wave = Wave.Triangle, Frequency = 880, Arpeggio = [1f, 1.26f, 1.5f, 2f], ArpeggioStep = 0.045f, Sustain = 0.14f, Decay = 0.12f, Volume = 0.42f }));
        audio.AddSound("gem", R(new SfxParams { Wave = Wave.Square, Frequency = 1320, Arpeggio = [1f, 1.5f], ArpeggioStep = 0.04f, Sustain = 0.05f, Decay = 0.12f, Volume = 0.2f, LowPass = 7000 }));
        audio.AddSound("key", R(new SfxParams { Wave = Wave.Triangle, Frequency = 660, Arpeggio = [1f, 1.335f, 2f, 2.67f], ArpeggioStep = 0.05f, Sustain = 0.2f, Decay = 0.2f, Volume = 0.45f }));
        audio.AddSound("chest", Synth.Layer(
            (R(new SfxParams { Wave = Wave.Square, Frequency = 140, EndFrequency = 260, Sustain = 0.18f, Decay = 0.05f, Volume = 0.16f, VibratoDepth = 0.15f, VibratoRate = 40, LowPass = 1200 }), 0),
            (R(new SfxParams { Wave = Wave.Noise, Frequency = 400, EndFrequency = 100, Sustain = 0.02f, Decay = 0.15f, Volume = 0.35f, LowPass = 900 }), 0.22f)));
        audio.AddSound("unlock", Synth.Layer(
            (R(new SfxParams { Wave = Wave.Noise, Frequency = 4000, Sustain = 0.005f, Decay = 0.04f, Volume = 0.3f }), 0),
            (R(new SfxParams { Wave = Wave.Square, Frequency = 740, Arpeggio = [1f, 0.75f, 1.5f], ArpeggioStep = 0.06f, Sustain = 0.12f, Decay = 0.1f, Volume = 0.2f, LowPass = 3000 }), 0.05f)));
        audio.AddSound("shutter", R(new SfxParams { Wave = Wave.Noise, Frequency = 380, EndFrequency = 90, Sustain = 0.22f, Decay = 0.2f, Volume = 0.45f, LowPass = 1400, Crush = 10 }));
        audio.AddSound("door_slam", Synth.Layer(
            (R(new SfxParams { Wave = Wave.Noise, Frequency = 260, EndFrequency = 60, Sustain = 0.04f, Decay = 0.3f, Volume = 0.55f, LowPass = 900 }), 0),
            (R(new SfxParams { Wave = Wave.Sine, Frequency = 90, EndFrequency = 45, Sustain = 0.05f, Decay = 0.25f, Volume = 0.5f }), 0)));
        audio.AddSound("push", R(new SfxParams { Wave = Wave.Noise, Frequency = 220, EndFrequency = 160, Attack = 0.03f, Sustain = 0.16f, Decay = 0.08f, Volume = 0.38f, LowPass = 700 }));
        audio.AddSound("switch", Synth.Layer(
            (R(new SfxParams { Wave = Wave.Square, Frequency = 880, Arpeggio = [1f, 2f], ArpeggioStep = 0.05f, Sustain = 0.06f, Decay = 0.1f, Volume = 0.2f }), 0),
            (R(new SfxParams { Wave = Wave.Sine, Frequency = 1760, Sustain = 0.05f, Decay = 0.4f, Volume = 0.3f }), 0.05f)));
        audio.AddSound("boomerang", R(new SfxParams { Wave = Wave.Triangle, Frequency = 640, EndFrequency = 900, Sustain = 0.06f, Decay = 0.05f, Volume = 0.22f, VibratoDepth = 0.12f, VibratoRate = 24 }));
        audio.AddSound("blip", R(new SfxParams { Wave = Wave.Square, Frequency = 980, Attack = 0.001f, Sustain = 0.018f, Decay = 0.02f, Volume = 0.1f, LowPass = 4000 }));
        audio.AddSound("menu_move", R(new SfxParams { Wave = Wave.Square, Frequency = 660, Sustain = 0.02f, Decay = 0.05f, Volume = 0.16f, LowPass = 4000 }));
        audio.AddSound("menu_ok", R(new SfxParams { Wave = Wave.Square, Frequency = 660, Arpeggio = [1f, 1.5f, 2f], ArpeggioStep = 0.04f, Sustain = 0.08f, Decay = 0.1f, Volume = 0.2f, LowPass = 5000 }));
        audio.AddSound("menu_back", R(new SfxParams { Wave = Wave.Square, Frequency = 520, EndFrequency = 330, Sustain = 0.03f, Decay = 0.08f, Volume = 0.16f, LowPass = 4000 }));
        audio.AddSound("cut", Synth.Layer(
            (R(new SfxParams { Wave = Wave.Noise, Frequency = 3600, EndFrequency = 1400, Sustain = 0.02f, Decay = 0.1f, Volume = 0.3f }), 0),
            (R(new SfxParams { Wave = Wave.Triangle, Frequency = 260, EndFrequency = 160, Sustain = 0.02f, Decay = 0.06f, Volume = 0.25f }), 0)));
        audio.AddSound("pot", Synth.Layer(
            (R(new SfxParams { Wave = Wave.Noise, Frequency = 1800, EndFrequency = 300, Sustain = 0.02f, Decay = 0.18f, Volume = 0.4f, Crush = 8 }), 0),
            (R(new SfxParams { Wave = Wave.Square, Frequency = 1200, EndFrequency = 900, Sustain = 0.01f, Decay = 0.05f, Volume = 0.12f }), 0.03f)));
        audio.AddSound("spit", R(new SfxParams { Wave = Wave.Square, Frequency = 620, EndFrequency = 180, Sustain = 0.02f, Decay = 0.1f, Volume = 0.22f, LowPass = 2200 }));
        audio.AddSound("hop", R(new SfxParams { Wave = Wave.Triangle, Frequency = 180, EndFrequency = 420, Sustain = 0.02f, Decay = 0.08f, Volume = 0.22f }));
        audio.AddSound("flap", R(new SfxParams { Wave = Wave.Noise, Frequency = 700, EndFrequency = 300, Sustain = 0.01f, Decay = 0.05f, Volume = 0.1f, LowPass = 1500 }));
        audio.AddSound("roar", Synth.Layer(
            (R(new SfxParams { Wave = Wave.Saw, Frequency = 140, EndFrequency = 55, Attack = 0.05f, Sustain = 0.4f, Decay = 0.4f, Volume = 0.38f, VibratoDepth = 0.12f, VibratoRate = 11, LowPass = 1200 }), 0),
            (R(new SfxParams { Wave = Wave.Noise, Frequency = 400, EndFrequency = 120, Attack = 0.05f, Sustain = 0.3f, Decay = 0.4f, Volume = 0.25f, LowPass = 900 }), 0)));
        audio.AddSound("stomp", Synth.Layer(
            (R(new SfxParams { Wave = Wave.Noise, Frequency = 180, EndFrequency = 40, Sustain = 0.05f, Decay = 0.35f, Volume = 0.55f, LowPass = 700 }), 0),
            (R(new SfxParams { Wave = Wave.Sine, Frequency = 90, EndFrequency = 35, Sustain = 0.06f, Decay = 0.3f, Volume = 0.6f }), 0)));
        audio.AddSound("charge", R(new SfxParams { Wave = Wave.Saw, Frequency = 180, EndFrequency = 720, Attack = 0.02f, Sustain = 0.3f, Decay = 0.08f, Volume = 0.25f, LowPass = 2400 }));
        audio.AddSound("boss_hurt", Synth.Layer(
            (R(new SfxParams { Wave = Wave.Square, Frequency = 300, EndFrequency = 90, Sustain = 0.05f, Decay = 0.2f, Volume = 0.4f, Crush = 5 }), 0),
            (R(new SfxParams { Wave = Wave.Noise, Frequency = 900, EndFrequency = 200, Sustain = 0.02f, Decay = 0.2f, Volume = 0.3f }), 0)));
        audio.AddSound("boss_die", Synth.Layer(
            (R(new SfxParams { Wave = Wave.Noise, Frequency = 900, EndFrequency = 50, Sustain = 0.6f, Decay = 0.9f, Volume = 0.5f, LowPass = 2200 }), 0),
            (R(new SfxParams { Wave = Wave.Saw, Frequency = 220, EndFrequency = 40, Sustain = 0.5f, Decay = 0.6f, Volume = 0.3f, VibratoDepth = 0.2f, VibratoRate = 14, LowPass = 1500 }), 0.1f)));
        audio.AddSound("stairs", Synth.Layer(
            (R(new SfxParams { Wave = Wave.Triangle, Frequency = 520, Sustain = 0.03f, Decay = 0.05f, Volume = 0.3f }), 0),
            (R(new SfxParams { Wave = Wave.Triangle, Frequency = 440, Sustain = 0.03f, Decay = 0.05f, Volume = 0.3f }), 0.12f),
            (R(new SfxParams { Wave = Wave.Triangle, Frequency = 370, Sustain = 0.03f, Decay = 0.05f, Volume = 0.3f }), 0.24f),
            (R(new SfxParams { Wave = Wave.Triangle, Frequency = 330, Sustain = 0.03f, Decay = 0.08f, Volume = 0.3f }), 0.36f)));
        audio.AddSound("low_health", R(new SfxParams { Wave = Wave.Square, Frequency = 1046, Sustain = 0.04f, Decay = 0.03f, Volume = 0.1f, LowPass = 5000 }));
        audio.AddSound("pause", R(new SfxParams { Wave = Wave.Square, Frequency = 523, Arpeggio = [1f, 1.5f, 2f, 1.5f], ArpeggioStep = 0.04f, Sustain = 0.12f, Decay = 0.06f, Volume = 0.18f, LowPass = 5000 }));
        audio.AddSound("splash", R(new SfxParams { Wave = Wave.Noise, Frequency = 2000, EndFrequency = 400, Sustain = 0.04f, Decay = 0.2f, Volume = 0.3f, LowPass = 3000 }));
        audio.AddSound("death", R(new SfxParams { Wave = Wave.Square, Frequency = 660, EndFrequency = 110, Sustain = 0.5f, Decay = 0.4f, Volume = 0.3f, VibratoDepth = 0.06f, VibratoRate = 9, LowPass = 3000 }));

        audio.AddSound("fanfare", Jingle(150, "G4 B4 D5 G5 - - - - B5 - - - - - . .", "D4 G4 B4 D5 - - - - G5 - - - - - . ."));
        audio.AddSound("fanfare_big", Jingle(130, "C5 E5 G5 C6 - - B5 - C6 - D6 - E6 - - - - - - - . . . .", "G4 C5 E5 G5 - - G5 - A5 - B5 - C6 - - - - - - - . . . ."));
        audio.AddSound("secret", Jingle(170, "A5 G#5 D#5 A4 G#4 E5 G#5 C6 - - - - . . . .", "A4 G#4 D#4 A3 G#3 E4 G#4 C5 - - - - . . . ."));
        audio.AddSound("save", Jingle(160, "E5 G5 C6 - - - . .", "C5 E5 G5 - - - . ."));
        audio.AddSound("gameover", Jingle(90, "B4 - A4 - G4 - F#4 - E4 - - - - - . .", "G3 - F#3 - E3 - D#3 - B2 - - - - - . ."));
        audio.AddSound("victory", Jingle(140, "E5 E5 E5 - C5 - D5 - E5 - D5 E5 G5 - - - - - . .", "C4 C4 C4 - A3 - B3 - C4 - B3 C4 E4 - - - - - . ."));
    }

    /// <summary>A short two-voice tune rendered as a one-shot sound (no echo, so it does not wrap).</summary>
    private static AudioClip Jingle(float bpm, string lead, string harmony)
    {
        var song = new Song(bpm) { Echo = 0, DrumVolume = 0 };
        song.Track(new Instrument(Wave.Pulse25, 0.26f, 0.004f, 0.1f, 0.6f, 0.12f, 0.4f, 0.004f, 6f, 6000), lead);
        song.Track(new Instrument(Wave.Triangle, 0.3f, 0.004f, 0.12f, 0.5f, 0.12f, 0.6f), harmony);
        var stereo = song.Render();
        // Mix to mono: one-shot sounds are panned by the mixer.
        var mono = new float[stereo.Samples.Length / 2];
        for (int i = 0; i < mono.Length; i++) mono[i] = (stereo.Samples[i * 2] + stereo.Samples[i * 2 + 1]) * 0.5f;
        return new AudioClip(mono);
    }
}
