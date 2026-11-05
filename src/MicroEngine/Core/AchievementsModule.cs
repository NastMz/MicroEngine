using MicroEngine.Core;
using MicroEngine.Kernel;

namespace MicroEngine.Achievements;

/// <summary>
/// Achievements as an engine module: registers <see cref="Tracker"/> as the engine's <see cref="AchievementTracker"/>
/// service and schedules <see cref="SystemName"/> in <see cref="FramePhase.PostFixed"/>, which writes pending progress
/// about once a second of simulated time (<see cref="FlushInterval"/>; unlocks are written at once by the tracker) and
/// raises <see cref="Stepped"/> every step; progress is written too when the platform suspends the game. A composition
/// that never adds the module has no tracker, no system and no achievement file. <see cref="Game"/> adds it when the game
/// declares achievements (<c>UseAchievements</c> before the engine is composed).
/// </summary>
/// <example>
/// <code>
/// var tracker = new AchievementTracker(catalog, new SaveStore("com.example.mygame"));
/// builder.Add(new AchievementsModule(tracker));
/// </code>
/// </example>
public sealed class AchievementsModule : IEngineModule
{
    /// <summary>The name of the module's frame system: "engine.achievements".</summary>
    public const string SystemName = "engine.achievements";
    /// <summary>Seconds of simulated time between writes of plain progress: 1.</summary>
    public const float FlushInterval = 1f;
    private float _sinceFlush;

    /// <summary>Creates the module over <paramref name="tracker"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="tracker"/> is null.</exception>
    public AchievementsModule(AchievementTracker tracker) => Tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));

    /// <summary>The tracker the module registers and flushes.</summary>
    public AchievementTracker Tracker { get; }

    /// <summary>Raised every fixed step with the step's length in seconds, before progress is written when due (the game's unlock banners advance on it).</summary>
    public event Action<float>? Stepped;

    /// <summary>
    /// Registers the tracker, schedules <see cref="SystemName"/> (before <c>engine.input.end</c> when the composition has
    /// it, the place it always had in a game's step) and writes progress when the game is suspended.
    /// </summary>
    public void Configure(EngineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.Add(Tracker);
        const string inputEnd = "engine.input.end";
        string? before = builder.Schedule.PhaseOf(inputEnd) == FramePhase.PostFixed ? inputEnd : null;
        builder.Schedule.Add(FramePhase.PostFixed, SystemName, step => Step((float)step.FixedDelta), before: before);
        builder.Lifecycle.Suspending += () => Tracker.Flush();
    }

    /// <summary>Advances the module by one step of <paramref name="dt"/> seconds: raises <see cref="Stepped"/>, then writes progress when <see cref="FlushInterval"/> has passed.</summary>
    public void Step(float dt)
    {
        Stepped?.Invoke(dt);
        if ((_sinceFlush += dt) < FlushInterval) return;
        _sinceFlush = 0;
        Tracker.Flush();
    }
}
