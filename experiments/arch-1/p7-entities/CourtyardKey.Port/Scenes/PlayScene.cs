using System.Numerics;
using Arch1.Scene;
using CourtyardKey.Content;
using CourtyardKey.Entities;
using CourtyardKey.Model;
using CourtyardKey.UI;
using CourtyardKey.Visuals;
using CourtyardKey.World;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.World;

namespace CourtyardKey.Scenes;

public enum PlayMode { Play, Scroll, Cutscene }

/// <summary>
/// The adventure itself: the current room, its actors, room-to-room scrolling, combat resolution,
/// puzzles, treasure and the HUD. Split across partial files by concern.
/// </summary>
public sealed partial class PlayScene : Scene
{
    public const int HudHeight = 20;
    public static readonly RectF WorldView = new(0, HudHeight, AreaDef.RoomPixelWidth, AreaDef.RoomPixelHeight);

    private readonly List<RectF> _solids = [];
    private readonly List<Actor> _drawList = [];
    private readonly EntityId _camera;
    private RoomRenderer _tiles = null!;
    private Hud _hud = null!;
    private float _lowHealthTimer, _ambientTimer;
    private float _fade;
    private Color _fadeColor = Color.Black;

    public PlayScene(CourtyardKeyGame game, Progress progress)
    {
        G = game;
        Progress = progress;
        // Draws what new Random(seed) draws; a SeededRandom so its state takes part in the state hash.
        Rng = new SeededRandom(1234 + progress.Deaths);
        // The camera is an entity too, so the world interpolates it with everything else.
        _camera = Objects.Create();
    }

    /// <summary>
    /// The room's objects (P7): every actor and the camera are entities of this world. In stage 3 the scene module
    /// owns it and calls its step and draw hooks; here <see cref="Update"/> and <see cref="Draw"/> do.
    /// </summary>
    public Arch1.Scene.World Objects { get; } = new(StorageChoice.Current);

    /// <summary>
    /// State hash: the progress, the room's generator, the mode and its timers (hitstop, heartbeat, ambience), the
    /// scroll in progress, the camera position, the current room (area, cell and tiles: bushes can be cut), the hero,
    /// then every actor of the room and of the room being left, in creation order.
    /// Left out: particles (presentation only, with generators of their own), the HUD (toasts and banners only show),
    /// the fade (drawing), the transforms the world keeps for blending, and the light map and renderers (platform objects).
    /// </summary>
    public override void HashState(StateHasher hash)
    {
        hash.Section("play.progress");
        hash.Add(Progress);
        hash.Add((IStateHashed)Rng);
        hash.Add((int)Mode);
        hash.Add(Objects.Clock.FreezeRemaining);
        hash.Add(_lowHealthTimer);
        hash.Add(_ambientTimer);
        hash.Add((int)PresentedItem);
        hash.Add(CameraPosition);
        hash.Section("play.room");
        HashRoom(hash, Room);
        HashRoom(hash, _scrollFrom);
        hash.Add(_scrollHeroFrom);
        hash.Add(_scrollHeroTo);
        hash.Add(_scrollCamFrom);
        hash.Add(_scrollCamTo);
        hash.Add(_scrollTime);
        hash.Add(_closeShuttersPending);
        hash.Section("play.hero");
        hash.Add(Hero);
        hash.Section("play.actors");
        HashActors(hash, Actors);
        HashActors(hash, LeavingActors);
        // The original hashed its spawns not yet flushed here; the world has no such list. Writing its empty count keeps
        // the trace comparable with the original's (P7 equivalence check).
        hash.Add(0);
    }

    private static void HashRoom(StateHasher hash, Room? room)
    {
        hash.Add(room is not null);
        if (room is null) return;
        hash.Add(room.Area.Id);
        hash.Add(room.X);
        hash.Add(room.Y);
        var tiles = room.Tiles;
        for (int y = 0; y < tiles.Height; y++)
            for (int x = 0; x < tiles.Width; x++)
                hash.Add(tiles[x, y]);
    }

    private static void HashActors(StateHasher hash, RoomActors<Actor> actors)
    {
        hash.Add(actors.Count);
        foreach (var actor in actors) hash.Add(actor);
    }

