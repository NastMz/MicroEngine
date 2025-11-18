namespace MicroEngine.Core.ECS;

/// <summary>
/// The central container for all ECS data and operations.
/// Manages entities, components, and systems.
/// </summary>
public sealed class World
{
    private readonly Dictionary<Type, IComponentArray> _componentArrays = new();
    private readonly List<ISystem> _systems = new();
    private readonly Queue<Entity> _entitiesToDestroy = new();
    private readonly HashSet<Entity> _entitiesPendingDestruction = new();
    private readonly Dictionary<Entity, string?> _entityNames = new();
    private readonly List<CachedQuery> _cachedQueries = new();

    private uint _nextEntityId = 1;
    private readonly Dictionary<uint, uint> _entityVersions = new();
    private readonly HashSet<Entity> _activeEntities = new();

    /// <summary>
    /// Gets the number of active entities in the world.
    /// </summary>
    public int EntityCount => _activeEntities.Count;

    /// <summary>
    /// Gets the number of registered systems.
    /// </summary>
    public int SystemCount => _systems.Count;

    /// <summary>
    /// Creates a new entity with an optional name for debugging.
    /// </summary>
    public Entity CreateEntity(string? name = null)
    {
        uint id = _nextEntityId++;
        _entityVersions.TryGetValue(id, out uint version);

        var entity = new Entity(id, version);
        _activeEntities.Add(entity);

        if (name != null)
        {
            _entityNames[entity] = name;
        }

        return entity;
    }

    /// <summary>
    /// Destroys an entity and all its components.
    /// Destruction is deferred until end of frame.
    /// </summary>
    public void DestroyEntity(Entity entity)
    {
        if (!IsEntityValid(entity))
        {
            return;
        }

        _entitiesToDestroy.Enqueue(entity);
        _entitiesPendingDestruction.Add(entity);
    }

    /// <summary>
    /// Checks if an entity is valid (exists and has correct version).
    /// </summary>
    public bool IsEntityValid(Entity entity)
    {
        return _activeEntities.Contains(entity) && !_entitiesPendingDestruction.Contains(entity);
    }

    /// <summary>
    /// Gets the debug name of an entity.
    /// </summary>
    public string? GetEntityName(Entity entity)
    {
        return _entityNames.GetValueOrDefault(entity);
    }

    /// <summary>
    /// Adds a component to an entity.
    /// </summary>
    public void AddComponent<T>(Entity entity, T component) where T : struct, IComponent
    {
        if (!IsEntityValid(entity))
        {
            throw new InvalidOperationException($"Cannot add component to invalid entity {entity}");
        }

        GetOrCreateComponentArray<T>().Add(entity, component);
        InvalidateCachedQueries();
    }

    /// <summary>
    /// Removes a component from an entity.
    /// </summary>
    public void RemoveComponent<T>(Entity entity) where T : struct, IComponent
    {
        if (!IsEntityValid(entity))
        {
            return;
        }

        if (_componentArrays.TryGetValue(typeof(T), out var array))
        {
            array.Remove(entity);
            InvalidateCachedQueries();
        }
    }

    /// <summary>
    /// Gets a reference to a component on an entity.
    /// </summary>
    public ref T GetComponent<T>(Entity entity) where T : struct, IComponent
    {
        if (!IsEntityValid(entity))
        {
            throw new InvalidOperationException($"Cannot get component from invalid entity {entity}");
        }

        return ref GetOrCreateComponentArray<T>().Get(entity);
    }

    /// <summary>
    /// Tries to get a component from an entity.
    /// </summary>
    public bool TryGetComponent<T>(Entity entity, out T component) where T : struct, IComponent
    {
        component = default;

        if (!IsEntityValid(entity))
        {
            return false;
        }

        if (_componentArrays.TryGetValue(typeof(T), out var array))
        {
            var typedArray = ((ComponentArrayWrapper<T>)array).Array;
            return typedArray.TryGet(entity, out component);
        }

        return false;
    }

    /// <summary>
    /// Checks if an entity has a specific component.
    /// </summary>
    public bool HasComponent<T>(Entity entity) where T : struct, IComponent
    {
        if (!IsEntityValid(entity))
        {
            return false;
        }

        if (_componentArrays.TryGetValue(typeof(T), out var array))
        {
            return array.Has(entity);
        }

        return false;
    }

