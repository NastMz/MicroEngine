using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace MicroEngine.Platform;

/// <summary>
/// Save slots in the page's localStorage under "microengine/&lt;game&gt;/&lt;slot&gt;": the default save medium in
/// browsers. The browser's file system is in memory and vanishes on reload; localStorage writes are synchronous and
/// survive it. The page registers the "microengine" JavaScript module (its <c>storage.get</c>, <c>storage.set</c> and
/// <c>storage.remove</c> functions) before the game starts; the browser host's page does.
/// </summary>
/// <remarks>
/// A key is the game's prefix followed by the slot as given. localStorage keys are flat text with no folders, so a slot
/// with <c>/</c> or <c>..</c> is one more key under the game's prefix and never another game's. Every member throws
/// <see cref="ArgumentException"/> for a slot that is null or empty, which would be the prefix itself. Names are not
/// otherwise restricted here: <c>SaveStore</c> is what keeps slots to names every system can store.
/// </remarks>
[SupportedOSPlatform("browser")]
public sealed partial class BrowserStorage : IStorage
{
    private readonly string _prefix;

    /// <summary>The slots of the game <paramref name="gameId"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="gameId"/> is null, empty or white space.</exception>
    public BrowserStorage(string gameId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        _prefix = $"microengine/{gameId}/";
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
        Call(() => { SetItem(key, text); return 0; });
    }

    /// <inheritdoc/>
    public bool Exists(string slot) => Read(slot) is not null;

    /// <inheritdoc/>
    public void Delete(string slot)
    {
        var key = Key(_prefix, slot);
        Call(() => { RemoveItem(key); return 0; });
    }

    /// <inheritdoc/>
    public void SetAside(string slot)
    {
        var key = Key(_prefix, slot);
        try
        {
            if (GetItem(key) is { } text) SetItem(key + ".corrupt", text);
            RemoveItem(key);
        }
        catch (JSException) { /* Stays in place; callers that check Exists will not overwrite it. */ }
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
}
