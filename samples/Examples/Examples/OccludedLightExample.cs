using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace Examples;

/// <summary>
/// Two lights that stop at walls, made of render targets and blend states only, so it runs on every graphics device:
/// each light is drawn into a target of its own with the shadows of the walls over it, the lights are added into a
/// light map, and the light map multiplies the finished picture.
/// </summary>
public sealed class OccludedLightExample : Scene
{
    #region setup
    private static readonly RectF[] Walls = [new(56, 44, 44, 12), new(150, 70, 12, 56), new(214, 40, 50, 12), new(96, 118, 28, 28), new(236, 104, 14, 44)];
    private static readonly Color Ambient = new(25, 25, 51);   // what the picture keeps where no light reaches
    private static readonly RectF Whole = new(0, 0, 320, 180);
    private AssetCache? _assets;
    private RenderTarget2D? _lights, _one;   // the light map, and one light with its shadows
    private Texture? _glow;
    private Vector2 _torch = new(120, 92);

    public override void Enter()
    {
        _assets = Assets.CreateScope("light");
        _lights = _assets.RenderTarget("light/map", 320, 180, TextureFilter.Nearest);
        _one = _assets.RenderTarget("light/one", 320, 180, TextureFilter.Nearest);
        // A round falloff, white in the middle and transparent at the rim.
        var falloff = new PixelImage(64, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float d = Math.Clamp(1 - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32, 32)) / 32, 0, 1);
                falloff[x, y] = Color.White.WithAlpha(d * d);
            }
        _glow = Game.Graphics.CreateTexture(falloff, TextureFilter.Linear);
    }

    public override void Exit()
    {
        _glow?.Dispose();
        _assets?.Dispose();
    }
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        _torch += Input.Vector("left", "right", "up", "down") * 90 * dt;
    }

    public override void Draw(IRenderer r)
    {
        // The room, drawn as usual: a tiled floor and the walls.
        for (int y = 0; y < 180; y += 20)
            for (int x = 0; x < 320; x += 20)
                r.FillRect(new RectF(x, y, 20, 20), ((x + y) / 20) % 2 == 0 ? Color.Hex("566c86") : Color.Hex("94b0c2"));
        foreach (var wall in Walls) r.FillRect(wall, Color.Hex("333c57"));

        #region lightmap
        r.PushTarget(_lights!);
        r.Clear(Ambient);
        r.PopTarget();
        AddLight(r, _torch, 110, new Color(255, 217, 153));
        AddLight(r, new Vector2(280, 150), 80, new Color(115, 204, 255));

        // The light map multiplies the picture: full light keeps a pixel, the ambient colour darkens it.
        r.PushState(DrawState.Multiply);
        r.Draw(_lights!.Texture, Whole, Vector2.Zero, Color.White);
        r.PopState();
        #endregion
        Heading.Draw(r, Font, "LIGHT BEHIND WALLS", "ARROWS: MOVE THE TORCH");
    }

    #region light
    /// <summary>One light: its glow, then the shadow of every wall in black over it, added to the light map.</summary>
    private void AddLight(IRenderer r, Vector2 at, float radius, Color color)
    {
        r.PushTarget(_one!);
        r.Clear(Color.Black);
        r.PushState(DrawState.Additive);
        r.Draw(_glow!, _glow!.Bounds, at - new Vector2(radius), color, new DrawOptions(Scale: new Vector2(radius / 32)));
        r.PopState();
        foreach (var wall in Walls)
        {
            Vector2 a = new(wall.X, wall.Y), b = new(wall.X + wall.Width, wall.Y), c = new(wall.X + wall.Width, wall.Y + wall.Height), d = new(wall.X, wall.Y + wall.Height);
            Shadow(r, at, a, b, new Vector2(0, -1));
            Shadow(r, at, b, c, new Vector2(1, 0));
            Shadow(r, at, c, d, new Vector2(0, 1));
            Shadow(r, at, d, a, new Vector2(-1, 0));
        }
        r.PopTarget();

        r.PushTarget(_lights!);
        r.PushState(DrawState.Additive);
        r.Draw(_one!.Texture, Whole, Vector2.Zero, Color.White);
        r.PopState();
        r.PopTarget();
    }

    /// <summary>
    /// The shadow an edge casts: only edges that face away from the light cast one (so a wall keeps its own light), as
    /// a quad from the edge to far beyond the screen, straight away from the light.
    /// </summary>
    private static void Shadow(IRenderer r, Vector2 light, Vector2 from, Vector2 to, Vector2 outward)
    {
        if (Vector2.Dot(outward, from - light) <= 0) return;
        const float Far = 1000;
        Vector2 farFrom = from + Vector2.Normalize(from - light) * Far, farTo = to + Vector2.Normalize(to - light) * Far;
        r.FillTriangle(from, to, farTo, Color.Black);
        r.FillTriangle(from, farTo, farFrom, Color.Black);
    }
    #endregion
}
