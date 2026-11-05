using System.Numerics;
using NeonRift.Art;
using NeonRift.Content;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.World;

namespace NeonRift.Gameplay;

public enum StagePhase { Playing, Warning, Boss, Cleared, GameOver }

/// <summary>
/// One stage in play: forward-only scrolling camera, spawn triggers, collisions between the player,
/// enemies, shots and pickups, deaths and respawns, and the boss arena. Contains no drawing code
/// besides what actors render, so tests can run it headlessly. Its actors and its camera are entities of
/// <see cref="World"/> (the play scene's objects), which keeps them in spawn order and interpolates their drawing.
/// </summary>
public sealed class Stage : IStateHashed
{
    public const float WarningTime = 2.6f, ClearTime = 5.5f, RespawnDelay = 0.6f;
    private readonly Session _session;
    private readonly List<(float At, Action Action)> _scheduled = [];
    private readonly float[] _checkpoints;
    private int _nextSpawn;
    private float _phaseTime, _waitTime, _time;
    private float _lockLeft;
    private float _idleTime, _lingerTimer;
    /// <summary>Seconds without lingering pressure; after this, troopers keep arriving until the screen scrolls.</summary>
    public const float LingerGrace = 3.5f;

    /// <param name="def">The stage to play.</param>
    /// <param name="session">The run's lives, score and weapon.</param>
    /// <param name="world">Where the stage's actors live: the play scene's <c>Objects</c>, stepped with the scene.</param>
    /// <param name="checkpoint">The checkpoint to start from (0: the start).</param>
    /// <param name="seed">The seed of the stage's generator.</param>
    public Stage(StageDef def, Session session, IWorld world, int checkpoint = 0, int seed = 7)
    {
        Def = def;
        World = world;
        _session = session;
        // Simulation draws from it (the rapid gun's jitter, trooper timing, boss explosions): a SeededRandom draws the
        // same sequence as new Random(seed) and takes part in the state hash.
        Random = new SeededRandom(seed);
        Grid = new TileGrid(def.Layer, Tiles.Size, new Dictionary<int, TileFlags>
        {
            [TileId.Solid] = TileFlags.Solid,
            [TileId.OneWay] = TileFlags.OneWay,
            [TileId.Lava] = TileFlags.Hazard,
        })
        {
            // Left and right edges are walls; the bottom is open so pits are deadly.
            OutsideRule = (x, y) => x < 0 || x >= def.Layer.Width ? TileFlags.Solid : TileFlags.None,
        };
        Camera = new Camera2D(480, 270) { Bounds = new RectF(0, 0, def.PixelWidth, def.PixelHeight), FollowSharpness = 0 };
        var checkpoints = _checkpoints = def.Checkpoints.ToArray();
        float startX = checkpoint > 0 && checkpoint <= checkpoints.Length ? checkpoints[checkpoint - 1] : 48;
        CheckpointIndex = checkpoint > 0 && checkpoint <= checkpoints.Length ? checkpoint : 0;
        Camera.Position = new Vector2(MathF.Max(0, startX - 80), def.PixelHeight - 270);
        Camera.Clamp();
        CameraEntity = world.Create();
        world.Local(CameraEntity).Position2D = Camera.Position;
        _lockLeft = Camera.Position.X;
        // Skip spawns already behind the starting screen.
        while (_nextSpawn < def.Spawns.Count && def.Spawns[_nextSpawn].X < Camera.View.Left) _nextSpawn++;
        Player = Spawn(new Player(new Vector2(startX, GroundBelow(startX, 0) ?? StageBuilder.Surface(3)), session.Weapon));
    }

