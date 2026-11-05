using System.Numerics;

namespace MicroEngine.Physics.Box2D;

/// <summary>
/// How a <see cref="Box2DPhysicsWorld2D"/> maps the game's world units onto Box2D, which works in metres with y up.
/// The defaults are Box2D's own: one world unit is a metre and y grows upward. A pixel-art game that keeps its pixel,
/// y-down space (the engine's 2D space) sets <see cref="UnitsPerMeter"/> (for example 32) and <see cref="YDown"/>; the
/// world then takes and returns pixels and every conversion happens inside it.
/// </summary>
public sealed record Box2DWorldOptions
{
    /// <summary>World units in one metre (positive). Box2D is tuned for moving objects of 0.1 to 10 metres.</summary>
    public float UnitsPerMeter { get; init; } = 1;

    /// <summary>Whether y grows downward in world units (the engine's 2D space); <see cref="IPhysicsWorld2D.Up"/> is then (0, -1).</summary>
    public bool YDown { get; init; }

    /// <summary>
    /// Gravity in world units per second squared; null for 9.81 metres per second squared downward (against
    /// <see cref="IPhysicsWorld2D.Up"/>).
    /// </summary>
    public Vector2? Gravity { get; init; }

    /// <summary>Box2D sub-steps per <see cref="IPhysicsWorld2D.Step"/> (4 by default: Box2D's recommendation).</summary>
    public int SubSteps { get; init; } = 4;

    /// <summary>Whether resting dynamic bodies may sleep (true by default); it saves work and changes no result.</summary>
    public bool EnableSleep { get; init; } = true;

    /// <summary>Throws when an option is out of range.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A scale that is not positive and finite, a gravity that is not finite, or fewer than one sub-step.</exception>
    public void Validate()
    {
        if (!(UnitsPerMeter > 0 && float.IsFinite(UnitsPerMeter)))
            throw new ArgumentOutOfRangeException(nameof(UnitsPerMeter), UnitsPerMeter, "World units per metre must be positive and finite.");
        if (Gravity is { } g && !(float.IsFinite(g.X) && float.IsFinite(g.Y)))
            throw new ArgumentOutOfRangeException(nameof(Gravity), g, "Gravity must be finite.");
        if (SubSteps < 1) throw new ArgumentOutOfRangeException(nameof(SubSteps), SubSteps, "At least one sub-step.");
    }
}
