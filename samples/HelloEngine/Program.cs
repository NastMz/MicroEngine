using System.Numerics;
using MicroEngine;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Desktop;
using MicroEngine.Scenes;

return DesktopLauncher.Run(new HelloGame(), args);

/// <summary>The smallest complete MicroEngine game: procedural sprite, synth sound, music and a scene.</summary>
internal sealed class HelloGame : Game
{
    public override GameSettings Settings { get; } = new("Hello MicroEngine") { Background = Color.Hex("1d2b53") };
    public SpriteAtlas Atlas { get; private set; } = null!;

    protected override void Load()
    {
        var palette = Art.Palette("o=#000000", "y=#ffec27", "w=#fff1e8");
        var star = PixelImage.FromMap(palette,
            "....o....",
            "...oyo...",
            "ooooyoooo",
            "oyyywyyyo",
            ".oyyyyyo.",
            "..oyyyo..",
            ".oyyoyyo.",
            "oyyo.oyyo",
            "ooo...ooo");
        Atlas = new SpriteAtlasBuilder().Add("star", star, new Vector2(4.5f, 4.5f)).Build(Graphics);
        Audio.AddSound("ding", Synth.Render(new SfxParams { Wave = Wave.Square, Frequency = 880, Arpeggio = [1f, 1.5f], Sustain = 0.05f, Decay = 0.2f }));
        Audio.AddMusic("theme", new Song(120).Track(Instrument.Lead, "C5 - E5 - G5 - E5 - | C5 - - - . . . .").Track(Instrument.Bass, "C3 . C3 . G2 . G2 .").Drums("k . h . s . h .").Render());
        Scenes.Push(new HelloScene(this));
    }

    protected override void Unload() => Atlas.Dispose();
}

internal sealed class HelloScene(HelloGame game) : Scene
{
    private readonly ParticleSystem _particles = new();
    private Vector2 _position = new(160, 100);

    public override void Enter() => Audio.PlayMusic("theme");

    public override void Update(float dt)
    {
        var move = new Vector2(Input.Axis("left", "right"), Input.Axis("up", "down"));
        _position += move * 90 * dt;
        if (Input.Pressed("confirm"))
        {
            Audio.Play("ding", pitchJitter: 0.05f);
            _particles.Emit(_position, new ParticleSpec { StartColor = Color.Hex("ffec27"), EndColor = Color.Hex("ff004d00"), Gravity = 80 }, 24);
        }
        _particles.Update(dt);
    }

    public override void Draw(IRenderer r)
    {
        Font.DrawCentered(r, "Hello, MicroEngine!", new Vector2(160, 40), Color.Hex("fff1e8"), 2, Color.Hex("000000"));
        Font.DrawCentered(r, "Arrows move \u00B7 Z/Space bursts", new Vector2(160, 160), Color.Hex("c2c3c7"));
        _particles.Draw(r);
        r.Draw(game.Atlas["star"], _position + new Vector2(0, MathX.Wave(Age, 1.5f, 2)));
    }
}
