# State machines

A character that idles, runs, jumps and attacks, an enemy that patrols and chases, a boss with phases, a level that
goes from its intro to play to its ending: each is in one state at a time and changes for a reason. `StateMachine`
holds that shape, so the states, what they do and the ways out of them are declared in one place instead of spread
over flags, timers and a `switch`.

| To do this | Use |
| --- | --- |
| Say what a state does when it starts, every step and when it ends | `State(s).OnEnter`, `OnUpdate`, `OnExit` |
| Leave a state when something becomes true | `When(condition, next)` |
| Leave a state after a time | `After(seconds, next)` |
| Leave a state after a number of updates | `AfterSteps(steps, next)` |
| Leave a state when the game says so (a hit, a button, a message) | `On(event, next)` and `Fire(event)` |
| React to an event without leaving | `Do(event, action)` |
| Interrupt from any state | `Any.On(...)`, `Any.When(...)` |
| Share ways out among several states | `SubstateOf(parent)` |
| Know how long the state has lasted | `Time`, `TimeIn(state)`, `Steps`, `StepsIn(state)` |
| Start a state part of the way in, or hold it | `SetTime(seconds)`, `SetSteps(steps)` |
| Save and load it | `Snapshot()`, `Restore(snapshot)` |

It is part of the engine's core package and has no dependency. Stepping a machine and firing an event allocate
nothing, so a machine per enemy is fine. A machine needs no scene: whatever owns it calls `Update`, so the rules of a
level kept in a plain class that tests step by themselves use the same machine a scene does.

## Declaring a machine

The states and the events are usually enums. `StateMachine<TState>` is enough when nothing is fired from outside;
`StateMachine<TState, TEvent>` adds events.

<!-- code: samples/Examples/Examples/StateMachineExample.cs#states -->
```csharp
private enum Guard { Patrol, Chase, WindUp, Strike, Stunned, Awake }
private enum Blow { Stomp }

private readonly StateMachine<Guard, Blow> _guard = new(Guard.Patrol);
```

<!-- code: samples/Examples/Examples/StateMachineExample.cs#declare -->
```csharp
public override void Enter()
{
    // What every waking state shares: a stomp interrupts any of them.
    _guard.State(Guard.Awake)
        .On(Blow.Stomp, Guard.Stunned);

    _guard.State(Guard.Patrol).SubstateOf(Guard.Awake)
        .OnUpdate(dt => Walk(_patrolDirection, 30, dt))
        .When(() => Distance < 90, Guard.Chase);

    _guard.State(Guard.Chase).SubstateOf(Guard.Awake)
        .OnEnter(() => Audio.TryPlay("select", 0.4f))
        .OnUpdate(dt => Walk(MathF.Sign(_playerAt.X - _guardAt.X), 55, dt))
        .When(() => Distance < 22, Guard.WindUp)         // declared first: checked first
        .When(() => Distance > 130, Guard.Patrol);

    _guard.State(Guard.WindUp).SubstateOf(Guard.Awake)
        .After(0.35f, Guard.Strike);                      // the machine keeps the time in the state

    _guard.State(Guard.Strike).SubstateOf(Guard.Awake)
        .OnEnter(() => _flash = 1)
        .OnExit(() => _flash = 0)                         // runs however the state ends, a stomp included
        .After(0.2f, Guard.Chase);

    _guard.State(Guard.Stunned)                           // not a part of Awake: a second stomp is not taken
        .After(1.2f, Guard.Patrol);
}
```

A state that is only named as a target needs no declaration of its own.

## Stepping it

Call `Update` once per step with the step the scene received, and `Fire` when something happens.

<!-- code: samples/Examples/Examples/StateMachineExample.cs#step -->
```csharp
public override void Update(float dt)
{
    if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
    _playerAt.X = Math.Clamp(_playerAt.X + Input.Axis("left", "right") * 90 * dt, 12, 308);
    if (Input.Pressed("action") && Distance < 40) _guard.Fire(Blow.Stomp);
    _guard.Update(dt);   // the state's update, then at most one way out
}

// One line puts the machine's state and its time in the scene's state hash.
public override void HashState(StateHasher hash)
{
    hash.Add(_guard);
    hash.Add(_guardAt);
    hash.Add(_playerAt);
    hash.Add(_patrolDirection);
}
```

