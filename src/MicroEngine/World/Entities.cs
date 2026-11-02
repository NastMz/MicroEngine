using System.Numerics;
using MicroEngine.Graphics;

namespace MicroEngine.World;

/// <summary>
/// A lightweight game object: a position, a hitbox relative to it, and update/draw hooks.
/// MicroEngine deliberately prefers small inheritance-based entities over an ECS: the sample
/// games have dozens of objects, and plain classes keep game code readable and debuggable.
/// </summary>
public abstract class Entity
{
    /// <summary>World position in pixels; the hitbox and drawing are relative to it.</summary>
    public Vector2 Position { get; set; }
    /// <summary>Hitbox relative to <see cref="Position"/>.</summary>
    public RectF Hitbox { get; set; } = new(0, 0, 16, 16);
    /// <summary>False after <see cref="Destroy"/>. Dead entities are skipped by their list and removed at its next flush.</summary>
    public bool Alive { get; private set; } = true;
    /// <summary>Draw order: lower first. Top-down games usually use the feet Y.</summary>
    public virtual float Depth => 0;
    /// <summary>The hitbox in world pixels.</summary>
    public RectF Bounds => Hitbox.Offset(Position);
    /// <summary>The centre of <see cref="Bounds"/>.</summary>
    public Vector2 Center => Bounds.Center;

    /// <summary>Marks the entity dead; it stops updating and drawing and leaves its list (calling <see cref="Removed"/>) at the end of the list's update.</summary>
    public void Destroy() => Alive = false;
    /// <summary>Advances the entity by <paramref name="dt"/> seconds; called by <see cref="EntityList{T}.Update"/> while it is alive.</summary>
    public virtual void Update(float dt) { }
    /// <summary>Draws the entity; called by <see cref="EntityList{T}.Draw"/> while it is alive.</summary>
    public virtual void Draw(IRenderer renderer) { }
    /// <summary>Called once when the entity leaves its list after <see cref="Destroy"/>.</summary>
    public virtual void Removed() { }
}

/// <summary>
/// Owns entities of a kind. Additions and removals made during iteration are deferred to
/// the end of the update, so entities can spawn and destroy each other safely.
/// </summary>
public sealed class EntityList<T> : IEnumerable<T> where T : Entity
{
    private readonly List<T> _items = [];
    private readonly List<T> _added = [];
    private readonly List<(float Depth, int Order, T Entity)> _drawn = [];
    private bool _updating;

    /// <summary>Number of entities held, including dead ones not yet flushed and additions not yet applied.</summary>
    public int Count => _items.Count + _added.Count;

