using MicroEngine.Loop;
using MicroEngine.TwoD;
namespace Platformer.Model;

/// <summary>One game's rules and immutable content, using the optional engine collision module.</summary>
public sealed class LevelGame
{
    public static IReadOnlyList<Rect> Solids { get; } = Array.AsReadOnly<Rect>([
        new(0,432,1920,48), new(336,384,96,48), new(432,360,96,72), new(768,336,144,24), new(1104,384,144,48)]);
    public static IReadOnlyList<Rect> Coins { get; } = Array.AsReadOnly<Rect>([new(192,400,24,24), new(672,400,24,24), new(1344,400,24,24)]);
    public static Rect Hazard => new(1440,412,48,20);
    public static Rect Flag => new(1824,336,48,96);
    private readonly StaticWorld _world = new(Solids);
    private readonly bool[] _collected = new bool[3];
    private long _pending = -1;
    private float _velocity;
    public Rect Body { get; private set; } = new(48,392,28,40);
    public bool Grounded { get; private set; } = true;
    public bool Completed { get; private set; }
    public bool Dead { get; private set; }
    public bool Walking { get; private set; }
    public int RunNumber { get; private set; }
    public float CameraX => Math.Clamp(Body.X + 14 - 384, 0, 1152);
    public int CollectedCount => _collected.Count(value => value);
    public bool IsCollected(int index) => _collected[index];
    public void ClearJumpBuffer() => _pending = -1;
    public void Update(TimeSpan delta, InputSnapshot input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (delta <= TimeSpan.Zero || delta > TimeSpan.FromMilliseconds(100)) throw new ArgumentOutOfRangeException(nameof(delta));
        bool jump = input.Pressed.Contains("jump");
        if (Completed || Dead)
        {
            if (jump) Reset();
            return;
        }
        if (_pending >= 0) _pending = delta.Ticks > _pending ? -1 : _pending - delta.Ticks;
        if (jump) _pending = TimeSpan.FromMilliseconds(100).Ticks;
        if (Grounded && _pending >= 0) Launch();
        float seconds = (float)delta.TotalSeconds;
        int horizontal = (input.IsHeld("right") ? 1 : 0) - (input.IsHeld("left") ? 1 : 0);
        float dx = Math.Clamp(Body.X + horizontal * 210 * seconds, 0, 1892) - Body.X;
        _velocity += 1200 * seconds;
        var motion = _world.Move(Body, dx, _velocity * seconds);
        Walking = motion.Bounds.X != Body.X;
        Body = motion.Bounds;
        Grounded = motion.BlockedY && _velocity > 0;
        if (motion.BlockedY) _velocity = 0;
        if (Grounded && _pending >= 0) Launch();
        if (Body.Y > 528 || StaticWorld.Overlaps(Body, Hazard)) { Dead = true; Freeze(); return; }
        for (int i = 0; i < _collected.Length; i++) if (!_collected[i] && StaticWorld.Overlaps(Body, Coins[i])) _collected[i] = true;
        if (CollectedCount == 3 && StaticWorld.Overlaps(Body, Flag)) { Completed = true; Freeze(); }
    }
    private void Launch() { _velocity = -540; Grounded = false; _pending = -1; }
    private void Freeze() { _velocity = 0; _pending = -1; Walking = false; }
    private void Reset()
    {
        Body = new(48,392,28,40); Grounded = true; Completed = false; Dead = false; Walking = false;
        _velocity = 0; _pending = -1; Array.Clear(_collected); RunNumber++;
    }
}