    public CourtyardKeyGame G { get; }
    public Sprites Art => G.Art;
    public Progress Progress { get; }
    public Random Rng { get; }
    public Hero Hero { get; private set; } = null!;
    public override string? Describe() => Hero is null ? null : FormattableString.Invariant($"x={Hero.Position.X:0} y={Hero.Position.Y:0}");
    public PlayMode Mode { get; private set; }
    public Room Room { get; private set; } = null!;
    public AreaMap Area { get; private set; } = null!;
    public AreaMap OverworldMap { get; private set; } = null!;
    public AreaMap DungeonMap { get; private set; } = null!;
    /// <summary>The live actors of the current room, in creation order (the hero is in no room).</summary>
    public RoomActors<Actor> Actors => new(Objects, Room);
    /// <summary>The camera's entity.</summary>
    public EntityId CameraEntity => _camera;
    /// <summary>The camera's logical position: an entity's transform, so drawing blends it like any actor.</summary>
    public Vector2 CameraPosition
    {
        get => Objects.Local(_camera).Position2D;
        private set => Objects.Local(_camera).Position2D = value;
    }
    public ParticleSystem Particles { get; } = new(1024, 11);
    public ParticleSystem Glows { get; } = new(512, 12) { Additive = true };
    public ParticleSystem Ambient { get; } = new(256, 13);
    public Camera2D Camera { get; } = new(AreaDef.RoomPixelWidth, AreaDef.RoomPixelHeight + HudHeight) { MaxShake = 4 };
    public LightMap Light { get; private set; } = null!;
    public Boomerang? ActiveBoomerang => Actors.OfType<Boomerang>().FirstOrDefault();
    /// <summary>The Warden while it is in the room (drives the boss health bar).</summary>
    public Warden? Boss { get; private set; }

    public override void Enter()
    {
        G.Play = this;
        Light = new LightMap(G.Graphics) { Shade = Visuals.Pal.DungeonDark };
        _tiles = new RoomRenderer(Art);
        _hud = new Hud(this);
        OverworldMap = new AreaMap(Overworld.Create());
        DungeonMap = new AreaMap(Content.Dungeon.Create());
        Hero = new Hero(this, new Health(Progress.MaxHealth, Math.Max(2, Progress.Health)));
        PlaceAtCheckpoint(Progress.Entrance);
        Coroutines.Start(FadeIn(0.5f));
    }

    public override void Exit()
    {
        if (G.Play == this) G.Play = null;
        Light.Dispose();
    }

    public override void Resume() => Input.Reset();

    public override void Update(float dt)
    {
        Objects.BeginStep(Game.UpdateCount);   // stage 3: the scene module does this before the scene's step
        Progress.PlaySeconds += dt;
        Camera.Update(dt);
        Particles.Update(dt);
        Glows.Update(dt);
        Ambient.Update(dt);
        UpdateAmbience(dt);
        if (Mode == PlayMode.Play && Input.Pressed("pause") && !Hero.Health.IsDead)
        {
            Audio.Play("pause");
            Game.Scenes.Push(new PauseScene(this));
            return;
        }
        // Hitstop: the world's clock is frozen (0); otherwise it returns dt exactly (scale 1).
        if (Objects.Clock.Advance(dt) == 0) return;
        switch (Mode)
        {
            case PlayMode.Play: UpdatePlay(dt); break;
            case PlayMode.Scroll: UpdateScroll(dt); break;
            case PlayMode.Cutscene:
                foreach (var e in Actors.OfType<Effect>()) e.Update(dt);
                break;
        }
        _hud.Update(dt);
    }

    private void UpdatePlay(float dt)
    {
        Hero.Update(dt);
        foreach (var a in Actors) a.Update(dt);
        ResolveCombat();
        UpdateRoomRules(dt);
        if (Mode == PlayMode.Play) CheckExits();
        // A soft heartbeat when one heart or less remains.
        if (Hero.Health.Current <= 2 && !Hero.Health.IsDead && (_lowHealthTimer -= dt) <= 0)
        {
            _lowHealthTimer = 0.9f;
            Audio.Play("low_health", 0.6f);
        }
    }

    // ------------------------------------------------------------------ movement services for actors

    /// <summary>Moves an actor through the room's tiles and every other solid actor.</summary>
    public MoveResult MoveActor(Actor actor, Vector2 delta, bool flying, bool throughActors = false)
    {
        _solids.Clear();
        if (!throughActors)
            foreach (var a in Actors)
                if (a.IsSolid && a != actor) _solids.Add(a.SolidBox);
        var result = TileMotion.Move(actor.Bounds, delta, GridFor(flying), _solids);
        actor.Position += result.Position - actor.Bounds.Position;
        return result;
    }

    private RoomGrid? _walkGrid, _flyGrid;
    private Func<int, int, Room?>? _neighbour;

    /// <summary>The current room's collision view, made once per room instead of once per move.</summary>
    private RoomGrid GridFor(bool flying)
    {
        _neighbour ??= (x, y) => Area.Pristine(x, y);
        ref var grid = ref flying ? ref _flyGrid : ref _walkGrid;
        if (grid is null || !grid.Views(Room)) grid = new RoomGrid(Room, _neighbour, flying);
        return grid;
    }

    /// <summary>Destroys every actor of <paramref name="room"/> (the world releases them when the step ends).</summary>
    private void DestroyActors(Room? room)
    {
        foreach (var a in new RoomActors<Actor>(Objects, room)) a.Destroy();
    }

