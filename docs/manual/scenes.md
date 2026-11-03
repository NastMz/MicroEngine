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
| `Current`, `Count`, `Stack`, `InTransition` | Read-only state |

Changes are **queued**, not applied immediately: they run before and after the scene updates of a step, so a
scene can call `Pop()` from inside its own `Update` safely. Every push and pop resets input edges
(`InputMap.Reset`), so the key that opened a menu does not also activate its first option.

Only the top scene, plus any scenes under non-blocking overlays, are updated. Drawing starts at the top-most
non-overlay scene and goes up, so a pause menu draws over a frozen level.

### Overlays

A pause menu from Neon Rift: an overlay that leaves the level frozen underneath.

```csharp
internal sealed class PauseScene(RiftGame game) : Scene
{
    private readonly SelectionList _menu = new("RESUME", "QUIT TO TITLE");
    public override bool IsOverlay => true;

    public override void Enter() => _menu.Moved += () => game.Sfx("menu-move", 0.6f);

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Close(); return; }
        if (_menu.Update(Input) is { } choice)
        {
            game.Sfx("menu-select", 0.7f);
            if (choice == 0) Close();
            else Game.Scenes.Reset(new TitleScene(game), Transition.Fade(0.6f));
        }
    }
    // ...
}
```

Source: [`games/NeonRift/Scenes/PlayScene.cs`](../../games/NeonRift/Scenes/PlayScene.cs)

Override `BlocksUpdateBelow => false` for an overlay that should let the scene below keep running.

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
class ([`src/MicroEngine/Math/MathX.cs`](../../src/MicroEngine/Math/MathX.cs)).

```csharp
Tweens.Add(1.2f, t => _revealed = t, Ease.Linear, () =>
{
    game.Sfx("unlock", 0.9f);
    _target = justUnlocked;
    _walk = 0;
}, delay: 0.6f);
```

Source: [`games/MeadowRun/Scenes/MapScene.cs`](../../games/MeadowRun/Scenes/MapScene.cs)

Tweens have no handle; `Tweens.Clear()` drops all of them, and popping the scene clears them as well.

## The scene world

Next SDK version (not published yet: arrives with the next SDK version). `IWorld` holds a scene's objects as
entities: each has an `EntityId` (a slot and a generation, so the id of a destroyed entity never reaches the entity
that reuses its slot), a `Transform` relative to an optional parent (stored in 3D; `Position2D`, `Rotation2D` and
`Scale2D` read and write the 2D part) and typed attachments, which are your own classes (`Attach`, `TryGet`, `Get`,
`Detach`; `All<T>()` walks every attachment of a type). `SceneWorld` is the engine's implementation; game code
programs against `IWorld`. The rules a game can rely on:

- **Creating** an entity takes effect at once: its id is valid and its transform writable. A walk over `All<T>()` or
  `Children(parent)` that is already running does not visit it; every later one does. There is no spawn queue to flush.
- **Destroying** an entity takes effect at once for queries: `IsAlive` is false, `Count` drops and every walk skips it
  and its children. Its transform and attachments stay readable until the world ends the step; then attachments that
  implement `IDestroyedCallback` are told, in the order the entities were destroyed, and the slot is released.
- **Order**: `All<T>()` walks attachments in the order they were attached, and destroying entities never reorders the
  rest, so rules that take the first match or consume random numbers in update order behave the same every run.
  `foreach` over `All<T>()` and `Children(parent)` allocates nothing.
- **Drawing between steps**: `DrawPosition2D(id)` and `WorldMatrix(id)` blend each entity from where it was at the
  start of the step to where it is now, by the world's `Blend`; at blend 1, or for an entity that did not move, they
  return exactly the simulated transform. A new, `Teleport`ed or re-parented entity is drawn where it is until the
  next step. `Blend` is 1 when the world did not step in the engine's latest step (its scene was paused or under an
  overlay), so a frozen world is drawn as its last step instead of rocking between its last two.
- **Clock**: each world has a `WorldClock` that advances with the world's step: `Scale` slows it down (0 pauses it),
  `Freeze(seconds)` stops whole steps for a hitstop (the longer freeze wins), and a world that does not step consumes
  no time. At scale 1 the step it reports is exactly the engine's.
- **State hash**: the world adds its ids, hierarchy, transforms, which attachments each entity has, the state of
  attachments that implement `IStateHashed`, and its clock; drawing never changes it.

Whoever owns a `SceneWorld` drives it: `BeginStep(Game.UpdateCount, dt)` before the world's step (it returns the
step's length on the world's clock, 0 while frozen), `EndStep()` after it, and `BeginDraw(Game.UpdateCount,
Game.FrameBlend)` before drawing it.

## Related

- [Lifecycle](lifecycle.md): where scene updates sit in a frame
- [UI](ui.md): dialogue and menus used inside scenes
