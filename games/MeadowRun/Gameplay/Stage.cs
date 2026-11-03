using System.Numerics;
using MeadowRun.Art;
using MeadowRun.Content;
using MeadowRun.Gameplay.Actors;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.World;

namespace MeadowRun.Gameplay;

internal enum StagePhase { Playing, Dying, FlagSlide, WalkOff, Tally, Victory, Done }
internal enum StageOutcome { None, Died, Cleared, Won }

/// <summary>
/// One attempt at a level. Pure simulation plus effect requests (sound, shake, hit-stop, particles),
/// so the whole level can be played headlessly in tests. The play scene draws it and plays sounds.
/// </summary>
internal sealed partial class Stage : IStateHashed
{
    public const float TimeUnitSeconds = 0.4f;
    private const int MaxSeeds = 2;

    private readonly List<PlatformActor> _platforms = [];
    private readonly Dictionary<(int, int), float> _bumps = [];
    private readonly Dictionary<(int, int), int> _multiCoins = [];
    private readonly List<Checkpoint> _checkpoints = [];
    private float _freeze, _phaseTime, _throwPose;
    private int _stompChain;
    private bool _hurryPlayed;

    public Level Level { get; }
    public Session Session { get; }
    public Player Player { get; }
    public EntityList<Actor> Actors { get; } = new();
    public IReadOnlyList<PlatformActor> Platforms => _platforms;
    public Camera2D Camera { get; }
    public StagePhase Phase { get; private set; } = StagePhase.Playing;
    public StageOutcome Outcome { get; private set; }
    /// <summary>Seconds of stage time; drives platforms, fire bars and animation.</summary>
    public float Clock { get; private set; }
    /// <summary>Remaining time units (one unit = <see cref="TimeUnitSeconds"/>).</summary>
    public float TimeLeft { get; private set; }
    public int CheckpointReached { get; private set; }
    public GoalPole? Goal { get; private set; }
    public Grumbold? Boss { get; private set; }
    public RectF? CameraLock { get; private set; }
    public float ThrowPose => _throwPose;
    public bool Frozen => _freeze > 0;
    /// <summary>Score awarded by the goal pole grab, shown in the tally.</summary>
    public int FlagScore { get; private set; }
    public int TimeBonus { get; private set; }

    /// <summary>Sound requests: name, volume, pitch, pan.</summary>
    public Action<string, float, float, float>? SoundSink { get; set; }
    /// <summary>Music requests raised by the stage (boss intro, victory).</summary>
    public Action<string>? MusicSink { get; set; }
    /// <summary>Called for every enemy stomped (achievement progress).</summary>
    public Action? StompSink { get; set; }

    public Stage(Level level, Session session, int checkpoint = 0)
    {
        Level = level ?? throw new ArgumentNullException(nameof(level));
        Session = session ?? throw new ArgumentNullException(nameof(session));
        TimeLeft = level.Def.Time;
        Camera = new Camera2D(320, 180) { Bounds = level.Bounds, MaxShake = 4 };
        Player = new Player(level.StartPosition(level.Start)) { Form = session.Form };
        SpawnFromLevel(checkpoint);
        CheckpointReached = checkpoint;
        if (checkpoint > 0 && checkpoint <= _checkpoints.Count)
        {
            var cp = _checkpoints[checkpoint - 1];
            Player.Position = new Vector2(cp.Center.X - Player.Width / 2f, cp.Bounds.Bottom - Player.Height);
        }
        SnapCamera();
    }

