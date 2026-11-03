using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Arch1.Scene;

/// <summary>
/// The P7 world: entity slots with generations, local and step-start transforms, an intrusive parent/child tree, and
/// attachments in one of the <see cref="AttachmentStorage"/> designs. The scene module calls <see cref="BeginStep"/>
/// before its world's step, <see cref="EndStep"/> after it and <see cref="BeginDraw"/> before drawing it; game code
/// uses <see cref="IWorld"/>.
/// </summary>
/// <remarks>Not thread-safe. Allocates only when it grows (entities, attachment types, per-type capacity).</remarks>
public sealed class World : IWorld
{
    private const byte Free = 0, Alive = 1, Destroyed = 2;

    internal uint[] _gen = new uint[64];
    internal byte[] _state = new byte[64];
    private Transform[] _local = new Transform[64];
    private Transform[] _previous = new Transform[64];
    private bool[] _snap = new bool[64];
    private int[] _parent = new int[64];
    private int[] _firstChild = new int[64];
    private int[] _nextSibling = new int[64];
    private int _used;
    private readonly Stack<int> _free = new();
    private readonly List<int> _pending = [];
    internal int[] _order = new int[64];
    internal int _orderCount;
    private int _count;
    private float _blend = 1;

    // DenseStable / DenseSwapRemove: one store per attachment type, indexed by TypeIndex<T>.
    private IStore?[] _stores = new IStore?[8];
    private readonly List<IStore> _storeList = [];
    // PerEntityDictionary: one bag per slot.
    internal Dictionary<Type, object>?[] _bags = new Dictionary<Type, object>?[64];

    /// <summary>A world storing attachments in <paramref name="storage"/>.</summary>
    public World(AttachmentStorage storage = AttachmentStorage.DenseStable)
    {
        Storage = storage;
        Dense = storage != AttachmentStorage.PerEntityDictionary;
    }

    /// <summary>The attachment storage design.</summary>
    public AttachmentStorage Storage { get; }
    internal bool Dense { get; }

    /// <inheritdoc/>
    public int Count => _count;
    /// <inheritdoc/>
    public WorldClock Clock { get; } = new();
    /// <inheritdoc/>
    public float Blend => _blend;
    /// <summary>The engine step this world last stepped in (-1: never).</summary>
    public long SteppedAt { get; private set; } = -1;

    // ------------------------------------------------------------------ engine hooks (the scene module calls these)

    /// <summary>
    /// Starts a step of this world inside engine step <paramref name="engineStep"/>: releases what is still pending, keeps
    /// every transform as the step's start (the blend origin) and ends every teleport.
    /// </summary>
    public void BeginStep(long engineStep)
    {
        Release();
        Array.Copy(_local, _previous, _used);
        Array.Clear(_snap, 0, _used);
        SteppedAt = engineStep;
    }

    /// <summary>Ends the step: tells <see cref="IDestroyedCallback"/> attachments and releases destroyed entities.</summary>
    public void EndStep() => Release();

    /// <summary>
    /// Sets <see cref="Blend"/> for this frame: <paramref name="engineBlend"/> when this world stepped in the latest
    /// engine step, otherwise 1 (it is frozen: drawn as its last step).
    /// </summary>
    public void BeginDraw(long latestEngineStep, float engineBlend) =>
        _blend = SteppedAt == latestEngineStep ? engineBlend : 1;

    // ------------------------------------------------------------------ entities

    /// <inheritdoc/>
    public EntityId Create(EntityId parent = default)
    {
        int parentSlot = parent.IsNone ? -1 : ResolveAlive(parent);
        int slot;
        if (_free.Count > 0) slot = _free.Pop();
        else
        {
            if (_used == _gen.Length) Grow();
            slot = _used++;
        }
        if (_gen[slot] == 0) _gen[slot] = 1;
        _state[slot] = Alive;
        _local[slot] = Transform.Identity;
        _previous[slot] = Transform.Identity;
        _snap[slot] = true;
        _parent[slot] = -1;
        _firstChild[slot] = -1;
        _nextSibling[slot] = -1;
        if (parentSlot >= 0) Link(slot, parentSlot);
        if (_orderCount == _order.Length) Array.Resize(ref _order, _order.Length * 2);
        _order[_orderCount++] = slot;
        _count++;
        return new EntityId((uint)slot, _gen[slot]);
    }

