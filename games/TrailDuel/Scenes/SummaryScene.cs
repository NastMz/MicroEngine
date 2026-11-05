using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using TrailDuel.Rules;
using TrailDuel.Ui;
using TrailDuel.Visuals;

namespace TrailDuel.Scenes;

/// <summary>
/// A creature's card: portrait, type, stats, experience and moves. Left/right browses the party; up/down
/// opens the moves page, where the highlighted move's full card replaces the stats.
/// </summary>
internal sealed class SummaryScene(TrailDuelGame game, int index) : Scene
{
    private static readonly string[] StatNames = ["Attack", "Defense", "Special", "Speed"];
    // Texts kept while their values stay the same, so the summary makes no garbage per frame.
    private CachedText _numberText, _toNextText, _backHint;
    private string? _descriptionFor;
    private IReadOnlyList<string> _descriptionLines = [];
    private int _index = index;
    private float _switchTime;
    /// <summary>
    /// The highlighted move on the moves page, or -1 on the overview. Not the engine's <c>SelectionList</c>: the page has
    /// a no-move state, down opens it on the first move and up on the last, and confirm goes back instead of choosing.
    /// </summary>
    private int _move = -1;

    internal int MoveCursor => _move;
    internal int CreatureIndex => _index;

    private List<Creature> Party => game.State.Party;

    /// <summary>State hash: the creature shown and the move cursor. Left out: the page-switch animation timer and the text caches drawing fills.</summary>
    public override void HashState(StateHasher hash)
    {
        hash.Add(_index);
        hash.Add(_move);
    }

    public override void Update(float dt)
    {
        _switchTime += dt;
        if (Input.Pressed("cancel") || Input.Pressed("confirm"))
        {
            Audio.Play("cancel", 0.7f);
            // Back from the moves page to the overview first, then out.
            if (_move >= 0) _move = -1;
            else Game.Scenes.Pop();
            return;
        }
        int moves = Party[_index].Moves.Count;
        int vertical = (Input.Pressed("down") ? 1 : 0) - (Input.Pressed("up") ? 1 : 0);
        if (vertical != 0 && moves > 0)
        {
            _move = _move < 0 ? (vertical > 0 ? 0 : moves - 1) : (_move + vertical + moves) % moves;
            Audio.Play("move", 0.6f);
        }
        int dir = (Input.Pressed("right") ? 1 : 0) - (Input.Pressed("left") ? 1 : 0);
        if (dir != 0 && Party.Count > 1)
        {
            _index = (_index + dir + Party.Count) % Party.Count;
            _switchTime = 0;
            if (_move >= 0) _move = Math.Min(_move, Party[_index].Moves.Count - 1);
            Audio.Play("move", 0.6f);
        }
    }

