# Scenes

Every screen of a MicroEngine game (title, level, battle, pause menu, dialogue) is a `Scene`. Scenes live on a
stack owned by the game's `SceneManager`: overlays draw on top of the scene below and can freeze it, scene
changes are applied between updates, and optional transitions swap scenes at their midpoint. Each scene also
owns a coroutine runner and a tweener that stop when it leaves the stack.

## The `Scene` class

| Member | Purpose |
| --- | --- |
| `Update(float dt)`, `Draw(IRenderer r)` (abstract) | Called each fixed step and each rendered frame |
| `Enter()`, `Exit()` | Called when the scene is pushed and when it is popped |
| `Resume()` | Called when a scene pushed above this one is popped |
| `IsOverlay` | `true`: the scene below is drawn first (default `false`) |
| `BlocksUpdateBelow` | `true` (default): scenes below an overlay are not updated |
| `Game`, `Input`, `Audio`, `Font` | Shortcuts to the owning game's services |
| `Coroutines`, `Tweens` | Per-scene `CoroutineRunner` and `Tweener` |
| `Age` | Seconds this scene has been updated |
| `DrawUi(IRenderer ui)`, `UiContains(Vector2)`, `WorldCamera` | The interface drawn at the window's resolution over the world, where it takes the pointer, and the camera pushed around `Draw` ([The world view and the UI view](rendering.md#the-world-view-and-the-ui-view)) |

```csharp
public abstract class Scene
{
    // ...
    /// <summary>When true, the scene below is drawn first.</summary>
    public virtual bool IsOverlay => false;
    /// <summary>When true (default), scenes below an overlay are not updated.</summary>
    public virtual bool BlocksUpdateBelow => true;

    public virtual void Enter() { }
    public virtual void Exit() { }
    /// <summary>Called when a scene pushed above this one is popped.</summary>
    public virtual void Resume() { }
    public abstract void Update(float dt);
    public abstract void Draw(IRenderer renderer);
    // ...
}
```

Source: [`src/MicroEngine/Scenes/Scene.cs`](../../src/MicroEngine/Scenes/Scene.cs)

Each update a scene first advances its coroutines, then its tweens, then calls your `Update`.

## The scene stack

`Game.Scenes` is a `SceneManager`:

| Method | Effect |
| --- | --- |
| `Push(scene, transition?)` | Adds a scene on top (overlays, sub-screens) |
| `Pop(transition?)` | Removes the top scene; the one below gets `Resume()` |
| `Replace(scene, transition?)` | Pops the top scene and pushes another |
| `Reset(scene, transition?)` | Pops every scene, then pushes one (back to the title) |
| `Push(scene, onResult, transition?)` | Pushes a `Scene<TResult>`; when it calls `Finish(value)` it is popped and `onResult` receives the value |
| `Current`, `Count`, `Stack`, `InTransition` | Read-only state |

Changes are **queued**, not applied immediately: they run before and after the scene updates of a step, so a
scene can call `Pop()` from inside its own `Update` safely. Every push and pop resets input edges
(`InputMap.Reset`), so the key that opened a menu does not also activate its first option.

Only the top scene, plus any scenes under non-blocking overlays, are updated. Drawing starts at the top-most
non-overlay scene and goes up, so a pause menu draws over a frozen level.

### Overlays

The tutorial's pause menu: an overlay that leaves the round frozen underneath.

