# Class VirtualControls

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.Touch.dll  
Guide: [Input](../manual/input.md)  

On-screen controls at run time: hit testing, finger tracking, d-pad and joystick values, tap latching, haptics and
drawing. It is an [IActionSource](MicroEngine.Input.IActionSource.md) (read by [InputMap](MicroEngine.Input.InputMap.md) with the keyboard and gamepad), an
[ITouchOverlay](MicroEngine.Input.ITouchOverlay.md) (drawn by the backend over the game) and an input layer ([IInputLayer](MicroEngine.Input.IInputLayer.md), at
[TouchControls](MicroEngine.Input.InputLayerOrder.md#MicroEngine_Input_InputLayerOrder_TouchControls)): a finger that lands on a control is the control's, every other finger
goes on to gameplay as a pointer. Each finger belongs to the control it started on until it lifts, so two thumbs
work independently and a thumb that slides a little stays on its control.

```csharp
public sealed class VirtualControls : IActionSource, ITouchOverlay, IInputLayer
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[VirtualControls](MicroEngine.Input.VirtualControls.md)

#### Implements

IActionSource, 
[ITouchOverlay](MicroEngine.Input.ITouchOverlay.md), 
IInputLayer

## Constructors

### <a id="MicroEngine_Input_VirtualControls__ctor_MicroEngine_Input_TouchLayout_"></a> VirtualControls\(TouchLayout?\)

Creates the controls for <code class="paramref">layout</code> (null: [CreateDefault](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_CreateDefault_MicroEngine_Input_ActionNames_)). Games use the instance in <code>Game.Touch</code>.

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

 InputDevice

### <a id="MicroEngine_Input_VirtualControls_FingerCount"></a> FingerCount

Fingers currently on the screen (on a control or not).

```csharp
public int FingerCount { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_VirtualControls_IsActive"></a> IsActive

Whether the controls take part in the game: the game drawing them over the frame, reading them as an action source
and giving them the touches over them. A game's controls start inactive and cost nothing; they become active when
the game sets a [Layout](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_Layout) or the platform's touch screen is touch-first ([TouchFirst](MicroEngine.Input.ITouchScreen.md#MicroEngine_Input_ITouchScreen_TouchFirst)),
and stay active. Controls created and driven directly ([Update](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_Update_MicroEngine_Input_ITouchScreen_System_Single_MicroEngine_Input_InputDevice_)) work whatever this says.

```csharp
public bool IsActive { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_VirtualControls_KeepFrameClear"></a> KeepFrameClear

Whether the game frame is fitted around the controls while they are shown (default true): on screens close to
16:9, where the letterbox bars cannot hold them, the frame is made as large as it can be without any control
covering it (see [Frame](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_Frame_System_Numerics_Vector2_MicroEngine_RectF_System_Int32_System_Int32_MicroEngine_Graphics_Presentation_)).

```csharp
public bool KeepFrameClear { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_VirtualControls_LastFrame"></a> LastFrame

The frame chosen by the last [Frame](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_Frame_System_Numerics_Vector2_MicroEngine_RectF_System_Int32_System_Int32_MicroEngine_Graphics_Presentation_) call, in screen pixels (tests and diagnostics).

```csharp
public RectF LastFrame { get; }
```

#### Property Value

 RectF

### <a id="MicroEngine_Input_VirtualControls_Layout"></a> Layout

The layout in use. Setting it (a scene with other controls) releases every finger and activates the controls
([IsActive](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_IsActive)).

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

### <a id="MicroEngine_Input_VirtualControls_Activate"></a> Activate\(\)

Activates the controls ([IsActive](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_IsActive)); the game wires them in at its next frame. Does nothing when they already are.

```csharp
public void Activate()
```

### <a id="MicroEngine_Input_VirtualControls_BeginFrame_MicroEngine_Input_ITouchScreen_"></a> BeginFrame\(ITouchScreen?\)

Starts a frame: taps an update has read stop counting, and the controls are laid out again when the screen's
size, safe area or density changed (releasing every finger). Called by the loop before the frame's touches.

```csharp
public void BeginFrame(ITouchScreen? screen)
```

#### Parameters

`screen` [ITouchScreen](MicroEngine.Input.ITouchScreen.md)?

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

### <a id="MicroEngine_Input_VirtualControls_EndFrame_System_Single_MicroEngine_Input_InputDevice_"></a> EndFrame\(float, InputDevice\)

Ends a frame's touches: works out the actions the controls press, whether they are shown and their press
animations. <code class="paramref">otherDevice</code> is the device that last pressed an action (a keyboard or gamepad hides
[Auto](MicroEngine.Input.TouchVisibility.md) controls).

```csharp
public void EndFrame(float dt, InputDevice otherDevice = InputDevice.None)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

`otherDevice` InputDevice

### <a id="MicroEngine_Input_VirtualControls_Frame_System_Numerics_Vector2_MicroEngine_RectF_System_Int32_System_Int32_MicroEngine_Graphics_Presentation_"></a> Frame\(Vector2, RectF, int, int, Presentation?\)

Where the backend draws the game frame. Without shown controls it is [Fit](MicroEngine.Input.FrameLayout.md#MicroEngine_Input_FrameLayout_Fit_System_Numerics_Vector2_MicroEngine_RectF_System_Int32_System_Int32_MicroEngine_Graphics_Presentation_). With shown
controls that would cover the largest frame, the frame moves to the top of the safe area, or just below a control
anchored at the top (horizontally centred), and takes the largest size at which no control of the layout overlaps it, never below
[MinimumFrameFraction](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_MinimumFrameFraction); a whole-number scale is kept when it is within 90 % of that size, so pixels
stay even when it costs little (unless the presentation's rounding is fractional, as in [HighResolution2D](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_HighResolution2D); null <code class="paramref">presentation</code> is [PixelArt](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_PixelArt)). Controls hidden by context still count, so a dialogue does not resize the game.

```csharp
public RectF Frame(Vector2 window, RectF safeArea, int virtualWidth, int virtualHeight, Presentation? presentation = null)
```

#### Parameters

`window` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`safeArea` RectF

`virtualWidth` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`virtualHeight` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`presentation` Presentation?

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

### <a id="MicroEngine_Input_VirtualControls_Handle_MicroEngine_Input_InputEvent__"></a> Handle\(in InputEvent\)

The input layer: takes the touch-screen pointers ([Touch](MicroEngine.Input.PointerKind.md) of
[TouchScreen](MicroEngine.Input.DeviceId.md#MicroEngine_Input_DeviceId_TouchScreen)) that land on a control ([HandleTouch](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_HandleTouch_MicroEngine_Input_TouchEvent_), with
[ScreenPosition](MicroEngine.Input.InputEvent.md#MicroEngine_Input_InputEvent_ScreenPosition)); everything else goes on.

```csharp
public bool Handle(in InputEvent e)
```

#### Parameters

`e` InputEvent

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_VirtualControls_HandleTouch_MicroEngine_Input_TouchEvent_"></a> HandleTouch\(TouchEvent\)

Applies one touch (screen pixels). Returns whether the finger is a control's: a finger that lands on a visible
control is, until it lifts; any other finger is not, and its events are left for gameplay.

```csharp
public bool HandleTouch(TouchEvent e)
```

#### Parameters

`e` [TouchEvent](MicroEngine.Input.TouchEvent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

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

The actions the current context needs (the top scene's <code>Scene.TouchActions</code>, set by the game
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

### <a id="MicroEngine_Input_VirtualControls_TakeScripted_System_Collections_Generic_List_MicroEngine_Input_TouchEvent__"></a> TakeScripted\(List<TouchEvent\>\)

Moves the scripted touches queued since the previous frame ([ScriptTouch](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_ScriptTouch_System_Int32_System_String_System_Numerics_Vector2_), [ScriptTouchAt](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_ScriptTouchAt_System_Int32_System_Numerics_Vector2_),
[ScriptLift](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_ScriptLift_System_Int32_)) into <code class="paramref">events</code> as touch events in screen pixels, after the events
already in it. Call after [BeginFrame](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_BeginFrame_MicroEngine_Input_ITouchScreen_), which lays the controls out; without a screen they stay queued.

```csharp
public void TakeScripted(List<TouchEvent> events)
```

#### Parameters

`events` [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[TouchEvent](MicroEngine.Input.TouchEvent.md)\>

### <a id="MicroEngine_Input_VirtualControls_Update_MicroEngine_Input_ITouchScreen_System_Single_MicroEngine_Input_InputDevice_"></a> Update\(ITouchScreen?, float, InputDevice\)

Called once per frame by the loop, before the steps: lays the controls out for the screen, applies its touch
events and scripted touches, and advances visibility and press animations. <code class="paramref">otherDevice</code> is the
device that last pressed an action (a keyboard or gamepad hides [Auto](MicroEngine.Input.TouchVisibility.md) controls).
Equivalent to [BeginFrame](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_BeginFrame_MicroEngine_Input_ITouchScreen_), then every touch of the screen and every scripted touch through
[HandleTouch](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_HandleTouch_MicroEngine_Input_TouchEvent_), then [EndFrame](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_EndFrame_System_Single_MicroEngine_Input_InputDevice_); the engine's loop routes the touches through the input
layers instead, so fingers no control takes reach gameplay.

```csharp
public void Update(ITouchScreen? screen, float dt, InputDevice otherDevice = InputDevice.None)
```

#### Parameters

`screen` [ITouchScreen](MicroEngine.Input.ITouchScreen.md)?

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

`otherDevice` InputDevice

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
