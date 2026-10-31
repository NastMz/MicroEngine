namespace TrailDuel.Rules;

public enum Side { Player, Enemy }

public enum BattleOutcome { None, Won, Lost, Ran, Caught }

/// <summary>
/// The battle engine resolves a whole turn instantly and describes it as events. The battle scene
/// replays them one by one with animations, so the rules never depend on presentation timing.
/// Events carry the values to display (HP after a hit, XP fraction...) because the creatures
/// themselves already hold the end-of-turn state.
/// </summary>
public abstract record BattleEvent;

public sealed record MessageEvent(string Text) : BattleEvent;
public sealed record SendOutEvent(Side Side, Creature Creature, int Hp, int PartyIndex) : BattleEvent;
public sealed record WithdrawEvent(Side Side, Creature Creature) : BattleEvent;
public sealed record MoveUsedEvent(Side User, MoveData Move) : BattleEvent;
public sealed record DamageEvent(Side Target, int Amount, int HpAfter, float Effectiveness, bool Critical) : BattleEvent;
public sealed record HealEvent(Side Target, int HpAfter) : BattleEvent;
public sealed record StatusEvent(Side Target, StatusCondition Status) : BattleEvent;
public sealed record StatStageEvent(Side Target, Stat Stat, int Delta) : BattleEvent;
public sealed record StatusTickEvent(Side Target, StatusCondition Status) : BattleEvent;
public sealed record FaintEvent(Side Side) : BattleEvent;
/// <summary>The XP bar of party member <paramref name="PartyIndex"/> fills to <paramref name="Fraction"/>.</summary>
public sealed record XpEvent(int PartyIndex, float Fraction) : BattleEvent;
public sealed record LevelUpEvent(int PartyIndex, Creature Creature, LevelUp Info, int HpAfter) : BattleEvent;
/// <summary>The creature wants to learn a move but already knows four; the scene asks the player.</summary>
public sealed record LearnMoveEvent(int PartyIndex, Creature Creature, string MoveId) : BattleEvent;
public sealed record OrbThrowEvent(ItemData Orb, int Shakes, bool Caught) : BattleEvent;
public sealed record MoneyEvent(int Amount) : BattleEvent;
public sealed record EndEvent(BattleOutcome Outcome) : BattleEvent;
/// <summary>The defeated trainer steps back onto the field to speak.</summary>
public sealed record TrainerReturnsEvent : BattleEvent;

/// <summary>What the player chose this turn.</summary>
public abstract record PlayerAction
{
    public sealed record Fight(int MoveSlot) : PlayerAction;
    public sealed record Switch(int PartyIndex) : PlayerAction;
    public sealed record UseItem(string ItemId, int PartyIndex) : PlayerAction;
    public sealed record Run : PlayerAction;
}
