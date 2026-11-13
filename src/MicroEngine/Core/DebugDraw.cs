using System.Numerics;
using MicroEngine.Graphics;
using MicroEngine.Physics;
using MicroEngine.Physics.Tiles;
using MicroEngine.World;

namespace MicroEngine.Core;

/// <summary>Where a <see cref="DebugDraw"/> item is placed.</summary>
public enum DebugSpace
{
    /// <summary>World coordinates, under the current scene's <see cref="Scenes.Scene.WorldCamera"/> (canvas pixels when it has none).</summary>
    World,
    /// <summary>Canvas pixels, above the scenes, whatever the camera.</summary>
    Screen,
}

/// <summary>
/// Shapes and text any code can ask for while the game runs, drawn above the scenes at the end of the frame
/// (<see cref="Game.DebugDraw"/>): a ray an enemy casts, the box a move swept, a path, a value next to an entity. Calls
/// do nothing while <see cref="Enabled"/> is off, so they can stay in shipped code.
/// </summary>
/// <remarks>
/// An item drawn with <c>seconds: 0</c> shows for one frame; ask for it every update to keep it. A longer one stays that
/// many seconds of real time. World items follow the camera of the scene on top (<see cref="Scenes.Scene.WorldCamera"/>);
/// screen items are in the canvas's pixels. Lines are one world unit (or one canvas pixel) thick.
/// </remarks>
/// <example>
/// <code>
/// // In Load:
/// DebugDraw.Enabled = true;
/// // Anywhere in an update:
/// game.DebugDraw.Line(enemy.Position, player.Position, Color.Red);
/// game.DebugDraw.Physics(world);
/// </code>
/// </example>
public sealed class DebugDraw
{
    private enum Kind : byte { Line, Rect, Circle, Text }

    private readonly record struct Item(Kind Kind, DebugSpace Space, Vector2 A, Vector2 B, float Radius, Color Color, string? Text, float Remaining);

    /// <summary>The colours <see cref="Physics"/> uses.</summary>
    public static class PhysicsColors
    {
        /// <summary>Shapes of static bodies.</summary>
        public static readonly Color Static = new(90, 200, 90);
        /// <summary>Shapes of kinematic bodies.</summary>
        public static readonly Color Kinematic = new(90, 160, 255);
        /// <summary>Shapes of dynamic bodies.</summary>
        public static readonly Color Dynamic = new(255, 210, 60);
        /// <summary>Sensors.</summary>
        public static readonly Color Sensor = new(80, 230, 230, 160);
        /// <summary>One-way shapes and one-way tiles.</summary>
        public static readonly Color OneWay = new(255, 150, 60);
        /// <summary>Contacts that began during the latest step: the normal from the first shape's body.</summary>
        public static readonly Color Contact = new(255, 70, 70);
    }

    private readonly Game _game;
    private readonly List<Item> _items = [];
    private bool _enabled;

    internal DebugDraw(Game game) => _game = game;

