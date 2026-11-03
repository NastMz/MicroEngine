using System.Numerics;
using CourtyardKey.Content;
using CourtyardKey.Entities;
using CourtyardKey.Model;
using CourtyardKey.World;
using MicroEngine;
using MicroEngine.World;

namespace CourtyardKey.Scenes;

/// <summary>Room loading, screen scrolling, area changes and per-room puzzle rules.</summary>
public sealed partial class PlayScene
{
    public const float ScrollTime = 0.75f;

    private Room? _scrollFrom;
    private Vector2 _scrollHeroFrom, _scrollHeroTo, _scrollCamFrom, _scrollCamTo;
    private float _scrollTime;
    private bool _closeShuttersPending;

    /// <summary>Raised after the hero settles in a room (tests and the HUD banner listen).</summary>
    public event Action<Room>? RoomEntered;

    private Room? _solvedFlagRoom;
    private string _solvedFlag = "";

    /// <summary>The progress flag of the current room's puzzle (built once per room).</summary>
    public string SolvedFlag
    {
        get
        {
            if (!ReferenceEquals(_solvedFlagRoom, Room))
            {
                _solvedFlag = $"solved:{Room.Key}";
                _solvedFlagRoom = Room;
            }
            return _solvedFlag;
        }
    }
    public bool RoomSolved => Progress.Has(SolvedFlag);

    private static Vector2 CameraFor(Room room) => room.Origin - new Vector2(0, HudHeight);

    public void PlaceAtCheckpoint(Checkpoint cp)
    {
        Area = cp.Area == Content.Dungeon.Id ? DungeonMap : OverworldMap;
        LoadRoom(cp.RoomX, cp.RoomY);
        Hero.Position = Room.TileToWorld(cp.TileX, cp.TileY);
        Hero.Facing = cp.Facing;
        CameraPosition = CameraFor(Room);
        SnapDrawPositions();
        Mode = PlayMode.Play;
        OnRoomSettled(fromEdge: null);
        Audio.PlayMusic(MusicForRoom());
    }

    private string MusicForRoom() =>
        Room.Def.Rule == RoomRule.Boss && !Progress.Has(BossFlag) ? "boss" : Area.Def.Music;

    private void LoadRoom(int x, int y)
    {
        Room = Area.Fresh(x, y);
        Progress.MarkVisited(Area.Id, x, y);
        G.Achievements.SetProgress(CourtyardAchievements.Cartographer, CourtyardAchievements.VisitedDungeonRooms(Progress));
        Spawner.Populate(this, Room);
        Boss = Actors.OfType<Warden>().FirstOrDefault();
        Light.Darkness = Room.Def.Darkness;
        Light.Shade = Area.Def.IsDungeon ? Visuals.Pal.DungeonDark : Visuals.Pal.ForestDark;
    }

    // ------------------------------------------------------------------ edges

    private void CheckExits()
    {
        var p = Hero.Position;
        var b = Room.Bounds;
        Dir? dir = p.X < b.Left ? Dir.Left : p.X >= b.Right ? Dir.Right : p.Y - 4 < b.Top ? Dir.Up : p.Y >= b.Bottom ? Dir.Down : null;
        if (dir is { } d) { LeaveThrough(d); return; }
        // Stairs: step on with the body centre.
        var (tx, ty) = Room.WorldToTile(Hero.Position + new Vector2(0, -3));
        if (Room.TileAt(tx, ty) == Tile.Stairs && Area.Def.Links.FirstOrDefault(l => l.Edge is null && l.RoomX == Room.X && l.RoomY == Room.Y) is { } stairs)
            Coroutines.Start(ChangeArea(stairs.Target, viaStairs: true));
    }

    private void LeaveThrough(Dir dir)
    {
        if (Room.Def.LeaveNeedsSword && !Progress.HasSword)
        {
            Hero.Position -= dir.ToVector() * 10;
            Hero.Facing = dir.Opposite();
            ShowText("Elder Oswin", "Wren! Don't wander off yet. Come and see me first, by the north road.");
            return;
        }
        if (Area.Def.Links.FirstOrDefault(l => l.Edge == dir && l.RoomX == Room.X && l.RoomY == Room.Y) is { } link)
        {
            Coroutines.Start(ChangeArea(link.Target, viaStairs: false));
            return;
        }
        var (dx, dy) = dir.ToStep();
        if (!Area.Exists(Room.X + dx, Room.Y + dy)) { Hero.Position -= dir.ToVector() * 2; return; }
        StartScroll(dir);
    }

    private void StartScroll(Dir dir)
    {
        var (dx, dy) = dir.ToStep();
        // The room's actors become the leaving room's (LeavingActors) because another room becomes the current one.
        _scrollFrom = Room;
        foreach (var b in LeavingActors.OfType<Boomerang>()) b.Destroy();
        LoadRoom(Room.X + dx, Room.Y + dy);
        _scrollHeroFrom = Hero.Position;
        var nb = Room.Bounds;
        _scrollHeroTo = dir switch
        {
            // The hero walks clear of the doorway, so shutters can close behind them.
            Dir.Right => new Vector2(nb.Left + 24, Hero.Position.Y),
            Dir.Left => new Vector2(nb.Right - 24, Hero.Position.Y),
            Dir.Down => new Vector2(Hero.Position.X, nb.Top + 30),
            _ => new Vector2(Hero.Position.X, nb.Bottom - 22),
        };
        _scrollCamFrom = CameraPosition;
        _scrollCamTo = CameraFor(Room);
        _scrollTime = 0;
        Hero.Controllable = false;
        Mode = PlayMode.Scroll;
        if (Area.Def.Music != Audio.CurrentMusic && MusicForRoom() != Audio.CurrentMusic) Audio.PlayMusic(MusicForRoom());
    }