    /// <inheritdoc/>
    public bool IsAlive(EntityId entity) =>
        entity.Index < (uint)_used && _gen[entity.Index] == entity.Generation && _state[entity.Index] == Alive;

    /// <inheritdoc/>
    public void Destroy(EntityId entity)
    {
        if (!IsAlive(entity)) return;
        DestroySlot((int)entity.Index);
    }

    private void DestroySlot(int slot)
    {
        _state[slot] = Destroyed;
        _count--;
        _pending.Add(slot);
        for (int c = _firstChild[slot]; c >= 0; c = _nextSibling[c])
            if (_state[c] == Alive) DestroySlot(c);
    }

    /// <inheritdoc/>
    public EntityId ParentOf(EntityId entity)
    {
        int p = _parent[Resolve(entity)];
        return p < 0 ? EntityId.None : new EntityId((uint)p, _gen[p]);
    }

    /// <inheritdoc/>
    public void SetParent(EntityId entity, EntityId parent)
    {
        int slot = ResolveAlive(entity);
        int parentSlot = parent.IsNone ? -1 : ResolveAlive(parent);
        for (int p = parentSlot; p >= 0; p = _parent[p])
            if (p == slot) throw new InvalidOperationException($"{entity} cannot become a descendant of itself.");
        Unlink(slot);
        if (parentSlot >= 0) Link(slot, parentSlot);
    }

    private void Link(int slot, int parentSlot)
    {
        _parent[slot] = parentSlot;
        _nextSibling[slot] = _firstChild[parentSlot];
        _firstChild[parentSlot] = slot;
    }

    private void Unlink(int slot)
    {
        int p = _parent[slot];
        if (p < 0) return;
        ref int link = ref _firstChild[p];
        while (link != slot) link = ref _nextSibling[link];
        link = _nextSibling[slot];
        _parent[slot] = -1;
        _nextSibling[slot] = -1;
    }

    // ------------------------------------------------------------------ transforms and interpolation

    /// <inheritdoc/>
    public ref Transform Local(EntityId entity) => ref _local[Resolve(entity)];

    /// <inheritdoc/>
    public Matrix4x4 WorldMatrix(EntityId entity) => WorldMatrix(entity, _blend);

    /// <inheritdoc/>
    public Matrix4x4 WorldMatrix(EntityId entity, float blend)
    {
        int slot = Resolve(entity);
        var m = Drawn(slot, blend).ToMatrix();
        for (int p = _parent[slot]; p >= 0; p = _parent[p]) m *= Drawn(p, blend).ToMatrix();
        return m;
    }

    /// <inheritdoc/>
    public Vector2 DrawPosition2D(EntityId entity) => DrawPosition2D(entity, _blend);

    /// <inheritdoc/>
    public Vector2 DrawPosition2D(EntityId entity, float blend)
    {
        int slot = Resolve(entity);
        if (_parent[slot] >= 0)
        {
            var t = WorldMatrix(entity, blend).Translation;
            return new Vector2(t.X, t.Y);
        }
        var current = _local[slot].Position2D;
        if (blend >= 1 || _snap[slot]) return current;
        var previous = _previous[slot].Position2D;
        return previous == current ? current : Vector2.Lerp(previous, current, blend);
    }

    private Transform Drawn(int slot, float blend) =>
        blend >= 1 || _snap[slot] ? _local[slot] : Transform.Interpolate(_previous[slot], _local[slot], blend);

