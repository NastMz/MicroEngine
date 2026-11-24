namespace MicroEngine.World;

/// <summary>Collision semantics of a tile. Games may use the Custom bits for their own meaning.</summary>
[Flags]
public enum TileFlags
{
    /// <summary>No collision meaning: the tile is empty space.</summary>
    None = 0,
    /// <summary>Blocks movement from every side.</summary>
    Solid = 1,
    /// <summary>
    /// Blocks only bodies moving down onto its top edge (jump-through platform). For overlaps, point queries and the
    /// nearest surface it is a platform at the top of the tile, <c>PhysicsContract.OneWayTileThickness</c> of a tile thick.
    /// </summary>
    OneWay = 2,
    /// <summary>Hurts on contact; does not block.</summary>
    Hazard = 4,
    /// <summary>Liquid; does not block.</summary>
    Water = 8,
    /// <summary>Free for the game's own meaning (ladders, ice, triggers...).</summary>
    Custom1 = 16,
    /// <summary>Free for the game's own meaning.</summary>
    Custom2 = 32,
    /// <summary>Free for the game's own meaning.</summary>
    Custom3 = 64,
    /// <summary>Free for the game's own meaning.</summary>
    Custom4 = 128,
    /// <summary>
    /// A slope that rises to the right: the half of the tile under the line from its lower left corner to its upper
    /// right one is solid. Geometry only on a backend that declares
    /// <see cref="MicroEngine.Physics.PhysicsCapabilities.TileSlopes"/>. A character walks on slopes as a capsule; a
    /// box is stopped at the foot of one.
    /// </summary>
    SlopeRight = 256,
    /// <summary>
    /// A slope that rises to the left: the half of the tile under the line from its upper left corner to its lower
    /// right one is solid. Geometry only on a backend that declares
    /// <see cref="MicroEngine.Physics.PhysicsCapabilities.TileSlopes"/>.
    /// </summary>
    SlopeLeft = 512,
}

/// <summary>
/// A grid of square tiles with collision flags: what tile geometry is made of
/// (<see cref="MicroEngine.Physics.IPhysicsWorld2D.AddTiles"/>) and what the tile helpers read.
/// </summary>
public interface ICollisionGrid
{
    /// <summary>Size of a square tile in pixels.</summary>
    int TileSize { get; }
    /// <summary>The flags of the tile at (<paramref name="tileX"/>, <paramref name="tileY"/>), in tiles; any coordinate, inside the map or not.</summary>
    TileFlags FlagsAt(int tileX, int tileY);
}
