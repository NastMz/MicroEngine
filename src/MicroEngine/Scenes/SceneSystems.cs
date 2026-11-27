using System.Diagnostics.CodeAnalysis;
using MicroEngine.Core;
using MicroEngine.Kernel;

namespace MicroEngine.Scenes;

/// <summary>
/// A part of a scene that lives with it: the engine starts it when the scene is on the stack, steps it with the scene
/// (<see cref="Update"/> before the scene's own update, <see cref="LateUpdate"/> after it), adds its state to the
/// scene's state hash and stops it when the scene leaves, with no call from the scene's own <c>Update</c>,
/// <c>HashState</c> or <c>Exit</c>. Add one to <see cref="Scenes.Scene.Systems"/>, or give every scene one through
/// <see cref="SceneManager.AddSystem{T}"/>. A component store, a physics world, a set of state machines and a
/// scheduler are scene systems.
/// </summary>
/// <example>
/// <code>
/// public sealed class Weather : SceneSystem
/// {
///     public float Rain { get; private set; }
///     protected override void Update(float dt) => Rain = MathF.Min(1, Rain + dt * 0.1f);
///     public override void HashState(StateHasher hash) => hash.Add(Rain);
/// }
///
/// // in a scene
/// public override void Enter() => Systems.Add(new Weather());
/// public override void Draw(IRenderer r) => DrawRain(r, Systems.Get&lt;Weather&gt;().Rain);
/// </code>
/// </example>
public abstract class SceneSystem : IStateHashed
{
    private Scene? _scene;
    internal bool Started;
    internal int Sequence;

    /// <summary>The scene this system belongs to.</summary>
    /// <exception cref="InvalidOperationException">The system is not in a scene (before it is added, after it was removed).</exception>
    public Scene Scene => _scene ?? throw new InvalidOperationException($"{GetType().Name} is not in a scene.");
    /// <summary>The scene's game. Available from <see cref="Start"/> on.</summary>
    public Game Game => Scene.Game;
    /// <summary>Whether the system is in a scene.</summary>
    public bool IsAttached => _scene is not null;

    /// <summary>
    /// Where the system runs among the scene's systems: lower first, and systems of the same order in the order they
    /// were added. Stopping runs in the reverse order. Read once, when the system is added. 0 by default.
    /// </summary>
    public virtual int Order => 0;

    internal void CallStart() => Start();
    internal void CallUpdate(float dt) => Update(dt);
    internal void CallLateUpdate(float dt) => LateUpdate(dt);
    internal void CallBeginDraw(float blend) => BeginDraw(blend);
    internal void CallStop() => Stop();

    internal void Bind(Scene scene) => _scene = scene;
    internal void Unbind()
    {
        _scene = null;
        Started = false;
    }

    /// <summary>
    /// Called once, when the scene is on the stack and has its game: before the scene's <see cref="Scenes.Scene.Enter"/>
    /// for a system added earlier, at once for one added from <see cref="Scenes.Scene.Enter"/> on.
    /// </summary>
    protected virtual void Start() { }

    /// <summary>
    /// Advances the system by one fixed step, after the scene's coroutines, tweens and widgets and before the scene's
    /// <see cref="Scenes.Scene.Update"/>. Not called while the scene is not updated (under a blocking overlay, during
    /// the covering half of a transition).
    /// </summary>
    /// <param name="dt">The step the scene's <see cref="Scenes.Scene.Update"/> receives: the step on the world's clock once the scene has objects, the engine's step otherwise.</param>
    protected virtual void Update(float dt) { }

    /// <summary>
    /// Called after the scene's <see cref="Scenes.Scene.Update"/> in the same step, before the scene's world releases
    /// what was destroyed: the place to apply what the step decided (deferred changes, positions written back).
    /// </summary>
    /// <param name="dt">The same step <see cref="Update"/> received.</param>
    protected virtual void LateUpdate(float dt) { }

    /// <summary>
    /// Called once per drawn frame before the scene's <see cref="Scenes.Scene.Draw"/>, for a system that prepares what
    /// is drawn (positions blended between two steps). It must not change simulation state: a run that draws no frame
    /// never calls it.
    /// </summary>
    /// <param name="blend">How far the frame lies between the last two steps (0 to 1); 1 when the scene was not updated in the latest step.</param>
    protected virtual void BeginDraw(float blend) { }

    /// <summary>
    /// Adds the system's simulation state to the scene's state hash, after the scene's coroutines, tweens, objects and
    /// widgets and before the scene's own <see cref="Scenes.Scene.HashState"/>. The engine adds the system's type name
    /// first. The default adds nothing.
    /// </summary>
    public virtual void HashState(StateHasher hash) { }

    /// <summary>
    /// Called once when the system is removed or its scene leaves the stack, after the scene's
    /// <see cref="Scenes.Scene.Exit"/>: release what the system owns. <see cref="Scene"/> is still readable.
    /// </summary>
    protected virtual void Stop() { }
}

