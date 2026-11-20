using System.Text.Json.Nodes;
using MicroEngine.Core;

namespace Examples;

#region data
/// <summary>The saved data in its current shape, version 3. It holds no version itself: the schema does.</summary>
public sealed class Profile
{
    public int Gold { get; set; }
    public int Lives { get; set; } = 3;
}
#endregion

/// <summary>The slot a profile is kept in, how its shape changed between releases, and loading and saving it.</summary>
public static class ProfileSaves
{
    #region schema
    public const string Slot = "profile";

    /// <summary>
    /// This build writes version 3. One step for each earlier version: append a step for every change, and never edit
    /// one that has shipped.
    /// </summary>
    public static readonly SaveSchema<Profile> Schema = new SaveSchema<Profile>(version: 3, SaveTypes.Default.Profile)
        .Migrate(from: 1, AddLives)
        .Migrate(from: 2, RenameCoins);

    /// <summary>1 to 2: lives arrived; saves from before start with three.</summary>
    public static void AddLives(JsonObject save) => save["Lives"] = 3;

    /// <summary>2 to 3: "Coins" was renamed "Gold".</summary>
    public static void RenameCoins(JsonObject save)
    {
        save["Gold"] = save["Coins"]?.DeepClone();
        save.Remove("Coins");
    }
    #endregion

    #region use
    /// <summary>The saved profile, or a new one; <paramref name="notice"/> is what to tell the player, if anything.</summary>
    public static Profile Load(SaveStore store, out string notice)
    {
        var loaded = store.Load(Slot, Schema);
        notice = loaded.Status switch
        {
            SaveLoadStatus.Migrated => $"Your save was updated from version {loaded.Version}.",
            SaveLoadStatus.TooNew => "This save is from a newer version of the game. Progress will not be saved.",
            SaveLoadStatus.MigrationFailed or SaveLoadStatus.Unreadable => "The save could not be read. Progress will not be saved.",
            SaveLoadStatus.Corrupt => "The save was damaged and has been set aside.",
            _ => "",   // Loaded, Missing
        };
        return loaded.Value ?? new Profile();
    }

    /// <summary>False when nothing was written: the medium refused, or the slot holds a save this build must not replace.</summary>
    public static bool Save(SaveStore store, Profile profile) => store.Save(Slot, profile, Schema);
    #endregion
}
