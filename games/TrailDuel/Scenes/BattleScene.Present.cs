using System.Collections;
using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using TrailDuel.Rules;
using TrailDuel.Visuals;
using TrailDuel.World;

namespace TrailDuel.Scenes;

/// <summary>Turns battle events into animation: sends-outs, attacks, HP drains, faints, captures and level-ups.</summary>
internal sealed partial class BattleScene
{
    /// <summary>What one side of the field currently shows (lags behind the rules while animating).</summary>
    private sealed class Display
    {
        public Creature Creature = null!;
        public float Hp;
        public float Xp;
        public int Level;
        public int MaxHp;
        public StatusCondition Status;
        public bool Visible = true;
        public bool HudVisible;
        public Vector2 Offset;
        public float Scale = 1;
        public float White;
        /// <summary>A coloured glow over the sprite (charging an elemental move).</summary>
        public Color Tint = Color.White;
        public float TintAlpha;
        public bool Blink;
        public float SinkDepth;
        /// <summary>Its <see cref="Offset"/> in the scene's objects, to draw lunges and knockbacks between steps.</summary>
        public EntityId Entity;
    }

    private Display _enemy = null!, _player = null!;
    private readonly ParticleSystem _fx = new(600, 41);
    private readonly ParticleSystem _glow = new(200, 43) { Additive = true };
    private SpriteFx _sprites = null!;
    private float _introT, _enemyTrainerX = 1, _playerTrainerX = 1, _shake, _flash;
    private bool _orbVisible;
    private Vector2 _orbPos;
    private float _orbAngle;
    // Sliding things in the scene's objects, so the world draws them between steps.
    private EntityId _enemyPlatform, _playerPlatform, _enemyTrainer, _playerTrainer, _orb;
    private bool _orbOpen;
    private string _orbSprite = "orb";
    private LevelUp? _levelPanel;
    private string? _levelPanelName;

    private static readonly Vector2 EnemyBase = new(236, 82);
    private static readonly Vector2 PlayerBase = new(86, 136);

    private Display Of(Side side) => side == Side.Player ? _player : _enemy;
    private Vector2 BaseOf(Side side) => side == Side.Player ? PlayerBase : EnemyBase;
    /// <summary>The middle of the creature on that side, from its drawn height.</summary>
    private Vector2 CenterOf(Side side)
    {
        float h = _game.Art.CreatureHeight(Of(side).Creature.SpeciesId);
        return BaseOf(side) + new Vector2(0, (side == Side.Enemy ? -2 : 0) - h * 0.5f);
    }

    private void UpdateEffects(float dt)
    {
        _fx.Update(dt);
        _sprites.Update(dt);
        _glow.Update(dt);
        _shake = MathF.Max(0, _shake - dt * 3);
        _flash = MathF.Max(0, _flash - dt * 3);
    }

    private IEnumerator Tween(float seconds, Action<float> apply, Func<float, float>? ease = null)
    {
        ease ??= Ease.OutQuad;
        for (float t = 0; t < seconds; t += EngineStep)
        {
            apply(ease(Math.Clamp(t / seconds, 0, 1)));
            yield return null;
        }
        apply(1);
    }

    private IEnumerator Intro()
    {
        _player.Visible = false;
        _player.Level = _battle.Player.Level;
        _player.MaxHp = _battle.Player.MaxHp;
        _enemy.Level = _battle.Enemy.Level;
        _enemy.MaxHp = _battle.Enemy.MaxHp;
        _enemyTrainerX = _setup.Trainer is null ? 1 : 0;
        _playerTrainerX = 0;
        yield return Tween(0.9f, t => _introT = t, Ease.OutCubic);
        if (_setup.Trainer is null)
        {
            // The wild creature cries out with a little hop.
            Audio.Play("atk:" + _battle.Enemy.Type, 0.6f);
            yield return Tween(0.3f, t => _enemy.Offset = new Vector2(0, -MathF.Sin(t * MathF.PI) * 6));
        }
    }

    private string? _lastLine;

