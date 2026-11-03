using MicroEngine.Kernel;

namespace TrailDuel.Rules;

/// <summary>An opposing trainer: name, team, prize and dialogue for the end of the duel.</summary>
public sealed record TrainerInfo(string Name, IReadOnlyList<Creature> Team, int Prize, string DefeatLine, bool Smart = true, bool IsMaster = false);

public enum BattlePhase { ChooseAction, ForcedSwitch, Ended }

/// <summary>
/// Turn-based battle rules. The player's party list is mutated in place (HP, XP, PP, captures), so
/// the overworld simply keeps using it afterwards. Each call returns the events that happened.
/// </summary>
public sealed class Battle : IStateHashed
{
    public const int MaxParty = 6;
    private static readonly MoveData Struggle = new("struggle", "Struggle", ElementType.Normal, MoveCategory.Physical, 50, 0, 1, "A desperate attack.");

    private readonly Random _rng;
    private readonly HashSet<int> _participants = [];
    private int _escapeAttempts;
    private bool _enemyFainted, _playerFainted;

    public List<Creature> Party { get; }
    public IReadOnlyList<Creature> EnemyTeam { get; }
    public TrainerInfo? Trainer { get; }
    public Bag Bag { get; }
    public int ActiveIndex { get; private set; }
    public int EnemyIndex { get; private set; }
    public int[] PlayerStages { get; } = new int[4];
    public int[] EnemyStages { get; } = new int[4];
    public BattlePhase Phase { get; private set; } = BattlePhase.ChooseAction;
    public BattleOutcome Outcome { get; private set; }
    /// <summary>The creature caught this battle, if any; <see cref="SentToBox"/> when the party was full.</summary>
    public Creature? Caught { get; private set; }
    public bool SentToBox { get; private set; }
    public int Turn { get; private set; }

    public bool IsTrainer => Trainer is not null;
    public Creature Player => Party[ActiveIndex];
    public Creature Enemy => EnemyTeam[EnemyIndex];

    public Battle(List<Creature> party, IReadOnlyList<Creature> enemyTeam, TrainerInfo? trainer, Bag bag, Random rng)
    {
        Party = party ?? throw new ArgumentNullException(nameof(party));
        EnemyTeam = enemyTeam ?? throw new ArgumentNullException(nameof(enemyTeam));
        if (EnemyTeam.Count == 0) throw new ArgumentException("The enemy needs at least one creature.", nameof(enemyTeam));
        if (Party.All(c => c.Fainted)) throw new ArgumentException("The party has no creature able to fight.", nameof(party));
        Trainer = trainer;
        Bag = bag ?? throw new ArgumentNullException(nameof(bag));
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        ActiveIndex = Party.FindIndex(c => !c.Fainted);
        _participants.Add(ActiveIndex);
    }

    /// <summary>
    /// State hash: the generator (when it is a <see cref="SeededRandom"/>, as the game always passes; a plain
    /// <see cref="Random"/> from a unit test adds only that it is not there), the active and enemy slots, the enemy team,
    /// stat stages, phase, outcome, turn, escape attempts, faint flags, the creatures that took part (sorted) and the
    /// catch. The player's party and bag are the adventure's own lists, hashed by <see cref="GameState"/>.
    /// </summary>
    public void HashState(StateHasher hash)
    {
        hash.Add(_rng as IStateHashed);
        hash.Add(Trainer?.Name);
        hash.Add(ActiveIndex);
        hash.Add(EnemyIndex);
        GameState.AddAll(hash, EnemyTeam);
        foreach (int stage in PlayerStages) hash.Add(stage);
        foreach (int stage in EnemyStages) hash.Add(stage);
        hash.Add((int)Phase);
        hash.Add((int)Outcome);
        hash.Add(Turn);
        hash.Add(_escapeAttempts);
        hash.Add(_enemyFainted);
        hash.Add(_playerFainted);
        hash.Add(_participants.Count);
        foreach (int index in _participants.Order()) hash.Add(index);
        hash.Add(Caught);
        hash.Add(SentToBox);
    }

