using System.Text.Json.Serialization;
using MicroEngine.Kernel;

namespace TrailDuel.Rules;

/// <summary>A known move and its remaining power points.</summary>
public sealed class MoveSlot : IStateHashed
{
    public string Id { get; set; } = "";
    public int Pp { get; set; }
    [JsonIgnore] public MoveData Data => MoveDex.Get(Id);

    public MoveSlot() { }
    public MoveSlot(string id) { Id = id; Pp = MoveDex.Get(id).Pp; }

    /// <summary>State hash: the move and its remaining power points.</summary>
    public void HashState(StateHasher hash)
    {
        hash.Add(Id);
        hash.Add(Pp);
    }
}

/// <summary>
/// One owned or wild creature. A plain mutable class so it serializes directly into saves.
/// Stats derive from species base stats and level (no hidden random values, keeping balance predictable).
/// </summary>
public sealed class Creature : IStateHashed
{
    public const int MaxLevel = 30;
    public const int MaxMoves = 4;

    public string SpeciesId { get; set; } = "";
    public int Level { get; set; } = 1;
    public int Xp { get; set; }
    public int Hp { get; set; }
    public StatusCondition Status { get; set; }
    public int SleepTurns { get; set; }
    public List<MoveSlot> Moves { get; set; } = [];

    [JsonIgnore] public SpeciesData Species => SpeciesDex.Get(SpeciesId);
    [JsonIgnore] public string Name => Species.Name;
    [JsonIgnore] public ElementType Type => Species.Type;
    [JsonIgnore] public bool Fainted => Hp <= 0;
    [JsonIgnore] public int MaxHp => 2 * Species.Base.Hp * Level / 100 + Level + 10;
    [JsonIgnore] public int Attack => StatFormula(Species.Base.Attack);
    [JsonIgnore] public int Defense => StatFormula(Species.Base.Defense);
    [JsonIgnore] public int Special => StatFormula(Species.Base.Special);
    [JsonIgnore] public int Speed => StatFormula(Species.Base.Speed);

    private int StatFormula(int baseStat) => 2 * baseStat * Level / 100 + 5;

    public int Stat(Stat stat) => stat switch
    {
        Rules.Stat.Attack => Attack,
        Rules.Stat.Defense => Defense,
        Rules.Stat.Special => Special,
        _ => Speed,
    };

    public static Creature Create(string speciesId, int level)
    {
        var species = SpeciesDex.Get(speciesId);
        level = Math.Clamp(level, 1, MaxLevel);
        var c = new Creature { SpeciesId = species.Id, Level = level, Xp = XpForLevel(level) };
        c.Hp = c.MaxHp;
        c.Moves = species.MovesAt(level).Select(m => new MoveSlot(m)).ToList();
        return c;
    }

    /// <summary>Total experience needed to reach <paramref name="level"/> (a gentle cubic curve).</summary>
    public static int XpForLevel(int level) => level <= 1 ? 0 : level * level * level * 4 / 5;

    public float HpFraction => MaxHp == 0 ? 0 : Math.Clamp(Hp / (float)MaxHp, 0, 1);

    /// <summary>Progress 0..1 from the current level threshold to the next.</summary>
    public float XpFraction
    {
        get
        {
            if (Level >= MaxLevel) return 1;
            int from = XpForLevel(Level), to = XpForLevel(Level + 1);
            return Math.Clamp((Xp - from) / (float)(to - from), 0, 1);
        }
    }

    public bool Knows(string moveId) => Moves.Any(m => m.Id == moveId);

    /// <summary>Restores HP, PP and status (shrine or home rest).</summary>
    public void FullHeal()
    {
        Hp = MaxHp;
        Status = StatusCondition.None;
        SleepTurns = 0;
        foreach (var m in Moves) m.Pp = m.Data.Pp;
    }

    /// <summary>Heals up to <paramref name="amount"/> HP; returns the amount actually restored.</summary>
    public int Heal(int amount)
    {
        if (Fainted) return 0;
        int before = Hp;
        Hp = Math.Min(MaxHp, Hp + amount);
        return Hp - before;
    }

    /// <summary>Changes species in place, keeping the same HP damage taken.</summary>
    public void EvolveInto(string speciesId)
    {
        int damage = MaxHp - Hp;
        SpeciesId = speciesId;
        Hp = Math.Max(1, MaxHp - damage);
    }

    public bool CanEvolve => Species.EvolvesInto is not null && Level >= Species.EvolveLevel;

    /// <summary>State hash: species, level, experience, HP, status and its moves (stats derive from these).</summary>
    public void HashState(StateHasher hash)
    {
        hash.Add(SpeciesId);
        hash.Add(Level);
        hash.Add(Xp);
        hash.Add(Hp);
        hash.Add((int)Status);
        hash.Add(SleepTurns);
        hash.Add(Moves.Count);
        foreach (var m in Moves) hash.Add(m);
    }

    public Creature Clone() => new()
    {
        SpeciesId = SpeciesId, Level = Level, Xp = Xp, Hp = Hp, Status = Status, SleepTurns = SleepTurns,
        Moves = Moves.Select(m => new MoveSlot { Id = m.Id, Pp = m.Pp }).ToList(),
    };
}
