using System.Collections;
using System.Numerics;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace MicroEngine.World;

/// <summary>The message and diagnostic of the transitional adapter.</summary>
internal static class Migration
{
    public const string Message = "Transitional adapter over IWorld, kept only while the demos migrate: keep a scene's objects in Scene.Objects (an IWorld) or in classes of your own.";
    public const string Diagnostic = "MEMIG001";
}

/// <summary>
/// TRANSITIONAL: the legacy game object (a position, a hitbox relative to it, update and draw hooks) as an attachment of
/// an <see cref="IWorld"/> entity. Its position lives in the entity's transform once it joins an
/// <see cref="EntityList{T}"/>; before that, and after it was removed, the object keeps it.
/// </summary>
[Obsolete(Migration.Message, DiagnosticId = Migration.Diagnostic)]
public abstract class Entity : IDestroyedCallback
{
    private Vector2 _position;
    private bool _dead;
    private SceneWorld? _world;
    private IEntityOwner? _owner;

    /// <summary>The entity of the list's world; <see cref="EntityId.None"/> before the object joins a list and after it left.</summary>
    public EntityId Id { get; private set; }
    /// <summary>World position in pixels: the entity's transform while it is in a list.</summary>
    public Vector2 Position
    {
        get => _world is { } world ? world.Local(Id).Position2D : _position;
        set
        {
            if (_world is { } world) world.Local(Id).Position2D = value;
            else _position = value;
        }
    }
    /// <summary>Hitbox relative to <see cref="Position"/>.</summary>
    public RectF Hitbox { get; set; } = new(0, 0, 16, 16);
    /// <summary>False after <see cref="Destroy"/>: the entity stops updating and drawing at once.</summary>
    public bool Alive => _world is { } world ? world.IsAlive(Id) : !_dead;
    /// <summary>Draw order: lower first.</summary>
    public virtual float Depth => 0;
    /// <summary>The hitbox in world pixels.</summary>
    public RectF Bounds => Hitbox.Offset(Position);
    /// <summary>The centre of <see cref="Bounds"/>.</summary>
    public Vector2 Center => Bounds.Center;

    /// <summary>Destroys the entity: it stops updating and drawing at once and leaves its list (calling <see cref="Removed"/>) when the list is flushed.</summary>
    public void Destroy()
    {
        if (_world is { } world) world.Destroy(Id);
        else _dead = true;
    }
    /// <summary>Advances the entity; called by <see cref="EntityList{T}.Update"/> while it is alive.</summary>
    public virtual void Update(float dt) { }
    /// <summary>Draws the entity; called by <see cref="EntityList{T}.Draw"/> while it is alive.</summary>
    public virtual void Draw(IRenderer renderer) { }
    /// <summary>Called once when the world releases the destroyed entity (the list's flush), in destroy order.</summary>
    public virtual void Removed() { }

    internal bool Joined => _owner is not null;

    private int _epoch;

    internal void Join(IEntityOwner owner, SceneWorld world, int epoch)
    {
        var id = world.Create();
        world.Local(id).Position2D = _position;
        world.Attach<Entity>(id, this);
        bool dead = _dead;
        (_owner, _world, Id, _dead, _epoch) = (owner, world, id, false, epoch);
        if (dead) world.Destroy(id);
    }

    void IDestroyedCallback.OnDestroyed(EntityId entity)
    {
        if (_world is not { } world) return;
        _position = world.Local(entity).Position2D;
        var owner = _owner;
        (_owner, _world, Id, _dead) = (null, null, EntityId.None, true);
        owner?.Released(_epoch);
        Removed();
    }
}

internal interface IEntityOwner
{
    void Released(int epoch);
}

/// <summary>
/// TRANSITIONAL: the legacy list of entities, over a world of its own (<see cref="World"/>). Additions are visible at
/// once to every walk that starts afterwards (a walk already running, such as <see cref="Update"/>, does not visit
/// them); destroyed entities are skipped at once and released when the list is flushed (at the end of
/// <see cref="Update"/>, or <see cref="Flush"/>), calling <see cref="Entity.Removed"/> in destroy order.
/// </summary>
[Obsolete(Migration.Message, DiagnosticId = Migration.Diagnostic)]
public sealed class EntityList<T> : IEnumerable<T>, IEntityOwner where T : Entity
{
    private readonly SceneWorld _world = new();
    private readonly List<(float Depth, int Order, T Entity)> _drawn = [];
    private int _count;
    private int _walking;
    private int _epoch;   // each Clear starts a new one: entities added before it no longer count

    /// <summary>The world the list's entities live in.</summary>
    public IWorld World => _world;
    /// <summary>Entities held, destroyed ones included until the list is flushed; 0 right after <see cref="Clear"/>, as before.</summary>
    public int Count => _count;