    private string EnemyName => (IsTrainer ? "Foe " : "Wild ") + Enemy.Name;
    private string NameOf(Side side) => side == Side.Player ? Player.Name : EnemyName;
    private Creature Of(Side side) => side == Side.Player ? Player : Enemy;
    private int[] StagesOf(Side side) => side == Side.Player ? PlayerStages : EnemyStages;

    public List<BattleEvent> Start()
    {
        var ev = new List<BattleEvent>();
        if (Trainer is { } t)
        {
            ev.Add(new MessageEvent($"{t.Name} wants to duel!"));
            ev.Add(new MessageEvent($"{t.Name} sent out {Enemy.Name}!"));
        }
        else ev.Add(new MessageEvent($"A wild {Enemy.Name} appeared!"));
        ev.Add(new SendOutEvent(Side.Enemy, Enemy, Enemy.Hp, EnemyIndex));
        ev.Add(new MessageEvent($"Go, {Player.Name}!"));
        ev.Add(new SendOutEvent(Side.Player, Player, Player.Hp, ActiveIndex));
        _participants.Add(ActiveIndex);
        return ev;
    }

    /// <summary>Checks whether an action is allowed right now; <paramref name="reason"/> explains a refusal.</summary>
    public bool CanSubmit(PlayerAction action, out string reason)
    {
        reason = "";
        if (Phase != BattlePhase.ChooseAction) { reason = "Not now!"; return false; }
        switch (action)
        {
            case PlayerAction.Fight f:
                if (f.MoveSlot < 0 && Player.Moves.Any(m => m.Pp > 0)) { reason = "Choose a move!"; return false; }
                if (f.MoveSlot >= Player.Moves.Count) { reason = "No such move."; return false; }
                if (f.MoveSlot >= 0 && Player.Moves[f.MoveSlot].Pp <= 0) { reason = "No PP left for this move!"; return false; }
                return true;
            case PlayerAction.Switch s:
                return CanSwitchTo(s.PartyIndex, out reason);
            case PlayerAction.UseItem u:
                var item = ItemDex.Get(u.ItemId);
                if (Bag.Count(item.Id) <= 0) { reason = $"You have no {item.Name}."; return false; }
                if (item.Kind == ItemKind.Orb)
                {
                    if (IsTrainer) { reason = "You can't catch another trainer's creature!"; return false; }
                    return true;
                }
                if (u.PartyIndex < 0 || u.PartyIndex >= Party.Count || !ItemDex.CanUseOn(item, Party[u.PartyIndex])) { reason = "It won't have any effect."; return false; }
                return true;
            case PlayerAction.Run:
                if (IsTrainer) { reason = "There's no running from a trainer duel!"; return false; }
                return true;
        }
        return false;
    }

    private bool CanSwitchTo(int index, out string reason)
    {
        reason = "";
        if (index < 0 || index >= Party.Count) { reason = "No such creature."; return false; }
        if (Party[index].Fainted) { reason = $"{Party[index].Name} has no energy left to fight!"; return false; }
        if (index == ActiveIndex) { reason = $"{Party[index].Name} is already out!"; return false; }
        return true;
    }

