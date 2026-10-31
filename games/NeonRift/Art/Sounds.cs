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

    public static void Register(AudioMixer audio, ContentFolder content)
    {
        foreach (var name in Effects) audio.AddSoundFile(name, content.PathOf($"sfx/{name}.wav"));
        foreach (var (name, file, loop) in Tracks) audio.AddMusicFile(name, content.PathOf(file), loop);
    }
}
