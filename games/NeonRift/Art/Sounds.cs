using MicroEngine.Audio;
using MicroEngine.Core;

namespace NeonRift.Art;

/// <summary>Registers every sound effect and music track shipped in <c>Content/</c> (see CREDITS.md).</summary>
internal static class Sounds
{
    public static readonly string[] Effects =
    [
        "shot", "spread", "laser", "rapid", "enemy-shot", "cannon", "hit", "clank", "explode", "explode-medium",
        "explode-big", "player-death", "robot-death", "jump", "land", "powerup", "one-up", "fanfare", "victory",
        "menu-move", "menu-select", "pause-in", "pause-out", "alarm",
    ];

    /// <summary>Music name to file; jingles do not loop.</summary>
    public static readonly (string Name, string File, bool Loop)[] Tracks =
    [
        ("title", "music/title.wav", true),
        ("city", "music/city.ogg", true),
        ("grotto", "music/grotto.ogg", true),
        ("lab", "music/lab.ogg", true),
        ("boss", "music/boss.ogg", true),
        ("ending", "music/ending.wav", true),
    ];

    /// <summary>Loads the files through the game's assets (held for the game's lifetime) and registers them by name.</summary>
    public static void Register(AudioMixer audio, AssetCache assets)
    {
        foreach (var name in Effects) audio.AddSound(name, assets.Sound($"sfx/{name}.wav"));
        foreach (var (name, file, loop) in Tracks) audio.AddMusic(name, assets.Music(file), loop);
    }
}
