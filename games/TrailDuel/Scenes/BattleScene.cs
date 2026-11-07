using System.Collections;
using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using TrailDuel.Rules;
using TrailDuel.Ui;
using TrailDuel.World;

namespace TrailDuel.Scenes;

public sealed record BattleSetup(Backdrop Backdrop, string Music, TrainerDef? Trainer);

/// <summary>
/// The battle screen. A flow coroutine alternates between the player's choice (menus) and the
/// presentation of the events the rules produced, so every message, HP drain and animation
/// plays in order. The scene pops itself and reports the outcome when the battle ends.
/// </summary>
internal sealed partial class BattleScene : Scene
{
    private enum Mode { Busy, Actions, Moves }

    private static readonly string[] ActionLabels = ["FIGHT", "BAG", "PARTY", "RUN"];

    private readonly TrailDuelGame _game;
    private readonly Battle _battle;
    private readonly BattleSetup _setup;
    private readonly Action<BattleOutcome> _onEnd;
    private TextPanel _text = null!;
    private Mode _mode = Mode.Busy;
    private int _actionIndex, _moveIndex;
    /// <summary>Whether the full move card is open over the field (toggled with the action button in the move menu).</summary>
    private bool _moveInfo;
    /// <summary>While choosing a move to forget: the moves on offer (the last one is the new move, for "Keep all").</summary>
    private (MoveData Move, int? Pp)[]? _forgetOptions;
    private PlayerAction? _pending;
    private float _menuTime, _lowHpBeep;
    private bool _finished;

    public BattleScene(TrailDuelGame game, Battle battle, BattleSetup setup, Action<BattleOutcome> onEnd)
    {
        _game = game;
        _sprites = new SpriteFx(game.Atlas);
        _battle = battle;
        _setup = setup;
        _onEnd = onEnd;
    }

    public Battle Battle => _battle;
    public bool AwaitingChoice => _mode != Mode.Busy;
    /// <summary>On touch screens: while the battle narrates, only A and B; menus get every control.</summary>
    public override IReadOnlyCollection<string>? TouchActions => _mode == Mode.Busy && _choice is not { Menu: not null } ? OverworldScene.TextActions : null;
    internal bool InMoveMenu => _mode == Mode.Moves;
    internal int MoveCursor => _moveIndex;
    internal bool MoveInfoOpen => _moveInfo;

    /// <summary>
    /// State hash, in sections: <c>battle.rules</c> (the <see cref="Rules.Battle"/>: generator, slots, enemy team, stages,
    /// phase, outcome) and <c>battle.screen</c> (menu mode and cursors, the move card toggle, the pending action, the
    /// moves offered to forget, the low-HP beep timer, whether it ended, the text box, the open choice and what each
    /// side shows: creature, level, displayed HP and XP, status and visibility, which lag behind the rules while
    /// animating). Left out: particles, sprite effects, shakes, flashes, slides, lunges, tints, the orb's flight
    /// (presentation the flow coroutine drives; the coroutine is hashed by the engine, and the slid positions are in the
    /// scene's objects, which the engine hashes too) and the label caches drawing fills.
    /// </summary>
    public override void HashState(StateHasher hash)
    {
        hash.Section("battle.rules");
        hash.Add(_battle);
        hash.Section("battle.screen");
        hash.Add((int)_mode);
        hash.Add(_actionIndex);
        hash.Add(_moveIndex);
        hash.Add(_moveInfo);
        hash.Add(_pending?.GetType().Name);
        hash.Add(_forgetOptions?.Length ?? -1);
        hash.Add(_lowHpBeep);
        hash.Add(_finished);
        hash.Add(_text);
        hash.Add(_choice);
        AddDisplay(hash, _enemy);
        AddDisplay(hash, _player);
    }

