namespace MicroEngine.Core.ECS;

/// <summary>
/// Type-safe storage for components of a specific type.
/// Provides efficient access to component data organized by entity.
/// </summary>
internal class ComponentArray<T> where T : struct, IComponent
{
    private T[] _components = new T[16];
    private readonly Dictionary<Entity, int> _entityToIndex = new();
    private readonly Dictionary<int, Entity> _indexToEntity = new();
    private int _size = 0;

    /// <summary>
    /// Gets the number of components stored.
    /// </summary>
    public int Count => _size;

    /// <summary>
    /// Adds a component for the specified entity.
    /// </summary>
    public void Add(Entity entity, T component)
    {
        if (_entityToIndex.ContainsKey(entity))
        {
            throw new InvalidOperationException($"Entity {entity} already has component {typeof(T).Name}");
        }

        if (_size >= _components.Length)
        {
            Array.Resize(ref _components, _components.Length * 2);
        }

        int index = _size;
        _components[index] = component;
        _entityToIndex[entity] = index;
        _indexToEntity[index] = entity;
        _size++;
    }

    /// <summary>
    /// Removes the component for the specified entity.
    /// </summary>
    public void Remove(Entity entity)
    {
        if (!_entityToIndex.TryGetValue(entity, out int index))
        {
            return;
        }

        int lastIndex = _size - 1;
        if (index != lastIndex)
        {
            _components[index] = _components[lastIndex];
            Entity lastEntity = _indexToEntity[lastIndex];
            _entityToIndex[lastEntity] = index;
            _indexToEntity[index] = lastEntity;
        }

        _entityToIndex.Remove(entity);
        _indexToEntity.Remove(lastIndex);
        _size--;
    }

    /// <summary>
    /// Gets a reference to the component for the specified entity.
    /// </summary>
    public ref T Get(Entity entity)
    {
        if (!_entityToIndex.TryGetValue(entity, out int index))
        {
            throw new InvalidOperationException($"Entity {entity} does not have component {typeof(T).Name}");
        }

        return ref _components[index];
    }

    /// <summary>
    /// Tries to get a reference to the component for the specified entity.
    /// </summary>
    public bool TryGet(Entity entity, out T component)
    {
        if (_entityToIndex.TryGetValue(entity, out int index))
        {
            component = _components[index];
            return true;
        }

        component = default;
        return false;
    }

    /// <summary>
    /// Checks if the entity has this component type.
    /// </summary>
    public bool Has(Entity entity)
    {
        return _entityToIndex.ContainsKey(entity);
    }

    /// <summary>
    /// Gets all entities that have this component.
    /// </summary>
    public IEnumerable<Entity> GetEntities()
    {
        return _entityToIndex.Keys;
    }

    /// <summary>
    /// Gets a span of all components (for efficient iteration).
    /// </summary>
    public ReadOnlySpan<T> AsSpan()
    {
        return new ReadOnlySpan<T>(_components, 0, _size);
    }
}