    private IEnumerator Present(List<BattleEvent> events)
    {
        foreach (var e in events)
        {
            switch (e)
            {
                case MessageEvent m:
                    _lastLine = m.Text;
                    _text.WaitForConfirmOnLast = true;
                    yield return _text.Say(m.Text);
                    break;
                case SendOutEvent s: yield return SendOut(s); break;
                case WithdrawEvent w: yield return Withdraw(w.Side); break;
                case MoveUsedEvent u:
                    // Keep "X used Y!" on screen through the attack and its hit; the next message replaces it.
                    // The caption must not auto-advance or be dismissed while it is only a caption.
                    _text.WaitForConfirmOnLast = false;
                    if (_lastLine is { } caption) { _text.Show(caption); _text.FinishTyping(); }
                    yield return MoveAnimation(u.User, u.Move);
                    break;
                case DamageEvent d: yield return Damage(d); break;
                case HealEvent h: yield return HealAnim(h); break;
                case StatusEvent s:
                    Of(s.Target).Status = s.Status;
                    if (s.Status != StatusCondition.None)
                    {
                        Audio.Play("status");
                        BattleFx.Status(_fx, s.Status, CenterOf(s.Target));
                        yield return Wait.Seconds(0.4f);
                    }
                    break;
                case StatusTickEvent t:
                    Audio.Play(t.Status == StatusCondition.Sleep ? "blip" : "status", 0.6f);
                    BattleFx.Status(_fx, t.Status, CenterOf(t.Target));
                    yield return Wait.Seconds(0.45f);
                    break;
                case StatStageEvent st:
                    Audio.Play(st.Delta > 0 ? "statup" : "statdown");
                    for (int i = 0; i < 3; i++) { BattleFx.StatChange(_glow, CenterOf(st.Target), st.Delta > 0); yield return Wait.Seconds(0.12f); }
                    yield return Wait.Seconds(0.2f);
                    break;
                case FaintEvent f: yield return Faint(f.Side); break;
                case XpEvent x: yield return XpFill(x); break;
                case LevelUpEvent l: yield return LevelUpAnim(l); break;
                case LearnMoveEvent lm: yield return LearnMove(lm); break;
                case OrbThrowEvent o: yield return OrbThrow(o); break;
                case TrainerReturnsEvent: yield return Tween(0.5f, t => _enemyTrainerX = 1 - t); break;
                case MoneyEvent: Audio.Play("buy"); break;
                case EndEvent end:
                    if (end.Outcome == BattleOutcome.Won) Audio.PlayMusic("victory", 0.2f);
                    break;
            }
        }
        _text.WaitForConfirmOnLast = true;
    }

    private IEnumerator SendOut(SendOutEvent s)
    {
        var d = Of(s.Side);
        d.Creature = s.Creature;
        d.Hp = s.Hp;
        d.MaxHp = s.Creature.MaxHp;
        d.Level = s.Creature.Level;
        d.Status = s.Creature.Status;
        d.Offset = Vector2.Zero;
        d.SinkDepth = 0;
        if (s.Side == Side.Player) d.Xp = s.Creature.XpFraction;
        if (s.Side == Side.Enemy && _setup.Trainer is null)
        {
            d.Visible = true;
            d.HudVisible = true;
            Audio.Play("sendout", 0.6f);
            yield return Wait.Seconds(0.2f);
            yield break;
        }
        // Trainers step aside, an orb arcs onto the platform and bursts open.
        if (s.Side == Side.Enemy && _enemyTrainerX < 1) yield return Tween(0.4f, t => _enemyTrainerX = t);
        if (s.Side == Side.Player && _playerTrainerX < 1) yield return Tween(0.4f, t => _playerTrainerX = t);
        var target = BaseOf(s.Side) + new Vector2(0, s.Side == Side.Player ? -30 : -18);
        var start = s.Side == Side.Player ? new Vector2(-10, 110) : new Vector2(330, 30);
        _orbSprite = "orb";
        ShowOrb();
        Audio.Play("throw", 0.7f);
        yield return Tween(0.35f, t =>
        {
            _orbPos = Vector2.Lerp(start, target, t) + new Vector2(0, -MathF.Sin(t * MathF.PI) * 24);
            _orbAngle = t * 12;
        }, Ease.Linear);
        _orbVisible = false;
        _orbAngle = 0;
        Audio.Play("sendout");
        _flash = 0.6f;
        BattleFx.Stars(_glow, target);
        _sprites.Play("spark-ring", target, 20);
        d.Visible = true;
        d.White = 1;
        yield return Tween(0.28f, t => { d.Scale = t; }, Ease.OutBack);
        yield return Tween(0.2f, t => d.White = 1 - t);
        d.HudVisible = true;
        Audio.Play("atk:" + s.Creature.Type, 0.4f);
    }