    private void UpdateScroll(float dt)
    {
        _scrollTime += dt;
        float t = MathF.Min(1, _scrollTime / ScrollTime);
        float e = Ease.InOutSine(t);
        CameraPosition = Vector2.Lerp(_scrollCamFrom, _scrollCamTo, e);
        Hero.Position = Vector2.Lerp(_scrollHeroFrom, _scrollHeroTo, t);
        Hero.Update(0);
        if (t < 1) return;
        DestroyActors(_scrollFrom);
        var from = _scrollFrom;
        _scrollFrom = null;
        Hero.Controllable = true;
        Mode = PlayMode.Play;
        OnRoomSettled(from is null ? null : DirExtensions.FromVector(Room.Origin - from.Origin));
    }

    /// <summary>The room being scrolled away from (null when not scrolling).</summary>
    public Room? ScrollingFrom => _scrollFrom;
    /// <summary>The live actors of the room being scrolled away from (none when not scrolling).</summary>
    public RoomActors<Actor> LeavingActors => new(Objects, _scrollFrom);

    private System.Collections.IEnumerator ChangeArea(Checkpoint target, bool viaStairs)
    {
        Mode = PlayMode.Cutscene;
        Hero.Controllable = false;
        Audio.Play(viaStairs ? "stairs" : "door_slam", 0.7f);
        yield return FadeOut(0.45f);
        DestroyActors(Room);
        PlaceAtCheckpoint(target);
        Hero.Controllable = false;
        Mode = PlayMode.Cutscene;
        Progress.Entrance = target;
        SaveGame();
        _hud.Banner(Area.Def.Name);
        yield return FadeIn(0.5f);
        Hero.Controllable = true;
        Mode = PlayMode.Play;
    }

    private void OnRoomSettled(Dir? fromEdge)
    {
        _closeShuttersPending = false;
        var rule = Room.Def.Rule;
        if (rule == RoomRule.Ambush && !RoomSolved) _closeShuttersPending = true;
        if (rule == RoomRule.Boss && !Progress.Has(BossFlag))
        {
            _closeShuttersPending = true;
            Audio.PlayMusic("boss", 0.2f);
        }
        if (Room.Def.Name != _hud.LastRoomName) _hud.RoomName(Room.Def.Name);
        RoomEntered?.Invoke(Room);
    }

    // ------------------------------------------------------------------ room rules

    public RoomActors<Door> Doors => Actors.OfType<Door>();

    private void UpdateRoomRules(float dt)
    {
        if (_closeShuttersPending)
        {
            bool clear = true;
            foreach (var a in Actors)
                if (a is Door { Kind: DoorKind.Shutter } d && d.Bounds.Inflate(2).Intersects(Hero.Bounds)) { clear = false; break; }
            if (clear)
            {
                _closeShuttersPending = false;
                foreach (var d in Doors) if (d.Kind == DoorKind.Shutter) d.SetOpen(false);
                Audio.Play("door_slam");
                ShakeCamera(0.25f);
                if (Room.Def.Rule == RoomRule.Boss) Coroutines.Start(BossIntro());
            }
        }
        if (Room.Def.Rule == RoomRule.Ambush && !RoomSolved && !_closeShuttersPending && !AnyEnemyLeftToClear())
            Solve();
    }

    private bool AnyEnemyLeftToClear()
    {
        foreach (var a in Actors)
            if (a is Enemy { CountsForClear: true }) return true;
        return false;
    }

    /// <summary>Marks the room solved: shutters open and hidden treasure appears.</summary>
    public void Solve()
    {
        if (!Progress.Set(SolvedFlag)) return;
        foreach (var d in Doors) if (d.Kind == DoorKind.Shutter) d.SetOpen(true);
        Spawner.RevealHiddenChests(this, Room);
        Audio.Play("secret");
        Audio.Play("shutter", 0.6f);
    }

    /// <summary>True when the cell is a plate with a block resting on it.</summary>
    public bool PlatePressed(int x, int y)
    {
        foreach (var a in Actors)
            if (a is PushBlock { Sliding: false } b && b.Cell == (x, y)) return true;
        return false;
    }

    public void OnBlockSettled(PushBlock block)
    {
        if (Room.Def.Rule == RoomRule.Plate && Room.TileAt(block.Cell.X, block.Cell.Y) == Tile.Plate)
        {
            Audio.Play("switch", 0.8f, 0.8f);
            Solve();
        }
    }

    /// <summary>Whether a push block may slide into a room cell: open floor, nothing standing there.</summary>
    public bool CanBlockEnter(int x, int y, PushBlock self)
    {
        if (!Room.Tiles.InBounds(x, y)) return false;
        var tile = Room.TileAt(x, y);
        if (tile is not (Tile.Floor or Tile.Plate or Tile.Cracked or Tile.Carpet)) return false;
        var rect = Room.TileRect(x, y).Inflate(-1);
        foreach (var a in Actors)
            if (a != self && a.IsSolid && a.SolidBox.Intersects(rect)) return false;
        foreach (var e in Actors.OfType<Enemy>())
            if (e.Bounds.Intersects(rect)) return false;
        return true;
    }

    public void OnCrystalStruck(Crystal crystal)
    {
        if (Room.Def.Rule == RoomRule.Crystal) Solve();
    }
}
