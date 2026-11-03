namespace MicroEngine.Kernel;

/// <summary>What a frame system knows about time when it runs (<see cref="FrameSchedule"/>).</summary>
/// <param name="Delta">
/// Seconds this frame stands for: in real time, the elapsed time measured by the loop (paced to the display and at most
/// <see cref="TimeSettings.MaxFrameDelta"/>); in deterministic time, exactly <see cref="FixedDelta"/>.
/// </param>
/// <param name="FixedDelta">Seconds of one fixed step: <c>1 / TimeSettings.FixedRate</c>.</param>
/// <param name="Blend">
/// How far the frame is between the last fixed step and the next one, 0..1: the part of a step of time that has passed
/// but has not been simulated yet. Drawing at <c>Lerp(previous, current, Blend)</c> makes motion even on displays whose
/// rate is not a multiple of the fixed rate. Always 1 in deterministic time (the frame shows exactly the last step) and
/// 0 inside the fixed phases (they run on a step boundary).
/// </param>
/// <param name="FixedStep">The number of fixed steps completed before this moment; inside a fixed phase, the index of the step being run, from 0.</param>
/// <param name="Frame">The number of the frame, from 1 for the first frame run.</param>
public readonly record struct FrameContext(double Delta, double FixedDelta, float Blend, long FixedStep, long Frame);
