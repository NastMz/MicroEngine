namespace MicroEngine.Core.State;

/// <summary>
/// Concrete implementation of a finite state machine with support for guarded transitions,
/// entry/exit actions, and flexible configuration.
/// </summary>
/// <typeparam name="TState">The enumeration type representing states.</typeparam>
/// <typeparam name="TTrigger">The enumeration type representing triggers.</typeparam>
public sealed class StateMachine<TState, TTrigger> : IStateMachine<TState, TTrigger>
    where TState : struct, Enum
    where TTrigger : struct, Enum
{
    private readonly Dictionary<TState, StateConfiguration> _configurations = new();
    private readonly TState _initialState;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateMachine{TState, TTrigger}"/> class.
    /// </summary>
    /// <param name="initialState">The initial state of the state machine.</param>
    public StateMachine(TState initialState)
    {
        _initialState = initialState;
        CurrentState = initialState;
    }

    /// <inheritdoc />
    public TState CurrentState { get; private set; }

    /// <inheritdoc />
    public bool CanFire(TTrigger trigger)
    {
        if (!_configurations.TryGetValue(CurrentState, out var config))
        {
            return false;
        }

        return config.CanTransition(trigger);
    }

    /// <inheritdoc />
    public bool Fire(TTrigger trigger)
    {
        if (!_configurations.TryGetValue(CurrentState, out var config))
        {
            return false;
        }

        if (!config.TryGetTargetState(trigger, out var targetState))
        {
            return false;
        }

        ExecuteTransition(targetState);
        return true;
    }

    /// <inheritdoc />
    public void FireStrict(TTrigger trigger)
    {
        if (!Fire(trigger))
        {
            throw new InvalidOperationException(
                $"No transition defined from state '{CurrentState}' with trigger '{trigger}'.");
        }
    }

    /// <inheritdoc />
    public IStateConfiguration<TState, TTrigger> Configure(TState state)
    {
        if (!_configurations.TryGetValue(state, out var config))
        {
            config = new StateConfiguration(state);
            _configurations[state] = config;
        }

        return config;
    }

    /// <inheritdoc />
    public void Reset()
    {
        if (CurrentState.Equals(_initialState))
        {
            return;
        }

        ExecuteTransition(_initialState);
    }

    private void ExecuteTransition(TState targetState)
    {
        // Execute exit action for current state
        if (_configurations.TryGetValue(CurrentState, out var currentConfig))
        {
            currentConfig.ExecuteExit();
        }

        CurrentState = targetState;

        // Execute entry action for target state
        if (_configurations.TryGetValue(targetState, out var targetConfig))
        {
            targetConfig.ExecuteEntry();
        }
    }

    /// <summary>
    /// Internal state configuration implementing the fluent API.
    /// </summary>
    private sealed class StateConfiguration : IStateConfiguration<TState, TTrigger>
    {
        private readonly Dictionary<TTrigger, Transition> _transitions = new();
        private readonly HashSet<TTrigger> _ignoredTriggers = new();
        private Action? _onEntry;
        private Action? _onExit;

        public StateConfiguration(TState state)
        {
            // State is stored in the outer dictionary key
        }

        public IStateConfiguration<TState, TTrigger> Permit(TTrigger trigger, TState targetState)
        {
            _transitions[trigger] = new Transition(targetState, guard: null);
            _ignoredTriggers.Remove(trigger);
            return this;
        }

        public IStateConfiguration<TState, TTrigger> PermitIf(TTrigger trigger, TState targetState, Func<bool> guard)
        {
            if (guard == null)
            {
                throw new ArgumentNullException(nameof(guard));
            }

            _transitions[trigger] = new Transition(targetState, guard);
            _ignoredTriggers.Remove(trigger);
            return this;
        }

        public IStateConfiguration<TState, TTrigger> OnEntry(Action onEntry)
        {
            _onEntry = onEntry ?? throw new ArgumentNullException(nameof(onEntry));
            return this;
        }

        public IStateConfiguration<TState, TTrigger> OnExit(Action onExit)
        {
            _onExit = onExit ?? throw new ArgumentNullException(nameof(onExit));
            return this;
        }

        public IStateConfiguration<TState, TTrigger> Ignore(TTrigger trigger)
        {
            _ignoredTriggers.Add(trigger);
            _transitions.Remove(trigger);
            return this;
        }

        public bool CanTransition(TTrigger trigger)
        {
            if (_ignoredTriggers.Contains(trigger))
            {
                return false;
            }

            if (!_transitions.TryGetValue(trigger, out var transition))
            {
                return false;
            }

            return transition.Guard == null || transition.Guard();
        }

        public bool TryGetTargetState(TTrigger trigger, out TState targetState)
        {
            targetState = default;

            if (_ignoredTriggers.Contains(trigger))
            {
                return false;
            }

            if (!_transitions.TryGetValue(trigger, out var transition))
            {
                return false;
            }

            if (transition.Guard != null && !transition.Guard())
            {
                return false;
            }

            targetState = transition.TargetState;
            return true;
        }

        public void ExecuteEntry()
        {
            _onEntry?.Invoke();
        }

        public void ExecuteExit()
        {
            _onExit?.Invoke();
        }

        private sealed class Transition
        {
            public TState TargetState { get; }
            public Func<bool>? Guard { get; }

            public Transition(TState targetState, Func<bool>? guard)
            {
                TargetState = targetState;
                Guard = guard;
            }
        }
    }
}
