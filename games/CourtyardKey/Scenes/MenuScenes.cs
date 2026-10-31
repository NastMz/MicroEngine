using System.Numerics;
using CourtyardKey.Model;
using CourtyardKey.UI;
using CourtyardKey.Visuals;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace CourtyardKey.Scenes;

/// <summary>Pause: inventory, quest status and a map of visited rooms. Resume or save and quit.</summary>
public sealed class PauseScene : Scene
{
    private readonly PlayScene _play;
    private readonly SelectionList _menu = new("Resume", "Save & Quit");

    public PauseScene(PlayScene play)
    {
        _play = play;
        _menu.Moved += () => Audio.Play("menu_move");
    }

    public override bool IsOverlay => true;

    public override void Update(float dt)
    {
        if (Input.Pressed("pause") || Input.Pressed("cancel"))
        {
            Audio.Play("menu_back");
            Game.Scenes.Pop();
            return;
        }
        if (_menu.Update(Input) is { } choice)
        {
            Audio.Play("menu_ok");
            if (choice == 0) Game.Scenes.Pop();
            else
            {
                _play.SaveGame(toast: false);
                Audio.StopMusic(0.5f);
                Game.Scenes.Reset(new TitleScene(_play.G), Transition.Fade(0.8f));
            }
        }
    }

    public override void Draw(IRenderer r)
    {
        r.FillRect(new RectF(0, 0, r.Width, r.Height), Pal.Ink.WithAlpha(0.6f));
        var art = _play.Art;
        var p = _play.Progress;
        // Inventory panel
        var inv = new RectF(10, 26, 136, 146);
        UiStyle.Frame(r, inv);
        Font.DrawShadowed(r, "INVENTORY", new Vector2(inv.X + 10, inv.Y + 7), Pal.Gold[3], Pal.Ink);
        (string Sprite, string Label, bool Owned)[] items =
        [
            ("sword", "Oswin's Sword", p.HasSword),
            ("boomerang", "Wind Boomerang", p.HasBoomerang),
            ("key_boss", "Warden's Key", p.HasBossKey),
            ("courtyard_key", "Courtyard Key", p.HasCourtyardKey),
        ];
        for (int i = 0; i < items.Length; i++)
        {
            var slot = new RectF(inv.X + 10, inv.Y + 20 + i * 21, 18, 18);
            r.FillRect(slot, Pal.Ink);
            r.DrawRect(slot, items[i].Owned ? Pal.Gold[1] : Pal.Stone[2]);
            if (items[i].Owned)
            {
                var s = art[items[i].Sprite];
                r.DrawPivoted(s, slot.Center, 0, new Vector2(s.Height > 15 ? 0.85f : 1f));
            }
            Font.DrawShadowed(r, items[i].Owned ? items[i].Label : "???", new Vector2(slot.Right + 6, slot.Y + 5), items[i].Owned ? Pal.White : Pal.Stone[3], Pal.Ink);
        }
        float y = inv.Y + 108;
        r.DrawPivoted(art["gem_green"], new Vector2(inv.X + 16, y + 4));
        Font.DrawShadowed(r, $"{p.Gems} gems", new Vector2(inv.X + 26, y), Pal.White, Pal.Ink);
        r.DrawPivoted(art["key_small"], new Vector2(inv.X + 84, y + 4));
        Font.DrawShadowed(r, $"×{p.SmallKeys}", new Vector2(inv.X + 92, y), Pal.White, Pal.Ink);
        var time = TimeSpan.FromSeconds(p.PlaySeconds);
        Font.Draw(r, $"Time {(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}   Falls {p.Deaths}", new Vector2(inv.X + 10, y + 16), Pal.Stone[4]);

        // Map panel
        var map = new RectF(154, 26, 156, 104);
        UiStyle.Frame(r, map);
        var area = _play.Area.Def;
        Font.DrawShadowed(r, area.Name.ToUpperInvariant(), new Vector2(map.X + 10, map.Y + 7), Pal.Gold[3], Pal.Ink);
        MapView.Draw(r, _play, new RectF(map.X + 10, map.Y + 20, map.Width - 20, map.Height - 28), Age);

        // Menu
        var menu = new RectF(154, 136, 156, 36);
        UiStyle.Frame(r, menu);
        _menu.Draw(r, Font, new Vector2(menu.X + 8, menu.Y + 7), UiStyle.Theme, Age, 12);
        Font.Draw(r, "Esc: back", new Vector2(menu.Right - 48, menu.Y + 7), Pal.Stone[3]);
    }
}

