using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Hosting;
using MicroEngine.Graphics;
using MicroEngine.Input;
using GemGarden.Scenes;

namespace GemGarden;

/// <summary>Gem Garden: its settings, the art every scene shares, and the first scene.</summary>
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
            .Add("grass", GardenArt.Grass())
            .Build(Graphics);

        Scenes.Push(new TitleScene(this));
    }

    protected override void Unload() => Sprites.Dispose();
    #endregion
}
