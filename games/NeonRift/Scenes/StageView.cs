using System.Numerics;
using NeonRift.Art;
using NeonRift.Gameplay;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.World;

namespace NeonRift.Scenes;

/// <summary>Draws a <see cref="Stage"/>: parallax backdrop, props, tiles, actors, foreground and HUD.</summary>
internal sealed class StageView(Assets art, BitmapFont font)
{
    public void Draw(IRenderer r, Stage stage)
    {
        var cam = stage.Camera;
        DrawBackdrop(r, stage.Def.Theme, cam.Position, stage.Time, foreground: false);

        r.PushCamera(cam);
        foreach (var prop in stage.Def.Props)
            if (cam.IsVisible(new RectF(prop.X - 64, prop.Y - 140, 128, 140)))
                r.Draw(art[prop.Sprite], new Vector2(prop.X, prop.Y));
        string theme = stage.Def.Theme;
        var layer = stage.Def.Layer;
        TileRenderer.Draw(r, layer, Tiles.Size, cam, (_, x, y) => Tiles.For(art, theme, layer, x, y));
        if (theme == "lab") DrawUndersides(r, layer, cam);
        if (theme == "grotto") DrawLavaGlow(r, stage);

        foreach (var c in stage.Pickups) c.Render(r, art);
        foreach (var e in stage.Enemies.OrderBy(e => e is Boss ? 0 : 1)) if (cam.IsVisible(e.Bounds, 140)) e.Render(r, art);
        stage.Player.Render(r, art);
        foreach (var s in stage.PlayerShots) s.Render(r, art);
        foreach (var s in stage.EnemyShots) s.Render(r, art);
        foreach (var fx in stage.Effects) fx.Render(r, art);
        r.PopCamera();

        DrawBackdrop(r, theme, cam.Position, stage.Time, foreground: true);
        Hud(r, stage);
    }

    public void DrawBackdrop(IRenderer r, string theme, Vector2 camera, float time, bool foreground)
    {
        var layers = art.Backdrop(theme);
        if (!foreground && layers[0].Fill.A > 0) r.FillRect(new RectF(0, 0, r.Width, layers[0].Y + 1), layers[0].Fill);
        foreach (var l in layers)
        {
            if (l.Foreground != foreground) continue;
            new ParallaxLayer(l.Texture) { Factor = l.Factor, Y = l.Y, Tint = foreground ? Color.White.WithAlpha(0.55f) : Color.White }.Draw(r, camera, time);
        }
    }

    private void DrawUndersides(IRenderer r, TileLayer layer, Camera2D cam)
    {
        var view = cam.View;
        int x0 = Math.Max(0, (int)(view.Left / Tiles.Size) - 1), x1 = Math.Min(layer.Width - 1, (int)(view.Right / Tiles.Size) + 1);
        for (int y = 1; y < layer.Height; y++)
        for (int x = x0; x <= x1; x++)
            if (Tiles.Underside(art, "lab", layer, x, y) is { } s) r.Draw(s, new Vector2(x * Tiles.Size, y * Tiles.Size));
    }

    private static void DrawLavaGlow(IRenderer r, Stage stage)
    {
        var layer = stage.Def.Layer;
        var view = stage.Camera.View;
        r.SetBlendMode(BlendMode.Additive);
        for (int x = Math.Max(0, (int)(view.Left / Tiles.Size)); x <= Math.Min(layer.Width - 1, (int)(view.Right / Tiles.Size)); x++)
        for (int y = 1; y < layer.Height; y++)
        {
            if (layer[x, y] != TileId.Lava || layer[x, y - 1] == TileId.Lava) continue;
            float pulse = 0.25f + 0.12f * MathF.Sin(stage.Time * 3 + x * 0.7f);
            r.FillRect(new RectF(x * Tiles.Size, y * Tiles.Size - 10, Tiles.Size, 10), Color.Hex("ff6a1a").WithAlpha(pulse * 0.5f));
            r.FillRect(new RectF(x * Tiles.Size, y * Tiles.Size - 4, Tiles.Size, 4), Color.Hex("ffb03a").WithAlpha(pulse));
        }
        r.SetBlendMode(BlendMode.Alpha);
    }

