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
        foreach (var e in _items.Where(e => e.Alive && (camera is null || camera.IsVisible(e.Bounds, cullMargin))).OrderBy(e => e.Depth))
            e.Draw(renderer);
    }

    /// <summary>Living entities of type <typeparamref name="TEntity"/>.</summary>
    public IEnumerable<TEntity> OfType<TEntity>() where TEntity : T => _items.OfType<TEntity>().Where(e => e.Alive);
    /// <summary>Living entities whose <see cref="Entity.Bounds"/> overlap <paramref name="area"/> (sharing an edge does not count).</summary>
    public IEnumerable<T> Overlapping(RectF area) => _items.Where(e => e.Alive && e.Bounds.Intersects(area));
    /// <summary>Removes every entity at once, without calling <see cref="Entity.Removed"/>.</summary>
    public void Clear() { _items.Clear(); _added.Clear(); }
    /// <summary>Enumerates the living entities in insertion order (deferred additions excluded until flushed).</summary>
    public IEnumerator<T> GetEnumerator() => _items.Where(e => e.Alive).GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
