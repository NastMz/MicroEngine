using System.Text;

namespace MicroEngine.Core;

/// <summary>
/// Slots kept in memory: empty when created, gone with the object. It is where a run nobody plays keeps its saves (a
/// scripted run, a run in deterministic time, a game stepped by a test), so such a run never reads or writes the
/// player's saves and two of them start from the same state. A test that plays a game twice over the same saves
/// hands both the same instance (<c>RunOptions.Saves</c>, <c>Game.InitializeForTest</c>). It takes the names the file
/// storage takes (a plain name, see <see cref="SaveStore"/>), so a slot that works here works on a disk.
/// </summary>
/// <remarks>
/// It keeps no time of writing (<see cref="Platform.SlotInfo.LastWrite"/> is null): a run that must be the same every
/// time cannot show the clock's. Not safe for use from several threads at once.
/// </remarks>
public sealed class MemorySaveStorage : ISaveStorage
{
    private readonly Dictionary<string, string> _slots = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _aside = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>"memory": there is no folder.</summary>
    public string Location => "memory";

    /// <inheritdoc/>
    public string? Read(string slot) => _slots.GetValueOrDefault(SlotName.Checked(slot));

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    public void Write(string slot, string text) => _slots[SlotName.Checked(slot)] = text ?? throw new ArgumentNullException(nameof(text));

    /// <inheritdoc/>
    public bool Exists(string slot) => _slots.ContainsKey(SlotName.Checked(slot));

    /// <inheritdoc/>
    public void Delete(string slot) => _slots.Remove(SlotName.Checked(slot));

    /// <summary>Takes the slot's text out of the slot and keeps it (<see cref="ReadSetAside"/>), replacing an earlier copy.</summary>
    public void SetAside(string slot)
    {
        if (_slots.Remove(SlotName.Checked(slot), out var text)) _aside[slot] = text;
    }

    /// <summary>The text <see cref="SetAside"/> took out of <paramref name="slot"/>, or null when nothing was set aside.</summary>
    public string? ReadSetAside(string slot) => _aside.GetValueOrDefault(SlotName.Checked(slot));

    /// <summary>The slots that have text, with its size in UTF-8 bytes (what a file would hold) and no time of writing.</summary>
    public IReadOnlyList<Platform.SlotInfo> List()
    {
        var slots = new List<Platform.SlotInfo>(_slots.Count);
        foreach (var (name, text) in _slots) slots.Add(new(name, Encoding.UTF8.GetByteCount(text), null));
        return slots;
    }
}