    /// <summary>Adds an entity, at once; a walk already running does not visit it.</summary>
    /// <exception cref="InvalidOperationException">The entity already belongs to a list.</exception>
    public TEntity Add<TEntity>(TEntity entity) where TEntity : T
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (entity.Joined) throw new InvalidOperationException("The entity already belongs to a list.");
        entity.Join(this, _world, _epoch);
        _count++;
        return entity;
    }

    void IEntityOwner.Released(int epoch)
    {
        if (epoch == _epoch) _count--;
    }

    /// <summary>Updates every living entity in insertion order, then flushes.</summary>
    public void Update(float dt)
    {
        _walking++;
        try
        {
            foreach (var e in _world.All<Entity>())
                if (_world.IsAlive(e.Id)) e.Update(dt);
        }
        finally { _walking--; }
        Flush();
    }

    /// <summary>Releases destroyed entities (calling <see cref="Entity.Removed"/>); waits for the end of a running <see cref="Update"/>.</summary>
    public void Flush()
    {
        if (_walking == 0) _world.EndStep();
    }

    /// <summary>Draws living entities sorted by <see cref="Entity.Depth"/> (stable), skipping those outside <paramref name="camera"/>'s view.</summary>
    public void Draw(IRenderer renderer, Camera2D? camera = null, float cullMargin = 32)
    {
        _drawn.Clear();
        int order = 0;
        foreach (var e in _world.All<Entity>())
        {
            var t = (T)e;
            if (camera is null || camera.IsVisible(t.Bounds, cullMargin)) _drawn.Add((t.Depth, order, t));
            order++;
        }
        _drawn.Sort(ByDepth);
        foreach (var (_, _, e) in _drawn) e.Draw(renderer);
        _drawn.Clear();
    }

    private static readonly Comparison<(float Depth, int Order, T Entity)> ByDepth = static (a, b) =>
        a.Depth.CompareTo(b.Depth) is var c and not 0 ? c : a.Order.CompareTo(b.Order);

    /// <summary>Living entities of type <typeparamref name="TEntity"/>, in insertion order.</summary>
    public TypedEntities<TEntity> OfType<TEntity>() where TEntity : T => new(_world);
    /// <summary>Living entities whose bounds overlap <paramref name="area"/> (sharing an edge does not count), in insertion order.</summary>
    public OverlappingEntities Overlapping(RectF area) => new(_world, area);
    /// <summary>
    /// Destroys every entity: <see cref="Count"/> is 0 and walks skip them at once (also the rest of a running
    /// <see cref="Update"/>); they are released, calling <see cref="Entity.Removed"/>, when the list is flushed.
    /// Entities added afterwards stay.
    /// </summary>
    public void Clear()
    {
        foreach (var e in _world.All<Entity>()) _world.Destroy(e.Id);
        _count = 0;
        _epoch++;
    }

    /// <summary>Walks the living entities in insertion order.</summary>
    public Enumerator GetEnumerator() => new(_world);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>The living entities of one type.</summary>
    public readonly struct TypedEntities<TEntity> : IEnumerable<TEntity> where TEntity : T
    {
        private readonly SceneWorld _world;
        internal TypedEntities(SceneWorld world) => _world = world;
        /// <summary>Starts a walk.</summary>
        public Enumerator GetEnumerator() => new(_world);
        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>Walks the living entities of type <typeparamref name="TEntity"/>.</summary>
        public struct Enumerator : IEnumerator<TEntity>
        {
            private Attachments<Entity>.Enumerator _inner;
            internal Enumerator(SceneWorld world) => (_inner, Current) = (world.All<Entity>().GetEnumerator(), null!);
            /// <summary>The current entity.</summary>
            public TEntity Current { get; private set; }
            readonly object IEnumerator.Current => Current;
            /// <summary>Moves to the next living entity of the type.</summary>
            public bool MoveNext()
            {
                while (_inner.MoveNext())
                    if (_inner.Current is TEntity e) { Current = e; return true; }
                return false;
            }
            /// <summary>Starts over.</summary>
            public void Reset() => _inner.Reset();
            /// <summary>Nothing to release.</summary>
            public readonly void Dispose() { }
        }
    }

    /// <summary>The living entities overlapping an area.</summary>
    public readonly struct OverlappingEntities : IEnumerable<T>
    {
        private readonly SceneWorld _world;
        private readonly RectF _area;
        internal OverlappingEntities(SceneWorld world, RectF area) => (_world, _area) = (world, area);
        /// <summary>Starts a walk.</summary>
        public Enumerator GetEnumerator() => new(_world, _area);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>Walks the living entities overlapping the area.</summary>
        public struct Enumerator : IEnumerator<T>
        {
            private Attachments<Entity>.Enumerator _inner;
            private readonly RectF _area;
            internal Enumerator(SceneWorld world, RectF area) => (_inner, _area, Current) = (world.All<Entity>().GetEnumerator(), area, null!);
            /// <summary>The current entity.</summary>
            public T Current { get; private set; }
            readonly object IEnumerator.Current => Current;
            /// <summary>Moves to the next living entity that overlaps the area.</summary>
            public bool MoveNext()
            {
                while (_inner.MoveNext())
                    if (_inner.Current.Bounds.Intersects(_area)) { Current = (T)_inner.Current; return true; }
                return false;
            }
            /// <summary>Starts over.</summary>
            public void Reset() => _inner.Reset();
            /// <summary>Nothing to release.</summary>
            public readonly void Dispose() { }
        }
    }

    /// <summary>Walks the living entities without allocating.</summary>
    public struct Enumerator : IEnumerator<T>
    {
        private Attachments<Entity>.Enumerator _inner;
        internal Enumerator(SceneWorld world) => (_inner, Current) = (world.All<Entity>().GetEnumerator(), null!);
        /// <summary>The current entity.</summary>
        public T Current { get; private set; }
        readonly object IEnumerator.Current => Current;
        /// <summary>Moves to the next living entity.</summary>
        public bool MoveNext()
        {
            if (!_inner.MoveNext()) return false;
            Current = (T)_inner.Current;
            return true;
        }
        /// <summary>Starts over.</summary>
        public void Reset() => _inner.Reset();
        /// <summary>Nothing to release.</summary>
        public readonly void Dispose() { }
    }
}
