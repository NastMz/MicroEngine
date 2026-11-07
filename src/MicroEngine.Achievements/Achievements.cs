using MicroEngine.Graphics;

namespace MicroEngine.Core;

/// <summary>
/// One entry of a game's achievement catalog. <see cref="Id"/> is the stable key stored in saves, so
/// names and descriptions can change freely. <see cref="Goal"/> of 1 is an immediate achievement;
/// larger goals are progressive ("win 10 battles").
/// </summary>
public sealed record Achievement(string Id, string Name, string Description, int Goal = 1)
{
    /// <summary>Optional picture for the game's banner and list.</summary>
    public Sprite? Icon { get; init; }
}

/// <summary>A snapshot of one achievement: its definition, accumulated progress and whether it is unlocked.</summary>
public readonly record struct AchievementStatus(Achievement Definition, int Progress, bool Unlocked)
{
    /// <summary>The achievement's id.</summary>
    public string Id => Definition.Id;
    /// <summary>The progress needed to unlock it.</summary>
    public int Goal => Definition.Goal;
    /// <summary>Whether the goal is larger than 1, so the achievement shows progress.</summary>
    public bool IsProgressive => Definition.Goal > 1;
    /// <summary>0..1 for drawing progress bars.</summary>
    public float Fraction => Unlocked ? 1f : Math.Clamp(Progress / (float)Goal, 0f, 1f);
}

/// <summary>What is written to the save slot. Entries are keyed by achievement id.</summary>
public sealed class AchievementSaveData
{
    /// <summary>Format version of the saved data.</summary>
    public int Version { get; set; } = 1;
    /// <summary>Saved state per achievement id.</summary>
    public Dictionary<string, AchievementRecord>? Entries { get; set; } = [];
}

/// <summary>The saved state of one achievement.</summary>
public sealed class AchievementRecord
{
    /// <summary>Accumulated progress.</summary>
    public int Progress { get; set; }
    /// <summary>Whether the achievement is unlocked.</summary>
    public bool Unlocked { get; set; }
}

/// <summary>
/// Owns a game's achievements: the catalog, progress, unlocking and persistence. Games decide when
/// something happened and report it with <see cref="Unlock"/>, <see cref="AddProgress"/> or
/// <see cref="SetProgress"/>; the tracker guarantees that an achievement unlocks (and raises
/// <see cref="Unlocked"/>) exactly once, across sessions.
/// </summary>
/// <remarks>
/// State lives in the <see cref="SaveSlot"/> slot of the game's <see cref="SaveStore"/>, so each game
/// keeps its own file. Missing or corrupt data starts from scratch (the store keeps the corrupt file aside);
/// a file that exists but cannot be read is never overwritten during that session. Entries for ids that are no longer in
/// the catalog are kept untouched, and new catalog entries start locked. Unlocks are written at once;
/// plain progress is written by <see cref="Flush"/> (the game loop calls it about once a second and on exit).
/// </remarks>
public sealed class AchievementTracker
{
    /// <summary>The <see cref="SaveStore"/> slot that holds achievement state: "achievements".</summary>
    public const string SaveSlot = "achievements";

    private readonly List<Achievement> _catalog;
    private readonly Dictionary<string, Achievement> _byId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AchievementRecord> _records = new(StringComparer.Ordinal);
    private readonly SaveStore? _store;
    /// <summary>Saved data exists but could not be read: writing would replace it with a partial state.</summary>
    private readonly bool _storeUnreadable;

    /// <summary>Raised once when an achievement becomes unlocked (never for ones loaded already unlocked).</summary>
    public event Action<Achievement>? Unlocked;

    /// <param name="catalog">The game's achievements, in display order.</param>
    /// <param name="store">Where progress persists; null keeps it in memory only.</param>
    public AchievementTracker(IEnumerable<Achievement> catalog, SaveStore? store = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _catalog = [.. catalog];
        foreach (var a in _catalog)
        {
            ArgumentNullException.ThrowIfNull(a, nameof(catalog));
            if (string.IsNullOrWhiteSpace(a.Id)) throw new ArgumentException("Achievement ids must not be empty.", nameof(catalog));
            if (a.Goal < 1) throw new ArgumentException($"Achievement '{a.Id}' needs a goal of at least 1.", nameof(catalog));
            if (!_byId.TryAdd(a.Id, a)) throw new ArgumentException($"Duplicate achievement id '{a.Id}'.", nameof(catalog));
        }
        _store = store;
        _storeUnreadable = !Load();
    }

