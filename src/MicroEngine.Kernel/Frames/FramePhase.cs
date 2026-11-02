namespace MicroEngine.Kernel;

/// <summary>
/// The phases of a frame, in the order they run. The four fixed phases (<see cref="PreFixed"/>,
/// <see cref="FixedUpdate"/>, <see cref="Physics"/>, <see cref="PostFixed"/>) run once per fixed step, zero or more times
/// a frame as time calls for (<see cref="TimeSettings"/>); every other phase runs once per frame.
/// </summary>
public enum FramePhase
{
    /// <summary>First, once per frame: bookkeeping before anything reads the frame (finishing background loads).</summary>
    BeginFrame,
    /// <summary>Once per frame, after the platform's events were read: input that follows the display rate (touch controls).</summary>
    Input,
    /// <summary>Once per fixed step, before the simulation: sampling the input the step reads.</summary>
    PreFixed,
    /// <summary>Once per fixed step: the simulation (scenes and game rules).</summary>
    FixedUpdate,
    /// <summary>Once per fixed step, after <see cref="FixedUpdate"/>: physics and collision.</summary>
    Physics,
    /// <summary>Once per fixed step, last: what follows the simulated state (audio, toasts, saving, resetting input edges).</summary>
    PostFixed,
    /// <summary>Once per frame, after the frame's steps, at the display's rate: camera smoothing, UI animation, interpolation.</summary>
    Update,
    /// <summary>Once per frame, after <see cref="Update"/>: what must see everything updated first.</summary>
    LateUpdate,
    /// <summary>Once per frame: copying what will be drawn out of the simulation.</summary>
    Extract,
    /// <summary>Once per frame: drawing.</summary>
    Render,
    /// <summary>Once per frame: presenting the drawn frame.</summary>
    Present,
    /// <summary>Last, once per frame.</summary>
    EndFrame,
}
