using MicroEngine.Audio;

namespace TrailDuel.Sound;

/// <summary>All synthesized sound effects, registered once at load (jingles are files, see <see cref="MusicBank"/>).</summary>
internal static class SoundBank
{
    public static void Register(AudioMixer audio)
    {
        void S(string name, SfxParams p) => audio.AddSound(name, Synth.Render(p));

        // Interface
        S("blip", new() { Wave = Wave.Square, Frequency = 820, Sustain = 0.008f, Decay = 0.03f, Volume = 0.22f, LowPass = 3500 });
        S("text", new() { Wave = Wave.Pulse25, Frequency = 660, EndFrequency = 990, Sustain = 0.02f, Decay = 0.05f, Volume = 0.3f, LowPass = 5000 });
        S("move", new() { Wave = Wave.Pulse25, Frequency = 1250, Sustain = 0.01f, Decay = 0.035f, Volume = 0.3f, LowPass = 6000 });
        S("confirm", new() { Wave = Wave.Square, Frequency = 880, Arpeggio = [1f, 1.5f], ArpeggioStep = 0.04f, Sustain = 0.05f, Decay = 0.07f, Volume = 0.32f, LowPass = 5000 });
        S("cancel", new() { Wave = Wave.Square, Frequency = 620, EndFrequency = 380, Sustain = 0.03f, Decay = 0.06f, Volume = 0.3f, LowPass = 4000 });
        S("deny", new() { Wave = Wave.Square, Frequency = 150, Sustain = 0.1f, Decay = 0.06f, Volume = 0.35f, LowPass = 1500 });
        S("menu", new() { Wave = Wave.Triangle, Frequency = 520, Arpeggio = [1f, 1.25f, 1.5f], ArpeggioStep = 0.035f, Sustain = 0.08f, Decay = 0.08f, Volume = 0.45f });
        S("save", new() { Wave = Wave.Sine, Frequency = 660, Arpeggio = [1f, 1.25f, 1.5f, 2f], ArpeggioStep = 0.07f, Sustain = 0.25f, Decay = 0.25f, Volume = 0.45f });
        S("buy", new() { Wave = Wave.Square, Frequency = 988, Arpeggio = [1f, 1.335f], ArpeggioStep = 0.06f, Sustain = 0.1f, Decay = 0.18f, Volume = 0.3f, LowPass = 6000 });

        // Overworld
        S("bump", new() { Wave = Wave.Triangle, Frequency = 130, EndFrequency = 70, Sustain = 0.03f, Decay = 0.08f, Volume = 0.6f });
        S("step", new() { Wave = Wave.Noise, Frequency = 1800, Sustain = 0.005f, Decay = 0.03f, Volume = 0.12f, LowPass = 1200 });
        S("grass", new() { Wave = Wave.Noise, Frequency = 5200, Sustain = 0.02f, Decay = 0.12f, Volume = 0.28f, LowPass = 3200 });
        S("ledge", new() { Wave = Wave.Square, Frequency = 300, EndFrequency = 700, Sustain = 0.06f, Decay = 0.08f, Volume = 0.3f, LowPass = 3000 });
        S("land", new() { Wave = Wave.Triangle, Frequency = 110, EndFrequency = 60, Sustain = 0.02f, Decay = 0.1f, Volume = 0.5f });
        audio.AddSound("door", Synth.Layer(
            (Synth.Render(new() { Wave = Wave.Noise, Frequency = 900, Sustain = 0.05f, Decay = 0.15f, Volume = 0.35f, LowPass = 900 }), 0f),
            (Synth.Render(new() { Wave = Wave.Square, Frequency = 240, EndFrequency = 180, Sustain = 0.04f, Decay = 0.12f, Volume = 0.25f, LowPass = 1400 }), 0.05f)));
        S("exclaim", new() { Wave = Wave.Square, Frequency = 1320, Arpeggio = [1f, 0f, 1f], ArpeggioStep = 0.05f, Sustain = 0.12f, Decay = 0.05f, Volume = 0.35f, LowPass = 6000 });
        S("encounter", new() { Wave = Wave.Saw, Frequency = 330, Arpeggio = [1f, 1.26f, 1.5f, 2f, 1.5f, 2f, 2.52f, 3f], ArpeggioStep = 0.05f, Sustain = 0.35f, Decay = 0.3f, Volume = 0.35f, LowPass = 3500, VibratoDepth = 0.01f });
        S("pickup", new() { Wave = Wave.Square, Frequency = 784, Arpeggio = [1f, 1.26f, 1.5f, 2f], ArpeggioStep = 0.06f, Sustain = 0.2f, Decay = 0.2f, Volume = 0.3f, LowPass = 5000 });

        // Battle
        audio.AddSound("hit", Synth.Layer(
            (Synth.Render(new() { Wave = Wave.Noise, Frequency = 3000, Sustain = 0.02f, Decay = 0.12f, Volume = 0.5f, LowPass = 3000, Crush = 10 }), 0f),
            (Synth.Render(new() { Wave = Wave.Square, Frequency = 220, EndFrequency = 60, Sustain = 0.02f, Decay = 0.12f, Volume = 0.35f, LowPass = 2000 }), 0f)));
        audio.AddSound("supereffective", Synth.Layer(
            (Synth.Render(new() { Wave = Wave.Noise, Frequency = 4000, Sustain = 0.05f, Decay = 0.2f, Volume = 0.6f, LowPass = 5000, Crush = 6 }), 0f),
            (Synth.Render(new() { Wave = Wave.Square, Frequency = 320, EndFrequency = 50, Sustain = 0.05f, Decay = 0.2f, Volume = 0.4f, LowPass = 2500 }), 0f),
            (Synth.Render(new() { Wave = Wave.Square, Frequency = 1200, EndFrequency = 400, Sustain = 0.02f, Decay = 0.08f, Volume = 0.2f }), 0.06f)));
        S("weakhit", new() { Wave = Wave.Noise, Frequency = 1500, Sustain = 0.02f, Decay = 0.1f, Volume = 0.35f, LowPass = 1100 });
        S("faint", new() { Wave = Wave.Square, Frequency = 620, EndFrequency = 70, Sustain = 0.15f, Decay = 0.45f, Volume = 0.35f, LowPass = 2500, VibratoDepth = 0.02f });
        S("throw", new() { Wave = Wave.Triangle, Frequency = 300, EndFrequency = 1100, Sustain = 0.1f, Decay = 0.12f, Volume = 0.4f });
        S("shake", new() { Wave = Wave.Square, Frequency = 180, EndFrequency = 120, Sustain = 0.03f, Decay = 0.08f, Volume = 0.45f, LowPass = 1500 });
        S("click", new() { Wave = Wave.Square, Frequency = 1900, Sustain = 0.015f, Decay = 0.03f, Volume = 0.4f });
        S("breakfree", new() { Wave = Wave.Noise, Frequency = 4000, EndFrequency = 800, Sustain = 0.05f, Decay = 0.25f, Volume = 0.45f, LowPass = 4000 });
        S("statup", new() { Wave = Wave.Sine, Frequency = 440, Arpeggio = [1f, 1.25f, 1.5f, 2f, 2.5f, 3f], ArpeggioStep = 0.045f, Sustain = 0.25f, Decay = 0.1f, Volume = 0.45f });
        S("statdown", new() { Wave = Wave.Sine, Frequency = 880, Arpeggio = [1f, 0.84f, 0.67f, 0.56f, 0.5f, 0.42f], ArpeggioStep = 0.045f, Sustain = 0.25f, Decay = 0.1f, Volume = 0.45f });
        S("status", new() { Wave = Wave.Saw, Frequency = 260, EndFrequency = 200, Sustain = 0.25f, Decay = 0.15f, Volume = 0.3f, LowPass = 1600, VibratoDepth = 0.06f, VibratoRate = 18 });
        S("heal", new() { Wave = Wave.Sine, Frequency = 520, Arpeggio = [1f, 1.5f, 2f, 1.5f, 2f, 2.5f], ArpeggioStep = 0.06f, Sustain = 0.3f, Decay = 0.2f, Volume = 0.4f });
        S("sendout", new() { Wave = Wave.Triangle, Frequency = 500, EndFrequency = 1500, Sustain = 0.05f, Decay = 0.15f, Volume = 0.45f });
        S("run", new() { Wave = Wave.Noise, Frequency = 1500, EndFrequency = 400, Sustain = 0.1f, Decay = 0.25f, Volume = 0.3f, LowPass = 2000 });
        S("lowhp", new() { Wave = Wave.Square, Frequency = 980, Sustain = 0.06f, Decay = 0.04f, Volume = 0.18f, LowPass = 4000 });
        S("xp", new() { Wave = Wave.Sine, Frequency = 600, EndFrequency = 1400, Sustain = 0.4f, Decay = 0.05f, Volume = 0.22f });
        S("sparkle", new() { Wave = Wave.Sine, Frequency = 1600, Arpeggio = [1f, 1.5f, 1.25f, 2f], ArpeggioStep = 0.03f, Sustain = 0.08f, Decay = 0.1f, Volume = 0.25f });
        S("evolve", new() { Wave = Wave.Sine, Frequency = 300, EndFrequency = 1200, Sustain = 1.2f, Decay = 0.4f, Volume = 0.35f, VibratoDepth = 0.02f });

        // Attacks, one voice per type.
        S("atk:Normal", new() { Wave = Wave.Square, Frequency = 320, EndFrequency = 110, Sustain = 0.04f, Decay = 0.1f, Volume = 0.35f, LowPass = 2000 });
        audio.AddSound("atk:Flame", Synth.Layer(
            (Synth.Render(new() { Wave = Wave.Noise, Frequency = 2500, Sustain = 0.25f, Decay = 0.25f, Volume = 0.45f, LowPass = 1800, VibratoDepth = 0.4f, VibratoRate = 22 }), 0f),
            (Synth.Render(new() { Wave = Wave.Saw, Frequency = 180, EndFrequency = 90, Sustain = 0.2f, Decay = 0.2f, Volume = 0.2f, LowPass = 900 }), 0f)));
        S("atk:Tide", new() { Wave = Wave.Sine, Frequency = 500, Arpeggio = [1f, 1.4f, 0.9f, 1.6f, 1.1f, 1.8f], ArpeggioStep = 0.04f, Sustain = 0.25f, Decay = 0.1f, Volume = 0.45f, VibratoDepth = 0.15f, VibratoRate = 30 });
        S("atk:Leaf", new() { Wave = Wave.Noise, Frequency = 7000, EndFrequency = 2000, Sustain = 0.15f, Decay = 0.2f, Volume = 0.35f, LowPass = 6000 });
        S("atk:Volt", new() { Wave = Wave.Square, Frequency = 900, Arpeggio = [1f, 2f, 0.7f, 2.4f, 1f, 1.8f], ArpeggioStep = 0.025f, Sustain = 0.2f, Decay = 0.12f, Volume = 0.3f, Crush = 6 });
        audio.AddSound("atk:Stone", Synth.Layer(
            (Synth.Render(new() { Wave = Wave.Noise, Frequency = 800, Sustain = 0.06f, Decay = 0.25f, Volume = 0.5f, LowPass = 700 }), 0f),
            (Synth.Render(new() { Wave = Wave.Triangle, Frequency = 110, EndFrequency = 45, Sustain = 0.05f, Decay = 0.2f, Volume = 0.6f }), 0f)));
        S("atk:Gale", new() { Wave = Wave.Noise, Frequency = 2500, Sustain = 0.25f, Decay = 0.25f, Volume = 0.35f, LowPass = 1400, VibratoDepth = 0.5f, VibratoRate = 6 });
    }
}
