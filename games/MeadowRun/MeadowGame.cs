using MeadowRun.Art;
using MeadowRun.Audio;
using MeadowRun.Content;
using MeadowRun.Gameplay;
using MeadowRun.Scenes;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Input;
using MicroEngine.Kernel;

namespace MeadowRun;

/// <summary>Persistent progress: unlocked levels, best scores and whether the Sunstone was recovered.</summary>
public sealed class SaveData : IStateHashed
{
    public int Unlocked { get; set; } = 1;
    public int[] BestScores { get; set; } = new int[Levels.All.Count];
    public bool Finished { get; set; }
    /// <summary>Coins collected in each level's best run (shown on the map).</summary>
    public int[] BestCoins { get; set; } = new int[Levels.All.Count];

    /// <summary>Hashes the progress kept in memory (what the title, map and records read).</summary>
    public void HashState(StateHasher hash)
    {
        hash.Add(Unlocked);
        hash.Add(Finished);
        hash.Add(BestScores.Length);
        foreach (int score in BestScores) hash.Add(score);
        hash.Add(BestCoins.Length);
        foreach (int coins in BestCoins) hash.Add(coins);
    }
}

/// <summary>Developer launch options (environment variables), used for captures and debugging.</summary>
public sealed record LaunchOptions(string Start = "title", int Level = 1, int Checkpoint = 0, PipForm Form = PipForm.Sprout)
{
    public static LaunchOptions FromEnvironment()
    {
        var start = Environment.GetEnvironmentVariable("MEADOW_START") ?? "title";
        int level = int.TryParse(Environment.GetEnvironmentVariable("MEADOW_LEVEL"), out var l) ? l : 1;
        int cp = int.TryParse(Environment.GetEnvironmentVariable("MEADOW_CHECKPOINT"), out var c) ? c : 0;
        var form = Enum.TryParse<PipForm>(Environment.GetEnvironmentVariable("MEADOW_FORM"), true, out var f) ? f : PipForm.Sprout;
        return new LaunchOptions(start, level, cp, form);
    }
}

/// <summary>Meadow Run: a side-scrolling platformer about Pip recovering the stolen Sunstone.</summary>
public sealed class MeadowGame(string? saveDirectory = null, LaunchOptions? launch = null) : Game
{
    private const string SaveSlot = "progress";
    private readonly LaunchOptions _launch = launch ?? new LaunchOptions();

    public override GameSettings Settings { get; } = new("Meadow Run") { Background = Pal.Ink };
    internal Assets Art { get; private set; } = null!;
    internal MicroEngine.Graphics.LightMap Lights { get; private set; } = null!;
    public SaveStore Store { get; } = new("MeadowRun", saveDirectory);
    public SaveData Save { get; private set; } = new();
    public Session Session { get; private set; } = new();
    /// <summary>Sound names requested but not registered (should stay empty; checked by tests).</summary>
    public HashSet<string> MissingSounds { get; } = [];
    public bool HasSave => Store.Exists(SaveSlot);

    protected override InputMap CreateInput() => InputMap.CreateDefault()
        .Bind("jump", [Key.Z, Key.Space, Key.J], [PadButton.A])
        .Bind("run", [Key.X, Key.C, Key.K, Key.L, Key.LeftShift], [PadButton.X, PadButton.B]);

    /// <summary>
    /// Touch: a four-way d-pad, because a platformer needs exact presses (a thumb rolling onto a diagonal must not
    /// crouch or drop through a platform); A jumps and confirms, B runs (and fires seeds) and cancels, like Z and X on a
    /// keyboard. The thumb may roll from B onto A to jump while running.
    /// </summary>
    protected override TouchLayout CreateTouchLayout() => TouchLayout.Of(
        new TouchDPad { Offset = new(104, 100), Diagonals = false },
        new TouchButton("jump", "jump", "A") { Anchor = TouchAnchor.BottomRight, Offset = new(70, 92), Radius = 40, Also = ["confirm"] },
        new TouchButton("run", "run", "B") { Anchor = TouchAnchor.BottomRight, Offset = new(164, 62), Radius = 34, Also = ["cancel"] },
        new TouchButton("pause", "pause", "II") { Anchor = TouchAnchor.TopRight, Offset = new(30, 30), Radius = 20 }) with
    {
        SlideBetweenButtons = true,
        // The game's own colours: cream outlines, presses in meadow green.
        Tint = MeadowRun.Art.Pal.Cream,
        PressedTint = MeadowRun.Art.Pal.Leaf,
    };

