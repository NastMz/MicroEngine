using System.Numerics;
using System.Runtime.CompilerServices;

namespace EcsBench.Prototype;

// A first-party prototype, written for ENTITY-1 to measure what an archetype store of the engine's own would cost and
// give (docs/evaluations/entity-1.md). It is NOT a product and is not shipped: it has what the four workloads need
// (create with up to four components, destroy, queries over component sets, generation-checked ids) and nothing else.
// No adding or removing a component of a live entity, no deferred commands, no relations, events, serialization, tags
// or parallel walks: the list of what a real module would still need is in the evaluation.

/// <summary>An entity of a <see cref="MiniWorld"/>: a slot and the generation of the entity that held it.</summary>
internal readonly record struct Ent(int Index, uint Generation);

internal static class ComponentTypes
{
    internal static int Count;
    internal static readonly Func<int, Array>[] Create = new Func<int, Array>[64];
}

/// <summary>A small integer per component type (at most 64, one bit each in an archetype's mask).</summary>
internal static class ComponentType<T> where T : struct
{
    public static readonly int Id = Register();
    public static readonly ulong Bit = 1UL << Id;

    private static int Register()
    {
        int id = Interlocked.Increment(ref ComponentTypes.Count) - 1;
        if (id >= 64) throw new InvalidOperationException("The prototype holds at most 64 component types.");
        ComponentTypes.Create[id] = static n => new T[n];
        return id;
    }
}

/// <summary>The entities with one exact set of components: one array per component, rows aligned, no holes.</summary>
internal sealed class Archetype(ulong mask)
{
    public readonly ulong Mask = mask;
    private readonly Array?[] _columns = new Array?[64];
    private Ent[] _entities = new Ent[16];
    private int _capacity = 16;

    public int Count { get; private set; }
    public ReadOnlySpan<Ent> Entities => _entities.AsSpan(0, Count);

    public Span<T> Span<T>() where T : struct => Unsafe.As<T[]>(_columns[ComponentType<T>.Id]!).AsSpan(0, Count);

    internal void AddColumns()
    {
        for (ulong bits = Mask; bits != 0; bits &= bits - 1)
        {
            int id = BitOperations.TrailingZeroCount(bits);
            _columns[id] = ComponentTypes.Create[id](_capacity);
        }
    }

    internal int Add(Ent entity)
    {
        if (Count == _capacity)
        {
            _capacity *= 2;
            Array.Resize(ref _entities, _capacity);
            for (ulong bits = Mask; bits != 0; bits &= bits - 1)
            {
                int id = BitOperations.TrailingZeroCount(bits);
                var grown = ComponentTypes.Create[id](_capacity);
                Array.Copy(_columns[id]!, grown, Count);
                _columns[id] = grown;
            }
        }
        _entities[Count] = entity;
        return Count++;
    }

    internal ref T At<T>(int row) where T : struct => ref Unsafe.As<T[]>(_columns[ComponentType<T>.Id]!)[row];

    /// <summary>Removes a row by moving the last one into it; returns the entity that moved, or default when none did.</summary>
    internal Ent RemoveAt(int row)
    {
        int last = --Count;
        if (row == last) return default;
        for (ulong bits = Mask; bits != 0; bits &= bits - 1)
        {
            var column = _columns[BitOperations.TrailingZeroCount(bits)]!;
            Array.Copy(column, last, column, row, 1);
        }
        return _entities[row] = _entities[last];
    }
}

/// <summary>Entities as rows of archetypes. Not thread-safe; structural changes are not allowed during a walk.</summary>
internal sealed class MiniWorld
{
    private uint[] _generation = new uint[64];
    private Archetype?[] _archetype = new Archetype?[64];
    private int[] _row = new int[64];
    private int _used;
    private readonly Stack<int> _free = new();
    private readonly List<Archetype> _archetypes = [];
    private readonly Dictionary<ulong, Archetype> _byMask = [];
    private readonly Dictionary<ulong, (int Version, Archetype[] Found)> _matches = [];

    public int Count { get; private set; }

    public Ent Create<A>(in A a) where A : struct
    {
        var archetype = ArchetypeOf(ComponentType<A>.Bit);
        var entity = Place(archetype, out int row);
        archetype.At<A>(row) = a;
        return entity;
    }

