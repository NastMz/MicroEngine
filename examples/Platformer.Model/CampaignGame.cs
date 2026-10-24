using MicroEngine.Loop;
namespace Platformer.Model;

/// <summary>Meadow Run progression only; no engine scene or persistent save semantics.</summary>
public sealed class CampaignGame
{
    public LevelGame Current { get; private set; } = new();
    public int LevelNumber => Current.LevelNumber;
    public bool Completed => LevelNumber == 3 && Current.Completed;
    public void ClearJumpBuffer() => Current.ClearJumpBuffer();
    public void Update(TimeSpan delta, InputSnapshot input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (delta <= TimeSpan.Zero || delta > TimeSpan.FromMilliseconds(100))
            throw new ArgumentOutOfRangeException(nameof(delta));
        if (Current.Completed)
        {
            if (input.Pressed.Contains("jump")) Current = new(LevelNumber < 3 ? LevelNumber + 1 : 1);
            return;
        }
        Current.Update(delta, input);
    }
}
