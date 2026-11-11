# 2. Scenes

Every screen of a MicroEngine game is a **scene**: the title, a level, a pause menu, a results screen. In this step you
give Gem Garden a title screen and an empty garden scene, and move between them.

## The game class

Replace `MainGame.cs` with this:

<!-- code: samples/GemGarden/part1-scenes/MainGame.cs -->
```csharp title="MainGame.cs"
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Hosting;
using GemGarden.Scenes;

namespace GemGarden;

/// <summary>Gem Garden: its settings and the first scene.</summary>
public sealed class MainGame : Game
{
    /// <summary>
    /// The game's composition, called by every build (desktop Program.cs, browser, Android, iOS): it chooses the game.
    /// </summary>
    public static void Configure(HostBuilder host) => host.UseGame(() => new MainGame());

    /// <summary>A 320x180 virtual screen, scaled to the window without blurring the pixels.</summary>
    public override GameSettings Settings { get; } = new("Gem Garden") { Background = Color.Hex("1a1c2c") };

    protected override void Load()
    {
        Scenes.Push(new TitleScene(this));
    }
}
```

`Configure` is the game's composition, as the template wrote it: `Program.cs` passes it to the desktop launcher, and the
browser and phone packages of [step 13](13-other-targets.md) call the same method.

`GameSettings` holds what the engine needs before the game starts:

| Setting | Here | Meaning |
| --- | --- | --- |
| `Title` | `"Gem Garden"` | the window's title |
| `VirtualWidth`, `VirtualHeight` | 320, 180 (defaults) | the size of the virtual screen every scene draws into |
| `Scale` | 4 (default) | the initial window size on desktops: 1280x720 |
| `UpdateRate` | 60 (default) | fixed updates per second |
| `Background` | dark blue | the colour every frame is cleared to |

`Load` runs once, after the window exists. For now it only pushes the first scene onto the **scene stack**
(`Scenes`). `Color.Hex` reads `rrggbb` or `rrggbbaa`.

## The title scene

Create `Scenes/TitleScene.cs`:

<!-- code: samples/GemGarden/part1-scenes/Scenes/TitleScene.cs -->
```csharp title="Scenes/TitleScene.cs"
using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace GemGarden.Scenes;

/// <summary>The title screen: the game's name until the player presses confirm.</summary>
public sealed class TitleScene(MainGame game) : Scene
{
    public override void Update(float dt)
    {
        if (Input.Pressed("confirm"))
            Game.Scenes.Replace(new PlayScene(game), Transition.Fade());
    }

    public override void Draw(IRenderer r)
    {
        Font.DrawCentered(r, "GEM GARDEN", new Vector2(160, 60), Color.Hex("a7f070"), scale: 3, shadow: Color.Hex("257179"));
        Font.DrawCentered(r, "PRESS CONFIRM", new Vector2(160, 120), Color.White);
    }
}
```

A scene derives from `Scene` and implements two methods:

- `Update(float dt)` runs every fixed step (60 times a second), with `dt` the step in seconds (1/60).
  `Input.Pressed("confirm")` is true on the one step when the `confirm` *action* goes down: Z, Space, Enter or J on a
  keyboard, A on a gamepad, the A button on a touch screen. Actions are covered in [step 4](04-input.md).
- `Draw(IRenderer r)` draws the scene. Positions are virtual pixels from the top-left corner; `(160, 60)` is the
  middle of the screen horizontally. `Font` is the engine's built-in pixel font ([step 9](09-hud.md)).

The scene receives the `MainGame` through its constructor (a C# primary constructor), so it can create the next scene
with it. Inside any scene, `Game`, `Input`, `Audio` and `Font` are available as properties.

## The play scene

Replace the template's `Scenes/PlayScene.cs` with this:

<!-- code: samples/GemGarden/part1-scenes/Scenes/PlayScene.cs -->
```csharp title="Scenes/PlayScene.cs"
using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace GemGarden.Scenes;

/// <summary>Where the game will happen. For now: a green screen and a way back to the title.</summary>
public sealed class PlayScene(MainGame game) : Scene
{
    public override void Update(float dt)
    {
        if (Input.Pressed("cancel"))
            Game.Scenes.Replace(new TitleScene(game), Transition.Fade());
    }

    public override void Draw(IRenderer r)
    {
        r.FillRect(new RectF(0, 0, Game.Width, Game.Height), Color.Hex("38b764"));
        Font.DrawCentered(r, "THE GARDEN", new Vector2(160, 80), Color.White);
        Font.DrawCentered(r, $"{Age:0.0} SECONDS HERE - CANCEL TO GO BACK", new Vector2(160, 100), Color.Hex("1a1c2c"));
    }
}
```

`Age` is how many seconds the scene has been updated. `cancel` is X, Backspace or K on a keyboard, B on a gamepad.

Run the game: the title appears; confirm fades to the green garden; cancel fades back.

## The scene stack

`Game.Scenes` is a stack of scenes. Only the top scene updates; drawing starts at the top scene, or below it when the top
one is an overlay (you will build a pause overlay in [step 10](10-states.md)).

| Call | Effect |
| --- | --- |
| `Scenes.Push(scene)` | puts a scene on top (a pause menu over the level) |
| `Scenes.Pop()` | removes the top scene; the one below continues |
| `Scenes.Replace(scene)` | pops the top scene and pushes another (title to level) |
| `Scenes.Reset(scene)` | empties the stack and pushes one (back to the title from anywhere) |

Each call takes an optional `Transition`: `Transition.Fade()`, `Transition.Wipe()`, `Transition.Diamonds()` or
`Transition.Iris(point)`. A transition covers the screen, changes scenes when the screen is covered, then reveals the new
scene.

Two rules make scenes safe to use:

- **Changes are queued.** `Replace` inside `Update` does not tear the scene down in the middle of its own method; the
  change happens between updates.
- **Input edges are reset on every change**, so the confirm press that left the title does not also count as a press in
  the next scene.

A scene can also react to being shown and hidden: `Enter()` runs when it is pushed, `Exit()` when it is popped, and
`Resume()` when the scene above it is popped. You will use them for music and menus. All the details:
[Scenes](../manual/scenes.md).

## Checkpoint

Your project now matches [`part1-scenes`](../../samples/GemGarden/part1-scenes). Next, [step 3](03-drawing.md) draws
the hero.
