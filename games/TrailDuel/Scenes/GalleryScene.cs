using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using TrailDuel.Rules;
using TrailDuel.Visuals;

namespace TrailDuel.Scenes;

/// <summary>
/// Debug view of every creature sprite (TRAIL_DEBUG=gallery) or zoomed pages of them (TRAIL_DEBUG=zoom), or of every character's walk frames
/// in all four directions (TRAIL_DEBUG=cast), or the capture orbs magnified (TRAIL_DEBUG=orbs).
/// </summary>
internal sealed class GalleryScene(TrailDuelGame game, bool cast = false, bool zoom = false, bool orbs = false) : Scene
{
    public override void Update(float dt) => _age += dt;

    public override void Draw(IRenderer r)
    {
        r.FillGradient(new RectF(0, 0, 320, 180), Color.Hex("cfe8f0"), Color.Hex("f6ecd2"));
        if (cast) { DrawCast(r); return; }
        if (zoom) { DrawZoom(r); return; }
        if (orbs) { DrawOrbs(r); return; }
        int i = 0;
        foreach (var s in SpeciesDex.All)
        {
            int col = i % 7, row = i / 7;
            var pos = new Vector2(24 + col * 45, 50 + row * 60);
            r.Draw(game.Atlas["front:" + s.Id], pos);
            i++;
        }
        for (int j = 0; j < 7; j++) r.Draw(game.Atlas["back:" + SpeciesDex.All[j].Id], new Vector2(24 + j * 45, 180));
    }

    private float _age;

    /// <summary>Three species per page: fronts at 2x, backs at 1x; pages advance every second.</summary>
    private void DrawZoom(IRenderer r)
    {
        int page = (int)_age % 5;
        for (int i = 0; i < 3 && page * 3 + i < SpeciesDex.All.Count; i++)
        {
            var id = SpeciesDex.All[page * 3 + i].Id;
            var front = game.Atlas["front:" + id];
            r.Draw(front.Texture, front.Source, new Vector2(8 + i * 104, 0), Color.White, new DrawOptions(Scale: new Vector2(2)));
            var back = game.Atlas["back:" + id];
            r.Draw(back.Texture, back.Source, new Vector2(28 + i * 104, 110), Color.White);
        }
    }

    /// <summary>Every orb sprite at 6x, then at 1x on light and dark grounds.</summary>
    private void DrawOrbs(IRenderer r)
    {
        string[] names = ["orb", "orbsuper", "orbsmall", "orbopen", "itemball"];
        float x = 4;
        foreach (var name in names)
        {
            var s = game.Atlas[name];
            r.Draw(s.Texture, s.Source, new Vector2(x, 8), Color.White, new DrawOptions(Scale: new Vector2(4)));
            r.Draw(s, new Vector2(x + 4, 120) + s.Origin);
            r.FillRect(new RectF(x, 140, 30, 24), Pal.Ink);
            r.Draw(s, new Vector2(x + 4, 144) + s.Origin);
            x += s.Width * 4 + 8;
        }
    }

    /// <summary>Four characters per page at 2x, Down/Left/Up walk frames; pages advance every second.</summary>
    private void DrawCast(IRenderer r)
    {
        var dirs = new[] { Direction.Down, Direction.Left, Direction.Up };
        int page = (int)_age % 4;
        for (int i = 0; i < 4; i++)
        {
            var look = CharacterLook.All[page * 4 + i];
            for (int d = 0; d < 3; d++)
            for (int f = 0; f < 3; f++)
            {
                var sprite = game.Atlas[CharacterArt.Key(look.Key, dirs[d], f)];
                r.Draw(sprite.Texture, sprite.Source, new Vector2(8 + (d * 3 + f) * 34 + d * 2, 2 + i * 45), Color.White, new DrawOptions(Scale: new Vector2(2)));
            }
        }
    }
}
