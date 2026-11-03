using System.Numerics;
using CourtyardKey.Content;
using CourtyardKey.Model;
using CourtyardKey.Scenes;
using CourtyardKey.Visuals;
using CourtyardKey.World;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;

namespace CourtyardKey.Entities;

public enum PickupKind { Heart, Gem, BlueGem, RedGem, HeartContainer }

/// <summary>A drop: pops out of its source, bobs, blinks before vanishing. The boomerang can fetch it.</summary>
public sealed class Pickup : Actor
{
    private float _age;
    private readonly Vector2 _popVelocity;
    private float _pop;

    public Pickup(PlayScene world, Vector2 at, PickupKind kind, bool pop = true, bool persistent = false) : base(world)
    {
        Position = at;
        Kind = kind;
        Persistent = persistent || kind == PickupKind.HeartContainer;
        Hitbox = new RectF(-4, -7, 8, 7);
        if (pop)
        {
            _pop = 0.001f;
            _popVelocity = new Vector2(((float)world.Rng.NextDouble() - 0.5f) * 40, 0);
        }
    }

    public PickupKind Kind { get; }
    public bool Persistent { get; }
    public bool Carried { get; set; }
    /// <summary>Progress flag set when collected, so placed treasure never respawns.</summary>
    public string? Flag { get; init; }
    public bool Collectable => _pop <= 0;
    private float Lift => _pop > 0 ? MathF.Sin(MathF.Min(1, _pop / 0.35f) * MathF.PI) * 10 : 0;

    /// <summary>State hash: kind, age (it expires), the pop in progress, whether it is persistent or carried, its flag.</summary>
    protected override void HashOwnState(StateHasher hash)
    {
        hash.Add((int)Kind);
        hash.Add(_age);
        hash.Add(_pop);
        hash.Add(_popVelocity);
        hash.Add(Persistent);
        hash.Add(Carried);
        hash.Add(Flag);
    }

    public override void Update(float dt)
    {
        _age += dt;
        if (_pop > 0)
        {
            _pop += dt;
            Move(_popVelocity * dt);
            if (_pop >= 0.35f) _pop = 0;
        }
        if (!Persistent && !Carried && _age > 9f) Destroy();
    }

    public override void DrawShadow(IRenderer r)
    {
        if (!Carried) r.Draw(Art["shadow_s"], DrawPosition);
    }

    public override void Draw(IRenderer r)
    {
        if (!Persistent && _age > 7f && (int)(_age * 10) % 2 == 0) return;
        string sprite = Kind switch
        {
            PickupKind.Heart => "heart",
            PickupKind.Gem => "gem_green",
            PickupKind.BlueGem => "gem_blue",
            PickupKind.RedGem => "gem_red",
            _ => "heart_container",
        };
        float bob = Carried ? 0 : MathF.Round(MathX.Wave(_age, 1.2f, 1));
        var s = Art[sprite];
        r.Draw(s, DrawPosition + new Vector2(0, -s.Height / 2f - 1 - Lift + bob));
    }

    public override void EmitLight(LightMap light) => light.Add(Center, 16, 0.4f);
}

/// <summary>The wind boomerang: flies out, returns to the hero, stuns enemies, triggers switches and fetches drops.</summary>
public sealed class Boomerang : Actor
{
    private const float OutTime = 0.34f, OutSpeed = 200f, ReturnSpeed = 230f;
    private Vector2 _velocity;
    private float _time, _soundTimer;
    private readonly HashSet<object> _hit = [];
    private readonly List<Pickup> _carried = [];

    public Boomerang(PlayScene world, Vector2 at, Vector2 direction) : base(world)
    {
        Position = at;
        _velocity = Vector2.Normalize(direction) * OutSpeed;
        Hitbox = new RectF(-5, -5, 10, 10);
    }

    public bool Returning { get; private set; }
    public override float Depth => Position.Y + 8;

    /// <summary>
    /// State hash: velocity, flight and sound timers, whether it returns, and how many targets it struck and drops it
    /// carries (the carried pickups hash themselves in the room's actor list).
    /// </summary>
    protected override void HashOwnState(StateHasher hash)
    {
        hash.Add(_velocity);
        hash.Add(_time);
        hash.Add(_soundTimer);
        hash.Add(Returning);
        hash.Add(_hit.Count);
        hash.Add(_carried.Count);
    }