/// <summary>
/// The systems of one scene (<see cref="Scene.Systems"/>), one per type. The scene steps, hashes and stops them; see
/// <see cref="SceneSystem"/>.
/// </summary>
public sealed class SceneSystems
{
    private readonly Scene _scene;
    private readonly List<SceneSystem> _items = [];
    private SceneSystem[] _run = [];
    private bool _runStale;
    private int _sequence;

    internal SceneSystems(Scene scene) => _scene = scene;

    /// <summary>How many systems the scene has.</summary>
    public int Count => _items.Count;

    /// <summary>
    /// Adds <paramref name="system"/> to the scene and returns it. It starts at once when the scene is on the stack,
    /// else when the scene is pushed. A system added while the scene's systems are being stepped takes part from the
    /// next call on.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="system"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The system is already in a scene, or the scene already has a system of that type.</exception>
    public T Add<T>(T system) where T : SceneSystem
    {
        ArgumentNullException.ThrowIfNull(system);
        if (system.IsAttached) throw new InvalidOperationException($"{system.GetType().Name} is already in a scene.");
        var type = system.GetType();
        foreach (var existing in _items)
            if (existing.GetType() == type)
                throw new InvalidOperationException($"{_scene.GetType().Name} already has a {type.Name} system.");
        system.Bind(_scene);
        system.Sequence = _sequence++;
        int order = system.Order;
        int at = _items.Count;
        while (at > 0 && _items[at - 1].Order > order) at--;
        _items.Insert(at, system);
        _runStale = true;
        if (_scene.OnStack) StartOne(system);
        return system;
    }

    /// <summary>The scene's system of type <typeparamref name="T"/>, added with its parameterless constructor when the scene has none.</summary>
    public T GetOrAdd<T>() where T : SceneSystem, new() => TryGet<T>(out var system) ? system : Add(new T());

    /// <summary>The scene's system of type <typeparamref name="T"/> (that type or one derived from it).</summary>
    /// <exception cref="InvalidOperationException">The scene has no such system.</exception>
    public T Get<T>() where T : SceneSystem =>
        TryGet<T>(out var system) ? system : throw new InvalidOperationException($"{_scene.GetType().Name} has no {typeof(T).Name} system.");

    /// <summary>Finds the scene's system of type <typeparamref name="T"/> (that type or one derived from it).</summary>
    public bool TryGet<T>([NotNullWhen(true)] out T? system) where T : SceneSystem
    {
        foreach (var item in _items)
            if (item is T match) { system = match; return true; }
        system = null;
        return false;
    }

    /// <summary>Whether the scene has a system of type <typeparamref name="T"/>.</summary>
    public bool Contains<T>() where T : SceneSystem => TryGet<T>(out _);

    /// <summary>Removes <paramref name="system"/> from the scene, stopping it when it had started; false when the scene does not have it.</summary>
    public bool Remove(SceneSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);
        if (!_items.Remove(system)) return false;
        _runStale = true;
        StopOne(system);
        return true;
    }

    private static void StartOne(SceneSystem system)
    {
        if (system.Started) return;
        system.Started = true;
        system.CallStart();
    }

    private static void StopOne(SceneSystem system)
    {
        try
        {
            if (system.Started) system.CallStop();
        }
        finally
        {
            system.Unbind();
        }
    }

    /// <summary>The systems in running order; rebuilt only after the set changed, so stepping allocates nothing.</summary>
    private SceneSystem[] Run()
    {
        if (!_runStale) return _run;
        _runStale = false;
        return _run = [.. _items];
    }

    internal void StartAll()
    {
        foreach (var system in Run()) if (system.IsAttached) StartOne(system);
    }

    internal void Update(float dt)
    {
        foreach (var system in Run()) if (system.Started) system.CallUpdate(dt);
    }

    internal void LateUpdate(float dt)
    {
        foreach (var system in Run()) if (system.Started) system.CallLateUpdate(dt);
    }

    internal void BeginDraw(float blend)
    {
        foreach (var system in Run()) if (system.Started) system.CallBeginDraw(blend);
    }

    internal void HashState(StateHasher hash)
    {
        // A scene without systems hashes exactly as it did before systems existed.
        if (_items.Count == 0) return;
        hash.Add(_items.Count);
        foreach (var system in _items)
        {
            hash.Add(system.GetType().FullName);
            system.HashState(hash);
        }
    }

    /// <summary>Stops every system in reverse order and empties the list; every system is stopped even when one throws.</summary>
    internal void StopAll()
    {
        var stopping = Run();
        _items.Clear();
        _runStale = true;
        List<Exception>? errors = null;
        for (int i = stopping.Length - 1; i >= 0; i--)
        {
            if (!stopping[i].IsAttached) continue;
            try { StopOne(stopping[i]); }
            catch (Exception e) { (errors ??= []).Add(e); }
        }
        if (errors is [var only]) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(only).Throw();
        if (errors is not null) throw new AggregateException(errors);
    }
}

