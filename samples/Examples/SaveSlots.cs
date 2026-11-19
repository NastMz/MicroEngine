using MicroEngine.Core;
using MicroEngine.Platform;

namespace Examples;

/// <summary>
/// What a "load game" screen and an options screen need from the game's store: the save games it holds, and the
/// settings, which are one more slot of the same store.
/// </summary>
public static class GameSaves
{
    #region slots
    /// <summary>The save games, newest first: every slot whose name starts with "save-".</summary>
    public static IReadOnlyList<SlotInfo> SaveGames(SaveStore store) =>
        [.. store.Slots("save-").OrderByDescending(slot => slot.LastWrite)];

    /// <summary>A line of the list: the slot and when it was last saved, when the medium knows.</summary>
    public static string Describe(SlotInfo slot) =>
        slot.LastWrite is { } time ? $"{slot.Name}   {time.LocalDateTime:g}" : slot.Name;
    #endregion

    #region settings
    /// <summary>What the player chose on the options screen.</summary>
    public sealed class Settings
    {
        public float MusicVolume { get; set; } = 1;
        public float SoundVolume { get; set; } = 1;
        public bool Fullscreen { get; set; }
    }

    public static readonly SaveSchema<Settings> SettingsSchema = new(version: 1, SaveTypes.Default.Settings);

    /// <summary>The saved settings, or the defaults when there are none this build can use.</summary>
    public static Settings LoadSettings(SaveStore store) =>
        store.Load("settings", SettingsSchema).Value ?? new Settings();

    /// <summary>Called when the player changes a setting, not when the game saves its progress.</summary>
    public static bool SaveSettings(SaveStore store, Settings settings) =>
        store.Save("settings", settings, SettingsSchema);
    #endregion
}