    public override void Update(float dt)
    {
        _time += dt;
        if ((_soundTimer -= dt) <= 0) { _soundTimer = 0.13f; World.Audio.Play("boomerang", 0.5f, Returning ? 0.9f : 1.1f); }
        if (!Returning)
        {
            var r = Move(_velocity * dt, flying: true, throughActors: true);
            if (r.HitWall || r.HitCeiling || r.HitFloor)
            {
                World.Burst(Position, Fx.HitSpark, 3, additive: true);
                World.Audio.Play("shield", 0.4f, 1.4f);
                Returning = true;
            }
            if (_time >= OutTime) Returning = true;
        }
        else
        {
            var to = World.Hero.Center - Position;
            float dist = to.Length();
            if (dist < 8)
            {
                foreach (var p in _carried) if (p.Alive) World.Collect(p);
                Destroy();
                return;
            }
            Position += to / dist * MathF.Min(dist, ReturnSpeed * dt);
        }
        foreach (var p in _carried) p.Position = Position + new Vector2(0, 6);
        if (World.BoomerangStrike(this)) Returning = true;
        if (World.Rng.NextDouble() < dt * 30) World.Glows.Emit(Position, Fx.SpiritMote, 1);
    }

    /// <summary>Registers a target as struck by this throw; returns false if it was already struck.</summary>
    public bool TryStrike(object target) => _hit.Add(target);

    public void Carry(Pickup pickup)
    {
        if (pickup.Carried || !pickup.Collectable) return;
        pickup.Carried = true;
        _carried.Add(pickup);
        Returning = true;
    }

    public override void Removed()
    {
        foreach (var p in _carried) p.Carried = false;
    }

    public override void Draw(IRenderer r) => r.DrawPivoted(Art["boomerang"], DrawPosition, _time * 22);
    public override void EmitLight(LightMap light) => light.Add(Position, 26, 0.6f, Pal.Spirit[2]);
}

/// <summary>A breakable clay pot. The sword shatters it; it may hide a heart or gems.</summary>
public sealed class Pot : Actor, IHittable
{
    public Pot(PlayScene world, Vector2 at) : base(world)
    {
        Position = at;
        Hitbox = new RectF(-6, -10, 12, 10);
    }

    public override bool IsSolid => true;
    public RectF Hurtbox => Bounds;

    public HitOutcome OnHit(in Hit hit)
    {
        if (hit.Kind != HitKind.Sword) return HitOutcome.Ignored;
        World.Audio.Play("pot", 0.8f, 1f, 0.1f);
        World.Burst(Center, Fx.Shards, 10);
        World.DropLoot(Center, 0.55f);
        Destroy();
        return HitOutcome.Destroyed;
    }

    public override void Draw(IRenderer r) => DrawSprite(r, Art["pot"], Position);
}

/// <summary>A treasure chest. Hidden chests appear when their room's puzzle or ambush is solved.</summary>
public sealed class Chest : Actor, IInteractable
{
    public Chest(PlayScene world, Vector2 at, int index, ChestDef contents, bool opened) : base(world)
    {
        Position = at;
        Index = index;
        Contents = contents;
        Opened = opened;
        Hitbox = new RectF(-7, -11, 14, 11);
    }

    public int Index { get; }
    public ChestDef Contents { get; }
    public bool Opened { get; set; }
    public override bool IsSolid => true;
    public bool CanInteract => !Opened;

    /// <summary>State hash: which chest of the room it is and whether it is open (its contents follow from the room).</summary>
    protected override void HashOwnState(StateHasher hash)
    {
        hash.Add(Index);
        hash.Add(Opened);
    }
    public void Interact(Hero hero) => World.OpenChest(this);
    public override void Draw(IRenderer r) => DrawSprite(r, Art[Opened ? "chest_open" : "chest"], Position);
}

public sealed class Sign : Actor, IInteractable
{
    public Sign(PlayScene world, Vector2 at, string text) : base(world)
    {
        Position = at;
        Text = text;
        Hitbox = new RectF(-7, -8, 14, 8);
    }

    public string Text { get; }
    public override bool IsSolid => true;
    public bool CanInteract => true;
    public void Interact(Hero hero) => World.ShowText(null, Text);
    public override void Draw(IRenderer r) => DrawSprite(r, Art["sign"], Position);
}

public sealed class Npc : Actor, IInteractable
{
    private readonly float _phase;

    public Npc(PlayScene world, NpcDef def) : base(world)
    {
        Def = def;
        Position = world.Room.TileToWorld(def.TileX, def.TileY);
        Hitbox = new RectF(-6, -9, 12, 9);
        _phase = def.TileX;
        string look = def.Look.ToString().ToLowerInvariant();
        _frames = [$"npc_{look}0", $"npc_{look}1"];
    }

    private readonly string[] _frames;

    public NpcDef Def { get; }
    public override bool IsSolid => true;
    public bool CanInteract => true;
    public bool FlipX { get; set; }

    /// <summary>State hash: which way it turned to face the hero (its definition follows from the room).</summary>
    protected override void HashOwnState(StateHasher hash) => hash.Add(FlipX);

    public void Interact(Hero hero)
    {
        FlipX = hero.Position.X < Position.X;
        World.Talk(this);
    }

