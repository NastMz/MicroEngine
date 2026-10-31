using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using TrailDuel.Rules;
using TrailDuel.Ui;
using TrailDuel.Visuals;

namespace TrailDuel.Scenes;

/// <summary>The Trail Shop buy screen: pick an item, choose a quantity with left/right, confirm to pay.</summary>
internal sealed class ShopScene(TrailDuelGame game, Action onDone) : Scene
{
    public static readonly IReadOnlyList<ItemData> Stock = ItemDex.All;

    private int _index;
    private int _quantity = 1;
    private bool _choosingQuantity;
    private string? _message;
    private float _messageTime;
    private float _coinPop;

    /// <summary>Buys <paramref name="quantity"/> of <paramref name="item"/> if affordable.</summary>
    public static bool TryBuy(GameState state, ItemData item, int quantity)
    {
        int cost = item.Price * quantity;
        if (quantity <= 0 || cost > state.Money) return false;
        state.Money -= cost;
        state.Bag.Add(item.Id, quantity);
        return true;
    }

    public override void Update(float dt)
    {
        _messageTime -= dt;
        _coinPop = MathF.Max(0, _coinPop - dt * 3);
        var item = Stock[_index];
        if (_choosingQuantity)
        {
            int max = Math.Clamp(game.State.Money / item.Price, 1, 99);
            int d = (Input.Pressed("right") || Input.Pressed("up") ? 1 : 0) - (Input.Pressed("left") || Input.Pressed("down") ? 1 : 0);
            if (d != 0) { _quantity = (_quantity - 1 + d + max) % max + 1; Audio.Play("move", 0.6f); }
            if (Input.Pressed("cancel")) { _choosingQuantity = false; Audio.Play("cancel", 0.7f); }
            else if (Input.Pressed("confirm"))
            {
                _choosingQuantity = false;
                if (TryBuy(game.State, item, _quantity))
                {
                    Audio.Play("buy");
                    _coinPop = 1;
                    Say(_quantity > 1 ? $"Here you go: {_quantity} {item.Name}s!" : $"Here you go: one {item.Name}!");
                }
                else { Audio.Play("deny"); Say("You don't have enough money."); }
            }
            return;
        }
        if (Input.Pressed("cancel")) { Audio.Play("cancel", 0.7f); onDone(); Game.Scenes.Pop(); return; }
        int dir = (Input.Pressed("down") ? 1 : 0) - (Input.Pressed("up") ? 1 : 0);
        if (dir != 0) { _index = (_index + dir + Stock.Count) % Stock.Count; Audio.Play("move", 0.6f); }
        if (Input.Pressed("confirm"))
        {
            if (game.State.Money < item.Price) { Audio.Play("deny"); Say("You don't have enough money."); return; }
            Audio.Play("confirm", 0.7f);
            _quantity = 1;
            _choosingQuantity = true;
        }
    }

    private void Say(string text) { _message = text; _messageTime = 2.4f; }

    public override void Draw(IRenderer r)
    {
        r.FillGradient(new RectF(0, 0, 320, 180), Color.Hex("86aee2"), Color.Hex("e6eef8"), 10);
        var money = new RectF(6, 6, 104, 22);
        UiKit.Box(r, money);
        UiKit.Text(r, Font, "MONEY", money.X + 8, money.Y + 7, UiKit.Muted);
        UiKit.TextRight(r, Font, $"${game.State.Money}", money.Right - 8, money.Y + 7 - MathF.Round(_coinPop * 3), _coinPop > 0 ? Pal.CoralDark : null);

        var list = new RectF(114, 6, 200, 118);
        UiKit.Box(r, list);
        for (int i = 0; i < Stock.Count; i++)
        {
            var item = Stock[i];
            float y = list.Y + 10 + i * 18;
            r.Draw(game.Sprite("icon:" + item.Id), new Vector2(list.X + 16, y - 2));
            UiKit.Text(r, Font, item.Name, list.X + 34, y, game.State.Money < item.Price ? UiKit.Muted : null);
            UiKit.TextRight(r, Font, $"${item.Price}", list.Right - 10, y);
            if (i == _index) UiKit.Cursor(r, Font, list.X + 5, y, Age);
        }

        var owned = new RectF(6, 32, 104, 22);
        UiKit.Box(r, owned);
        UiKit.Text(r, Font, "IN BAG", owned.X + 8, owned.Y + 7, UiKit.Muted);
        UiKit.TextRight(r, Font, game.State.Bag.Count(Stock[_index].Id).ToString(), owned.Right - 8, owned.Y + 7);

        // A clerk counter illustration.
        var clerk = game.Sprite(Visuals.CharacterArt.Key("clerk", Rules.Direction.Down, 0));
        r.Draw(clerk.Texture, clerk.Source, new Vector2(42, 52), Color.White, new DrawOptions(Scale: new Vector2(2)));
        r.FillRect(new RectF(14, 94, 88, 16), Color.Hex("8a6ac8"));
        r.FillRect(new RectF(14, 94, 88, 2), Color.Hex("b49ae0"));
        r.FillRect(new RectF(12, 90, 92, 5), Pal.Cream);
        r.DrawRect(new RectF(12, 90, 92, 20), Pal.Ink);
        r.Draw(game.Sprite("icon:" + Stock[_index].Id), new Vector2(70, 76 + MathF.Round(MathX.Wave(Age, 1, 2))));

        if (_choosingQuantity)
        {
            var q = new RectF(178, 128, 136, 22);
            UiKit.Box(r, q);
            UiKit.Text(r, Font, $"← ×{_quantity:00} →", q.X + 8, q.Y + 7);
            UiKit.TextRight(r, Font, $"${Stock[_index].Price * _quantity}", q.Right - 8, q.Y + 7);
        }
        var desc = new RectF(4, 152, 312, 24);
        UiKit.Box(r, desc);
        UiKit.Text(r, Font, _messageTime > 0 && _message is not null ? _message : Stock[_index].Description, desc.X + 10, desc.Y + 8);
    }
}
