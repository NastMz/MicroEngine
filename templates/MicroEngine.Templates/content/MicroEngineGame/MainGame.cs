using System.Numerics;
using MicroEngine;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MyGame.Scenes;

namespace MyGame;

/// <summary>The game: its settings, the assets every scene shares, and the first scene.</summary>
public sealed class MainGame : Game
{
    /// <summary>A 320x180 virtual screen, scaled to the window without blurring the pixels.</summary>
    public override GameSettings Settings { get; } = new("MyGameTitle") { Background = Color.Hex("1d2b53") };

    public SpriteAtlas Sprites { get; private set; } = null!;

    protected override void Load()
    {
        // An image file from the Content folder, and a sound made in code.
        var content = new ContentFolder();
        Sprites = new SpriteAtlasBuilder()
            .Add("hero", content.Image("hero.png"), new Vector2(8, 8))
            .Build(Graphics);
        Audio.AddSound("coin", Synth.Render(new SfxParams { Wave = Wave.Square, Frequency = 988, Arpeggio = [1f, 1.33f], Sustain = 0.04f, Decay = 0.15f }));
        Scenes.Push(new PlayScene(this));
    }

    protected override void Unload() => Sprites.Dispose();
}