    /// <summary>Resolves one full turn for the player's action and the enemy's response.</summary>
    public List<BattleEvent> Submit(PlayerAction action)
    {
        if (!CanSubmit(action, out var reason)) throw new InvalidOperationException(reason);
        var ev = new List<BattleEvent>();
        Turn++;
        _enemyFainted = _playerFainted = false;
        int enemyMove = TrainerAI.ChooseMove(Enemy, EnemyStages, Player, PlayerStages, Trainer?.Smart ?? false, _rng);

        switch (action)
        {
            case PlayerAction.Run:
                _escapeAttempts++;
                if (Formulas.RollEscape(Formulas.EffectiveSpeed(Player, PlayerStages), Formulas.EffectiveSpeed(Enemy, EnemyStages), _escapeAttempts, _rng))
                {
                    ev.Add(new MessageEvent("Got away safely!"));
                    return End(ev, BattleOutcome.Ran);
                }
                ev.Add(new MessageEvent("Couldn't get away!"));
                break;
            case PlayerAction.Switch s:
                ev.Add(new MessageEvent($"Come back, {Player.Name}!"));
                ev.Add(new WithdrawEvent(Side.Player, Player));
                SendOutPlayer(s.PartyIndex, ev);
                break;
            case PlayerAction.UseItem u:
                if (UseItem(ItemDex.Get(u.ItemId), u.PartyIndex, ev)) return ev;
                break;
        }

        if (action is PlayerAction.Fight fight)
        {
            bool playerFirst = PlayerMovesFirst(fight.MoveSlot, enemyMove);
            if (playerFirst) { ExecuteMove(Side.Player, fight.MoveSlot, ev); ExecuteMove(Side.Enemy, enemyMove, ev); }
            else { ExecuteMove(Side.Enemy, enemyMove, ev); ExecuteMove(Side.Player, fight.MoveSlot, ev); }
        }
        else ExecuteMove(Side.Enemy, enemyMove, ev);

        StatusTick(Side.Player, ev);
        StatusTick(Side.Enemy, ev);
        ResolveFaints(ev);
        return ev;
    }

    /// <summary>After the active creature fainted, the player picks a replacement (no enemy move).</summary>
    public List<BattleEvent> SubmitReplacement(int partyIndex)
    {
        if (Phase != BattlePhase.ForcedSwitch) throw new InvalidOperationException("No replacement is needed.");
        if (!CanSwitchTo(partyIndex, out var reason)) throw new InvalidOperationException(reason);
        var ev = new List<BattleEvent>();
        SendOutPlayer(partyIndex, ev);
        Phase = BattlePhase.ChooseAction;
        return ev;
    }

    private void SendOutPlayer(int index, List<BattleEvent> ev)
    {
        ActiveIndex = index;
        Array.Clear(PlayerStages);
        _participants.Add(index);
        ev.Add(new MessageEvent($"Go, {Player.Name}!"));
        ev.Add(new SendOutEvent(Side.Player, Player, Player.Hp, index));
    }

    private bool PlayerMovesFirst(int playerSlot, int enemySlot)
    {
        int pp = playerSlot >= 0 ? Player.Moves[playerSlot].Data.Priority : 0;
        int ep = enemySlot >= 0 ? Enemy.Moves[enemySlot].Data.Priority : 0;
        if (pp != ep) return pp > ep;
        float ps = Formulas.EffectiveSpeed(Player, PlayerStages), es = Formulas.EffectiveSpeed(Enemy, EnemyStages);
        return ps != es ? ps > es : _rng.Next(2) == 0;
    }

    /// <summary>Returns true when the item ended the battle (a capture).</summary>
    private bool UseItem(ItemData item, int partyIndex, List<BattleEvent> ev)
    {
        Bag.Remove(item.Id);
        if (item.Kind != ItemKind.Orb)
        {
            var target = Party[partyIndex];
            ev.Add(new MessageEvent($"You used a {item.Name}."));
            string text = ItemDex.Apply(item, target);
            if (partyIndex == ActiveIndex)
            {
                if (item.Kind == ItemKind.Heal) ev.Add(new HealEvent(Side.Player, target.Hp));
                else ev.Add(new StatusEvent(Side.Player, StatusCondition.None));
            }
            ev.Add(new MessageEvent(text));
            return false;
        }

        ev.Add(new MessageEvent($"You threw a {item.Name}!"));
        var (shakes, caught) = Formulas.RollCapture(Enemy, item.OrbMultiplier, _rng);
        ev.Add(new OrbThrowEvent(item, shakes, caught));
        if (!caught)
        {
            ev.Add(new MessageEvent(shakes switch
            {
                0 => $"Oh no! {Enemy.Name} broke free!",
                1 => "Aww! It appeared to be caught!",
                2 => "Argh! It was so close!",
                _ => "Shoot! Almost had it!",
            }));
            return false;
        }
        ev.Add(new MessageEvent($"Gotcha! {Enemy.Name} was caught!"));
        var creature = Enemy;
        Caught = creature;
        if (Party.Count < MaxParty)
        {
            Party.Add(creature);
            ev.Add(new MessageEvent($"{creature.Name} joined your party!"));
        }
        else
        {
            SentToBox = true;
            ev.Add(new MessageEvent($"Your party is full. {creature.Name} was sent to the Shrine Box."));
        }
        End(ev, BattleOutcome.Caught);
        return true;
    }

