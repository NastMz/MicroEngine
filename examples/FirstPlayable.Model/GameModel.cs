using MicroEngine.Loop;

namespace FirstPlayable.Model;

public sealed class GameModel
{
    public float X { get; private set; } = 40;
    public float Y { get; private set; } = 268;
    public float VerticalVelocity { get; private set; }
    public bool Grounded { get; private set; } = true;

    public void Update(TimeSpan delta, InputSnapshot input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (delta <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delta));
        var seconds = (float)delta.TotalSeconds;
        var horizontal = (input.IsHeld("right") ? 1 : 0) - (input.IsHeld("left") ? 1 : 0);
        X = Math.Clamp(X + horizontal * 180 * seconds, 0, 616);
        if (Grounded && input.Pressed.Contains("jump"))
        {
            VerticalVelocity = -300;
            Grounded = false;
        }
        if (!Grounded)
        {
            VerticalVelocity += 900 * seconds;
            Y += VerticalVelocity * seconds;
            if (Y >= 268)
            {
                Y = 268;
                VerticalVelocity = 0;
                Grounded = true;
            }
        }
    }
}