    /// <inheritdoc/>
    public void Teleport(EntityId entity) => _snap[Resolve(entity)] = true;

    // ------------------------------------------------------------------ attachments

    /// <inheritdoc/>
    public T Attach<T>(EntityId entity, T attachment) where T : class
    {
        ArgumentNullException.ThrowIfNull(attachment);
        int slot = ResolveAlive(entity);
        if (Dense) StoreFor<T>().Set(slot, attachment);
        else (_bags[slot] ??= [])[typeof(T)] = attachment;
        return attachment;
    }

    /// <inheritdoc/>
    public bool TryGet<T>(EntityId entity, [NotNullWhen(true)] out T? attachment) where T : class
    {
        uint slot = entity.Index;
        if (slot < (uint)_used && _gen[slot] == entity.Generation && _state[slot] != Free)
        {
            if (Dense)
            {
                if (ExistingStore<T>() is { } store && store.TryGet((int)slot, out attachment)) return true;
            }
            else if (_bags[slot] is { } bag && bag.TryGetValue(typeof(T), out var value))
            {
                attachment = Unsafe.As<T>(value);
                return true;
            }
        }
        attachment = null;
        return false;
    }

    /// <inheritdoc/>
    public T Get<T>(EntityId entity) where T : class
    {
        if (TryGet<T>(entity, out var attachment)) return attachment;
        Resolve(entity);
        throw new InvalidOperationException($"{entity} has no {typeof(T).Name} attachment.");
    }

    /// <inheritdoc/>
    public bool Detach<T>(EntityId entity) where T : class => DetachAny<T>(entity);

    private bool DetachAny<T>(EntityId entity)
    {
        int slot = ResolveAlive(entity);
        if (Dense) return ExistingStore<T>() is { } store && store.Remove(slot);
        return _bags[slot] is { } bag && bag.Remove(typeof(T));
    }

    /// <inheritdoc/>
    public Attachments<T> All<T>() where T : class => new(this, Dense ? ExistingStore<T>() : null);

    /// <summary>Attaches a struct value under <typeparamref name="T"/> and returns a reference to the stored copy.</summary>
    /// <remarks>
    /// Dense storage keeps it unboxed in the type's array (the reference is valid until that array grows or compacts);
    /// the dictionary design boxes it. P7 measures both; see the README for whether struct attachments are frozen.
    /// </remarks>
    public ref T AttachValue<T>(EntityId entity, in T value) where T : struct
    {
        int slot = ResolveAlive(entity);
        if (Dense)
        {
            var store = StoreFor<T>();
            store.Set(slot, value);
            return ref store.Ref(slot);
        }
        object box = value;
        (_bags[slot] ??= [])[typeof(T)] = box;
        return ref Unsafe.Unbox<T>(box);
    }

    /// <summary>A reference to the struct attachment <typeparamref name="T"/>; throws when there is none.</summary>
    public ref T Value<T>(EntityId entity) where T : struct
    {
        int slot = Resolve(entity);
        if (Dense)
        {
            if (ExistingStore<T>() is { } store && store.Contains(slot)) return ref store.Ref(slot);
        }
        else if (_bags[slot] is { } bag && bag.TryGetValue(typeof(T), out var box)) return ref Unsafe.Unbox<T>(box);
        throw new InvalidOperationException($"{entity} has no {typeof(T).Name} value.");
    }

    /// <summary>Removes the struct attachment <typeparamref name="T"/>; false when there was none.</summary>
    public bool DetachValue<T>(EntityId entity) where T : struct => DetachAny<T>(entity);

    private DenseStore<T> StoreFor<T>()
    {
        int index = TypeIndex<T>.Value;
        if (index >= _stores.Length) Array.Resize(ref _stores, Math.Max(index + 1, _stores.Length * 2));
        if (_stores[index] is { } existing) return Unsafe.As<DenseStore<T>>(existing);
        var store = new DenseStore<T>(Storage == AttachmentStorage.DenseSwapRemove);
        _stores[index] = store;
        _storeList.Add(store);
        return store;
    }

