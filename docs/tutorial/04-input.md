# 4. Input and movement

This step makes the gardener walk with the keyboard, a gamepad or a touch screen, and run while a button is held.

## Actions, not keys

A MicroEngine game never asks "is the left arrow down?". It asks whether an **action** is down: `"left"`, `"confirm"`,
`"run"`. Each action is bound to keys, gamepad buttons, a stick direction and, on touch screens, an on-screen control.
The game reads every device the same way, players can use whichever they have, and automated tests drive the same
actions.

Every game starts with these actions:

| Action | Keyboard | Gamepad | Touch (default layout) |
| --- | --- | --- | --- |
| `left` `right` `up` `down` | arrows, WASD | d-pad, left stick | d-pad |
| `confirm` | Z, Space, Enter, J | A | A |
| `cancel` | X, Backspace, K | B | B |
| `action` | C, L | X | C |
| `pause` | Escape, P | Start | II |

Gamepad buttons use Xbox names; other controllers map to the same positions.

## Adding an action

Gem Garden adds `run`. Override `CreateInput` in `MainGame` and, while you are there, choose the on-screen controls
for touch screens with `CreateTouchLayout`:

<!-- code: samples/GemGarden/part2-hero/MainGame.cs#input -->
```csharp title="MainGame.cs"
/// <summary>The default actions (arrows, WASD, d-pad, left stick, confirm, cancel, pause) plus "run".</summary>
protected override InputMap CreateInput() => InputMap.CreateDefault()
    .Bind("run", [Key.LeftShift, Key.X], [PadButton.B, PadButton.RightShoulder]);

/// <summary>On touch screens: a floating joystick on the left, A (run, and confirm in menus) and pause on the right.</summary>
protected override TouchLayout CreateTouchLayout() => TouchLayout.Of(
    new TouchStick { Offset = new(104, 100) },
    new TouchButton("run", "run", "A") { Anchor = TouchAnchor.BottomRight, Offset = new(72, 92), Radius = 40, Also = ["confirm"] },
    new TouchButton("pause", "pause", "II") { Anchor = TouchAnchor.TopRight, Offset = new(30, 30), Radius = 20 });
```

(`using MicroEngine.Input;` at the top.)

- `InputMap.CreateDefault()` is the table above; `Bind("run", keys, buttons)` adds an action bound to Left Shift or X,
  and to the gamepad's B or right shoulder. Binding an existing action adds bindings to it; X now presses both
  `cancel` and `run`, which is fine because no screen reads both.
- The touch layout replaces the default d-pad with a **floating joystick**: it appears where the left thumb lands and
  gives an analog direction, so a light push walks slowly. The `A` button presses `run`, and also `confirm`
  (`Also`), so the same button starts the game from the title. Positions and sizes are in density-independent pixels
  (dp) from a corner of the screen's safe area, so the controls keep their size on any phone.

