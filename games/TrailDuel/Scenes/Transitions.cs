using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using TrailDuel.Visuals;

namespace TrailDuel.Scenes;

/// <summary>Classic battle wipe: horizontal bands sweep in from alternating sides, then open from the centre.</summary>
internal sealed class BattleIntroTransition() : Transition(1.1f)
{
    public override void Draw(IRenderer r)
    {
        const int bands = 9;
        float bandH = r.Height / (float)bands;
        for (int i = 0; i < bands; i++)
        {
            float w = r.Width * Math.Clamp(Cover * 1.15f - i * 0.015f, 0, 1);
            float y = MathF.Floor(i * bandH);
            var rect = i % 2 == 0 ? new RectF(0, y, w, MathF.Ceiling(bandH)) : new RectF(r.Width - w, y, w, MathF.Ceiling(bandH));
            r.FillRect(rect, Pal.Ink);
            if (w > 2 && w < r.Width) r.FillRect(i % 2 == 0 ? new RectF(w - 2, y, 2, MathF.Ceiling(bandH)) : new RectF(r.Width - w, y, 2, MathF.Ceiling(bandH)), Pal.Coral);
        }
    }
}