    private IEnumerator Withdraw(Side side)
    {
        var d = Of(side);
        d.White = 1;
        Audio.Play("sendout", 0.5f, 0.8f);
        yield return Tween(0.25f, t => d.Scale = 1 - t, Ease.InQuad);
        d.Visible = false;
        d.HudVisible = false;
        d.Scale = 1;
        d.White = 0;
    }

    private IEnumerator MoveAnimation(Side user, MoveData move)
    {
        var target = user == Side.Player ? Side.Enemy : Side.Player;
        var ud = Of(user);
        float dir = user == Side.Player ? 1 : -1;
        Audio.Play("atk:" + move.Type, 0.8f);
        if (!move.IsDamaging)
        {
            bool self = move.Effect is MoveEffect.RaiseSelf or MoveEffect.HealSelf;
            var at = CenterOf(self ? user : target);
            ud.Tint = Pal.TypeColor(move.Type);
            if (move.Effect == MoveEffect.RaiseSelf) _sprites.Play("boost", BaseOf(user) + new Vector2(0, -2), 14);
            else if (move.Effect == MoveEffect.HealSelf) _sprites.Play("sparkle", at, 14);
            else _sprites.Play("aura", BaseOf(target) + new Vector2(0, -4), 12, tint: Pal.TypeColor(move.Type));
            for (int i = 0; i < 3; i++)
            {
                BattleFx.Attack(_glow, move.Type, at, false);
                yield return Tween(0.12f, t =>
                {
                    ud.Offset = new Vector2(0, -MathF.Sin(t * MathF.PI) * 3);
                    ud.TintAlpha = 0.35f * MathF.Sin(t * MathF.PI);
                });
            }
            ud.TintAlpha = 0;
            yield break;
        }
        if (move.Category == MoveCategory.Physical)
        {
            // Anticipation: crouch back and down, a beat, then a fast lunge that stops dead on contact.
            yield return Tween(0.15f, t => ud.Offset = new Vector2(-dir * 8 * t, 2 * t));
            yield return Wait.Seconds(0.05f);
            yield return Tween(0.09f, t => ud.Offset = new Vector2(-dir * 8 + dir * 34 * t, 2 - (dir * 10 + 2) * t), Ease.InQuad);
            Impact(move, target, user == Side.Enemy);
            BattleFx.Attack(_glow, move.Type, CenterOf(target), false);
            yield return Wait.Seconds(0.06f);
            var reach = ud.Offset;
            yield return Tween(0.2f, t => ud.Offset = reach * (1 - t));
            ud.Offset = Vector2.Zero;
        }
        else
        {
            // Charge: the user glows in its element's colour and rises, then a projectile crosses the field.
            ud.Tint = Pal.TypeColor(move.Type);
            yield return Tween(0.22f, t =>
            {
                ud.TintAlpha = 0.6f * MathF.Sin(t * MathF.PI);
                ud.Offset = new Vector2(-dir * 2 * MathF.Sin(t * MathF.PI), -2 * MathF.Sin(t * MathF.PI));
            });
            ud.TintAlpha = 0;
            ud.Offset = Vector2.Zero;
            var from = CenterOf(user);
            var to = CenterOf(target);
            yield return Tween(0.3f, t =>
            {
                var p = Vector2.Lerp(from, to, t) + new Vector2(0, -MathF.Sin(t * MathF.PI) * 16);
                BattleFx.Trail(_glow, move.Type, p);
                BattleFx.Trail(_fx, move.Type, p);
            }, Ease.Linear);
            Impact(move, target, user == Side.Enemy);
            BattleFx.Attack(_fx, move.Type, to, false);
            BattleFx.Attack(_glow, move.Type, to, false);
            yield return Wait.Seconds(0.15f);
        }
    }

    /// <summary>The element's hand-animated impact on the target, mirrored when the enemy attacks.</summary>
    private void Impact(MoveData move, Side target, bool mirrored)
    {
        bool big = move.Power >= 80;
        var at = CenterOf(target);
        var feet = BaseOf(target) + new Vector2(0, target == Side.Player ? -4 : -2);
        switch (move.Type)
        {
            case ElementType.Volt:
                // Two bolts strike a beat apart, then the spark ring.
                _sprites.Play("thunder", feet + new Vector2(-7, -6), 18);
                _sprites.Play("thunder", feet + new Vector2(8, -2), 18, delay: 0.08f, flipX: true);
                if (big) _sprites.Play("spark-ring", at, 18, delay: 0.12f);
                break;
            case ElementType.Tide when big:
            case ElementType.Stone when big:
                _sprites.Play(FxArt.Impact(move.Type, big), feet, 16, flipX: mirrored);
                break;
            case ElementType.Leaf:
                _sprites.Play("leaves", at, 18, flipX: mirrored);
                if (big) _sprites.Play("leaves", at + new Vector2(6, 4), 18, delay: 0.1f, flipX: !mirrored);
                break;
            default:
                _sprites.Play(FxArt.Impact(move.Type, big), at, move.Type == ElementType.Gale ? 14 : 18, flipX: mirrored);
                break;
        }
    }

