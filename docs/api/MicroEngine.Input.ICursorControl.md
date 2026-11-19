# Interface ICursorControl

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The mouse cursor over the game's window: shown or hidden, free or locked. A platform capability
(<code>Game.TryGetCapability</code>): platforms with no cursor to control (phones, a console) do not offer it, so a game
asks and goes on without it.

```csharp
public interface ICursorControl
```

## Examples

```csharp
// Hold the secondary button to look around; the cursor is back where it was when it is let go.
if (Game.TryGetCapability<ICursorControl>(out var cursor))
{
    if (Input.Pressed("look")) cursor.SetRelative(true);
    if (Input.Released("look")) cursor.SetRelative(false);
}
camera.Yaw += Input.Motion("look-left", "look-right", "look-up", "look-down").X * 0.2f;
```

## Remarks

<p>
[Visible](MicroEngine.Input.ICursorControl.md#MicroEngine_Input_ICursorControl_Visible) hides the system's arrow while it is over the window, for a game that draws its own cursor or
none. [SetRelative](MicroEngine.Input.ICursorControl.md#MicroEngine_Input_ICursorControl_SetRelative_System_Boolean_) locks the cursor: it disappears, cannot leave the window, and the mouse reports only
how far it moved ([Delta](MicroEngine.Input.PointerState.md#MicroEngine_Input_PointerState_Delta), or an action bound to [PointerRight](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_PointerRight)
and its siblings) while [Position](MicroEngine.Input.PointerState.md#MicroEngine_Input_PointerState_Position) stays where it was. That is what looking around with the
mouse, or dragging a view without running out of desk, needs.
</p>
<p>
On desktops both take effect at once. In a browser a page may lock the cursor only in answer to a click or a key
press, and the player's Escape key always frees it: ask from the update in which the player clicked, and read
[Relative](MicroEngine.Input.ICursorControl.md#MicroEngine_Input_ICursorControl_Relative) afterwards instead of assuming it stayed on. The window losing the focus frees the cursor
everywhere; the lock comes back with the focus on desktops.
</p>

## Properties

### <a id="MicroEngine_Input_ICursorControl_Relative"></a> Relative

Whether the cursor is locked to the window and the mouse reports motion only.

```csharp
bool Relative { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_ICursorControl_Visible"></a> Visible

Whether the system's cursor is drawn while it is over the window (true at start). A locked cursor is never drawn.

```csharp
bool Visible { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Input_ICursorControl_SetRelative_System_Boolean_"></a> SetRelative\(bool\)

Locks (<code class="paramref">relative</code>) or frees the cursor. Returns whether the platform did it: false when the
system refused (a browser outside a click or key press).

```csharp
bool SetRelative(bool relative)
```

#### Parameters

`relative` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
