using MicroEngine.Audio;
using MicroEngine.Core;
using Music = NeonRift.Art.GameContent.Music;
using Sfx = NeonRift.Art.GameContent.Sfx;

namespace NeonRift.Art;

/// <summary>Registers every sound effect and music track shipped in <c>Content/</c> (see CREDITS.md).</summary>
internal static class Sounds
{
    /// <summary>Sound name to file: the name is the file's, without its folder and extension.</summary>
    public static readonly (string Name, string File)[] Effects =
    [
        .. new[]
        {
            Sfx.Shot, Sfx.Spread, Sfx.Laser, Sfx.Rapid, Sfx.EnemyShot, Sfx.Cannon, Sfx.Hit, Sfx.Clank, Sfx.Explode, Sfx.ExplodeMedium,
            Sfx.ExplodeBig, Sfx.PlayerDeath, Sfx.RobotDeath, Sfx.Jump, Sfx.Land, Sfx.Powerup, Sfx.OneUp, Sfx.Fanfare, Sfx.Victory,
            Sfx.MenuMove, Sfx.MenuSelect, Sfx.PauseIn, Sfx.PauseOut, Sfx.Alarm,
        }.Select(static file => (Path.GetFileNameWithoutExtension(file), file)),
    ];

    /// <summary>Music name to file; every track loops.</summary>
    public static readonly (string Name, string File, bool Loop)[] Tracks =
    [
        ("title", Music.Title, true),
        ("city", Music.City, true),
        ("grotto", Music.Grotto, true),
        ("lab", Music.Lab, true),
        ("boss", Music.Boss, true),
        ("ending", Music.Ending, true),
    ];

    /// <summary>Loads the files through the game's assets (held for the game's lifetime) and registers them by name.</summary>
    public static void Register(AudioMixer audio, AssetCache assets)
    {
        foreach (var (name, file) in Effects) audio.AddSound(name, assets.Sound(file));
        foreach (var (name, file, loop) in Tracks) audio.AddMusic(name, assets.Music(file), loop);
    }
}
