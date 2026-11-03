using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using MicroEngine.Kernel;

namespace MicroEngine.Scenes;

/// <summary>
/// The engine's <see cref="IWorld"/>: entity slots with generations, local and step-start transforms, a parent/child
/// tree, and one dense array per attachment type. Games program against <see cref="IWorld"/>; whoever owns the world
/// (its scene) calls <see cref="BeginStep"/> before the world's step, <see cref="EndStep"/> after it and
/// <see cref="BeginDraw"/> before drawing it.
/// </summary>
/// <remarks>
/// Not thread-safe. Allocates only when it grows (entities, attachment types, per-type capacity). Its simulation state
/// (ids, hierarchy, transforms, which attachments each entity has, the state of attachments that are
/// <see cref="IStateHashed"/>, and the clock) takes part in the per-step state hash; step-start transforms, teleports and
/// the blend are drawing state and do not.
/// </remarks>
public sealed class SceneWorld : IWorld, IStateHashed
{
    private const byte Free = 0, Alive = 1, Destroyed = 2;

    internal uint[] _gen = new uint[64];
    internal byte[] _state = new byte[64];
    private Transform[] _local = new Transform[64];
    private Transform[] _previous = new Transform[64];
    private bool[] _snap = new bool[64];
    internal int[] _parent = new int[64];
    internal int[] _firstChild = new int[64];
    internal int[] _lastChild = new int[64];
    internal int[] _nextSibling = new int[64];
    private int _used;
    private readonly Stack<int> _free = new();
    private readonly List<int> _pending = [];
    private int[] _order = new int[64];
    private int _orderCount;
    private int _count;
    private float _blend = 1;
    internal int _hierarchyVersion;

    private IAttachmentStore?[] _stores = new IAttachmentStore?[8];
    private readonly List<IAttachmentStore> _storeList = [];

    /// <inheritdoc/>
    public int Count => _count;
    /// <inheritdoc/>
    public WorldClock Clock { get; } = new();
    /// <inheritdoc/>
    public float Blend => _blend;
    /// <summary>The engine step this world last stepped in (-1: never).</summary>
    public long SteppedAt { get; private set; } = -1;

    // ------------------------------------------------------------------ hooks for the world's owner

    /// <summary>
    /// Starts a step of this world inside engine step <paramref name="engineStep"/> (the engine's update count): releases
    /// what is still pending, keeps every transform as the step's start (the blend origin), ends every teleport and
    /// advances <see cref="Clock"/> by <paramref name="dt"/>.
    /// </summary>
    /// <returns>The scaled length of the step: <paramref name="dt"/> exactly at scale 1, 0 while frozen.</returns>
    public float BeginStep(long engineStep, float dt)
    {
        Release();
        Array.Copy(_local, _previous, _used);
        Array.Clear(_snap, 0, _used);
        SteppedAt = engineStep;
        return Clock.Advance(dt);
    }

    /// <summary>Ends the step: tells <see cref="IDestroyedCallback"/> attachments and releases destroyed entities.</summary>
    public void EndStep() => Release();

