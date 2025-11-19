using MicroEngine.Core.Exceptions;

namespace MicroEngine.Core.ECS;

/// <summary>
/// Stores entities with the same component composition using existing IComponentArray infrastructure.
/// No reflection or dynamic types needed.
/// </summary>
internal class Archetype
{
    private readonly Dictionary<Type, IComponentArray> _componentArrays = new();
    private readonly HashSet<Entity> _entities = new();
    public ArchetypeId Id { get; }

    public Archetype(ArchetypeId id, IEnumerable<Type> componentTypes)
    {
        Id = id;
        foreach (var type in componentTypes)
        {
            var wrapperType = typeof(ComponentArrayWrapper<>).MakeGenericType(type);
            var wrapper = (IComponentArray)Activator.CreateInstance(wrapperType)!;
            _componentArrays[type] = wrapper;
        }
    }

    public IReadOnlySet<Entity> Entities => _entities;

    public void AddEntity(Entity entity, Dictionary<Type, object> components)
    {
        if (_entities.Contains(entity))
        {
            throw new InvalidOperationException($"Entity {entity} already exists in archetype");
        }

        foreach (var (type, component) in components)
        {
            if (!_componentArrays.TryGetValue(type, out var array))
            {
                throw new InvalidOperationException($"Archetype does not support component type {type.Name}");
            }
            
            array.AddBoxed(entity, component);
        }

        _entities.Add(entity);
    }

    public void RemoveEntity(Entity entity)
    {
        if (!_entities.Remove(entity))
        {
            return;
        }

        foreach (var array in _componentArrays.Values)
        {
            array.Remove(entity);
        }
    }

    public bool HasComponentType(Type componentType)
    {
        return _componentArrays.ContainsKey(componentType);
    }

    public IComponentArray GetComponentArray(Type componentType)
    {
        return _componentArrays[componentType];
    }

    public bool MatchesQuery(HashSet<Type> requiredTypes)
    {
        return requiredTypes.All(t => _componentArrays.ContainsKey(t));
    }
}