    internal DenseStore<T>? ExistingStore<T>()
    {
        int index = TypeIndex<T>.Value;
        return index < _stores.Length ? Unsafe.As<DenseStore<T>?>(_stores[index]) : null;
    }

    // ------------------------------------------------------------------ release

    private void Release()
    {
        if (_pending.Count == 0)
        {
            foreach (var store in _storeList) store.Compact();   // holes left by Detach
            return;
        }
        // Callbacks first, while every destroyed entity is still readable; a callback may destroy more (appended).
        for (int i = 0; i < _pending.Count; i++)
        {
            int slot = _pending[i];
            var id = new EntityId((uint)slot, _gen[slot]);
            if (Dense)
            {
                foreach (var store in _storeList)
                    if (store.Callback(slot) is { } callback) callback.OnDestroyed(id);
            }
            else if (_bags[slot] is { } bag)
            {
                foreach (var value in bag.Values)
                    if (value is IDestroyedCallback callback) callback.OnDestroyed(id);
            }
        }
        foreach (int slot in _pending)
        {
            Unlink(slot);
            for (int c = _firstChild[slot]; c >= 0;)
            {
                int next = _nextSibling[c];
                _parent[c] = -1;
                _nextSibling[c] = -1;
                c = next;
            }
            _firstChild[slot] = -1;
            if (Dense) foreach (var store in _storeList) store.Remove(slot);
            else _bags[slot] = null;
            _state[slot] = Free;
            _gen[slot] = _gen[slot] == uint.MaxValue ? 1 : _gen[slot] + 1;
        }
        // Stable compaction of the creation order (O(n), not O(n) per removal).
        int kept = 0;
        for (int i = 0; i < _orderCount; i++)
            if (_state[_order[i]] != Free) _order[kept++] = _order[i];
        _orderCount = kept;
        foreach (var store in _storeList) store.Compact();
        // Slots go back to the free list only now, so nothing created during a callback reused one being released.
        foreach (int slot in _pending) _free.Push(slot);
        _pending.Clear();
    }

    // ------------------------------------------------------------------ slots

    // The fast path inlines into Local/DrawPosition2D; the throwing path stays out of line (P7 measured 10.6 ns per
    // Local read with the throws inline, see the README).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Resolve(EntityId entity)
    {
        uint slot = entity.Index;
        var gen = _gen;
        if (slot < (uint)gen.Length && gen[slot] == entity.Generation && entity.Generation != 0 && _state[slot] != Free) return (int)slot;
        return ResolveFailed(entity);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int ResolveFailed(EntityId entity)
    {
        uint slot = entity.Index;
        if (entity.IsNone) throw new InvalidOperationException("The id is entity:none: it refers to nothing.");
        if (slot >= (uint)_used) throw new InvalidOperationException($"{entity} was not issued by this world.");
        throw new InvalidOperationException($"{entity} is stale: it was destroyed (its slot is at generation {_gen[slot]}).");
    }

    private int ResolveAlive(EntityId entity)
    {
        int slot = Resolve(entity);
        if (_state[slot] != Alive) throw new InvalidOperationException($"{entity} was destroyed this step.");
        return slot;
    }

    private void Grow()
    {
        int size = _gen.Length * 2;
        Array.Resize(ref _gen, size);
        Array.Resize(ref _state, size);
        Array.Resize(ref _local, size);
        Array.Resize(ref _previous, size);
        Array.Resize(ref _snap, size);
        Array.Resize(ref _parent, size);
        Array.Resize(ref _firstChild, size);
        Array.Resize(ref _nextSibling, size);
        Array.Resize(ref _bags, size);
    }
}

/// <summary>A small integer per attachment type, so a world finds a type's store with one array index.</summary>
internal static class TypeIndex
{
    internal static int Next;
}

internal static class TypeIndex<T>
{
    public static readonly int Value = Interlocked.Increment(ref TypeIndex.Next) - 1;
}

internal interface IStore
{
    IDestroyedCallback? Callback(int slot);
    bool Remove(int slot);
    void Compact();
}

/// <summary>One attachment type's dense array with a sparse slot index (slot → dense index + 1).</summary>
internal sealed class DenseStore<T>(bool swapRemove) : IStore
{
    private int[] _sparse = new int[64];
    internal T[] _items = new T[16];
    internal int[] _slots = new int[16];
    internal int _count;
    private bool _holes;
    private static readonly bool ValueCallbacks = typeof(IDestroyedCallback).IsAssignableFrom(typeof(T));

