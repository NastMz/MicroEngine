using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace MicroEngine.Scenes;

/// <summary>A small integer per attachment type, so a world finds a type's store with one array index.</summary>
internal static class AttachmentType
{
    internal static int Next;
}

internal static class AttachmentType<T>
{
    public static readonly int Index = Interlocked.Increment(ref AttachmentType.Next) - 1;
}

internal interface IAttachmentStore
{
    string TypeName { get; }
    object? Find(int slot);
    bool Remove(int slot);
    void Compact();
}

/// <summary>
/// One attachment type's dense array with a sparse slot index (slot → dense index + 1). A removal leaves a hole so a
/// running iteration is never shifted; <see cref="Compact"/> closes the holes in order at the step's boundaries.
/// </summary>
internal sealed class AttachmentStore<T> : IAttachmentStore where T : class
{
    private int[] _sparse = new int[64];
    internal T?[] _items = new T?[16];
    internal int[] _slots = new int[16];
    internal int _count;
    private bool _holes;

    public string TypeName { get; } = typeof(T).FullName ?? typeof(T).Name;

    public void Set(int slot, T value)
    {
        if (slot >= _sparse.Length) Array.Resize(ref _sparse, Math.Max(slot + 1, _sparse.Length * 2));
        int d = _sparse[slot] - 1;
        if (d >= 0) { _items[d] = value; return; }
        if (_count == _items.Length)
        {
            Array.Resize(ref _items, _count * 2);
            Array.Resize(ref _slots, _count * 2);
        }
        _items[_count] = value;
        _slots[_count] = slot;
        _sparse[slot] = ++_count;
    }

    public bool TryGet(int slot, [MaybeNullWhen(false)] out T value)
    {
        if (slot < _sparse.Length && _sparse[slot] is var d and > 0)
        {
            value = _items[d - 1]!;
            return true;
        }
        value = null;
        return false;
    }

    public object? Find(int slot) => TryGet(slot, out var value) ? value : null;

    public bool Remove(int slot)
    {
        if (slot >= _sparse.Length || _sparse[slot] == 0) return false;
        int d = _sparse[slot] - 1;
        _sparse[slot] = 0;
        _slots[d] = -1;
        _items[d] = null;
        _holes = true;
        return true;
    }

    // Stable: attach order is part of the contract (a swap-remove store changes what games do).
    public void Compact()
    {
        if (!_holes) return;
        _holes = false;
        int kept = 0;
        for (int i = 0; i < _count; i++)
        {
            int slot = _slots[i];
            if (slot < 0) continue;
            if (kept != i)
            {
                _items[kept] = _items[i];
                _slots[kept] = slot;
                _sparse[slot] = kept + 1;
            }
            kept++;
        }
        Array.Clear(_items, kept, _count - kept);
        _count = kept;
    }
}

/// <summary>The attachments of type <typeparamref name="T"/> of live entities, in attach order (<see cref="IWorld.All{T}"/>).</summary>
public readonly struct Attachments<T> : IEnumerable<T> where T : class
{
    private readonly SceneWorld _world;
    private readonly AttachmentStore<T>? _store;

    internal Attachments(SceneWorld world, AttachmentStore<T>? store) => (_world, _store) = (world, store);

    /// <summary>Starts a walk over the attachments that exist now.</summary>
    public Enumerator GetEnumerator() => new(_world, _store);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Walks the attachments; allocates nothing.</summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly SceneWorld _world;
        private readonly AttachmentStore<T>? _store;
        private readonly int _end;
        private int _index;

        internal Enumerator(SceneWorld world, AttachmentStore<T>? store)
        {
            _world = world;
            _store = store;
            _end = store?._count ?? 0;
            _index = -1;
            Current = null!;
            Entity = default;
        }

        /// <summary>The current attachment.</summary>
        public T Current { get; private set; }
        /// <summary>The entity of <see cref="Current"/>.</summary>
        public EntityId Entity { get; private set; }
        readonly object IEnumerator.Current => Current;

        /// <summary>Moves to the next attachment of a live entity.</summary>
        public bool MoveNext()
        {
            if (_store is not { } store) return false;
            var state = _world._state;
            while (++_index < _end)
            {
                int slot = store._slots[_index];
                if (slot < 0 || state[slot] != 1) continue;
                Current = store._items[_index]!;
                Entity = new EntityId((uint)slot, _world._gen[slot]);
                return true;
            }
            return false;
        }

        /// <summary>Starts over.</summary>
        public void Reset() => _index = -1;
        /// <summary>Nothing to release.</summary>
        public readonly void Dispose() { }
    }
}

/// <summary>The live direct children of an entity, in parenting order (<see cref="IWorld.Children"/>).</summary>
public readonly struct EntityChildren : IEnumerable<EntityId>
{
    private readonly SceneWorld _world;
    private readonly int _parent;

    internal EntityChildren(SceneWorld world, int parent) => (_world, _parent) = (world, parent);

    /// <summary>Starts a walk over the children the entity has now.</summary>
    public Enumerator GetEnumerator() => new(_world, _parent);
    IEnumerator<EntityId> IEnumerable<EntityId>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Walks the children; allocates nothing.</summary>
    public struct Enumerator : IEnumerator<EntityId>
    {
        private readonly SceneWorld _world;
        private readonly int _parent;
        private readonly int _version;
        private int _last;      // the last child when the walk started: children added later wait for the next walk
        private int _next;

        internal Enumerator(SceneWorld world, int parent)
        {
            _world = world;
            _parent = parent;
            _version = world._hierarchyVersion;
            Current = default;
            _last = _next = -1;
            Reset();
        }

        /// <summary>The current child.</summary>
        public EntityId Current { get; private set; }
        readonly object IEnumerator.Current => Current;

        /// <summary>Moves to the next live child.</summary>
        /// <exception cref="InvalidOperationException">An entity changed parent since the walk started.</exception>
        public bool MoveNext()
        {
            if (_world._hierarchyVersion != _version)
                throw new InvalidOperationException("The hierarchy changed during a Children walk (SetParent); walk again after re-parenting.");
            while (_next >= 0)
            {
                int c = _next;
                _next = c == _last ? -1 : _world._nextSibling[c];
                if (_world._state[c] != 1) continue;
                Current = new EntityId((uint)c, _world._gen[c]);
                return true;
            }
            return false;
        }

        /// <summary>Starts over.</summary>
        public void Reset()
        {
            _next = _world._firstChild[_parent];
            _last = _world._lastChild[_parent];
        }

        /// <summary>Nothing to release.</summary>
        public readonly void Dispose() { }
    }
}
