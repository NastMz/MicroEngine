using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using TrailDuel.Rules;
using TrailDuel.Ui;
using TrailDuel.Visuals;

namespace TrailDuel.Scenes;

/// <summary>The bag: items with counts and descriptions. In the field it heals; in battle it hands the choice back.</summary>
internal sealed class BagScene(TrailDuelGame game, Battle? battle, Action<string?, int>? onUse = null) : Scene
{
    private int _index;
    private string? _message;
    private float _messageTime;
    private bool _closed;

    private List<(ItemData Item, int Count)> Items => game.State.Bag.Owned().ToList();

    public override void Update(float dt)
    {
        _messageTime -= dt;
        if (_closed) return;
        var items = Items;
        if (Input.Pressed("cancel")) { Audio.Play("cancel", 0.7f); Close(null, -1); return; }
        if (items.Count == 0) return;
        _index = Math.Clamp(_index, 0, items.Count - 1);
        int dir = (Input.Pressed("down") ? 1 : 0) - (Input.Pressed("up") ? 1 : 0);
        if (dir != 0) { _index = (_index + dir + items.Count) % items.Count; Audio.Play("move", 0.6f); }
        if (!Input.Pressed("confirm")) return;
        Audio.Play("confirm", 0.7f);
        var item = items[_index].Item;
        if (item.Kind == ItemKind.Orb)
        {
            if (battle is null) { Say("There's nothing to catch here!"); Audio.Play("deny"); return; }
            if (battle.IsTrainer) { Say("You can't catch another trainer's creature!"); Audio.Play("deny"); return; }
            Close(item.Id, -1);
            return;
        }
        Game.Scenes.Push(new PartyScene(game, PartyScene.Purpose.ItemTarget, target =>
        {
            if (target < 0) return;
            var creature = game.State.Party[target];
            if (!ItemDex.CanUseOn(item, creature)) { Say("It won't have any effect."); Audio.Play("deny"); return; }
            if (battle is not null) { Close(item.Id, target); return; }
            game.State.Bag.Remove(item.Id);
            Audio.Play("heal");
            Say(ItemDex.Apply(item, creature));
        }, prompt: $"Use the {item.Name} on which creature?"));
    }

    private void Say(string text) { _message = text; _messageTime = 2.4f; }

    private void Close(string? item, int target)
    {
        if (_closed) return;
        _closed = true;
        onUse?.Invoke(item, target);
        Game.Scenes.Pop();
    }

    public override void Draw(IRenderer r)
    {
        r.FillGradient(new RectF(0, 0, 320, 180), Color.Hex("e8b878"), Color.Hex("f6e2b8"), 10);
        // The satchel bobs gently on the left.
        r.Draw(game.Sprite("satchel"), new Vector2(56, 60 + MathF.Round(MathX.Wave(Age, 0.6f, 1.5f))));
        UiKit.Title(r, Font, "BAG", new Vector2(56, 102));
        UiKit.Title(r, Font, $"${game.State.Money}", new Vector2(56, 116), 1, Pal.Gold);

        var list = new RectF(112, 8, 202, 120);
        UiKit.Box(r, list);
        var items = Items;
        if (items.Count == 0) UiKit.Text(r, Font, "The bag is empty.", list.X + 14, list.Y + 10, UiKit.Muted);
        for (int i = 0; i < items.Count; i++)
        {
            var (item, count) = items[i];
            float y = list.Y + 8 + i * 16;
            r.Draw(game.Sprite("icon:" + item.Id), new Vector2(list.X + 16, y - 2));
            UiKit.Text(r, Font, item.Name, list.X + 34, y);
            UiKit.TextRight(r, Font, "×" + count, list.Right - 10, y);
            if (i == _index) UiKit.Cursor(r, Font, list.X + 5, y, Age);
        }
        var desc = new RectF(4, 134, 312, 42);
        UiKit.Box(r, desc);
        string text = _messageTime > 0 && _message is not null ? _message : items.Count > 0 ? items[Math.Clamp(_index, 0, items.Count - 1)].Item.Description : "";
        UiKit.Text(r, Font, text, desc.X + 10, desc.Y + 9);
        UiKit.Text(r, Font, "Z: use   X: back", desc.X + 10, desc.Y + 23, UiKit.Muted);
    }
}
