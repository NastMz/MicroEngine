using System.Numerics;
using System.Runtime.InteropServices;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using Dev = MicroEngine.Graphics.Devices;

namespace Examples;

/// <summary>
/// A chain of full-screen passes over the finished picture: its bright parts blurred across, then down, at half size,
/// and a last pass that adds that glow and darkens the corners and every other line. Needs a graphics device with custom
/// shaders (<c>--graphics sdl-gpu</c>); on any other the scene draws the picture as it is.
/// </summary>
public sealed class PostProcessExample : Scene
{
    private static readonly RectF Whole = new(0, 0, 320, 180), Half = new(0, 0, 160, 90), All = new(0, 0, 1, 1);

    /// <summary>The blur's parameters, laid out as its <c>cbuffer Blur</c>.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct BlurParameters
    {
        public Vector2 Step;
        public float Threshold, Unused;
    }

    /// <summary>The last pass's parameters, laid out as its <c>cbuffer Look</c>.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct LookParameters
    {
        public float GlowStrength, Vignette, Scanlines, Lines;
    }

    private Renderer2D? _batcher;
    private RawShaderPass? _blur, _combine;
    private Dev.RenderTarget? _scene, _across, _glow;
    private bool _on = true;
    private float _time;

    public override void Enter()
    {
        if (Game.Graphics is not DeviceGraphics graphics || !graphics.Device.Capabilities.Has(Dev.GraphicsFeatures.Shaders)) return;
        var device = graphics.Device;
        _batcher = new Renderer2D(device);
        _blur = new RawShaderPass(device, Assets, "glow", Dev.BlendState.Opaque);
        _combine = new RawShaderPass(device, Assets, "combine", Dev.BlendState.Opaque);
        _scene = device.CreateRenderTarget(new Dev.RenderTargetDescription(320, 180));
        _across = device.CreateRenderTarget(new Dev.RenderTargetDescription(160, 90));
        _glow = device.CreateRenderTarget(new Dev.RenderTargetDescription(160, 90));
    }

    public override void Exit()
    {
        _glow?.Dispose();
        _across?.Dispose();
        _scene?.Dispose();
        _combine?.Dispose();
        _blur?.Dispose();
        _batcher?.Dispose();
    }

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        if (Input.Pressed("confirm")) _on = !_on;
        _time += dt;
    }

    public override void Draw(IRenderer r)
    {
        // The picture: a dark room with a few bright things in it.
        r.FillGradient(Whole, Color.Hex("1a1c2c"), Color.Hex("29366f"));
        for (int i = 0; i < 6; i++)
        {
            var at = new Vector2(40 + i * 48, 90 + MathF.Sin(_time * 1.5f + i) * 34);
            r.FillCircle(at, 7, Color.Hex(i % 2 == 0 ? "ffcd75" : "73eff7"));
        }
        r.FillRect(new RectF(0, 150, 320, 30), Color.Hex("333c57"));
        r.FillRect(new RectF(20, 146, 280, 4), Color.Hex("f4f4f4"));
        Heading.Draw(r, Font, "POST-PROCESSING", _on ? "CONFIRM: OFF" : "CONFIRM: ON");

        if (r is not DeviceRenderer canvas || _blur is null)
        {
            Font.DrawCentered(r, "THE SHADERS NEED --GRAPHICS SDL-GPU", new Vector2(160, 30), Color.White);
            return;
        }
        if (!_on) return;

        canvas.Flush();
        // 1. The finished picture, copied where the passes can read it.
        _batcher!.Begin(new BatchSettings(_scene!, Blend: Dev.BlendState.Opaque));
        _batcher.Draw(canvas.Target.Color!, Whole, Matrix3x2.Identity, ColorF.White);
        _batcher.End();
        // 2. Its bright parts blurred across, at half size; 3. that, blurred down.
        _blur.Draw(_across!, Half, All, new BlurParameters { Step = new Vector2(1f / 160, 0), Threshold = 0.55f }, _scene!.Color!);
        _blur.Draw(_glow!, Half, All, new BlurParameters { Step = new Vector2(0, 1f / 90) }, _across!.Color!);
        // 4. The picture and the glow together, back into the canvas.
        _combine!.Draw(canvas.Target, Whole, All, new LookParameters { GlowStrength = 1.4f, Vignette = 0.9f, Scanlines = 0.18f, Lines = 180 },
            _scene.Color!, _glow!.Color!);
    }
}
