using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace MicroEngine.Platform;

/// <summary>
/// Save slots in the page's localStorage under "microengine/&lt;game&gt;/&lt;slot&gt;": the default save medium in
/// browsers. The browser's file system is in memory and vanishes on reload; localStorage writes are synchronous and
/// survive it. The page registers the "microengine" JavaScript module (its <c>storage.get</c>, <c>storage.set</c> and
/// <c>storage.remove</c> functions, and <c>storage.keys</c> to list the slots) before the game starts; the browser
/// host's page does.
/// </summary>
/// <remarks>
/// A key is the game's prefix followed by the slot as given. localStorage keys are flat text with no folders, so a slot
/// with <c>/</c> or <c>..</c> is one more key under the game's prefix and never another game's. Every member throws
/// <see cref="ArgumentException"/> for a slot that is null or empty, which would be the prefix itself. Names are not
/// otherwise restricted here: <c>SaveStore</c> is what keeps slots to names every system can store.
/// <para>
/// localStorage keeps no times, so each write also stamps when it happened under
/// "microengine.time/&lt;game&gt;/&lt;slot&gt;" (milliseconds since 1970), outside every game's slot keys;
/// <see cref="List"/> reads it back. The stamp is best effort: a slot without one lists with no time.
/// </para>
/// </remarks>
[SupportedOSPlatform("browser")]
public sealed partial class BrowserStorage : IStorage
{
    private readonly string _prefix;
    private readonly string _timePrefix;

    /// <summary>The slots of the game <paramref name="gameId"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="gameId"/> is null, empty or white space.</exception>
    public BrowserStorage(string gameId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        _prefix = $"microengine/{gameId}/";
        _timePrefix = TimePrefix(gameId);
    }

    /// <summary>"localStorage:" followed by the key prefix of this game's slots.</summary>
    public string Location => "localStorage:" + _prefix;

    /// <inheritdoc/>
    public string? Read(string slot)
    {
        var key = Key(_prefix, slot);
        return Call(() => GetItem(key));
    }

    /// <inheritdoc/>
    public void Write(string slot, string text)
    {
        var key = Key(_prefix, slot);
        // The stamp first: if only one of the two is written, it is the one that loses nothing.
        try { SetItem(_timePrefix + slot, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)); }
        catch (JSException) { /* The slot lists without a time. */ }
        Call(() => { SetItem(key, text); return 0; });
    }

    /// <inheritdoc/>
    public bool Exists(string slot) => Read(slot) is not null;

    /// <inheritdoc/>
    public void Delete(string slot)
    {
        var key = Key(_prefix, slot);
        Call(() => { RemoveItem(key); RemoveItem(_timePrefix + slot); return 0; });
    }

    /// <inheritdoc/>
    public void SetAside(string slot)
    {
        var key = Key(_prefix, slot);
        try
        {
            if (GetItem(key) is { } text) SetItem(key + SetAsideSuffix, text);
            RemoveItem(key);
            RemoveItem(_timePrefix + slot);
        }
        catch (JSException) { /* Stays in place; callers that check Exists will not overwrite it. */ }
    }

    /// <summary>
    /// The game's slots: every key under its prefix but the copies set aside, with the length of its text as the size and
    /// the time of its last write when one was stamped.
    /// </summary>
    public IReadOnlyList<SlotInfo> List() => Call(() => Slots(_prefix, _timePrefix, Keys(_prefix), GetItem));

    private const string SetAsideSuffix = ".corrupt";

    /// <summary>Where the write times of a game's slots are stamped: a root of its own, so no slot of any game can be one.</summary>
    internal static string TimePrefix(string gameId) => $"microengine.time/{gameId}/";

    /// <summary>The slots among <paramref name="keys"/>: the listing, apart from the page that holds the keys.</summary>
    internal static IReadOnlyList<SlotInfo> Slots(string prefix, string timePrefix, IEnumerable<string> keys, Func<string, string?> read)
    {
        var slots = new List<SlotInfo>();
        foreach (var key in keys)
        {
            if (key.Length <= prefix.Length || !key.StartsWith(prefix, StringComparison.Ordinal)) continue;
            string slot = key[prefix.Length..];
            if (slot.EndsWith(SetAsideSuffix, StringComparison.Ordinal) || read(key) is not { } text) continue;
            DateTimeOffset? written = null;
            if (read(timePrefix + slot) is { } stamp && long.TryParse(stamp, NumberStyles.None, CultureInfo.InvariantCulture, out long milliseconds)
                && milliseconds <= DateTimeOffset.MaxValue.ToUnixTimeMilliseconds())
                written = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
            slots.Add(new(slot, text.Length, written));
        }
        return slots;
    }

    /// <summary>The localStorage key of a slot: the game's prefix and the slot, as given.</summary>
    /// <exception cref="ArgumentException"><paramref name="slot"/> is null or empty.</exception>
    internal static string Key(string prefix, string slot)
    {
        ArgumentException.ThrowIfNullOrEmpty(slot);
        return prefix + slot;
    }

    /// <summary>Storage failures (quota, privacy mode) surface as IOException, like a full disk.</summary>
    private static T Call<T>(Func<T> action)
    {
        try { return action(); }
        catch (JSException e) { throw new IOException(e.Message, e); }
    }

    [JSImport("storage.get", "microengine")]
    private static partial string? GetItem(string key);

    [JSImport("storage.set", "microengine")]
    private static partial void SetItem(string key, string value);

    [JSImport("storage.remove", "microengine")]
    private static partial void RemoveItem(string key);

    [JSImport("storage.keys", "microengine")]
    [return: JSMarshalAs<JSType.Array<JSType.String>>]
    private static partial string[] Keys(string prefix);
}
