using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace Examples;

/// <summary>
/// Water that mirrors and distorts what is above it: a fragment shader of the game's own, loaded as a material and
/// drawn with the usual drawing calls. The shader runs on a graphics device with custom shaders
/// (<c>--graphics sdl-gpu</c>); on any other the same code draws flat water, the material's fallback, and the scene
/// says so.
/// </summary>
public sealed class WaterExample : Scene
{
    private const float Surface = 118;   // the water line, in canvas pixels
    private static readonly RectF Whole = new(0, 0, 320, 180), Water = new(0, Surface, 320, 180 - Surface);

    private AssetCache? _assets;
    private Material2D? _water;
    private RenderTarget2D? _above;   // the picture, for the shader to read
    private float _time, _amplitude = 0.006f;

    #region setup
    public override void Enter()
    {
        _assets = Assets.CreateScope("water");
        // Compiled ahead of time by `microengine shaders` from Shaders/water.frag.hlsl into Content/shaders.
        _water = _assets.Material("shaders/water.frag.shader");
        _above = _assets.RenderTarget("water/above", 320, 180, TextureFilter.Nearest);
    }

    public override void Exit() => _assets?.Dispose();   // the material and the target go with their scope
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        _time += dt;
        _amplitude = Math.Clamp(_amplitude + Input.Vector("left", "right", "up", "down").X * 0.02f * dt, 0, 0.03f);
    }

    public override void Draw(IRenderer r)
    {
        #region draw
        // The world is drawn into a target, so the shader can read it: sky, a sun, hills, a few posts and flat water.
        r.PushTarget(_above!);
        r.FillGradient(new RectF(0, 0, 320, Surface), Color.Hex("29366f"), Color.Hex("ef7d57"));
        r.FillCircle(new Vector2(236, 64), 22, Color.Hex("ffcd75"));
        r.FillEllipse(new Vector2(70, Surface + 6), 90, 46, Color.Hex("257179"));
        r.FillEllipse(new Vector2(250, Surface + 10), 110, 36, Color.Hex("38b764"));
        for (int i = 0; i < 5; i++) r.FillRect(new RectF(120 + i * 22, Surface - 26 - i % 2 * 8, 5, 30 + i % 2 * 8), Color.Hex("1a1c2c"));
        r.FillRect(Water, Color.Hex("3b5dc9"));
        r.PopTarget();
        r.Draw(_above!.Texture, Whole, Vector2.Zero, Color.White);

        // The shader's parameters, by the names they have in water.frag.hlsl.
        _water!.Set("Tint", new ColorF(0.16f, 0.30f, 0.62f, 0.45f));
        _water.Set("Time", _time);
        _water.Set("Amplitude", _amplitude);
        _water.Set("Frequency", 9f);
        _water.Set("Speed", 3f);
        _water.Set("Surface", Surface / 180);
        // The water's part of the picture, drawn in its own place through the material.
        r.PushState(DrawState.Opaque with { Material = _water });
        r.Draw(_above.Texture, Water, new Vector2(0, Surface), Color.White);
        r.PopState();
        #endregion
        if (!_water.IsSupported) Font.DrawCentered(r, "THE SHADER NEEDS --GRAPHICS SDL-GPU", new Vector2(160, 150), Color.White);
        Heading.Draw(r, Font, "WATER", "LEFT/RIGHT: WAVES");
    }
}
