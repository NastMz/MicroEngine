using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace Examples;

/// <summary>
/// A chain of full-screen passes over the finished picture: its bright parts blurred across, then down, at half size,
/// and a last pass that adds that glow and darkens the corners and every other line. Each pass is one draw of the
/// previous pass's target into the next through a material. The shaders run on a graphics device with custom shaders
/// (<c>--graphics sdl-gpu</c>); on any other the same passes copy the picture as it is, the materials' fallback.
/// </summary>
public sealed class PostProcessExample : Scene
{
    private static readonly RectF Whole = new(0, 0, 320, 180);

    private AssetCache? _assets;
    private Material2D? _blur, _look;
    private RenderTarget2D? _scene, _across, _glow;
    private bool _on = true;
    private float _time;

    public override void Enter()
    {
        _assets = Assets.CreateScope("post-processing");
        _blur = _assets.Material("shaders/glow.frag.shader");
        _look = _assets.Material("shaders/combine.frag.shader");
        _scene = _assets.RenderTarget("post/scene", 320, 180, TextureFilter.Nearest);
        _across = _assets.RenderTarget("post/across", 160, 90, TextureFilter.Linear);   // the glow at half size
        _glow = _assets.RenderTarget("post/glow", 160, 90, TextureFilter.Linear);
    }

    public override void Exit() => _assets?.Dispose();

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        if (Input.Pressed("confirm")) _on = !_on;
        _time += dt;
    }

    public override void Draw(IRenderer r)
    {
        // The picture, drawn where the passes can read it: a dark room with a few bright things in it.
        r.PushTarget(_scene!);
        r.FillGradient(Whole, Color.Hex("1a1c2c"), Color.Hex("29366f"));
        for (int i = 0; i < 6; i++)
        {
            var at = new Vector2(40 + i * 48, 90 + MathF.Sin(_time * 1.5f + i) * 34);
            r.FillCircle(at, 7, Color.Hex(i % 2 == 0 ? "ffcd75" : "73eff7"));
        }
        r.FillRect(new RectF(0, 150, 320, 30), Color.Hex("333c57"));
        r.FillRect(new RectF(20, 146, 280, 4), Color.Hex("f4f4f4"));
        r.PopTarget();

        if (_on)
        {
            #region chain
            // 1. The picture's bright parts blurred across, at half size. 2. That, blurred down.
            _blur!.Set("Step", new Vector2(1f / 160, 0));
            _blur.Set("Threshold", 0.55f);
            Pass(r, _scene!, _across, _blur);
            _blur.Set("Step", new Vector2(0, 1f / 90));
            _blur.Set("Threshold", 0f);
            Pass(r, _across!, _glow, _blur);
            // 3. The picture and the glow together, into the canvas.
            _look!.Set("GlowStrength", 1.4f);
            _look.Set("Vignette", 0.9f);
            _look.Set("Scanlines", 0.18f);
            _look.Set("Lines", 180f);
            _look.SetTexture("Glow", _glow!.Texture);
            Pass(r, _scene!, null, _look);
            #endregion
        }
        else Pass(r, _scene!, null, null);
        if (!_blur!.IsSupported) Font.DrawCentered(r, "THE SHADERS NEED --GRAPHICS SDL-GPU", new Vector2(160, 30), Color.White);
        Heading.Draw(r, Font, "POST-PROCESSING", _on ? "CONFIRM: OFF" : "CONFIRM: ON");
    }

    #region pass
    /// <summary>
    /// One pass: the whole of <paramref name="source"/> drawn over the whole of <paramref name="target"/> (the canvas
    /// when null), through a material or as it is.
    /// </summary>
    private static void Pass(IRenderer r, RenderTarget2D source, RenderTarget2D? target, Material2D? material)
    {
        if (target is not null) r.PushTarget(target);
        r.PushState(DrawState.Opaque.Filtered(TextureFilter.Linear) with { Material = material });
        r.DrawStretched(source.Texture, source.Bounds, new RectF(0, 0, r.Width, r.Height), Color.White);
        r.PopState();
        if (target is not null) r.PopTarget();
    }
    #endregion
}
