using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace Examples;

/// <summary>
/// What a game draws every frame, without garbage and without a class of its own: one-shot effects played from a
/// point of the sprite, a row of torches each on its own frame of one clip, and a HUD of numbers and a hint.
/// </summary>
public sealed class EffectsExample : Scene
{
    private AssetCache _assets = null!;
    private SpriteAtlas _caster = null!;
    private SpriteAtlas _art = null!;
    private bool _facingLeft;
    private float _time;

    /// <summary>How many times the caster has cast.</summary>
    public int Casts { get; private set; }
    /// <summary>Points scored: 125 per cast.</summary>
    public int Score { get; private set; }

    #region setup
    public override void Enter()
    {
        _assets = Assets.CreateScope("effects");
        // effects.sheet gives every frame a point "hand" and a box "body".
        _caster = _assets.Atlas("effects.sheet");
        // Strips drawn in code are clips as soon as they are added with a rate.
        _art = new SpriteAtlasBuilder()
            .AddFrames("burst", Enumerable.Range(0, 6).Select(Burst), 18, new Vector2(8, 8), loop: false)
            .AddFrames("torch", Enumerable.Range(0, 3).Select(Torch), 9, new Vector2(3, 10))
            .Build(Game.Graphics);
    }
    #endregion

    public override void Exit()
    {
        _art.Dispose();
        _assets.Dispose();
    }

    #region play
    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        _time += dt;
        int turn = Input.Axis("left", "right");
        if (turn != 0) _facingLeft = turn < 0;
        if (!Input.Pressed("confirm")) return;

        // Three bursts from the caster's hand: the point of the frame showing now, mirrored with the sprite.
        var frame = _caster.Clip("caster").FrameAt(_time);
        var hand = Feet + frame.Point("hand", _facingLeft) * Scale;
        var burst = _art.Clip("burst");
        Effects.Play(burst, hand);
        Effects.Play(burst, hand + new Vector2(_facingLeft ? -22 : 22, -6), delay: 0.08f, flipX: true, tint: Gold);
        Effects.Play(burst, hand + new Vector2(_facingLeft ? -44 : 44, 2), delay: 0.16f, tint: Sky, framesPerSecond: 12);
        Casts++;
        Score += 125;
    }
    #endregion

    private const float Scale = 4;
    private static readonly Vector2 Feet = new(160, 128);
    private static readonly Color Gold = Color.Hex("#FFD866"), Sky = Color.Hex("#7FD8FF");

    #region draw
    public override void Draw(IRenderer r)
    {
        // One clip, one clock: each torch starts as many frames in as its place in the row.
        var torch = _art.Clip("torch");
        for (int i = 0; i < 7; i++) r.Draw(torch.FrameAt(_time, frameOffset: i), new Vector2(40 + i * 40, 150));

        var frame = _caster.Clip("caster").FrameAt(_time);
        r.DrawPivoted(frame, Feet, scale: new Vector2(Scale), flipX: _facingLeft);
        Effects.Draw(r);

        // Numbers written where they are drawn: no string is made, whatever the values.
        Font.DrawRight(r, $"{Score}", new Vector2(312, 30), Color.White, shadow: Color.Black);
        Font.Draw(r, $"CASTS {Casts}   EFFECTS {Effects.Count}", new Vector2(8, 30), Color.White);
        Font.DrawCentered(r, $"{(int)_time / 60}:{(int)_time % 60:00}.{(int)(_time * 10) % 10}", new Vector2(160, 34), Gold);
        // A hint is built once and kept until the player changes device.
        Font.DrawCentered(r, Game.Prompts.Cached("{confirm}: CAST   {left} {right}: TURN"), new Vector2(160, 168), Color.White);
        Heading.Draw(r, Font, "EFFECTS", "CLIPS, OFFSETS, POINTS");
    }
    #endregion

    /// <summary>The body box of the frame showing, in canvas pixels: what a hit test would use.</summary>
    public RectF Body
    {
        get
        {
            var box = _caster.Clip("caster").FrameAt(_time).Box("body", _facingLeft);
            return new RectF(Feet.X + box.X * Scale, Feet.Y + box.Y * Scale, box.Width * Scale, box.Height * Scale);
        }
    }

    /// <summary>A ring that grows and thins: frame <paramref name="i"/> of six.</summary>
    private static PixelImage Burst(int i)
    {
        var image = new PixelImage(16, 16);
        float radius = 2 + i * 1.1f, thickness = MathF.Max(0.6f, 2.4f - i * 0.4f);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(8, 8));
                if (MathF.Abs(d - radius) <= thickness) image[x, y] = Color.White;
                else if (i < 2 && d < radius) image[x, y] = Color.White.WithAlpha(0.6f);
            }
        // A spur to one side, so a mirrored burst is told from a plain one.
        for (int x = 9; x < Math.Min(16, 11 + i); x++) image[x, 8 - i / 2] = Color.White;
        return image;
    }

    /// <summary>A torch on a stick whose flame leans left, stands and leans right: frame <paramref name="i"/> of three.</summary>
    private static PixelImage Torch(int i)
    {
        var image = new PixelImage(7, 11);
        image.FillRect(3, 5, 1, 6, Color.Hex("#8A5A3A"));
        int lean = i - 1;
        image.FillRect(2, 3, 3, 2, Color.Hex("#FF8A3A"));
        image.FillRect(2 + lean, 1, 3, 2, Color.Hex("#FFD866"));
        image[3 + lean * 2, 0] = Color.Hex("#FFF4C2");
        return image;
    }
}
