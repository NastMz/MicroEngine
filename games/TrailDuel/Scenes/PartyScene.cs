using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.UI;
using TrailDuel.Rules;
using TrailDuel.Ui;
using TrailDuel.Visuals;

namespace TrailDuel.Scenes;

/// <summary>
/// The party list: HP bars, status and a hopping icon for the highlighted creature. Used from the
/// start menu (summary, reorder), in battle (shift) and to pick an item target.
/// </summary>
internal sealed class PartyScene : Scene
{
    public enum Purpose { Field, BattleSwitch, ForcedSwitch, ItemTarget }

    private readonly TrailDuelGame _game;
    private readonly Purpose _purpose;
    private readonly Action<int>? _picked;
    private readonly int _activeIndex;
    private readonly string _prompt;
    /// <summary>One row per creature (null while the party is empty: a list needs at least one row).</summary>
    private SelectionList? _list;
    private int Index => _list?.Index ?? 0;
    private int _swapFrom = -1;
    private ChoiceMenu? _menu;
    private string? _message;
    private float _messageTime;
    private bool _closed;

    public PartyScene(TrailDuelGame game, Purpose purpose, Action<int>? picked = null, int activeIndex = -1, string? prompt = null)
    {
        _game = game;
        _purpose = purpose;
        _picked = picked;
        _activeIndex = activeIndex;
        _prompt = prompt ?? purpose switch
        {
            Purpose.ForcedSwitch => "Choose a creature to send out.",
            Purpose.BattleSwitch => "Choose a creature.",
            Purpose.ItemTarget => "Use on which creature?",
            _ => "Choose a creature.",
        };
    }

    private List<Creature> Party => _game.State.Party;

    public override void Update(float dt)
    {
        _messageTime -= dt;
        if (_closed) return;
        if (_menu is not null)
        {
            _menu.Update(dt, Input);
            if (_menu.Result is { } r) { _menu = null; OnMenu(r); }
            return;
        }
        if (Party.Count == 0) { Close(-1); return; }
        _list ??= UiKit.List(Audio, [.. Party.Select(c => c.Name)]);
        int? chosen = _list.Update(Input);
        if (Input.Pressed("cancel"))
        {
            if (_swapFrom >= 0) { _swapFrom = -1; Audio.Play("cancel", 0.7f); return; }
            if (_purpose == Purpose.ForcedSwitch) { Audio.Play("deny"); return; }
            Audio.Play("cancel", 0.7f);
            Close(-1);
            return;
        }
        if (chosen is null) return;
        Audio.Play("confirm", 0.7f);
        if (_swapFrom >= 0)
        {
            (Party[_swapFrom], Party[Index]) = (Party[Index], Party[_swapFrom]);
            _list.SetOptions(Party.Select(c => c.Name));
            _swapFrom = -1;
            Audio.Play("sendout", 0.5f);
            return;
        }
        switch (_purpose)
        {
            case Purpose.ItemTarget: Close(Index); break;
            case Purpose.Field: _menu = new ChoiceMenu(Font, Audio, "SUMMARY", "SWITCH", "CANCEL"); break;
            default: _menu = new ChoiceMenu(Font, Audio, "SEND OUT", "SUMMARY", "CANCEL") { CanCancel = true }; break;
        }
    }

    private void OnMenu(int choice)
    {
        bool field = _purpose == Purpose.Field;
        if (choice < 0 || choice == 2) return;
        if (field ? choice == 0 : choice == 1) { Game.Scenes.Push(new SummaryScene(_game, Index)); return; }
        if (field) { _swapFrom = Index; Say("Move to where?"); return; }
        var c = Party[Index];
        if (c.Fainted) { Audio.Play("deny"); Say($"{c.Name} has no energy left to fight!"); return; }
        if (Index == _activeIndex) { Audio.Play("deny"); Say($"{c.Name} is already in battle!"); return; }
        Close(Index);
    }

    private void Say(string text) { _message = text; _messageTime = 2.2f; }

    /// <summary>
    /// State hash: purpose, active slot, cursor, the creature picked to swap, the open menu, the message with its timer
    /// and whether it closed. The party itself is the adventure's (section <c>game</c>).
    /// </summary>
    public override void HashState(StateHasher hash)
    {
        hash.Add((int)_purpose);
        hash.Add(_activeIndex);
        hash.Add(Index);
        hash.Add(_swapFrom);
        hash.Add(_menu);
        hash.Add(_message);
        hash.Add(_messageTime);
        hash.Add(_closed);
    }

    private void Close(int result)
    {
        if (_closed) return;
        _closed = true;
        _picked?.Invoke(result);
        Game.Scenes.Pop();
    }

    public override void Draw(IRenderer r)
    {
        r.FillGradient(TrailDuelGame.Canvas, Color.Hex("8ac8b0"), Color.Hex("cfe8c8"), 12);
        for (int i = 0; i < 12; i++)
        {
            float x = (i * 37 + Age * 6) % 360 - 20;
            r.FillCircle(new Vector2(x, 20 + (i * 53) % 150), 10 + i % 3 * 4, Pal.White.WithAlpha(0.12f));
        }
        for (int i = 0; i < Party.Count; i++) DrawSlot(r, i);
        var bottom = new RectF(4, 156, 312, 21);
        UiKit.Box(r, bottom);
        UiKit.Text(r, Font, _messageTime > 0 && _message is not null ? _message : _prompt, bottom.X + 8, bottom.Y + 7);
        _menu?.Draw(r, new Vector2(316, 152));
    }

    private static readonly PrefixedKeys MiniKeys = new("mini:"), Mini2Keys = new("mini2:");

    private void DrawSlot(IRenderer r, int i)
    {
        var c = Party[i];
        bool selected = i == Index;
        var rect = new RectF(selected ? 10 : 6, 4 + i * 25, 300, 23);
        var fill = c.Fainted ? Color.Hex("e8d0c8") : selected ? Color.Hex("fff3d6") : (Color?)null;
        UiKit.Box(r, rect, fill);
        if (selected) r.DrawRect(new RectF(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2), Pal.Coral);
        if (i == _swapFrom) r.DrawRect(new RectF(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2), Color.Hex("4a7ad8"));
        string icon = (selected && !c.Fainted && (int)(Age * 4) % 2 == 0 ? Mini2Keys : MiniKeys)[c.SpeciesId];
        r.Draw(_game.Sprite(icon), new Vector2(rect.X + 16, rect.Y + 11));
        UiKit.Text(r, Font, c.Name, rect.X + 32, rect.Y + 4);
        UiKit.Text(r, Font, NumberTexts.Level(c.Level), rect.X + 32, rect.Y + 13, UiKit.Muted);
        UiKit.TypeBadge(r, Font, c.Type, rect.X + 92, rect.Y + 3);
        UiKit.StatusBadge(r, Font, c.Fainted ? StatusCondition.None : c.Status, rect.X + 92, rect.Y + 13);
        if (c.Fainted) UiKit.Text(r, Font, "FNT", rect.X + 94, rect.Y + 13, Color.Hex("c8504a"));
        if (i == _activeIndex) UiKit.Text(r, Font, "★", rect.X + 140, rect.Y + 4, Pal.Gold);
        UiKit.HpBar(r, Font, rect.X + 160, rect.Y + 4, 100, c.HpFraction);
        UiKit.TextRight(r, Font, NumberTexts.Ratio(c.Hp, c.MaxHp), rect.Right - 8, rect.Y + 13);
    }
}
