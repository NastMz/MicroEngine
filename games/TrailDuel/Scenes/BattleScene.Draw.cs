using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using TrailDuel.Rules;
using TrailDuel.Ui;
using TrailDuel.Visuals;
using TrailDuel.World;

namespace TrailDuel.Scenes;

/// <summary>Battle rendering: backdrop, platforms, creatures, trainers, HUD panels, menus and effects.</summary>
internal sealed partial class BattleScene
{
    private string PlatformSuffix => _setup.Backdrop switch { Backdrop.Forest => "forest", Backdrop.Hall => "hall", Backdrop.Indoor => "indoor", _ => "" };

    public override void Draw(IRenderer r)
    {
        var shake = _shake > 0 ? new Vector2(MathF.Round(MathF.Sin(Age * 90) * 3 * _shake), 0) : Vector2.Zero;
        var bd = _setup.Backdrop;
        r.Draw(_game.Sprite("backdrop:" + bd), shake);
        if (bd == Backdrop.Meadow)
            for (int i = 0; i < 3; i++)
            {
                float x = (i * 130 + Age * (4 + i * 2)) % 420 - 60;
                r.Draw(_game.Sprite(i == 1 ? "cloud2" : "cloud"), new Vector2(MathF.Round(x), 8 + i * 14), Pal.White.WithAlpha(0.85f));
            }
        if (bd == Backdrop.Forest)
            for (int i = 0; i < 9; i++)
            {
                // Pollen and fireflies drifting through the light shafts.
                float x = (i * 37 + Age * (3 + i % 3)) % 330 - 5, y = 20 + (i * 23) % 70 + MathF.Sin(Age * 0.9f + i) * 6;
                r.Draw(_game.Sprite("mote"), new Vector2(MathF.Round(x), MathF.Round(y)), Color.White.WithAlpha(0.45f + 0.4f * MathF.Sin(Age * 2 + i * 1.7f)));
            }

        float slide = 1 - _introT;
        var enemyPlat = EnemyBase + new Vector2(-slide * 330, 0) + shake;
        var playerPlat = PlayerBase + new Vector2(slide * 330, 0) + shake;
        r.Draw(_game.Sprite("platform:enemy" + PlatformSuffix), enemyPlat + new Vector2(0, -2));
        r.Draw(_game.Sprite("platform:player" + PlatformSuffix), playerPlat + new Vector2(0, -10));

        // Opposing trainer and the player's own trainer, at battle size.
        if (_setup.Trainer is { } trainerDef && _enemyTrainerX < 1)
        {
            var look = LookFor(trainerDef);
            r.Draw(_game.Sprite(CharacterArt.PortraitKey(look.Key, Direction.Down)), enemyPlat + new Vector2(_enemyTrainerX * 120, 0));
        }
        if (_playerTrainerX < 1)
            r.Draw(_game.Sprite(CharacterArt.PortraitKey("player", Direction.Up)), playerPlat + new Vector2(-_playerTrainerX * 140, -2));

        DrawCreature(r, _enemy, "front:", "white:", enemyPlat, clipBottom: enemyPlat.Y + 2);
        DrawCreature(r, _player, "back:", "whiteback:", playerPlat, clipBottom: 180);

        if (_orbVisible)
        {
            var orb = _game.Sprite(_orbOpen ? "orbopen" : _orbSprite);
            r.Draw(orb.Texture, orb.Source, _orbPos - orb.Origin, Color.White, new DrawOptions(Rotation: _orbAngle, Origin: orb.Origin));
        }
        _sprites.Draw(r);
        _fx.Draw(r);
        _glow.Draw(r);

        if (_enemy.HudVisible && _introT >= 1) DrawEnemyHud(r);
        if (_player.HudVisible) DrawPlayerHud(r);

        DrawBottom(r);
        if (_levelPanel is { } lp) DrawLevelPanel(r, lp);
        if (_flash > 0) r.FillRect(new RectF(0, 0, 320, 180), Pal.White.WithAlpha(_flash * 0.8f));
    }

    private static CharacterLook LookFor(TrainerDef def)
    {
        if (def.IsRival) return CharacterLook.Rival;
        if (def.IsMaster) return CharacterLook.Master;
        foreach (var map in Maps.Every)
            foreach (var npc in map.Npcs)
                if (npc.Trainer?.Name == def.Name) return npc.Look;
        return CharacterLook.Youngster;
    }

    private static void DrawScaled(IRenderer r, Sprite sprite, Vector2 anchor, float scale, Color tint, Vector2 origin)
        => r.Draw(sprite.Texture, sprite.Source, anchor - origin * scale, tint, new DrawOptions(Scale: new Vector2(scale)));