    /// <summary>Adds an entity. During <see cref="Update"/> the addition is deferred to the end of the update, so the new entity first updates in the next one.</summary>
    /// <returns><paramref name="entity"/>, for chaining or keeping a typed reference.</returns>
    public TEntity Add<TEntity>(TEntity entity) where TEntity : T
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (_updating) _added.Add(entity); else _items.Add(entity);
        return entity;
    }

    /// <summary>Updates every living entity in insertion order, then applies deferred additions and removes destroyed entities.</summary>
    public void Update(float dt)
    {
        _updating = true;
        try
        {
            foreach (var e in _items) if (e.Alive) e.Update(dt);
        }
        finally { _updating = false; }
        Flush();
    }

    /// <summary>Applies deferred additions and removes destroyed entities.</summary>
    public void Flush()
    {
        _items.AddRange(_added);
        _added.Clear();
        for (int i = _items.Count - 1; i >= 0; i--)
            if (!_items[i].Alive) { var e = _items[i]; _items.RemoveAt(i); e.Removed(); }
    }

    /// <summary>Draws living entities sorted by <see cref="Entity.Depth"/> (stable for equal depth).</summary>
    public void Draw(IRenderer renderer, Camera2D? camera = null, float cullMargin = 32)
    {
        // Called every frame: the sort buffer is reused, and insertion order breaks ties so the sort stays stable.
        _drawn.Clear();
        for (int i = 0; i < _items.Count; i++)
        {
            var e = _items[i];
            if (e.Alive && (camera is null || camera.IsVisible(e.Bounds, cullMargin))) _drawn.Add((e.Depth, i, e));
        }
        _drawn.Sort(ByDepth);
        foreach (var (_, _, e) in _drawn) e.Draw(renderer);
        _drawn.Clear();
    }

    private static readonly Comparison<(float Depth, int Order, T Entity)> ByDepth = static (a, b) =>
        a.Depth.CompareTo(b.Depth) is var c and not 0 ? c : a.Order.CompareTo(b.Order);

    /// <summary>Living entities of type <typeparamref name="TEntity"/>, in insertion order; <c>foreach</c> over them allocates nothing.</summary>
    public TypedEntities<TEntity> OfType<TEntity>() where TEntity : T => new(_items);
    /// <summary>
    /// Living entities whose <see cref="Entity.Bounds"/> overlap <paramref name="area"/> (sharing an edge does not
    /// count), in insertion order; <c>foreach</c> over them allocates nothing.
    /// </summary>
    public OverlappingEntities Overlapping(RectF area) => new(_items, area);
    /// <summary>Removes every entity at once, without calling <see cref="Entity.Removed"/>.</summary>
    public void Clear() { _items.Clear(); _added.Clear(); }
    /// <summary>
    /// Enumerates the living entities in insertion order (deferred additions excluded until flushed). <c>foreach</c>
    /// over the list allocates nothing.
    /// </summary>
    public Enumerator GetEnumerator() => new(_items);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>The living entities of one type (<see cref="OfType{TEntity}"/>), walked without allocating.</summary>
    public readonly struct TypedEntities<TEntity> : IEnumerable<TEntity> where TEntity : T
    {
        private readonly List<T> _items;
        internal TypedEntities(List<T> items) => _items = items;

        /// <summary>Starts a walk.</summary>
        public Enumerator GetEnumerator() => new(_items);
        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator() => GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>Walks the living entities of type <typeparamref name="TEntity"/>.</summary>
        public struct Enumerator : IEnumerator<TEntity>
        {
            private readonly List<T> _items;
            private int _index;
            internal Enumerator(List<T> items) => (_items, _index, Current) = (items, -1, null!);
            /// <summary>The current entity.</summary>
            public TEntity Current { get; private set; }
            readonly object System.Collections.IEnumerator.Current => Current;
            /// <summary>Moves to the next living entity of the type.</summary>
            public bool MoveNext()
            {
                while (++_index < _items.Count)
                    if (_items[_index] is TEntity { Alive: true } e) { Current = e; return true; }
                return false;
            }
            /// <summary>Starts over.</summary>
            public void Reset() => _index = -1;
            /// <summary>Nothing to release.</summary>
            public readonly void Dispose() { }
        }
    }

    /// <summary>The living entities overlapping an area (<see cref="Overlapping"/>), walked without allocating.</summary>
    public readonly struct OverlappingEntities : IEnumerable<T>
    {
        private readonly List<T> _items;
        private readonly RectF _area;
        internal OverlappingEntities(List<T> items, RectF area) => (_items, _area) = (items, area);

        /// <summary>Starts a walk.</summary>
        public Enumerator GetEnumerator() => new(_items, _area);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>Walks the living entities overlapping the area.</summary>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly List<T> _items;
            private readonly RectF _area;
            private int _index;
            internal Enumerator(List<T> items, RectF area) => (_items, _area, _index, Current) = (items, area, -1, null!);
            /// <summary>The current entity.</summary>
            public T Current { get; private set; }
            readonly object System.Collections.IEnumerator.Current => Current;
            /// <summary>Moves to the next living entity that overlaps the area.</summary>
            public bool MoveNext()
            {
                while (++_index < _items.Count)
                {
                    var e = _items[_index];
                    if (e.Alive && e.Bounds.Intersects(_area)) { Current = e; return true; }
                }
                return false;
            }
            /// <summary>Starts over.</summary>
            public void Reset() => _index = -1;
            /// <summary>Nothing to release.</summary>
            public readonly void Dispose() { }
        }
    }

    /// <summary>Walks the living entities of an <see cref="EntityList{T}"/> without allocating.</summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly List<T> _items;
        private int _index;

        internal Enumerator(List<T> items) => (_items, _index, Current) = (items, -1, null!);

        /// <summary>The current entity.</summary>
        public T Current { get; private set; }
        readonly object System.Collections.IEnumerator.Current => Current;

        /// <summary>Moves to the next living entity.</summary>
        public bool MoveNext()
        {
            while (++_index < _items.Count)
                if (_items[_index].Alive) { Current = _items[_index]; return true; }
            return false;
        }

        /// <summary>Starts over.</summary>
        public void Reset() => _index = -1;
        /// <summary>Nothing to release.</summary>
        public readonly void Dispose() { }
    }
}
