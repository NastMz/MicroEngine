using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using Dev = MicroEngine.Graphics.Devices;

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
    private static readonly ColorF Ambient = new(0.10f, 0.10f, 0.20f, 1);   // what the picture keeps where no light reaches
    private static readonly RectF Whole = new(0, 0, 320, 180);
    private Renderer2D? _batcher;            // a batcher of our own: any target, any blend state
    private Dev.RenderTarget? _lights, _one;  // the light map, and one light with its shadows
    private Texture? _glow;
    private Vector2 _torch = new(120, 92);

    public override void Enter()
    {
        if (Game.Graphics is not DeviceGraphics graphics) return;   // a platform that does not draw on a graphics device
        _batcher = new Renderer2D(graphics.Device);
        _lights = graphics.Device.CreateRenderTarget(new Dev.RenderTargetDescription(320, 180));
        _one = graphics.Device.CreateRenderTarget(new Dev.RenderTargetDescription(320, 180));
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
        _one?.Dispose();
        _lights?.Dispose();
        _batcher?.Dispose();
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

        if (r is DeviceRenderer canvas && _batcher is not null)
        {
            #region lightmap
            canvas.Flush();   // the room is in the canvas now; what follows lands after it

            _batcher.Begin(new BatchSettings(_lights!, Clear: Ambient));
            _batcher.End();
            AddLight(_torch, 110, new ColorF(1.0f, 0.85f, 0.6f, 1));
            AddLight(new Vector2(280, 150), 80, new ColorF(0.45f, 0.8f, 1.0f, 1));

            // The light map multiplies the picture: full light keeps a pixel, the ambient colour darkens it.
            _batcher.Begin(new BatchSettings(canvas.Target, Blend: Dev.BlendState.Multiply));
            _batcher.Draw(_lights!.Color!, Whole, Matrix3x2.Identity, ColorF.White);
            _batcher.End();
            #endregion
        }
        else Font.DrawCentered(r, "THIS EXAMPLE NEEDS A WINDOW", new Vector2(160, 86), Color.White);

        Heading.Draw(r, Font, "LIGHT BEHIND WALLS", "ARROWS: MOVE THE TORCH");
    }

    #region light
    /// <summary>One light: its glow, then the shadow of every wall in black over it, added to the light map.</summary>
    private void AddLight(Vector2 at, float radius, ColorF color)
    {
        var glow = ((DeviceTexture)_glow!).Native;
        _batcher!.Begin(new BatchSettings(_one!, Blend: Dev.BlendState.Additive, Sampler: Dev.SamplerDescription.LinearClamp, Clear: ColorF.Black));
        _batcher.Draw(glow, new RectF(0, 0, 64, 64), Matrix3x2.CreateScale(radius / 32) * Matrix3x2.CreateTranslation(at - new Vector2(radius)), color);
        _batcher.State = _batcher.State with { Blend = Dev.BlendState.Opaque };
        foreach (var wall in Walls)
        {
            Vector2 a = new(wall.X, wall.Y), b = new(wall.X + wall.Width, wall.Y), c = new(wall.X + wall.Width, wall.Y + wall.Height), d = new(wall.X, wall.Y + wall.Height);
            Shadow(at, a, b, new Vector2(0, -1));
            Shadow(at, b, c, new Vector2(1, 0));
            Shadow(at, c, d, new Vector2(0, 1));
            Shadow(at, d, a, new Vector2(-1, 0));
        }
        _batcher.End();

        _batcher.Begin(new BatchSettings(_lights!, Blend: Dev.BlendState.Additive));
        _batcher.Draw(_one!.Color!, Whole, Matrix3x2.Identity, ColorF.White);
        _batcher.End();
    }

    /// <summary>
    /// The shadow an edge casts: only edges that face away from the light cast one (so a wall keeps its own light), as
    /// a quad from the edge to far beyond the screen, straight away from the light.
    /// </summary>
    private void Shadow(Vector2 light, Vector2 from, Vector2 to, Vector2 outward)
    {
        if (Vector2.Dot(outward, from - light) <= 0) return;
        const float Far = 1000;
        Vector2 farFrom = from + Vector2.Normalize(from - light) * Far, farTo = to + Vector2.Normalize(to - light) * Far;
        _batcher!.DrawGeometry(null,
            [new(from, default, ColorF.Black), new(to, default, ColorF.Black), new(farTo, default, ColorF.Black), new(farFrom, default, ColorF.Black)],
            [0, 1, 2, 0, 2, 3]);
    }
    #endregion
}
