using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using TrailDuel.Rules;
using TrailDuel.Ui;
using TrailDuel.Visuals;

namespace TrailDuel.Scenes;

/// <summary>The pause menu over the overworld: party, bag, trail log, save, controls.</summary>
internal sealed class StartMenuScene(TrailDuelGame game) : Scene
{
    private static readonly string[] Options = ["CREATURES", "BAG", "TRAIL LOG", "SAVE", "CONTROLS", "CLOSE"];
    private int _index;
    private string? _message;
    private float _messageTime, _open;

    public override bool IsOverlay => true;

    public override void Update(float dt)
    {
        _open = MathF.Min(1, _open + dt * 8);
        _messageTime -= dt;
        if (Input.Pressed("cancel") || Input.Pressed("pause")) { Audio.Play("cancel", 0.7f); Game.Scenes.Pop(); return; }
        int dir = (Input.Pressed("down") ? 1 : 0) - (Input.Pressed("up") ? 1 : 0);
        if (dir != 0) { _index = (_index + dir + Options.Length) % Options.Length; Audio.Play("move", 0.6f); }
        if (!Input.Pressed("confirm")) return;
        Audio.Play("confirm", 0.7f);
        switch (_index)
        {
            case 0:
                if (game.State.Party.Count == 0) { Say("You don't have any creatures yet."); Audio.Play("deny"); break; }
                Game.Scenes.Push(new PartyScene(game, PartyScene.Purpose.Field));
                break;
            case 1: Game.Scenes.Push(new BagScene(game, null)); break;
            case 2: Game.Scenes.Push(new TrailLogScene(game)); break;
            case 3:
                game.SaveGame();
                Audio.Play("save");
                Say("Progress saved! Rest easy.");
                break;
            case 4: Game.Scenes.Push(new ControlsScene()); break;
            default: Game.Scenes.Pop(); break;
        }
    }

    private void Say(string text) { _message = text; _messageTime = 2.2f; }

    public override void Draw(IRenderer r)
    {
        r.FillRect(new RectF(0, 0, 320, 180), Pal.Ink.WithAlpha(0.25f * _open));
        var box = new RectF(MathF.Round(222 + (1 - _open) * 110), 6, 92, Options.Length * 14 + 12);
        UiKit.Box(r, box);
        for (int i = 0; i < Options.Length; i++)
        {
            float y = box.Y + 8 + i * 14;
            UiKit.Text(r, Font, Options[i], box.X + 16, y);
            if (i == _index) UiKit.Cursor(r, Font, box.X + 6, y, Age);
        }
        var s = game.State;
        var info = new RectF(6, 6, 124, 46);
        UiKit.Box(r, info);
        UiKit.Text(r, Font, GameState.PlayerName, info.X + 8, info.Y + 6, Pal.Coral);
        if (s.Has("master")) UiKit.TextRight(r, Font, "★ Champion", info.Right - 8, info.Y + 6, Pal.GoldDark);
        UiKit.Text(r, Font, "Money", info.X + 8, info.Y + 18, UiKit.Muted);
        UiKit.TextRight(r, Font, $"${s.Money}", info.Right - 8, info.Y + 18);
        var t = TimeSpan.FromSeconds(s.PlaySeconds);
        UiKit.Text(r, Font, "Time", info.X + 8, info.Y + 30, UiKit.Muted);
        UiKit.TextRight(r, Font, $"{(int)t.TotalHours}:{t.Minutes:00}", info.Right - 8, info.Y + 30);
        if (_messageTime > 0 && _message is not null)
        {
            var m = new RectF(4, 150, 312, 26);
            UiKit.Box(r, m);
            UiKit.Text(r, Font, _message, m.X + 10, m.Y + 9);
        }
        else
        {
            var hint = new RectF(4, 150, 312, 26);
            UiKit.Box(r, hint);
            UiKit.Text(r, Font, Objective(s), hint.X + 10, hint.Y + 9);
        }
    }

    /// <summary>A one-line reminder of what to do next.</summary>
    public static string Objective(GameState s)
    {
        if (!s.Has("starter")) return "Goal: visit Mentor Ilse's lab in Sprout Town.";
        if (!s.Has("rival2")) return "Goal: travel north through Route 1 and Hollow Wood.";
        if (!s.Has("master")) return "Goal: challenge Master Odessa in Brookhaven's hall.";
        return "You are the Trail Champion! Complete your Trail Log.";
    }
}

/// <summary>Key bindings reference.</summary>
internal sealed class ControlsScene : Scene
{
    public override void Update(float dt)
    {
        if (Input.Pressed("cancel") || Input.Pressed("confirm") || Input.Pressed("pause")) { Audio.Play("cancel", 0.7f); Game.Scenes.Pop(); }
    }

