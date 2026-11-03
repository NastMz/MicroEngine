using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace MicroEngine.Platform;

/// <summary>
/// Save slots in the page's localStorage under "microengine/&lt;game&gt;/&lt;slot&gt;": the default save medium in
/// browsers. The browser's file system is in memory and vanishes on reload; localStorage writes are synchronous and
/// survive it. The page registers the "microengine" JavaScript module (its <c>storage.get</c>, <c>storage.set</c> and
/// <c>storage.remove</c> functions) before the game starts; the browser host's page does.
/// </summary>
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
    public string? Read(string slot) => Call(() => GetItem(_prefix + slot));

    /// <inheritdoc/>
    public void Write(string slot, string text) => Call(() => { SetItem(_prefix + slot, text); return 0; });

    /// <inheritdoc/>
    public bool Exists(string slot) => Read(slot) is not null;

    /// <inheritdoc/>
    public void Delete(string slot) => Call(() => { RemoveItem(_prefix + slot); return 0; });

    /// <inheritdoc/>
    public void SetAside(string slot)
    {
        try
        {
            if (GetItem(_prefix + slot) is { } text) SetItem(_prefix + slot + ".corrupt", text);
            RemoveItem(_prefix + slot);
        }
        catch (JSException) { /* Stays in place; callers that check Exists will not overwrite it. */ }
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
