using MicroEngine.Kernel;

namespace MicroEngine;

/// <summary>The event type of a state machine that has no events (<see cref="StateMachine{TState}"/>).</summary>
public enum NoEvent { }

/// <summary>Something stepped like a state machine: what a scene's <c>StateMachines</c> system runs.</summary>
public interface IStateMachine : IStateHashed
{
    /// <summary>Advances the machine by one step of <paramref name="dt"/> seconds.</summary>
    void Update(float dt);
}

/// <summary>A machine's state and how long it has been in it, to save and restore (<see cref="StateMachine{TState, TEvent}.Restore"/>).</summary>
/// <param name="State">The state.</param>
/// <param name="Time">Seconds in that state.</param>
/// <param name="Steps">Updates in that state (<see cref="StateMachine{TState, TEvent}.Steps"/>).</param>
public readonly record struct MachineSnapshot<TState>(TState State, float Time, int Steps = 0);

/// <summary>A state machine without events: its states change by conditions, by time and by <c>Go</c>.</summary>
/// <typeparam name="TState">The states, usually an enum.</typeparam>
public sealed class StateMachine<TState> : StateMachine<TState, NoEvent> where TState : notnull
{
    /// <summary>Creates a machine that starts in <paramref name="initial"/>.</summary>
    public StateMachine(TState initial) : base(initial) { }
}

