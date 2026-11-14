using System.Numerics;
using System.Runtime.InteropServices;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using Dev = MicroEngine.Graphics.Devices;

namespace Examples;

/// <summary>
/// A chain of full-screen passes over the finished picture: its bright parts blurred across, then down, at half size,
/// and a last pass that adds that glow and darkens the corners and every other line. Each pass is one draw of the
/// previous pass's target through a material. Needs a graphics device with custom shaders (<c>--graphics sdl-gpu</c>);
/// on any other the scene draws the picture as it is.
/// </summary>
public sealed class PostProcessExample : Scene
{
    private static readonly RectF Whole = new(0, 0, 320, 180);

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
    private Dev.Shader? _vertex, _blurFragment, _lookFragment;
    private Material2D? _blur, _look;
    private Dev.RenderTarget? _scene, _across, _glow;
    private bool _on = true;
    private float _time;

    public override void Enter()
    {
        if (Game.Graphics is not DeviceGraphics graphics || !graphics.Device.Capabilities.Has(Dev.GraphicsFeatures.Shaders)) return;
        var device = graphics.Device;
        _batcher = new Renderer2D(device);
        _vertex = Dev.CompiledShader.Parse(Assets.Bytes("shaders/sprite.vert.shader")).Create(device);
        _blurFragment = Dev.CompiledShader.Parse(Assets.Bytes("shaders/glow.frag.shader")).Create(device);
        _lookFragment = Dev.CompiledShader.Parse(Assets.Bytes("shaders/combine.frag.shader")).Create(device);
        _blur = new Material2D(_vertex, _blurFragment);
        _look = new Material2D(_vertex, _lookFragment);
        _scene = device.CreateRenderTarget(new Dev.RenderTargetDescription(320, 180));
        _across = device.CreateRenderTarget(new Dev.RenderTargetDescription(160, 90));   // the glow at half size
        _glow = device.CreateRenderTarget(new Dev.RenderTargetDescription(160, 90));
    }

    public override void Exit()
    {
        _glow?.Dispose();
        _across?.Dispose();
        _scene?.Dispose();
        _look?.Dispose();
        _blur?.Dispose();
        _lookFragment?.Dispose();
        _blurFragment?.Dispose();
        _vertex?.Dispose();
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

        if (r is DeviceRenderer canvas && _blur is not null)
        {
            if (_on)
            {
                #region chain
                canvas.Flush();
                // 1. The finished picture, copied where the passes can read it.
                Pass(canvas.Target.Color!, _scene!, null);
                // 2. Its bright parts blurred across, at half size. 3. That, blurred down.
                _blur.SetUniforms(new BlurParameters { Step = new Vector2(1f / 160, 0), Threshold = 0.55f });
                Pass(_scene!.Color!, _across!, _blur);
                _blur.SetUniforms(new BlurParameters { Step = new Vector2(0, 1f / 90) });
                Pass(_across!.Color!, _glow!, _blur);
                // 4. The picture and the glow together, back into the canvas.
                _look!.SetUniforms(new LookParameters { GlowStrength = 1.4f, Vignette = 0.9f, Scanlines = 0.18f, Lines = 180 });
                _look.SetTexture(1, _glow!.Color!, Dev.SamplerDescription.LinearClamp);
                Pass(_scene.Color!, canvas.Target, _look);
                #endregion
            }
        }
        else Font.DrawCentered(r, "THE SHADERS NEED --GRAPHICS SDL-GPU", new Vector2(160, 30), Color.White);

        Heading.Draw(r, Font, "POST-PROCESSING", _on ? "CONFIRM: OFF" : "CONFIRM: ON");
    }

    #region pass
    /// <summary>One pass: the whole of <paramref name="source"/> drawn over the whole of <paramref name="target"/>, through a material or as it is.</summary>
    private void Pass(Dev.Texture source, Dev.RenderTarget target, Material2D? material)
    {
        _batcher!.Begin(new BatchSettings(target, Blend: Dev.BlendState.Opaque, Sampler: Dev.SamplerDescription.LinearClamp) { Material = material });
        _batcher.Draw(source, new RectF(0, 0, source.Width, source.Height),
            Matrix3x2.CreateScale(target.Width / (float)source.Width, target.Height / (float)source.Height), ColorF.White);
        _batcher.End();
    }
    #endregion
}
