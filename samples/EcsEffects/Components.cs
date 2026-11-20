using System.Numerics;
using MicroEngine.Ecs;
using MicroEngine.Kernel;

namespace EcsEffects;

/// <summary>What a bolt carries.</summary>
public enum Element { Fire, Ice, Plain }

#region components
// What a thing is made of. A walker, a crate and a bolt are not classes: each is a row with some of these.
public struct Position : IStateHashed
{
    public Vector2 Value;
    public readonly void HashState(StateHasher hash) => hash.Add(Value);
}

public struct Velocity : IStateHashed
{
    public Vector2 Value;
    public readonly void HashState(StateHasher hash) => hash.Add(Value);
}

public struct Health : IStateHashed
{
    public int Value;
    public readonly void HashState(StateHasher hash) => hash.Add(Value);
}

/// <summary>A bolt in flight.</summary>
public struct Shot : IStateHashed
{
    public int Damage, Life, Fired;
    public Element Element;
    public readonly void HashState(StateHasher hash)
    {
        hash.Add(Damage);
        hash.Add(Life);
        hash.Add(Fired);
        hash.Add((int)Element);
    }
}

// Effects come and go on a live row: adding the component starts the effect, removing it ends it.
/// <summary>On fire: hurt at each tick, and at each tick the fire reaches what is near.</summary>
public struct Burning : IStateHashed
{
    public int Left, Tick;
    public readonly void HashState(StateHasher hash)
    {
        hash.Add(Left);
        hash.Add(Tick);
    }
}

/// <summary>In ice: it does not move, and the next hit shatters it for twice the damage.</summary>
public struct Frozen : IStateHashed
{
    public int Left;
    public readonly void HashState(StateHasher hash) => hash.Add(Left);
}

/// <summary>Takes the next hit whole, and is gone.</summary>
public struct Shielded;

// What a kind of thing can have: a walker carries both, a crate neither.
/// <summary>The row can be frozen.</summary>
public struct Freezes;
/// <summary>The row can take a shield.</summary>
public struct Shieldable;
/// <summary>The row is a crate: it counts as broken, not as a kill.</summary>
public struct Crate;
#endregion

public static class Rules
{
    public const float HitRadius = 6, SpreadRadius = 24, NearRadius = 48;
    public const int ShotLife = 120, BurnSteps = 180, BurnTick = 20, FrozenSteps = 150;

    #region declared
    // Every component type of the game, declared once: the data is simulation state (in the state hash), the empty
    // ones are tags (which rows have them is state), and a position is drawn between two steps.
    public static readonly EcsComponents Components = new EcsComponents()
        .AddState<Position>().AddState<Velocity>().AddState<Health>().AddState<Shot>().AddState<Burning>().AddState<Frozen>()
        .AddTag<Shielded>().AddTag<Freezes>().AddTag<Shieldable>().AddTag<Crate>()
        .Interpolate<Position>(static (in Position from, in Position to, float blend) => new Position { Value = Vector2.Lerp(from.Value, to.Value, blend) });
    #endregion
}
