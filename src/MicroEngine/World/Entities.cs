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
    public Vector2 Position { get; set; }
    /// <summary>Hitbox relative to <see cref="Position"/>.</summary>
    public RectF Hitbox { get; set; } = new(0, 0, 16, 16);
    public bool Alive { get; private set; } = true;
    /// <summary>Draw order: lower first. Top-down games usually use the feet Y.</summary>
    public virtual float Depth => 0;
    public RectF Bounds => Hitbox.Offset(Position);
    public Vector2 Center => Bounds.Center;

    public void Destroy() => Alive = false;
    public virtual void Update(float dt) { }
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

    public int Count => _items.Count + _added.Count;

    public TEntity Add<TEntity>(TEntity entity) where TEntity : T
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (_updating) _added.Add(entity); else _items.Add(entity);
        return entity;
    }

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

    public IEnumerable<TEntity> OfType<TEntity>() where TEntity : T => _items.OfType<TEntity>().Where(e => e.Alive);
    public IEnumerable<T> Overlapping(RectF area) => _items.Where(e => e.Alive && e.Bounds.Intersects(area));
    public void Clear() { _items.Clear(); _added.Clear(); }
    public IEnumerator<T> GetEnumerator() => _items.Where(e => e.Alive).GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
