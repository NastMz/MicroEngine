namespace TrailDuel.Rules;

public enum ItemKind { Heal, Cure, Orb }

/// <summary>An item definition. <see cref="Power"/> is HP restored for heals or the catch multiplier x10 for orbs.</summary>
public sealed record ItemData(string Id, string Name, ItemKind Kind, int Power, int Price, string Description)
{
    public float OrbMultiplier => Power / 10f;
}

public static class ItemDex
{
    public static readonly ItemData Potion = new("potion", "Potion", ItemKind.Heal, 20, 200, "Restores 20 HP.");
    public static readonly ItemData SuperPotion = new("superpotion", "Super Potion", ItemKind.Heal, 50, 500, "Restores 50 HP.");
    public static readonly ItemData Remedy = new("remedy", "Remedy", ItemKind.Cure, 0, 150, "Cures burn, poison and sleep.");
    public static readonly ItemData Orb = new("orb", "Capture Orb", ItemKind.Orb, 10, 150, "Throw at a wild creature to catch it.");
    public static readonly ItemData SuperOrb = new("superorb", "Super Orb", ItemKind.Orb, 15, 400, "A better orb. Catch rate x1.5.");

    public static readonly IReadOnlyList<ItemData> All = [Potion, SuperPotion, Remedy, Orb, SuperOrb];

    public static ItemData Get(string id) => All.FirstOrDefault(i => i.Id == id) ?? throw new KeyNotFoundException($"Unknown item '{id}'.");

    /// <summary>Whether using <paramref name="item"/> on <paramref name="target"/> would do anything.</summary>
    public static bool CanUseOn(ItemData item, Creature target) => item.Kind switch
    {
        ItemKind.Heal => !target.Fainted && target.Hp < target.MaxHp,
        ItemKind.Cure => !target.Fainted && target.Status != StatusCondition.None,
        _ => false,
    };

    /// <summary>Applies a heal or cure item and returns the message to show.</summary>
    public static string Apply(ItemData item, Creature target)
    {
        switch (item.Kind)
        {
            case ItemKind.Heal:
                int healed = target.Heal(item.Power);
                return $"{target.Name} recovered {healed} HP!";
            case ItemKind.Cure:
                target.Status = StatusCondition.None;
                target.SleepTurns = 0;
                return $"{target.Name} feels refreshed!";
            default:
                throw new InvalidOperationException($"{item.Name} cannot be used on a creature.");
        }
    }
}

/// <summary>Item counts keyed by item id; serializes as a plain dictionary.</summary>
public sealed class Bag
{
    public Dictionary<string, int> Items { get; set; } = new(StringComparer.Ordinal);

    public int Count(string id) => Items.TryGetValue(id, out var n) ? n : 0;

    public void Add(string id, int amount = 1)
    {
        ItemDex.Get(id);
        Items[id] = Count(id) + amount;
    }

    public bool Remove(string id, int amount = 1)
    {
        if (Count(id) < amount) return false;
        Items[id] = Count(id) - amount;
        if (Items[id] == 0) Items.Remove(id);
        return true;
    }

    /// <summary>Owned items in catalogue order.</summary>
    public IEnumerable<(ItemData Item, int Count)> Owned()
        => ItemDex.All.Where(i => Count(i.Id) > 0).Select(i => (i, Count(i.Id)));
}