    protected override void Load()
    {
        Art = new Assets(Graphics);
        // Banded halo keeps light pools pixel-art instead of airbrushed.
        Lights = new MicroEngine.Graphics.LightMap(Graphics, DecoArt.Glow(24)) { Darkness = 1 };
        foreach (var (name, clip) in SoundBank.All()) Audio.AddSound(name, clip);
        foreach (var (name, song, loop) in MusicBank.All()) Audio.AddMusic(name, song, loop);
        UseAchievements(MeadowAchievements.Catalog, Store, MeadowRun.Scenes.Ui.Theme);
        AchievementToasts!.Top = 17;   // below the HUD row
        Achievements.Unlocked += _ => Sfx("checkpoint", 0.8f);
        Save = Store.Load<SaveData>(SaveSlot) ?? new SaveData();
        Normalize(Save);
        Session.Form = _launch.Form;
        Scenes.Push(_launch.Start switch
        {
            "map" => new MapScene(this, -1, Math.Clamp(_launch.Level, 1, Levels.All.Count) - 1),
            "level" => new PlayScene(this, Math.Clamp(_launch.Level, 1, Levels.All.Count) - 1, _launch.Checkpoint),
            "credits" => new CreditsScene(this),
            "sheet" => new SheetScene(this),
            "gameover" => new GameOverScene(this),
            _ => new TitleScene(this),
        });
    }

    private static void Normalize(SaveData save)
    {
        int n = Levels.All.Count;
        if (save.BestScores.Length != n) save.BestScores = [.. save.BestScores.Concat(new int[n]).Take(n)];
        if (save.BestCoins.Length != n) save.BestCoins = [.. save.BestCoins.Concat(new int[n]).Take(n)];
        save.Unlocked = Math.Clamp(save.Unlocked, 1, n);
    }

    /// <summary>
    /// Hashes the run (<see cref="Session"/>) and the progress in memory (<see cref="SaveData"/>). Leaves out the save
    /// folder and store (environment), the art, lights and sound banks (platform resources), and
    /// <see cref="MissingSounds"/> (a diagnostic).
    /// </summary>
    public override void HashState(StateHasher hash)
    {
        hash.Add(Session);
        hash.Add(Save);
    }

    public void NewGame()
    {
        var keep = Save;
        Save = new SaveData { BestScores = keep.BestScores, BestCoins = keep.BestCoins, Finished = keep.Finished };
        Session = new Session();
        Persist();
    }

    public void ContinueGame() => Session = new Session();

    /// <summary>Records a cleared level: best score, coins and the next unlock.</summary>
    public bool RecordClear(int levelIndex, int score, int coins)
    {
        bool unlockedNew = false;
        Save.BestScores[levelIndex] = Math.Max(Save.BestScores[levelIndex], score);
        Save.BestCoins[levelIndex] = Math.Max(Save.BestCoins[levelIndex], coins);
        if (levelIndex + 2 > Save.Unlocked && levelIndex + 1 < Levels.All.Count)
        {
            Save.Unlocked = levelIndex + 2;
            unlockedNew = true;
        }
        if (levelIndex == Levels.All.Count - 1) Save.Finished = true;
        Persist();
        return unlockedNew;
    }

    public void Persist()
    {
        try { Store.Save(SaveSlot, Save); }
        catch (IOException) { /* A read-only disk must not crash the game; progress stays in memory. */ }
        catch (UnauthorizedAccessException) { }
    }

    /// <summary>Plays a sound by name, tolerating names that are not registered.</summary>
    public void Sfx(string name, float volume = 1, float pitch = 1, float pan = 0.5f)
    {
        if (!Audio.TryPlay(name, volume, pitch, 0, pan)) MissingSounds.Add(name);
    }

    public void Music(string name)
    {
        if (name.Length == 0) Audio.StopMusic(0.3f);
        else Audio.PlayMusic(name, 0.5f);
    }

    protected override void Unload()
    {
        Lights.Dispose();
        Art.Dispose();
    }
}
