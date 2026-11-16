using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>A meadow of three thousand fireflies, five screens wide and five high: a spatial index finds the ones near
/// the view (only those are moved and drawn) and the ones near the lantern (those light up), without walking the
/// world.</summary>
public sealed class NearbyExample : Scene
{
    #region setup
    private sealed class Firefly
    {
        public SpatialHandle Place;   // where the index has it
        public Vector2 Velocity;
        public bool Lit;
    }

    private static readonly RectF Meadow = new(0, 0, 1600, 900);
    private readonly SpatialIndex2D<EntityId> _index = new(cellSize: 64);   // about the size of the usual query
    private readonly List<EntityId> _found = [];                              // reused by every query: no garbage
    private readonly Camera2D _camera = new(320, 180) { Bounds = Meadow, FollowSharpness = 8 };
    private Vector2 _lantern = Meadow.Center;

    public override void Enter()
    {
        for (int i = 0; i < 3000; i++)
        {
            var id = Objects.Create();
            var at = new Vector2(Game.Random.NextSingle() * Meadow.Width, Game.Random.NextSingle() * Meadow.Height);
            Objects.Local(id).Position2D = at;
            Objects.Attach(id, new Firefly
            {
                Place = _index.Add(id, at),   // a point; Add(id, rectangle) files something with a size
                Velocity = new Vector2(Game.Random.Next(-20, 21), Game.Random.Next(-20, 21)),
            });
        }
        _camera.CenterOn(_lantern);
    }
    #endregion

    /// <summary>Fireflies in the meadow.</summary>
    public int Total => Objects.Count;
    /// <summary>Fireflies moved in the last update: the ones in the view and the margin around it.</summary>
    public int Awake { get; private set; }
    /// <summary>Fireflies within the lantern's light in the last update.</summary>
    public int Near { get; private set; }
    /// <summary>Fireflies drawn in the last frame.</summary>
    public int Drawn { get; private set; }
    /// <summary>Where the lantern is, in the meadow.</summary>
    public Vector2 Lantern => _lantern;

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        _lantern = Vector2.Clamp(_lantern + Input.Vector("left", "right", "up", "down") * 160 * dt, Meadow.Position, Meadow.Position + Meadow.Size);
        _camera.Follow(_lantern, dt);

        #region region
        // The active region: the view and a margin around it. A firefly outside it keeps its place and is not touched.
        _found.Clear();
        Awake = _index.Query(_camera.VisibleBounds.Inflate(64), _found);
        foreach (var id in _found)
        {
            var firefly = Objects.Get<Firefly>(id);
            ref var local = ref Objects.Local(id);
            var p = local.Position2D + firefly.Velocity * dt;
            if (p.X < Meadow.Left || p.X > Meadow.Right) firefly.Velocity.X = -firefly.Velocity.X;
            if (p.Y < Meadow.Top || p.Y > Meadow.Bottom) firefly.Velocity.Y = -firefly.Velocity.Y;
            local.Position2D = p = Vector2.Clamp(p, Meadow.Position, Meadow.Position + Meadow.Size);
            _index.Move(firefly.Place, p);   // tell the index where it is now
            firefly.Lit = false;
        }
        #endregion

        #region near
        // The ones within 40 pixels of the lantern, whatever the cell they are in.
        _found.Clear();
        Near = _index.QueryCircle(_lantern, 40, _found);
        foreach (var id in _found) Objects.Get<Firefly>(id).Lit = true;
        #endregion
    }

    public override void Draw(IRenderer r)
    {
        #region draw
        r.PushCamera(_camera);
        r.DrawRect(Meadow, Color.Hex("38b764"), 2);
        r.FillCircle(_lantern, 40, Color.Hex("ffcd75").WithAlpha(0.15f));
        // What the view shows: a firefly is 2 pixels wide, so one 2 pixels outside the view still shows.
        Span<EntityId> visible = stackalloc EntityId[512];
        Drawn = Math.Min(visible.Length, _index.Query(_camera.VisibleBounds.Inflate(2), visible));
        foreach (var id in visible[..Drawn])
        {
            bool lit = Objects.Get<Firefly>(id).Lit;
            r.FillCircle(Objects.DrawPosition2D(id), lit ? 2 : 1, lit ? Color.Hex("ffcd75") : Color.Hex("a7f070"));
        }
        r.FillCircle(_lantern, 3, Color.Hex("f4f4f4"));
        r.PopCamera();
        #endregion

        Font.Draw(r, $"{Total} IN THE MEADOW  {Awake} AWAKE  {Drawn} DRAWN  {Near} IN THE LIGHT", new Vector2(4, 168), UiTheme.Default.Text);
        Heading.Draw(r, Font, "NEARBY OBJECTS", "ARROWS: LANTERN");
    }
}