    private IEnumerator Damage(DamageEvent d)
    {
        var disp = Of(d.Target);
        string sound = d.Effectiveness > 1 ? "supereffective" : d.Effectiveness < 1 ? "weakhit" : "hit";
        Audio.Play(sound);
        if (d.Effectiveness > 1 || d.Critical) { _shake = 1; _flash = 0.35f; }
        BattleFx.Hit(_glow, CenterOf(d.Target));
        // Impact: a white flash and a knockback away from the attacker, then the classic blinks.
        float away = d.Target == Side.Player ? -1 : 1;
        float push = d.Effectiveness > 1 || d.Critical ? 9 : d.Effectiveness < 1 ? 3 : 6;
        disp.White = 1;
        disp.Offset = new Vector2(away * push, 0);
        yield return Wait.Seconds(0.07f);
        disp.White = 0;
        yield return Tween(0.16f, t => disp.Offset = new Vector2(away * push * (1 - t), 0));
        for (int i = 0; i < 3; i++)
        {
            disp.Blink = true;
            disp.Offset = new Vector2(i % 2 == 0 ? 3 : -3, 0);
            yield return Wait.Seconds(0.06f);
            disp.Blink = false;
            disp.Offset = Vector2.Zero;
            yield return Wait.Seconds(0.05f);
        }
        yield return DrainHp(disp, d.HpAfter);
    }

    private IEnumerator DrainHp(Display disp, int target)
    {
        float speed = MathF.Max(18, disp.MaxHp * 1.1f);
        while (MathF.Abs(disp.Hp - target) > 0.01f)
        {
            disp.Hp = MathX.Approach(disp.Hp, target, speed * EngineStep);
            yield return null;
        }
    }

    private IEnumerator HealAnim(HealEvent h)
    {
        var disp = Of(h.Target);
        Audio.Play("heal", 0.7f);
        BattleFx.Healing(_glow, CenterOf(h.Target));
        _sprites.Play("sparkle", CenterOf(h.Target), 14);
        yield return DrainHp(disp, h.HpAfter);
    }

    private IEnumerator Faint(Side side)
    {
        var d = Of(side);
        Audio.Play("faint");
        // A last little lurch upward, then the slide down behind the platform.
        yield return Tween(0.12f, t => d.Offset = new Vector2(0, -3 * MathF.Sin(t * MathF.PI)));
        d.Offset = Vector2.Zero;
        _sprites.Play("smoke", BaseOf(side) + new Vector2(0, side == Side.Player ? -8 : -6), 12, delay: 0.15f);
        yield return Tween(0.4f, t => d.SinkDepth = t, Ease.InQuad);
        d.Visible = false;
        d.SinkDepth = 0;
        d.HudVisible = false;
        yield return Wait.Seconds(0.15f);
    }

    private IEnumerator XpFill(XpEvent x)
    {
        if (x.PartyIndex != _battle.ActiveIndex || _player.Creature != _battle.Party[x.PartyIndex]) yield break;
        float from = _player.Xp;
        if (x.Fraction < from) { _player.Xp = x.Fraction; yield break; }
        Audio.Play("xp", 0.5f);
        float seconds = 0.25f + (x.Fraction - from) * 0.9f;
        yield return Tween(seconds, t => _player.Xp = MathX.Lerp(from, x.Fraction, t), Ease.Linear);
    }

    private IEnumerator LevelUpAnim(LevelUpEvent l)
    {
        bool active = l.PartyIndex == _battle.ActiveIndex && _player.Creature == l.Creature;
        Audio.Play("jingle:levelup");
        if (active)
        {
            _player.Level = l.Info.Level;
            _player.MaxHp = l.Info.After[0];
            _player.Hp = l.HpAfter;
            _player.Xp = 0;
            BattleFx.Stars(_glow, CenterOf(Side.Player));
            BattleFx.StatChange(_glow, CenterOf(Side.Player), true);
        }
        _levelPanel = l.Info;
        _levelPanelName = l.Creature.Name;
        yield return _text.Say($"{l.Creature.Name} grew to Lv. {l.Info.Level}!");
        _levelPanel = null;
    }

