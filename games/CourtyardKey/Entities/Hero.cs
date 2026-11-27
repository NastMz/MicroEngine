using System.Numerics;
using CourtyardKey.Model;
using CourtyardKey.Scenes;
using CourtyardKey.Visuals;
using CourtyardKey.World;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;

namespace CourtyardKey.Entities;

public enum HeroState { Walk, Swing, Throw, Hurt, Pose, Dead }

/// <summary>
/// Wren: walks in eight directions but faces four, swings a sword arc, throws the boomerang,
/// pushes blocks and takes knockback with invulnerability frames.
/// </summary>
public sealed class Hero : Actor
{
    public const float Speed = 84f;
    public const float SwingTime = 0.26f;
    public const float ThrowTime = 0.18f;
    public const float PushDelay = 0.32f;

    private readonly StateMachine<HeroState> _state = new(HeroState.Walk);
    private float _animTime, _knockTime, _dustTimer;
    private Vector2 _knock;
    private readonly HashSet<object> _struck = [];

    public Hero(PlayScene world, Health health) : base(world, inRoom: false)
    {
        Health = health;
        Hitbox = new RectF(-5, -8, 10, 8);
        _state.State(HeroState.Walk).OnUpdate(UpdateWalk);
        // A small lunge sells the swing's weight.
        _state.State(HeroState.Swing).OnEnter(_struck.Clear)
            .OnUpdate(dt => { if (StateTime < 0.08f) Move(Facing.ToVector() * 40 * dt); })
            .After(SwingTime, HeroState.Walk);
        _state.State(HeroState.Throw).After(ThrowTime, HeroState.Walk);
        _state.State(HeroState.Hurt).OnUpdate(UpdateHurt);
    }

    public Health Health { get; }
    public Dir Facing { get; set; } = Dir.Down;
    public HeroState State => _state.Current;
    /// <summary>Seconds in the current state.</summary>
    public float StateTime => _state.Time;
    /// <summary>When false the hero ignores input (cutscenes, transitions).</summary>
    public bool Controllable { get; set; } = true;
    public bool IsMoving { get; private set; }
    /// <summary>Seconds spent walking into an obstacle in the facing direction.</summary>
    public float PushTime { get; private set; }
    /// <summary>The direction the player is holding this update (zero when none).</summary>
    public Vector2 InputDirection { get; private set; }

    /// <summary>Body hurtbox, slightly taller than the feet collision box.</summary>
    public RectF Hurtbox => new(Position.X - 5, Position.Y - 13, 10, 13);

    /// <summary>The sword's damaging area while a swing is active.</summary>
    public RectF? SwordBox
    {
        get
        {
            if (State != HeroState.Swing || StateTime < 0.02f || StateTime > 0.19f) return null;
            var p = Position;
            return Facing switch
            {
                Dir.Down => new RectF(p.X - 8, p.Y - 6, 16, 18),
                Dir.Up => new RectF(p.X - 8, p.Y - 28, 16, 18),
                Dir.Right => new RectF(p.X + 2, p.Y - 16, 17, 16),
                _ => new RectF(p.X - 19, p.Y - 16, 17, 16),
            };
        }
    }

    /// <summary>Area in front of the hero used to find something to talk to or open.</summary>
    public RectF InteractProbe
    {
        get
        {
            var c = Position + new Vector2(0, -4) + Facing.ToVector() * 10;
            return RectF.FromCenter(c, 12, 12);
        }
    }

    /// <summary>
    /// State hash: health, facing, the state machine and the timers, knockback, control, walking and push state, and how many
    /// targets the current swing struck (the targets themselves are object identities, the same set in the same run).
    /// </summary>
    protected override void HashOwnState(StateHasher hash)
    {
        hash.Add(Health);
        hash.Add((int)Facing);
        hash.Add(_state);
        hash.Add(_animTime);
        hash.Add(_knockTime);
        hash.Add(_dustTimer);
        hash.Add(_knock);
        hash.Add(Controllable);
        hash.Add(IsMoving);
        hash.Add(PushTime);
        hash.Add(InputDirection);
        hash.Add(_struck.Count);
    }

    /// <summary>Returns true the first time a swing touches <paramref name="target"/> (one hit per swing).</summary>
    public bool MarkStruck(object target) => _struck.Add(target);

    /// <summary>Enters a state from its beginning (also the one the hero is in).</summary>
    public void SetState(HeroState state) => _state.Go(state);

    public override void Update(float dt)
    {
        Health.Update(dt);
        IsMoving = false;
        InputDirection = Vector2.Zero;
        _state.Update(dt);
    }

    private void UpdateHurt(float dt)
    {
        if (_knockTime <= 0) { SetState(HeroState.Walk); return; }
        _knockTime -= dt;
        Move(_knock * dt);
    }

