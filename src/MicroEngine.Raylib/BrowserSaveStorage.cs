using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using MicroEngine.Core;

namespace MicroEngine.Raylib;

/// <summary>
/// Save slots in the page's localStorage under "microengine/&lt;game&gt;/&lt;slot&gt;". The browser's file system
/// is in memory and vanishes on reload; localStorage writes are synchronous and survive it. The page registers
/// the "microengine" module (see web/README.md) before the game starts.
/// </summary>
[SupportedOSPlatform("browser")]
public sealed partial class BrowserSaveStorage(string gameId) : ISaveStorage
{
    private readonly string _prefix = $"microengine/{gameId}/";

    public string Location => "localStorage:" + _prefix;

    public string? Read(string slot) => Call(() => GetItem(_prefix + slot));

    public void Write(string slot, string text) => Call(() => { SetItem(_prefix + slot, text); return 0; });

    public bool Exists(string slot) => Read(slot) is not null;

    public void Delete(string slot) => Call(() => { RemoveItem(_prefix + slot); return 0; });

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
