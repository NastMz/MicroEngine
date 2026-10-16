using FirstPlayable.Model;
using MicroEngine.Loop;

namespace FirstPlayable;

internal static class AvatarRenderer
{
    internal static void Draw(ICanvas canvas, float x, float y, AvatarAnimation animation)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(animation);
        if (!float.IsFinite(x)) throw new ArgumentOutOfRangeException(nameof(x));
        if (!float.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(y));
        var gold = new Rgba(240, 180, 65);
        void Fill(float localX, float localY, float width, float height, Rgba color)
            => canvas.Fill(new Rect(x + (animation.FacingLeft ? 24 - localX - width : localX), y + localY, width, height), color);
        Fill(4, 2, 16, 10, gold);
        Fill(6, 12, 12, 12, new Rgba(210, 140, 45));
        Fill(16, 5, 2, 3, new Rgba(24, 28, 40));
        if (animation.Pose == AvatarPose.Air)
        {
            Fill(3, 24, 7, 5, gold); Fill(14, 24, 7, 5, gold);
        }
        else if (animation.Pose == AvatarPose.Idle)
        {
            Fill(5, 24, 5, 8, gold); Fill(14, 24, 5, 8, gold);
        }
        else if (animation.WalkFrame == 0)
        {
            Fill(2, 24, 8, 8, gold); Fill(15, 24, 6, 5, gold);
        }
        else
        {
            Fill(3, 24, 6, 5, gold); Fill(14, 24, 8, 8, gold);
        }
    }
}