    /// <summary>
    /// Hashes the attempt: phase and outcome, clocks and timers, stomp chain, scores of the tally, the camera (its
    /// position wakes actors) and its lock, the tiles (bumped and broken blocks change them), the multi-coin bricks,
    /// Pip, the platforms and the actors in list order. Leaves out what only presentation uses: particles, popups,
    /// block bump offsets, the throw pose, ambient emission and camera shake (the camera's own generator only moves
    /// the drawn view).
    /// </summary>
    public void HashState(StateHasher hash)
    {
        hash.Add((int)Phase);
        hash.Add((int)Outcome);
        hash.Add(DeathCause);
        hash.Add(Clock);
        hash.Add(TimeLeft);
        hash.Add(_freeze);
        hash.Add(_phaseTime);
        hash.Add(_stompChain);
        hash.Add(_hurryPlayed);
        hash.Add(_lastInputJumpHeld);
        hash.Add(CheckpointReached);
        hash.Add(FlagScore);
        hash.Add(TimeBonus);
        hash.Add(_look);
        hash.Add(Camera.Position);
        hash.Add(CameraLock.HasValue);
        if (CameraLock is { } view)
        {
            hash.Add(view.X);
            hash.Add(view.Y);
            hash.Add(view.Width);
            hash.Add(view.Height);
        }
        var tiles = Level.Tiles;
        hash.Add(tiles.Width);
        hash.Add(tiles.Height);
        for (int y = 0; y < tiles.Height; y++)
        for (int x = 0; x < tiles.Width; x++)
            hash.Add(tiles[x, y]);
        hash.Add(_multiCoins.Count);
        foreach (var ((x, y), left) in _multiCoins.OrderBy(e => e.Key))
        {
            hash.Add(x);
            hash.Add(y);
            hash.Add(left);
        }
        hash.Add(Player);
        hash.Add(_platforms.Count);
        foreach (var platform in _platforms) hash.Add(platform);
        hash.Add(Actors.Count);
        foreach (var actor in Actors) hash.Add(actor);
    }

    public void Spawn(Actor actor)
    {
        if (actor is PlatformActor p) _platforms.Add(p);
        else Actors.Add(actor);
    }

    public bool IsNearView(RectF bounds, float margin) => Camera.View.Inflate(margin).Intersects(bounds);

    public void Update(float dt, PlayerInput input)
    {
        UpdateBumps(dt);
        UpdateEffects(dt);
        Camera.Update(dt);
        if (_freeze > 0)
        {
            _freeze -= dt;
            return;
        }
        Clock += dt;
        _phaseTime += dt;
        _throwPose = MathF.Max(0, _throwPose - dt);
        switch (Phase)
        {
            case StagePhase.Playing: UpdatePlaying(dt, input); break;
            case StagePhase.Dying: UpdateDying(dt); break;
            case StagePhase.FlagSlide: UpdateFlagSlide(dt); break;
            case StagePhase.WalkOff: UpdateWalkOff(dt); break;
            case StagePhase.Tally: UpdateTally(dt); break;
            case StagePhase.Victory: UpdateVictory(dt); break;
        }
        if (Phase != StagePhase.Dying) FollowCamera(dt);
        EmitAmbient(dt);
    }

    private void UpdatePlaying(float dt, PlayerInput input)
    {
        NoteInput(input);
        TimeLeft = MathF.Max(0, TimeLeft - dt / TimeUnitSeconds);
        if (!_hurryPlayed && TimeLeft <= 100 && Level.Def.Time > 100)
        {
            _hurryPlayed = true;
            Sfx("hurry", 1, 1);
            Popup("HURRY!", Player.Center + new Vector2(0, -24), Pal.Coral);
        }

        foreach (var p in _platforms) p.Update(dt);
        float previousBottom = Player.Bounds.Bottom;
        float fallSpeed = Player.Velocity.Y;
        Player.Step(dt, input, Level, _platforms);
        if (Player.Grounded) _stompChain = 0;
        HandlePlayerEvents(fallSpeed);

        if (Player.Form == PipForm.Ember && input.RunPressed && Actors.OfType<SeedShot>().Count() < MaxSeeds)
        {
            Spawn(new SeedShot(this, Player.Center + new Vector2(Player.Facing * 8, -2), Player.Facing));
            _throwPose = 0.16f;
            Sfx("throw", 1, 0.7f, Player.Center);
        }

        Actors.Update(dt);
        ResolveItems();
        ResolveEnemies(previousBottom);
        ResolveHazards();
        UpdateBossArena();
        if (TimeLeft <= 0 && Phase == StagePhase.Playing) Die("time");
    }

