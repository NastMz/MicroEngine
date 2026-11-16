using System.Numerics;
using System.Runtime.InteropServices;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using Dev = MicroEngine.Graphics.Devices;

namespace Examples;

/// <summary>
/// Water that mirrors and distorts what is above it: a fragment shader of the game's own, with its parameters, as a
/// material of the 2D batcher. Needs a graphics device with custom shaders (<c>--graphics sdl-gpu</c>); on any other
/// the scene says so and draws flat water.
/// </summary>
public sealed class WaterExample : Scene
{
    private const float Surface = 118;   // the water line, in canvas pixels
    private static readonly RectF Whole = new(0, 0, 320, 180), Water = new(0, Surface, 320, 180 - Surface);

    #region parameters
    /// <summary>The shader's parameters, laid out as its <c>cbuffer Water</c>.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct WaterParameters
    {
        public ColorF Tint;
        public float Time, Amplitude, Frequency, Speed;
        public float Surface;
        public Vector3 Unused;
    }
    #endregion

    private Renderer2D? _batcher;
    private Dev.Shader? _vertex, _fragment;
    private Material2D? _water;
    private Dev.RenderTarget? _above;   // a copy of the picture, for the shader to read
    private float _time, _amplitude = 0.006f;

    #region setup
    public override void Enter()
    {
        // Shaders are an optional feature of a graphics device: ask, and keep a plain path for devices without them.
        if (Game.Graphics is not DeviceGraphics graphics || !graphics.Device.Capabilities.Has(Dev.GraphicsFeatures.Shaders)) return;
        var device = graphics.Device;
        _batcher = new Renderer2D(device);
        // Compiled ahead of time by `microengine shaders` from Shaders/*.hlsl into Content/shaders.
        _vertex = Dev.CompiledShader.Parse(Assets.Bytes("shaders/sprite.vert.shader")).Create(device);
        _fragment = Dev.CompiledShader.Parse(Assets.Bytes("shaders/water.frag.shader")).Create(device);
        _water = new Material2D(_vertex, _fragment);
        _above = device.CreateRenderTarget(new Dev.RenderTargetDescription(320, 180));
    }

    public override void Exit()
    {
        _above?.Dispose();
        _water?.Dispose();
        _fragment?.Dispose();
        _vertex?.Dispose();
        _batcher?.Dispose();
    }
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        _time += dt;
        _amplitude = Math.Clamp(_amplitude + Input.Vector("left", "right", "up", "down").X * 0.02f * dt, 0, 0.03f);
    }

    public override void Draw(IRenderer r)
    {
        // The world above the water: sky, a sun, hills and a few posts; then flat water, which the shader replaces.
        r.FillGradient(new RectF(0, 0, 320, Surface), Color.Hex("29366f"), Color.Hex("ef7d57"));
        r.FillCircle(new Vector2(236, 64), 22, Color.Hex("ffcd75"));
        r.FillEllipse(new Vector2(70, Surface + 6), 90, 46, Color.Hex("257179"));
        r.FillEllipse(new Vector2(250, Surface + 10), 110, 36, Color.Hex("38b764"));
        for (int i = 0; i < 5; i++) r.FillRect(new RectF(120 + i * 22, Surface - 26 - i % 2 * 8, 5, 30 + i % 2 * 8), Color.Hex("1a1c2c"));
        r.FillRect(Water, Color.Hex("3b5dc9"));

        if (r is DeviceRenderer canvas && _water is not null)
        {
            #region draw
            canvas.Flush();   // what was drawn so far is in the canvas
            // A pass cannot read the target it draws into: copy the picture, then draw the water from the copy.
            _batcher!.Begin(new BatchSettings(_above!, Blend: Dev.BlendState.Opaque));
            _batcher.Draw(canvas.Target.Color!, Whole, Matrix3x2.Identity, ColorF.White);
            _batcher.End();

            _water.SetUniforms(new WaterParameters
            {
                Tint = new ColorF(0.16f, 0.30f, 0.62f, 0.45f),
                Time = _time, Amplitude = _amplitude, Frequency = 9, Speed = 3,
                Surface = Surface / 180,
            });
            // The water's part of the copy, drawn in its own place through the material.
            _batcher.Begin(new BatchSettings(canvas.Target, Blend: Dev.BlendState.Opaque) { Material = _water });
            _batcher.Draw(_above!.Color!, Water, Matrix3x2.CreateTranslation(0, Surface), ColorF.White);
            _batcher.End();
            #endregion
        }
        else Font.DrawCentered(r, "THE SHADER NEEDS --GRAPHICS SDL-GPU", new Vector2(160, 150), Color.White);

        Heading.Draw(r, Font, "WATER", "LEFT/RIGHT: WAVES");
    }
}