    private void DrawCreature(IRenderer r, Display d, string prefix, string whitePrefix, Vector2 platform, float clipBottom)
    {
        if (!d.Visible || d.Blink) return;
        bool player = prefix == "back:";
        var anchor = platform + (player ? new Vector2(0, 0) : new Vector2(0, -2)) + d.Offset;
        var white = _game.Sprite(whitePrefix + d.Creature.SpeciesId);
        if (!player && d.SinkDepth <= 0)
        {
            // The ground shadow stays on the platform while the creature lunges or hops.
            var shadow = _game.Sprite("cshadow:" + d.Creature.SpeciesId);
            r.Draw(shadow, new Vector2(MathF.Round(anchor.X), platform.Y - 2), Color.White.WithAlpha(Math.Clamp(d.Scale, 0, 1)));
        }
        // Idle breathing: alternate with the raised frame while the feet stay planted, out of phase per side.
        bool idle = (_mode != Mode.Busy || d.Offset == Vector2.Zero) && d.White <= 0 && d.TintAlpha <= 0;
        bool breathe = idle && (int)((Age + (player ? 0.45f : 0)) / 0.55f) % 2 == 1;
        var sprite = _game.Sprite((breathe ? (player ? "back2:" : "front2:") : prefix) + d.Creature.SpeciesId);
        float scale = MathF.Max(0.01f, d.Scale);
        if (d.SinkDepth > 0)
        {
            // Fainting: the creature slides down behind the platform edge.
            float sink = d.SinkDepth * sprite.Height;
            float visible = sprite.Height - sink;
            if (visible <= 0) return;
            var src = new RectF(sprite.Source.X, sprite.Source.Y, sprite.Source.Width, visible);
            r.Draw(sprite.Texture, src, anchor - sprite.Origin + new Vector2(0, sink), Color.White);
            return;
        }
        if (MathF.Abs(scale - 1) > 0.01f)
        {
            DrawScaled(r, d.White > 0.5f ? white : sprite, anchor, scale, Color.White, sprite.Origin);
            return;
        }
        r.Draw(sprite, anchor);
        if (d.TintAlpha > 0) r.Draw(white, anchor, d.Tint.WithAlpha(d.TintAlpha));
        if (d.White > 0) r.Draw(white, anchor, Color.White.WithAlpha(d.White));
        _ = clipBottom;
    }

    private void DrawEnemyHud(IRenderer r)
    {
        var rect = new RectF(8, 10, 130, 34);
        UiKit.Box(r, rect);
        UiKit.Text(r, Font, _enemy.Creature.Name, rect.X + 8, rect.Y + 6);
        UiKit.TextRight(r, Font, "Lv" + _enemy.Level, rect.Right - 8, rect.Y + 6);
        if (_setup.Trainer is null && _game.State.Caught.Contains(_enemy.Creature.SpeciesId))
            r.Draw(_game.Sprite("orbsmall"), new Vector2(rect.X + 12 + Font.MeasureWidth(_enemy.Creature.Name), rect.Y + 10));
        UiKit.HpBar(r, Font, rect.X + 8, rect.Y + 19, 98, _enemy.Hp / Math.Max(1, _enemy.MaxHp));
        UiKit.StatusBadge(r, Font, _enemy.Status, rect.Right - 4 - 19, rect.Y - 6);
    }

    private void DrawPlayerHud(IRenderer r)
    {
        var rect = new RectF(176, 84, 138, 44);
        UiKit.Box(r, rect);
        UiKit.Text(r, Font, _player.Creature.Name, rect.X + 8, rect.Y + 6);
        UiKit.TextRight(r, Font, "Lv" + _player.Level, rect.Right - 8, rect.Y + 6);
        UiKit.HpBar(r, Font, rect.X + 8, rect.Y + 18, 100, _player.Hp / Math.Max(1, _player.MaxHp));
        UiKit.TextRight(r, Font, $"{(int)MathF.Ceiling(_player.Hp)}/{_player.MaxHp}", rect.Right - 8, rect.Y + 27);
        UiKit.XpBar(r, rect.X + 8, rect.Y + 37, 80, _player.Xp);
        UiKit.StatusBadge(r, Font, _player.Status, rect.X + 6, rect.Y - 6);
    }