    /// <summary>The achievements in display order.</summary>
    public IReadOnlyList<Achievement> Catalog => _catalog;
    /// <summary>Number of achievements in the catalog.</summary>
    public int Count => _catalog.Count;
    /// <summary>Number of unlocked achievements in the catalog.</summary>
    public int UnlockedCount => _catalog.Count(a => IsUnlocked(a.Id));
    /// <summary>True when progress changed since the last successful save.</summary>
    public bool HasUnsavedChanges { get; private set; }

    /// <summary>Every achievement in catalog order.</summary>
    public IReadOnlyList<AchievementStatus> All => [.. _catalog.Select(a => Get(a.Id))];

    /// <summary>The current state of one achievement; progress is capped at its goal.</summary>
    /// <exception cref="KeyNotFoundException"><paramref name="id"/> is not in the catalog.</exception>
    public AchievementStatus Get(string id)
    {
        var a = Find(id);
        var rec = _records.GetValueOrDefault(id);
        return new AchievementStatus(a, Math.Min(rec?.Progress ?? 0, a.Goal), rec?.Unlocked ?? false);
    }

    /// <summary>Whether the achievement is unlocked.</summary>
    /// <exception cref="KeyNotFoundException"><paramref name="id"/> is not in the catalog.</exception>
    public bool IsUnlocked(string id) => Get(id).Unlocked;
    /// <summary>The achievement's progress, from 0 to its goal.</summary>
    /// <exception cref="KeyNotFoundException"><paramref name="id"/> is not in the catalog.</exception>
    public int Progress(string id) => Get(id).Progress;

    /// <summary>Unlocks an achievement now. Returns true only the first time.</summary>
    public bool Unlock(string id)
    {
        var a = Find(id);
        return Raise(a, a.Goal);
    }

    /// <summary>Adds to an achievement's progress; unlocks it when the goal is reached. Returns true only on that unlock.</summary>
    public bool AddProgress(string id, int amount = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var a = Find(id);
        if (amount == 0) return false;
        int current = _records.GetValueOrDefault(id)?.Progress ?? 0;
        return Raise(a, (int)Math.Min((long)current + amount, a.Goal));
    }

    /// <summary>
    /// Reports an absolute count ("rooms visited: 7"). Progress never goes down, so recounting after a new
    /// game cannot erase what was achieved before. Returns true only when this call unlocks it.
    /// </summary>
    public bool SetProgress(string id, int value)
    {
        var a = Find(id);
        return Raise(a, Math.Clamp(value, 0, a.Goal));
    }

    /// <summary>
    /// Writes pending progress. Failures (read-only disk, unreadable existing file) keep it in memory and
    /// return false; the next call tries again.
    /// </summary>
    public bool Flush()
    {
        if (!HasUnsavedChanges) return true;
        if (_store is null) { HasUnsavedChanges = false; return true; }
        if (_storeUnreadable) return false;
        var data = new AchievementSaveData { Entries = new Dictionary<string, AchievementRecord>(_records, StringComparer.Ordinal) };
        if (!_store.Save(SaveSlot, data)) return false;
        HasUnsavedChanges = false;
        return true;
    }

    private Achievement Find(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _byId.TryGetValue(id, out var a) ? a : throw new KeyNotFoundException($"Unknown achievement '{id}'.");
    }

    private bool Raise(Achievement a, int progress)
    {
        if (!_records.TryGetValue(a.Id, out var rec)) _records[a.Id] = rec = new AchievementRecord();
        if (rec.Unlocked || progress <= rec.Progress) return false;
        rec.Progress = progress;
        HasUnsavedChanges = true;
        if (progress < a.Goal) return false;
        rec.Unlocked = true;
        Flush();
        Unlocked?.Invoke(a);
        return true;
    }

    /// <summary>Reads saved records. Returns false when a saved file exists but could not be read.</summary>
    private bool Load()
    {
        if (_store is null) return true;
        var data = _store.Load<AchievementSaveData>(SaveSlot);
        if (data is null) return !_store.Exists(SaveSlot);
        if (data.Entries is null) return true;
        foreach (var (id, rec) in data.Entries)
        {
            if (id is null || rec is null) continue;
            var clean = new AchievementRecord { Progress = Math.Max(0, rec.Progress), Unlocked = rec.Unlocked };
            // A goal lowered in a later version unlocks quietly instead of waiting for a new report.
            if (_byId.TryGetValue(id, out var a) && clean.Progress >= a.Goal) clean.Unlocked = true;
            _records[id] = clean;
        }
        return true;
    }
}