    private static void AddDisplay(StateHasher hash, Display? d)
    {
        hash.Add(d is not null);
        if (d is null) return;
        hash.Add(d.Creature.SpeciesId);
        hash.Add(d.Level);
        hash.Add(d.MaxHp);
        hash.Add(d.Hp);
        hash.Add(d.Xp);
        hash.Add((int)d.Status);
        hash.Add(d.Visible);
        hash.Add(d.HudVisible);
    }

    public override void Enter()
    {
        _text = new TextPanel(Font, Audio) { Area = new RectF(4, 134, 312, 42), AutoAdvance = 1.6f };
        _choice = new ChoicePrompt(Font, Audio);
        Audio.PlayMusic(_setup.Music, 0f);
        _enemy = new Display { Creature = _battle.Enemy, Hp = _battle.Enemy.Hp, Visible = _setup.Trainer is null };
        _player = new Display { Creature = _battle.Player, Hp = _battle.Player.Hp, Xp = _battle.Player.XpFraction };
        CreateObjects();
        Coroutines.Start(Flow());
    }

    /// <summary>Debug only (TRAIL_DEBUG=fx): replays these moves instead of running the battle, for effect captures.</summary>
    internal string[]? Showcase { get; init; }

    /// <summary>Debug only (TRAIL_DEBUG=learn): offers this move to the active creature right after the intro.</summary>
    internal string? LearnShowcase { get; init; }

    private IEnumerator Flow()
    {
        yield return Intro();
        if (LearnShowcase is { } learn)
        {
            yield return Present(_battle.Start());
            yield return Present([new LearnMoveEvent(_battle.ActiveIndex, _battle.Player, learn)]);
        }
        if (Showcase is { } moves)
        {
            yield return Present(_battle.Start());
            for (int i = 0; ; i++)
            {
                var user = i % 2 == 0 ? Side.Player : Side.Enemy;
                var move = MoveDex.Get(moves[i / 2 % moves.Length]);
                var target = user == Side.Player ? Side.Enemy : Side.Player;
                _lastLine = $"{Of(user).Creature.Name} used {move.Name}!";
                _text.Show(_lastLine);
                yield return Present([new MoveUsedEvent(user, move)]);
                if (move.IsDamaging) yield return Present([new DamageEvent(target, 1, (int)Of(target).Hp, 1, false)]);
                yield return Wait.Seconds(0.6f);
            }
        }
        yield return Present(_battle.Start());
        while (_battle.Phase != BattlePhase.Ended)
        {
            if (_battle.Phase == BattlePhase.ForcedSwitch)
            {
                int pick = -1;
                yield return PickParty(forced: true, i => pick = i);
                yield return Present(_battle.SubmitReplacement(pick));
                continue;
            }
            PlayerAction? action = null;
            yield return ChooseAction(a => action = a);
            yield return Present(_battle.Submit(action!));
        }
        yield return Outro();
    }

    private IEnumerator ChooseAction(Action<PlayerAction> chosen)
    {
        while (true)
        {
            _pending = null;
            _mode = Mode.Actions;
            _menuTime = 0;
            _text.WaitForConfirmOnLast = false;
            _text.Show($"What will {_battle.Player.Name} do?");
            _text.FinishTyping();
            while (_pending is null) yield return null;
            _mode = Mode.Busy;
            _text.WaitForConfirmOnLast = true;
            _text.Close();
            if (_battle.CanSubmit(_pending, out var reason)) { chosen(_pending); yield break; }
            Audio.Play("deny");
            yield return _text.Say(reason);
        }
    }

    public override void Update(float dt)
    {
        Advance(dt);
        Place();
    }

    private void Advance(float dt)
    {
        _menuTime += dt;
        _text.Update(dt, Input);
        _choice.Update(dt, Input);
        UpdateEffects(dt);
        if (_mode == Mode.Busy) return;
        if (_battle.Player.HpFraction <= 0.2f && !_battle.Player.Fainted)
        {
            _lowHpBeep -= dt;
            if (_lowHpBeep <= 0) { Audio.Play("lowhp", 0.5f); _lowHpBeep = 0.55f; }
        }
        if (_mode == Mode.Actions) UpdateActions();
        else UpdateMoves();
    }

