using System.Numerics;
using MicroEngine;
using MicroEngine.Audio;
using MicroEngine.Audio.Retro;
using MicroEngine.Core;
using MicroEngine.Hosting;
using MicroEngine.Graphics;
using MicroEngine.Input;
using GemGarden.Scenes;

namespace GemGarden;

/// <summary>Gem Garden: its settings, the art and sounds every scene shares, and the first scene.</summary>
public sealed class MainGame : Game
{
    /// <summary>
    /// The game's composition, called by every build (desktop Program.cs, browser, Android, iOS): it chooses the game.
    /// </summary>
    public static void Configure(HostBuilder host) => host.UseGame(() => new MainGame());

    #region settings
    /// <summary>A 320x180 virtual screen, scaled to the window without blurring the pixels.</summary>
    public override GameSettings Settings { get; } = new("Gem Garden") { Background = Color.Hex("1a1c2c") };
    #endregion

    public SpriteAtlas Sprites { get; private set; } = null!;

    #region input
    /// <summary>The default actions (arrows, WASD, d-pad, left stick, confirm, cancel, pause) plus "run".</summary>
    protected override InputMap CreateInput() => InputMap.CreateDefault()
        .Bind("run", [Key.LeftShift, Key.X], [PadButton.B, PadButton.RightShoulder]);

    /// <summary>On touch screens: a floating joystick on the left, A (run, and confirm in menus) and pause on the right.</summary>
    protected override TouchLayout CreateTouchLayout() => TouchLayout.Of(
        new TouchStick { Offset = new(104, 100) },
        new TouchButton("run", "run", "A") { Anchor = TouchAnchor.BottomRight, Offset = new(72, 92), Radius = 40, Also = ["confirm"] },
        new TouchButton("pause", "pause", "II") { Anchor = TouchAnchor.TopRight, Offset = new(30, 30), Radius = 20 });
    #endregion

    #region load
    protected override void Load()
    {
        var content = new ContentFolder();
        Sprites = new SpriteAtlasBuilder()
            .AddGrid("hero", content.Image("hero.png"), 16, 16, origin: new Vector2(8, 16))
            .Add("gem", GardenArt.Gem(), new Vector2(4, 4))
            .AddFrames("slime", GardenArt.Slime(), new Vector2(6, 8))
            .Add("grass", GardenArt.Grass())
            .Add("hedge", GardenArt.Hedge())
            .Build(Graphics);

        LoadSounds(content);
        Scenes.Push(new TitleScene(this));
    }

    protected override void Unload() => Sprites.Dispose();
    #endregion

    #region sounds
    private void LoadSounds(ContentFolder content)
    {
        // A sound file from Content, and sounds and music made in code.
        Audio.AddSoundFile("pickup", content.PathOf("pickup.wav"));
        Audio.AddSound("hit", Synth.Render(new SfxParams { Wave = Wave.Noise, Frequency = 600, EndFrequency = 120, Sustain = 0.05f, Decay = 0.2f }));
        Audio.AddSound("select", Synth.Render(new SfxParams { Wave = Wave.Square, Frequency = 660, Sustain = 0.03f, Decay = 0.08f, Volume = 0.5f }));
        Audio.AddMusic("garden", new Song(112)
            .Track(Instrument.Pluck, "C5 . E5 . G5 . E5 . | D5 . F5 . A5 . F5 . | E5 . G5 . C6 . G5 . | D5 . B4 . G4 . . .")
            .Track(Instrument.Bass, "C3 - - . G2 - - . | D3 - - . A2 - - . | C3 - - . E3 - - . | G2 - - . G2 - - .")
            .Drums("k . h . s . h .")
            .Render());
    }
    #endregion
}
