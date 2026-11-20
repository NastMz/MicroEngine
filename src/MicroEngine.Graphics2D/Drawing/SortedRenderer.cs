using System.Numerics;

namespace MicroEngine.Graphics;

/// <summary>
/// Draws in an order that does not depend on the order of the calls: between <see cref="Begin"/> and <see cref="End"/>
/// every draw is kept with the <see cref="Layer"/> and <see cref="Depth"/> of that moment, and <see cref="End"/> sends
/// them to the renderer by layer (lower first), then by depth (smaller first, so a larger depth covers), then in the
/// order they were made. It is a renderer itself, so sprites, fonts, tiles and particles draw into it as into any other,
/// on every platform. Objects standing on the ground cover each other correctly with their feet's Y as the depth.
/// </summary>
/// <remarks>
/// Each draw keeps the camera, blend mode, clip and states it was recorded under, whatever the order it ends up in. A
/// sorted block starts like a frame (no camera, no clip, <see cref="BlendMode.Alpha"/>) except that a camera, a state
/// or a clip the renderer already has stays under it; a block that sets a clip leaves the renderer without one. What
/// has no place in an order is refused: <see cref="Clear"/> and render targets. Keep one instance and reuse it every
/// frame: it keeps its memory.
/// </remarks>
/// <example>
/// <code>
/// var world = _sorted.Begin(renderer);                    // a SortedRenderer the scene keeps
/// foreach (var tree in _trees) { world.Depth = tree.Feet.Y; tree.Draw(world); }
/// world.Depth = _player.Feet.Y; _player.Draw(world);       // behind the trees below it, in front of those above
/// world.At(layer: 1).FillRect(_marker, Color.White);       // a higher layer: over everything in layer 0
/// _sorted.End();
/// </code>
/// </example>
public sealed class SortedRenderer : IRenderer
{
    private enum Kind : byte { Draw, Stretched, Rect, Circle, Triangle, Line }

    private readonly record struct Command(Kind Kind, int Layer, float Depth, int Order, int Context, Texture? Texture,
        RectF A, RectF B, Vector2 P, Vector2 Q, float F, Color Color, DrawOptions Options);

    /// <summary>What a draw was recorded under. A null blend or an untouched clip keeps the renderer's own.</summary>
    private readonly record struct Context(Camera2D? Camera, BlendMode? Blend, bool BlendInState, bool Clipped, RectF? Clip, DrawState[] States)
    {
        /// <summary>Whether the blend mode needs a state of its own around it, so that leaving the context undoes it.</summary>
        public bool Wrapped => Blend is not null && !(BlendInState && States.Length > 0);
    }

    private static readonly Comparison<Command> ByOrder = (a, b) =>
        a.Layer != b.Layer ? a.Layer.CompareTo(b.Layer) : a.Depth.CompareTo(b.Depth) is var d and not 0 ? d : a.Order.CompareTo(b.Order);

    private readonly List<Command> _commands = [];
    private readonly List<Context> _contexts = [];
    private readonly Stack<Camera2D?> _cameras = new();
    private readonly List<DrawState> _states = [];
    private readonly Stack<(BlendMode? Blend, bool InState)> _blends = new();
    private IRenderer? _target;
    private Camera2D? _camera;
    private BlendMode? _blend;
    private RectF? _clip;
    private bool _clipped, _contextChanged, _blendInState;
    private DrawState[] _snapshot = [];

    /// <summary>The layer of the draws made from now on: lower layers are drawn first, whatever their depth. 0 at <see cref="Begin"/>.</summary>
    public int Layer { get; set; }

    /// <summary>
    /// The depth of the draws made from now on, within their layer: a draw with a larger depth is drawn later and covers
    /// those with a smaller one (the Y of an object's feet, for objects on the ground). 0 at <see cref="Begin"/>.
    /// A depth that is not a number is refused.
    /// </summary>
    /// <exception cref="ArgumentException">The value is NaN.</exception>
    public float Depth
    {
        get => _depth;
        set => _depth = !float.IsNaN(value) ? value : throw new ArgumentException("A depth must be a number.", nameof(value));
    }
    private float _depth;

    /// <summary>Draws recorded since <see cref="Begin"/> and not yet sent.</summary>
    public int Count => _commands.Count;

    /// <summary>Whether a block is open (between <see cref="Begin"/> and <see cref="End"/>).</summary>
    public bool IsOpen => _target is not null;

