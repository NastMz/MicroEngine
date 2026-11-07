using System.Numerics;
using CourtyardKey.Scenes;
using CourtyardKey.Visuals;
using MicroEngine;
using MicroEngine.Graphics;

namespace CourtyardKey.UI;

/// <summary>
/// The top bar (hearts, gems, keys, equipped items) plus transient messages: toasts, the
/// area banner, the room-name tag and full-screen flashes.
/// </summary>
/// <remarks>
/// The messages keep their own timers instead of the game's achievement banners: those are two-line panels that slide in
/// for 3.4 s, while the HUD's are a framed line low on the screen, a large centred banner and a tag under the bar, each
/// with its own length and fade. Only the room tag defers to the achievement banners, which use its place.
/// </remarks>
public sealed class Hud(PlayScene world)
{
    private string? _toast, _banner;
    private float _toastTime, _bannerTime, _roomTime, _flashTime, _flashDuration;
    private Color _flash;
    private int _shownGems;
    private float _gemTick;
    private static readonly Color BarTop = Color.Hex("2a2244"), SlotHighlight = Color.Hex("3a3058");
    private readonly NumberText _gemsText = new(n => $"{n:000}");
    private readonly NumberText _keysText = new(n => $"×{n}");
    private CachedPrompt? _confirmKey, _actionKey;

    public string? LastRoomName { get; private set; }

    public void Toast(string text) { _toast = text; _toastTime = 2.4f; }
    public void Banner(string text) { _banner = text; _bannerTime = 2.6f; }
    public void RoomName(string text) { LastRoomName = text; _roomTime = 2.2f; }
    public void Flash(Color color, float seconds) { _flash = color; _flashTime = _flashDuration = seconds; }

    public void Update(float dt)
    {
        _toastTime -= dt;
        _bannerTime -= dt;
        if (world.G.Banners.Current is null) _roomTime -= dt;
        _flashTime -= dt;
        // Gem counter rolls toward the real total, like a till.
        int target = world.Progress.Gems;
        if (_shownGems != target && (_gemTick -= dt) <= 0)
        {
            _gemTick = 0.025f;
            _shownGems += Math.Sign(target - _shownGems) * Math.Max(1, Math.Abs(target - _shownGems) / 12);
        }
    }

