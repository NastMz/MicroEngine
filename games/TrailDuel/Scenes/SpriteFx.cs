using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;

namespace TrailDuel.Scenes;

/// <summary>One-shot sprite animations (impacts, auras, smoke) that play once at a point and disappear.</summary>
internal sealed class SpriteFx
{
    private sealed class Playing
    {
        public required IReadOnlyList<Sprite> Frames;
        public Vector2 At;
        public float Time;
        public float Delay;
        public float Fps;
        public bool FlipX;
        public Color Tint;
    }

    private readonly List<Playing> _playing = [];
    private readonly SpriteAtlas _atlas;

    public SpriteFx(SpriteAtlas atlas) => _atlas = atlas;

    public bool Busy => _playing.Count > 0;

    /// <summary>Starts effect <paramref name="name"/> (atlas frames "fx:{name}N") after an optional delay.</summary>
    public void Play(string name, Vector2 at, float fps = 16, float delay = 0, bool flipX = false, Color? tint = null)
        => _playing.Add(new Playing { Frames = _atlas.Frames("fx:" + name), At = at, Fps = fps, Delay = delay, FlipX = flipX, Tint = tint ?? Color.White });

    /// <summary>How long effect <paramref name="name"/> runs at <paramref name="fps"/>.</summary>
    public float Duration(string name, float fps = 16) => _atlas.Frames("fx:" + name).Count / fps;

    public void Update(float dt)
    {
        foreach (var p in _playing)
        {
            if (p.Delay > 0) p.Delay -= dt;
            else p.Time += dt;
        }
        _playing.RemoveAll(p => p.Time * p.Fps >= p.Frames.Count);
    }

    public void Draw(IRenderer r)
    {
        foreach (var p in _playing)
        {
            if (p.Delay > 0) continue;
            var f = p.Frames[Math.Min(p.Frames.Count - 1, (int)(p.Time * p.Fps))];
            var at = new Vector2(MathF.Round(p.At.X), MathF.Round(p.At.Y));
            r.Draw(f.Texture, f.Source, at - (p.FlipX ? new Vector2(f.Width - f.Origin.X, f.Origin.Y) : f.Origin), p.Tint, new DrawOptions(FlipX: p.FlipX));
        }
    }
}
