namespace MicroEngine.Kernel;

/// <summary>
/// The systems that run each frame: named callbacks placed in a <see cref="FramePhase"/>, run in phase order. Within a
/// phase, systems run in the order they were added unless a system asks to run after or before another one of the same
/// phase; the order is resolved once, when the engine is built, and a cycle of such constraints is an error that names
/// the systems in it.
/// </summary>
/// <remarks>
/// Modules add systems while the engine is configured (<see cref="EngineBuilder"/>); once it is built the schedule is
/// fixed and adding, replacing or removing a system throws. Running it allocates nothing.
/// </remarks>
public sealed class FrameSchedule
{
    private sealed class Entry(FramePhase phase, string name, Action<FrameContext> run, string? after, string? before, int order)
    {
        public FramePhase Phase { get; } = phase;
        public string Name { get; } = name;
        public Action<FrameContext> Run { get; set; } = run;
        public string? After { get; } = after;
        public string? Before { get; } = before;
        public int Order { get; } = order;
    }

    private static readonly int PhaseCount = Enum.GetValues<FramePhase>().Length;
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private Action<FrameContext>[][]? _compiled;
    private string[][]? _names;
    private double[][]? _times;
    private bool _measure;
    private int _added;

    /// <summary>Whether the schedule is fixed (the engine was built): it can run but no longer change.</summary>
    public bool IsFrozen { get; private set; }

    /// <summary>The number of systems scheduled.</summary>
    public int Count => _entries.Count;

    /// <summary>Whether <paramref name="phase"/> runs once per fixed step (<see cref="FramePhase.PreFixed"/> to <see cref="FramePhase.PostFixed"/>) rather than once per frame.</summary>
    public static bool IsFixed(FramePhase phase) => phase is FramePhase.PreFixed or FramePhase.FixedUpdate or FramePhase.Physics or FramePhase.PostFixed;

    /// <summary>
    /// Schedules <paramref name="run"/> in <paramref name="phase"/> under <paramref name="name"/>, optionally after or
    /// before another system of the same phase.
    /// </summary>
    /// <param name="phase">The phase it runs in.</param>
    /// <param name="name">A name unique in the whole schedule, such as "engine.scenes" or "mygame.camera".</param>
    /// <param name="run">The system; it receives the frame's <see cref="FrameContext"/>.</param>
    /// <param name="after">The name of a system of the same phase this one must run after, or null.</param>
    /// <param name="before">The name of a system of the same phase this one must run before, or null.</param>
    /// <returns>This schedule, to chain calls.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty, or a system with that name is already scheduled.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="run"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="phase"/> is not a <see cref="FramePhase"/>.</exception>
    /// <exception cref="InvalidOperationException">The schedule is frozen.</exception>
    /// <remarks>Constraints naming a system that does not exist, or one in another phase, are reported when the engine is built.</remarks>
    public FrameSchedule Add(FramePhase phase, string name, Action<FrameContext> run, string? after = null, string? before = null)
    {
        ThrowIfFrozen();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(run);
        if (!Enum.IsDefined(phase)) throw new ArgumentOutOfRangeException(nameof(phase), phase, "Not a frame phase.");
        if (_entries.TryGetValue(name, out var existing))
            throw new ArgumentException($"A frame system named '{name}' is already scheduled (in {existing.Phase}); use Replace to change what it runs.", nameof(name));
        _entries.Add(name, new Entry(phase, name, run, after, before, _added++));
        return this;
    }

    /// <summary>Replaces what the system <paramref name="name"/> runs, keeping its phase and order: the way to substitute a built-in system.</summary>
    /// <exception cref="KeyNotFoundException">No system is named <paramref name="name"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="run"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The schedule is frozen.</exception>
    public FrameSchedule Replace(string name, Action<FrameContext> run)
    {
        ThrowIfFrozen();
        ArgumentNullException.ThrowIfNull(run);
        if (!_entries.TryGetValue(name, out var entry)) throw new KeyNotFoundException($"No frame system named '{name}' is scheduled.");
        entry.Run = run;
        return this;
    }

    /// <summary>Removes the system <paramref name="name"/>; false when there was none. Constraints of other systems that name it then fail when the engine is built.</summary>
    /// <exception cref="InvalidOperationException">The schedule is frozen.</exception>
    public bool Remove(string name)
    {
        ThrowIfFrozen();
        return _entries.Remove(name);
    }

    /// <summary>Whether a system named <paramref name="name"/> is scheduled.</summary>
    public bool Contains(string name) => _entries.ContainsKey(name);

    /// <summary>The phase of the system <paramref name="name"/>, or null when there is none.</summary>
    public FramePhase? PhaseOf(string name) => _entries.TryGetValue(name, out var entry) ? entry.Phase : null;

    /// <summary>The names of the systems of <paramref name="phase"/> in the order they run, resolving the order if needed.</summary>
    /// <exception cref="InvalidOperationException">A constraint names a missing system or one in another phase, or the constraints form a cycle.</exception>
    public IReadOnlyList<string> Order(FramePhase phase)
    {
        Compile();
        return _names![(int)phase];
    }

