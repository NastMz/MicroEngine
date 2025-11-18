namespace MicroEngine.Core.ECS;

/// <summary>
/// Cached query result for efficient repeated entity lookups.
/// Automatically invalidated when components are added/removed.
/// </summary>
public sealed class CachedQuery
{
    private readonly World _world;
    private readonly Type[] _componentTypes;
    private readonly List<Entity> _cachedEntities;
    private bool _isDirty;

    /// <summary>
    /// Gets the entities matching this query.
    /// </summary>
    public IReadOnlyList<Entity> Entities
    {
        get
        {
            if (_isDirty)
            {
                Refresh();
            }

            return _cachedEntities;
        }
    }

    /// <summary>
    /// Gets whether the cache is currently dirty and needs refresh.
    /// </summary>
    public bool IsDirty => _isDirty;

    internal CachedQuery(World world, params Type[] componentTypes)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _componentTypes = componentTypes ?? throw new ArgumentNullException(nameof(componentTypes));
        _cachedEntities = new List<Entity>();
        _isDirty = true;
    }

    /// <summary>
    /// Marks the cache as dirty, forcing a refresh on next access.
    /// </summary>
    public void Invalidate()
    {
        _isDirty = true;
    }

    /// <summary>
    /// Refreshes the cached entity list immediately.
    /// </summary>
    public void Refresh()
    {
        _cachedEntities.Clear();

        // Get all entities from first component type
        if (_componentTypes.Length == 0)
        {
            _isDirty = false;
            return;
        }

        var allEntities = _world.GetAllEntities();

        // Filter entities that have all required components
        var matchingEntities = allEntities.Where(EntityHasAllComponents);
        _cachedEntities.AddRange(matchingEntities);

        _isDirty = false;
    }

    /// <summary>
    /// Returns the number of entities matching this query.
    /// </summary>
    public int Count => Entities.Count;

    private bool EntityHasAllComponents(Entity entity)
    {
        return _componentTypes.All(type => _world.HasComponentOfType(entity, type));
    }
}