One `Update` does three things, in this order:

1. adds the step to the time in the state;
2. runs the state's `OnUpdate` (a parent's first);
3. takes at most one way out: the state's own in the order they were declared, then its parents', then `Any`'s.

That order is how an interruption gets its priority: declare the urgent way out first, or put it on a parent or on
`Any` when several states share it. The initial state is entered at the first `Update` or `Fire` (or by `Start()`),
so a machine can be declared after it is created.

Because the machine runs on the step it is given, it follows the scene's clock: slow motion slows its timed states and
a hitstop holds them, with no code of yours.

## Time in a state

`After(seconds, next)` leaves on the update on which the time in the state reaches `seconds`. The machine adds the
steps it is given in double precision, so the length of a state is the same wherever it starts in a run, and at a
fixed update rate it can be worked out before running anything:

> `seconds` at R updates a second is exactly `seconds x R` updates when that product is a whole number, and the next
> whole number when it is not.

| `After` | 30 updates a second | 50 | 60 | 120 | 144 |
| --- | --- | --- | --- | --- | --- |
| 0.1 s | 3 | 5 | 6 | 12 | 15 (14.4) |
| 0.5 s | 15 | 25 | 30 | 60 | 72 |
| 1.9 s | 57 | 95 | 114 | 228 | 274 (273.6) |
| 4.2 s | 126 | 210 | 252 | 504 | 605 (604.8) |
| 600 s | 18,000 | 30,000 | 36,000 | 72,000 | 86,400 |

The update a state is entered on is not one of them: a state entered on update 100 with `After(0.5f, next)` at 60
updates a second is left on update 130. The rule holds for times up to several hours.

A timer kept by hand usually does not follow that rule. `t += dt` in a `float` does not add up to the exact product
(sixty steps of 1/60 are a little under or over 1), and a comparison with `>` instead of `>=` waits one update more
when the sum lands on the value: at 60 updates a second such a timer leaves a state of 1.9, 2.0, 2.4 or 4.2 seconds
after 115, 121, 145 and 253 updates, and one of 0.3, 0.5 or 0.6 seconds after 18, 30 and 36. The machine's 114, 120,
144, 252, 18, 30 and 36 are the regular ones. For the same reason, compare with `After` and not with
`machine.Time > seconds` in an `OnUpdate`: `Time` is a `float` to read and draw, and a test against it brings the
last-digit question back.

### Counting updates

`AfterSteps(steps, next)` leaves after that many updates in the state, whatever the step: for a game that thinks in
frames (an attack with 4 frames of start-up and 12 of recovery), and for moving a hand-kept timer onto a machine
without changing the update anything happens on.

```csharp
attack.State(Move.Windup).AfterSteps(4, Move.Hit);
attack.State(Move.Hit).AfterSteps(3, Move.Recover);
attack.State(Move.Recover).AfterSteps(12, Move.Idle);
```

`Steps` is the number of updates spent in the state (`StepsIn(state)` for a parent). Steps count updates, not time: at
another update rate the state lasts another length in seconds, slow motion does not stretch it, and an update of zero
seconds (the world frozen by a hitstop) is not counted, so a freeze holds a state counted in steps as it holds one
counted in seconds.

To move a game with recorded runs onto a machine, measure each of its timers once and keep the number:

```csharp
// The update a hand-kept timer of `seconds` leaves on (t += dt, then t > seconds).
static int UpdatesOf(float seconds, float dt)
{
    float t = 0;
    for (int update = 1; ; update++) { t += dt; if (t > seconds) return update; }
}
// UpdatesOf(1.9f, 1f / 60f) is 115: State(Phase.Fall).AfterSteps(115, Phase.Done)
```

### Setting the time

