using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Physics;
using MicroEngine.Scenes;

namespace PhysicsPlayground;

/// <summary>
/// Dynamic bodies and joints on the scene's physics world: a chain of links pinned to the ceiling, a lantern on a rope,
/// a cart on two sprung, motor-driven wheels that the player drives over a ramp, and crates to push. The scene never
/// names a backend: the game chose Box2D in one line (<see cref="PlaygroundGame"/>).
/// </summary>
public sealed class PlaygroundScene : Scene
{
    private const float MotorSpeed = 10;             // radians per second at full throttle
    private readonly List<JointId> _wheels = [];
    private readonly List<JointId> _lines = [];      // joints drawn as a line between their anchors

    /// <summary>The cart's chassis.</summary>
    public BodyId Chassis { get; private set; }
    /// <summary>The last link of the chain.</summary>
    public BodyId ChainEnd { get; private set; }
    /// <summary>The lantern that hangs from the rope.</summary>
    public BodyId Lantern { get; private set; }
    /// <summary>The rope's joint.</summary>
    public JointId Rope { get; private set; }

    public override void Enter()
    {
        var world = Physics.World;
        Physics.SyncTransforms = false;              // this scene draws bodies straight from the physics world

        // The room: a floor, two walls, a ceiling and a ramp, all shapes of one static body.
        var room = world.CreateBody(default, BodyDescription.Static());
        world.AddShape(room, ShapeDescription.Box(new Vector2(320, 12), new Vector2(160, 174)));
        world.AddShape(room, ShapeDescription.Box(new Vector2(8, 180), new Vector2(4, 90)));
        world.AddShape(room, ShapeDescription.Box(new Vector2(8, 180), new Vector2(316, 90)));
        world.AddShape(room, ShapeDescription.Box(new Vector2(320, 8), new Vector2(160, 4)));
        world.AddShape(room, ShapeDescription.Polygon(new[] { new Vector2(200, 168), new Vector2(250, 168), new Vector2(250, 150) }));
        world.AddShape(room, ShapeDescription.Polygon(new[] { new Vector2(250, 168), new Vector2(300, 168), new Vector2(250, 150) }));

        #region chain
        // A chain: each link is pinned to the one before it; the first hangs from the ceiling.
        var previous = room;
        for (int i = 0; i < 8; i++)
        {
            var link = world.CreateBody(default, BodyDescription.Dynamic(new Vector2(60, 13 + i * 10)));
            world.AddShape(link, ShapeDescription.Box(new Vector2(3, 10)) with { Density = 2 });
            world.CreateJoint(JointDescription.Revolute(previous, link, new Vector2(60, 8 + i * 10)));
            previous = link;
        }
        ChainEnd = previous;
        #endregion

        #region rope
        // A rope: slack until the lantern is 40 pixels from the hook, taut beyond.
        Lantern = world.CreateBody(default, BodyDescription.Dynamic(new Vector2(130, 20)));
        world.AddShape(Lantern, ShapeDescription.Circle(5));
        Rope = world.CreateJoint(JointDescription.Rope(room, Lantern, new Vector2(110, 8), new Vector2(130, 20), 40));
        _lines.Add(Rope);
        #endregion

        #region cart
        // A cart: a chassis on two wheels, each on a wheel joint whose spring is the suspension and whose motor drives it.
        Chassis = world.CreateBody(default, BodyDescription.Dynamic(new Vector2(60, 152)));
        world.AddShape(Chassis, ShapeDescription.Box(new Vector2(36, 8)));
        foreach (float x in (ReadOnlySpan<float>)[46, 74])
        {
            var wheel = world.CreateBody(default, BodyDescription.Dynamic(new Vector2(x, 161)));
            world.AddShape(wheel, ShapeDescription.Circle(6) with { Friction = 1.5f });
            _wheels.Add(world.CreateJoint(JointDescription.Wheel(Chassis, wheel, new Vector2(x, 161), world.Up) with
            {
                EnableSpring = true, Hertz = 6, DampingRatio = 0.7f,
                EnableMotor = true, MotorSpeed = 0, MaxMotorForce = 9000,
            }));
        }
        #endregion

        // Crates to run into.
        for (int i = 0; i < 3; i++)
        {
            var crate = world.CreateBody(default, BodyDescription.Dynamic(new Vector2(150, 160 - i * 12)));
            world.AddShape(crate, ShapeDescription.Box(new Vector2(12, 12)) with { Density = 0.5f });
        }
    }

    #region drive
    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Quit(); return; }
        var world = Physics.World;

        // Throttle: the wheels' motors aim for a speed; the joints do the rest.
        float throttle = Input.Axis("left", "right");
        foreach (var wheel in _wheels) world.SetJointMotor(wheel, true, throttle * MotorSpeed, 9000);

        // A hop: an impulse through the centre of mass changes the velocity at once.
        if (Input.Pressed("confirm")) world.ApplyImpulse(Chassis, world.Up * world.MassOf(Chassis) * 140);
    }
    #endregion

    public override void Draw(IRenderer r)
    {
        var world = Physics.World;
        Span<ShapeId> shapes = stackalloc ShapeId[8];
        foreach (var body in world.Bodies)
        {
            var color = world.KindOf(body) == BodyKind.Static ? Color.Hex("566c86") : body == Chassis ? Color.Hex("41a6f6") : Color.Hex("ffcd75");
            var at = world.PositionOf(body);
            var turn = Matrix3x2.CreateRotation(world.RotationOf(body));
            int count = world.ShapesOf(body, shapes);
            for (int i = 0; i < count; i++)
            {
                var d = world.DescriptionOf(shapes[i]);
                Vector2 Point(Vector2 local) => at + Vector2.Transform(d.Offset + local, turn);
                switch (d.Kind)
                {
                    case ShapeKind.Circle:
                        r.FillCircle(Point(default), d.Radius, color);
                        r.DrawLine(Point(default), Point(new Vector2(d.Radius, 0)), 1, Color.Hex("1a1c2c"));
                        break;
                    case ShapeKind.Polygon:
                        var v = d.Vertices.Span;
                        for (int k = 1; k + 1 < v.Length; k++) r.FillTriangle(Point(v[0]), Point(v[k]), Point(v[k + 1]), color);
                        break;
                    default:
                        var h = d.Size / 2;
                        r.FillTriangle(Point(new Vector2(-h.X, -h.Y)), Point(new Vector2(h.X, -h.Y)), Point(new Vector2(h.X, h.Y)), color);
                        r.FillTriangle(Point(new Vector2(-h.X, -h.Y)), Point(new Vector2(h.X, h.Y)), Point(new Vector2(-h.X, h.Y)), color);
                        break;
                }
            }
        }
        foreach (var joint in _lines)
            if (world.IsAlive(joint)) r.DrawLine(world.AnchorAOf(joint), world.AnchorBOf(joint), 1, Color.Hex("f4f4f4"));
        Font.Draw(r, "DRIVE: LEFT/RIGHT   HOP: Z", new Vector2(14, 12), Color.White);
    }
}
