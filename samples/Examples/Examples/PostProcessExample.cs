using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace Examples;

/// <summary>
/// Post-processing with the scene's chain: the scene draws its picture as usual and the chain runs it through a
/// bloom, a pass through a shader of the game's own, a vignette and scanlines. Confirm turns the chain off and on;
/// action adds an effect written in the game (large pixels). Where the graphics device runs no shaders the bloom is
/// its simpler form and the shader pass is left out; the scene says so from what the chain reports.
/// </summary>
public sealed class PostProcessExample : Scene
{
    private static readonly RectF Whole = new(0, 0, 320, 180);

    private AssetCache? _assets;
    private readonly Pixelate _pixelate = new() { Enabled = false };
    private string _support = "";
    private float _time;

    #region chain
    public override void Enter()
    {
        _assets = Assets.CreateScope("post-processing");
        var fringe = _assets.Material("shaders/fringe.frag.shader");   // a fragment shader of the game's own
        fringe.Set("Amount", 0.004f);
        PostProcess
            .Add(new Bloom { Threshold = 0.55f, Strength = 1.4f, Radius = 6 })
            .Add(new ShaderEffect(fringe))
            .Add(_pixelate)
            .Add(new Vignette { Strength = 0.7f })
            .Add(new Scanlines { Strength = 0.18f });
        // What this device leaves out or simplifies, to tell the player.
        _support = string.Join(", ", PostProcess.SupportOn(Game.Graphics)
            .Where(e => e.Support != PostEffectSupport.Full)
            .Select(e => $"{e.Effect.GetType().Name.ToUpperInvariant()} {(e.Support == PostEffectSupport.None ? "OFF" : "REDUCED")}"));
    }
    #endregion

    public override void Exit() => _assets?.Dispose();

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        if (Input.Pressed("confirm")) PostProcess.Enabled = !PostProcess.Enabled;
        if (Input.Pressed("action")) _pixelate.Enabled = !_pixelate.Enabled;
        _time += dt;
    }

    #region draw
    public override void Draw(IRenderer r)
    {
        // The picture, drawn as in any scene: a dark room with a few bright things in it. The chain does the rest.
        r.FillGradient(Whole, Color.Hex("1a1c2c"), Color.Hex("29366f"));
        for (int i = 0; i < 6; i++)
        {
            var at = new Vector2(40 + i * 48, 90 + MathF.Sin(_time * 1.5f + i) * 34);
            r.FillCircle(at, 7, Color.Hex(i % 2 == 0 ? "ffcd75" : "73eff7"));
        }
        r.FillRect(new RectF(0, 150, 320, 30), Color.Hex("333c57"));
        r.FillRect(new RectF(20, 146, 280, 4), Color.Hex("f4f4f4"));
    }

    // The interface is not part of the scene's picture: it stays sharp over the effects.
    public override void DrawUi(IRenderer ui)
    {
        Heading.Draw(ui, Font, "POST-PROCESSING", PostProcess.Enabled ? "CONFIRM: OFF   ACTION: PIXELS" : "CONFIRM: ON");
        // At the bottom of the window, over the floor: clear of the heading and of the lights, whatever the window's size.
        if (_support.Length > 0) Font.DrawCentered(ui, "NO SHADERS HERE: " + _support, new Vector2(ui.Width / 2f, ui.Height - 12), Color.White);
    }
    #endregion

    #region effect
    /// <summary>An effect written in the game: the picture in large pixels. It needs no shader, so it is the same everywhere.</summary>
    public sealed class Pixelate : PostEffect
    {
        /// <summary>The side of a large pixel, in pixels of the picture.</summary>
        public int Size { get; set; } = 4;

        public override PostEffectSupport SupportOn(IGraphicsDevice graphics) => PostEffectSupport.Full;

        protected override void Apply(PostPass pass)
        {
            // The picture shrunk into a target of the chain's (linear: each large pixel is the average of what it
            // covers), then enlarged again with nearest sampling as the new picture.
            var small = pass.Scratch("small", Math.Max(1, pass.Width / Size), Math.Max(1, pass.Height / Size), TextureFilter.Nearest);
            pass.Draw(pass.Source, small, DrawState.Opaque.Filtered(TextureFilter.Linear));
            pass.Replace(small.Texture, TextureFilter.Nearest);
        }
    }
    #endregion
}
