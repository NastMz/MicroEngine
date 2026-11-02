using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace Examples;

/// <summary>Sprites from a PNG in Content: a sheet cut into frames, one frame cropped by hand, origins and flipping.</summary>
public sealed class SpritesExample : Scene
{
    #region load
    private SpriteAtlas _atlas = null!;

    public override void Enter()
    {
        // Content/hero.png is a 64x16 strip of four 16x16 frames, copied next to the executable by the project file.
        var content = new ContentFolder();
        var sheet = content.Image("hero.png");
        _atlas = new SpriteAtlasBuilder()
            .AddGrid("hero", sheet, 16, 16, origin: new Vector2(8, 16))      // hero0..hero3, origin at the feet
            .Add("head", sheet.Crop(0, 0, 16, 9), new Vector2(8, 9))         // part of the image
            .Build(Game.Graphics);                                           // one texture for all of them
    }

    public override void Exit() => _atlas.Dispose();
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) Game.Scenes.Pop();
    }

    public override void Draw(IRenderer r)
    {
        #region draw
        // Draw puts the sprite's origin at the position: here the feet stand on the line.
        r.DrawLine(new Vector2(20, 80), new Vector2(300, 80), 1, Color.Hex("566c86"));
        var frames = _atlas.Frames("hero");
        for (int i = 0; i < frames.Count; i++)
            r.Draw(frames[i], new Vector2(40 + i * 32, 80));
        r.Draw(_atlas["hero0"], new Vector2(200, 80), flipX: true);                         // mirrored
        r.Draw(_atlas["hero0"], new Vector2(240, 80), tint: Color.Hex("ef7d57"));          // tinted
        r.DrawPivoted(_atlas["hero0"], new Vector2(280, 80), rotation: 0, scale: new Vector2(2, 2 + MathF.Sin(Age * 4) * 0.2f));
        r.Draw(_atlas["head"], new Vector2(160, 130));
        #endregion

        Font.Draw(r, $"{_atlas.Names.Count} SPRITES IN ONE {_atlas.Texture.Width}x{_atlas.Texture.Height} TEXTURE", new Vector2(20, 150), Color.White);
        Heading.Draw(r, Font, "SPRITES FROM FILES");
    }
}