    /// <summary>
    /// Sets <see cref="Blend"/> for this frame: <paramref name="engineBlend"/> when this world stepped in
    /// <paramref name="latestEngineStep"/>, otherwise 1 (the world is frozen: drawn as its last step).
    /// </summary>
    public void BeginDraw(long latestEngineStep, float engineBlend) =>
        _blend = SteppedAt == latestEngineStep ? Math.Clamp(engineBlend, 0, 1) : 1;

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
        _lastChild[slot] = -1;
        _nextSibling[slot] = -1;
        if (parentSlot >= 0) Link(slot, parentSlot);
        if (_orderCount == _order.Length) Array.Resize(ref _order, _order.Length * 2);
        _order[_orderCount++] = slot;
        _count++;
        return new EntityId((uint)slot, _gen[slot]);
    }

    /// <inheritdoc/>
    public bool IsAlive(EntityId entity) =>
        entity.Index < (uint)_used && entity.Generation != 0 && _gen[entity.Index] == entity.Generation && _state[entity.Index] == Alive;

    /// <inheritdoc/>
    public void Destroy(EntityId entity)
    {
        if (IsAlive(entity)) DestroySlot((int)entity.Index);
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
        if (_parent[slot] == parentSlot) return;
        Unlink(slot);
        if (parentSlot >= 0) Link(slot, parentSlot);
        _snap[slot] = true;
    }

    /// <inheritdoc/>
    public EntityChildren Children(EntityId parent) => new(this, Resolve(parent));

    private void Link(int slot, int parentSlot)
    {
        _parent[slot] = parentSlot;
        _nextSibling[slot] = -1;
        if (_lastChild[parentSlot] < 0) _firstChild[parentSlot] = slot;
        else _nextSibling[_lastChild[parentSlot]] = slot;
        _lastChild[parentSlot] = slot;
    }

    private void Unlink(int slot)
    {
        int p = _parent[slot];
        if (p < 0) return;
        _hierarchyVersion++;
        int previous = -1;
        for (int c = _firstChild[p]; c != slot; c = _nextSibling[c]) previous = c;
        if (previous < 0) _firstChild[p] = _nextSibling[slot];
        else _nextSibling[previous] = _nextSibling[slot];
        if (_lastChild[p] == slot) _lastChild[p] = previous;
        _parent[slot] = -1;
        _nextSibling[slot] = -1;
    }

    // ------------------------------------------------------------------ transforms and interpolation

    /// <inheritdoc/>
    public ref Transform Local(EntityId entity) => ref _local[Resolve(entity)];

    /// <inheritdoc/>
    public Matrix4x4 WorldMatrix(EntityId entity) => MatrixOf(Resolve(entity));

    private Matrix4x4 MatrixOf(int slot)
    {
        var m = Drawn(slot).ToMatrix();
        for (int p = _parent[slot]; p >= 0; p = _parent[p]) m *= Drawn(p).ToMatrix();
        return m;
    }

    /// <inheritdoc/>
    public Vector2 DrawPosition2D(EntityId entity)
    {
        int slot = Resolve(entity);
        var position = DrawnPosition2D(slot);
        for (int p = _parent[slot]; p >= 0; p = _parent[p])
        {
            // Translation-only parents (the 2D case) add their position; a rotated or scaled one needs the matrices.
            if (!_local[p].TranslationOnly || !_snap[p] && _blend < 1 && !_previous[p].TranslationOnly)
            {
                var t = MatrixOf(slot).Translation;
                return new Vector2(t.X, t.Y);
            }
            position += DrawnPosition2D(p);
        }
        return position;
    }

    private Vector2 DrawnPosition2D(int slot)
    {
        var current = _local[slot].Position2D;
        if (_blend >= 1 || _snap[slot]) return current;
        var previous = _previous[slot].Position2D;
        return previous == current ? current : Vector2.Lerp(previous, current, _blend);
    }

    private Transform Drawn(int slot) =>
        _blend >= 1 || _snap[slot] ? _local[slot] : Transform.Interpolate(_previous[slot], _local[slot], _blend);

    /// <inheritdoc/>
    public void Teleport(EntityId entity) => _snap[Resolve(entity)] = true;

    // ------------------------------------------------------------------ attachments

    /// <inheritdoc/>
    public T Attach<T>(EntityId entity, T attachment) where T : class
    {
        ArgumentNullException.ThrowIfNull(attachment);
        int slot = ResolveAlive(entity);
        StoreFor<T>().Set(slot, attachment);
        return attachment;
    }

    /// <inheritdoc/>
    public bool TryGet<T>(EntityId entity, [NotNullWhen(true)] out T? attachment) where T : class
    {
        uint slot = entity.Index;
        if (slot < (uint)_used && entity.Generation != 0 && _gen[slot] == entity.Generation && _state[slot] != Free
            && ExistingStore<T>() is { } store && store.TryGet((int)slot, out attachment)) return true;
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
    public bool Detach<T>(EntityId entity) where T : class
    {
        int slot = ResolveAlive(entity);
        return ExistingStore<T>() is { } store && store.Remove(slot);
    }

    /// <inheritdoc/>
    public Attachments<T> All<T>() where T : class => new(this, ExistingStore<T>());

    private AttachmentStore<T> StoreFor<T>() where T : class
    {
        int index = AttachmentType<T>.Index;
        if (index >= _stores.Length) Array.Resize(ref _stores, Math.Max(index + 1, _stores.Length * 2));
        if (_stores[index] is { } existing) return Unsafe.As<AttachmentStore<T>>(existing);
        var store = new AttachmentStore<T>();
        _stores[index] = store;
        _storeList.Add(store);
        return store;
    }

    private AttachmentStore<T>? ExistingStore<T>() where T : class
    {
        int index = AttachmentType<T>.Index;
        return index < _stores.Length ? Unsafe.As<AttachmentStore<T>?>(_stores[index]) : null;
    }

    // ------------------------------------------------------------------ state hash

    /// <summary>
    /// Adds the world's simulation state: the clock, then each entity in creation order (id, state, parent, local
    /// transform, and each attachment's type in the order the world first stored that type, followed by the
    /// attachment's own state when it is <see cref="IStateHashed"/>). Drawing never changes it.
    /// </summary>
    public void HashState(StateHasher hash)
    {
        hash.Add(Clock.Time);
        hash.Add(Clock.Scale);
        hash.Add(Clock.FreezeRemaining);
        hash.Add(_count);
        hash.Add(_orderCount);
        for (int i = 0; i < _orderCount; i++)
        {
            int slot = _order[i];
            hash.Add(slot);
            hash.Add((long)_gen[slot]);
            hash.Add(_state[slot]);
            hash.Add(_parent[slot]);
            ref var t = ref _local[slot];
            hash.Add(t.Position.X); hash.Add(t.Position.Y); hash.Add(t.Position.Z);
            hash.Add(t.Rotation.X); hash.Add(t.Rotation.Y); hash.Add(t.Rotation.Z); hash.Add(t.Rotation.W);
            hash.Add(t.Scale.X); hash.Add(t.Scale.Y); hash.Add(t.Scale.Z);
            foreach (var store in _storeList)
            {
                if (store.Find(slot) is not { } attachment) continue;
                hash.Add(store.TypeName);
                if (attachment is IStateHashed hashed) hashed.HashState(hash);
            }
        }
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
            foreach (var store in _storeList)
                if (store.Find(slot) is IDestroyedCallback callback) callback.OnDestroyed(id);
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
            _lastChild[slot] = -1;
            foreach (var store in _storeList) store.Remove(slot);
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

    // The fast path inlines into Local/DrawPosition2D; the throwing path stays out of line (with the throws inline a
    // Local read cost about 8 times more).
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
        if (slot >= (uint)_used || entity.Generation > _gen[slot]) throw new InvalidOperationException($"{entity} was not issued by this world.");
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
        Array.Resize(ref _lastChild, size);
        Array.Resize(ref _nextSibling, size);
    }
}