    private void HandlePlayerEvents(float fallSpeed)
    {
        var e = Player.Events;
        if ((e & PlayerEvents.Jumped) != 0)
        {
            float speed01 = MathF.Abs(Player.Velocity.X) / Tuning.RunSpeed;
            Sfx("jump", 1 + speed01 * 0.12f, 0.7f, Player.Center);
            Dust(Player.Feet, 0, 5);
        }
        if ((e & PlayerEvents.Landed) != 0 && fallSpeed > 120)
        {
            LandPuff(Player.Feet, fallSpeed / Tuning.TerminalVelocity);
            Sfx("land", 0.9f + Player.LandImpact * 0.2f, 0.25f + Player.LandImpact * 0.35f, Player.Center);
        }
        if ((e & PlayerEvents.Skid) != 0 && (int)(Clock * 30) % 3 == 0)
        {
            Dust(Player.Feet, -MathF.Sign(Player.Velocity.X), 1);
            if ((int)(Clock * 30) % 9 == 0) Sfx("skid", 1, 0.25f, Player.Center);
        }
        else if (Player.Grounded && MathF.Abs(Player.Velocity.X) > Tuning.WalkSpeed + 10 && (int)(Clock * 60) % 9 == 0)
            Dust(Player.Feet, -MathF.Sign(Player.Velocity.X), 1);
        if ((e & PlayerEvents.DroppedThrough) != 0) Sfx("drop", 1, 0.4f, Player.Center);
        if ((e & PlayerEvents.HeadBump) != 0) BumpBlock(Player.BumpedTile.X, Player.BumpedTile.Y);
    }

    /// <summary>Called when Pip takes damage: powered forms shrink one step, Sprout dies.</summary>
    public void Hurt(string cause = "hit")
    {
        if (Phase != StagePhase.Playing || Player.Invulnerable > 0) return;
        if (Player.Form == PipForm.Sprout) { Die(cause); return; }
        Player.Form = Player.Form == PipForm.Ember ? PipForm.Bloom : PipForm.Sprout;
        Session.Form = Player.Form;
        Player.Invulnerable = Tuning.HurtInvulnerability;
        Sfx("powerdown", 1, 0.9f, Player.Center);
        Shake(0.35f);
        Freeze(0.12f);
        Burst(Player.Center, Pal.Blush, 10);
    }

    /// <summary>Why the last attempt ended (for debugging and tests).</summary>
    public string DeathCause { get; private set; } = "";

    public void Die(string cause = "hit")
    {
        if (Phase != StagePhase.Playing) return;
        DeathCause = cause;
        Phase = StagePhase.Dying;
        _phaseTime = 0;
        Player.Velocity = Vector2.Zero;
        Player.Controllable = false;
        Freeze(0.35f);
        Shake(0.4f);
        MusicSink?.Invoke("");
        Sfx("death", 1, 1);
    }

    private void UpdateDying(float dt)
    {
        // Classic hop: pause, pop up, fall through everything off the bottom of the screen.
        if (_phaseTime < 0.3f) return;
        if (Player.Velocity.Y == 0 && _phaseTime < 0.4f) Player.Velocity.Y = -300;
        Player.Velocity.Y = MathF.Min(Player.Velocity.Y + 800 * dt, 320);
        Player.Position += Player.Velocity * dt;
        if (_phaseTime > 2.4f) { Phase = StagePhase.Done; Outcome = StageOutcome.Died; }
    }

    private void StartFlag(GoalPole pole)
    {
        Phase = StagePhase.FlagSlide;
        _phaseTime = 0;
        float height = Math.Clamp((pole.Bottom - Player.Feet.Y) / (pole.Bottom - pole.Top), 0, 1);
        FlagScore = Session.FlagPoints(height);
        Session.AddScore(FlagScore);
        Popup(FlagScore.ToString(System.Globalization.CultureInfo.InvariantCulture), new Vector2(pole.Center.X + 14, Player.Center.Y), Pal.Butter);
        Player.Controllable = false;
        Player.Velocity = Vector2.Zero;
        Player.Position.X = pole.Bounds.Center.X - Player.Width + 1;
        Player.Position.Y = MathF.Max(Player.Position.Y, pole.Top);
        MusicSink?.Invoke("");
        Sfx("flag", 1, 1);
    }

    private void UpdateFlagSlide(float dt)
    {
        var pole = Goal!;
        float bottom = pole.Bottom - Player.Height;
        Player.Position.Y = MathF.Min(bottom, Player.Position.Y + 130 * dt);
        pole.PennantDrop = MathF.Min(pole.Bottom - pole.Top - 26, pole.PennantDrop + 130 * dt);
        if (Player.Position.Y >= bottom && _phaseTime > 0.6f)
        {
            Phase = StagePhase.WalkOff;
            _phaseTime = 0;
            MusicSink?.Invoke("clear");
        }
    }