    public bool Contains(int slot) => slot < _sparse.Length && _sparse[slot] != 0;

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
            value = _items[d - 1];
            return true;
        }
        value = default;
        return false;
    }

    public ref T Ref(int slot) => ref _items[_sparse[slot] - 1];

    public IDestroyedCallback? Callback(int slot)
    {
        if (!Contains(slot)) return null;
        var value = _items[_sparse[slot] - 1];
        return typeof(T).IsValueType && !ValueCallbacks ? null : value as IDestroyedCallback;
    }

    // Leaves a hole (slot -1) so a running iteration is never shifted; Compact closes holes at the step's end.
    public bool Remove(int slot)
    {
        if (!Contains(slot)) return false;
        int d = _sparse[slot] - 1;
        _sparse[slot] = 0;
        _slots[d] = -1;
        _items[d] = default!;
        _holes = true;
        return true;
    }

    public void Compact()
    {
        if (!_holes) return;
        _holes = false;
        if (swapRemove)
        {
            // Fill each hole with the last live element: O(holes), and attach order is lost.
            int i = 0;
            while (i < _count)
            {
                if (_slots[i] >= 0) { i++; continue; }
                int last = --_count;
                if (last == i) break;
                _items[i] = _items[last];
                _slots[i] = _slots[last];
                _items[last] = default!;
                if (_slots[i] >= 0) _sparse[_slots[i]] = i + 1;
            }
            return;
        }
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
    private readonly World _world;
    private readonly DenseStore<T>? _store;

    internal Attachments(World world, DenseStore<T>? store) => (_world, _store) = (world, store);

    /// <summary>Starts a walk over the attachments that exist now.</summary>
    public Enumerator GetEnumerator() => new(_world, _store);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Walks the attachments; allocates nothing.</summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly World _world;
        private readonly DenseStore<T>? _store;
        private readonly int _end;
        private int _index;

        internal Enumerator(World world, DenseStore<T>? store)
        {
            _world = world;
            _store = store;
            _end = world.Dense ? store?._count ?? 0 : world._orderCount;
            _index = -1;
            Current = null!;
            Entity = default;
        }

        /// <summary>The current attachment.</summary>
        public T Current { get; private set; }
        /// <summary>The entity of <see cref="Current"/>.</summary>
        public EntityId Entity { get; private set; }
        readonly object System.Collections.IEnumerator.Current => Current;

        /// <summary>Moves to the next attachment of a live entity.</summary>
        public bool MoveNext()
        {
            var state = _world._state;
            if (_store is { } store)
            {
                while (++_index < _end)
                {
                    int slot = store._slots[_index];
                    if (slot < 0 || state[slot] != 1) continue;
                    Current = store._items[_index];
                    Entity = new EntityId((uint)slot, _world._gen[slot]);
                    return true;
                }
                return false;
            }
            if (_world.Dense) return false;
            var order = _world._order;
            var bags = _world._bags;
            while (++_index < _end)
            {
                int slot = order[_index];
                if (state[slot] != 1 || bags[slot] is not { } bag || !bag.TryGetValue(typeof(T), out var value)) continue;
                Current = Unsafe.As<T>(value);
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
