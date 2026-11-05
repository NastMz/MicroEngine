using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.UI;
using TrailDuel.Rules;
using TrailDuel.Ui;
using TrailDuel.Visuals;

namespace TrailDuel.Scenes;

/// <summary>The pause menu over the overworld: party, bag, trail log, save, controls.</summary>
internal sealed class StartMenuScene(TrailDuelGame game) : Scene
{
    private CachedText _timeText;
    private static readonly string[] Options = ["CREATURES", "BAG", "TRAIL LOG", "SAVE", "CONTROLS", "CLOSE"];
    private SelectionList _list = null!;
    private string? _message;
    private float _messageTime, _open;

    public override bool IsOverlay => true;

    public override void Enter() => _list = UiKit.List(Audio, Options);

    public override void Update(float dt)
    {
        _open = MathF.Min(1, _open + dt * 8);
        _messageTime -= dt;
        if (Input.Pressed("cancel") || Input.Pressed("pause")) { Audio.Play("cancel", 0.7f); Game.Scenes.Pop(); return; }
        if (_list.Update(Input) is not { } chosen) return;
        Audio.Play("confirm", 0.7f);
        switch (chosen)
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

    /// <summary>State hash: the cursor and the message with its timer. Left out: the opening slide and the play-time text cache.</summary>
    public override void HashState(StateHasher hash)
    {
        hash.Add(_list.Index);
        hash.Add(_message);
        hash.Add(_messageTime);
    }

    public override void Draw(IRenderer r)
    {
        r.FillRect(new RectF(0, 0, 320, 180), Pal.Ink.WithAlpha(0.25f * _open));
        var box = new RectF(MathF.Round(222 + (1 - _open) * 110), 6, 92, Options.Length * 14 + 12);
        UiKit.Box(r, box);
        for (int i = 0; i < Options.Length; i++)
        {
            float y = box.Y + 8 + i * 14;
            UiKit.Text(r, Font, Options[i], box.X + 16, y);
            if (i == _list.Index) UiKit.Cursor(r, Font, box.X + 6, y, Age);
        }
        var s = game.State;
        var info = new RectF(6, 6, 124, 46);
        UiKit.Box(r, info);
        UiKit.Text(r, Font, GameState.PlayerName, info.X + 8, info.Y + 6, Pal.Coral);
        if (s.Has("master")) UiKit.TextRight(r, Font, "★ Champion", info.Right - 8, info.Y + 6, Pal.GoldDark);
        UiKit.Text(r, Font, "Money", info.X + 8, info.Y + 18, UiKit.Muted);
        UiKit.TextRight(r, Font, NumberTexts.Money(s.Money), info.Right - 8, info.Y + 18);
        var t = TimeSpan.FromSeconds(s.PlaySeconds);
        UiKit.Text(r, Font, "Time", info.X + 8, info.Y + 30, UiKit.Muted);
        UiKit.TextRight(r, Font, _timeText.Get((int)t.TotalHours, t.Minutes, static (h, m) => $"{h}:{m:00}"), info.Right - 8, info.Y + 30);
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
        // The controls of the device being played: keys, a gamepad's buttons, or the on-screen controls.
        var p = Game.Prompts;
        var device = p.Device;
        var rows = new List<(string Key, string What)>
        {
            (p.Directions(separator: " / "), "Walk, move cursor"),
            (p.TextAll("confirm"), "Confirm, talk, read"),
            (p.TextAll("cancel"), "Back. Hold to run"),
            (p.TextAll("pause"), "Open the menu"),
        };
        if (device == InputDevice.Keyboard) rows.Add(("F11", "Toggle fullscreen"));
        rows.Add(("Tap a direction", "Turn in place"));
        rows.Add((p.TextAll("action"), "Move details in battle"));
        if (device == InputDevice.Touch) rows.Add(("Keyboard, gamepad", "Work too"));
        // The description column starts after the widest key text, so long key lists never run into it.
        float what = box.X + 14 + Math.Max(106, rows.Max(row => Font.MeasureWidth(row.Key)) + 12);
        for (int i = 0; i < rows.Count; i++)
        {
            float y = box.Y + 28 + i * 16;
            UiKit.Text(r, Font, rows[i].Key, box.X + 14, y);
            UiKit.Text(r, Font, rows[i].What, what, y, UiKit.Muted);
        }
    }
}

/// <summary>The Trail Log: every species, marked seen or caught.</summary>
internal sealed class TrailLogScene(TrailDuelGame game) : Scene
{
    private static readonly string?[] DexNumbers = new string?[1000];
    private CachedText _seenText;

