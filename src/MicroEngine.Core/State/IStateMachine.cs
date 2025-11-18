namespace MicroEngine.Core.State;

/// <summary>
/// Represents a finite state machine that manages transitions between states based on triggers.
/// </summary>
/// <typeparam name="TState">The enumeration type representing states.</typeparam>
/// <typeparam name="TTrigger">The enumeration type representing triggers that cause state transitions.</typeparam>
public interface IStateMachine<TState, TTrigger>
    where TState : struct, Enum
    where TTrigger : struct, Enum
{
    /// <summary>
    /// Gets the current state of the state machine.
    /// </summary>
    TState CurrentState { get; }

    /// <summary>
    /// Gets a value indicating whether the state machine can transition to another state with the given trigger.
    /// </summary>
    /// <param name="trigger">The trigger to evaluate.</param>
    /// <returns>True if the transition is allowed; otherwise, false.</returns>
    bool CanFire(TTrigger trigger);

    /// <summary>
    /// Attempts to fire a trigger, causing a state transition if allowed.
    /// </summary>
    /// <param name="trigger">The trigger to fire.</param>
    /// <returns>True if the transition occurred; otherwise, false.</returns>
    bool Fire(TTrigger trigger);

    /// <summary>
    /// Fires a trigger, causing a state transition. Throws if the transition is not allowed.
    /// </summary>
    /// <param name="trigger">The trigger to fire.</param>
    /// <exception cref="InvalidOperationException">Thrown when the transition is not permitted.</exception>
    void FireStrict(TTrigger trigger);

    /// <summary>
    /// Configures a state with allowed triggers and entry/exit actions.
    /// </summary>
    /// <param name="state">The state to configure.</param>
    /// <returns>A state configuration object for fluent API usage.</returns>
    IStateConfiguration<TState, TTrigger> Configure(TState state);

    /// <summary>
    /// Resets the state machine to the initial state.
    /// </summary>
    void Reset();
}

/// <summary>
/// Provides a fluent API for configuring state transitions, entry actions, and exit actions.
/// </summary>
/// <typeparam name="TState">The enumeration type representing states.</typeparam>
/// <typeparam name="TTrigger">The enumeration type representing triggers.</typeparam>
public interface IStateConfiguration<TState, TTrigger>
    where TState : struct, Enum
    where TTrigger : struct, Enum
{
    /// <summary>
    /// Permits a trigger to transition from the configured state to a target state.
    /// </summary>
    /// <param name="trigger">The trigger that causes the transition.</param>
    /// <param name="targetState">The state to transition to.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    IStateConfiguration<TState, TTrigger> Permit(TTrigger trigger, TState targetState);

    /// <summary>
    /// Permits a trigger to transition to a target state only if a guard condition is met.
    /// </summary>
    /// <param name="trigger">The trigger that causes the transition.</param>
    /// <param name="targetState">The state to transition to.</param>
    /// <param name="guard">A function that evaluates whether the transition is allowed.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    IStateConfiguration<TState, TTrigger> PermitIf(TTrigger trigger, TState targetState, Func<bool> guard);

    /// <summary>
    /// Defines an action to execute when entering the configured state.
    /// </summary>
    /// <param name="onEntry">The action to execute on state entry.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    IStateConfiguration<TState, TTrigger> OnEntry(Action onEntry);

    /// <summary>
    /// Defines an action to execute when exiting the configured state.
    /// </summary>
    /// <param name="onExit">The action to execute on state exit.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    IStateConfiguration<TState, TTrigger> OnExit(Action onExit);

    /// <summary>
    /// Permits a trigger to be ignored in the configured state (no transition occurs).
    /// </summary>
    /// <param name="trigger">The trigger to ignore.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    IStateConfiguration<TState, TTrigger> Ignore(TTrigger trigger);
}
