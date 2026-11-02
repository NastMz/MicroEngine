using System.Globalization;
using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Desktop;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

// Synthetic load for measuring the engine (docs/guide/performance.md). Its own options come first; every other argument
// goes to the launcher, so `--profile FILE`, `--platform NAME`, `--frames N` and the rest work as in any game:
//
//   PerfBench [--rate N] [--sprites N] [--shapes N] [--text N] [--particles N] [--interpolate] [--canvas WxH] <launcher args>
var options = BenchOptions.Parse(args, out var rest);
return DesktopLauncher.Run(new BenchGame(options), rest);

/// <summary>The load a run draws, from the command line.</summary>
internal sealed record BenchOptions(int Rate, int Sprites, int Shapes, int Text, int Particles, bool Interpolate, int Width, int Height)
{
    public static BenchOptions Parse(string[] args, out string[] rest)
    {
        var o = new BenchOptions(60, 200, 50, 10, 300, false, 320, 180);
        var others = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            int Int() => int.Parse(args[++i], CultureInfo.InvariantCulture);
            switch (args[i])
            {
                case "--rate": o = o with { Rate = Int() }; break;
                case "--sprites": o = o with { Sprites = Int() }; break;
                case "--shapes": o = o with { Shapes = Int() }; break;
                case "--text": o = o with { Text = Int() }; break;
                case "--particles": o = o with { Particles = Int() }; break;
                case "--interpolate": o = o with { Interpolate = true }; break;
                case "--canvas":
                    var size = args[++i].Split('x');
                    o = o with { Width = int.Parse(size[0], CultureInfo.InvariantCulture), Height = int.Parse(size[1], CultureInfo.InvariantCulture) };
                    break;
                default: others.Add(args[i]); break;
            }
        }
        rest = [.. others];
        return o;
    }
}

internal sealed class BenchGame(BenchOptions options) : Game
{
    public override GameSettings Settings { get; } = new("MicroEngine PerfBench", options.Width, options.Height, 3)
    {
        UpdateRate = options.Rate,
        Background = Color.Hex("101820"),
    };

    public BenchOptions Options => options;
    public Sprite Ball { get; private set; } = null!;

    protected override void Load()
    {
        var palette = Art.Palette("o=#1a1c2c", "y=#ffcd75", "w=#f4f4f4");
        Ball = new SpriteAtlasBuilder().Add("ball", PixelImage.FromMap(palette,
            "..oooo..",
            ".oyyyyo.",
            "oyywyyyo",
            "oyyyyyyo",
            "oyyyyyyo",
            "oyyyyyyo",
            ".oyyyyo.",
            "..oooo.."), new Vector2(4, 4)).Build(Graphics)["ball"];
        Scenes.Push(new BenchScene(this));
    }
}

/// <summary>
/// Moving sprites at constant speeds (any unevenness in their motion is frame pacing), a scrolling bar to look at,
/// filled shapes, text and additive particles.
/// </summary>
internal sealed class BenchScene(BenchGame game) : Scene
{
    private const float BarSpeed = 120; // pixels per second
    private readonly Vector2[] _position = new Vector2[game.Options.Sprites];
    private readonly Vector2[] _previous = new Vector2[game.Options.Sprites];
    private readonly Vector2[] _velocity = new Vector2[game.Options.Sprites];
    private readonly ParticleSystem _particles = new(Math.Max(1, game.Options.Particles));
    private readonly ParticleSpec _spark = new() { StartColor = Color.Hex("ffcd75"), Life = (0.6f, 1.2f), Speed = (20, 70) };
    private float _bar, _previousBar, _emit;

    public override void Enter()
    {
        var random = new Random(4);
        for (int i = 0; i < _position.Length; i++)
        {
            _position[i] = _previous[i] = new Vector2(random.Next(game.Width), random.Next(game.Height));
            _velocity[i] = new Vector2(random.Next(-90, 90), random.Next(-90, 90));
        }
    }

    public override void Update(float dt)
    {
        _previousBar = _bar;
        _bar = (_bar + BarSpeed * dt) % game.Width;
        for (int i = 0; i < _position.Length; i++)
        {
            _previous[i] = _position[i];
            var p = _position[i] + _velocity[i] * dt;
            if (p.X < 0 || p.X > game.Width) _velocity[i].X = -_velocity[i].X;
            if (p.Y < 0 || p.Y > game.Height) _velocity[i].Y = -_velocity[i].Y;
            _position[i] = Vector2.Clamp(p, Vector2.Zero, new Vector2(game.Width, game.Height));
            if (MathF.Abs(_position[i].X - _previous[i].X) > game.Width / 2) _previous[i] = _position[i];
        }
        _emit += game.Options.Particles * dt;
        int count = (int)_emit;
        _emit -= count;
        if (count > 0) _particles.Emit(new Vector2(game.Width / 2f, game.Height / 2f), _spark, count);
        _particles.Update(dt);
    }

    public override void Draw(IRenderer r)
    {
        float blend = game.Options.Interpolate ? game.FrameBlend : 1;
        for (int i = 0; i < game.Options.Shapes; i++)
            r.FillCircle(new Vector2(i * 37 % game.Width, i * 23 % game.Height), 6 + i % 5, Color.Hex("29366f").WithAlpha(0.8f));
        for (int i = 0; i < _position.Length; i++) r.Draw(game.Ball, Vector2.Lerp(_previous[i], _position[i], blend));
        r.SetBlendMode(BlendMode.Additive);
        _particles.Draw(r);
        r.SetBlendMode(BlendMode.Alpha);
        // The bar wraps around the canvas; it is drawn where it is, not swept across the wrap.
        float bar = _bar < _previousBar ? _bar : float.Lerp(_previousBar, _bar, blend);
        r.FillRect(new RectF(bar, 0, 3, game.Height), Color.White);
        for (int i = 0; i < game.Options.Text; i++)
            game.Font.Draw(r, $"LINE {i} {game.UpdateCount}", new Vector2(4, 4 + i * 8 % (game.Height - 8)), Color.Hex("94b0c2"));
    }
}
