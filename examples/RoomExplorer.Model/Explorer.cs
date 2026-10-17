using MicroEngine.Loop;

namespace RoomExplorer.Model;

public enum Room { Courtyard, Hall }

public sealed class Explorer
{
    public const string MarkerId = "courtyard-marker";
    public const Room MarkerRoom = Room.Courtyard;
    public const float MarkerX = 300, MarkerY = 150, MarkerWidth = 40, MarkerHeight = 60;
    public bool MarkerActivated { get; private set; }
    public bool InteractionOccurred { get; private set; }
    public Room Room { get; private set; }
    public float X { get; private set; } = 40;
    public float Y { get; private set; } = 164;
    public void Update(TimeSpan delta, InputSnapshot input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (delta <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delta));
        InteractionOccurred = false;
        var dx = (input.IsHeld("right") ? 1f : 0) - (input.IsHeld("left") ? 1f : 0);
        var dy = (input.IsHeld("down") ? 1f : 0) - (input.IsHeld("up") ? 1f : 0);
        if (dx != 0 && dy != 0) { dx /= MathF.Sqrt(2); dy /= MathF.Sqrt(2); }
        var distance = 180 * (float)delta.TotalSeconds;
        var requestedX = X + dx * distance;
        var resolvedY = Math.Clamp(Y + dy * distance, 0, 328);
        if (resolvedY >= 120 && resolvedY + 32 <= 240)
        {
            if (Room == Room.Courtyard && requestedX > 616)
            {
                Room = Room.Hall; X = 8; Y = 164; return;
            }
            if (Room == Room.Hall && requestedX < 0)
            {
                Room = Room.Courtyard; X = 608; Y = 164; return;
            }
        }
        X = Math.Clamp(requestedX, 0, 616);
        Y = resolvedY;
        if (Room == MarkerRoom && X < MarkerX + MarkerWidth && X + 24 > MarkerX
            && Y < MarkerY + MarkerHeight && Y + 32 > MarkerY && input.Pressed.Contains("jump"))
        {
            MarkerActivated = !MarkerActivated;
            InteractionOccurred = true;
        }
    }
}
