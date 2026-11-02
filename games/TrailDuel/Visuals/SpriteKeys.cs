namespace TrailDuel.Visuals;

/// <summary>
/// Atlas keys built once instead of formatted every frame: a prefix followed by a number (<c>"t:path5"</c>) or by an
/// id (<c>"front:" + species</c>). Drawing looks them up, so it produces no garbage per frame.
/// </summary>
internal static class SpriteKeys
{
    /// <summary>Front sprites of creatures ("front:" + species), their ground shadows ("cshadow:") and item icons ("icon:" + item).</summary>
    public static readonly PrefixedKeys Front = new("front:"), Shadow = new("cshadow:"), Icon = new("icon:");

    /// <summary><paramref name="prefix"/> followed by 0 to <paramref name="count"/> - 1.</summary>
    public static string[] Numbered(string prefix, int count)
    {
        var keys = new string[count];
        for (int i = 0; i < count; i++) keys[i] = prefix + i;
        return keys;
    }

    /// <summary>The key for <paramref name="index"/> from <paramref name="keys"/>, built on the spot when out of range.</summary>
    public static string Pick(string[] keys, string prefix, int index) => (uint)index < (uint)keys.Length ? keys[index] : prefix + index;
}

/// <summary>Keys of one prefix followed by an id, built on first use and kept.</summary>
internal sealed class PrefixedKeys(string prefix)
{
    private readonly Dictionary<string, string> _keys = [];

    public string this[string id]
    {
        get
        {
            if (!_keys.TryGetValue(id, out var key)) _keys[id] = key = prefix + id;
            return key;
        }
    }
}