/// <summary>
/// A state machine for gameplay: a character's moves, an enemy's behaviour, a boss's phases, the flow of a level. Each
/// state has what runs when it is entered, every step while it lasts and when it is left, and the ways out of it: a
/// condition checked every step (<c>When</c>), a time in the state in seconds (<c>After</c>) or in updates
/// (<c>AfterSteps</c>) or an event the game fires (<c>On</c>). The machine keeps the seconds and the updates spent in
/// the current state, runs on whatever step it is given (so it follows a scene's clock through slow motion and a
/// hitstop), needs no scene (the owner of a rule object calls <see cref="Update"/>), takes part in the state hash and
/// can be saved and restored. Stepping and firing allocate nothing.
/// </summary>
/// <remarks>
/// <para>
/// One step (<see cref="Update"/>) adds the step to the time, runs the update of the current state (its parents'
/// first), then takes at most one way out: the current state's, in the order they were declared, then its parents',
/// then those declared on <see cref="Any"/>. That order is the priority of an interruption.
/// </para>
/// <para>
/// A change runs to completion: the exits, then the entries, then <see cref="Changed"/>. An event fired or a state
/// asked for from inside one of those handlers waits and is applied right after, in the order asked, so a handler
/// never sees the machine half changed.
/// </para>
/// <para>
/// A state may be a part of another (<c>SubstateOf</c>): the parent's ways out and events apply to all its parts, and
/// its entry and exit run only when the machine crosses its border.
/// </para>
/// </remarks>
/// <typeparam name="TState">The states, usually an enum.</typeparam>
/// <typeparam name="TEvent">The events the game fires, usually an enum.</typeparam>
/// <example>
/// <code>
/// var enemy = new StateMachine&lt;Enemy, Hit&gt;(Enemy.Patrol);
/// enemy.State(Enemy.Patrol)
///     .OnUpdate(dt => Walk(dt))
///     .When(() => SeesPlayer, Enemy.Chase);
/// enemy.State(Enemy.Chase)
///     .OnEnter(() => Audio.Play("alert"))
///     .OnUpdate(dt => RunAtPlayer(dt))
///     .When(() => !SeesPlayer, Enemy.Patrol);
/// enemy.State(Enemy.Stunned).After(0.8f, Enemy.Patrol);
/// enemy.Any.On(Hit.Stomp, Enemy.Stunned);
///
/// enemy.Update(dt);          // every step
/// enemy.Fire(Hit.Stomp);     // when it happens
/// </code>
/// </example>
public class StateMachine<TState, TEvent> : IStateMachine
    where TState : notnull
    where TEvent : notnull
{
    // A time counts as reached within this much of it: the sum of single-precision steps is not the exact product, and
    // the error grows with the time. Far under one step for any time a game uses, so no time is reached early.
    private const double Epsilon = 1e-5;
    private const double RelativeEpsilon = 5e-7;

    internal sealed class Node(TState? id, int index)
    {
        public readonly TState? Id = id;
        public readonly int Index = index;
        public Node? Parent;
        public Action? Enter, Exit;
        public Action<float>? Update;
        public readonly List<Rule> Rules = [];
        public readonly List<EventRule> Events = [];
        public double EnteredAt;
        public long EnteredStep;
    }

    internal readonly record struct Rule(Func<bool>? When, float After, int Steps, Node Target);
    internal readonly record struct EventRule(TEvent Event, Node? Target, Func<bool>? Guard, Action? Action);
    private readonly record struct Request(bool IsEvent, TEvent? Event, Node? Target);

    private readonly Dictionary<TState, Node> _nodes = [];
    private readonly List<Node> _path = [];
    private readonly Queue<Request> _waiting = new();
    private readonly Node _any = new(default, -1);
    private readonly EqualityComparer<TEvent> _events = EqualityComparer<TEvent>.Default;
    private Node _current;
    private TState _previous;
    private double _clock;
    private long _steps;
    private bool _started;
    private bool _busy;
    private int _changes;

    /// <summary>Creates a machine that starts in <paramref name="initial"/>. Its entry runs at the first step, the first event or <see cref="Start"/>, so the states can be declared first.</summary>
    public StateMachine(TState initial)
    {
        ArgumentNullException.ThrowIfNull(initial);
        _current = NodeOf(initial);
        _previous = initial;
        Any = new StateConfig(this, _any);
    }

    /// <summary>The current state.</summary>
    public TState Current => _current.Id!;
    /// <summary>The state before the latest change; the initial state until the first one.</summary>
    public TState Previous => _previous;
    /// <summary>Seconds spent in the current state: 0 when it is entered, then the sum of the steps since.</summary>
    public float Time => (float)(_clock - _current.EnteredAt);
    /// <summary>Updates spent in the current state: 0 when it is entered, then one more for every update of more than zero seconds.</summary>
    public int Steps => (int)(_steps - _current.EnteredStep);
    /// <summary>How many changes of state the machine made.</summary>
    public int Changes => _changes;
    /// <summary>Whether the initial state was entered.</summary>
    public bool IsStarted => _started;

    /// <summary>Raised after every change, with the state left and the state entered, once the entries ran.</summary>
    public event Action<TState, TState>? Changed;
    /// <summary>Raised when an event is fired that no way out of the current state, its parents or <see cref="Any"/> takes.</summary>
    public event Action<TState, TEvent>? Unhandled;
    /// <summary>When true, an event nothing takes throws <see cref="InvalidOperationException"/> instead of being ignored. False by default.</summary>
    public bool ThrowOnUnhandled { get; set; }

    /// <summary>What applies in every state, after the state's own and its parents': ways out and events that interrupt anything (a hit that stuns, a death).</summary>
    public StateConfig Any { get; }

    /// <summary>Declares or continues declaring <paramref name="state"/>.</summary>
    public StateConfig State(TState state) => new(this, NodeOf(state));

    /// <summary>Whether the machine is in <paramref name="state"/> or in one of its parts.</summary>
    public bool IsIn(TState state)
    {
        for (var node = _current; node is not null; node = node.Parent)
            if (EqualityComparer<TState>.Default.Equals(node.Id, state)) return true;
        return false;
    }

    /// <summary>Seconds since <paramref name="state"/> was entered while the machine is in it or in one of its parts; 0 otherwise.</summary>
    public float TimeIn(TState state)
    {
        for (var node = _current; node is not null; node = node.Parent)
            if (EqualityComparer<TState>.Default.Equals(node.Id, state)) return (float)(_clock - node.EnteredAt);
        return 0;
    }

    /// <summary>Updates since <paramref name="state"/> was entered while the machine is in it or in one of its parts; 0 otherwise.</summary>
    public int StepsIn(TState state)
    {
        for (var node = _current; node is not null; node = node.Parent)
            if (EqualityComparer<TState>.Default.Equals(node.Id, state)) return (int)(_steps - node.EnteredStep);
        return 0;
    }

    /// <summary>
    /// Sets the seconds spent in the current state, as if it had been entered that long ago: a head start
    /// (<c>Go</c> then <c>SetTime</c>), a state held at a moment while something else finishes, a countdown put back.
    /// Its ways out by time count from the new value; nothing runs. The parents of the state keep their own time, and
    /// <see cref="Steps"/> does not change.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="seconds"/> is negative or not a number.</exception>
    public void SetTime(float seconds)
    {
        if (!(seconds >= 0) || float.IsInfinity(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A time is zero or more seconds.");
        _current.EnteredAt = _clock - seconds;
    }

    /// <summary>
    /// Sets the updates spent in the current state (<see cref="Steps"/>), as <see cref="SetTime"/> sets its seconds.
    /// Its ways out by steps count from the new value. The parents of the state keep their own.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="steps"/> is negative.</exception>
    public void SetSteps(int steps)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(steps);
        _current.EnteredStep = _steps - steps;
    }

    /// <summary>Enters the initial state now (its parents' entries first). Does nothing once started.</summary>
    public void Start()
    {
        if (_started) return;
        _started = true;
        Run(() => EnterFrom(null, _current));
    }

    /// <summary>Advances the machine by one step: see the remarks of <see cref="StateMachine{TState, TEvent}"/>.</summary>
    /// <param name="dt">The step in seconds; 0 runs the updates and the conditions without moving the time.</param>
    /// <exception cref="InvalidOperationException">Called from one of the machine's own handlers.</exception>
    public void Update(float dt)
    {
        if (_busy) throw new InvalidOperationException("A state machine cannot be updated from one of its own handlers.");
        Start();
        _clock += dt;
        if (dt > 0) _steps++;
        int before = _changes;
        _busy = true;
        try { UpdatePath(_current, dt); }
        finally { _busy = false; }
        Drain();
        // A handler that changed the state already took this step's way out.
        if (_changes != before) return;
        if (FindRule() is { } target) Change(target);
    }

    private void UpdatePath(Node node, float dt)
    {
        if (node.Parent is { } parent) UpdatePath(parent, dt);
        node.Update?.Invoke(dt);
    }

    private Node? FindRule()
    {
        for (var node = _current; node is not null; node = node.Parent)
            if (FirstRule(node, node) is { } found) return found;
        return FirstRule(_any, _current);
    }

    private Node? FirstRule(Node owner, Node timed)
    {
        foreach (var rule in owner.Rules)
        {
            if (rule.After >= 0 && _clock - timed.EnteredAt < rule.After - Math.Max(Epsilon, rule.After * RelativeEpsilon)) continue;
            if (rule.Steps >= 0 && _steps - timed.EnteredStep < rule.Steps) continue;
            if (rule.When is { } when && !when()) continue;
            return rule.Target;
        }
        return null;
    }

    /// <summary>
    /// Fires <paramref name="trigger"/>: the first way out that takes it (the current state's, then its parents', then
    /// <see cref="Any"/>'s) changes the state or runs its action. Fired from one of the machine's handlers, it waits
    /// until the change in progress is complete.
    /// </summary>
    /// <returns>False when nothing took the event; true when it was taken or is waiting.</returns>
    /// <exception cref="InvalidOperationException">Nothing took the event and <see cref="ThrowOnUnhandled"/> is set.</exception>
    public bool Fire(TEvent trigger)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        if (_busy) { _waiting.Enqueue(new Request(true, trigger, null)); return true; }
        Start();
        bool taken = FireNow(trigger);
        Drain();
        return taken;
    }

    private bool FireNow(TEvent trigger)
    {
        for (var node = _current; node is not null; node = node.Parent)
            if (TryEvents(node, trigger)) return true;
        if (TryEvents(_any, trigger)) return true;
        Unhandled?.Invoke(Current, trigger);
        if (ThrowOnUnhandled) throw new InvalidOperationException($"Nothing takes the event {trigger} in the state {Current}.");
        return false;
    }

    private bool TryEvents(Node node, TEvent trigger)
    {
        foreach (var rule in node.Events)
        {
            if (!_events.Equals(rule.Event, trigger)) continue;
            if (rule.Guard is { } guard && !guard()) continue;
            if (rule.Action is { } action) Run(action);
            if (rule.Target is { } target) ChangeNow(target);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Changes to <paramref name="state"/> whatever the declared ways out say, running the exits and the entries.
    /// Going to the current state leaves and enters it again. Called from one of the machine's handlers, it waits until
    /// the change in progress is complete.
    /// </summary>
    public void Go(TState state)
    {
        var target = NodeOf(state);
        if (_busy) { _waiting.Enqueue(new Request(false, default, target)); return; }
        Start();
        Change(target);
    }

    /// <summary>The current state, its time and its steps, to write into a save.</summary>
    public MachineSnapshot<TState> Snapshot() => new(Current, Time, Steps);

    /// <summary>
    /// Puts the machine in the state of <paramref name="snapshot"/> with its time and steps, without running any exit or
    /// entry: what loading a save does. The parents of the state take the same time and steps. The machine counts as
    /// started, and its ways out by time and by steps are taken on the update the saved machine would have taken them.
    /// </summary>
    /// <exception cref="InvalidOperationException">Called from one of the machine's own handlers.</exception>
    public void Restore(MachineSnapshot<TState> snapshot)
    {
        if (_busy) throw new InvalidOperationException("A state machine cannot be restored from one of its own handlers.");
        _current = NodeOf(snapshot.State);
        _previous = snapshot.State;
        _started = true;
        _waiting.Clear();
        for (var node = _current; node is not null; node = node.Parent)
        {
            node.EnteredAt = _clock - snapshot.Time;
            node.EnteredStep = _steps - snapshot.Steps;
        }
    }

    /// <summary>Adds the current state (by the order the states were first named) and the time and the steps in it and in each of its parents.</summary>
    public void HashState(StateHasher hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        hash.Add(_started);
        for (var node = _current; node is not null; node = node.Parent)
        {
            hash.Add(node.Index);
            hash.Add((float)(_clock - node.EnteredAt));
            hash.Add(_steps - node.EnteredStep);
        }
    }

    private Node NodeOf(TState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!_nodes.TryGetValue(state, out var node)) _nodes[state] = node = new Node(state, _nodes.Count);
        return node;
    }

    private void Change(Node target)
    {
        ChangeNow(target);
        Drain();
    }

    private void ChangeNow(Node target)
    {
        var from = _current;
        var border = from == target ? target.Parent : CommonParent(from, target);
        _busy = true;
        try
        {
            for (var node = from; node is not null && node != border; node = node.Parent) node.Exit?.Invoke();
            _previous = from.Id!;
            _current = target;
            _changes++;
            EnterFrom(border, target);
        }
        finally { _busy = false; }
        Changed?.Invoke(_previous, Current);
    }

    /// <summary>Enters the states from just under <paramref name="border"/> down to <paramref name="target"/>, outermost first.</summary>
    private void EnterFrom(Node? border, Node target)
    {
        int start = _path.Count;
        for (var node = target; node is not null && node != border; node = node.Parent) _path.Add(node);
        for (int i = _path.Count - 1; i >= start; i--)
        {
            _path[i].EnteredAt = _clock;
            _path[i].EnteredStep = _steps;
            _path[i].Enter?.Invoke();
        }
        _path.RemoveRange(start, _path.Count - start);
    }

    private static Node? CommonParent(Node a, Node b)
    {
        for (var x = a; x is not null; x = x.Parent)
            for (var y = b; y is not null; y = y.Parent)
                if (x == y) return x;
        return null;
    }

    private void Run(Action action)
    {
        bool outer = _busy;
        _busy = true;
        try { action(); }
        finally { _busy = outer; }
    }

    private void Drain()
    {
        while (!_busy && _waiting.TryDequeue(out var request))
        {
            if (request.IsEvent) FireNow(request.Event!);
            else ChangeNow(request.Target!);
        }
    }

    /// <summary>The declaration of one state (or of <see cref="Any"/>); every call returns it, to chain them.</summary>
    public sealed class StateConfig
    {
        private readonly StateMachine<TState, TEvent> _machine;
        private readonly Node _node;

        internal StateConfig(StateMachine<TState, TEvent> machine, Node node)
        {
            _machine = machine;
            _node = node;
        }

        private bool IsAny => _node == _machine._any;

        /// <summary>Runs when the state is entered. Several calls add up, in order.</summary>
        /// <exception cref="InvalidOperationException">Declared on <see cref="Any"/>, which is never entered.</exception>
        public StateConfig OnEnter(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ThrowIfAny(nameof(OnEnter));
            _node.Enter += action;
            return this;
        }

        /// <summary>Runs every step while the machine is in the state or in one of its parts, with the step. Several calls add up, in order.</summary>
        /// <exception cref="InvalidOperationException">Declared on <see cref="Any"/>.</exception>
        public StateConfig OnUpdate(Action<float> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ThrowIfAny(nameof(OnUpdate));
            _node.Update += action;
            return this;
        }

        /// <summary>Runs when the state is left. Several calls add up, in order.</summary>
        /// <exception cref="InvalidOperationException">Declared on <see cref="Any"/>, which is never left.</exception>
        public StateConfig OnExit(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ThrowIfAny(nameof(OnExit));
            _node.Exit += action;
            return this;
        }

        /// <summary>Makes the state a part of <paramref name="parent"/>: the parent's ways out and events apply to it, and the parent's entry and exit run only when the machine crosses the parent's border.</summary>
        /// <exception cref="InvalidOperationException">Declared on <see cref="Any"/>, the state already has a parent, or the parent is the state or one of its parts.</exception>
        public StateConfig SubstateOf(TState parent)
        {
            ThrowIfAny(nameof(SubstateOf));
            var node = _machine.NodeOf(parent);
            if (_node.Parent is not null && _node.Parent != node)
                throw new InvalidOperationException($"{_node.Id} is already a part of {_node.Parent.Id}.");
            for (var up = node; up is not null; up = up.Parent)
                if (up == _node) throw new InvalidOperationException($"{parent} cannot be the parent of {_node.Id}: it is that state or one of its parts.");
            _node.Parent = node;
            return this;
        }

        /// <summary>A way out checked every step: when <paramref name="condition"/> is true the machine changes to <paramref name="target"/>.</summary>
        public StateConfig When(Func<bool> condition, TState target)
        {
            ArgumentNullException.ThrowIfNull(condition);
            _node.Rules.Add(new Rule(condition, -1, -1, _machine.NodeOf(target)));
            return this;
        }

        /// <summary>
        /// A way out by time: once the state lasted <paramref name="seconds"/> (and <paramref name="condition"/> is true,
        /// when given) the machine changes to <paramref name="target"/>, on the update on which the time reaches them.
        /// At a fixed rate of R updates a second that is after exactly <paramref name="seconds"/> x R updates when the
        /// product is a whole number (1.9 seconds at 60: 114) and after the next whole number otherwise. Declared on a
        /// parent, the time is the parent's; declared on <see cref="Any"/>, the current state's.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="seconds"/> is negative or not a number.</exception>
        public StateConfig After(float seconds, TState target, Func<bool>? condition = null)
        {
            if (!(seconds >= 0) || float.IsInfinity(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A time is zero or more seconds.");
            _node.Rules.Add(new Rule(condition, seconds, -1, _machine.NodeOf(target)));
            return this;
        }

        /// <summary>
        /// A way out by updates: once the state lasted <paramref name="steps"/> updates (and <paramref name="condition"/>
        /// is true, when given) the machine changes to <paramref name="target"/>, on that update. It counts updates,
        /// not time: the length in seconds follows the update rate, slow motion does not stretch it, and an update of
        /// zero seconds (a frozen world) is not counted. For a game that thinks in frames, and for a hand-kept timer
        /// moved onto a machine without changing the update it ends on. Declared on a parent, the steps are the
        /// parent's; declared on <see cref="Any"/>, the current state's.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="steps"/> is negative.</exception>
        public StateConfig AfterSteps(int steps, TState target, Func<bool>? condition = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(steps);
            _node.Rules.Add(new Rule(condition, -1, steps, _machine.NodeOf(target)));
            return this;
        }

        /// <summary>A way out by event: when <paramref name="trigger"/> is fired (and <paramref name="guard"/> is true, when given) the machine changes to <paramref name="target"/>.</summary>
        public StateConfig On(TEvent trigger, TState target, Func<bool>? guard = null)
        {
            ArgumentNullException.ThrowIfNull(trigger);
            _node.Events.Add(new EventRule(trigger, _machine.NodeOf(target), guard, null));
            return this;
        }

        /// <summary>Takes an event without leaving the state: when <paramref name="trigger"/> is fired (and <paramref name="guard"/> is true, when given) <paramref name="action"/> runs. An empty action ignores the event on purpose.</summary>
        public StateConfig Do(TEvent trigger, Action action, Func<bool>? guard = null)
        {
            ArgumentNullException.ThrowIfNull(trigger);
            ArgumentNullException.ThrowIfNull(action);
            _node.Events.Add(new EventRule(trigger, null, guard, action));
            return this;
        }

        private void ThrowIfAny(string member)
        {
            if (IsAny) throw new InvalidOperationException($"{member} belongs to a state; Any only holds ways out and events.");
        }
    }
}