    /// <summary>The dex number as three digits, built once per number.</summary>
    private static string DexNumber(int n) => (uint)n < (uint)DexNumbers.Length ? DexNumbers[n] ??= $"{n:000}" : $"{n:000}";
    /// <summary>One row per species; confirm picks nothing here, the list only browses.</summary>
    private SelectionList _list = null!;

    public override void Enter() => _list = UiKit.List(Audio, [.. SpeciesDex.All.Select(s => s.Id)]);

    /// <summary>State hash: the cursor. Left out: the seen/caught text cache.</summary>
    public override void HashState(StateHasher hash) => hash.Add(_list.Index);

    public override void Update(float dt)
    {
        if (Input.Pressed("cancel") || Input.Pressed("pause")) { Audio.Play("cancel", 0.7f); Game.Scenes.Pop(); return; }
        _list.Update(Input);
    }

    public override void Draw(IRenderer r)
    {
        var s = game.State;
        r.FillGradient(new RectF(0, 0, 320, 180), Color.Hex("e88a6a"), Color.Hex("f8dcc0"), 10);
        var list = new RectF(176, 6, 138, 168);
        UiKit.Box(r, list);
        int start = Math.Clamp(_list.Index - 5, 0, Math.Max(0, SpeciesDex.All.Count - 11));
        for (int i = start; i < Math.Min(SpeciesDex.All.Count, start + 11); i++)
        {
            var sp = SpeciesDex.All[i];
            float y = list.Y + 8 + (i - start) * 14;
            bool seen = s.Seen.Contains(sp.Id), caught = s.Caught.Contains(sp.Id);
            UiKit.Text(r, Font, DexNumber(i + 1), list.X + 16, y, UiKit.Muted);
            UiKit.Text(r, Font, seen ? sp.Name : "-----", list.X + 40, y);
            if (caught) r.Draw(game.Sprite("orbsmall"), new Vector2(list.Right - 12, y + 3));
            if (i == _list.Index) UiKit.Cursor(r, Font, list.X + 5, y, Age);
        }
        var sel = SpeciesDex.All[_list.Index];
        var card = new RectF(6, 6, 166, 120);
        UiKit.Box(r, card);
        bool known = s.Seen.Contains(sel.Id);
        r.Draw(game.Sprite("platform:enemy"), new Vector2(card.Center.X, card.Y + 84));
        r.Draw(game.Sprite(SpriteKeys.Front[sel.Id]), new Vector2(card.Center.X, card.Y + 84), known ? Color.White : Pal.Ink.WithAlpha(0.85f));
        UiKit.Text(r, Font, known ? sel.Name : "???", card.X + 8, card.Y + 96);
        if (known) UiKit.TypeBadge(r, Font, sel.Type, card.Right - 8 - UiKit.TypeBadgeWidth(Font, sel.Type), card.Y + 95);
        UiKit.Text(r, Font, _seenText.Get(s.Seen.Count, s.Caught.Count, static (seen, caught) => $"Seen {seen}  Caught {caught}"), card.X + 8, card.Y + 108, UiKit.Muted);
        var desc = new RectF(6, 130, 166, 44);
        UiKit.Box(r, desc);
        var lines = Font.Wrap(known ? sel.Description : "Not yet seen on the trail.", 150);
        for (int i = 0; i < Math.Min(3, lines.Count); i++) UiKit.Text(r, Font, lines[i], desc.X + 8, desc.Y + 6 + i * 11);
    }
}
