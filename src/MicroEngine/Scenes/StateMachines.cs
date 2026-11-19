using MicroEngine.Kernel;

namespace MicroEngine.Scenes;

/// <summary>
/// Runs state machines for a scene (<see cref="Scene.Machines"/>): each machine added is stepped every update before
/// the scene's <see cref="Scene.Update"/>, with the same step, in the order they were added, and is part of the
/// scene's state hash. It suits the machines of the scene itself (the flow of a level, a boss fight's phases, a
/// cutscene). A machine that belongs to an object is better stepped by that object, where its place among the object's
/// rules is explicit; such a machine needs no runner, only a call to its <c>Update</c>.
/// </summary>
/// <example>
/// <code>
/// var flow = Machines.Add(new StateMachine&lt;Phase&gt;(Phase.Intro));
/// flow.State(Phase.Intro).After(2f, Phase.Playing);
/// flow.State(Phase.Playing).When(() => _player.Dead, Phase.GameOver);
/// </code>
/// </example>
public sealed class StateMachines : SceneSystem
{
    private readonly List<IStateMachine> _machines = [];
    private IStateMachine[] _run = [];
    private bool _stale;

    /// <summary>How many machines the scene runs.</summary>
    public int Count => _machines.Count;

    /// <summary>Adds <paramref name="machine"/> and returns it. Added during a step, it runs from the next one.</summary>
    /// <exception cref="InvalidOperationException">The machine was already added.</exception>
    public T Add<T>(T machine) where T : class, IStateMachine
    {
        ArgumentNullException.ThrowIfNull(machine);
        if (_machines.Contains(machine)) throw new InvalidOperationException("This state machine is already run by the scene.");
        _machines.Add(machine);
        _stale = true;
        return machine;
    }

    /// <summary>Stops running <paramref name="machine"/>; false when the scene did not run it.</summary>
    public bool Remove(IStateMachine machine)
    {
        if (!_machines.Remove(machine)) return false;
        _stale = true;
        return true;
    }

    /// <inheritdoc/>
    protected override void Update(float dt)
    {
        if (_stale)
        {
            _stale = false;
            _run = [.. _machines];
        }
        foreach (var machine in _run) machine.Update(dt);
    }

    /// <inheritdoc/>
    public override void HashState(StateHasher hash)
    {
        hash.Add(_machines.Count);
        foreach (var machine in _machines) machine.HashState(hash);
    }

    /// <inheritdoc/>
    protected override void Stop()
    {
        _machines.Clear();
        _stale = true;
    }
}