    public override void DrawShadow(IRenderer r) => r.Draw(Art["shadow_m"], Position);

    public override void Draw(IRenderer r)
    {
        int f = (int)((World.Age + _phase) * 1.6f) % 2;
        DrawSprite(r, Art[_frames[f]], Position, FlipX);
    }
}

/// <summary>A stone block that slides one tile when the hero leans on it.</summary>
public sealed class PushBlock : Actor
{
    private Vector2 _from, _to;
    private float _slide = -1;
    public const float SlideTime = 0.28f;

    public PushBlock(PlayScene world, int cellX, int cellY) : base(world)
    {
        Cell = (cellX, cellY);
        Position = world.Room.TileToWorld(cellX, cellY + 1);
        Hitbox = new RectF(0, -16, 16, 16);
    }

    public (int X, int Y) Cell { get; private set; }
    public bool Sliding => _slide >= 0;

    /// <summary>State hash: the cell and the slide in progress.</summary>
    protected override void HashOwnState(StateHasher hash)
    {
        hash.Add(Cell.X);
        hash.Add(Cell.Y);
        hash.Add(_from);
        hash.Add(_to);
        hash.Add(_slide);
    }
    public override bool IsSolid => true;
    public override float Depth => Position.Y - 1;

    /// <summary>Starts sliding to the neighbouring cell if it is free. Returns true when it moves.</summary>
    public bool TryPush(Dir dir)
    {
        if (Sliding) return false;
        var (dx, dy) = dir.ToStep();
        var target = (Cell.X + dx, Cell.Y + dy);
        if (!World.CanBlockEnter(target.Item1, target.Item2, this)) return false;
        _from = Position;
        _to = World.Room.TileToWorld(target.Item1, target.Item2 + 1);
        Cell = target;
        _slide = 0;
        World.Audio.Play("push");
        return true;
    }

    public override void Update(float dt)
    {
        if (_slide < 0) return;
        _slide += dt;
        float t = MathF.Min(1, _slide / SlideTime);
        Position = Vector2.Lerp(_from, _to, Ease.InOutQuad(t));
        if (t >= 1)
        {
            _slide = -1;
            World.Burst(Position + new Vector2(8, -2), Fx.Dust, 5);
            World.OnBlockSettled(this);
        }
    }

    public override void Draw(IRenderer r)
    {
        var at = DrawPosition;
        r.Draw(Art["block"], new Vector2(MathF.Round(at.X), MathF.Round(at.Y - 16)));
    }
}

public enum DoorKind { Open, Locked, Shutter, Big }

/// <summary>A two-tile door in a dungeon wall. Locked doors take a small key, the big door takes the boss key.</summary>
public sealed class Door : Actor
{
    private float _anim = 1;
    private readonly string _sprite;

    public Door(PlayScene world, DoorKind kind, Dir side, int cellX, int cellY, bool open) : base(world)
    {
        Kind = kind;
        Side = side;
        CellX = cellX;
        CellY = cellY;
        IsOpen = open;
        bool horizontal = side is Dir.Up or Dir.Down;
        var origin = world.Room.TileToWorld(cellX, cellY);
        Position = origin + new Vector2(0, horizontal ? 16 : 32);
        Hitbox = horizontal ? new RectF(0, -16, 32, 16) : new RectF(0, -32, 16, 32);
        string s = side switch { Dir.Up => "n", Dir.Down => "s", Dir.Left => "w", _ => "e" };
        _sprite = kind switch { DoorKind.Locked => $"door_locked_{s}", DoorKind.Shutter => $"door_shutter_{s}", DoorKind.Big => $"door_bigdoor_{s}", _ => "" };
        _anim = open ? 0 : 1;
    }

    public DoorKind Kind { get; }
    public Dir Side { get; }
    public int CellX { get; }
    public int CellY { get; }
    public bool IsOpen { get; private set; }
    public override bool IsSolid => !IsOpen;

    /// <summary>State hash: kind, side, cell, whether it is open and how far it has opened.</summary>
    protected override void HashOwnState(StateHasher hash)
    {
        hash.Add((int)Kind);
        hash.Add((int)Side);
        hash.Add(CellX);
        hash.Add(CellY);
        hash.Add(IsOpen);
        hash.Add(_anim);
    }
    public override float Depth => Side == Dir.Down ? Position.Y + 100 : -1000;

    public void SetOpen(bool open, bool animate = true)
    {
        if (open == IsOpen) return;
        IsOpen = open;
        if (!animate) _anim = open ? 0 : 1;
    }

    public override void Update(float dt) => _anim = MathX.Approach(_anim, IsOpen ? 0 : 1, dt * 4f);