    /// <summary>Sets <see cref="Layer"/> and <see cref="Depth"/> for the draws made from now on and returns this renderer.</summary>
    public SortedRenderer At(int layer, float depth = 0)
    {
        (Layer, Depth) = (layer, depth);
        return this;
    }

    /// <summary>
    /// Starts a sorted block over <paramref name="renderer"/>: draws made into this renderer are kept until
    /// <see cref="End"/>. Layer and depth start at 0.
    /// </summary>
    /// <returns>This renderer, to draw into.</returns>
    /// <exception cref="InvalidOperationException">A block is already open.</exception>
    public SortedRenderer Begin(IRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        if (_target is not null) throw new InvalidOperationException("Begin called twice without End.");
        if (ReferenceEquals(renderer, this)) throw new ArgumentException("A sorted renderer cannot sort into itself.", nameof(renderer));
        Reset();
        _target = renderer;
        return this;
    }

    /// <summary>
    /// Sends the recorded draws to the renderer, by layer, then depth, then the order they were made, and closes the
    /// block. A block that left a camera or a state pushed draws nothing and is closed.
    /// </summary>
    /// <exception cref="InvalidOperationException">No block is open, or a camera or a state pushed in it was not popped.</exception>
    public void End()
    {
        var target = _target ?? throw new InvalidOperationException("End without Begin.");
        _target = null;
        try
        {
            if (_states.Count != 0) throw new InvalidOperationException("Unbalanced PushState/PopState in the sorted block.");
            if (_cameras.Count != 0) throw new InvalidOperationException("Unbalanced PushCamera/PopCamera in the sorted block.");
            _commands.Sort(ByOrder);
            int applied = -1;
            bool clipped = false;
            try
            {
                foreach (var command in _commands)
                {
                    if (command.Context != applied)
                    {
                        if (applied >= 0) Leave(target, _contexts[applied]);
                        applied = -1;
                        var context = _contexts[command.Context];
                        clipped |= context.Clipped;
                        Enter(target, context, clipped);
                        applied = command.Context;
                    }
                    Send(target, command);
                }
            }
            finally
            {
                if (applied >= 0) Leave(target, _contexts[applied]);
                if (clipped) target.SetClip(null);
            }
        }
        finally { Reset(); }
    }

    private void Reset()
    {
        _commands.Clear();
        _contexts.Clear();
        _cameras.Clear();
        _states.Clear();
        _blends.Clear();
        (_camera, _blend, _blendInState, _clip, _clipped, _contextChanged, _snapshot) = (null, null, false, null, false, true, []);
        (Layer, _depth) = (0, 0);
    }

    private static void Enter(IRenderer target, in Context context, bool clipped)
    {
        // A blend mode set under a state is undone by that state's pop, so the renderer's own blending comes back.
        if (context.Wrapped)
        {
            target.PushState(default);
            target.SetBlendMode(context.Blend!.Value);
        }
        if (clipped) target.SetClip(context.Clipped ? context.Clip : null);
        if (context.Camera is { } camera) target.PushCamera(camera);
        foreach (var state in context.States) target.PushState(state);
        // A blend mode set after the last state was pushed applies over it, and that state's pop undoes it.
        if (context.Blend is { } blend && !context.Wrapped) target.SetBlendMode(blend);
    }

    private static void Leave(IRenderer target, in Context context)
    {
        for (int i = 0; i < context.States.Length; i++) target.PopState();
        if (context.Camera is not null) target.PopCamera();
        if (context.Wrapped) target.PopState();
    }

    private static void Send(IRenderer target, in Command c)
    {
        switch (c.Kind)
        {
            case Kind.Draw: target.Draw(c.Texture!, c.A, c.P, c.Color, c.Options); break;
            case Kind.Stretched: target.DrawStretched(c.Texture!, c.A, c.B, c.Color); break;
            case Kind.Rect: target.FillRect(c.A, c.Color); break;
            case Kind.Circle: target.FillCircle(c.P, c.F, c.Color); break;
            case Kind.Triangle: target.FillTriangle(c.P, c.Q, new Vector2(c.B.X, c.B.Y), c.Color); break;
            default: target.DrawLine(c.P, c.Q, c.F, c.Color); break;
        }
    }

    private IRenderer Open => _target ?? throw new InvalidOperationException("Call Begin first: a sorted renderer draws between Begin and End.");

