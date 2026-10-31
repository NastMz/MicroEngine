# Input

Games never ask whether a key is down: they ask whether an *action* is, such as `"jump"` or `"confirm"`. An
`InputMap` binds each action to keyboard keys, gamepad buttons and a left-stick direction, samples the backend's
devices once per fixed step and computes press and release edges per step. Scripted input forces the same actions,
so automated runs exercise exactly the code players do.

## Default actions

`InputMap.CreateDefault()` is what `Game.CreateInput()` returns unless a game overrides it:

```csharp
public static InputMap CreateDefault() => new InputMap()
    .Bind("left", [Key.Left, Key.A], [PadButton.DPadLeft], StickDirection.Left)
    .Bind("right", [Key.Right, Key.D], [PadButton.DPadRight], StickDirection.Right)
    .Bind("up", [Key.Up, Key.W], [PadButton.DPadUp], StickDirection.Up)
    .Bind("down", [Key.Down, Key.S], [PadButton.DPadDown], StickDirection.Down)
    .Bind("confirm", [Key.Z, Key.Space, Key.Enter, Key.J], [PadButton.A])
    .Bind("cancel", [Key.X, Key.Backspace, Key.K], [PadButton.B])
    .Bind("action", [Key.C, Key.L], [PadButton.X])
    .Bind("pause", [Key.Escape, Key.P], [PadButton.Start]);
```

Source: [`src/MicroEngine/Input/Input.cs`](../../src/MicroEngine/Input/Input.cs)

## Custom bindings

Override `CreateInput` and chain `Bind` calls. Binding an existing action adds keys and buttons to it; binding a
new name creates it.

```csharp
protected override InputMap CreateInput() => InputMap.CreateDefault()
    .Bind("jump", [Key.Z, Key.Space, Key.J], [PadButton.A])
    .Bind("fire", [Key.X, Key.K], [PadButton.X, PadButton.B])
    .Bind("lock", [Key.C, Key.L, Key.LeftShift], [PadButton.RightShoulder, PadButton.LeftShoulder]);
```

Source: [`games/NeonRift/RiftGame.cs`](../../games/NeonRift/RiftGame.cs)

| Overload | Use |
| --- | --- |
| `Bind(action, params Key[] keys)` | Keys only, e.g. `Bind("action", Key.X)` |
| `Bind(action, Key[] keys, PadButton[] buttons, StickDirection stick = None)` | Keys, pad buttons and a stick direction |

The `Key` enum covers letters, digits, arrows, Space, Enter, Escape, Backspace, Tab, Shift, left Control and
F1-F5, F11, F12. `PadButton` uses Xbox names (`A`, `B`, `X`, `Y`, `Start`, `Back`, shoulders, d-pad). A stick
direction counts as down beyond half deflection. Only the first gamepad is read by the raylib backend.

## Querying actions

| Method | True when |
| --- | --- |
| `Down(action)` | The action is held (or was tapped during this step) |
| `Pressed(action)` | First step after it went down, even if it was released within the same frame |
| `Released(action)` | First step after it went up |
| `Axis(negative, positive)` | Returns -1, 0 or 1 from two opposing actions |
| `Actions` | Lists every bound action name |

```csharp
var move = new Vector2(Input.Axis("left", "right"), Input.Axis("up", "down"));
_position += move * 90 * dt;
if (Input.Pressed("confirm"))
{
    Audio.Play("ding", pitchJitter: 0.05f);
    // ...
}
```

Source: [`samples/HelloEngine/Program.cs`](../../samples/HelloEngine/Program.cs)

## How edges are computed

- `Sample(source)` runs before each fixed step and records which actions are down. An action that goes down is
  also *latched*, so a tap shorter than a step still produces one `Pressed`.
- `EndUpdate()` runs after each step: the current state becomes the previous state and latches clear. A tap is
  therefore never lost and never reported twice.
- `Reset()` runs on every scene push and pop: held actions count as already held, so the key that opened a menu
  does not immediately press its first option.

The raylib backend adds one more guarantee: a key pressed and released between two frames is reported as down
until a simulation step has read it (see `RaylibInput.CollectTaps` in
[`RaylibPlatform.cs`](../../src/MicroEngine.Raylib/RaylibPlatform.cs)).

## Scripted input

`InputScript` drives actions by update number. It is used by the desktop `--script` argument, by tests and by
development web packages.

```text
# comment
30 press confirm          # down for 2 updates
60 hold right 90          # down for 90 updates
60 down jump / 75 up jump # explicit edges
200 capture title.png
900 quit
```

Source: format documented on `InputScript` in [`src/MicroEngine/Core/GameRunner.cs`](../../src/MicroEngine/Core/GameRunner.cs)

| Command | Effect |
| --- | --- |
| `N press action` | Down at update N for 2 updates |
| `N hold action D` | Down at update N for D updates |
| `N down action` / `N up action` | Explicit edges |
| `N capture file.png` | Save the frame after update N into the capture directory |
| `N quit` | End the run at update N |

`@+N` instead of an absolute number means N updates after the previous command. A malformed line throws a
`FormatException` naming the line. Scripts force actions through `InputMap.SetForced`, which tests can also call
directly. In code the same script can be built with `Press`, `Hold`, `Capture` and `QuitAfter`.

On the desktop, a game started through `DesktopLauncher` plays a script when it is given
`--script FILE`; `--hidden`, `--mute` and `--captures DIR` run it without a visible window or sound and choose
where captures go ([Backends](backends.md#desktop-raylib)).

On desktop a script implies a fixed step (one update per frame) and seed 1 unless `--seed` is given, so the run and its captures are reproducible.

## Browser input

In a browser the same bindings apply. The page keeps keys that would scroll or trigger browser features (arrows,
Space, Tab, `/`, `'`) away from the browser while playing, and leaves Ctrl/Cmd shortcuts and function keys to it.
Details: [Embedding](../examples/embedding.md#the-iframe).

## Limitations

- Keyboard and gamepad only. There is no mouse or pointer API, and no touch input or on-screen controls.
- No runtime rebinding UI; bindings are code.
- One gamepad (index 0), left stick only, digital thresholds; no triggers or analog values exposed to games.
- In the browser, gamepad play was validated manually in one Chromium-based browser with the controller that was
  tested; no claim is made for other controllers or browsers ([Platforms](../guide/platforms.md#gamepads)).
