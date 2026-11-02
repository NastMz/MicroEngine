using System.Numerics;
using CourtyardKey.Model;
using CourtyardKey.Scenes;
using CourtyardKey.Visuals;
using CourtyardKey.World;
using MicroEngine;
using MicroEngine.Graphics;

namespace CourtyardKey.Entities;

public enum WardenState { Intro, Stalk, TellCharge, Charge, Dazed, TellLeap, Leap, TellVolley, Dying }

/// <summary>
/// The Moss Warden. Stone armour turns the sword aside, so the fight is about creating openings:
/// bait its telegraphed charge into a wall, or stun it with the boomerang, then strike the heart.
/// Below half health it enrages: faster stalking and radial ember volleys join the pattern.
/// </summary>
public sealed class Warden : Enemy
{
    public const int MaxHealth = 10;
    private const float ChargeSpeed = 200f;

    private float _stateTime, _boomerangCooldown;
    private Vector2 _chargeDir, _leapFrom, _leapTo;
    private int _patternIndex;

    public Warden(PlayScene world, Vector2 at) : base(world, at, MaxHealth)
    {
        Hitbox = new RectF(-13, -10, 26, 10);
        State = WardenState.Intro;
    }

    public WardenState State { get; private set; }
    public bool Enraged => Health.Current <= MaxHealth / 2;
    public bool Vulnerable => State == WardenState.Dazed;
    public override int ContactDamage => 2;
    public override bool Harmful => State is not (WardenState.Dazed or WardenState.Intro or WardenState.Dying) && Height < 6;
    protected override float KnockbackScale => 0;
    public override bool CountsForClear => false;
    public override RectF Hurtbox => new(Position.X - 14, Position.Y - 32, 28, 32);
    private float Height => State == WardenState.Leap ? MathF.Sin(MathF.Min(1, _stateTime / LeapTime) * MathF.PI) * 40 : 0;
    private float LeapTime => Enraged ? 0.6f : 0.75f;

    private void Enter(WardenState state)
    {
        State = state;
        _stateTime = 0;
    }

    public override void Update(float dt)
    {
        _boomerangCooldown = MathF.Max(0, _boomerangCooldown - dt);
        base.Update(dt);
    }

    protected override void Think(float dt)
    {
        _stateTime += dt;
        switch (State)
        {
            case WardenState.Intro:
                if (_stateTime > 1.6f) Enter(WardenState.Stalk);
                break;
            case WardenState.Stalk:
                var toHero = ToHero;
                if (toHero.LengthSquared() > 1) Move(Vector2.Normalize(toHero) * (Enraged ? 34 : 24) * dt);
                if (_stateTime > (Enraged ? 0.9f : 1.4f)) NextAttack();
                break;
            case WardenState.TellCharge:
                if (_stateTime > (Enraged ? 0.5f : 0.7f))
                {
                    _chargeDir = DirExtensions.FromVector(ToHero).ToVector();
                    World.Audio.Play("charge");
                    Enter(WardenState.Charge);
                }
                break;
            case WardenState.Charge:
                var r = Move(_chargeDir * ChargeSpeed * dt);
                if (_stateTime > 0.05f && (r.HitWall || r.HitCeiling || r.HitFloor))
                {
                    World.OnWardenCrash(this);
                    Enter(WardenState.Dazed);
                }
                else if (_stateTime > 1.6f) Enter(WardenState.Stalk);
                break;
            case WardenState.Dazed:
                if (_stateTime > (Enraged ? 1.8f : 2.4f)) Enter(WardenState.Stalk);
                break;
            case WardenState.TellLeap:
                if (_stateTime > 0.45f)
                {
                    _leapFrom = Position;
                    _leapTo = World.Hero.Position;
                    World.Audio.Play("charge", 0.6f, 0.7f);
                    Enter(WardenState.Leap);
                }
                break;
            case WardenState.Leap:
                float t = MathF.Min(1, _stateTime / LeapTime);
                var target = Vector2.Lerp(_leapFrom, _leapTo, Ease.InOutQuad(t));
                Move(target - Position);
                if (t >= 1)
                {
                    World.OnWardenLand(this);
                    Enter(WardenState.Stalk);
                }
                break;
            case WardenState.TellVolley:
                if (_stateTime > 0.6f)
                {
                    int count = 10;
                    float offset = (float)World.Rng.NextDouble() * MathF.Tau;
                    for (int i = 0; i < count; i++)
                    {
                        var dir = MathX.FromAngle(offset + i * MathF.Tau / count);
                        World.Spawn(new Projectile(World, Center + dir * 12, dir * 80, "orb", damage: 1));
                    }
                    World.Audio.Play("spit", 0.9f, 0.6f);
                    Enter(WardenState.Stalk);
                }
                break;
        }
    }