    public StageDef Def { get; }
    public TileGrid Grid { get; }
    public Camera2D Camera { get; }
    public SeededRandom Random { get; }
    public Player Player { get; }
    /// <summary>The world the stage's actors and camera live in.</summary>
    public IWorld World { get; }
    /// <summary>The camera's entity: its transform follows <see cref="Camera"/> after every update, so the scroll is drawn between updates.</summary>
    public EntityId CameraEntity { get; }
    /// <summary>Living enemies (bosses and their turrets included), in spawn order.</summary>
    public Attachments<Enemy> Enemies => World.All<Enemy>();
    /// <summary>Living player shots, in spawn order.</summary>
    public Attachments<PlayerShot> PlayerShots => World.All<PlayerShot>();
    /// <summary>Living enemy shots, in spawn order.</summary>
    public Attachments<EnemyShot> EnemyShots => World.All<EnemyShot>();
    /// <summary>Living weapon capsules, in spawn order.</summary>
    public Attachments<Capsule> Pickups => World.All<Capsule>();
    /// <summary>Living visual effects, in spawn order.</summary>
    public Attachments<Effect> Effects => World.All<Effect>();
    public Session Session => _session;
    public StagePhase Phase { get; private set; } = StagePhase.Playing;
    public float PhaseTime => _phaseTime;
    public float Time => _time;
    public Boss? Boss { get; private set; }
    public int CheckpointIndex { get; private set; }
    /// <summary>True once the clear sequence has finished; the scene moves on.</summary>
    public bool Finished => Phase == StagePhase.Cleared && _phaseTime >= ClearTime;
    public bool InArena => Camera.Position.X >= Def.ArenaX - 0.5f;
    /// <summary>Seconds since the screen last scrolled forward.</summary>
    public float IdleTime => _idleTime;

    /// <summary>Whether this is the last stage: its boss falls to the victory jingle instead of the stage fanfare.</summary>
    public bool Final { get; init; }
    public Action<string, float, float, float>? SoundSink { get; set; }
    public Action<string>? MusicSink { get; set; }
    /// <summary>Called when the player destroys an enemy (achievement progress).</summary>
    public Action<Enemy>? DefeatSink { get; set; }
    /// <summary>Optional lookup of the hero's arm rigs so bullets leave the drawn gun; tests can leave it null.</summary>
    internal Func<string, ArmRig?>? Rigs { get; set; }
    /// <summary>Optional lookup of an animation strip's length in frames (an effect plays its strip once); 1 frame when null, as in tests.</summary>
    public Func<string, int>? Strips { get; set; }
    /// <summary>The length in frames of the strip <paramref name="sprite"/>, from <see cref="Strips"/>; 1 without it.</summary>
    public int StripLength(string sprite) => Strips?.Invoke(sprite) ?? 1;
    /// <summary>Scratch list player shots fill with the enemies they touch, reused every update.</summary>
    internal List<Enemy> HitBuffer { get; } = [];

    /// <summary>
    /// Adds the stage's simulation state in sections named after its parts, so a divergence names the part:
    /// <c>stage</c> (phase and its timers, clock, scroll and linger timers, next spawn, checkpoint, camera position, the
    /// stage's generator, pending scheduled actions by time, the boss), <c>stage.player</c>, <c>stage.enemies</c>,
    /// <c>stage.shots</c> (the player's, then the enemies') and <c>stage.pickups</c>. Left out: effects (nothing in play
    /// reads them, see <see cref="Effect"/>), the camera's shake (drawing only), the session (hashed by the game) and the
    /// stage definition (fixed). The world hashes every entity (actors, effects and the camera) in the scene's section.
    /// </summary>
    public void HashState(StateHasher hash)
    {
        hash.Section("stage");
        hash.Add((int)Phase);
        hash.Add(_phaseTime);
        hash.Add(_waitTime);
        hash.Add(_time);
        hash.Add(_lockLeft);
        hash.Add(_idleTime);
        hash.Add(_lingerTimer);
        hash.Add(_nextSpawn);
        hash.Add(CheckpointIndex);
        hash.Add(Camera.Position);
        hash.Add(Random);
        hash.Add(_scheduled.Count);
        foreach (var (at, _) in _scheduled) hash.Add(at);
        // The boss is hashed with the enemies while alive; after its defeat the clear sequence still explodes around it.
        hash.Add(Boss is not null);
        if (Boss is not null)
        {
            hash.Add(Boss.Alive);
            hash.Add(Boss.Position);
        }
        hash.Section("stage.player");
        hash.Add(true);   // the player is always there (the section's layout from before actors were entities)
        Player.HashState(hash);
        hash.Section("stage.enemies");
        Actor.HashAll(hash, Enemies);
        hash.Section("stage.shots");
        Actor.HashAll(hash, PlayerShots);
        Actor.HashAll(hash, EnemyShots);
        hash.Section("stage.pickups");
        Actor.HashAll(hash, Pickups);
    }

