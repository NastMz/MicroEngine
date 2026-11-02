using System.Numerics;
using CourtyardKey.Content;
using CourtyardKey.Model;
using MicroEngine;
using MicroEngine.World;

namespace CourtyardKey.World;

/// <summary>A placed object read from a room map: what it is and which tile cell it starts on.</summary>
public readonly record struct Placement(Marker Marker, int X, int Y);

/// <summary>
/// The live state of one screen: its tile layer (bushes can be cut) and the objects its map places.
/// A fresh room is built every time the hero enters, so cut bushes regrow and enemies respawn.
/// </summary>
public sealed class Room
{
    public const int W = AreaDef.RoomTilesX, Hgt = AreaDef.RoomTilesY, Ts = AreaDef.TileSize;

    public AreaDef Area { get; }
    public RoomDef Def { get; }
    public int X { get; }
    public int Y { get; }
    public TileLayer Tiles { get; }
    public IReadOnlyList<Placement> Placements { get; }
    public Vector2 Origin => new(X * AreaDef.RoomPixelWidth, Y * AreaDef.RoomPixelHeight);
    public RectF Bounds => new(Origin.X, Origin.Y, AreaDef.RoomPixelWidth, AreaDef.RoomPixelHeight);
    public string Key => Progress.RoomKey(Area.Id, X, Y);

    public Room(AreaDef area, int x, int y)
    {
        Area = area;
        X = x;
        Y = y;
        Def = area.Room(x, y) ?? throw new ArgumentException($"No room at {x},{y} in {area.Id}.");
        Tiles = new TileLayer(W, Hgt);
        var placements = new List<Placement>();
        for (int ty = 0; ty < Hgt; ty++)
            for (int tx = 0; tx < W; tx++)
            {
                char c = ty < Def.Map.Length && tx < Def.Map[ty].Length ? Def.Map[ty][tx] : '.';
                var (tile, marker) = Legend.Read(c, area.IsDungeon, Def.Base);
                Tiles[tx, ty] = (int)tile;
                if (marker != Marker.None) placements.Add(new Placement(marker, tx, ty));
            }
        Placements = placements;
    }

    public Tile TileAt(int tx, int ty) => (Tile)Tiles.Get(tx, ty);
    public void SetTile(int tx, int ty, Tile tile) => Tiles[tx, ty] = (int)tile;

    /// <summary>Converts a room-local tile to the world-space top-left pixel.</summary>
    public Vector2 TileToWorld(float tx, float ty) => Origin + new Vector2(tx * Ts, ty * Ts);

    /// <summary>Room-local tile containing a world point (may be outside the room).</summary>
    public (int X, int Y) WorldToTile(Vector2 p) => ((int)MathF.Floor((p.X - Origin.X) / Ts), (int)MathF.Floor((p.Y - Origin.Y) / Ts));

    public RectF TileRect(int tx, int ty) => new(Origin.X + tx * Ts, Origin.Y + ty * Ts, Ts, Ts);
}

/// <summary>
/// Collision view of the current room in world coordinates. Cells beyond an edge look into the
/// neighbouring room (so the hero can only cross where both sides are open); edges without a
/// neighbour are solid unless an area link leaves through them. Walkers treat water as solid.
/// </summary>
public sealed class RoomGrid(Room room, Func<int, int, Room?> neighbour, bool flying) : ICollisionGrid
{
    public int TileSize => Room.Ts;

    /// <summary>Whether this is the collision view of <paramref name="other"/>.</summary>
    public bool Views(Room other) => ReferenceEquals(room, other);

    public TileFlags FlagsAt(int worldTileX, int worldTileY)
    {
        int lx = worldTileX - room.X * Room.W, ly = worldTileY - room.Y * Room.Hgt;
        if (room.Tiles.InBounds(lx, ly)) return Resolve(room.TileAt(lx, ly));
        int rx = room.X + (lx < 0 ? -1 : lx >= Room.W ? 1 : 0);
        int ry = room.Y + (ly < 0 ? -1 : ly >= Room.Hgt ? 1 : 0);
        // Corners never connect: only straight edges lead somewhere.
        if (rx != room.X && ry != room.Y) return TileFlags.Solid;
        var dir = rx < room.X ? Dir.Left : rx > room.X ? Dir.Right : ry < room.Y ? Dir.Up : Dir.Down;
        var links = room.Area.Links;
        for (int i = 0; i < links.Count; i++)
            if (links[i] is var l && l.Edge == dir && l.RoomX == room.X && l.RoomY == room.Y) return TileFlags.None;
        var next = neighbour(rx, ry);
        if (next is null) return TileFlags.Solid;
        int nx = ((lx % Room.W) + Room.W) % Room.W, ny = ((ly % Room.Hgt) + Room.Hgt) % Room.Hgt;
        return Resolve(next.TileAt(nx, ny));
    }

    private TileFlags Resolve(Tile tile)
    {
        var flags = TileFlagsTable.For(tile);
        if (!flying && (flags & TileFlags.Water) != 0) flags |= TileFlags.Solid;
        // Fliers pass over water and low obstacles but not walls, trees or cliffs.
        if (flying && tile is Tile.Bush or Tile.Rock or Tile.Fence or Tile.Statue or Tile.Brazier) flags &= ~TileFlags.Solid;
        return flags;
    }
}