    private void NextAttack()
    {
        // A fixed rotation keeps the fight learnable; enraged adds the volley.
        WardenState[] pattern = Enraged
            ? [WardenState.TellCharge, WardenState.TellVolley, WardenState.TellLeap, WardenState.TellCharge, WardenState.TellVolley]
            : [WardenState.TellCharge, WardenState.TellLeap, WardenState.TellCharge];
        var next = pattern[_patternIndex++ % pattern.Length];
        if (next == WardenState.TellCharge) World.Audio.Play("roar", 0.5f, 1.2f);
        Enter(next);
    }

    public override HitOutcome OnHit(in Hit hit)
    {
        if (State is WardenState.Intro or WardenState.Dying) return HitOutcome.Ignored;
        if (hit.Kind == HitKind.Boomerang)
        {
            if (_boomerangCooldown > 0 || State == WardenState.Leap) return HitOutcome.Ignored;
            _boomerangCooldown = 4f;
            World.Audio.Play("hit", 0.8f, 0.8f);
            Enter(WardenState.Dazed);
            _stateTime = 0.9f; // a boomerang daze is shorter than a wall crash
            return HitOutcome.Stunned;
        }
        if (!Vulnerable)
        {
            World.OnShieldBlock(this, hit);
            return HitOutcome.Blocked;
        }
        bool wasEnraged = Enraged;
        var outcome = base.OnHit(hit);
        if (outcome == HitOutcome.Damaged) World.Audio.Play("boss_hurt");
        if (!wasEnraged && Enraged && outcome == HitOutcome.Damaged)
        {
            World.Audio.Play("roar");
            World.ShakeCamera(0.5f);
            Enter(WardenState.TellVolley);
        }
        return outcome;
    }

    protected override void Die()
    {
        Enter(WardenState.Dying);
        World.OnWardenDefeated(this);
    }

    public void FinishDying()
    {
        World.Puff(Center, big: true);
        Destroy();
    }

    public override void DrawShadow(IRenderer r)
    {
        float scale = 1 - Height / 80f;
        r.DrawPivoted(Art["shadow_l"], DrawPosition, 0, new Vector2(scale));
    }

    public override void Draw(IRenderer r)
    {
        string name = State switch
        {
            WardenState.TellCharge or WardenState.TellLeap or WardenState.TellVolley => "boss_tell",
            WardenState.Dazed or WardenState.Dying => "boss_dazed",
            _ => (Enraged ? "boss_rage" : "boss_idle") + ((int)(Time * 3) % 2),
        };
        var at = DrawPosition + new Vector2(0, -Height);
        if (State is WardenState.TellCharge or WardenState.TellVolley || State == WardenState.Dying) at.X += (int)(Time * 40) % 2 * 2 - 1;
        if (State == WardenState.Intro) at.Y += MathF.Max(0, 1.2f - _stateTime) * 20;
        DrawSprite(r, Flashing(name), at);
        if (State == WardenState.Dazed) DrawStunStars(r, 50);
    }

    public override void EmitLight(LightMap light)
    {
        var core = Position + new Vector2(0, -19 - Height);
        bool hot = Enraged || State is WardenState.TellCharge or WardenState.TellLeap or WardenState.TellVolley;
        light.Add(core, Vulnerable ? 70 : 52, 0.9f, hot ? Pal.Ember[2] : Pal.Spirit[2]);
    }

    /// <summary>Test hook: forces the dazed window.</summary>
    internal void DazeForTest() => Enter(WardenState.Dazed);
}

/// <summary>An expanding ring from the Warden's landing: jump it by being outside, or take a hit.</summary>
public sealed class Shockwave : Actor
{
    private float _radius;
    private readonly float _maxRadius;
    private bool _hit;

    public Shockwave(PlayScene world, Vector2 at, float maxRadius) : base(world)
    {
        Position = at;
        _maxRadius = maxRadius;
        Hitbox = new RectF(-2, -2, 4, 4);
    }

    public override float Depth => Position.Y - 100;

    public override void Update(float dt)
    {
        _radius += 150 * dt;
        if (!_hit)
        {
            // The ring is drawn squashed for perspective, so test against the same ellipse.
            var offset = World.Hero.Position - Position;
            float d = new Vector2(offset.X, offset.Y / 0.55f).Length();
            if (MathF.Abs(d - _radius) < 8 && World.Hero.TakeDamage(1, Position)) _hit = true;
        }
        if (_radius >= _maxRadius) Destroy();
    }

    public override void Draw(IRenderer r)
    {
        float a = 1 - _radius / _maxRadius;
        int segments = 28;
        for (int i = 0; i < segments; i++)
        {
            float t0 = i * MathF.Tau / segments, t1 = (i + 1) * MathF.Tau / segments;
            var p0 = Position + new Vector2(MathF.Cos(t0) * _radius, MathF.Sin(t0) * _radius * 0.55f);
            var p1 = Position + new Vector2(MathF.Cos(t1) * _radius, MathF.Sin(t1) * _radius * 0.55f);
            r.DrawLine(p0, p1, 2, Pal.Cream.WithAlpha(a * 0.9f));
        }
    }
}