    private void UpdateWalkOff(float dt)
    {
        Player.Controllable = true;
        var auto = _phaseTime > 0.35f ? new PlayerInput(false, true, false, false, false, false, false) : default;
        Player.Step(dt, auto, Level, _platforms);
        if (_phaseTime > 1.9f)
        {
            Phase = StagePhase.Tally;
            _phaseTime = 0;
            Player.Controllable = false;
        }
    }

    private void UpdateTally(float dt)
    {
        if (_phaseTime < 0.5f) return;
        if (TimeLeft > 0)
        {
            int units = Math.Clamp((int)MathF.Ceiling(TimeLeft), 1, 3);
            TimeLeft = MathF.Max(0, MathF.Floor(TimeLeft) - units);
            int bonus = units * Session.TimeBonusPerUnit;
            TimeBonus += bonus;
            Session.AddScore(bonus);
            if ((int)(_phaseTime * 60) % 3 == 0) Sfx("tick", 1, 0.35f);
            _phaseTime = 0.5f;
            return;
        }
        if (_phaseTime > 2.0f) { Phase = StagePhase.Done; Outcome = StageOutcome.Cleared; }
    }

    private void UpdateVictory(float dt)
    {
        Player.Step(dt, default, Level, _platforms);
        if (_phaseTime > 4.2f) { Phase = StagePhase.Done; Outcome = StageOutcome.Won; }
    }

    public void BossDefeated(Grumbold boss)
    {
        Session.AddScore(boss.Points);
        Popup("5000", boss.Center + new Vector2(0, -30), Pal.Butter);
        MusicSink?.Invoke("");
        var arena = boss.Arena;
        Spawn(new Sunstone(this, new Vector2(arena.Center.X, arena.Top + 20), arena.Bottom - 56));
    }

    public void Freeze(float seconds) => _freeze = MathF.Max(_freeze, seconds);
    public void Shake(float trauma) => Camera.Shake(trauma);

    public void Sfx(string name, float pitch = 1, float volume = 1, Vector2? at = null)
    {
        float pan = at is { } p ? Math.Clamp(0.5f + (p.X - Camera.Center.X) / 480f, 0.15f, 0.85f) : 0.5f;
        SoundSink?.Invoke(name, volume, pitch, pan);
    }

    /// <summary>Visual offset of a tile that was just bumped (for the renderer).</summary>
    public float BumpOffset(int x, int y) => _bumps.TryGetValue((x, y), out var t) ? -MathF.Sin(t / 0.2f * MathF.PI) * 5 : 0;

    private void UpdateBumps(float dt)
    {
        if (_bumps.Count == 0) return;
        foreach (var key in _bumps.Keys.ToList())
        {
            float t = _bumps[key] + dt;
            if (t >= 0.2f) _bumps.Remove(key); else _bumps[key] = t;
        }
    }

    private void SnapCamera()
    {
        Camera.CenterOn(new Vector2(Player.Center.X + 24, Player.Feet.Y - 180 * 0.12f));
    }

    private float _look;

    /// <summary>
    /// Platformer camera: look-ahead in the facing direction; vertically it hugs the ground and only
    /// rises when Pip's head gets close to the top edge, so the floor stays in view during normal jumps.
    /// </summary>
    private void FollowCamera(float dt)
    {
        _look = MathX.Damp(_look, Player.Facing * 26 + Player.Velocity.X * 0.18f, 2.5f, dt);
        float targetY = Player.Bounds.Top - 46;
        float targetX = Player.Center.X + _look - 160;
        var limits = CameraLock ?? Level.Bounds;
        targetX = Math.Clamp(targetX, limits.Left, MathF.Max(limits.Left, limits.Right - Camera.ViewWidth));
        targetY = Math.Clamp(targetY, limits.Top, MathF.Max(limits.Top, limits.Bottom - Camera.ViewHeight));
        Camera.Position = new Vector2(MathX.Damp(Camera.Position.X, targetX, 8, dt), MathX.Damp(Camera.Position.Y, targetY, 5, dt));
        Camera.Clamp();
    }

    /// <summary>Restricts the camera (boss arena); the lock eases in so the view never jumps.</summary>
    public void LockCamera(RectF arena) => CameraLock = arena;
}