    public void Burst(Vector2 at, ParticleSpec spec, int count, bool additive = false)
        => (additive ? Glows : Particles).Emit(at, spec, count);

    public void Puff(Vector2 at, bool big = false)
    {
        new Effect(this, at, Art.Frames("puff"), 14);
        if (big)
            for (int i = 0; i < 4; i++)
                new Effect(this, at + MathX.FromAngle(i * MathF.PI / 2, 10), Art.Frames("puff"), 10);
    }

    public void ShakeCamera(float trauma) => Camera.Shake(trauma);
    public void Hitstop(float seconds) => Objects.Clock.Freeze(seconds);

    /// <summary>Rolls a drop with the given chance. Hearts are favoured when the hero is hurt.</summary>
    public void DropLoot(Vector2 at, float chance)
    {
        if (Rng.NextDouble() >= chance) return;
        double roll = Rng.NextDouble();
        bool hurt = Hero.Health.Current < Hero.Health.Max;
        PickupKind kind = hurt && roll < 0.4 ? PickupKind.Heart
            : roll < 0.8 ? PickupKind.Gem
            : roll < 0.96 ? PickupKind.BlueGem
            : PickupKind.RedGem;
        new Pickup(this, at, kind);
    }

    private void UpdateAmbience(float dt)
    {
        if (Room is null) return;
        _ambientTimer -= dt;
        if (_ambientTimer > 0) return;
        var view = Room.Bounds;
        var at = new Vector2(view.X + (float)Rng.NextDouble() * view.Width, view.Y + (float)Rng.NextDouble() * view.Height);
        switch (Room.Def.Ambience)
        {
            case Ambience.Pollen: _ambientTimer = 0.18f; Ambient.Emit(at, Fx.Pollen); break;
            case Ambience.Leaves: _ambientTimer = 0.35f; Ambient.Emit(at with { Y = view.Y }, Fx.FallingLeaf); break;
            case Ambience.Fireflies: _ambientTimer = 0.22f; Glows.Emit(at, Fx.Firefly); break;
            case Ambience.Dust: _ambientTimer = 0.2f; Ambient.Emit(at, Fx.Mote); break;
            case Ambience.Spray: _ambientTimer = 0.25f; Ambient.Emit(at, Fx.Pollen); if (Rng.NextDouble() < 0.5) Ambient.Emit(at, Fx.Spray); break;
            default: _ambientTimer = 1; break;
        }
    }

    // ------------------------------------------------------------------ fades

    private IEnumerator<object?> FadeIn(float seconds, Color? color = null)
    {
        _fadeColor = color ?? Color.Black;
        _fade = 1;
        for (float t = 0; t < seconds; t += 1 / 60f) { _fade = 1 - t / seconds; yield return null; }
        _fade = 0;
    }

    private IEnumerator<object?> FadeOut(float seconds, Color? color = null)
    {
        _fadeColor = color ?? Color.Black;
        for (float t = 0; t < seconds; t += 1 / 60f) { _fade = t / seconds; yield return null; }
        _fade = 1;
    }

    /// <summary>Copies live hero health into the progress and writes the save slot.</summary>
    public void SaveGame(bool toast = true)
    {
        Progress.Health = Hero.Health.Current;
        Progress.MaxHealth = Hero.Health.Max;
        G.Saves.Save(Progress);
        if (toast) _hud.Toast("Progress saved");
    }
}

/// <summary>
/// The live actors of one room that are a <typeparamref name="T"/>, in creation order: the world's
/// <see cref="Actor"/> attachments filtered by <see cref="Actor.Home"/>. <c>foreach</c> allocates nothing.
/// </summary>
public readonly struct RoomActors<T>(Arch1.Scene.World objects, Room? room) : IEnumerable<T> where T : Actor
{
    /// <summary>The ones that are also a <typeparamref name="TOther"/>.</summary>
    public RoomActors<TOther> OfType<TOther>() where TOther : Actor => new(objects, room);
    /// <summary>How many there are (walks them).</summary>
    public int Count
    {
        get
        {
            int n = 0;
            foreach (var _ in this) n++;
            return n;
        }
    }
    /// <summary>The first one, or null.</summary>
    public T? FirstOrDefault()
    {
        foreach (var a in this) return a;
        return null;
    }

    public Enumerator GetEnumerator() => new(objects.All<Actor>().GetEnumerator(), room);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator(Arch1.Scene.Attachments<Actor>.Enumerator inner, Room? room) : IEnumerator<T>
    {
        private Arch1.Scene.Attachments<Actor>.Enumerator _inner = inner;
        public T Current { get; private set; } = null!;
        readonly object System.Collections.IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (room is null) return false;
            while (_inner.MoveNext())
                if (_inner.Current is T a && ReferenceEquals(a.Home, room)) { Current = a; return true; }
            return false;
        }

        public void Reset() => _inner.Reset();
        public readonly void Dispose() { }
    }
}