On-screen controls appear only where the platform has a touch screen: phones, tablets, touch laptops and mobile
browsers. On a desktop without touch nothing is drawn. More options (d-pads, toggle buttons, controls per scene):
[Input](../manual/input.md#touch-controls).

## The player class

Create `World/Player.cs`. Its fields and constructor:

<!-- code: samples/GemGarden/part2-hero/World/Player.cs#fields -->
```csharp title="World/Player.cs"
private const float WalkSpeed = 70, RunSpeed = 115;   // pixels per second

private readonly Animator _animator = new();
private RectF _bounds;          // a small box at the feet: what will collide with the world
private bool _facingLeft;

public Player(SpriteAtlas sprites, Vector2 start)
{
    _animator
        .Add("idle", new AnimationClip([sprites["hero0"]], 1))
        .Add("walk", new AnimationClip(sprites.Frames("hero"), 8));
    _animator.Play("idle");
    _bounds = RectF.FromCenter(start, 10, 8);
}

/// <summary>The point between the feet, in pixels: where the sprite's origin is drawn.</summary>
public Vector2 Position => new(_bounds.Center.X, _bounds.Bottom);
public RectF Bounds => _bounds;
```

The player is a plain C# class: MicroEngine has no entity system, and none is needed. Its state is a **box at its
feet** (`_bounds`, 10x8 pixels) rather than a point. The box is what will collide with the garden's hedges in step 7;
`Position`, the middle of its bottom edge, is where the sprite's origin (the feet) is drawn. The animator lines are
explained in [step 5](05-animation.md).

## Moving with `dt`

`Update` reads the actions and moves the box:

<!-- code: samples/GemGarden/part2-hero/World/Player.cs#update -->
```csharp title="World/Player.cs"
public void Update(float dt, InputMap input, RectF area)
{
    // Keys give unit directions (diagonals normalised); a stick or the touch joystick also gives how far it is pushed.
    var direction = input.Vector("left", "right", "up", "down");
    float speed = input.Down("run") ? RunSpeed : WalkSpeed;
    var moved = _bounds.Offset(direction * speed * dt);

    // Stay inside the area: the next part replaces this with collisions against the garden's hedges.
    _bounds = moved with
    {
        X = Math.Clamp(moved.X, area.Left, area.Right - moved.Width),
        Y = Math.Clamp(moved.Y, area.Top, area.Bottom - moved.Height),
    };

    if (direction.X != 0) _facingLeft = direction.X < 0;
    _animator.Play(direction == Vector2.Zero ? "idle" : "walk");
    _animator.Speed = speed / WalkSpeed;
    _animator.Update(dt);
}
```

- `input.Vector("left", "right", "up", "down")` combines four actions into a direction at most 1 long. With keys it is
  a unit vector (diagonals are normalised, so walking diagonally is not faster); with a stick or the touch joystick
  its length is how far the stick is pushed.
- `input.Down("run")` is true while run is held. Its siblings: `Pressed` (the step it went down), `Released` (the step
  it went up), `Value` (0..1, analog for triggers and sticks).
- Speeds are in **pixels per second**; multiplying by `dt` (the step, in seconds) gives the distance for this step.
  Because MicroEngine always steps the simulation by exactly `dt`, the hero walks at the same speed on every machine.
- `RectF` is an immutable rectangle: `Offset` returns a moved copy, and `with { X = ... }` a copy with other fields.
  Clamping keeps the box on screen until the garden has walls.

## The scene uses the player

The play scene creates the player and calls its `Update` every step:

<!-- code: samples/GemGarden/part2-hero/Scenes/PlayScene.cs#fields -->
```csharp title="Scenes/PlayScene.cs"
private readonly MainGame _game;
private readonly Player _player;

public PlayScene(MainGame game)
{
    _game = game;
    _player = new Player(game.Sprites, new Vector2(160, 100));
}
```

<!-- code: samples/GemGarden/part2-hero/Scenes/PlayScene.cs#update -->
```csharp title="Scenes/PlayScene.cs"
public override void Update(float dt)
{
    if (Input.Pressed("pause"))
    {
        Game.Scenes.Replace(new TitleScene(_game), Transition.Fade());
        return;
    }
    _player.Update(dt, Input, new RectF(0, 0, Game.Width, Game.Height));
}
```

The scene now leaves with `pause` (Escape, P, Start) instead of `cancel`, because X is also `run`. The scene's `Input`
is the game's `InputMap`, so actions bound in `CreateInput` work in every scene.

## Gamepads

Plug in a gamepad and it just works: the d-pad and the left stick press the direction actions, B and the right shoulder
run, Start pauses. MicroEngine reads one gamepad at a time (the first connected; the next one takes over when it is
unplugged). Triggers and the right stick can be bound to actions too, as analog values (`BindAxis`): see the
[gamepad example](../examples/focused.md#gamepad-and-analog-input).

## Next

The gardener slides around without moving his legs. [Step 5](05-animation.md) animates him; at its end, part 2 compiles
and runs.