    public void Update(float dt, PlayerInput input)
    {
        _time += dt;
        _phaseTime += dt;
        for (int i = _scheduled.Count - 1; i >= 0; i--)
        {
            if (_scheduled[i].At > _time) continue;
            var action = _scheduled[i].Action;
            _scheduled.RemoveAt(i);
            action();
        }
        Player.Step(dt, Phase is StagePhase.Cleared or StagePhase.GameOver ? PlayerInput.None : input);
        UpdateCamera(dt);
        ActivateSpawns();
        foreach (var e in Enemies) e.Update(dt);
        foreach (var s in PlayerShots) s.Update(dt);
        foreach (var s in EnemyShots) s.Update(dt);
        foreach (var c in Pickups) c.Update(dt);
        foreach (var fx in Effects) fx.Update(dt);
        Collide();
        UpdatePhase(dt);
    }

    private void UpdateCamera(float dt)
    {
        float arena = Def.ArenaX;
        if (Player.State == PlayerState.Alive && Phase is StagePhase.Playing)
        {
            float target = Math.Min(Player.Position.X - 170, arena);
            if (target > Camera.Position.X) Camera.Position = Camera.Position with { X = target };
        }
        Camera.Clamp();
        // Standing still is never safe: if the screen has not scrolled for a while, troopers keep coming.
        _idleTime = Camera.Position.X > _lockLeft + 0.5f ? 0 : _idleTime + dt;
        if (Phase == StagePhase.Playing && Player.State == PlayerState.Alive && _idleTime > LingerGrace)
        {
            _lingerTimer -= dt;
            if (_lingerTimer <= 0)
            {
                _lingerTimer = Math.Max(1.2f, 2.6f - (_idleTime - LingerGrace) * 0.1f);
                float sx = Camera.View.Right + 12;
                if (GroundBelow(sx, Camera.View.Top) is { } ground)
                    Spawn(new Trooper(new Vector2(sx, ground), -1, 0.6f + (float)Random.NextDouble()));
            }
        }
        else _lingerTimer = 0;
        _lockLeft = Camera.Position.X;
        Camera.Update(dt);
        World.Local(CameraEntity).Position2D = Camera.Position;
    }

    private void ActivateSpawns()
    {
        while (_nextSpawn < Def.Spawns.Count && Def.Spawns[_nextSpawn].X < Camera.View.Right + 24)
            Activate(Def.Spawns[_nextSpawn++]);
        if (Phase == StagePhase.Playing && InArena)
        {
            Phase = StagePhase.Warning;
            _phaseTime = 0;
            MusicSink?.Invoke("");
            Sound("alarm", 0.7f);
            Schedule(0.9f, () => Sound("alarm", 0.7f));
            Schedule(1.8f, () => Sound("alarm", 0.7f));
        }
    }

