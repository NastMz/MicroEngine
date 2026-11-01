using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace Examples;

/// <summary>The renderer's primitives, and a texture made from an image: drawn whole, stretched, rotated, blended.</summary>
public sealed class ShapesExample : Scene
{
    #region texture
    private Texture _checker = null!;

    public override void Enter()
    {
        // A PixelImage is pixels in memory; CreateTexture uploads it to the graphics device once.
        var image = new PixelImage(16, 16).Map((x, y, _) => (x / 4 + y / 4) % 2 == 0 ? Color.Hex("ffcd75") : Color.Hex("b13e53"));
        _checker = Game.Graphics.CreateTexture(image);
    }

    // A texture you create is yours to release.
    public override void Exit() => _checker.Dispose();
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) Game.Scenes.Pop();
    }

    public override void Draw(IRenderer r)
    {
        #region shapes
        r.FillRect(new RectF(16, 24, 48, 32), Color.Hex("3b5dc9"));
        r.DrawRect(new RectF(72, 24, 48, 32), Color.White, thickness: 2);
        r.FillCircle(new Vector2(148, 40), 16, Color.Hex("38b764"));
        r.FillEllipse(new Vector2(196, 40), 22, 10, Color.Hex("73eff7"));
        r.FillTriangle(new Vector2(232, 56), new Vector2(256, 24), new Vector2(280, 56), Color.Hex("ef7d57"));
        r.DrawLine(new Vector2(16, 68), new Vector2(304, 76), 2, Color.Hex("a7f070"));
        r.FillGradient(new RectF(16, 84, 288, 20), Color.Hex("29366f"), Color.Hex("41a6f6"), bands: 6);
        #endregion

        #region draw-texture
        r.Draw(_checker, new Vector2(16, 120));                                                   // whole, at its top-left
        r.DrawStretched(_checker, _checker.Bounds, new RectF(48, 116, 64, 24), Color.White);     // stretched into a rectangle
        r.Draw(_checker, new RectF(0, 0, 8, 8), new Vector2(124, 124), Color.Hex("73eff7"));    // part of it, tinted
        r.Draw(_checker, _checker.Bounds, new Vector2(176, 128), Color.White,
            new DrawOptions { Rotation = Age, Origin = new Vector2(8, 8), Scale = new Vector2(2) }); // rotated around its centre

        r.SetBlendMode(BlendMode.Additive);                                                      // light adds up
        r.FillCircle(new Vector2(250, 128), 14, Color.Hex("b13e53"));
        r.FillCircle(new Vector2(266, 128), 14, Color.Hex("3b5dc9"));
        r.SetBlendMode(BlendMode.Alpha);                                                         // back to normal
        #endregion

        Heading.Draw(r, Font, "SHAPES AND TEXTURES");
    }
}
