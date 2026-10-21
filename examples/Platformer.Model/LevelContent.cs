using MicroEngine.Loop;
namespace Platformer.Model;

/// <summary>Immutable authored Meadow Run content, not an engine level format or editor schema.</summary>
public sealed class LevelContent
{
    public IReadOnlyList<Rect> Terrain { get; }
    public IReadOnlyList<Rect> Coins { get; }
    public Rect Danger { get; }
    public Rect Goal { get; }
    private LevelContent(Rect[] terrain, Rect[] coins, Rect danger, Rect goal)
    {
        Terrain = Array.AsReadOnly(terrain.ToArray());
        Coins = Array.AsReadOnly(coins.ToArray());
        Danger = danger;
        Goal = goal;
    }
    public static LevelContent First { get; } = new(
        [new(0,432,1920,48),new(336,384,96,48),new(432,360,96,72),new(768,336,144,24),new(1104,384,144,48)],
        [new(192,400,24,24),new(672,400,24,24),new(1344,400,24,24)],
        new(1440,412,48,20),new(1824,336,48,96));
    public static LevelContent Second { get; } = new(
        [new(0,432,480,48),new(576,432,1344,48),new(720,384,96,48),new(816,336,96,96)],
        [new(240,400,24,24),new(864,304,24,24),new(1368,400,24,24)],
        new(1536,412,48,20),new(1824,336,48,96));
    public static LevelContent For(int number) => number switch
    {
        1 => First,
        2 => Second,
        _ => throw new ArgumentOutOfRangeException(nameof(number))
    };
}