    private IEnumerator LearnMove(LearnMoveEvent lm)
    {
        var c = lm.Creature;
        var move = MoveDex.Get(lm.MoveId);
        yield return _text.Say($"{c.Name} wants to learn {move.Name}.");
        yield return _text.Say($"But {c.Name} already knows four moves.");
        _text.WaitForConfirmOnLast = false;
        _text.Show($"Forget a move to make room for {move.Name}?");
        while (!_text.PageComplete) yield return null;
        int yes = -1;
        _forgetOptions = [(move, null)];
        yield return _choice.Choose(["YES", "NO"], i => yes = i);
        _forgetOptions = null;
        _text.WaitForConfirmOnLast = true;
        _text.Close();
        if (yes == 0)
        {
            _text.WaitForConfirmOnLast = false;
            _text.Show("Which move should be forgotten?");
            _text.FinishTyping();
            int slot = -1;
            _forgetOptions = [.. c.Moves.Select(m => (m.Data, (int?)m.Pp)), (move, null)];
            yield return _choice.Choose([.. c.Moves.Select(m => m.Data.Name), "Keep all"], i => slot = i);
            _forgetOptions = null;
            _text.WaitForConfirmOnLast = true;
            _text.Close();
            if (slot >= 0 && slot < c.Moves.Count)
            {
                string old = c.Moves[slot].Data.Name;
                Progression.ReplaceMove(c, slot, move.Id);
                Audio.Play("confirm");
                yield return _text.Say($"1, 2 and... Poof! {c.Name} forgot {old}...");
                yield return _text.Say($"...and learned {move.Name}!");
                yield break;
            }
        }
        yield return _text.Say($"{c.Name} did not learn {move.Name}.");
    }

    private IEnumerator OrbThrow(OrbThrowEvent o)
    {
        _orbSprite = o.Orb.Id == "superorb" ? "orbsuper" : "orb";
        ShowOrb();
        _orbOpen = false;
        var start = new Vector2(40, 120);
        var top = EnemyBase + new Vector2(0, -34);
        var rest = EnemyBase + new Vector2(0, -6);
        Audio.Play("throw");
        yield return Tween(0.5f, t =>
        {
            _orbPos = Vector2.Lerp(start, top, t) + new Vector2(0, -MathF.Sin(t * MathF.PI) * 40);
            _orbAngle = t * 14;
        }, Ease.Linear);
        _orbAngle = 0;
        _orbOpen = true;
        Audio.Play("sendout", 0.8f, 1.2f);
        _enemy.White = 1;
        BattleFx.Stars(_glow, top);
        yield return Tween(0.3f, t => _enemy.Scale = 1 - t, Ease.InQuad);
        _enemy.Visible = false;
        _orbOpen = false;
        yield return Tween(0.35f, t => _orbPos = Vector2.Lerp(top, rest, Ease.OutBounce(t)), Ease.Linear);
        Audio.Play("land", 0.6f);
        yield return Wait.Seconds(0.35f);
        for (int i = 0; i < o.Shakes; i++)
        {
            Audio.Play("shake");
            yield return Tween(0.4f, t => _orbAngle = MathF.Sin(t * MathF.Tau) * 0.45f, Ease.Linear);
            _orbAngle = 0;
            yield return Wait.Seconds(0.35f);
        }
        if (o.Caught)
        {
            Audio.Play("click");
            BattleFx.Stars(_glow, rest + new Vector2(0, -4));
            _sprites.Play("spark-ring", rest + new Vector2(0, -4), 16);
            _enemy.HudVisible = false;
            yield return Wait.Seconds(0.3f);
            Audio.Play("jingle:catch");
            yield return Wait.Seconds(0.9f);
            yield break;
        }
        Audio.Play("breakfree");
        _orbVisible = false;
        _flash = 0.5f;
        BattleFx.Stars(_glow, rest);
        _enemy.Visible = true;
        yield return Tween(0.25f, t => { _enemy.Scale = t; _enemy.White = 1 - t; }, Ease.OutBack);
        _enemy.White = 0;
    }
}