<!-- code: samples/GemGarden/part4-complete/Scenes/PauseScene.cs -->
```csharp
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace GemGarden.Scenes;

/// <summary>An overlay over the frozen round: resume it or go back to the title.</summary>
public sealed class PauseScene(MainGame game) : Scene
{
    /// <summary>The play scene is drawn first, below this one, and stops updating while this one is on top.</summary>
    public override bool IsOverlay => true;

    public override void Enter()
    {
        Audio.PauseMusic(true);
        Widgets.Content = new Panel(new Stack { Spacing = 4 }
            .Add(new Label("PAUSED") { Color = UiTheme.Default.Accent })
            .Add(new Button("RESUME", () => Game.Scenes.Pop()) { Width = 100 })
            .Add(new Button("QUIT TO TITLE", () => Game.Scenes.Reset(new TitleScene(game), Transition.Fade())) { Width = 100 }))
        { Padding = 8 };
        Widgets.FocusChanged += change =>
        {
            if (change.From is not null) Audio.Play("select");
        };
    }

    public override void Exit() => Audio.PauseMusic(false);

    public override void Update(float dt)
    {
        if (Input.Pressed("pause") || Input.Pressed("cancel")) Game.Scenes.Pop();
    }

    public override void Draw(IRenderer r) => r.FillRect(new RectF(0, 0, Game.Width, Game.Height), Color.Black.WithAlpha(0.5f));
}
```

Override `BlocksUpdateBelow => false` for an overlay that should let the scene below keep running.

### Scenes that return a value

A dialogue, a shop or a yes/no question can
derive from `Scene<TResult>` and end with `Finish(value)`. Pushed with `Game.Scenes.Push(scene, onResult)`, it is
popped (deferred, like `Pop`) and `onResult` receives the value after the scene's `Exit` and before the scene below
receives `Resume`. Only the first `Finish` counts; a result scene removed any other way (popped, replaced, the stack
reset) returns nothing, and one pushed with plain `Push` simply pops when it finishes.

## Transitions

A `Transition` covers the old scene during the first half of its duration, performs the scene change at
progress 0.5, and reveals the new scene during the second half. While the screen is being covered the stack is
not updated; during the reveal the new scene already updates, so it is alive when it appears.

| Factory | Default duration | Look |
| --- | --- | --- |
| `Transition.Fade(duration, color?)` | 0.5 s | Full-screen fade |
| `Transition.Iris(center, duration, color?)` | 0.8 s | Closing circle around a screen point |
| `Transition.Diamonds(duration, color?)` | 0.7 s | Grid of growing diamonds sweeping diagonally |
| `Transition.Wipe(duration, color?)` | 0.6 s | Horizontal wipe |

<!-- sdk-version: history -->
The colour defaults to black. A custom effect subclasses `Transition` and draws using `Cover` (0 clear, 1 fully
covered). If a change with a transition is requested while another transition runs, its own transition is dropped
and it applies as a change without one. In 0.4.0-alpha.1 such a change could apply before the playing transition
swapped its scenes; from 0.4.0-alpha.2 every change waits for that swap and keeps its order.

```csharp
Game.Scenes.Replace(new PlayScene(_game, progress), Transition.Iris(new Vector2(160, 70), 1.0f));
```

Source: [`games/CourtyardKey/Scenes/TitleScene.cs`](../../games/CourtyardKey/Scenes/TitleScene.cs)

## Coroutines

`Coroutines.Start(IEnumerator)` runs an iterator one step per update and returns a `CoroutineHandle`
(`IsFinished`, `Cancel()`). A coroutine may yield:

| Yield | Meaning |
| --- | --- |
| `null` | Continue next update |
| `Wait.Seconds(s)` | Pause for simulated seconds |
| `Wait.Until(() => condition)` | Pause until the condition is true (checked every update) |
| another `IEnumerator` | Run it as a sub-routine and continue when it finishes |

Any other value throws `InvalidOperationException`. When the scene is popped, its coroutines are stopped.
Courtyard Key's item-get sequence shows a typical cutscene:

```csharp
private System.Collections.IEnumerator ItemGetRoutine(Item item, int amount, Action? done)
{
    Mode = PlayMode.Cutscene;
    // ...
    if (major) Audio.PauseMusic(true);
    Audio.Play(major ? "fanfare_big" : item == Item.Gems ? "gem" : "fanfare");
    Burst(Hero.Position + new Vector2(0, -24), World.Fx.Sparkle, 18, additive: true);
    yield return Wait.Seconds(major ? 1.3f : 0.6f);
    Apply(item, amount);
    bool closed = false;
    ShowText(null, ItemText(item, amount), () => closed = true);
    yield return Wait.Until(() => closed);
    // ...
}
```

