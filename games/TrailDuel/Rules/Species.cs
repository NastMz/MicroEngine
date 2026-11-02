namespace TrailDuel.Rules;

public readonly record struct BaseStats(int Hp, int Attack, int Defense, int Special, int Speed);

/// <summary>Immutable species definition: stats, learnset and evolution.</summary>
public sealed record SpeciesData(
    string Id,
    string Name,
    ElementType Type,
    BaseStats Base,
    int CatchRate,
    int BaseXp,
    IReadOnlyList<(int Level, string Move)> Learnset,
    string Description,
    string? EvolvesInto = null,
    int EvolveLevel = 0)
{
    /// <summary>The moves a freshly generated creature knows: the last four learned at or below its level.</summary>
    public IReadOnlyList<string> MovesAt(int level)
    {
        var known = new List<string>();
        foreach (var (lv, move) in Learnset)
        {
            if (lv > level) break;
            known.Remove(move);
            known.Add(move);
        }
        return known.Count > 4 ? known[^4..] : known;
    }

    public IEnumerable<string> MovesLearnedAt(int level) => Learnset.Where(l => l.Level == level).Select(l => l.Move);
}

/// <summary>Every creature species in the game. All names and designs are original.</summary>
public static class SpeciesDex
{
    private static readonly Dictionary<string, SpeciesData> Species = new(StringComparer.Ordinal);
    private static readonly List<SpeciesData> Ordered = [];

    public static readonly string[] Starters = ["emberpup", "puddlepip", "sprigbun"];

    static SpeciesDex()
    {
        Add(new("emberpup", "Emberpup", ElementType.Flame, new(44, 52, 43, 50, 60), 45, 64,
            [(1, "scratch"), (1, "glare"), (5, "ember"), (9, "quickdash"), (13, "flamefang"), (16, "kindle"), (19, "blazerush")],
            "A fluffy pup with an ember-warm coat. Its fur crackles with sparks when it is happy.", "blazehound", 12));
        Add(new("blazehound", "Blazehound", ElementType.Flame, new(70, 82, 65, 72, 85), 45, 150,
            [(1, "scratch"), (1, "glare"), (5, "ember"), (9, "quickdash"), (13, "flamefang"), (16, "kindle"), (19, "blazerush"), (22, "bodyslam")],
            "Its mane burns hotter than a forge. Loyal to the end."));
        Add(new("puddlepip", "Puddlepip", ElementType.Tide, new(50, 46, 55, 50, 45), 45, 64,
            [(1, "tackle"), (1, "glare"), (5, "bubble"), (9, "splashjet"), (13, "mistveil"), (17, "tidalcrash")],
            "A shark pup that races fishing boats along the shore for fun.", "tidalotl", 12));
        Add(new("tidalotl", "Tidefang", ElementType.Tide, new(80, 70, 80, 78, 62), 45, 150,
            [(1, "tackle"), (1, "glare"), (5, "bubble"), (9, "splashjet"), (13, "mistveil"), (17, "tidalcrash"), (21, "bodyslam")],
            "It hunts the surf in packs. Its fins cut the waves like blades."));
        Add(new("sprigbun", "Sprigfawn", ElementType.Leaf, new(46, 48, 48, 55, 57), 45, 64,
            [(1, "tackle"), (1, "glare"), (5, "leafflick"), (9, "vinelash"), (12, "sporepuff"), (14, "sapsip"), (18, "petalstorm")],
            "A shy fawn. New leaves sprout along its back every spring.", "grovehare", 12));
        Add(new("grovehare", "Grovestag", ElementType.Leaf, new(72, 75, 70, 85, 80), 45, 150,
            [(1, "tackle"), (1, "glare"), (5, "leafflick"), (9, "vinelash"), (12, "sporepuff"), (14, "sapsip"), (18, "petalstorm")],
            "Wherever it bounds, meadows bloom in its wake."));
        Add(new("breezel", "Breezel", ElementType.Gale, new(40, 45, 40, 35, 65), 255, 50,
            [(1, "tackle"), (1, "gust"), (5, "quickdash"), (8, "featherfall"), (11, "wingcut"), (17, "cyclone")],
            "A chirpy songbird that rides warm updrafts.", "gustowl", 10));
        Add(new("gustowl", "Gustwing", ElementType.Gale, new(62, 60, 55, 65, 70), 90, 120,
            [(1, "tackle"), (1, "gust"), (1, "glare"), (5, "quickdash"), (8, "featherfall"), (9, "wingcut"), (11, "lullaby"), (15, "cyclone")],
            "A sharp-eyed raptor of the deep wood. Its leaf wings ride every gust."));
        Add(new("puffmop", "Puffmop", ElementType.Normal, new(95, 60, 60, 55, 40), 235, 55,
            [(1, "tackle"), (1, "glare"), (5, "lullaby"), (9, "soothe"), (12, "bodyslam")],
            "A fluffy puffball. It naps in sunny patches of grass and shrugs off almost any blow."));
        Add(new("pebblit", "Pebblit", ElementType.Stone, new(40, 58, 62, 30, 25), 190, 55,
            [(1, "tackle"), (4, "harden"), (6, "pebbletoss"), (11, "rockslam"), (18, "boulderdrop")],
            "A little rock lizard. It wandered off a mountain and kept rolling.", "bouldox", 12));
        Add(new("bouldox", "Bouldox", ElementType.Stone, new(80, 86, 88, 45, 40), 45, 160,
            [(1, "tackle"), (1, "harden"), (6, "pebbletoss"), (11, "rockslam"), (12, "bodyslam"), (14, "boulderdrop")],
            "An armoured rhino with a mossy hide. It never backs down."));
        Add(new("fizzbit", "Fizzbit", ElementType.Volt, new(35, 45, 35, 58, 80), 170, 60,
            [(1, "tackle"), (1, "statichum"), (5, "spark"), (10, "voltbolt"), (13, "quickdash"), (18, "thundercrash")],
            "A spiky hedgehog. Its quills crackle with static before a storm.", "voltusk", 12));
        Add(new("voltusk", "Voltiger", ElementType.Volt, new(65, 80, 65, 55, 62), 75, 140,
            [(1, "tackle"), (1, "statichum"), (5, "spark"), (10, "voltbolt"), (13, "quickdash"), (14, "bodyslam"), (16, "thundercrash")],
            "A storm-striped tiger. Lightning crackles from its tail when it roars."));
        Add(new("mossnail", "Mossback", ElementType.Leaf, new(70, 55, 90, 70, 25), 190, 58,
            [(1, "tackle"), (4, "toxicspore"), (6, "leafflick"), (9, "hunker"), (12, "sapsip"), (19, "petalstorm")],
            "A walking thicket. Birds nest among the berries on its back, safe behind its bark."));
    }

    private static void Add(SpeciesData data)
    {
        Species.Add(data.Id, data);
        Ordered.Add(data);
    }

    public static SpeciesData Get(string id) => Species.TryGetValue(id, out var s) ? s : throw new KeyNotFoundException($"Unknown species '{id}'.");
    public static IReadOnlyList<SpeciesData> All => Ordered;
    public static int Number(string id)
    {
        // A plain loop: the summary shows the number every frame, and a lambda over the id allocated each call.
        for (int i = 0; i < Ordered.Count; i++) if (Ordered[i].Id == id) return i + 1;
        return 0;
    }
}
