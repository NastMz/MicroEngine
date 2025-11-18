namespace MicroEngine.Core.ECS;

/// <summary>
/// Manages archetype creation, assignment, and entity lifecycle.
/// </summary>
internal class ArchetypeManager
{
    private readonly Dictionary<ArchetypeId, Archetype> _archetypes = new();
    private readonly Dictionary<Entity, Archetype> _entityToArchetype = new();

    public Archetype GetOrCreateArchetype(IEnumerable<Type> componentTypes)
    {
        var typeArray = componentTypes.ToArray();
        var id = new ArchetypeId(typeArray);

        if (!_archetypes.TryGetValue(id, out var archetype))
        {
            archetype = new Archetype(id, typeArray);
            _archetypes[id] = archetype;
        }

        return archetype;
    }

    public void AddEntityToArchetype(Entity entity, Archetype archetype, Dictionary<Type, object> components)
    {
        if (_entityToArchetype.ContainsKey(entity))
        {
            throw new InvalidOperationException($"Entity {entity} already belongs to an archetype");
        }

        archetype.AddEntity(entity, components);
        _entityToArchetype[entity] = archetype;
    }

    public void RemoveEntity(Entity entity)
    {
        if (!_entityToArchetype.TryGetValue(entity, out var archetype))
        {
            return;
        }

        archetype.RemoveEntity(entity);
        _entityToArchetype.Remove(entity);
    }

    public void MoveEntity(Entity entity, Archetype newArchetype, Dictionary<Type, object> components)
    {
        if (_entityToArchetype.TryGetValue(entity, out var oldArchetype))
        {
            oldArchetype.RemoveEntity(entity);
        }

        newArchetype.AddEntity(entity, components);
        _entityToArchetype[entity] = newArchetype;
    }

    public IEnumerable<Archetype> GetMatchingArchetypes(HashSet<Type> requiredTypes)
    {
        return _archetypes.Values.Where(a => a.MatchesQuery(requiredTypes));
    }

    public bool TryGetEntityArchetype(Entity entity, out Archetype? archetype)
    {
        return _entityToArchetype.TryGetValue(entity, out archetype);
    }
}