    public Ent Create<A, B>(in A a, in B b) where A : struct where B : struct
    {
        var archetype = ArchetypeOf(ComponentType<A>.Bit | ComponentType<B>.Bit);
        var entity = Place(archetype, out int row);
        archetype.At<A>(row) = a;
        archetype.At<B>(row) = b;
        return entity;
    }

    public Ent Create<A, B, C>(in A a, in B b, in C c) where A : struct where B : struct where C : struct
    {
        var archetype = ArchetypeOf(ComponentType<A>.Bit | ComponentType<B>.Bit | ComponentType<C>.Bit);
        var entity = Place(archetype, out int row);
        archetype.At<A>(row) = a;
        archetype.At<B>(row) = b;
        archetype.At<C>(row) = c;
        return entity;
    }

    public Ent Create<A, B, C, D>(in A a, in B b, in C c, in D d) where A : struct where B : struct where C : struct where D : struct
    {
        var archetype = ArchetypeOf(ComponentType<A>.Bit | ComponentType<B>.Bit | ComponentType<C>.Bit | ComponentType<D>.Bit);
        var entity = Place(archetype, out int row);
        archetype.At<A>(row) = a;
        archetype.At<B>(row) = b;
        archetype.At<C>(row) = c;
        archetype.At<D>(row) = d;
        return entity;
    }

    public bool IsAlive(Ent entity) =>
        (uint)entity.Index < (uint)_used && entity.Generation != 0 && _generation[entity.Index] == entity.Generation && _archetype[entity.Index] is not null;

    /// <summary>Destroys the entity at once; nothing for a stale id.</summary>
    public void Destroy(Ent entity)
    {
        if (!IsAlive(entity)) return;
        var moved = _archetype[entity.Index]!.RemoveAt(_row[entity.Index]);
        if (moved.Generation != 0) _row[moved.Index] = _row[entity.Index];
        _archetype[entity.Index] = null;
        _generation[entity.Index]++;
        _free.Push(entity.Index);
        Count--;
    }

    /// <summary>The component of a live entity, by reference.</summary>
    public ref T Get<T>(Ent entity) where T : struct
    {
        if (!IsAlive(entity)) throw new InvalidOperationException($"{entity} is not alive.");
        return ref _archetype[entity.Index]!.At<T>(_row[entity.Index]);
    }

    /// <summary>The archetypes that have every component of <paramref name="mask"/>, including empty ones.</summary>
    public ReadOnlySpan<Archetype> Match(ulong mask)
    {
        if (_matches.TryGetValue(mask, out var cached) && cached.Version == _archetypes.Count) return cached.Found;
        return Rematch(mask);
    }

    // Apart from Match, so the walk that finds its archetypes cached allocates no closure.
    private Archetype[] Rematch(ulong mask)
    {
        var found = _archetypes.Where(a => (a.Mask & mask) == mask).ToArray();
        _matches[mask] = (_archetypes.Count, found);
        return found;
    }

    public ReadOnlySpan<Archetype> With<A, B>() where A : struct where B : struct => Match(ComponentType<A>.Bit | ComponentType<B>.Bit);
    public ReadOnlySpan<Archetype> With<A, B, C>() where A : struct where B : struct where C : struct =>
        Match(ComponentType<A>.Bit | ComponentType<B>.Bit | ComponentType<C>.Bit);
    public ReadOnlySpan<Archetype> With<A, B, C, D>() where A : struct where B : struct where C : struct where D : struct =>
        Match(ComponentType<A>.Bit | ComponentType<B>.Bit | ComponentType<C>.Bit | ComponentType<D>.Bit);

    private Archetype ArchetypeOf(ulong mask)
    {
        if (_byMask.TryGetValue(mask, out var archetype)) return archetype;
        archetype = new Archetype(mask);
        archetype.AddColumns();
        _byMask[mask] = archetype;
        _archetypes.Add(archetype);
        return archetype;
    }

    private Ent Place(Archetype archetype, out int row)
    {
        int slot;
        if (_free.Count > 0) slot = _free.Pop();
        else
        {
            if (_used == _generation.Length)
            {
                Array.Resize(ref _generation, _used * 2);
                Array.Resize(ref _archetype, _used * 2);
                Array.Resize(ref _row, _used * 2);
            }
            slot = _used++;
        }
        if (_generation[slot] == 0) _generation[slot] = 1;
        var entity = new Ent(slot, _generation[slot]);
        row = archetype.Add(entity);
        _archetype[slot] = archetype;
        _row[slot] = row;
        Count++;
        return entity;
    }
}
