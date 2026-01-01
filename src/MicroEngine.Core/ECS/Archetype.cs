using MicroEngine.Core.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace MicroEngine.Core.ECS;

/// <summary>
/// Represents a specific combination of component types.
/// Used solely for indexing and fast iteration (CachedQueries).
/// DOES NOT store component data.
/// </summary>
internal class Archetype
{
    private readonly HashSet<Type> _componentTypes;
    private readonly HashSet<Entity> _entities = new();
    public ArchetypeId Id { get; }

    public Archetype(ArchetypeId id, IEnumerable<Type> componentTypes)
    {
        Id = id;
        _componentTypes = new HashSet<Type>(componentTypes);
    }

    public IReadOnlyCollection<Entity> Entities => _entities;

    public void AddEntity(Entity entity)
    {
        if (_entities.Contains(entity))
        {
            throw new InvalidOperationException($"Entity {entity} already exists in archetype");
        }

        _entities.Add(entity);
    }

    public void RemoveEntity(Entity entity)
    {
        _entities.Remove(entity);
    }

    public bool HasComponentType(Type componentType)
    {
        return _componentTypes.Contains(componentType);
    }

    public bool MatchesQuery(HashSet<Type> requiredTypes)
    {
        return requiredTypes.IsSubsetOf(_componentTypes);
    }
}
