using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace Examples;

/// <summary>
/// A picture shown only through a mask, with render targets and the blend states every graphics device has: the
/// picture, the mask and the mask's opposite are drawn into targets of their own with the usual drawing calls; the mask
/// multiplies the picture, its opposite multiplies the scene, and the two are added.
/// </summary>
public sealed class MaskExample : Scene
{
    #region setup
    private static readonly RectF Whole = new(0, 0, 320, 180);
    private AssetCache? _assets;
    private RenderTarget2D? _picture, _mask, _cover;
    private Vector2 _spot = new(230, 120);
    private float _time;

    public override void Enter()
    {
        _assets = Assets.CreateScope("masks");
        _picture = _assets.RenderTarget("masks/picture", 320, 180, TextureFilter.Nearest);
        _mask = _assets.RenderTarget("masks/mask", 320, 180, TextureFilter.Nearest);
        _cover = _assets.RenderTarget("masks/cover", 320, 180, TextureFilter.Nearest);
    }

    public override void Exit() => _assets?.Dispose();
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        _spot += Input.Vector("left", "right", "up", "down") * 90 * dt;
        _time += dt;
    }

    public override void Draw(IRenderer r)
    {
        r.FillGradient(Whole, Color.Hex("1a1c2c"), Color.Hex("333c57"));
        #region mask
        // The picture: anything the drawing API draws. Here, moving stripes.
        r.PushTarget(_picture!);
        r.Clear(Color.Hex("ef7d57"));
        for (int i = -2; i < 12; i++)
            r.FillRect(new RectF(i * 32 + _time * 24 % 32, 0, 16, 180), Color.Hex(i % 2 == 0 ? "ffcd75" : "a7f070"));
        r.PopTarget();

        // The mask is white where the picture shows and black elsewhere; the cover is its opposite.
        Shapes(r, _mask!, Color.Black, Color.White);
        Shapes(r, _cover!, Color.White, Color.Black);

        // Picture × mask: the picture where the mask is white, black elsewhere.
        r.PushTarget(_picture!);
        Over(r, _mask!, DrawState.Multiply);
        r.PopTarget();
        // Scene × cover: a black hole in the scene where the picture goes. Then the picture is added into the hole.
        Over(r, _cover!, DrawState.Multiply);
        Over(r, _picture!, DrawState.Additive);
        #endregion
        Heading.Draw(r, Font, "MASKS", "ARROWS: MOVE THE CIRCLE");
    }

    #region helpers
    /// <summary>The mask's shapes, text and a circle, drawn into a target like anything else.</summary>
    private void Shapes(IRenderer r, RenderTarget2D target, Color background, Color shape)
    {
        r.PushTarget(target);
        r.Clear(background);
        Font.DrawCentered(r, "MASK", new Vector2(160, 70), shape, scale: 9);
        r.FillCircle(_spot, 26, shape);
        r.PopTarget();
    }

    /// <summary>Draws the whole of a target's picture over what is being drawn into, with a blend state.</summary>
    private static void Over(IRenderer r, RenderTarget2D source, DrawState blend)
    {
        r.PushState(blend);
        r.Draw(source.Texture, Whole, Vector2.Zero, Color.White);
        r.PopState();
    }
    #endregion
}
