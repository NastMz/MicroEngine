using System.Numerics;
using MeadowRun.Art;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace MeadowRun.Scenes;

/// <summary>Developer-only sprite sheet (MEADOW_START=sheet) for reviewing art at 2x zoom.</summary>
internal sealed class SheetScene(MeadowGame game) : Scene
{
    public override void Update(float dt) { }

    public override void Draw(IRenderer r)
    {
        r.FillRect(new RectF(0, 0, 320, 180), Pal.Haze);
        var a = game.Art;
        float x = 4, y = 4;
        void Put(Sprite s)
        {
            if (x + s.Width * 2 > 316) { x = 4; y += 52; }
            r.Draw(s.Texture, s.Source, new Vector2(x, y), Color.White, new DrawOptions(Scale: new Vector2(2)));
            x += s.Width * 2 + 4;
        }
        foreach (var clip in new[] { "idle", "blink", "run", "skid", "jump", "fall", "hurt", "victory", "throw" })
            foreach (var f in a.Pip(PipForm.Sprout, clip)) Put(f);
        Put(a.Pip(PipForm.Bloom, "idle")[0]);
        Put(a.Pip(PipForm.Ember, "idle")[0]);
        Put(a.Pip(PipForm.Bloom, "run")[1]);
        Put(a.Pip(PipForm.Ember, "jump")[0]);
        foreach (var n in new[] { "bumblet", "thornback", "hopkin", "mothwing", "spitpod" })
            foreach (var f in a.Frames(n)) Put(f);
        foreach (var n in new[] { "sunpetal0", "emberseed0", "lifesprout", "spring0", "spring1", "checkpoint0", "checkpoint1" }) Put(a[n]);
    }
}
