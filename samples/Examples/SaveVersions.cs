using System.Text.Json;
using System.Text.Json.Nodes;
using MicroEngine.Core;

namespace Examples;

#region data
/// <summary>The saved data in its current shape. <see cref="Version"/> records the shape a slot was written in.</summary>
public sealed class Profile
{
    public int Version { get; set; }
    public int Gold { get; set; }
    public int Lives { get; set; } = 3;
}
#endregion

#region check
/// <summary>What <see cref="ProfileSaves.Load"/> found.</summary>
public enum SaveCheck
{
    /// <summary>No usable save: start a new game (missing, or corrupt and set aside by the store).</summary>
    Missing,
    /// <summary>A save in the current shape.</summary>
    Current,
    /// <summary>An older save, upgraded in memory; the original is kept in a backup slot.</summary>
    Upgraded,
    /// <summary>A save this build cannot upgrade safely (written by a newer build, or the backup failed): never save over it.</summary>
    Protected,
}
#endregion

#region upgrade
/// <summary>
/// One slot whose shape changes between releases. Old saves are read as JSON, copied to a backup slot, upgraded one
/// version at a time and only then turned into a <see cref="Profile"/>; saves from a newer build are left alone.
/// </summary>
public static class ProfileSaves
{
    public const string Slot = "profile";

    // Each entry turns version i into version i + 1. Append a step for every change; never edit a published one.
    private static readonly Action<JsonObject>[] Upgrades =
    [
        // 0 -> 1: lives arrived; saves from before start with three.
        json => json["Lives"] = 3,
        // 1 -> 2: "Coins" was renamed "Gold".
        json => { json["Gold"] = json["Coins"]?.DeepClone(); json.Remove("Coins"); },
    ];

    /// <summary>The shape this build writes: one more than the last upgrade step.</summary>
    public static int CurrentVersion => Upgrades.Length;

    public static Profile? Load(SaveStore store, out SaveCheck check)
    {
        check = SaveCheck.Missing;
        if (store.Load<JsonObject>(Slot) is not { } json) return null;   // missing, corrupt or unreadable

        int version = 0;                                                  // saves from before versions existed
        if (json["Version"] is { } field && !(field is JsonValue value && value.TryGetValue(out version)))
        {
            check = SaveCheck.Protected;                                  // a version this build cannot read
            return null;
        }
        if (version > CurrentVersion)
        {
            check = SaveCheck.Protected;                                  // written by a newer build
            return null;
        }
        if (version < CurrentVersion)
        {
            // Keep the original before changing anything; without a copy, do not upgrade.
            if (!store.Save($"{Slot}.v{version}", json))
            {
                check = SaveCheck.Protected;
                return null;
            }
            for (int v = version; v < CurrentVersion; v++) Upgrades[v](json);
            json["Version"] = CurrentVersion;
        }
        try
        {
            var profile = json.Deserialize<Profile>();
            check = profile is null ? SaveCheck.Protected : version < CurrentVersion ? SaveCheck.Upgraded : SaveCheck.Current;
            return profile;
        }
        catch (JsonException)
        {
            check = SaveCheck.Protected;                                  // a field holds a value of the wrong type
            return null;
        }
    }

    /// <summary>Writes the profile in the current shape; false when the medium refused.</summary>
    public static bool Save(SaveStore store, Profile profile)
    {
        profile.Version = CurrentVersion;
        return store.Save(Slot, profile);
    }
}
#endregion