    public override void Draw(IRenderer r)
    {
        if (Kind == DoorKind.Open || _anim <= 0) return;
        var s = Art[_sprite];
        // Doors retract into the wall (toward the outside of the room) as they open.
        float hidden = 1 - _anim;
        var top = new Vector2(Bounds.X, Bounds.Y);
        var src = s.Source;
        switch (Side)
        {
            case Dir.Up:
                r.Draw(s.Texture, src with { Y = src.Y + src.Height * hidden, Height = src.Height * _anim }, top, Color.White);
                break;
            case Dir.Down:
                r.Draw(s.Texture, src with { Height = src.Height * _anim }, top + new Vector2(0, src.Height * hidden), Color.White);
                break;
            case Dir.Left:
                r.Draw(s.Texture, src with { X = src.X + src.Width * hidden, Width = src.Width * _anim }, top, Color.White);
                break;
            default:
                r.Draw(s.Texture, src with { Width = src.Width * _anim }, top + new Vector2(src.Width * hidden, 0), Color.White);
                break;
        }
    }
}

/// <summary>A crystal switch: strike it (sword or boomerang) to solve its room.</summary>
public sealed class Crystal : Actor, IHittable
{
    private float _pulse;

    public Crystal(PlayScene world, Vector2 at, bool lit) : base(world)
    {
        Position = at;
        Lit = lit;
        Hitbox = new RectF(-6, -14, 12, 14);
    }

    public bool Lit { get; private set; }

    /// <summary>State hash: whether it is lit and its pulse.</summary>
    protected override void HashOwnState(StateHasher hash)
    {
        hash.Add(Lit);
        hash.Add(_pulse);
    }

    public override bool IsSolid => true;
    public override RectF SolidBox => new(Position.X - 6, Position.Y - 6, 12, 6);
    public RectF Hurtbox => Bounds;

    public HitOutcome OnHit(in Hit hit)
    {
        if (Lit) return HitOutcome.Ignored;
        Lit = true;
        _pulse = 1;
        World.Audio.Play("switch");
        World.Burst(Center, Fx.Sparkle, 14, additive: true);
        World.OnCrystalStruck(this);
        return HitOutcome.Damaged;
    }

    public override void Update(float dt) => _pulse = MathF.Max(0, _pulse - dt * 2);
    public override void Draw(IRenderer r) => DrawSprite(r, Art[Lit ? "crystal_gold" : "crystal_blue"], Position + new Vector2(0, MathF.Round(MathX.Wave(World.Age, 0.8f, 1))));
    public override void EmitLight(LightMap light) => light.Add(Center, 30 + _pulse * 30, 0.7f, Lit ? Pal.Gold[2] : Pal.Spirit[2]);
}

/// <summary>The pedestal in the Warden's court where the Courtyard Key rests once the Warden falls.</summary>
public sealed class Pedestal : Actor, IInteractable
{
    private float _rise;

    public Pedestal(PlayScene world, Vector2 at) : base(world)
    {
        Position = at;
        Hitbox = new RectF(-7, -8, 14, 8);
    }

    public bool Revealed { get; private set; }
    public bool HasKey { get; set; } = true;

    /// <summary>State hash: whether it is revealed and holds the key, and how far it has risen (taking the key waits for it).</summary>
    protected override void HashOwnState(StateHasher hash)
    {
        hash.Add(Revealed);
        hash.Add(HasKey);
        hash.Add(_rise);
    }
    public override bool IsSolid => Revealed;
    public bool CanInteract => Revealed && HasKey && _rise >= 1;
    public void Interact(Hero hero) => World.TakeCourtyardKey(this);

    public void Reveal(bool instantly)
    {
        Revealed = true;
        if (instantly) _rise = 1;
    }

    public override void Update(float dt)
    {
        if (Revealed && _rise < 1)
        {
            _rise = MathF.Min(1, _rise + dt * 0.6f);
            if (World.Rng.NextDouble() < 0.5) World.Burst(Position, Fx.Rubble, 1);
        }
        if (Revealed && HasKey && World.Rng.NextDouble() < dt * 12) World.Glows.Emit(Position + new Vector2(0, -22), Fx.Sparkle, 1);
    }

    public override void Draw(IRenderer r)
    {
        if (!Revealed) return;
        float sink = (1 - _rise) * 14;
        var s = Art["pedestal"];
        r.Draw(s.Texture, s.Source with { Height = s.Source.Height - sink }, Position - s.Origin + new Vector2(0, sink), Color.White);
        if (HasKey && _rise >= 1)
            r.Draw(Art["courtyard_key"], Position + new Vector2(0, -22 + MathF.Round(MathX.Wave(World.Age, 0.7f, 2))));
    }

    public override void EmitLight(LightMap light)
    {
        if (Revealed && HasKey) light.Add(Position + new Vector2(0, -20), 60 * MathF.Max(0.3f, _rise), 0.9f, Pal.Gold[2]);
    }
}
