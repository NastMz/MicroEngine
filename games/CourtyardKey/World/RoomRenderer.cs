using System.Numerics;
using CourtyardKey.Content;
using CourtyardKey.Visuals;
using MicroEngine;
using MicroEngine.Graphics;

namespace CourtyardKey.World;

/// <summary>
/// Picks and draws the sprite for every tile of a room: autotile masks, animation frames and
/// dungeon wall pieces. Sprite lookups are cached in arrays so drawing allocates nothing.
/// </summary>
public sealed class RoomRenderer
{
    private readonly Sprite[] _grass, _tall, _ruin, _floor, _path, _carpet, _cliff, _falls, _fence;
    private readonly Sprite[][] _flowers, _water, _channel;
    private readonly Sprite _bridgeTop, _bridgeBottom, _rock, _bush, _stairs, _stairsLeft, _stairsMiddle, _stairsRight, _cracked, _plateUp, _plateDown, _statue, _brazier;
    private readonly Sprite _wallN, _wallS, _wallW, _wallE, _wallCorner, _wallTorch;
    private readonly Sprite[] _frames; // n_a, n_b, e_a, e_b, s_a, s_b, w_a, w_b

    public RoomRenderer(Sprites s)
    {
        Sprite[] Seq(string prefix, int n, string suffix = "") => Enumerable.Range(0, n).Select(i => s[$"{prefix}{i}{suffix}"]).ToArray();
        _grass = Seq("grass", 4);
        _tall = Seq("tallgrass", 2);
        _ruin = Seq("ruin", 3);
        _floor = Seq("floor", 4);
        _path = Seq("path", 16);
        _carpet = Seq("carpet", 16);
        _cliff = Seq("cliff", 4);
        _falls = Seq("falls", TileArt.WaterFrames);
        _fence = Seq("fence", 4);
        _flowers = [Seq("flowers0_", 2), Seq("flowers1_", 2)];
        _water = Enumerable.Range(0, 16).Select(m => Seq($"water{m}_", TileArt.WaterFrames)).ToArray();
        _channel = Enumerable.Range(0, 16).Select(m => Seq($"channel{m}_", TileArt.WaterFrames)).ToArray();
        _bridgeTop = s["bridge_top"]; _bridgeBottom = s["bridge_bottom"]; _rock = s["rock"]; _bush = s["bush"];
        _stairs = s["stairs"]; _stairsLeft = s["stairs_l"]; _stairsMiddle = s["stairs_m"]; _stairsRight = s["stairs_r"]; _cracked = s["cracked"]; _plateUp = s["plate_up"]; _plateDown = s["plate_down"];
        _statue = s["statue"]; _brazier = s["brazier"];
        _wallN = s["wall_n"]; _wallS = s["wall_s"]; _wallW = s["wall_w"]; _wallE = s["wall_e"]; _wallCorner = s["wall_nw"]; _wallTorch = s["wall_torch"];
        _frames = [s["frame_n_a"], s["frame_n_b"], s["frame_e_a"], s["frame_e_b"], s["frame_s_a"], s["frame_s_b"], s["frame_w_a"], s["frame_w_b"]];
    }

    /// <summary>Draws the room in world space. <paramref name="pressed"/> tells which plates hold a block.</summary>
    public void Draw(IRenderer r, Room room, float time, Func<int, int, bool> pressed)
    {
        int waterFrame = (int)(time * 4) % TileArt.WaterFrames;
        int flowerFrame = (int)(time * 1.6f) % 2;
        int grassFrame = (int)(time * 1.1f + 0.5f) % 2;
        for (int y = 0; y < Room.Hgt; y++)
            for (int x = 0; x < Room.W; x++)
            {
                var sprite = Pick(room, x, y, waterFrame, flowerFrame, grassFrame, pressed);
                r.Draw(sprite.Texture, sprite.Source, room.Origin + new Vector2(x * Room.Ts, y * Room.Ts), Color.White);
            }
    }

    private static int Hash(int x, int y, int seed = 0) => (int)(Art.Hash(x, y, seed) * 1000);