/// <summary>Draws an area's room grid: visited rooms with their open edges, the current room blinking.</summary>
public static class MapView
{
    public static void Draw(IRenderer r, PlayScene play, RectF bounds, float time)
    {
        var area = play.Area.Def;
        float cw = MathF.Floor(MathF.Min(bounds.Width / area.Width, bounds.Height / area.Height * 2f));
        float ch = MathF.Floor(cw / 2f);
        float ox = MathF.Round(bounds.X + (bounds.Width - cw * area.Width) / 2), oy = MathF.Round(bounds.Y + (bounds.Height - ch * area.Height) / 2);
        for (int x = 0; x < area.Width; x++)
            for (int y = 0; y < area.Height; y++)
            {
                if (area.Room(x, y) is null) continue;
                var cell = new RectF(ox + x * cw + 1, oy + y * ch + 1, cw - 2, ch - 2);
                bool visited = play.Progress.WasVisited(area.Id, x, y);
                bool current = play.Room.X == x && play.Room.Y == y;
                if (!visited) { if (!area.IsDungeon) r.DrawRect(cell, Pal.Stone[2]); continue; }
                var fill = area.IsDungeon ? Pal.Stone[3] : Pal.Grass[2];
                if (area.Room(x, y)!.Rule == Content.RoomRule.Boss) fill = Pal.Carpet[2];
                r.FillRect(cell, fill);
                r.DrawRect(cell, area.IsDungeon ? Pal.Stone[5] : Pal.Grass[4]);
                if (current)
                {
                    var head = play.Art["hero_down0"];
                    r.Draw(head, cell.Center + new Vector2(0, 7 + ((int)(time * 3) % 2 == 0 ? 0 : -1)));
                }
            }
    }
}

/// <summary>Marla's shop: a tonic that refills hearts and a one-time pie that adds a heart.</summary>
public sealed class ShopScene : Scene
{
    public const int TonicPrice = 10, PiePrice = 60;
    public const string PieFlag = "shop:pie";
    private readonly PlayScene _play;
    private readonly SelectionList _menu = new("Heart Tonic", "Hearty Pie", "Leave");
    private string _message = "What'll it be, dear?";

    public ShopScene(PlayScene play)
    {
        _play = play;
        _menu.Moved += () => Audio.Play("menu_move");
    }

    public override bool IsOverlay => true;

    public override void Update(float dt)
    {
        _menu.SetEnabled(1, !_play.Progress.Has(PieFlag));
        if (Input.Pressed("cancel")) { Close(); return; }
        if (_menu.Update(Input) is not { } choice) return;
        switch (choice)
        {
            case 0: _message = BuyTonic(_play); break;
            case 1: _message = BuyPie(_play); break;
            default: Close(); break;
        }
    }

    private void Close()
    {
        Audio.Play("menu_back");
        Game.Scenes.Pop();
    }

    /// <summary>Shop rules are static so tests can exercise them without the menu.</summary>
    public static string BuyTonic(PlayScene play)
    {
        var hp = play.Hero.Health;
        if (hp.Current >= hp.Max) { play.Audio.Play("menu_back"); return "You look perfectly hale already!"; }
        if (!play.Progress.TrySpend(TonicPrice)) { play.Audio.Play("menu_back"); return "Not enough gems, I'm afraid."; }
        hp.Restore();
        play.Audio.Play("heart");
        return "Drink up! Good as new.";
    }

