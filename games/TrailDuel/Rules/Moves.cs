namespace TrailDuel.Rules;

public enum MoveCategory { Physical, Special, Status }

public enum Stat { Attack, Defense, Special, Speed }

public enum StatusCondition { None, Burn, Poison, Sleep }

/// <summary>What a move does besides (or instead of) dealing damage.</summary>
public enum MoveEffect { None, Burn, Poison, Sleep, RaiseSelf, LowerFoe, HealSelf, Drain }

/// <summary>Immutable move definition. <see cref="EffectChance"/> is a percentage; 0 means always for status moves.</summary>
public sealed record MoveData(
    string Id,
    string Name,
    ElementType Type,
    MoveCategory Category,
    int Power,
    int Accuracy,
    int Pp,
    string Description,
    MoveEffect Effect = MoveEffect.None,
    int EffectChance = 0,
    Stat Stat = Stat.Attack,
    int Stages = 0,
    int Priority = 0,
    bool HighCrit = false)
{
    public bool IsDamaging => Category != MoveCategory.Status && Power > 0;
    /// <summary>Accuracy 0 means the move never misses.</summary>
    public bool NeverMisses => Accuracy <= 0;
}

/// <summary>The move list. Names are original to Trail Duel.</summary>
public static class MoveDex
{
    private static readonly Dictionary<string, MoveData> Moves = new(StringComparer.Ordinal);

