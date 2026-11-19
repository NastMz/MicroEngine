using MicroEngine;

namespace FsmCompare;

/// <summary>The enemy's states. Patrol, Chase, Attack and Stunned are parts of <see cref="Alive"/>, which is never the current state itself.</summary>
public enum Mode { Patrol, Chase, Attack, Stunned, Dead, Alive }

/// <summary>What the world tells an enemy in one step.</summary>
/// <param name="Distance">Distance to the player.</param>
/// <param name="Hit">The player hit it this step.</param>
/// <param name="Kill">Something killed it this step.</param>
public readonly record struct Sense(float Distance, bool Hit, bool Kill);

/// <summary>
/// The rules every version implements: it patrols until the player is within 80, chases until within 16 or beyond
/// 120, attacks for 0.4 s (its hitbox is live only while it attacks) and chases again; a hit stuns it for 0.8 s unless
/// it is already stunned; a kill ends it, and nothing moves a dead enemy.
/// </summary>
public interface IEnemy
{
    Mode Mode { get; }
    float TimeInMode { get; }
    bool HitboxLive { get; }
    int Attacks { get; }
    void Step(Sense sense, float dt);
}

#region by-hand
/// <summary>The way the demo games write it today: an enum, a time field, a switch and an enter method.</summary>
public sealed class HandEnemy : IEnemy
{
    public Mode Mode { get; private set; } = Mode.Patrol;
    public float TimeInMode { get; private set; }
    public bool HitboxLive { get; private set; }
    public int Attacks { get; private set; }

    private void Enter(Mode next)
    {
        // The exit of every state has to be remembered here, by hand.
        if (Mode == Mode.Attack) HitboxLive = false;
        Mode = next;
        TimeInMode = 0;
        if (next == Mode.Attack) { HitboxLive = true; Attacks++; }
    }

    public void Step(Sense sense, float dt)
    {
        TimeInMode += dt;
        if (Mode == Mode.Dead) return;
        if (sense.Kill) { Enter(Mode.Dead); return; }
        if (sense.Hit && Mode != Mode.Stunned) { Enter(Mode.Stunned); return; }
        switch (Mode)
        {
            case Mode.Patrol:
                if (sense.Distance < 80) Enter(Mode.Chase);
                break;
            case Mode.Chase:
                if (sense.Distance < 16) Enter(Mode.Attack);
                else if (sense.Distance > 120) Enter(Mode.Patrol);
                break;
            case Mode.Attack:
                if (TimeInMode >= 0.4f - 1e-5f) Enter(Mode.Chase);
                break;
            case Mode.Stunned:
                if (TimeInMode >= 0.8f - 1e-5f) Enter(Mode.Patrol);
                break;
        }
    }
}
#endregion

#region stateless
/// <summary>
/// On Stateless. The library changes state when a trigger is fired, so what the game checks every step (distance,
/// time in the state) has to become triggers that the game fires itself, and the time in the state is the game's.
/// </summary>
public sealed class StatelessEnemy : IEnemy
{
    private enum Trigger { SeePlayer, InRange, LostPlayer, TimeUp, Hit, Kill }

    private readonly Stateless.StateMachine<Mode, Trigger> _machine = new(Mode.Patrol);

    public Mode Mode => _machine.State;
    public float TimeInMode { get; private set; }
    public bool HitboxLive { get; private set; }
    public int Attacks { get; private set; }

    public StatelessEnemy()
    {
        _machine.OnTransitioned(_ => TimeInMode = 0);
        _machine.Configure(Mode.Alive)
            .Permit(Trigger.Kill, Mode.Dead)
            .Permit(Trigger.Hit, Mode.Stunned);
        _machine.Configure(Mode.Patrol).SubstateOf(Mode.Alive)
            .Permit(Trigger.SeePlayer, Mode.Chase);
        _machine.Configure(Mode.Chase).SubstateOf(Mode.Alive)
            .Permit(Trigger.InRange, Mode.Attack)
            .Permit(Trigger.LostPlayer, Mode.Patrol);
        _machine.Configure(Mode.Attack).SubstateOf(Mode.Alive)
            .OnEntry(() => { HitboxLive = true; Attacks++; })
            .OnExit(() => HitboxLive = false)
            .Permit(Trigger.TimeUp, Mode.Chase);
        _machine.Configure(Mode.Stunned).SubstateOf(Mode.Alive)
            .Ignore(Trigger.Hit)
            .Permit(Trigger.TimeUp, Mode.Patrol);
        _machine.Configure(Mode.Dead)
            .Ignore(Trigger.Hit)
            .Ignore(Trigger.Kill);
    }

    public void Step(Sense sense, float dt)
    {
        TimeInMode += dt;
        if (Mode == Mode.Dead) return;
        if (sense.Kill) { _machine.Fire(Trigger.Kill); return; }
        if (sense.Hit && Mode != Mode.Stunned) { _machine.Fire(Trigger.Hit); return; }
        // The switch does not go away: something has to decide which trigger this step's conditions mean.
        switch (Mode)
        {
            case Mode.Patrol:
                if (sense.Distance < 80) _machine.Fire(Trigger.SeePlayer);
                break;
            case Mode.Chase:
                if (sense.Distance < 16) _machine.Fire(Trigger.InRange);
                else if (sense.Distance > 120) _machine.Fire(Trigger.LostPlayer);
                break;
            case Mode.Attack:
                if (TimeInMode >= 0.4f - 1e-5f) _machine.Fire(Trigger.TimeUp);
                break;
            case Mode.Stunned:
                if (TimeInMode >= 0.8f - 1e-5f) _machine.Fire(Trigger.TimeUp);
                break;
        }
    }
}
#endregion

#region engine
/// <summary>On the engine's machine: conditions, times and events are declared where the state is, and the machine keeps the time.</summary>
public sealed class EngineEnemy : IEnemy
{
    private enum Event { Hit, Kill }

    private readonly StateMachine<Mode, Event> _machine = new(Mode.Patrol);
    private Sense _sense;

    public Mode Mode => _machine.Current;
    public float TimeInMode => _machine.Time;
    public bool HitboxLive { get; private set; }
    public int Attacks { get; private set; }

    public EngineEnemy()
    {
        _machine.State(Mode.Alive)
            .On(Event.Kill, Mode.Dead)
            .On(Event.Hit, Mode.Stunned, () => Mode != Mode.Stunned);
        _machine.State(Mode.Patrol).SubstateOf(Mode.Alive)
            .When(() => _sense.Distance < 80, Mode.Chase);
        _machine.State(Mode.Chase).SubstateOf(Mode.Alive)
            .When(() => _sense.Distance < 16, Mode.Attack)
            .When(() => _sense.Distance > 120, Mode.Patrol);
        _machine.State(Mode.Attack).SubstateOf(Mode.Alive)
            .OnEnter(() => { HitboxLive = true; Attacks++; })
            .OnExit(() => HitboxLive = false)
            .After(0.4f, Mode.Chase);
        _machine.State(Mode.Stunned).SubstateOf(Mode.Alive)
            .After(0.8f, Mode.Patrol);
    }

    public void Step(Sense sense, float dt)
    {
        _sense = sense;
        // An event that changes the state is this step's change; otherwise the step runs the state.
        if (sense.Kill && _machine.Fire(Event.Kill)) return;
        if (sense.Hit && _machine.Fire(Event.Hit)) return;
        _machine.Update(dt);
    }
}
#endregion
