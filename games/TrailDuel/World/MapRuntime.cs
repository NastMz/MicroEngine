using MicroEngine.Scenes;
using TrailDuel.Rules;

namespace TrailDuel.World;

public enum StepKind { Blocked, Step, Hop }

/// <summary>
/// The live state of the current map: collision (tiles, buildings, signs, NPCs, item balls),
/// warps, triggers and which NPCs are present given the story flags. No rendering here,
/// so movement rules are testable headlessly; given the scene's objects, its NPCs are drawn through them.
/// </summary>
internal sealed class MapRuntime
{
    private readonly bool[] _solid;
    private readonly IWorld? _world;
    public MapDef Def { get; }
    public GameState State { get; }
    public List<Actor> Npcs { get; } = [];
    public int Width => Def.Width;
    public int Height => Def.Height;

    /// <param name="world">The scene's objects its NPCs are drawn through; none for rule tests.</param>
    public MapRuntime(MapDef def, GameState state, IWorld? world = null)
    {
        Def = def;
        State = state;
        _world = world;
        _solid = new bool[def.Width * def.Height];
        for (int y = 0; y < def.Height; y++)
        for (int x = 0; x < def.Width; x++)
            _solid[y * def.Width + x] = Tiles.IsSolid(def.KindAt(x, y));
        foreach (var b in def.Buildings)
            for (int y = b.Y; y < b.Y + b.H; y++)
            for (int x = b.X; x < b.X + b.W; x++)
                _solid[y * def.Width + x] = b.Locked || !(y == b.Y + b.H - 1 && x == b.X + b.DoorX);
        foreach (var s in def.Signs) _solid[s.Y * def.Width + s.X] = true;
        RefreshNpcs();
    }

    /// <summary>Re-evaluates which NPCs exist after story flags change, keeping existing actors in place.</summary>
    public void RefreshNpcs()
    {
        Npcs.RemoveAll(a =>
        {
            if (a.Def is not { } d || IsPresent(d)) return false;
            a.Destroy();
            return true;
        });
        foreach (var d in Def.Npcs)
            if (IsPresent(d) && Npcs.All(a => a.Def != d))
                Npcs.Add(new Actor(d.Look, d.X, d.Y, d.Facing, _world) { Def = d });
    }

    /// <summary>Takes its NPCs out of the scene's objects (the player leaves the map).</summary>
    public void Unload()
    {
        foreach (var npc in Npcs) npc.Destroy();
    }

    private bool IsPresent(NpcDef d)
        => (d.ShowIf is null || State.Has(d.ShowIf)) && (d.HideIf is null || !State.Has(d.HideIf));

    public TileKind KindAt(int x, int y) => Def.KindAt(x, y);

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    public bool TileSolid(int x, int y) => !InBounds(x, y) || _solid[y * Width + x];

    // Plain loops: these run per tile while drawing and per step, where LINQ allocated a closure each call.
    public ItemBallDef? ItemAt(int x, int y)
    {
        foreach (var i in Def.Items) if (i.X == x && i.Y == y && !State.Has(i.Flag)) return i;
        return null;
    }

    public SignDef? SignAt(int x, int y)
    {
        foreach (var s in Def.Signs) if (s.X == x && s.Y == y) return s;
        return null;
    }

    public WarpDef? WarpAt(int x, int y)
    {
        foreach (var w in Def.Warps) if (w.X == x && w.Y == y) return w;
        return null;
    }

    public TriggerDef? TriggerAt(int x, int y)
    {
        foreach (var t in Def.Triggers) if (t.X == x && t.Y == y) return t;
        return null;
    }

    public BuildingDef? LockedDoorAt(int x, int y)
    {
        foreach (var b in Def.Buildings) if (b.Locked && x == b.X + b.DoorX && y == b.Y + b.H - 1) return b;
        return null;
    }

    /// <summary>NPC standing on, or moving into, a tile.</summary>
    public Actor? NpcAt(int x, int y)
    {
        foreach (var a in Npcs) if (a.Visible && a.X == x && a.Y == y) return a;
        return null;
    }

    public bool Occupied(int x, int y, Actor? player)
        => TileSolid(x, y) || NpcAt(x, y) is not null || ItemAt(x, y) is not null || (player is not null && player.X == x && player.Y == y);

    /// <summary>What happens if <paramref name="actor"/> tries to move one tile in <paramref name="dir"/>.</summary>
    public StepKind CheckStep(Actor actor, Direction dir, Actor? player)
    {
        var (dx, dy) = dir.Delta();
        int tx = actor.X + dx, ty = actor.Y + dy;
        if (KindAt(tx, ty) == TileKind.Ledge)
        {
            if (dir != Direction.Down || actor != player) return StepKind.Blocked;
            return Occupied(tx, ty + 1, null) || KindAt(tx, ty + 1) == TileKind.Ledge ? StepKind.Blocked : StepKind.Hop;
        }
        var other = actor == player ? null : player;
        if (actor != player && WarpAt(tx, ty) is not null) return StepKind.Blocked;
        return Occupied(tx, ty, other) ? StepKind.Blocked : StepKind.Step;
    }

    /// <summary>
    /// Whether a trainer standing at its post sees the player: same row or column in its facing
    /// direction, within sight range, with nothing solid in between.
    /// </summary>
    public bool TrainerSees(Actor trainer, int playerX, int playerY)
    {
        if (trainer.Def?.Trainer is not { } t) return false;
        var (dx, dy) = trainer.Facing.Delta();
        for (int i = 1; i <= t.Sight; i++)
        {
            int x = trainer.X + dx * i, y = trainer.Y + dy * i;
            if (x == playerX && y == playerY) return true;
            if (TileSolid(x, y) || NpcAt(x, y) is not null) return false;
        }
        return false;
    }
}