    private Sprite Pick(Room room, int x, int y, int wf, int ff, int gf, Func<int, int, bool> pressed)
    {
        var tile = room.TileAt(x, y);
        int gx = room.X * Room.W + x, gy = room.Y * Room.Hgt + y;
        switch (tile)
        {
            case Tile.Grass or Tile.TreeBase or Tile.HouseBase: return _grass[Hash(gx, gy, 1) % 4];
            case Tile.Flowers: return _flowers[Hash(gx, gy, 2) % 2][(ff + Hash(gx, gy, 3) % 2) % 2];
            case Tile.TallGrass: return _tall[(gf + (gx + gy) % 2) % 2];
            case Tile.Path: return _path[Mask(room, x, y, t => t is Tile.Path or Tile.Bridge or Tile.Stairs)];
            case Tile.Water:
                bool cliffSides = Is(room, x - 1, y, Tile.Cliff) || Is(room, x + 1, y, Tile.Cliff) || IsCliffRow(room, y);
                if (cliffSides) return _falls[wf];
                return _water[Mask(room, x, y, t => t is Tile.Water or Tile.Bridge)][wf];
            case Tile.Bridge: return Is(room, x, y - 1, Tile.Bridge) ? _bridgeBottom : _bridgeTop;
            case Tile.Rock: return _rock;
            case Tile.Bush: return _bush;
            case Tile.Cliff:
                int m = (!IsOrOutside(room, x, y - 1, Tile.Cliff) ? 1 : 0) | (!IsOrOutside(room, x, y + 1, Tile.Cliff) ? 2 : 0);
                return _cliff[m];
            case Tile.Fence:
                bool Joins(int tx) => !room.Tiles.InBounds(tx, y) || room.TileAt(tx, y) is Tile.Fence or Tile.Rock or Tile.TreeBase;
                return _fence[(Joins(x - 1) ? 1 : 0) | (Joins(x + 1) ? 2 : 0)];
            case Tile.RuinFloor or Tile.RuinBase: return _ruin[Hash(gx, gy, 4) % 3];
            case Tile.Stairs:
                // Side-by-side stair tiles join into one stairwell.
                bool joinLeft = Is(room, x - 1, y, Tile.Stairs), joinRight = Is(room, x + 1, y, Tile.Stairs);
                return joinLeft && joinRight ? _stairsMiddle : joinLeft ? _stairsRight : joinRight ? _stairsLeft : _stairs;
            case Tile.Floor or Tile.PillarBase: return _floor[Hash(gx, gy, 5) % 4];
            case Tile.Cracked: return _cracked;
            case Tile.Carpet: return _carpet[Mask(room, x, y, t => t is Tile.Carpet)];
            case Tile.Channel: return _channel[Mask(room, x, y, t => t is Tile.Channel, outside: false)][wf];
            case Tile.Statue: return _statue;
            case Tile.Brazier: return _brazier;
            case Tile.Plate: return pressed(x, y) ? _plateDown : _plateUp;
            case Tile.Doorway: return DoorFrame(room, x, y);
            case Tile.WallTorch: return y == 0 ? _wallTorch : WallPiece(x, y);
            case Tile.Wall: return WallPiece(x, y);
            default: return _grass[0];
        }
    }

    private static bool IsCliffRow(Room room, int y) => y >= 0 && y < Room.Hgt && room.TileAt(0, y) == Tile.Cliff;
    private static bool Is(Room room, int x, int y, Tile t) => room.Tiles.InBounds(x, y) && room.TileAt(x, y) == t;
    private static bool IsOrOutside(Room room, int x, int y, Tile t) => !room.Tiles.InBounds(x, y) || room.TileAt(x, y) == t;

    private static int Mask(Room room, int x, int y, Func<Tile, bool> same, bool outside = true)
    {
        bool S(int tx, int ty) => room.Tiles.InBounds(tx, ty) ? same(room.TileAt(tx, ty)) : outside;
        return (S(x, y - 1) ? 1 : 0) | (S(x + 1, y) ? 2 : 0) | (S(x, y + 1) ? 4 : 0) | (S(x - 1, y) ? 8 : 0);
    }

    private Sprite WallPiece(int x, int y)
    {
        bool top = y == 0, bottom = y == Room.Hgt - 1, left = x == 0, right = x == Room.W - 1;
        if ((top || bottom) && (left || right)) return _wallCorner;
        if (top) return _wallN;
        if (bottom) return _wallS;
        if (left) return _wallW;
        if (right) return _wallE;
        return _wallCorner;
    }

    private Sprite DoorFrame(Room room, int x, int y)
    {
        int side = y == 0 ? 0 : x == Room.W - 1 ? 1 : y == Room.Hgt - 1 ? 2 : 3;
        bool first = side is 0 or 2 ? !Is(room, x - 1, y, Tile.Doorway) : !Is(room, x, y - 1, Tile.Doorway);
        return _frames[side * 2 + (first ? 0 : 1)];
    }
}