/// <summary>
/// Calls actions after a time or at intervals, on the scene's time (<see cref="Scene.Timers"/>): the scene's own
/// <see cref="Timers"/>, stepped for it. Timers advance before the scene's <see cref="Scene.Update"/> by the step it
/// receives, so they slow down and freeze with the world's clock; an unscaled timer advances by
/// <see cref="Scene.EngineStep"/> instead. They stop with the scene, and their state is part of its state hash. For a
/// sequence of steps over time use a coroutine (<see cref="Scene.Coroutines"/>); for the rules of a game kept outside
/// a scene, create a <see cref="Timers"/> and step it yourself.
/// </summary>
/// <example>
/// <code>
/// Timers.After(2f, SpawnBoss);
/// var drip = Timers.Every(0.5f, SpawnDrop);
/// // later
/// Timers.Cancel(drip);
/// </code>
/// </example>
public sealed class SceneTimers : SceneSystem
{
    private readonly Timers _timers = new();

    /// <summary>How many timers are waiting.</summary>
    public int Count => _timers.Count;

    /// <summary>Calls <paramref name="action"/> once, <paramref name="seconds"/> from now, on the update on which the time reaches them (<see cref="Timers"/> gives the rule).</summary>
    /// <param name="seconds">The delay; 0 calls it in the scene's next step.</param>
    /// <param name="action">What to call.</param>
    /// <param name="unscaled">True to count the engine's step, so the timer runs through slow motion and a frozen world.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="seconds"/> is negative or not a number.</exception>
    public TimerHandle After(float seconds, Action action, bool unscaled = false) => _timers.After(seconds, action, unscaled);

    /// <summary>Calls <paramref name="action"/> every <paramref name="seconds"/>, the first time one interval from now.</summary>
    /// <param name="seconds">The interval, above zero. A step longer than the interval calls the action once per interval that passed.</param>
    /// <param name="action">What to call.</param>
    /// <param name="times">How many calls before the timer ends; 0 (the default) repeats until it is cancelled.</param>
    /// <param name="unscaled">True to count the engine's step, so the timer runs through slow motion and a frozen world.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="seconds"/> is not above zero, or <paramref name="times"/> is negative.</exception>
    public TimerHandle Every(float seconds, Action action, int times = 0, bool unscaled = false) => _timers.Every(seconds, action, times, unscaled);

    /// <summary>Calls <paramref name="action"/> once, on the <paramref name="steps"/>-th step of the scene from now; a step of zero seconds (the world frozen) is not counted.</summary>
    /// <param name="steps">The delay in steps; 0 and 1 both call it in the scene's next step.</param>
    /// <param name="action">What to call.</param>
    /// <param name="unscaled">True to count every step of the engine, so the timer runs through a frozen world.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="steps"/> is negative.</exception>
    public TimerHandle AfterSteps(int steps, Action action, bool unscaled = false) => _timers.AfterSteps(steps, action, unscaled);

    /// <summary>Calls <paramref name="action"/> every <paramref name="steps"/> steps of the scene, the first time that many steps from now.</summary>
    /// <param name="steps">The interval in steps, above zero.</param>
    /// <param name="action">What to call.</param>
    /// <param name="times">How many calls before the timer ends; 0 (the default) repeats until it is cancelled.</param>
    /// <param name="unscaled">True to count every step of the engine, so the timer runs through a frozen world.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="steps"/> is not above zero, or <paramref name="times"/> is negative.</exception>
    public TimerHandle EverySteps(int steps, Action action, int times = 0, bool unscaled = false) => _timers.EverySteps(steps, action, times, unscaled);

    /// <summary>Cancels a timer; false when it already ended or was cancelled. A timer may cancel itself from its action.</summary>
    public bool Cancel(TimerHandle handle) => _timers.Cancel(handle);

    /// <summary>Whether <paramref name="handle"/> names a timer that is still waiting.</summary>
    public bool IsActive(TimerHandle handle) => _timers.IsActive(handle);

    /// <summary>What is left before the next call of a timer, in seconds or in steps as it was added; 0 when it ended or was cancelled.</summary>
    public float Remaining(TimerHandle handle) => _timers.Remaining(handle);

    /// <summary>Cancels every timer.</summary>
    public void Clear() => _timers.Clear();

    /// <inheritdoc/>
    protected override void Update(float dt) => _timers.Update(dt, Scene.EngineStep);

    /// <inheritdoc/>
    public override void HashState(StateHasher hash) => _timers.HashState(hash);

    /// <inheritdoc/>
    protected override void Stop() => _timers.Clear();
}