    private void UpdateWalk(float dt)
    {
        if (!Controllable) { PushTime = 0; return; }
        var input = World.Input;
        // Analog: keys give unit directions (as before), a stick or the touch joystick also gives how far it is pushed.
        var dir = input.Vector("left", "right", "up", "down");
        InputDirection = dir;
        if (input.Pressed("confirm") && World.TryInteract(this)) return;
        if (input.Pressed("confirm") && World.Progress.HasSword)
        {
            SetState(HeroState.Swing);
            World.OnSwordSwing(this);
            return;
        }
        if (input.Pressed("action") && World.Progress.HasBoomerang && World.TryThrowBoomerang(this))
        {
            SetState(HeroState.Throw);
            return;
        }
        if (dir == Vector2.Zero) { PushTime = 0; _animTime = 0; return; }
        UpdateFacing(dir);
        var velocity = dir * Speed;
        var result = Move(velocity * dt);
        IsMoving = true;
        _animTime += dt;
        bool blockedAhead = Facing switch
        {
            Dir.Left => result.HitLeft,
            Dir.Right => result.HitRight,
            Dir.Up => result.HitCeiling,
            _ => result.HitFloor,
        };
        bool pressingFacing = Vector2.Dot(Vector2.Normalize(dir), Facing.ToVector()) > 0.7f;
        PushTime = blockedAhead && pressingFacing ? PushTime + dt : 0;
        if (PushTime > 0) World.OnHeroPushing(this, PushTime);
        if ((_dustTimer -= dt) <= 0)
        {
            _dustTimer = 0.28f;
            World.Particles.Emit(Position + new Vector2(0, -1), Fx.Dust, 1);
        }
    }

    /// <summary>Keeps the current facing while it is still held, so strafing along walls feels stable.</summary>
    private void UpdateFacing(Vector2 dir)
    {
        bool holdingFacing = Vector2.Dot(dir, Facing.ToVector()) > 0;
        if (holdingFacing) return;
        Facing = dir.X != 0 && (dir.Y == 0 || MathF.Abs(dir.X) >= MathF.Abs(dir.Y)) ? (dir.X < 0 ? Dir.Left : Dir.Right) : (dir.Y < 0 ? Dir.Up : Dir.Down);
    }

    /// <summary>Applies damage from <paramref name="source"/>. Returns true when it landed.</summary>
    public bool TakeDamage(int amount, Vector2 source)
    {
        if (State == HeroState.Dead || State == HeroState.Pose) return false;
        if (!Health.TryDamage(amount, Combat.HeroInvulnerability)) return false;
        Knockback(source, Combat.HeroKnockbackSpeed);
        World.OnHeroHurt(this);
        if (Health.IsDead) SetState(HeroState.Dead);
        return true;
    }

    /// <summary>A shove without damage (shield clang, bumping a boss).</summary>
    public void Knockback(Vector2 source, float speed)
    {
        _knock = Combat.KnockbackDirection(source, Center) * speed;
        _knockTime = Combat.KnockbackTime;
        if (State != HeroState.Dead) SetState(HeroState.Hurt);
    }

    public void ResetPush() => PushTime = 0;

    public void Revive(int health)
    {
        Health.SetCurrent(health);
        Health.ClearInvulnerability();
        SetState(HeroState.Walk);
    }

    public override void DrawShadow(IRenderer r) => r.Draw(Art["shadow_s"], DrawPosition);

    public override void Draw(IRenderer r)
    {
        // Invulnerability flicker (skipped while posing so the item-get moment reads clearly).
        if (Health.IsInvulnerable && State != HeroState.Dead && (int)(Health.Invulnerable * 20) % 2 == 0) return;
        if (State == HeroState.Swing) DrawSword(r, behind: Facing == Dir.Up);
        DrawSprite(r, BodySprite(out bool flip), DrawPosition + LungeOffset(), flip);
        if (State == HeroState.Swing) DrawSword(r, behind: false);
    }

    private Vector2 LungeOffset() => State switch
    {
        HeroState.Swing when StateTime < 0.16f => Facing.ToVector() * 2,
        HeroState.Throw => Facing.ToVector() * 1,
        _ => Vector2.Zero,
    };

    private Sprite BodySprite(out bool flip)
    {
        flip = Facing == Dir.Left;
        if (State == HeroState.Pose) return Art["hero_get"];
        if (State == HeroState.Dead)
        {
            if (StateTime > 1.1f) return Art["hero_lie"];
            var spin = (Dir)((int)(StateTime * 12) % 4);
            flip = spin == Dir.Left;
            return Art[spin switch { Dir.Up => "hero_up0", Dir.Down => "hero_down0", _ => "hero_side0" }];
        }
        var frames = Facing switch
        {
            Dir.Down => Art.Frames("hero_down"),
            Dir.Up => Art.Frames("hero_up"),
            _ => Art.Frames("hero_side"),
        };
        if (!IsMoving || State != HeroState.Walk) return frames[State is HeroState.Swing or HeroState.Throw ? 1 : 0];
        int step = (int)(_animTime * 8);
        if (Facing is Dir.Left or Dir.Right) return frames[step % 2 == 0 ? 1 : 0];
        return frames[(step % 4) switch { 0 => 1, 2 => 2, _ => 0 }];
    }

    private void DrawSword(IRenderer r, bool behind)
    {
        if (behind != (Facing == Dir.Up)) return;
        float time = StateTime, t = Math.Clamp(time / 0.16f, 0, 1);
        float eased = 1 - (1 - t) * (1 - t);
        float baseAngle = Facing.Angle();
        float angle = baseAngle - 1.25f + eased * 2.3f;
        var pivot = DrawPosition + new Vector2(0, -7) + Facing.ToVector() * 3;
        if (time < 0.2f)
        {
            var swoosh = Art["swoosh"];
            float alpha = 1 - time / 0.2f;
            r.DrawPivoted(swoosh, pivot, angle - 0.6f, new Vector2(1f), Color.White.WithAlpha(alpha * 0.9f));
        }
        r.DrawPivoted(Art["blade"], pivot, angle);
    }

    public override void EmitLight(LightMap light) => light.Add(Center, 44, 0.85f);
}
