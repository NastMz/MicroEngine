using MicroEngine.Core;
using MicroEngine.Kernel;

namespace CourtyardKey.Model;

/// <summary>Everything the adventure remembers between rooms, deaths and sessions. Serialized as JSON.</summary>
public sealed class Progress : IStateHashed
{
    public const int StartingHealth = 6;
    public const int MaxGems = 999;

    public int Version { get; set; } = 1;
    /// <summary>Maximum health in half hearts.</summary>
    public int MaxHealth { get; set; } = StartingHealth;
    public int Health { get; set; } = StartingHealth;
    public int Gems { get; set; }
    public int SmallKeys { get; set; }
    public bool HasSword { get; set; }
    public bool HasBoomerang { get; set; }
    public bool HasBossKey { get; set; }
    public bool HasCourtyardKey { get; set; }
    public int Deaths { get; set; }
    public double PlaySeconds { get; set; }
    /// <summary>Persistent world facts: opened chests, unlocked doors, solved puzzles, defeated bosses.</summary>
    public HashSet<string> Flags { get; set; } = [];
    /// <summary>Visited rooms as "area:x,y" for the pause-screen map.</summary>
    public HashSet<string> Visited { get; set; } = [];
    /// <summary>Checkpoint: where Continue places the hero.</summary>
    public Checkpoint Entrance { get; set; } = Checkpoint.Start;

    public bool Has(string flag) => Flags.Contains(flag);
    public bool Set(string flag) => Flags.Add(flag);

    public void AddGems(int amount) => Gems = Math.Clamp(Gems + amount, 0, MaxGems);

    public bool TrySpend(int amount)
    {
        if (amount < 0 || Gems < amount) return false;
        Gems -= amount;
        return true;
    }

    public bool TryUseSmallKey()
    {
        if (SmallKeys <= 0) return false;
        SmallKeys--;
        return true;
    }

    public static string RoomKey(string area, int x, int y) => $"{area}:{x},{y}";
    public void MarkVisited(string area, int x, int y) => Visited.Add(RoomKey(area, x, y));
    public bool WasVisited(string area, int x, int y)
    {
        // The pause map asks for every room each frame: look the key up without building a string.
        Span<char> key = stackalloc char[area.Length + 24];
        if (Visited.TryGetAlternateLookup<ReadOnlySpan<char>>(out var lookup) && key.TryWrite($"{area}:{x},{y}", out int length))
            return lookup.Contains(key[..length]);
        return Visited.Contains(RoomKey(area, x, y));
    }

    /// <summary>
    /// State hash: every field, flags and visited rooms in ordinal order (a hash set's own order follows the
    /// process's randomized string hashes, so it is not the same in two runs).
    /// </summary>
    public void HashState(StateHasher hash)
    {
        hash.Add(Version);
        hash.Add(MaxHealth);
        hash.Add(Health);
        hash.Add(Gems);
        hash.Add(SmallKeys);
        hash.Add(HasSword);
        hash.Add(HasBoomerang);
        hash.Add(HasBossKey);
        hash.Add(HasCourtyardKey);
        hash.Add(Deaths);
        hash.Add(PlaySeconds);
        AddSorted(hash, Flags);
        AddSorted(hash, Visited);
        hash.Add(Entrance.Area);
        hash.Add(Entrance.RoomX);
        hash.Add(Entrance.RoomY);
        hash.Add(Entrance.TileX);
        hash.Add(Entrance.TileY);
        hash.Add((int)Entrance.Facing);
    }

    private static void AddSorted(StateHasher hash, HashSet<string> set)
    {
        hash.Add(set.Count);
        foreach (string item in set.Order(StringComparer.Ordinal)) hash.Add(item);
    }
}

/// <summary>An area, room and tile-space position to respawn at.</summary>
public sealed record Checkpoint(string Area, int RoomX, int RoomY, float TileX, float TileY, Dir Facing)
{
    public static readonly Checkpoint Start = new("overworld", 1, 2, 4f, 4.6f, Dir.Down);
}

/// <summary>Reads and writes the single save slot through the engine's <see cref="SaveStore"/>.</summary>
public sealed class SaveSlots(SaveStore store)
{
    public const string Slot = "adventure";
    public SaveStore Store { get; } = store;
    public bool HasSave => Store.Exists(Slot);
    public Progress? Load() => Store.Load<Progress>(Slot);
    public void Save(Progress progress) => Store.Save(Slot, progress);
    public void Delete() => Store.Delete(Slot);
}