    public override void Draw(IRenderer r)
    {
        r.FillGradient(new RectF(0, 0, 320, 180), Color.Hex("b8a6d8"), Color.Hex("ece4f6"), 10);
        var box = new RectF(30, 18, 260, 144);
        UiKit.Box(r, box);
        UiKit.Text(r, Font, "CONTROLS", box.X + 12, box.Y + 10, Pal.Coral);
        // On a touch screen the on-screen controls are explained instead of the keys.
        (string Key, string What)[] rows = Game.Touch.Shown
        ?
        [
            ("D-pad", "Walk, move cursor"),
            ("A", "Confirm, talk, read"),
            ("B", "Back. Hold to run"),
            ("MENU", "Open the menu"),
            ("Tap a direction", "Turn in place"),
            ("C", "Move details in battle"),
            ("Keyboard, gamepad", "Work too"),
        ]
        :
        [
            ("Arrows / WASD", "Walk, move cursor"),
            ("Z / Space / Enter", "Confirm, talk, read"),
            ("X / Backspace", "Back. Hold to run"),
            ("Esc / P", "Open the menu"),
            ("F11", "Toggle fullscreen"),
            ("Tap a direction", "Turn in place"),
            ("C", "Move details in battle"),
        ];
        for (int i = 0; i < rows.Length; i++)
        {
            float y = box.Y + 28 + i * 16;
            UiKit.Text(r, Font, rows[i].Key, box.X + 14, y);
            UiKit.Text(r, Font, rows[i].What, box.X + 120, y, UiKit.Muted);
        }
    }
}

/// <summary>The Trail Log: every species, marked seen or caught.</summary>
internal sealed class TrailLogScene(TrailDuelGame game) : Scene
{
    private int _index;

    public override void Update(float dt)
    {
        if (Input.Pressed("cancel") || Input.Pressed("pause")) { Audio.Play("cancel", 0.7f); Game.Scenes.Pop(); return; }
        int n = SpeciesDex.All.Count;
        int dir = (Input.Pressed("down") ? 1 : 0) - (Input.Pressed("up") ? 1 : 0);
        if (dir != 0) { _index = (_index + dir + n) % n; Audio.Play("move", 0.6f); }
    }

    public override void Draw(IRenderer r)
    {
        var s = game.State;
        r.FillGradient(new RectF(0, 0, 320, 180), Color.Hex("e88a6a"), Color.Hex("f8dcc0"), 10);
        var list = new RectF(176, 6, 138, 168);
        UiKit.Box(r, list);
        int start = Math.Clamp(_index - 5, 0, Math.Max(0, SpeciesDex.All.Count - 11));
        for (int i = start; i < Math.Min(SpeciesDex.All.Count, start + 11); i++)
        {
            var sp = SpeciesDex.All[i];
            float y = list.Y + 8 + (i - start) * 14;
            bool seen = s.Seen.Contains(sp.Id), caught = s.Caught.Contains(sp.Id);
            UiKit.Text(r, Font, $"{i + 1:000}", list.X + 16, y, UiKit.Muted);
            UiKit.Text(r, Font, seen ? sp.Name : "-----", list.X + 40, y);
            if (caught) r.Draw(game.Sprite("orbsmall"), new Vector2(list.Right - 12, y + 3));
            if (i == _index) UiKit.Cursor(r, Font, list.X + 5, y, Age);
        }
        var sel = SpeciesDex.All[_index];
        var card = new RectF(6, 6, 166, 120);
        UiKit.Box(r, card);
        bool known = s.Seen.Contains(sel.Id);
        r.Draw(game.Sprite("platform:enemy"), new Vector2(card.Center.X, card.Y + 84));
        r.Draw(game.Sprite("front:" + sel.Id), new Vector2(card.Center.X, card.Y + 84), known ? Color.White : Pal.Ink.WithAlpha(0.85f));
        UiKit.Text(r, Font, known ? sel.Name : "???", card.X + 8, card.Y + 96);
        if (known) UiKit.TypeBadge(r, Font, sel.Type, card.Right - 8 - UiKit.TypeBadgeWidth(Font, sel.Type), card.Y + 95);
        UiKit.Text(r, Font, $"Seen {s.Seen.Count}  Caught {s.Caught.Count}", card.X + 8, card.Y + 108, UiKit.Muted);
        var desc = new RectF(6, 130, 166, 44);
        UiKit.Box(r, desc);
        var lines = Font.Wrap(known ? sel.Description : "Not yet seen on the trail.", 150);
        for (int i = 0; i < Math.Min(3, lines.Count); i++) UiKit.Text(r, Font, lines[i], desc.X + 8, desc.Y + 6 + i * 11);
    }
}