    private void Record(Kind kind, Texture? texture, RectF a, RectF b, Vector2 p, Vector2 q, float f, Color color, in DrawOptions options = default)
    {
        _ = Open;
        if (_contextChanged)
        {
            _contexts.Add(new Context(_camera, _blend, _blendInState, _clipped, _clip, _snapshot));
            _contextChanged = false;
        }
        _commands.Add(new Command(kind, Layer, _depth, _commands.Count, _contexts.Count - 1, texture, a, b, p, q, f, color, options));
    }

    /// <summary>The renderer's width, in the units draws are made in.</summary>
    /// <exception cref="InvalidOperationException">No block is open.</exception>
    public int Width => Open.Width;
    /// <summary>The renderer's height, in the units draws are made in.</summary>
    /// <exception cref="InvalidOperationException">No block is open.</exception>
    public int Height => Open.Height;

    /// <summary>Refused: a clear has no place in an order. Clear the renderer before <see cref="Begin"/>.</summary>
    /// <exception cref="InvalidOperationException">Always.</exception>
    public void Clear(Color color) => throw new InvalidOperationException("A sorted block cannot clear: clear the renderer before Begin.");

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException"><paramref name="texture"/> was disposed; nothing is recorded.</exception>
    public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        Record(Kind.Draw, texture, source, default, position, default, 0, tint, options);
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException"><paramref name="texture"/> was disposed; nothing is recorded.</exception>
    public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        Record(Kind.Stretched, texture, source, destination, default, default, 0, tint);
    }

    /// <inheritdoc/>
    public void FillRect(RectF rect, Color color) => Record(Kind.Rect, null, rect, default, default, default, 0, color);
    /// <inheritdoc/>
    public void FillCircle(Vector2 center, float radius, Color color) => Record(Kind.Circle, null, default, default, center, default, radius, color);
    /// <inheritdoc/>
    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color) => Record(Kind.Triangle, null, default, new RectF(c.X, c.Y, 0, 0), a, b, 0, color);
    /// <inheritdoc/>
    public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color) => Record(Kind.Line, null, default, default, from, to, thickness, color);

    /// <inheritdoc/>
    public void PushCamera(Camera2D camera)
    {
        ArgumentNullException.ThrowIfNull(camera);
        _ = Open;
        _cameras.Push(_camera);
        (_camera, _contextChanged) = (camera, true);
    }

    /// <inheritdoc/>
    public void PopCamera()
    {
        _ = Open;
        if (_cameras.Count == 0) throw new InvalidOperationException("PopCamera without PushCamera.");
        (_camera, _contextChanged) = (_cameras.Pop(), true);
    }

    /// <inheritdoc/>
    public void SetBlendMode(BlendMode mode)
    {
        _ = Open;
        (_blend, _blendInState, _contextChanged) = (mode, true, true);
    }

    /// <summary>
    /// Restricts the draws made from now on to a rectangle of the renderer (see <see cref="IRenderer.SetClip"/>); null
    /// removes it. A block that sets a clip replaces the one the renderer had and leaves it without one.
    /// </summary>
    public void SetClip(RectF? screenRect)
    {
        _ = Open;
        (_clip, _clipped, _contextChanged) = (screenRect, true, true);
    }

    /// <inheritdoc/>
    public void PushState(in DrawState state)
    {
        _ = Open;
        _states.Add(state);
        _blends.Push((_blend, _blendInState));
        (_blendInState, _snapshot, _contextChanged) = (false, [.. _states], true);
    }

    /// <inheritdoc/>
    public void PopState()
    {
        _ = Open;
        if (_states.Count == 0) throw new InvalidOperationException("PopState without PushState.");
        _states.RemoveAt(_states.Count - 1);
        // A blend mode set inside the state ends with it, as on every renderer.
        (_blend, _blendInState) = _blends.Pop();
        (_snapshot, _contextChanged) = ([.. _states], true);
    }

    /// <summary>Refused: draw into a render target with the renderer itself, outside the sorted block.</summary>
    /// <exception cref="InvalidOperationException">Always.</exception>
    public void PushTarget(RenderTarget2D target) => throw new InvalidOperationException("A sorted block cannot change the target: draw into render targets with the renderer itself, before Begin or after End.");

    /// <summary>Refused: see <see cref="PushTarget"/>.</summary>
    /// <exception cref="InvalidOperationException">Always.</exception>
    public void PopTarget() => throw new InvalidOperationException("A sorted block has no target to pop.");
}