    private void ExecuteMove(Side side, int slot, List<BattleEvent> ev)
    {
        if (Phase == BattlePhase.Ended) return;
        var user = Of(side);
        var targetSide = side == Side.Player ? Side.Enemy : Side.Player;
        var target = Of(targetSide);
        if (user.Fainted || target.Fainted) return;

        if (user.Status == StatusCondition.Sleep)
        {
            if (--user.SleepTurns > 0)
            {
                ev.Add(new StatusTickEvent(side, StatusCondition.Sleep));
                ev.Add(new MessageEvent($"{NameOf(side)} is fast asleep."));
                return;
            }
            user.Status = StatusCondition.None;
            ev.Add(new StatusEvent(side, StatusCondition.None));
            ev.Add(new MessageEvent($"{NameOf(side)} woke up!"));
        }

        MoveData move;
        if (slot < 0) move = Struggle;
        else
        {
            var moveSlot = user.Moves[slot];
            moveSlot.Pp = Math.Max(0, moveSlot.Pp - 1);
            move = moveSlot.Data;
        }
        ev.Add(new MessageEvent($"{NameOf(side)} used {move.Name}!"));
        bool selfTargeted = move.Effect is MoveEffect.RaiseSelf or MoveEffect.HealSelf;
        if (!selfTargeted && !Formulas.RollHit(move, _rng))
        {
            ev.Add(new MessageEvent($"{NameOf(side)}'s attack missed!"));
            return;
        }
        ev.Add(new MoveUsedEvent(side, move));

        if (move.IsDamaging)
        {
            var result = Formulas.Damage(user, StagesOf(side), target, StagesOf(targetSide), move, _rng);
            if (result.Effectiveness == 0)
            {
                ev.Add(new MessageEvent($"It doesn't affect {NameOf(targetSide)}..."));
                return;
            }
            int dealt = Math.Min(result.Damage, target.Hp);
            target.Hp -= dealt;
            ev.Add(new DamageEvent(targetSide, dealt, target.Hp, result.Effectiveness, result.Critical));
            if (result.Critical) ev.Add(new MessageEvent("A critical hit!"));
            if (TypeChart.Message(result.Effectiveness) is { } msg) ev.Add(new MessageEvent(msg));

            if (move.Effect == MoveEffect.Drain && user.Hp < user.MaxHp)
            {
                user.Heal(Math.Max(1, dealt / 2));
                ev.Add(new HealEvent(side, user.Hp));
                ev.Add(new MessageEvent($"{NameOf(targetSide)} had its energy drained!"));
            }
            if (move == Struggle)
            {
                int recoil = Math.Min(user.Hp, Math.Max(1, dealt / 4));
                user.Hp -= recoil;
                ev.Add(new DamageEvent(side, recoil, user.Hp, 1, false));
                ev.Add(new MessageEvent($"{NameOf(side)} is hit with recoil!"));
            }
            if (!target.Fainted && move.EffectChance > 0 && _rng.Next(100) < move.EffectChance)
                ApplyEffect(move, side, targetSide, ev, secondary: true);
        }
        else ApplyEffect(move, side, targetSide, ev, secondary: false);

        CheckFaint(targetSide, ev);
        CheckFaint(side, ev);
    }