    private void DrawBottom(IRenderer r)
    {
        r.FillRect(new RectF(0, 130, 320, 50), Pal.Ink);
        r.FillRect(new RectF(0, 130, 320, 1), Pal.Coral);
        if (_mode == Mode.Moves) { DrawMoveMenu(r); return; }
        if (_mode == Mode.Actions)
        {
            _text.Area = new RectF(4, 134, 176, 42);
            _text.Draw(r);
            var box = new RectF(184, 134, 132, 42);
            UiKit.Box(r, box);
            for (int i = 0; i < 4; i++)
            {
                float x = box.X + 16 + i % 2 * 60, y = box.Y + 9 + i / 2 * 14;
                UiKit.Text(r, Font, ActionLabels[i], x, y, i == 3 && _battle.IsTrainer ? UiKit.Muted : null);
                if (i == _actionIndex) UiKit.Cursor(r, Font, x - 10, y, _menuTime);
            }
            return;
        }
        _text.Area = new RectF(4, 134, 312, 42);
        if (!_text.Active) UiKit.Box(r, _text.Area);
        _text.Draw(r);
        _choice?.Draw(r, new Vector2(316, 132));
        // Choosing a move to forget: the card follows the highlighted option ("Keep all" shows the new move).
        if (_choice is not null && _forgetOptions is { } options)
        {
            var (move, pp) = options[Math.Clamp(_choice.Index, 0, options.Length - 1)];
            float left = _choice.Bounds(new Vector2(316, 132)).X;
            DrawMoveCard(r, move, pp, 4, left - 8);
        }
    }

    /// <summary>A move card resting on the bottom panel, from x <paramref name="left"/> to <paramref name="right"/>.</summary>
    private void DrawMoveCard(IRenderer r, MoveData move, int? pp, float left, float right)
    {
        var foe = new MoveCard.Foe(_enemy.Creature.Name, _enemy.Creature.Type);
        float width = right - left;
        float height = MoveCard.Height(Font, move, width, foe);
        MoveCard.Draw(r, Font, move, pp, left, 128 - height, width, foe);
    }

    private void DrawMoveMenu(IRenderer r)
    {
        var box = new RectF(4, 134, 208, 42);
        UiKit.Box(r, box);
        var moves = _battle.Player.Moves;
        for (int i = 0; i < 4; i++)
        {
            float x = box.X + 16 + i % 2 * 98, y = box.Y + 9 + i / 2 * 14;
            string label = i < moves.Count ? moves[i].Data.Name : "-";
            UiKit.Text(r, Font, label, x, y, i < moves.Count && moves[i].Pp == 0 ? UiKit.Muted : null);
            if (i == _moveIndex) UiKit.Cursor(r, Font, x - 10, y, _menuTime);
        }
        var info = new RectF(216, 134, 100, 42);
        UiKit.Box(r, info);
        if (_moveIndex < moves.Count)
        {
            var m = moves[_moveIndex];
            UiKit.Text(r, Font, "PP", info.X + 8, info.Y + 5, UiKit.Muted);
            var ppColor = m.Pp == 0 ? Color.Hex("e8504a") : m.Pp <= m.Data.Pp / 4 ? Color.Hex("d89a20") : (Color?)null;
            UiKit.TextRight(r, Font, $"{m.Pp}/{m.Data.Pp}", info.Right - 8, info.Y + 5, ppColor);
            UiKit.Text(r, Font, "POW", info.X + 8, info.Y + 16, UiKit.Muted);
            UiKit.Text(r, Font, MoveText.Power(m.Data), info.X + 28, info.Y + 16);
            UiKit.TextRight(r, Font, MoveText.Accuracy(m.Data), info.Right - 8, info.Y + 16);
            UiKit.TypeBadge(r, Font, m.Data.Type, info.X + 8, info.Y + 27);
            float eff = TypeChart.Effectiveness(m.Data.Type, _battle.Enemy.Type);
            if (m.Data.IsDamaging && eff != 1)
                UiKit.Text(r, Font, eff > 1 ? "↑" : "↓", info.X + 10 + UiKit.TypeBadgeWidth(Font, m.Data.Type), info.Y + 28, eff > 1 ? Color.Hex("3f9a4a") : Color.Hex("c8504a"));
            UiKit.TextRight(r, Font, _moveInfo ? "C:Hide" : "C:Info", info.Right - 8, info.Y + 28, UiKit.Muted);
            if (_moveInfo) DrawMoveCard(r, m.Data, m.Pp, 4, 312);
        }
    }

    private void DrawLevelPanel(IRenderer r, LevelUp lp)
    {
        var rect = new RectF(196, 8, 118, 74);
        UiKit.Box(r, rect);
        string[] names = ["Max HP", "Attack", "Defense", "Special", "Speed"];
        UiKit.Text(r, Font, _levelPanelName ?? "", rect.X + 8, rect.Y + 5, Pal.Coral);
        for (int i = 0; i < 5; i++)
        {
            float y = rect.Y + 17 + i * 11;
            UiKit.Text(r, Font, names[i], rect.X + 8, y);
            UiKit.TextRight(r, Font, $"{lp.After[i]}", rect.Right - 28, y);
            UiKit.TextRight(r, Font, $"+{lp.After[i] - lp.Before[i]}", rect.Right - 7, y, Color.Hex("3f9a4a"));
        }
    }
}