    public void Draw(IRenderer r)
    {
        var font = world.Font;
        var art = world.Art;
        var p = world.Progress;
        r.FillGradient(new RectF(0, 0, r.Width, PlayScene.HudHeight), BarTop, Pal.Ink, 5);
        r.FillRect(new RectF(0, PlayScene.HudHeight - 2, r.Width, 1), Pal.Gold[1]);
        r.FillRect(new RectF(0, PlayScene.HudHeight - 1, r.Width, 1), Pal.Ink);

        // Hearts: full, half or empty, with a gentle pulse on the last heart when low.
        var health = world.Hero.Health;
        int hearts = health.Max / 2;
        for (int i = 0; i < hearts; i++)
        {
            int value = health.Current - i * 2;
            string sprite = value >= 2 ? "heart" : value == 1 ? "heart_half" : "heart_empty";
            float scale = health.Current <= 2 && value > 0 ? 1 + MathF.Max(0, MathF.Sin(world.Age * 9)) * 0.25f : 1;
            r.DrawPivoted(art[sprite], new Vector2(10 + i * 9, 9), 0, new Vector2(scale));
        }

        float x = 112;
        r.DrawPivoted(art["gem_green"], new Vector2(x, 9));
        font.DrawShadowed(r, _gemsText.For(_shownGems), new Vector2(x + 6, 5), Pal.White, Pal.Ink);
        if (world.Area.Def.IsDungeon)
        {
            r.DrawPivoted(art["key_small"], new Vector2(x + 34, 9));
            font.DrawShadowed(r, _keysText.For(p.SmallKeys), new Vector2(x + 40, 5), Pal.White, Pal.Ink);
            if (p.HasBossKey) r.DrawPivoted(art["key_boss"], new Vector2(x + 64, 9));
        }
        // The room name waits while an achievement banner covers its place.
        if (_roomTime > 0 && LastRoomName is { } place && world.G.Banners.Current is null)
        {
            float a = Math.Clamp(MathF.Min(_roomTime, 2.2f - _roomTime) * 4, 0, 1);
            int w = font.MeasureWidth(place) + 14;
            var tag = new RectF(6, PlayScene.HudHeight + 5, w, 13);
            r.FillRect(tag, Pal.Ink.WithAlpha(a * 0.7f));
            r.FillRect(new RectF(tag.X, tag.Bottom - 1, tag.Width, 1), Pal.Gold[1].WithAlpha(a));
            font.DrawShadowed(r, place, new Vector2(tag.X + 7, tag.Y + 3), Pal.Gold[3].WithAlpha(a), Pal.Ink.WithAlpha(a));
        }

        // Each slot names the control that uses it on the device being played.
        _confirmKey ??= CachedPrompt.ForAction(world.G.Prompts, "confirm");
        _actionKey ??= CachedPrompt.ForAction(world.G.Prompts, "action");
        ItemBox(r, font, art, new Vector2(258, 2), _confirmKey.Text, p.HasSword ? art["sword"] : null);
        ItemBox(r, font, art, new Vector2(286, 2), _actionKey.Text, p.HasBoomerang ? art["boomerang"] : null);

        if (world.Boss is { Alive: true } boss && boss.State != Entities.WardenState.Intro)
        {
            var bar = new RectF(112, PlayScene.HudHeight + 14, 140, 7);
            // A dark plate keeps the name and bar readable over torches and the arena wall.
            r.FillRect(new RectF(bar.X - 5, bar.Y - 13, bar.Width + 10, bar.Height + 18), Pal.Ink.WithAlpha(0.7f));
            r.FillRect(bar.Inflate(2), Pal.Ink);
            r.FillRect(bar.Inflate(1), Pal.Gold[0]);
            r.FillRect(bar, Pal.Night);
            float fill = MathF.Max(0, boss.Health.Current) / (float)Entities.Warden.MaxHealth;
            var color = boss.Enraged ? Pal.Ember[2] : Pal.Spirit[2];
            r.FillRect(new RectF(bar.X, bar.Y, MathF.Round(bar.Width * fill), bar.Height), color);
            r.FillRect(new RectF(bar.X, bar.Y, MathF.Round(bar.Width * fill), 2), Color.Lerp(color, Pal.White, 0.5f));
            font.DrawShadowed(r, "MOSS WARDEN", new Vector2(bar.X, bar.Y - 10), Pal.Cream, Pal.Ink);
        }
        if (_bannerTime > 0 && _banner is not null)
        {
            float a = Math.Clamp(MathF.Min(_bannerTime, 2.6f - _bannerTime) * 3, 0, 1);
            var size = font.Measure(_banner, 2);
            var center = new Vector2(160, 62);
            r.FillRect(new RectF(0, center.Y - 12, r.Width, 24), Pal.Ink.WithAlpha(a * 0.6f));
            r.FillRect(new RectF(160 - size.X / 2 - 20, center.Y - 12, size.X + 40, 1), Pal.Gold[2].WithAlpha(a));
            r.FillRect(new RectF(160 - size.X / 2 - 20, center.Y + 11, size.X + 40, 1), Pal.Gold[2].WithAlpha(a));
            font.DrawCentered(r, _banner, center, Pal.Gold[3].WithAlpha(a), 2, Pal.Ink.WithAlpha(a));
        }
        if (_toastTime > 0 && _toast is not null)
        {
            int w = font.MeasureWidth(_toast) + 16;
            var rect = new RectF(MathF.Round(160 - w / 2f), 158, w, 16);
            UiStyle.Frame(r, rect);
            font.DrawShadowed(r, _toast, new Vector2(rect.X + 8, rect.Y + 4), Pal.White, Pal.Ink);
        }
        if (_flashTime > 0) r.FillRect(new RectF(0, 0, r.Width, r.Height), _flash.WithAlpha(_flashTime / _flashDuration * 0.85f));
    }

    private static void ItemBox(IRenderer r, BitmapFont font, Sprites art, Vector2 at, string key, Sprite? item)
    {
        var rect = new RectF(at.X, at.Y, 22, 15);
        r.FillRect(rect, Pal.Ink);
        r.DrawRect(rect, Pal.Gold[1]);
        r.FillRect(new RectF(rect.X + 1, rect.Y + 1, rect.Width - 2, 1), SlotHighlight);
        if (item is not null)
        {
            float scale = item.Height > 12 ? 0.8f : 1f;
            r.DrawPivoted(item, new Vector2(rect.X + 13, rect.Y + 8), item.Height > 12 ? 0.5f : 0, new Vector2(scale));
        }
        r.FillRect(new RectF(rect.X - 3, rect.Y + 6, 9, 10), Pal.Ink);
        r.FillRect(new RectF(rect.X - 2, rect.Y + 7, 7, 8), Pal.Gold[2]);
        font.Draw(r, key, new Vector2(rect.X - 1, rect.Y + 7), Pal.Ink);
    }
}