    private void ApplyEffect(MoveData move, Side userSide, Side targetSide, List<BattleEvent> ev, bool secondary)
    {
        var target = Of(targetSide);
        switch (move.Effect)
        {
            case MoveEffect.Burn:
            case MoveEffect.Poison:
            case MoveEffect.Sleep:
                var status = move.Effect switch { MoveEffect.Burn => StatusCondition.Burn, MoveEffect.Poison => StatusCondition.Poison, _ => StatusCondition.Sleep };
                bool immune = status == StatusCondition.Burn && target.Type == ElementType.Flame;
                if (target.Status != StatusCondition.None || immune)
                {
                    if (!secondary) ev.Add(new MessageEvent("But it failed!"));
                    return;
                }
                target.Status = status;
                if (status == StatusCondition.Sleep) target.SleepTurns = _rng.Next(2, 5);
                ev.Add(new StatusEvent(targetSide, status));
                ev.Add(new MessageEvent(status switch
                {
                    StatusCondition.Burn => $"{NameOf(targetSide)} was burned!",
                    StatusCondition.Poison => $"{NameOf(targetSide)} was poisoned!",
                    _ => $"{NameOf(targetSide)} fell asleep!",
                }));
                break;
            case MoveEffect.RaiseSelf:
                ChangeStage(userSide, move.Stat, move.Stages, ev);
                break;
            case MoveEffect.LowerFoe:
                ChangeStage(targetSide, move.Stat, -move.Stages, ev);
                break;
            case MoveEffect.HealSelf:
                var user = Of(userSide);
                if (user.Hp >= user.MaxHp) { ev.Add(new MessageEvent("But it failed!")); return; }
                user.Heal(Math.Max(1, user.MaxHp / 2));
                ev.Add(new HealEvent(userSide, user.Hp));
                ev.Add(new MessageEvent($"{NameOf(userSide)} regained health!"));
                break;
        }
    }

    private void ChangeStage(Side side, Stat stat, int delta, List<BattleEvent> ev)
    {
        var stages = StagesOf(side);
        int before = stages[(int)stat];
        int after = Math.Clamp(before + delta, -Formulas.MaxStage, Formulas.MaxStage);
        string statName = stat.ToString();
        if (after == before)
        {
            ev.Add(new MessageEvent($"{NameOf(side)}'s {statName} won't go {(delta > 0 ? "higher" : "lower")}!"));
            return;
        }
        stages[(int)stat] = after;
        ev.Add(new StatStageEvent(side, stat, after - before));
        string how = (after - before) switch { >= 2 => "sharply rose", > 0 => "rose", <= -2 => "harshly fell", _ => "fell" };
        ev.Add(new MessageEvent($"{NameOf(side)}'s {statName} {how}!"));
    }

    private void StatusTick(Side side, List<BattleEvent> ev)
    {
        if (Phase == BattlePhase.Ended) return;
        var c = Of(side);
        if (c.Fainted || c.Status is not (StatusCondition.Burn or StatusCondition.Poison)) return;
        int damage = Math.Min(c.Hp, Math.Max(1, c.MaxHp / 8));
        c.Hp -= damage;
        ev.Add(new StatusTickEvent(side, c.Status));
        ev.Add(new DamageEvent(side, damage, c.Hp, 1, false));
        ev.Add(new MessageEvent($"{NameOf(side)} is hurt by {(c.Status == StatusCondition.Burn ? "its burn" : "poison")}!"));
        CheckFaint(side, ev);
    }