    /// <summary>
    /// Checks if an entity has a component of the specified type (non-generic version).
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="componentType">The component type to check for.</param>
    /// <returns>True if the entity has the component, false otherwise.</returns>
    internal bool HasComponentOfType(Entity entity, Type componentType)
    {
        if (!IsEntityValid(entity))
        {
            return false;
        }

        if (_componentArrays.TryGetValue(componentType, out var array))
        {
            return array.Has(entity);
        }

        return false;
    }

    /// <summary>
    /// Registers a system to be updated each frame.
    /// </summary>
    public void RegisterSystem<T>(T system) where T : ISystem
    {
        if (_systems.Contains(system))
        {
            throw new InvalidOperationException($"System {typeof(T).Name} is already registered");
        }

        _systems.Add(system);
    }

    /// <summary>
    /// Unregisters a system.
    /// </summary>
    public void UnregisterSystem<T>(T system) where T : ISystem
    {
        _systems.Remove(system);
    }

    /// <summary>
    /// Updates all registered systems.
    /// </summary>
    public void Update(float deltaTime)
    {
        foreach (var system in _systems)
        {
            system.Update(this, deltaTime);
        }

        ProcessDestroyedEntities();
    }

    /// <summary>
    /// Gets all entities that have a specific component.
    /// </summary>
    public IEnumerable<Entity> GetEntitiesWith<T>() where T : struct, IComponent
    {
        if (_componentArrays.TryGetValue(typeof(T), out var array))
        {
            var typedArray = ((ComponentArrayWrapper<T>)array).Array;
            return typedArray.GetEntities();
        }

        return Enumerable.Empty<Entity>();
    }

    /// <summary>
    /// Gets all active entities in the world.
    /// </summary>
    public IEnumerable<Entity> GetAllEntities()
    {
        return _activeEntities;
    }

    /// <summary>
    /// Creates a cached query for entities with specific component types.
    /// The query result is cached and automatically invalidated when components change.
    /// </summary>
    /// <param name="componentTypes">The component types to query for.</param>
    /// <returns>A cached query that can be reused across frames.</returns>
    public CachedQuery CreateCachedQuery(params Type[] componentTypes)
    {
        var query = new CachedQuery(this, componentTypes);
        _cachedQueries.Add(query);
        return query;
    }

    /// <summary>
    /// Creates a cached query for entities with a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to query for.</typeparam>
    /// <returns>A cached query that can be reused across frames.</returns>
    public CachedQuery CreateCachedQuery<T>() where T : struct, IComponent
    {
        return CreateCachedQuery(typeof(T));
    }

    /// <summary>
    /// Creates a cached query for entities with two specific component types.
    /// </summary>
    /// <typeparam name="T1">The first component type.</typeparam>
    /// <typeparam name="T2">The second component type.</typeparam>
    /// <returns>A cached query that can be reused across frames.</returns>
    public CachedQuery CreateCachedQuery<T1, T2>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        return CreateCachedQuery(typeof(T1), typeof(T2));
    }

    private void InvalidateCachedQueries()
    {
        foreach (var query in _cachedQueries)
        {
            query.Invalidate();
        }
    }

    private ComponentArray<T> GetOrCreateComponentArray<T>() where T : struct, IComponent
    {
        var type = typeof(T);

        if (!_componentArrays.TryGetValue(type, out var array))
        {
            var wrapper = new ComponentArrayWrapper<T>();
            _componentArrays[type] = wrapper;
            return wrapper.Array;
        }

        return ((ComponentArrayWrapper<T>)array).Array;
    }

    private void ProcessDestroyedEntities()
    {
        while (_entitiesToDestroy.Count > 0)
        {
            var entity = _entitiesToDestroy.Dequeue();

            if (!_activeEntities.Contains(entity))
            {
                continue;
            }

            foreach (var array in _componentArrays.Values)
            {
                array.Remove(entity);
            }

            _activeEntities.Remove(entity);
            _entitiesPendingDestruction.Remove(entity);
            _entityNames.Remove(entity);

            _entityVersions[entity.Id] = entity.Version + 1;
        }
    }
}
