using MicroEngine.Audio;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace MicroEngine.Core;

/// <summary>
/// Base class for a game. Subclasses declare <see cref="Settings"/>, build content in
/// <see cref="Load"/> and push the first scene. The runner owns the loop.
/// </summary>
public abstract class Game
{
    private IPlatform? _platform;
    private float _achievementFlush;

    public abstract GameSettings Settings { get; }
    public IGraphicsDevice Graphics => Platform.Graphics;
    public AudioMixer Audio { get; private set; } = null!;
    public InputMap Input { get; private set; } = null!;
    public SceneManager Scenes { get; private set; } = null!;
    public BitmapFont Font { get; private set; } = null!;
    /// <summary>Seconds of simulated time since start.</summary>
    public float Time { get; private set; }
    public long UpdateCount { get; private set; }
    /// <summary>Deterministic per-run randomness; seeded by the launcher.</summary>
    public Random Random { get; private set; } = new(1);
    public bool QuitRequested { get; private set; }
    public int Width => Settings.VirtualWidth;
    public int Height => Settings.VirtualHeight;
    private AchievementTracker? _achievements;
    /// <summary>The game's achievements; available after <see cref="UseAchievements"/>.</summary>
    public AchievementTracker Achievements => _achievements ?? throw new InvalidOperationException("Achievements are off: call UseAchievements in Load.");
    public bool HasAchievements => _achievements is not null;
    /// <summary>Unlock banners drawn above every scene (null without achievements).</summary>
    public AchievementToasts? AchievementToasts { get; private set; }
    protected IPlatform Platform => _platform ?? throw new InvalidOperationException("Game is not running.");

    /// <summary>Called once after the platform exists. Create atlases, sounds and push the first scene.</summary>
    protected abstract void Load();
    /// <summary>Called once before the platform is disposed.</summary>
    protected virtual void Unload() { }
    /// <summary>Override to change bindings; the default suits most games.</summary>
    protected virtual InputMap CreateInput() => InputMap.CreateDefault();

    public void Quit() => QuitRequested = true;

    /// <summary>
    /// Turns on achievements, usually from <see cref="Load"/>. Progress persists in the "achievements" slot
    /// of <paramref name="store"/> (the game's own save folder) and every unlock shows a banner styled with
    /// <paramref name="theme"/>. Report progress through <see cref="Achievements"/>.
    /// </summary>
    protected AchievementTracker UseAchievements(IEnumerable<Achievement> catalog, SaveStore store, UiTheme? theme = null)
    {
        _achievements = new AchievementTracker(catalog, store);
        AchievementToasts = new AchievementToasts { Theme = theme ?? UiTheme.Default };
        _achievements.Unlocked += AchievementToasts.Show;
        return _achievements;
    }

    internal void Initialize(IPlatform platform, int seed)
    {
        _platform = platform;
        Random = new Random(seed);
        Input = CreateInput();
        Audio = new AudioMixer(platform.Audio);
        Font = BitmapFont.CreateDefault(platform.Graphics);
        Scenes = new SceneManager(this);
        Load();
        Scenes.ApplyPending();
    }

    internal void Step(float dt)
    {
        Time += dt;
        UpdateCount++;
        Scenes.Update(dt);
        Audio.Update(dt);
        AchievementToasts?.Update(dt);
        if (_achievements is { } achievements && (_achievementFlush += dt) >= 1f)
        {
            _achievementFlush = 0;
            achievements.Flush();
        }
        Input.EndUpdate();
    }

    internal void Render(IRenderer renderer)
    {
        renderer.Clear(Settings.Background);
        Scenes.Draw(renderer);
        AchievementToasts?.Draw(renderer, Font);
    }

    internal void Shutdown()
    {
        Scenes.Clear();
        _achievements?.Flush();
        Unload();
        Font.Dispose();
    }

    /// <summary>Test hook: initializes against a platform without running the loop.</summary>
    public void InitializeForTest(IPlatform platform, int seed = 1) => Initialize(platform, seed);
    /// <summary>Test hook: advances one fixed step after sampling input.</summary>
    public void StepForTest()
    {
        Input.Sample(Platform.Input);
        Step(1f / Settings.UpdateRate);
    }
}