Source: [`games/CourtyardKey/Scenes/PlayScene.Events.cs`](../../games/CourtyardKey/Scenes/PlayScene.Events.cs)

## Tweens

`Tweens.Add(duration, apply, ease?, done?, delay?)` calls `apply` with eased progress 0..1 every update for
`duration` seconds, then calls `done`. The default easing is `Ease.OutQuad`; other curves live in the `Ease`
class ([`src/MicroEngine.Kernel/Math/MathX.cs`](../../src/MicroEngine.Kernel/Math/MathX.cs)).

```csharp
Tweens.Add(1.2f, t => _revealed = t, Ease.Linear, () =>
{
    game.Sfx("unlock", 0.9f);
    _target = justUnlocked;
    _walk = 0;
}, delay: 0.6f);
```

Source: [`games/MeadowRun/Scenes/MapScene.cs`](../../games/MeadowRun/Scenes/MapScene.cs)

Tweens have no handle; `Tweens.Clear()` drops all of them, and popping the scene clears them as well. A value that
goes through more than two keys, several values moving together, or named moments along the way are a `Track<T>` and
a `Timeline`: [Animation](../guide/animation.md).

## Timers

`Timers.After(seconds, action)` calls an action once and `Timers.Every(seconds, action, times)` calls it at
intervals, until its count ends or `Timers.Cancel(handle)` stops it. Timers run before the scene's `Update` on the
step it receives, so a hitstop or slow motion on the world's clock holds them too; pass `unscaled: true` for one that
must keep running (a pause menu's blink). They end with the scene and are part of its state hash. Use a timer for a
single delayed or repeated call and a coroutine for a sequence of steps.

### When a timer is called

A timer in seconds is called on the update on which the time since it was added reaches its delay. Time is counted in
double precision, so at a fixed update rate the update can be worked out before running anything, and it is the same
wherever in a run the timer is added:

> `seconds` at R updates a second is exactly `seconds x R` updates when that product is a whole number (0.5 s at 60:
> 30; 1.9 s at 60: 114; 4 s at 144: 576), and the next whole number when it is not (0.1 s at 144: 15).

A repeating timer keeps what is left over, so its n-th call follows the same rule for `n x seconds`:
`Every(0.1f, ...)` at 60 updates a second is called every 6 updates for as long as it runs, and `Every(0.04f, ...)`
(2.4 updates) on updates 3, 5, 8, 10, 12, 15. A state machine's `After` follows the same rule
([Time in a state](../guide/state-machines.md#time-in-a-state)), so a timer and a state of the same length end on the
same update.

`Timers.AfterSteps(steps, action)` and `Timers.EverySteps(steps, action, times)` count updates instead of seconds, for
a game that thinks in frames and for a delay kept by hand (`t -= dt; if (t <= 0)`) that moves onto a timer without
changing the update it fires on. Steps count updates, not time: slow motion does not stretch them, their length in
seconds changes with the update rate, and an update of zero seconds (the world frozen) is not counted.
`Timers.Remaining(handle)` gives what is left of a timer, in the unit it was added with.

### Timers outside a scene

`Scene.Timers` is a `Timers` the scene steps. The rules of a game kept in a plain class (a stage, a board, a match
that tests step without a scene) create their own and step it:

```csharp
public sealed class Stage
{
    private readonly Timers _timers = new();

    public void BossDefeated()
    {
        _timers.After(2.6f, BigExplosion);
        _timers.After(3.4f, Fanfare);
    }

    public void Update(float dt)
    {
        _timers.Update(dt);                 // the due ones are called here, in the order they were added
        // the rules
    }

    public void HashState(StateHasher hash) => hash.Add(_timers);
}
```

`Update(dt, unscaledDt)` steps two clocks, the second for timers added with `unscaled: true`. A
[state machine](../guide/state-machines.md) needs no scene either: its owner calls `Update`. A save cannot hold a
timer's action; an object that is saved with timers waiting writes `Remaining` for each and adds them again when it is
loaded.

## Scene systems

A scene system is a part of a scene that the engine runs for it. Derive from `SceneSystem`, add it with
`Systems.Add`, and the engine starts it, steps it before the scene's `Update` (and once more after it, in
`LateUpdate`), adds its `HashState` to the scene's state hash and stops it when the scene leaves the stack. The scene
writes none of those calls. `Timers` is one and so is `Machines`, which steps the scene's own
[state machines](../guide/state-machines.md); `Physics` is the scene's physics world and `Sounds` its listener and
sound emitters ([Sounds with a place](audio.md#sounds-with-a-place)); a component store is another.

<!-- code: samples/Examples/Examples/SystemsExample.cs#system -->
```csharp
/// <summary>The time of day, shared by whatever in the scene wants to read it.</summary>
public sealed class DayCycle : SceneSystem
{
    /// <summary>0 at dawn, 0.5 at dusk, back to 0 after a full day.</summary>
    public float Time { get; private set; }
    public float DayLength { get; init; } = 4f;
    public bool IsNight => Time >= 0.5f;

    // Called by the engine before the scene's Update, with the same step.
    protected override void Update(float dt) => Time = (Time + dt / DayLength) % 1f;

    // The engine adds it to the scene's state hash: a deterministic run covers the system with no code in the scene.
    public override void HashState(StateHasher hash) => hash.Add(Time);
}
```

<!-- code: samples/Examples/Examples/SystemsExample.cs#use -->
```csharp
public override void Enter()
{
    _day = Systems.Add(new DayCycle());          // started now, stepped every update, stopped when the scene leaves
    Timers.Every(0.5f, () =>                     // the scene's time: it slows and freezes with the world's clock
    {
        if (_day.IsNight) _stars.Add(new Vector2(Game.Random.Next(20, 300), Game.Random.Next(30, 110)));
        else _stars.Clear();
    });
}

public override void Update(float dt)
{
    if (Input.Pressed("pause")) Game.Scenes.Pop();
}
```

| Hook | Called |
| --- | --- |
| `Start()` | once, when the scene is on the stack: before `Enter` for a system added earlier, at once for one added later |
| `Update(dt)` | every step the scene is updated, after its coroutines, tweens and widgets and before `Scene.Update`, with the same step |
| `LateUpdate(dt)` | after `Scene.Update` in the same step, before the world releases what was destroyed |
| `BeginDraw(blend)` | once per drawn frame before `Scene.Draw`; it must not change simulation state |
| `HashState(hash)` | when the state hash is computed, after the scene's world and widgets and before `Scene.HashState` |
| `Stop()` | once, after `Exit`, or when the system is removed |

A scene has one system per type: `Systems.Get<T>()` finds it. Systems run by `Order` (lower first), then in the order
they were added, and stop in reverse. A scene under a blocking overlay is not updated and neither are its systems.

`Game.Scenes.AddSystem(scene => ...)` gives a system to every scene that is pushed (return null for a scene that gets
none), and `Game.Scenes.Entered` and `Exited` report scenes going onto and off the stack. A module uses them from its
`Configure` so that it lives in the game's scenes with no code in them
([Modules and the frame schedule](lifecycle.md#modules-and-the-frame-schedule)).

## The scene world

Every scene can hold its objects in a world,
`Objects` (an `IWorld`), created the first time the scene reads it; a scene that never reads it has none. Create it
outside drawing, from `Enter`, `Update` or the scene's constructor: the world is simulation state, so reading `Objects`
for the first time from `Draw` or `DrawUi` throws an `InvalidOperationException` (a scene whose drawing alone reads the
world, for its `Blend`, reads `Objects` once in `Enter`). The world
holds entities: each has an `EntityId` (a slot and a generation, so the id of a destroyed entity never reaches the entity
that reuses its slot), a `Transform` relative to an optional parent (stored in 3D; `Position2D`, `Rotation2D` and
`Scale2D` read and write the 2D part) and typed attachments, which are your own classes (`Attach`, `TryGet`, `Get`,
`Detach`; `All<T>()` walks every attachment of a type, `Children(parent)` the children of an entity and
`Children<T>(parent)` the attachments of a type among them). `SceneWorld` is the engine's implementation; game code
programs against `IWorld`. The rules a game can rely on:

- **Creating** an entity takes effect at once: its id is valid and its transform writable. A walk over `All<T>()` or
  `Children(parent)` that is already running does not visit it; every later one does. There is no spawn queue to flush.
- **Destroying** an entity takes effect at once for queries: `IsAlive` is false, `Count` drops and every walk skips it
  and its children. Its transform and attachments stay readable until the world ends the step; then attachments that
  implement `IDestroyedCallback` are told, in the order the entities were destroyed, and the slot is released.
- **Order**: `All<T>()` walks attachments in the order they were attached, and destroying entities never reorders the
  rest, so rules that take the first match or consume random numbers in update order behave the same every run.
  `Children(parent)` and `Children<T>(parent)` walk the children in the order they were given that parent.
  `foreach` over any of the three allocates nothing.
- **Groups**: an entity with children is a group (the actors of a room, the pieces on a board, the items in a chest).
  `Children<T>(group)` walks the attachments of type `T` of its direct live children and skips the children that have
  none; its enumerator also gives the child's id (`Entity`). Destroying the group destroys what is in it. Attachments
  are found by the type they were attached under: actors attached as `Actor` are walked with `Children<Actor>(room)`,
  and a kind among them is picked with `is`.
- **Drawing between steps**: `DrawPosition2D(id)` and `WorldMatrix(id)` blend each entity from where it was at the
  start of the step to where it is now, by the world's `Blend`; at blend 1, or for an entity that did not move, they
  return exactly the simulated transform. A new, `Teleport`ed or re-parented entity is drawn where it is until the
  next step. `Blend` is 1 when the world did not step in the engine's latest step (its scene was paused or under an
  overlay), so a frozen world is drawn as its last step instead of rocking between its last two. A `Camera2D` the
  rules move is drawn the same way by setting its `DrawPosition` to the `DrawPosition2D` of an entity that follows it
  ([Camera](rendering.md#camera)).
- **Clock**: each world has a `WorldClock` that advances with the world's step: `Scale` slows it down (0 pauses it),
  `Freeze(seconds)` stops whole steps for a hitstop (the longer freeze wins), and a world that does not step consumes
  no time. At scale 1 the step it reports is exactly the engine's.
- **State hash**: the world adds its ids, hierarchy, transforms, which attachments each entity has, the state of
  attachments that implement `IStateHashed`, and its clock; drawing never changes it.

The engine steps a scene's world with the scene. While the scene is updated, each step starts before its coroutines,
tweens and `Update`, and ends after them (releasing what was destroyed); `Update` then receives the step on the
world's clock: the same `dt` at scale 1, scaled in slow motion, 0 during a freeze. The scene's `Age`, coroutines and
tweens keep the engine's time, and `EngineStep` is the engine's step before the clock scales or freezes it (the same
value every update, `1 / UpdateRate`; it is what `Update` receives when the scene has no world). Advance what keeps
moving through a hitstop or slow motion, such as play time, camera shake or particles, by `EngineStep`:

```csharp
public override void Update(float dt)   // 0 while Objects.Clock is frozen
{
    camera.Update(EngineStep);          // the shake decays during a hitstop
    if (dt == 0) return;
    // the rules, by dt
}
```
 Before the scene is drawn, the engine sets the world's `Blend`: the frame's blend while
the scene plays, 1 while it is frozen under a blocking overlay or by a transition, so a paused level stays still
instead of rocking between its last two updates, with no code in the game. The world's state is part of the scene's
section of the state hash.

A world you create yourself (`new SceneWorld()`, for a tool or a test) is driven by its owner:
`BeginStep(Game.UpdateCount, dt)` before its step (it returns the step on its clock), `EndStep()` after it, and
`BeginDraw(Game.UpdateCount, Game.FrameBlend)` before drawing it.

## Related

- [Lifecycle](lifecycle.md): where scene updates sit in a frame
- [UI](ui.md): dialogue and menus used inside scenes