    private void Hud(IRenderer r, Stage stage)
    {
        var session = stage.Session;
        r.FillGradient(new RectF(0, 0, r.Width, 20), Ui.Ink.WithAlpha(0.7f), Ui.Ink.WithAlpha(0f), 5);
        // Lives as small medals, weapon as its capsule.
        for (int i = 0; i < Math.Min(session.Lives, 8); i++)
        {
            var at = new Vector2(10 + i * 9, 9);
            r.FillRect(new RectF(at.X - 3, at.Y - 5, 6, 7), Ui.Magenta);
            r.FillRect(new RectF(at.X - 2, at.Y - 4, 4, 5), Ui.Gold);
            r.FillRect(new RectF(at.X - 1, at.Y + 2, 2, 3), Ui.Magenta);
        }
        if (session.Lives > 8) Ui.Label(r, font, "+" + (session.Lives - 8), new Vector2(84, 5));
        // Health for the current life: three cells under the medals.
        for (int i = 0; i < Player.MaxHealth; i++)
        {
            var cell = new RectF(7 + i * 12, 17, 10, 4);
            r.FillRect(cell.Inflate(1), Ui.Ink);
            r.FillRect(cell, i < stage.Player.Health && stage.Player.State == PlayerState.Alive ? Ui.Cyan : Color.Hex("2a2440"));
        }
        var weapon = stage.Player.Weapon;
        Capsule.DrawPod(r, art, new Vector2(112, 11), weapon.Letter, stage.Time);
        Ui.Label(r, font, weapon.Label, new Vector2(126, 6), Ui.Cyan);
        Ui.Centered(r, font, Ui.N(session.Score, 7), r.Width / 2f, 6, Ui.Text);
        Ui.Label(r, font, "HI " + Ui.N(Math.Max(session.HighScore, 30000), 7), new Vector2(r.Width - 92, 6), Ui.Muted);

        if (stage.Player.Barrier > 0)
        {
            float w = 60 * stage.Player.Barrier / Player.BarrierTime;
            r.FillRect(new RectF(110, 22, 60, 3), Ui.Ink.WithAlpha(0.7f));
            r.FillRect(new RectF(110, 22, w, 3), Color.Hex("9a7aff"));
        }

        if (stage.Phase == StagePhase.Warning)
        {
            bool on = (int)(stage.PhaseTime * 4) % 2 == 0;
            r.FillRect(new RectF(0, 110, r.Width, 40), Color.Hex("ff1040").WithAlpha(on ? 0.35f : 0.18f));
            r.FillRect(new RectF(0, 110, r.Width, 2), Color.Hex("ff3860"));
            r.FillRect(new RectF(0, 148, r.Width, 2), Color.Hex("ff3860"));
            Ui.Centered(r, font, "WARNING", r.Width / 2f, 116, on ? Color.White : Color.Hex("ffb0c0"), 2);
            Ui.Centered(r, font, "HOSTILE CRAFT APPROACHING", r.Width / 2f, 136, Ui.Gold);
        }
        if (stage.Boss is { } boss && stage.Phase == StagePhase.Boss)
        {
            var bar = new RectF(r.Width / 2f - 110, r.Height - 16, 220, 6);
            Ui.Centered(r, font, boss.Title, r.Width / 2f, r.Height - 27, Ui.Gold);
            r.FillRect(bar.Inflate(1), Ui.Ink);
            r.FillRect(bar, Color.Hex("3a1030"));
            r.FillRect(bar with { Width = bar.Width * (boss.Entering ? Math.Min(1, boss.Age / 2f) : boss.HealthFraction) }, Ui.Magenta);
            r.FillRect(bar with { Height = 2 }, Color.White.WithAlpha(0.25f));
        }
        if (stage.Phase == StagePhase.Cleared && stage.PhaseTime > 3.2f)
        {
            Ui.Neon(r, font, "STAGE CLEAR", r.Width / 2f, 100, 3, Ui.Cyan, stage.Time);
            Ui.Centered(r, font, "BONUS +10000", r.Width / 2f, 132, Ui.Gold);
        }
    }
}
