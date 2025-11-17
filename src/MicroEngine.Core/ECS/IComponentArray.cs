namespace MicroEngine.Core.ECS;

/// <summary>
/// Non-generic interface for component storage.
/// Allows type-agnostic component management.
/// </summary>
internal interface IComponentArray
{
    void Remove(Entity entity);
    bool Has(Entity entity);
}

/// <summary>
/// Type-erased wrapper for ComponentArray.
/// </summary>
internal class ComponentArrayWrapper<T> : IComponentArray where T : struct, IComponent
{
    private readonly ComponentArray<T> _array = new();

    public ComponentArray<T> Array => _array;

    public void Remove(Entity entity)
    {
        _array.Remove(entity);
    }

    public bool Has(Entity entity)
    {
        return _array.Has(entity);
    }
}