`SetTime(seconds)` and `SetSteps(steps)` set what the current state has already spent, as if it had been entered that
long ago; its ways out count from the new value and nothing runs. Use them for a state entered with a head start
(`Go(state)` then `SetTime`), and for a state held at a moment while something else finishes:

```csharp
// The score tally: the state waits at half a second while points are counted, then runs to its end.
flow.State(Phase.Tally)
    .OnUpdate(_ => { if (flow.Time >= 0.5f && CountSomePoints()) flow.SetTime(0.5f); })
    .After(2f, Phase.Done);
```

The state's parents keep their own time and steps.

## Events

`Fire(event)` asks the current state, then its parents, then `Any`, and the first that takes the event changes the
state or runs its action. It returns false when nothing took it; `Unhandled` reports those, and `ThrowOnUnhandled`
turns them into errors while you develop. `Do(event, () => { })` ignores an event on purpose.

An event fired, or a state asked for with `Go`, from inside one of the machine's own handlers waits until the change
in progress is complete, then applies, in the order asked. A handler never sees the machine half changed.

## States inside states

`SubstateOf` makes a state a part of another. The parent's ways out and events apply to all its parts, its `OnUpdate`
runs before theirs, and its `OnEnter` and `OnExit` run only when the machine crosses its border. `IsIn(parent)` is
true in any of its parts. In the example every waking state is a part of `Awake`, which holds the stomp once; the
stunned state is outside it, so a second stomp is not taken.

`After` on a parent counts the parent's time, which is how "at most ten seconds in this phase, whatever it was doing"
is written.

## Animation, sound and input

A state's entry is where its animation and sound start, and a way out can wait for them:

```csharp
machine.State(Hero.Attack)
    .OnEnter(() => { animator.Play("attack", restart: true); Audio.Play("swing"); })
    .When(() => animator.Finished, Hero.Idle);

machine.State(Hero.Idle)
    .When(() => Input.Pressed("action"), Hero.Attack)
    .When(() => Input.Axis("left", "right") != 0, Hero.Run);
```

The machine chooses the state; `Animator`, tracks and timelines play what the state shows
([Animation](animation.md)).

## The scene's own machines

A machine that belongs to the scene itself (the flow of a level, a cutscene) can be handed to the scene, which then
steps it before `Update` and adds it to its state hash:

```csharp
var flow = Machines.Add(new StateMachine<Phase>(Phase.Intro));
flow.State(Phase.Intro).After(2f, Phase.Playing);
flow.State(Phase.Playing).When(() => _player.Dead, Phase.GameOver);
```

A machine that belongs to an object is better stepped by that object, as in the example: its place among the object's
other rules stays explicit.

## Determinism and saves

A machine is `IStateHashed`: `hash.Add(machine)` puts its state, its time and its steps in the state hash, whether a
scene or a class of yours owns it. `Snapshot()` returns the state, the seconds and the updates in it, three values to
write in a save; `Restore` puts a machine back there without running any entry or exit, so loading a game does not
replay a sound or a spawn, and a machine restored in the middle of a state leaves it on the update the saved one
would have.

## Limits

- One way out per `Update`. A state entered in a step runs its `OnUpdate` from the next one.
- A machine is in one state at a time. Two things that vary independently (movement and weapon) are two machines.
- States are declared in code. There is no file format and no visual editor.
- `Restore` gives the state's parents the same time and steps as the state.
- A way out by seconds follows the rule above at a fixed update rate; with steps of varying length it is taken on the
  first update on which the time reaches it.
- A way out by steps does not follow slow motion, and its length in seconds changes with the update rate.

## Another library

`StateMachine` is built for what runs every step. A workflow driven only by events, with parameters on its triggers,
asynchronous actions or a diagram exported from the configuration, is what
[Stateless](https://github.com/dotnet-state-machine/stateless) does well, and it works in a MicroEngine game as it is:
reference the package, fire its triggers from your scene, and add `(int)machine.State` to the state hash. It
allocates on every trigger that changes its state, so keep it for a few long-lived machines (a quest, a dialogue, a
menu flow), not for one per enemy.
