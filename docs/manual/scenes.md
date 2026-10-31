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

The colour defaults to black. A custom effect subclasses `Transition` and draws using `Cover` (0 clear, 1 fully
covered). If a change with a transition is requested while another transition runs, it is queued as an
immediate change instead.

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

## Related

- [Lifecycle](lifecycle.md): where scene updates sit in a frame
- [UI](ui.md): dialogue and menus used inside scenes
- [ADR 0003](../decisions/0003-scenes-entities-runtime.md): why a scene stack instead of a global state machine