    /// <summary>Whether calls are recorded and drawn. Off by default; turning it off drops what is pending.</summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (!value) _items.Clear();
        }
    }

    /// <summary>Items waiting to be drawn (the current frame's and the longer-lived ones).</summary>
    public int Count => _items.Count;

    /// <summary>A line from <paramref name="from"/> to <paramref name="to"/>.</summary>
    public void Line(Vector2 from, Vector2 to, Color color, float seconds = 0, DebugSpace space = DebugSpace.World) =>
        Add(new Item(Kind.Line, space, from, to, 0, color, null, seconds));

    /// <summary>The outline of <paramref name="rect"/>.</summary>
    public void Rect(RectF rect, Color color, float seconds = 0, DebugSpace space = DebugSpace.World) =>
        Add(new Item(Kind.Rect, space, new Vector2(rect.X, rect.Y), new Vector2(rect.Width, rect.Height), 0, color, null, seconds));

    /// <summary>The outline of a circle.</summary>
    public void Circle(Vector2 center, float radius, Color color, float seconds = 0, DebugSpace space = DebugSpace.World) =>
        Add(new Item(Kind.Circle, space, center, default, radius, color, null, seconds));

    /// <summary>A small cross at <paramref name="position"/> (<paramref name="size"/> wide).</summary>
    public void Point(Vector2 position, Color color, float size = 4, float seconds = 0, DebugSpace space = DebugSpace.World)
    {
        if (!_enabled) return;
        float h = size / 2;
        Line(position - new Vector2(h, 0), position + new Vector2(h, 0), color, seconds, space);
        Line(position - new Vector2(0, h), position + new Vector2(0, h), color, seconds, space);
    }

    /// <summary><paramref name="text"/> in the game's font (<see cref="Game.Font"/>), its top-left corner at <paramref name="position"/>.</summary>
    public void Text(Vector2 position, string text, Color color, float seconds = 0, DebugSpace space = DebugSpace.World)
    {
        ArgumentNullException.ThrowIfNull(text);
        Add(new Item(Kind.Text, space, position, default, 0, color, text, seconds));
    }

    /// <summary>
    /// Draws <paramref name="world"/> as it is now: every shape of every body (coloured by body kind; sensors and
    /// one-way shapes in their own colour, <see cref="PhysicsColors"/>), the solid and one-way tiles of tile grids inside
    /// <paramref name="tileArea"/> (by default what the current scene's camera shows, or the canvas), and a normal for
    /// each contact that began during the latest step. Does nothing while <see cref="Enabled"/> is off.
    /// </summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.ShapeInspection"/>.</exception>
    public void Physics(IPhysicsWorld2D world, RectF? tileArea = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!_enabled) return;
        Span<ShapeId> shapes = stackalloc ShapeId[16];
        foreach (var body in world.Bodies)
        {
            int count = world.ShapesOf(body, shapes);
            var list = count <= shapes.Length ? shapes[..count] : new ShapeId[count];
            if (count > shapes.Length) world.ShapesOf(body, list);
            var kind = world.KindOf(body);
            var origin = world.PositionOf(body);
            var rotation = Rotation(world.RotationOf(body));
            foreach (var shape in list)
            {
                var d = world.DescriptionOf(shape);
                if (world is TilePhysicsWorld2D tiles && tiles.GridOf(shape) is { } grid)
                {
                    Tiles(grid, origin, tileArea ?? VisibleArea());
                    continue;
                }
                var color = d.IsSensor ? PhysicsColors.Sensor
                    : d.OneWay ? PhysicsColors.OneWay
                    : kind switch { BodyKind.Static => PhysicsColors.Static, BodyKind.Kinematic => PhysicsColors.Kinematic, _ => PhysicsColors.Dynamic };
                Shape(d, origin, rotation, color);
            }
        }
        foreach (var contact in world.ContactEvents)
        {
            if (contact.Kind != ContactEventKind.Begin || !world.IsAlive(contact.A)) continue;
            var from = world.PositionOf(world.BodyOf(contact.A));
            Line(from, from + contact.Normal * 8, PhysicsColors.Contact);
        }
    }

    private static Matrix3x2 Rotation(float radians) => radians == 0 ? Matrix3x2.Identity : Matrix3x2.CreateRotation(radians);

    private void Shape(ShapeDescription d, Vector2 origin, Matrix3x2 rotation, Color color)
    {
        Vector2 At(Vector2 local) => origin + Vector2.Transform(d.Offset + local, rotation);
        switch (d.Kind)
        {
            case ShapeKind.Box:
                var h = d.Size / 2;
                Polygon([At(new(-h.X, -h.Y)), At(new(h.X, -h.Y)), At(new(h.X, h.Y)), At(new(-h.X, h.Y))], color);
                break;
            case ShapeKind.Circle:
                Circle(At(Vector2.Zero), d.Radius, color);
                break;
            case ShapeKind.Capsule:
                float half = MathF.Max(0, d.Size.Y / 2 - d.Radius);
                Vector2 top = At(new(0, -half)), bottom = At(new(0, half));
                Circle(top, d.Radius, color);
                Circle(bottom, d.Radius, color);
                var side = Vector2.Transform(new Vector2(d.Radius, 0), rotation);
                Line(top + side, bottom + side, color);
                Line(top - side, bottom - side, color);
                break;
            default:
                var vertices = d.Vertices.Span;
                for (int i = 0; i < vertices.Length; i++) Line(At(vertices[i]), At(vertices[(i + 1) % vertices.Length]), color);
                break;
        }
    }

    private void Polygon(ReadOnlySpan<Vector2> points, Color color)
    {
        for (int i = 0; i < points.Length; i++) Line(points[i], points[(i + 1) % points.Length], color);
    }

    private void Tiles(ICollisionGrid grid, Vector2 origin, RectF area)
    {
        int size = grid.TileSize;
        int x0 = (int)MathF.Floor((area.X - origin.X) / size), y0 = (int)MathF.Floor((area.Y - origin.Y) / size);
        int x1 = (int)MathF.Ceiling((area.X + area.Width - origin.X) / size), y1 = (int)MathF.Ceiling((area.Y + area.Height - origin.Y) / size);
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
            {
                var flags = grid.FlagsAt(x, y);
                if (flags == TileFlags.None) continue;
                var cell = new RectF(origin.X + x * size, origin.Y + y * size, size, size);
                if ((flags & TileFlags.Solid) != 0) Rect(cell, PhysicsColors.Static);
                else Line(new Vector2(cell.X, cell.Y), new Vector2(cell.X + size, cell.Y), PhysicsColors.OneWay);
            }
    }

    private RectF VisibleArea() =>
        _game.Scenes.Current?.WorldCamera is { } camera ? camera.VisibleBounds : new RectF(0, 0, _game.Width, _game.Height);

    private void Add(in Item item)
    {
        if (!_enabled) return;
        if (!float.IsFinite(item.Remaining) || item.Remaining < 0) throw new ArgumentOutOfRangeException("seconds", item.Remaining, "The duration must be finite and not negative.");
        _items.Add(item);
    }

    /// <summary>Draws the pending items, then drops those whose time is up after <paramref name="elapsed"/> seconds.</summary>
    internal void Flush(IRenderer canvas, Camera2D? camera, float elapsed)
    {
        if (_items.Count == 0) return;
        if (camera is not null) canvas.PushCamera(camera);
        Draw(canvas, DebugSpace.World);
        if (camera is not null) canvas.PopCamera();
        Draw(canvas, DebugSpace.Screen);
        int kept = 0;
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            float remaining = item.Remaining - elapsed;
            if (remaining > 0) _items[kept++] = item with { Remaining = remaining };
        }
        _items.RemoveRange(kept, _items.Count - kept);
    }

    private void Draw(IRenderer canvas, DebugSpace space)
    {
        foreach (var item in _items)
        {
            if (item.Space != space) continue;
            switch (item.Kind)
            {
                case Kind.Line:
                    canvas.DrawLine(item.A, item.B, 1, item.Color);
                    break;
                case Kind.Rect:
                    Vector2 p = item.A, s = item.B;
                    canvas.DrawLine(p, p + new Vector2(s.X, 0), 1, item.Color);
                    canvas.DrawLine(p + new Vector2(s.X, 0), p + s, 1, item.Color);
                    canvas.DrawLine(p + s, p + new Vector2(0, s.Y), 1, item.Color);
                    canvas.DrawLine(p + new Vector2(0, s.Y), p, 1, item.Color);
                    break;
                case Kind.Circle:
                    const int Segments = 24;
                    for (int i = 0; i < Segments; i++)
                    {
                        float a0 = i * MathF.Tau / Segments, a1 = (i + 1) * MathF.Tau / Segments;
                        canvas.DrawLine(item.A + item.Radius * new Vector2(MathF.Cos(a0), MathF.Sin(a0)),
                            item.A + item.Radius * new Vector2(MathF.Cos(a1), MathF.Sin(a1)), 1, item.Color);
                    }
                    break;
                case Kind.Text:
                    _game.Font.Draw(canvas, item.Text!, item.A, item.Color);
                    break;
            }
        }
    }
}