    private void CheckFaint(Side side, List<BattleEvent> ev)
    {
        var c = Of(side);
        if (!c.Fainted) return;
        if (side == Side.Enemy)
        {
            if (_enemyFainted) return;
            _enemyFainted = true;
            ev.Add(new FaintEvent(Side.Enemy));
            ev.Add(new MessageEvent($"{EnemyName} fainted!"));
            AwardXp(ev);
        }
        else
        {
            if (_playerFainted) return;
            _playerFainted = true;
            c.Status = StatusCondition.None;
            ev.Add(new FaintEvent(Side.Player));
            ev.Add(new MessageEvent($"{Player.Name} fainted!"));
            _participants.Remove(ActiveIndex);
        }
    }

    private void AwardXp(List<BattleEvent> ev)
    {
        var earners = _participants.Where(i => !Party[i].Fainted).OrderBy(i => i).ToList();
        if (earners.Count == 0) return;
        int total = Formulas.ExperienceFor(Enemy, IsTrainer);
        int share = Math.Max(1, total / earners.Count);
        foreach (int index in earners)
        {
            var c = Party[index];
            if (c.Level >= Creature.MaxLevel) continue;
            ev.Add(new MessageEvent($"{c.Name} gained {share} XP!"));
            var ups = Progression.GainXp(c, share, out var pending);
            foreach (var up in ups)
            {
                ev.Add(new XpEvent(index, 1f));
                ev.Add(new LevelUpEvent(index, c, up, Math.Min(c.MaxHp, HpAtLevel(c, up))));
                ev.Add(new MessageEvent($"{c.Name} grew to Lv. {up.Level}!"));
                foreach (var move in up.NewMoves) ev.Add(new MessageEvent($"{c.Name} learned {MoveDex.Get(move).Name}!"));
            }
            foreach (var move in pending) ev.Add(new LearnMoveEvent(index, c, move));
            ev.Add(new XpEvent(index, c.XpFraction));
        }
    }

    // HP shown at each level-up step: final HP minus the max-HP growth still to come.
    private static int HpAtLevel(Creature c, LevelUp up) => c.Hp - (c.MaxHp - up.After[0]);

    private void ResolveFaints(List<BattleEvent> ev)
    {
        if (Phase == BattlePhase.Ended) return;
        bool playerOut = Party.All(p => p.Fainted);
        if (_enemyFainted)
        {
            int next = -1;
            for (int i = EnemyIndex + 1; i < EnemyTeam.Count; i++) if (!EnemyTeam[i].Fainted) { next = i; break; }
            if (next < 0 && !playerOut)
            {
                if (Trainer is { } t)
                {
                    ev.Add(new TrainerReturnsEvent());
                    ev.Add(new MessageEvent($"You defeated {t.Name}!"));
                    ev.Add(new MessageEvent($"{t.Name}: \"{t.DefeatLine}\""));
                    ev.Add(new MoneyEvent(t.Prize));
                    ev.Add(new MessageEvent($"You got ${t.Prize} for winning!"));
                }
                End(ev, BattleOutcome.Won);
                return;
            }
            if (next >= 0 && !playerOut)
            {
                EnemyIndex = next;
                Array.Clear(EnemyStages);
                _participants.Clear();
                if (!Player.Fainted) _participants.Add(ActiveIndex);
                ev.Add(new MessageEvent($"{Trainer!.Name} sent out {Enemy.Name}!"));
                ev.Add(new SendOutEvent(Side.Enemy, Enemy, Enemy.Hp, EnemyIndex));
            }
        }
        if (playerOut)
        {
            ev.Add(new MessageEvent("You have no creatures left to fight!"));
            ev.Add(new MessageEvent("You hurried back to safety..."));
            End(ev, BattleOutcome.Lost);
            return;
        }
        if (_playerFainted) Phase = BattlePhase.ForcedSwitch;
    }

    private List<BattleEvent> End(List<BattleEvent> ev, BattleOutcome outcome)
    {
        Outcome = outcome;
        Phase = BattlePhase.Ended;
        foreach (var c in Party) if (c.Status == StatusCondition.Sleep && c.Fainted) c.Status = StatusCondition.None;
        ev.Add(new EndEvent(outcome));
        return ev;
    }
}