    public static string BuyPie(PlayScene play)
    {
        if (play.Progress.Has(PieFlag)) return "That was my last pie!";
        if (!play.Progress.TrySpend(PiePrice)) { play.Audio.Play("menu_back"); return "Not enough gems, I'm afraid."; }
        play.Progress.Set(PieFlag);
        play.Hero.Health.RaiseMax(2, fill: true);
        play.Progress.MaxHealth = play.Hero.Health.Max;
        play.Audio.Play("fanfare");
        return "A hearty pie! You feel a whole heart stronger.";
    }

    public override void Draw(IRenderer r)
    {
        r.FillRect(new RectF(0, 0, r.Width, r.Height), Pal.Ink.WithAlpha(0.45f));
        var panel = new RectF(40, 40, 240, 104);
        UiStyle.Frame(r, panel);
        Font.DrawShadowed(r, "MARLA'S PROVISIONS", new Vector2(panel.X + 10, panel.Y + 8), Pal.Gold[3], Pal.Ink);
        var art = _play.Art;
        string[] icons = ["tonic", "pie", ""];
        string[] prices = [$"{TonicPrice}", _play.Progress.Has(PieFlag) ? "SOLD" : $"{PiePrice}", ""];
        string[] notes = ["Full heal", "+1 max heart", ""];
        for (int i = 0; i < 3; i++)
        {
            float y = panel.Y + 26 + i * 18;
            if (icons[i].Length > 0) r.DrawPivoted(art[icons[i]], new Vector2(panel.X + 16, y + 4));
            if (prices[i].Length > 0)
            {
                r.DrawPivoted(art["gem_green"], new Vector2(panel.Right - 40, y + 4));
                Font.DrawShadowed(r, prices[i], new Vector2(panel.Right - 32, y), Pal.White, Pal.Ink);
                Font.Draw(r, notes[i], new Vector2(panel.X + 100, y), Pal.Stone[4]);
            }
        }
        _menu.Draw(r, Font, new Vector2(panel.X + 22, panel.Y + 26), UiStyle.Theme, Age, 18);
        r.FillRect(new RectF(panel.X + 8, panel.Bottom - 22, panel.Width - 16, 1), Pal.Gold[0]);
        Font.DrawShadowed(r, _message, new Vector2(panel.X + 10, panel.Bottom - 16), Pal.Cream, Pal.Ink);
        r.DrawPivoted(art["gem_green"], new Vector2(panel.Right - 40, panel.Y + 12));
        Font.DrawShadowed(r, $"{_play.Progress.Gems}", new Vector2(panel.Right - 32, panel.Y + 8), Pal.Gold[3], Pal.Ink);
    }
}

/// <summary>Game over: the world dims to crimson; continue from the last entrance or quit.</summary>
public sealed class GameOverScene : Scene
{
    private readonly PlayScene _play;
    private readonly SelectionList _menu = new("Continue", "Save & Quit");

    public GameOverScene(PlayScene play)
    {
        _play = play;
        _menu.Moved += () => Audio.Play("menu_move");
    }

    public override bool IsOverlay => true;

    public override void Update(float dt)
    {
        if (Age < 0.8f) return;
        if (_menu.Update(Input) is not { } choice) return;
        Audio.Play("menu_ok");
        if (choice == 0)
        {
            Game.Scenes.Pop();
            _play.Continue();
        }
        else
        {
            _play.Progress.Deaths++;
            _play.Progress.Health = _play.Hero.Health.Max;
            _play.G.Saves.Save(_play.Progress);
            Game.Scenes.Reset(new TitleScene(_play.G), Transition.Fade(0.8f));
        }
    }

    public override void Draw(IRenderer r)
    {
        float a = MathF.Min(1, Age / 0.8f);
        r.FillRect(new RectF(0, 0, r.Width, r.Height), Color.Hex("2a0812").WithAlpha(a * 0.82f));
        Font.DrawCentered(r, "YOU FELL", new Vector2(160, 62), Pal.Heart[2].WithAlpha(a), 3, Pal.Ink);
        Font.DrawCentered(r, "The courtyard keeps its secrets... for now.", new Vector2(160, 88), Pal.Cream.WithAlpha(a), 1, Pal.Ink);
        if (Age >= 0.8f) _menu.Draw(r, Font, new Vector2(128, 112), UiStyle.Theme, Age, 13);
    }
}
