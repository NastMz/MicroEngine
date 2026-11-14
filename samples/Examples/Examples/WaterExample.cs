using System.Numerics;
using System.Runtime.InteropServices;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using Dev = MicroEngine.Graphics.Devices;

namespace Examples;

/// <summary>
/// Water that mirrors and distorts what is above it, with a fragment shader of the game's own and its parameters. Needs
/// a graphics device with custom shaders (<c>--graphics sdl-gpu</c>); on any other the scene says so and draws flat water.
/// </summary>
public sealed class WaterExample : Scene
{
    private const float Surface = 118;   // the water line, in canvas pixels
    private static readonly RectF Whole = new(0, 0, 320, 180);

    /// <summary>The shader's parameters, laid out as its <c>cbuffer Water</c>.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct WaterParameters
    {
        public ColorF Tint;
        public float Time, Amplitude, Frequency, Speed;
        public float Surface;
        public Vector3 Unused;
    }

    private Renderer2D? _batcher;
    private RawShaderPass? _water;
    private Dev.RenderTarget? _above;   // a copy of the picture, for the shader to read
    private float _time, _amplitude = 0.012f;

    public override void Enter()
    {
        if (Game.Graphics is not DeviceGraphics graphics || !graphics.Device.Capabilities.Has(Dev.GraphicsFeatures.Shaders)) return;
        _batcher = new Renderer2D(graphics.Device);
        _water = new RawShaderPass(graphics.Device, Assets, "water", Dev.BlendState.Opaque);
        _above = graphics.Device.CreateRenderTarget(new Dev.RenderTargetDescription(320, 180));
    }

    public override void Exit()
    {
        _above?.Dispose();
        _water?.Dispose();
        _batcher?.Dispose();
    }

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        _time += dt;
        _amplitude = Math.Clamp(_amplitude + (Input.Vector("left", "right", "up", "down").X) * 0.02f * dt, 0, 0.04f);
    }

    public override void Draw(IRenderer r)
    {
        // The world above the water: sky, a sun, hills and a few posts.
        r.FillGradient(new RectF(0, 0, 320, Surface), Color.Hex("29366f"), Color.Hex("ef7d57"));
        r.FillCircle(new Vector2(236, 64), 22, Color.Hex("ffcd75"));
        r.FillEllipse(new Vector2(70, Surface + 6), 90, 46, Color.Hex("257179"));
        r.FillEllipse(new Vector2(250, Surface + 10), 110, 36, Color.Hex("38b764"));
        for (int i = 0; i < 5; i++) r.FillRect(new RectF(120 + i * 22, Surface - 26 - i % 2 * 8, 5, 30 + i % 2 * 8), Color.Hex("1a1c2c"));
        r.FillRect(new RectF(0, Surface, 320, 180 - Surface), Color.Hex("3b5dc9"));

        if (r is DeviceRenderer canvas && _water is not null)
        {
            canvas.Flush();
            // A pass cannot read the target it draws into: copy the picture, then draw the water from the copy.
            _batcher!.Begin(new BatchSettings(_above!, Blend: Dev.BlendState.Opaque));
            _batcher.Draw(canvas.Target.Color!, Whole, Matrix3x2.Identity, ColorF.White);
            _batcher.End();
            var area = new RectF(0, Surface, 320, 180 - Surface);
            _water.Draw(canvas.Target, area, new RectF(0, Surface / 180, 1, (180 - Surface) / 180), new WaterParameters
            {
                Tint = new ColorF(0.16f, 0.30f, 0.62f, 0.45f),
                Time = _time, Amplitude = _amplitude, Frequency = 9, Speed = 3,
                Surface = Surface / 180,
            }, _above!.Color!);
        }
        else Font.DrawCentered(r, "THE SHADER NEEDS --GRAPHICS SDL-GPU", new Vector2(160, 150), Color.White);

        Heading.Draw(r, Font, "WATER", "LEFT/RIGHT: WAVES");
    }
}