    /// <summary>
    /// The action and move menus are two-by-two grids (left and right change column), which the engine's vertical
    /// <c>DemoKit.OptionList</c> does not cover, so their cursor stays here.
    /// </summary>
    private int GridMove(int index, int count)
    {
        int col = index % 2, row = index / 2;
        if (Input.Pressed("left") && col == 1) col = 0;
        else if (Input.Pressed("right") && col == 0 && row * 2 + 1 < count) col = 1;
        else if (Input.Pressed("up") && row == 1) row = 0;
        else if (Input.Pressed("down") && row == 0 && 2 + col < count) row = 1;
        int next = row * 2 + col;
        if (next != index) Audio.Play("move", 0.6f);
        return next;
    }

    private void UpdateActions()
    {
        _actionIndex = GridMove(_actionIndex, 4);
        if (!Input.Pressed("confirm")) return;
        Audio.Play("confirm", 0.7f);
        switch (_actionIndex)
        {
            case 0:
                _mode = Mode.Moves;
                _moveIndex = Math.Min(_moveIndex, _battle.Player.Moves.Count - 1);
                if (_battle.Player.Moves.All(m => m.Pp <= 0)) _pending = new PlayerAction.Fight(-1);
                break;
            case 1:
                _mode = Mode.Busy;
                Game.Scenes.Push(new BagScene(_game, _battle, (item, target) =>
                {
                    if (item is null) _mode = Mode.Actions;
                    else _pending = new PlayerAction.UseItem(item, target);
                }));
                break;
            case 2:
                _mode = Mode.Busy;
                Game.Scenes.Push(new PartyScene(_game, PartyScene.Purpose.BattleSwitch, i =>
                {
                    if (i < 0) _mode = Mode.Actions;
                    else _pending = new PlayerAction.Switch(i);
                }, activeIndex: _battle.ActiveIndex));
                break;
            default:
                _pending = new PlayerAction.Run();
                break;
        }
    }

    private void UpdateMoves()
    {
        if (Input.Pressed("cancel")) { Audio.Play("cancel", 0.7f); _mode = Mode.Actions; return; }
        if (Input.Pressed("action")) { _moveInfo = !_moveInfo; Audio.Play("move", 0.6f); }
        _moveIndex = GridMove(_moveIndex, _battle.Player.Moves.Count);
        if (Input.Pressed("confirm"))
        {
            Audio.Play("confirm", 0.7f);
            _pending = new PlayerAction.Fight(_moveIndex);
        }
    }

    private IEnumerator PickParty(bool forced, Action<int> picked)
    {
        int result = -2;
        Game.Scenes.Push(new PartyScene(_game, forced ? PartyScene.Purpose.ForcedSwitch : PartyScene.Purpose.BattleSwitch, i => result = i, activeIndex: _battle.ActiveIndex));
        while (result == -2) yield return null;
        picked(result);
    }

    private IEnumerator Outro()
    {
        var outcome = _battle.Outcome;
        if (outcome == BattleOutcome.Won && _setup.Trainer is null) yield return Wait.Seconds(0.2f);
        if (outcome == BattleOutcome.Ran) Audio.Play("run");
        yield return Wait.Seconds(outcome == BattleOutcome.Won ? 0.6f : 0.3f);
        if (_finished) yield break;
        _finished = true;
        _onEnd(outcome);
        var color = outcome == BattleOutcome.Lost ? Visuals.Pal.Ink : Color.Black;
        Game.Scenes.Pop(Transition.Fade(0.8f, color));
    }

    /// <summary>The menus drawn beside the text box while learning a move (forget one? which one?).</summary>
    private ChoicePrompt _choice = null!;
}
