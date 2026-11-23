using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Physics;
using MicroEngine.Scenes;

namespace PhysicsPlayground;

/// <summary>
/// A character with the feel of a classic platform game, made of the controller's rules alone: a gravity for each
/// phase of a jump, a skid when it turns at speed, a nudge past the corner of a ledge its head would clip, a ceiling
/// that sends it down, a run-up that jumps higher, and a hidden block that only a head coming from below finds.
/// </summary>
public sealed class ClassicScene : Scene
{
    private static readonly Vector2 Size = new(12, 14);
    private readonly List<(Vector2 At, float Left)> _dust = [];
    private ShapeId _hidden, _crate;
    private BodyId _room;

    /// <summary>The character.</summary>
    public CharacterController2D Hero { get; private set; } = null!;
    /// <summary>Whether the hidden block was found.</summary>
    public bool Revealed { get; private set; }
    /// <summary>How many steps the character skidded in.</summary>
    public int SkidSteps { get; private set; }
    /// <summary>How many times its head was nudged past a corner.</summary>
    public int Corrections { get; private set; }
    /// <summary>The hardest landing so far, in pixels per second.</summary>
    public float HardestLanding { get; private set; }

    #region motion
    // Every number of the character's feel, in pixels and seconds.
    private static readonly CharacterMotion Walk = new()
    {
        MoveSpeed = 92, Acceleration = 520, Deceleration = 620, AirControl = 0.8f,
        SkidDeceleration = 1500, SkidSpeed = 30,             // reversing above 30 px/s brakes hard before it turns
        JumpSpeed = 302, RunJumpBonus = 26,                  // a run-up adds up to 26 px/s to the jump
        Gravity = 640,                                       // while it rises with jump held
        JumpReleaseGravityScale = 1900f / 640,               // while it rises with jump released: a tap is a hop
        ApexGravityScale = 380f / 640, ApexSpeed = 48,       // under 48 px/s near the top: the jump floats
        FallGravityScale = 1050f / 640, MaxFallSpeed = 270,  // falling
        CoyoteTime = 0.1f, JumpBuffer = 0.12f,
        CornerCorrection = 4,                                // up to 4 px sideways past a corner
        CeilingReboundSpeed = 20,                            // a bumped head comes down at once
    };
    // Running is another record: the game swaps them, the controller keeps its state.
    private static readonly CharacterMotion Run = Walk with { MoveSpeed = 152, Acceleration = 600 };
    #endregion

    public override void Enter()
    {
        var world = Physics.World;
        Physics.SyncTransforms = false;
        _room = world.CreateBody(default, BodyDescription.Static());
        world.AddShape(_room, ShapeDescription.Box(new Vector2(320, 12), new Vector2(160, 174)));
        world.AddShape(_room, ShapeDescription.Box(new Vector2(8, 180), new Vector2(4, 90)));
        world.AddShape(_room, ShapeDescription.Box(new Vector2(8, 180), new Vector2(316, 90)));
        world.AddShape(_room, ShapeDescription.Box(new Vector2(60, 12), new Vector2(120, 126)));   // a ledge to clip
        _crate = world.AddShape(_room, ShapeDescription.Box(new Vector2(16, 16), new Vector2(250, 160)));

        #region hidden
        // A hidden block is a shape like any other. What makes it hidden is the character's rule for it: it is solid
        // only for a head that comes up from below, until it has been found.
        _hidden = world.AddShape(_room, ShapeDescription.Box(new Vector2(16, 16), new Vector2(210, 118)));
        Hero = Physics.Track(CharacterController2D.Create(world, default, new Vector2(40, 161), Size, Walk));
        Hero.Solid = (hero, shape) =>
        {
            if (shape != _hidden || Revealed) return true;
            float head = hero.Position.Y - Size.Y / 2, under = world.BoundsOf(_hidden).Bottom;
            return hero.Velocity.Y < 0 && head >= under - 1;
        };
        #endregion
    }

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }

        #region step
        Hero.Motion = Input.Down("cancel") ? Run : Walk;
        Hero.Step(dt, new CharacterInput(Input.Axis("left", "right"), Input.Pressed("confirm"), Input.Down("confirm")));

        // What the step did, for the game's effects.
        var feet = Hero.Position + new Vector2(0, Size.Y / 2);
        if (Hero.IsSkidding) { SkidSteps++; _dust.Add((feet, 0.2f)); }
        if ((Hero.Events & CharacterEvents.CornerCorrected) != 0) Corrections++;
        if ((Hero.Events & CharacterEvents.Landed) != 0)
        {
            HardestLanding = MathF.Max(HardestLanding, Hero.LandingSpeed);
            if (Hero.AirTime > 0.3f) _dust.Add((feet, 0.35f));
        }
        if ((Hero.Events & CharacterEvents.HitCeiling) != 0)
            foreach (var contact in Hero.LastMove.Contacts)
                if (contact.Shape == _hidden) Revealed = true;                    // the head found the block
        #endregion

        for (int i = _dust.Count - 1; i >= 0; i--)
        {
            _dust[i] = (_dust[i].At, _dust[i].Left - dt);
            if (_dust[i].Left <= 0) _dust.RemoveAt(i);
        }
    }

    /// <summary>The scene's own state (the world and the character hash themselves).</summary>
    public override void HashState(StateHasher hash)
    {
        hash.Add(Revealed);
        hash.Add(SkidSteps);
        hash.Add(Corrections);
        hash.Add(HardestLanding);
        hash.Add(_dust.Count);
    }

    public override void Draw(IRenderer r)
    {
        var world = Physics.World;
        Span<ShapeId> shapes = stackalloc ShapeId[8];
        int count = world.ShapesOf(_room, shapes);
        for (int i = 0; i < count; i++)
        {
            if (shapes[i] == _hidden && !Revealed) continue;
            r.FillRect(world.BoundsOf(shapes[i]), shapes[i] == _hidden ? Color.Hex("ffcd75") : shapes[i] == _crate ? Color.Hex("94b0c2") : Color.Hex("566c86"));
        }
        foreach (var (at, left) in _dust) r.FillCircle(at, 1 + left * 10, Color.Hex("f4f4f4"));
        var hero = Hero.Position;
        r.FillRect(new RectF(hero.X - Size.X / 2, hero.Y - Size.Y / 2, Size.X, Size.Y), Hero.IsSkidding ? Color.Hex("ef7d57") : Color.Hex("41a6f6"));
        Font.Draw(r, "MOVE: LEFT/RIGHT   JUMP: Z   RUN: X", new Vector2(14, 12), Color.White);
        Font.Draw(r, Revealed ? "YOU FOUND THE BLOCK" : "SOMETHING IS HIDDEN OVER THE GAP", new Vector2(14, 22), Color.Hex("94b0c2"));
    }
}