    /// <summary>Runs the systems of <paramref name="phase"/>, in order, with <paramref name="context"/>.</summary>
    /// <exception cref="InvalidOperationException">The order cannot be resolved (see <see cref="Order"/>).</exception>
    /// <remarks>An exception thrown by a system stops the phase and reaches the caller.</remarks>
    public void Run(FramePhase phase, in FrameContext context)
    {
        if (_compiled is null) Compile();
        var systems = _compiled![(int)phase];
        if (!MeasureTime)
        {
            foreach (var system in systems) system(context);
            return;
        }
        var times = _times![(int)phase];
        for (int i = 0; i < systems.Length; i++)
        {
            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            systems[i](context);
            times[i] += System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
    }

    /// <summary>
    /// Whether <see cref="Run"/> measures how long each system takes (<see cref="Times"/>). Off by default: a schedule
    /// that does not measure takes no timestamps. It may change once the schedule is frozen, since it changes no system.
    /// </summary>
    /// <exception cref="InvalidOperationException">Turning it on when the order cannot be resolved (see <see cref="Order"/>).</exception>
    public bool MeasureTime
    {
        get => _measure;
        set
        {
            if (value) Compile();
            _measure = value;
        }
    }

    /// <summary>
    /// The milliseconds each system of <paramref name="phase"/> took since the last <see cref="ResetTimes"/>, aligned with
    /// <see cref="Order"/>: a fixed phase adds up every step it ran. All zero while <see cref="MeasureTime"/> was off.
    /// </summary>
    /// <exception cref="InvalidOperationException">The order cannot be resolved (see <see cref="Order"/>).</exception>
    public ReadOnlySpan<double> Times(FramePhase phase)
    {
        Compile();
        return _times![(int)phase];
    }

    /// <summary>Sets every measured time (<see cref="Times"/>) back to zero; the loop does it at the start of each frame.</summary>
    public void ResetTimes()
    {
        if (_times is null) return;
        foreach (var phase in _times) Array.Clear(phase);
    }

    /// <summary>Resolves the order and fixes the schedule; the engine does this when it is built.</summary>
    /// <exception cref="InvalidOperationException">The order cannot be resolved (see <see cref="Order"/>).</exception>
    public void Freeze()
    {
        Compile();
        IsFrozen = true;
    }

    private void ThrowIfFrozen()
    {
        if (IsFrozen) throw new InvalidOperationException("The frame schedule is fixed once the engine is built; add, replace or remove systems while configuring it.");
        _compiled = null;
        _names = null;
    }

    private void Compile()
    {
        if (_compiled is not null) return;
        var compiled = new Action<FrameContext>[PhaseCount][];
        var names = new string[PhaseCount][];
        var times = new double[PhaseCount][];
        for (int p = 0; p < PhaseCount; p++)
        {
            var ordered = Resolve((FramePhase)p);
            compiled[p] = ordered.Select(e => e.Run).ToArray();
            names[p] = ordered.Select(e => e.Name).ToArray();
            times[p] = new double[ordered.Count];
        }
        (_compiled, _names, _times) = (compiled, names, times);
    }

    /// <summary>
    /// A stable order: systems are taken in the order they were added, each one preceded by the systems it must follow
    /// (depth first), so a constraint moves only the systems it names and everything else keeps its insertion order.
    /// </summary>
    private List<Entry> Resolve(FramePhase phase)
    {
        var systems = _entries.Values.Where(e => e.Phase == phase).OrderBy(e => e.Order).ToList();
        var previous = systems.ToDictionary(e => e.Name, _ => new List<Entry>(), StringComparer.Ordinal);
        foreach (var system in systems)
        {
            if (system.After is { } after) previous[system.Name].Add(Find(system, after, "after"));
            if (system.Before is { } before) previous[Find(system, before, "before").Name].Add(system);
        }
        foreach (var list in previous.Values) list.Sort((a, b) => a.Order.CompareTo(b.Order));

        var ordered = new List<Entry>(systems.Count);
        var done = new HashSet<string>(StringComparer.Ordinal);
        var path = new List<string>();
        foreach (var system in systems) Visit(system);
        return ordered;

        void Visit(Entry system)
        {
            if (done.Contains(system.Name)) return;
            int at = path.IndexOf(system.Name);
            if (at >= 0)
            {
                // The walk goes from a system to the ones it follows: reversed, the path is the running order.
                var cycle = path.Skip(at).Reverse().ToList();
                cycle.Add(cycle[0]);
                throw new InvalidOperationException(
                    $"Frame systems in {phase} form a cycle: {string.Join(" -> ", cycle)}. Remove one of their after/before constraints.");
            }
            path.Add(system.Name);
            foreach (var first in previous[system.Name]) Visit(first);
            path.RemoveAt(path.Count - 1);
            done.Add(system.Name);
            ordered.Add(system);
        }

        Entry Find(Entry system, string other, string relation)
        {
            if (!_entries.TryGetValue(other, out var target))
                throw new InvalidOperationException($"Frame system '{system.Name}' ({phase}) runs {relation} '{other}', but no system named '{other}' is scheduled.");
            if (target.Phase != phase)
                throw new InvalidOperationException($"Frame system '{system.Name}' ({phase}) runs {relation} '{other}', which is in {target.Phase}; after and before order systems within one phase.");
            return target;
        }
    }
}

