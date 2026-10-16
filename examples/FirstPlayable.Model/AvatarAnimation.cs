namespace FirstPlayable.Model;

public enum AvatarPose { Idle, Walk, Air }

public sealed class AvatarAnimation
{
    private static readonly long FrameTicks = TimeSpan.FromMilliseconds(120).Ticks;
    private long _phase;
    public AvatarPose Pose { get; private set; }
    public bool FacingLeft { get; private set; }
    public int WalkFrame { get; private set; }
    public void Update(TimeSpan delta, float displacementX, bool grounded)
    {
        if (delta <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delta));
        if (!float.IsFinite(displacementX)) throw new ArgumentOutOfRangeException(nameof(displacementX));
        if (displacementX != 0) FacingLeft = displacementX < 0;
        var next = !grounded ? AvatarPose.Air : displacementX != 0 ? AvatarPose.Walk : AvatarPose.Idle;
        // Age only an already-running gait. Entry and nonwalking poses start at zero.
        _phase = next == AvatarPose.Walk && Pose == AvatarPose.Walk
            ? (_phase + delta.Ticks % (FrameTicks * 2)) % (FrameTicks * 2) : 0;
        Pose = next;
        WalkFrame = (int)(_phase / FrameTicks);
    }
}