    public override void Draw(IRenderer r)
    {
        var c = Party[_index];
        var typeColor = Pal.TypeColor(c.Type);
        r.FillGradient(TrailDuelGame.Canvas, Color.Lerp(typeColor, Pal.White, 0.55f), Pal.Cream, 10);

        // Portrait card
        var card = new RectF(6, 6, 118, 130);
        UiKit.Box(r, card);
        r.FillGradient(new RectF(card.X + 4, card.Y + 4, card.Width - 8, 86), Color.Lerp(typeColor, Pal.White, 0.7f), Pal.Cream, 6);
        r.Draw(game.Sprite("platform:enemy"), new Vector2(card.X + 59, card.Y + 82));
        float hop = _switchTime < 0.3f ? -MathF.Sin(_switchTime / 0.3f * MathF.PI) * 8 : MathF.Round(MathX.Wave(Age, 0.8f, 1));
        r.Draw(game.Sprite(SpriteKeys.Front[c.SpeciesId]), new Vector2(card.X + 59, card.Y + 82 + MathF.Round(hop)));
        UiKit.Text(r, Font, c.Name, card.X + 8, card.Y + 94);
        UiKit.TextRight(r, Font, NumberTexts.Level(c.Level), card.Right - 8, card.Y + 94);
        UiKit.TypeBadge(r, Font, c.Type, card.X + 8, card.Y + 106);
        UiKit.StatusBadge(r, Font, c.Status, card.Right - 27, card.Y + 106);
        UiKit.Text(r, Font, _numberText.Get(SpeciesDex.Number(c.SpeciesId), static n => $"No.{n:000}"), card.X + 8, card.Y + 118, UiKit.Muted);
        if (_move < 0) UiKit.TextRight(r, Font, "↑↓ Moves", card.Right - 8, card.Y + 118, UiKit.Muted);
        if (_move >= 0) { DrawMovesPage(r, c); DrawArrows(r); return; }

        // Stats
        var stats = new RectF(128, 6, 186, 62);
        UiKit.Box(r, stats);
        UiKit.HpBar(r, Font, stats.X + 8, stats.Y + 7, 104, c.HpFraction);
        UiKit.TextRight(r, Font, NumberTexts.Ratio(c.Hp, c.MaxHp), stats.Right - 8, stats.Y + 7);
        for (int i = 0; i < 4; i++)
        {
            float x = stats.X + 8 + i % 2 * 90, y = stats.Y + 19 + i / 2 * 11;
            int value = i switch { 0 => c.Attack, 1 => c.Defense, 2 => c.Special, _ => c.Speed };
            UiKit.Text(r, Font, StatNames[i], x, y, UiKit.Muted);
            UiKit.TextRight(r, Font, NumberTexts.Of(value), x + 78, y);
        }
        int toNext = c.Level >= Creature.MaxLevel ? 0 : Creature.XpForLevel(c.Level + 1) - c.Xp;
        UiKit.Text(r, Font, "To next Lv", stats.X + 8, stats.Y + 42, UiKit.Muted);
        UiKit.TextRight(r, Font, _toNextText.Get(toNext, static n => n.ToString()), stats.Right - 8, stats.Y + 42);
        UiKit.XpBar(r, stats.X + 8, stats.Y + 53, 168, c.XpFraction);

        // Moves
        var moves = new RectF(128, 72, 186, 64);
        UiKit.Box(r, moves);
        for (int i = 0; i < c.Moves.Count; i++)
        {
            var m = c.Moves[i];
            float y = moves.Y + 6 + i * 14;
            int bw = UiKit.TypeBadgeWidth(Font, m.Data.Type);
            UiKit.TypeBadge(r, Font, m.Data.Type, moves.X + 8, y);
            UiKit.Text(r, Font, m.Data.Name, moves.X + 12 + bw, y + 1);
            UiKit.TextRight(r, Font, NumberTexts.Ratio(m.Pp, m.Data.Pp), moves.Right - 8, y + 1, UiKit.Muted);
        }

        var desc = new RectF(6, 140, 308, 36);
        UiKit.Box(r, desc);
        if (!ReferenceEquals(_descriptionFor, c.Species.Description)) (_descriptionFor, _descriptionLines) = (c.Species.Description, Font.Wrap(c.Species.Description, 290));
        var lines = _descriptionLines;
        for (int i = 0; i < Math.Min(2, lines.Count); i++) UiKit.Text(r, Font, lines[i], desc.X + 9, desc.Y + 7 + i * 12);
        DrawArrows(r);
    }

    private void DrawArrows(IRenderer r)
    {
        if (Party.Count <= 1) return;
        Font.Draw(r, "←", new Vector2(2, 70), Pal.Ink);
        Font.Draw(r, "→", new Vector2(313, 70), Pal.Ink);
    }

    /// <summary>The moves page: the move list in place of the stats, and the highlighted move's card below it.</summary>
    private void DrawMovesPage(IRenderer r, Creature c)
    {
        var list = new RectF(128, 6, 186, 64);
        UiKit.Box(r, list);
        for (int i = 0; i < c.Moves.Count; i++)
        {
            var m = c.Moves[i];
            float y = list.Y + 6 + i * 14;
            UiKit.Text(r, Font, m.Data.Name, list.X + 16, y + 1);
            UiKit.TextRight(r, Font, NumberTexts.Ratio(m.Pp, m.Data.Pp), list.Right - 8, y + 1, UiKit.Muted);
            if (i == _move) UiKit.Cursor(r, Font, list.X + 6, y + 1, Age);
        }
        var slot = c.Moves[Math.Clamp(_move, 0, c.Moves.Count - 1)];
        MoveCard.Draw(r, Font, slot.Data, slot.Pp, 128, 74, 186);
        var hint = new RectF(6, 140, 118, 36);
        UiKit.Box(r, hint);
        UiKit.Text(r, Font, "↑↓ Choose move", hint.X + 8, hint.Y + 7, UiKit.Muted);
        UiKit.Text(r, Font, _backHint.Get(Game.Prompts.Version, Game.Prompts, static p => p.Format("{cancel}  Back")), hint.X + 8, hint.Y + 19, UiKit.Muted);
    }
}
