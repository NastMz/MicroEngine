using Arch.Core;

namespace MicroEngine.Ecs;

using World = Arch.Core.World;

internal interface IEcsSnapshot
{
    Type Type { get; }
    void Take(World world, int step);
}

/// <summary>
/// The values a component type had when the step started, kept by the store for a type marked with
/// <see cref="EcsComponents.Interpolate{T}"/>, and the way drawing reads a row between that value and the current one
/// (<see cref="EcsStore.Interpolated{T}"/>). The game copies nothing.
/// </summary>
/// <remarks>
/// A row that did not have the component when the step started (a new row, a component added during the step, a row
/// that took a destroyed row's place) is read where it is, like a new entity of the scene's world. So is every row
/// while the scene is not stepped (under an overlay, in a transition), because the store's blend is then 1.
/// </remarks>
public sealed class EcsInterpolation<T> : IEcsSnapshot where T : struct
{
    private static readonly QueryDescription All = new QueryDescription().WithAll<T>();

    private readonly EcsStore _store;
    private readonly Blender<T> _blend;
    // By the row's id: the value, and the row's version and the step it was taken in, which say whether it is this row's.
    private T[] _was = new T[256];
    private int[] _version = new int[256], _step = new int[256];

    internal EcsInterpolation(EcsStore store, Blender<T> blend) => (_store, _blend) = (store, blend);

    Type IEcsSnapshot.Type => typeof(T);

    void IEcsSnapshot.Take(World world, int step)
    {
        foreach (ref var chunk in world.Query(in All))
        {
            var values = chunk.GetSpan<T>();
            for (int i = 0; i < chunk.Count; i++)
            {
                var row = chunk.Entity(i);
                int id = row.Id;
                if (id >= _was.Length) Grow(id);
                _was[id] = values[i];
                _version[id] = row.Version;
                _step[id] = step;
            }
        }
    }

    private void Grow(int id)
    {
        int size = Math.Max(id + 1, _was.Length * 2);
        Array.Resize(ref _was, size);
        Array.Resize(ref _version, size);
        Array.Resize(ref _step, size);
    }

    /// <summary>
    /// The component of <paramref name="row"/> as this frame draws it: between its value when the step started and
    /// <paramref name="now"/>, at the store's <see cref="EcsStore.Blend"/>. For a walk that already has the current
    /// value in hand.
    /// </summary>
    public T Blended(Entity row, in T now)
    {
        float blend = _store.Blend;
        int id = row.Id;
        if (blend >= 1f || (uint)id >= (uint)_was.Length || _step[id] != _store.StepNumber || _version[id] != row.Version) return now;
        return _blend(in _was[id], in now, blend);
    }

    /// <summary>The component of <paramref name="row"/> as this frame draws it, reading its current value from the store.</summary>
    public T Blended(Entity row) => Blended(row, in _store.World.Get<T>(row));
}
