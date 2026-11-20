using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace Examples;

/// <summary>
/// A large picture drawn much smaller than its pixels, twice: on the left as a linear texture, on the right as a texture
/// with mip levels. The fine lines of the left one flicker and break up as it turns; the right one stays even. Mip
/// levels need a graphics device that has them (<c>sdl-gpu</c>, raylib); elsewhere both textures are the same and the
/// scene says so.
/// </summary>
public sealed class MipLevelsExample : Scene
{
    private Texture _linear = null!, _levels = null!;
    private float _turn;

    /// <summary>The texture created with mip levels, for tests.</summary>
    public Texture Levels => _levels;

    #region textures
    public override void Enter()
    {
        var picture = Picture();
        _linear = Game.Graphics.CreateTexture(picture, TextureFilter.Linear);
        // The same call with another filter; from a file it is Assets.Texture("big.png", TextureFilter.LinearMipmap).
        _levels = Game.Graphics.CreateTexture(picture, TextureFilter.LinearMipmap);
    }

    public override void Exit()
    {
        _linear.Dispose();
        _levels.Dispose();
    }
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        _turn += dt * 0.3f;
    }

    public override void Draw(IRenderer r)
    {
        r.FillRect(new RectF(0, 0, 320, 180), Color.Hex("1a1c2c"));
        // 256 pixels drawn 64 wide (a quarter of its size), turning slowly around its centre at (80, 96) and (240, 96).
        var options = new DrawOptions(Rotation: _turn, Origin: new Vector2(128, 128), Scale: new Vector2(0.25f, 0.25f));
        r.Draw(_linear, _linear.Bounds, new Vector2(80 - 32, 96 - 32), Color.White, options);
        r.Draw(_levels, _levels.Bounds, new Vector2(240 - 32, 96 - 32), Color.White, options);
        Font.DrawCentered(r, "LINEAR", new Vector2(80, 150), Color.White);
        Font.DrawCentered(r, _levels.MipLevels > 1 ? $"MIP LEVELS ({_levels.MipLevels})" : "NO MIP LEVELS ON THIS DEVICE", new Vector2(240, 150), Color.White);
        Heading.Draw(r, Font, "SMALL PICTURES");
    }

    /// <summary>A 256 × 256 picture full of detail one pixel wide: thin bright lines on dark, and a ring.</summary>
    private static PixelImage Picture()
    {
        var image = new PixelImage(256, 256).Clear(Color.Hex("29366f"));
        for (int y = 0; y < 256; y++)
            for (int x = 0; x < 256; x++)
            {
                bool line = x % 6 == 0 || y % 6 == 0;
                float fromCentre = Vector2.Distance(new Vector2(x, y), new Vector2(128, 128));
                if (line) image[x, y] = Color.Hex("f4f4f4");
                if (fromCentre is > 90 and < 96) image[x, y] = Color.Hex("ffcd75");
            }
        return image;
    }
}