    private void Activate(Spawn s)
    {
        var view = Camera.View;
        switch (s.Kind)
        {
            case SpawnKind.Trooper: Spawn(new Trooper(new Vector2(s.X, s.Y), -1, 0.8f)); break;
            case SpawnKind.TrooperGate: Spawn(new TrooperGate(s.X, s.Count) { Position = new Vector2(s.X, s.Y) }); break;
            case SpawnKind.Gunner: Spawn(new Gunner(new Vector2(s.X, s.Y))); break;
            case SpawnKind.Turret: Spawn(new Turret(new Vector2(s.X, s.Y))); break;
            case SpawnKind.Egg: Spawn(new EggPod(new Vector2(s.X, s.Y))); break;
            case SpawnKind.Drones:
                for (int i = 0; i < s.Count; i++)
                    Spawn(new Drone(new Vector2(view.Right + 20 + i * 34, view.Top + 60 + (i % 2) * 34), i * 0.9f));
                break;
            case SpawnKind.Skiffs:
                for (int i = 0; i < s.Count; i++)
                    Spawn(new Skiff(new Vector2(view.Right + 20 + i * 30, view.Top + 50 + (i % 3) * 22), i * 0.5f));
                break;
            case SpawnKind.Carrier:
                Spawn(new Carrier(new Vector2(view.Left - 20, view.Top + 64), s.Param.Length > 0 ? s.Param[0] : 'S'));
                break;
            case SpawnKind.Checkpoint:
                break;
        }
    }

    /// <summary>
    /// Adds <paramref name="actor"/> to the stage: it becomes an entity of <see cref="World"/>, attached under its kind
    /// (enemy, player shot, enemy shot, capsule, effect or the player), at once. A walk already running does not visit
    /// it; every later one does, in spawn order.
    /// </summary>
    /// <exception cref="InvalidOperationException">The actor already joined a stage.</exception>
    public T Spawn<T>(T actor) where T : Actor
    {
        ArgumentNullException.ThrowIfNull(actor);
        actor.Join(this, World);
        return actor;
    }

    /// <summary>Destroys every living enemy shot.</summary>
    private void ClearEnemyShots()
    {
        foreach (var shot in EnemyShots) shot.Destroy();
    }

    public void Schedule(float delay, Action action) => _scheduled.Add((_time + delay, action));

    private void Collide()
    {
        if (Player.State == PlayerState.Alive)
        {
            var hurt = Player.Hurtbox;
            foreach (var shot in EnemyShots)
            {
                if (!shot.Bounds.Intersects(hurt)) continue;
                if (Player.Hurt()) { shot.Destroy(); break; }
            }
            if (Player.State == PlayerState.Alive)
                foreach (var enemy in Enemies)
                    if (enemy.Contact && enemy.Bounds.Inflate(-3).Intersects(hurt) && Player.Hurt()) break;
            foreach (var capsule in Pickups)
            {
                if (!capsule.Bounds.Intersects(hurt)) continue;
                capsule.Collect(Player, _session);
                Sound(capsule.Letter == '1' ? "one-up" : "powerup", 0.8f);
                AddScore(1000);
            }
            if (Player.Position.Y > Def.PixelHeight + 30) Player.Kill(force: true, fell: true);
            else if ((TileMotion.TouchedFlags(hurt.Inflate(-2), Grid) & TileFlags.Hazard) != 0 && Player.Invulnerable <= 0) Player.Kill(force: true);
        }

        // Checkpoints only matter for continues; Vela respawns where she fell.
        while (CheckpointIndex < _checkpoints.Length && Player.Position.X >= _checkpoints[CheckpointIndex]) CheckpointIndex++;
    }

    private void UpdatePhase(float dt)
    {
        if (Player.State == PlayerState.Waiting && Phase is not (StagePhase.GameOver or StagePhase.Cleared))
        {
            _waitTime += dt;
            if (_waitTime >= RespawnDelay)
            {
                _waitTime = 0;
                _session.Deaths++;
                if (_session.Lives <= 0)
                {
                    Phase = StagePhase.GameOver;
                    _phaseTime = 0;
                    MusicSink?.Invoke("");
                }
                else
                {
                    _session.Lives--;
                    _session.Weapon = WeaponKind.Pulse;
                    ClearEnemyShots();
                    Player.Respawn(SafeRespawnX(), WeaponKind.Pulse);
                }
            }
        }
        switch (Phase)
        {
            case StagePhase.Warning when _phaseTime >= WarningTime:
                Phase = StagePhase.Boss;
                _phaseTime = 0;
                var home = new Vector2(Def.ArenaX + 340, Camera.View.Top + 100);
                Boss = Spawn(Gameplay.Boss.Create(Def.Boss, home));
                MusicSink?.Invoke("boss");
                break;
            case StagePhase.Cleared:
                if (_phaseTime < 2.4f && (int)(_phaseTime * 10) != (int)((_phaseTime - dt) * 10) && Boss is not null)
                {
                    var at = Boss.Position + new Vector2((float)(Random.NextDouble() - 0.5) * Boss.Hitbox.Width, (float)(Random.NextDouble() - 0.5) * Boss.Hitbox.Height);
                    Explode(at, true);
                    Sound("explode-medium", 0.6f, pan: Pan(at.X));
                    Camera.Shake(0.25f);
                }
                break;
        }
    }

