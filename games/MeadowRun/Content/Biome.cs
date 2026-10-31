namespace MeadowRun.Content;

/// <summary>The four worlds of Meadow Run. Each has its own tileset, backdrop and music.</summary>
public enum Biome { Meadow, Caves, Sky, Castle }

/// <summary>Tile ids shared by every level. Zero is empty.</summary>
public static class Tile
{
    public const int Empty = 0;
    public const int Ground = 1;
    public const int Brick = 2;
    public const int CoinBlock = 3;
    public const int PowerBlock = 4;
    public const int LifeBlock = 5;
    public const int MultiCoinBrick = 6;
    public const int UsedBlock = 7;
    public const int HardBlock = 8;
    public const int OneWay = 9;
    public const int Spikes = 10;
    public const int Lava = 11;
    public const int BackWall = 12;
    public const int HiddenLife = 13;

    /// <summary>Blocks that react to being hit from below.</summary>
    public static bool IsBumpable(int id) => id is Brick or CoinBlock or PowerBlock or LifeBlock or MultiCoinBrick or HiddenLife;
    /// <summary>Blocks drawn with the animated "?" face.</summary>
    public static bool IsQuestion(int id) => id is CoinBlock or PowerBlock or LifeBlock;
}
