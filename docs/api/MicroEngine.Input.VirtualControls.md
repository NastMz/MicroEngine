# Class VirtualControls

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

On-screen controls at run time: hit testing, finger tracking, d-pad and joystick values, tap latching, haptics and
drawing. It is an [IActionSource](MicroEngine.Input.IActionSource.md) (read by [InputMap](MicroEngine.Input.InputMap.md) with the keyboard and gamepad) and an
[ITouchOverlay](MicroEngine.Input.ITouchOverlay.md) (drawn by the backend over the game). Each finger belongs to the control it started on
until it lifts, so two thumbs work independently and a thumb that slides a little stays on its control.

```csharp
public sealed class VirtualControls : IActionSource, ITouchOverlay
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[VirtualControls](MicroEngine.Input.VirtualControls.md)

#### Implements

[IActionSource](MicroEngine.Input.IActionSource.md), 
[ITouchOverlay](MicroEngine.Input.ITouchOverlay.md)

## Constructors

### <a id="MicroEngine_Input_VirtualControls__ctor_MicroEngine_Input_TouchLayout_"></a> VirtualControls\(TouchLayout?\)

Creates the controls for <code class="paramref">layout</code> (null: [CreateDefault](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_CreateDefault)). Games use the instance in [Touch](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Touch).

```csharp
public VirtualControls(TouchLayout? layout = null)
```

#### Parameters

`layout` [TouchLayout](MicroEngine.Input.TouchLayout.md)?

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

Two controls share an id.

## Properties

### <a id="MicroEngine_Input_VirtualControls_Actions"></a> Actions

Every action a control of the layout can press.

```csharp
public IEnumerable<string> Actions { get; }
```

#### Property Value

 [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Input_VirtualControls_Device"></a> Device

Always [Touch](MicroEngine.Input.InputDevice.md).

```csharp
public InputDevice Device { get; }
```

#### Property Value

 [InputDevice](MicroEngine.Input.InputDevice.md)

### <a id="MicroEngine_Input_VirtualControls_FingerCount"></a> FingerCount

Fingers currently on the screen (on a control or not).

```csharp
public int FingerCount { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_VirtualControls_KeepFrameClear"></a> KeepFrameClear

Whether the game frame is fitted around the controls while they are shown (default true): on screens close to
16:9, where the letterbox bars cannot hold them, the frame is made as large as it can be without any control
covering it (see [Frame](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_Frame_System_Numerics_Vector2_MicroEngine_RectF_System_Int32_System_Int32_MicroEngine_Graphics_CanvasScaling_)).

```csharp
public bool KeepFrameClear { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_VirtualControls_LastFrame"></a> LastFrame

The frame chosen by the last [Frame](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_Frame_System_Numerics_Vector2_MicroEngine_RectF_System_Int32_System_Int32_MicroEngine_Graphics_CanvasScaling_) call, in screen pixels (tests and diagnostics).

```csharp
public RectF LastFrame { get; }
```

#### Property Value

 RectF

### <a id="MicroEngine_Input_VirtualControls_Layout"></a> Layout

The layout in use. Setting it (a scene with other controls) releases every finger.

```csharp
public TouchLayout Layout { get; set; }
```

#### Property Value

 [TouchLayout](MicroEngine.Input.TouchLayout.md)

### <a id="MicroEngine_Input_VirtualControls_MinimumFrameFraction"></a> MinimumFrameFraction

The smallest frame, as a fraction of the largest, that [KeepFrameClear](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_KeepFrameClear) may choose; below it controls overlap the edge of the frame instead.

```csharp
public float MinimumFrameFraction { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_VirtualControls_PixelsPerDp"></a> PixelsPerDp

Pixels per dp used by the current layout, after shrinking to fit the screen.

```csharp
public float PixelsPerDp { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_VirtualControls_Shown"></a> Shown

Whether the controls are drawn this frame.

```csharp
public bool Shown { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_VirtualControls_TouchFirst"></a> TouchFirst

Whether the screen they were last laid out for is touch-first.

```csharp
public bool TouchFirst { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_VirtualControls_Vibrations"></a> Vibrations

Vibrations requested since start (tests and diagnostics).

```csharp
public int Vibrations { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Input_VirtualControls_CancelAll"></a> CancelAll\(\)

Releases every finger, as when the game loses focus or is suspended. Toggled buttons stay as they are.

```csharp
public void CancelAll()
```

### <a id="MicroEngine_Input_VirtualControls_Draw_MicroEngine_Input_IOverlayCanvas_"></a> Draw\(IOverlayCanvas\)

Draws the shown controls with the layout's skin ([Skin](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_Skin), by default
[DefaultTouchSkin](MicroEngine.Input.DefaultTouchSkin.md) shaped by the layout's styles), each with its state this frame.

```csharp
public void Draw(IOverlayCanvas canvas)
```

#### Parameters

`canvas` [IOverlayCanvas](MicroEngine.Input.IOverlayCanvas.md)

### <a id="MicroEngine_Input_VirtualControls_Frame_System_Numerics_Vector2_MicroEngine_RectF_System_Int32_System_Int32_MicroEngine_Graphics_CanvasScaling_"></a> Frame\(Vector2, RectF, int, int, CanvasScaling\)

Where the backend draws the game frame. Without shown controls it is [Fit](MicroEngine.Input.FrameLayout.md#MicroEngine_Input_FrameLayout_Fit_System_Numerics_Vector2_MicroEngine_RectF_System_Int32_System_Int32_MicroEngine_Graphics_CanvasScaling_). With shown
controls that would cover the largest frame, the frame moves to the top of the safe area, or just below a control
anchored at the top (horizontally centred), and takes the largest size at which no control of the layout overlaps it, never below
[MinimumFrameFraction](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_MinimumFrameFraction); a whole-number scale is kept when it is within 90 % of that size, so pixels
stay even when it costs little (pixel scaling only). Controls hidden by context still count, so a dialogue does not resize the game.

```csharp
public RectF Frame(Vector2 window, RectF safeArea, int virtualWidth, int virtualHeight, CanvasScaling scaling = CanvasScaling.Pixel)
```

#### Parameters

`window` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`safeArea` RectF

`virtualWidth` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`virtualHeight` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`scaling` [CanvasScaling](MicroEngine.Graphics.CanvasScaling.md)

#### Returns

 RectF

### <a id="MicroEngine_Input_VirtualControls_Geometry_System_String_"></a> Geometry\(string\)

The control's centre and drawn radius in screen pixels, as last laid out.

```csharp
public (Vector2 Center, float Radius) Geometry(string id)
```

#### Parameters

`id` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 \([Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2) [Center](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.numerics.vector2,system.single\-.center), [float](https://learn.microsoft.com/dotnet/api/system.single) [Radius](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.numerics.vector2,system.single\-.radius)\)

### <a id="MicroEngine_Input_VirtualControls_IsDown_System_String_"></a> IsDown\(string\)

Whether a control holds <code class="paramref">action</code> down; a tap shorter than an update still counts once.

```csharp
public bool IsDown(string action)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_VirtualControls_IsPressed_System_String_"></a> IsPressed\(string\)

Whether the control is pressed (a button held or toggled on, a d-pad or stick pushed).

```csharp
public bool IsPressed(string id)
```

#### Parameters

`id` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_VirtualControls_IsVisible_System_String_"></a> IsVisible\(string\)

Whether the control <code class="paramref">id</code> is shown and touchable, after [SetVisible](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_SetVisible_System_String_System_Boolean_) and the scene's context.

```csharp
public bool IsVisible(string id)
```

#### Parameters

`id` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

No control has that id.

### <a id="MicroEngine_Input_VirtualControls_ScriptLift_System_Int32_"></a> ScriptLift\(int\)

Lifts a scripted finger.

```csharp
public void ScriptLift(int finger)
```

#### Parameters

`finger` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_VirtualControls_ScriptTouch_System_Int32_System_String_System_Numerics_Vector2_"></a> ScriptTouch\(int, string, Vector2\)

A scripted touch, as an input script's <code>touch</code> command: finger <code class="paramref">finger</code> goes down (or moves)
on control <code class="paramref">target</code>, displaced by <code class="paramref">offset</code> radii from its centre, at the next
update. The touch goes through the same hit testing as a real finger.

```csharp
public void ScriptTouch(int finger, string target, Vector2 offset)
```

#### Parameters

`finger` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`target` [string](https://learn.microsoft.com/dotnet/api/system.string)

`offset` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Input_VirtualControls_ScriptTouchAt_System_Int32_System_Numerics_Vector2_"></a> ScriptTouchAt\(int, Vector2\)

A scripted touch at a point given as fractions (0..1) of the screen.

```csharp
public void ScriptTouchAt(int finger, Vector2 normalized)
```

#### Parameters

`finger` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`normalized` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Input_VirtualControls_SetContext_System_Collections_Generic_IEnumerable_System_String__"></a> SetContext\(IEnumerable<string\>?\)

The actions the current context needs (the top scene's [TouchActions](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_TouchActions), set by the game
every frame): only controls that press at least one of them are shown and touchable; null shows them all. A
dialogue that only advances with "confirm" hides the d-pad over its text, keeps the button that confirms, and
the hidden controls release their fingers. The game frame does not change size with the context.

```csharp
public void SetContext(IEnumerable<string>? actions)
```

#### Parameters

`actions` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

### <a id="MicroEngine_Input_VirtualControls_SetVisible_System_String_System_Boolean_"></a> SetVisible\(string, bool\)

Shows or hides one control (hidden ones ignore touches and release their finger).

```csharp
public void SetVisible(string id, bool visible)
```

#### Parameters

`id` [string](https://learn.microsoft.com/dotnet/api/system.string)

`visible` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_VirtualControls_Update_MicroEngine_Input_ITouchScreen_System_Single_MicroEngine_Input_InputDevice_"></a> Update\(ITouchScreen?, float, InputDevice\)

Called once per frame by the loop, before the steps: lays the controls out for the screen, applies its touch
events and scripted touches, and advances visibility and press animations. <code class="paramref">otherDevice</code> is the
device that last pressed an action (a keyboard or gamepad hides [Auto](MicroEngine.Input.TouchVisibility.md) controls).

```csharp
public void Update(ITouchScreen? screen, float dt, InputDevice otherDevice = InputDevice.None)
```

#### Parameters

`screen` [ITouchScreen](MicroEngine.Input.ITouchScreen.md)?

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

`otherDevice` [InputDevice](MicroEngine.Input.InputDevice.md)

### <a id="MicroEngine_Input_VirtualControls_Value_System_String_"></a> Value\(string\)

The action's strength, 0..1: a joystick's deflection, 1 for a pressed button or d-pad direction.

```csharp
public float Value(string action)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_VirtualControls_ValueOf_System_String_"></a> ValueOf\(string\)

A stick's value after its dead zone, or a d-pad's direction, each axis -1..1.

```csharp
public Vector2 ValueOf(string id)
```

#### Parameters

`id` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

</div>
