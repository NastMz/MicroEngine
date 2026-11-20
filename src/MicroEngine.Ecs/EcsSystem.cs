using System.Diagnostics.CodeAnalysis;
using Arch.Buffer;
using MicroEngine.Core;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace MicroEngine.Ecs;

using World = Arch.Core.World;

/// <summary>
/// One behaviour of a store: a class with the queries it walks and an <see cref="Update"/> the store calls once per
/// step, in the order the systems were added (<see cref="EcsStore.Systems"/> before the scene's update,
/// <see cref="EcsStore.LateSystems"/> after it). What it asks of <see cref="Commands"/> is applied when its update
/// returns, before the next system runs.
/// </summary>
/// <example>
/// <code>
/// public sealed class Movement : EcsSystem
/// {
///     private static readonly QueryDescription Moving = new QueryDescription().WithAll&lt;Position, Velocity&gt;().WithNone&lt;Frozen&gt;();
///     protected override void Update(float dt)
///     {
///         foreach (ref var chunk in World.Query(in Moving))
///         {
///             var positions = chunk.GetSpan&lt;Position&gt;();
///             var velocities = chunk.GetSpan&lt;Velocity&gt;();
///             for (int i = 0; i &lt; chunk.Count; i++) positions[i].Value += velocities[i].Value * dt;
///         }
///     }
/// }
/// </code>
/// </example>
public abstract class EcsSystem
{
    private EcsStore? _store;
    internal EcsSystems? List;
    internal bool Started;

    /// <summary>The store this system belongs to.</summary>
    /// <exception cref="InvalidOperationException">The system is not in a store.</exception>
    public EcsStore Store => _store ?? throw new InvalidOperationException($"{Name} is not in a store: add it to EcsStore.Systems or EcsStore.LateSystems.");
    /// <summary>The store's rows: the library's world. Available from <see cref="Start"/> on.</summary>
    public World World => Store.World;
    /// <summary>The store's deferred changes (<see cref="EcsStore.Commands"/>).</summary>
    public CommandBuffer Commands => Store.Commands;
    /// <summary>The store's scene.</summary>
    public Scene Scene => Store.Scene;
    /// <summary>The scene's game.</summary>
    public Game Game => Store.Game;

    /// <summary>The system's name within its list, unique there; the type's name unless overridden.</summary>
    public virtual string Name => GetType().Name;

    /// <summary>Whether the store calls <see cref="Update"/>. Simulation state: part of the state hash.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Called once, when the store has its rows (its scene is on the stack): at once for a system added later.</summary>
    protected virtual void Start() { }

    /// <summary>Advances the behaviour by one step.</summary>
    /// <param name="dt">The step the scene's <see cref="Scenes.Scene.Update"/> receives.</param>
    protected abstract void Update(float dt);

    /// <summary>Adds what the system itself keeps between steps (a counter, a timer) to the state hash. The default adds nothing.</summary>
    public virtual void HashState(StateHasher hash) { }

    internal void Bind(EcsStore store, EcsSystems list) => (_store, List) = (store, list);
    internal void Unbind() => (_store, List, Started) = (null, null, false);
    internal void CallStart()
    {
        if (Started) return;
        Started = true;
        Start();
    }
    internal void CallUpdate(float dt) => Update(dt);
}

/// <summary>
/// The systems of one place in a store's step, in the order they run (<see cref="EcsStore.Systems"/>,
/// <see cref="EcsStore.LateSystems"/>). The order is the order of the <c>Add</c> calls: it is written once, where the
/// scene is set up, and read there.
/// </summary>
public sealed class EcsSystems
{
    private sealed class Step(string name, Action<EcsStore, float> update) : EcsSystem
    {
        public override string Name => name;
        protected override void Update(float dt) => update(Store, dt);
    }

    private readonly EcsStore _store;
    private readonly string _what;
    private readonly List<EcsSystem> _items = [];

    internal EcsSystems(EcsStore store, string what) => (_store, _what) = (store, what);

    /// <summary>How many systems the list has.</summary>
    public int Count => _items.Count;

    /// <summary>Adds <paramref name="system"/> after the ones already there and returns it.</summary>
    /// <exception cref="InvalidOperationException">The system is already in a list, or the list has a system of that name.</exception>
    public T Add<T>(T system) where T : EcsSystem
    {
        ArgumentNullException.ThrowIfNull(system);
        if (system.List is not null) throw new InvalidOperationException($"{system.Name} is already in a store's systems.");
        string name = system.Name;
        foreach (var existing in _items)
            if (existing.Name == name) throw new InvalidOperationException($"{_what} already has a system named {name}.");
        system.Bind(_store, this);
        _items.Add(system);
        if (_store.IsRunning) system.CallStart();
        return system;
    }

    /// <summary>Adds a system that is one function, for a behaviour too small for a class, and returns it (to switch it off, or to remove it).</summary>
    /// <param name="name">Its name within the list.</param>
    /// <param name="update">Called once per step with the store and the step.</param>
    /// <exception cref="InvalidOperationException">The list has a system of that name.</exception>
    public EcsSystem Add(string name, Action<EcsStore, float> update)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(update);
        return Add(new Step(name, update));
    }

    /// <summary>The list's system of type <typeparamref name="T"/> (that type or one derived from it).</summary>
    /// <exception cref="InvalidOperationException">The list has no such system.</exception>
    public T Get<T>() where T : EcsSystem
    {
        foreach (var item in _items)
            if (item is T match) return match;
        throw new InvalidOperationException($"{_what} has no {typeof(T).Name} system.");
    }

    /// <summary>The list's system named <paramref name="name"/>.</summary>
    /// <exception cref="InvalidOperationException">The list has no such system.</exception>
    public EcsSystem this[string name] =>
        TryGet(name, out var system) ? system : throw new InvalidOperationException($"{_what} has no system named {name}.");

    /// <summary>Finds the list's system named <paramref name="name"/>.</summary>
    public bool TryGet(string name, [NotNullWhen(true)] out EcsSystem? system)
    {
        foreach (var item in _items)
            if (item.Name == name) { system = item; return true; }
        system = null;
        return false;
    }

    /// <summary>Removes <paramref name="system"/>; false when the list does not have it.</summary>
    public bool Remove(EcsSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);
        if (!_items.Remove(system)) return false;
        system.Unbind();
        return true;
    }

    internal void StartAll()
    {
        // By index: a system may add another one from its Start.
        for (int i = 0; i < _items.Count; i++) _items[i].CallStart();
    }

    internal void StopAll()
    {
        foreach (var item in _items) item.Started = false;
    }

    internal void Run(float dt)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            var system = _items[i];
            if (!system.Enabled) continue;
            system.CallUpdate(dt);
            _store.Flush();
        }
    }

    internal void HashState(StateHasher hash)
    {
        hash.Add(_items.Count);
        foreach (var item in _items)
        {
            hash.Add(item.Name);
            hash.Add(item.Enabled);
            item.HashState(hash);
        }
    }
}