    static MoveDex()
    {
        // Normal
        Add(new("tackle", "Tackle", ElementType.Normal, MoveCategory.Physical, 40, 100, 35, "A full-body charge."));
        Add(new("scratch", "Scratch", ElementType.Normal, MoveCategory.Physical, 40, 100, 35, "Rakes with small claws."));
        Add(new("quickdash", "Quick Dash", ElementType.Normal, MoveCategory.Physical, 40, 100, 30, "Always strikes first.", Priority: 1));
        Add(new("bodyslam", "Body Slam", ElementType.Normal, MoveCategory.Physical, 80, 100, 15, "Crushes with its whole weight."));
        Add(new("glare", "Glare Down", ElementType.Normal, MoveCategory.Status, 0, 100, 30, "Lowers the foe's Attack.", MoveEffect.LowerFoe, Stat: Stat.Attack, Stages: 1));
        Add(new("hunker", "Hunker", ElementType.Normal, MoveCategory.Status, 0, 0, 30, "Raises own Defense.", MoveEffect.RaiseSelf, Stat: Stat.Defense, Stages: 1));
        Add(new("lullaby", "Lullaby", ElementType.Normal, MoveCategory.Status, 0, 60, 15, "A soft song that causes sleep.", MoveEffect.Sleep));
        Add(new("soothe", "Soothe", ElementType.Normal, MoveCategory.Status, 0, 0, 10, "Restores half of max HP.", MoveEffect.HealSelf));
        // Flame
        Add(new("ember", "Ember", ElementType.Flame, MoveCategory.Special, 40, 100, 25, "Tiny flames. May burn.", MoveEffect.Burn, 10));
        Add(new("flamefang", "Flame Fang", ElementType.Flame, MoveCategory.Physical, 65, 95, 15, "A blazing bite. May burn.", MoveEffect.Burn, 15));
        Add(new("blazerush", "Blaze Rush", ElementType.Flame, MoveCategory.Physical, 90, 100, 10, "A charge wreathed in fire."));
        Add(new("kindle", "Kindle", ElementType.Flame, MoveCategory.Status, 0, 0, 20, "Stokes inner fire: +Attack.", MoveEffect.RaiseSelf, Stat: Stat.Attack, Stages: 1));
        // Tide
        Add(new("bubble", "Bubble", ElementType.Tide, MoveCategory.Special, 40, 100, 30, "Sprays bubbles. May slow.", MoveEffect.LowerFoe, 15, Stat.Speed, 1));
        Add(new("splashjet", "Splash Jet", ElementType.Tide, MoveCategory.Physical, 40, 100, 20, "A darting jet. Strikes first.", Priority: 1));
        Add(new("tidalcrash", "Tidal Crash", ElementType.Tide, MoveCategory.Special, 90, 100, 10, "A crashing wall of water."));
        Add(new("mistveil", "Mist Veil", ElementType.Tide, MoveCategory.Status, 0, 0, 20, "A cool mist: +Special.", MoveEffect.RaiseSelf, Stat: Stat.Special, Stages: 1));
        // Leaf
        Add(new("leafflick", "Leaf Flick", ElementType.Leaf, MoveCategory.Special, 40, 100, 25, "Flicks sharp leaves."));
        Add(new("vinelash", "Vine Lash", ElementType.Leaf, MoveCategory.Physical, 55, 100, 20, "Whips with a vine."));
        Add(new("sapsip", "Sap Sip", ElementType.Leaf, MoveCategory.Special, 45, 100, 15, "Drains energy to heal.", MoveEffect.Drain));
        Add(new("petalstorm", "Petal Storm", ElementType.Leaf, MoveCategory.Special, 90, 100, 10, "A whirl of razor petals."));
        Add(new("sporepuff", "Spore Puff", ElementType.Leaf, MoveCategory.Status, 0, 75, 15, "Sleepy spores.", MoveEffect.Sleep));
        Add(new("toxicspore", "Toxic Spores", ElementType.Leaf, MoveCategory.Status, 0, 85, 20, "Poisons the foe.", MoveEffect.Poison));
        // Volt
        Add(new("spark", "Spark", ElementType.Volt, MoveCategory.Physical, 50, 100, 25, "An electric tackle."));
        Add(new("voltbolt", "Volt Bolt", ElementType.Volt, MoveCategory.Special, 65, 100, 15, "A crackling bolt."));
        Add(new("thundercrash", "Thunder Crash", ElementType.Volt, MoveCategory.Special, 95, 85, 10, "A huge lightning strike."));
        Add(new("statichum", "Static Hum", ElementType.Volt, MoveCategory.Status, 0, 95, 20, "Buzzing static: -Speed.", MoveEffect.LowerFoe, Stat: Stat.Speed, Stages: 2));
        // Stone
        Add(new("pebbletoss", "Pebble Toss", ElementType.Stone, MoveCategory.Physical, 40, 100, 25, "Hurls a small stone."));
        Add(new("rockslam", "Rock Slam", ElementType.Stone, MoveCategory.Physical, 70, 90, 15, "Slams with a rocky body."));
        Add(new("boulderdrop", "Boulder Drop", ElementType.Stone, MoveCategory.Physical, 95, 85, 10, "Drops a massive boulder."));
        Add(new("harden", "Harden", ElementType.Stone, MoveCategory.Status, 0, 0, 25, "Sharply raises Defense.", MoveEffect.RaiseSelf, Stat: Stat.Defense, Stages: 2));
        // Gale
        Add(new("gust", "Gust", ElementType.Gale, MoveCategory.Special, 40, 100, 35, "A gust of wind."));
        Add(new("wingcut", "Wing Cut", ElementType.Gale, MoveCategory.Physical, 60, 100, 20, "Sharp wings. High crit rate.", HighCrit: true));
        Add(new("cyclone", "Cyclone", ElementType.Gale, MoveCategory.Special, 85, 90, 10, "A roaring cyclone."));
        Add(new("featherfall", "Feather Fall", ElementType.Gale, MoveCategory.Status, 0, 100, 20, "Soft down: -foe Attack x2.", MoveEffect.LowerFoe, Stat: Stat.Attack, Stages: 2));
    }

    private static void Add(MoveData move) => Moves.Add(move.Id, move);

    public static MoveData Get(string id) => Moves.TryGetValue(id, out var m) ? m : throw new KeyNotFoundException($"Unknown move '{id}'.");
    public static IReadOnlyCollection<MoveData> All => Moves.Values;
}
