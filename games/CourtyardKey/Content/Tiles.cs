using MicroEngine.World;

namespace CourtyardKey.Content;

/// <summary>Tile ids stored in room layers. Dynamic objects (doors, chests, enemies) are entities instead.</summary>
public enum Tile
{
    None = 0,
    // Overworld
    Grass, Flowers, TallGrass, Path, Water, Bridge, TreeBase, HouseBase, Rock, Cliff, Bush, Fence, RuinFloor, RuinBase, Stairs,
    // Dungeon
    Floor, Wall, WallTorch, Channel, Statue, Plate, Carpet, Cracked, Brazier, PillarBase, Doorway,
}

/// <summary>Things a map character spawns besides its tile.</summary>
public enum Marker
{
    None, Tree, House, Slime, Bat, Spitter, Knight, Chest, HiddenChest, Sign, Pot, RuinPillar, Pillar, Block, Crystal,
    Boss, Pedestal, Gem, DoorOpen, DoorLocked, Shutter, BigDoor, Exit, Torch, Brazier,
}

public static class TileFlagsTable
{
    /// <summary>Cut by the sword: bushes.</summary>
    public const TileFlags Cuttable = TileFlags.Custom1;
    /// <summary>Walking onto it starts an area change (dungeon stairs).</summary>
    public const TileFlags Stairs = TileFlags.Custom2;

    public static TileFlags For(Tile tile) => tile switch
    {
        Tile.TreeBase or Tile.HouseBase or Tile.Rock or Tile.Cliff or Tile.Fence or Tile.RuinBase
            or Tile.Wall or Tile.WallTorch or Tile.Statue or Tile.Brazier or Tile.PillarBase => TileFlags.Solid,
        Tile.Bush => TileFlags.Solid | Cuttable,
        Tile.Water or Tile.Channel => TileFlags.Water,
        Tile.Stairs => Stairs,
        _ => TileFlags.None,
    };

    public static bool IsWaterLike(Tile t) => t is Tile.Water or Tile.Channel;
}

/// <summary>Map characters. Overworld and dungeon maps share enemy and object letters.</summary>
public static class Legend
{
    public static (Tile Tile, Marker Marker) Read(char c, bool dungeon, Tile baseTile)
    {
        // Shared object letters stand on the room's base floor.
        switch (c)
        {
            case 'g': return (baseTile, Marker.Slime);
            case 'v': return (baseTile, Marker.Bat);
            case 'z': return (baseTile, Marker.Spitter);
            case 'k': return (baseTile, Marker.Knight);
            case 'C': return (baseTile, Marker.Chest);
            case 'c': return (baseTile, Marker.HiddenChest);
            case 'p': return (baseTile, Marker.Pot);
            case 'G': return (baseTile, Marker.Gem);
        }
        if (dungeon)
            return c switch
            {
                '#' => (Tile.Wall, Marker.None),
                'T' => (Tile.WallTorch, Marker.Torch),
                '.' => (Tile.Floor, Marker.None),
                '~' => (Tile.Channel, Marker.None),
                's' => (Tile.Statue, Marker.None),
                'B' => (Tile.Floor, Marker.Block),
                '_' => (Tile.Plate, Marker.None),
                '*' => (Tile.Floor, Marker.Crystal),
                '+' => (Tile.Carpet, Marker.None),
                ':' => (Tile.Cracked, Marker.None),
                'i' => (Tile.Brazier, Marker.Brazier),
                '|' => (Tile.PillarBase, Marker.Pillar),
                'D' => (Tile.Doorway, Marker.DoorOpen),
                'L' => (Tile.Doorway, Marker.DoorLocked),
                'X' => (Tile.Doorway, Marker.Shutter),
                'K' => (Tile.Doorway, Marker.BigDoor),
                'E' => (Tile.Doorway, Marker.Exit),
                'O' => (Tile.Carpet, Marker.Boss),
                'P' => (Tile.Carpet, Marker.Pedestal),
                _ => throw new FormatException($"Unknown dungeon map character '{c}'."),
            };
        return c switch
        {
            '.' => (Tile.Grass, Marker.None),
            ',' => (Tile.Flowers, Marker.None),
            ';' => (Tile.TallGrass, Marker.None),
            '=' => (Tile.Path, Marker.None),
            '~' => (Tile.Water, Marker.None),
            'H' => (Tile.Bridge, Marker.None),
            'T' => (Tile.TreeBase, Marker.Tree),
            't' => (Tile.TreeBase, Marker.None),
            'A' => (Tile.HouseBase, Marker.House),
            'h' => (Tile.HouseBase, Marker.None),
            'r' => (Tile.Rock, Marker.None),
            '^' => (Tile.Cliff, Marker.None),
            'b' => (Tile.Bush, Marker.None),
            'f' => (Tile.Fence, Marker.None),
            'x' => (Tile.RuinFloor, Marker.None),
            'I' => (Tile.RuinBase, Marker.RuinPillar),
            'E' => (Tile.Stairs, Marker.None),
            'S' => (baseTile, Marker.Sign),
            _ => throw new FormatException($"Unknown overworld map character '{c}'."),
        };
    }
}