    /// <summary>A column near the left of the view with solid footing (never lava or a pit).</summary>
    private float SafeRespawnX()
    {
        var view = Camera.View;
        for (float x = view.Left + 70; x < view.Right - 40; x += 8)
            if (GroundBelow(x, view.Top) is { } y && (Grid.FlagsAt((int)(x / Tiles.Size), (int)(y / Tiles.Size)) & TileFlags.Hazard) == 0)
                return x;
        return view.Left + 70;
    }

    /// <summary>Y of the first standable surface (solid or platform) at or below <paramref name="fromY"/>, or null over a pit.</summary>
    public float? GroundBelow(float x, float fromY)
    {
        int tx = (int)MathF.Floor(x / Tiles.Size);
        for (int ty = Math.Max(0, (int)(fromY / Tiles.Size)); ty < Def.Layer.Height; ty++)
        {
            int id = Def.Layer.Get(tx, ty);
            if (id is TileId.Solid or TileId.OneWay or TileId.Lava) return ty * Tiles.Size;
        }
        return null;
    }

    internal void BossDefeated(Boss boss)
    {
        Phase = StagePhase.Cleared;
        _phaseTime = 0;
        Boss = boss;
        ClearEnemyShots();
        foreach (var e in Enemies) if (e != boss) e.Destroy();
        Explode(boss.Center, true);
        Sound("explode-big", 1f);
        Camera.Shake(0.8f);
        MusicSink?.Invoke("");
        Schedule(2.6f, () => { Sound("explode-big", 1f); Camera.Shake(0.6f); });
        Schedule(3.4f, () => Sound(Final ? "victory" : "fanfare", 0.9f));
    }

    public bool OnScreen(RectF bounds) => Camera.View.Intersects(bounds);

    public void EnemyFire(Vector2 from, Vector2 velocity, bool heavy = false, float gravity = 0)
    {
        // A short grace period at the start so the first screen never kills before the player can react.
        if (Phase is StagePhase.Cleared || _time < 2f) return;
        Spawn(new EnemyShot(from, velocity, heavy) { Gravity = gravity });
        if (!heavy) Sound("enemy-shot", 0.3f, pan: Pan(from.X));
    }

    public void AddScore(int points) => _session.AddScore(points);

    public void Explode(Vector2 at, bool big)
    {
        Spawn(new Effect(big ? "explosion_big" : "explosion", at, big ? 14 : 18));
        if (big) Camera.Shake(0.15f);
    }

    /// <summary>A shot's impact effect (<see cref="WeaponSpec.Impact"/>) at <paramref name="at"/>.</summary>
    public void Impact(Vector2 at, ImpactSpec impact) => Spawn(new Effect(impact.Sprite, at, impact.Fps) { Scale = impact.Scale });

    /// <summary>The burst of sparks of a shot glancing off armour.</summary>
    public void Deflect(Vector2 at) => Impact(at, Deflection);

    private static readonly ImpactSpec Deflection = new("hit3", 28, 0.5f);

    public void Flash(Vector2 at) => Spawn(new Effect("shot_hit", at, 40));

    public float Pan(float worldX) => Math.Clamp((worldX - Camera.View.Left) / Camera.ViewWidth, 0.1f, 0.9f);

    public void Sound(string name, float volume = 1, float pitch = 1, float pan = 0.5f) => SoundSink?.Invoke(name, volume, pitch, pan);
}
