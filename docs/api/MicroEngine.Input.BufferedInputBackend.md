# Class BufferedInputBackend

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

A base for input backends of platforms whose system reports input as it happens or as per-frame state: a backend
emits events ([Emit](MicroEngine.Input.BufferedInputBackend.md#MicroEngine_Input_BufferedInputBackend_Emit_MicroEngine_Input_InputEvent__)) while it processes the system's, and the engine drains them
([Drain](MicroEngine.Input.BufferedInputBackend.md#MicroEngine_Input_BufferedInputBackend_Drain_System_Collections_Generic_List_MicroEngine_Input_InputEvent__)). It also answers the polled questions ([IInputSource](MicroEngine.Input.IInputSource.md)) from the same events, for
code that reads a platform's <code>Input</code> directly; there a press shorter than a frame reads as held until the next
[BeginFrame](MicroEngine.Input.BufferedInputBackend.md#MicroEngine_Input_BufferedInputBackend_BeginFrame).

```csharp
public abstract class BufferedInputBackend : IInputBackend, IInputSource
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BufferedInputBackend](MicroEngine.Input.BufferedInputBackend.md)

#### Implements

[IInputBackend](MicroEngine.Input.IInputBackend.md), 
[IInputSource](MicroEngine.Input.IInputSource.md)

## Constructors

### <a id="MicroEngine_Input_BufferedInputBackend__ctor"></a> BufferedInputBackend\(\)

Creates the backend with an empty queue.

```csharp
protected BufferedInputBackend()
```

## Properties

### <a id="MicroEngine_Input_BufferedInputBackend_LeftStick"></a> LeftStick

Left stick, each axis in -1..1.

```csharp
public Vector2 LeftStick { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Input_BufferedInputBackend_PadConnected"></a> PadConnected

Whether a gamepad is connected. Sources without gamepads keep the default.

```csharp
public bool PadConnected { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_BufferedInputBackend_PadStyle"></a> PadStyle

The family of the connected gamepad, for button names in prompts; sources that cannot tell keep Xbox.

```csharp
public GamepadStyle PadStyle { get; }
```

#### Property Value

 [GamepadStyle](MicroEngine.Input.GamepadStyle.md)

### <a id="MicroEngine_Input_BufferedInputBackend_Pointer"></a> Pointer

The mouse over the virtual canvas; sources without one keep the default (not present).

```csharp
public PointerState Pointer { get; }
```

#### Property Value

 [PointerState](MicroEngine.Input.PointerState.md)

### <a id="MicroEngine_Input_BufferedInputBackend_Pointers"></a> Pointers

Every pointer: the mouse while over the window, each finger or pen on the screen. Sources without pointers keep the default (none).

```csharp
public IReadOnlyList<PointerInfo> Pointers { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[PointerInfo](MicroEngine.Input.PointerInfo.md)\>

### <a id="MicroEngine_Input_BufferedInputBackend_State"></a> State

The state of the devices this backend reported, as its polled view reads it.

```csharp
protected InputDevices State { get; }
```

#### Property Value

 [InputDevices](MicroEngine.Input.InputDevices.md)

## Methods

### <a id="MicroEngine_Input_BufferedInputBackend_BeginFrame"></a> BeginFrame\(\)

Starts a frame of the polled view: presses already released stop reading as held. Call before the frame's events.

```csharp
public void BeginFrame()
```

### <a id="MicroEngine_Input_BufferedInputBackend_Drain_System_Collections_Generic_List_MicroEngine_Input_InputEvent__"></a> Drain\(List<InputEvent\>\)

Moves every event since the previous call into <code class="paramref">events</code>, oldest first.

```csharp
public void Drain(List<InputEvent> events)
```

#### Parameters

`events` [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[InputEvent](MicroEngine.Input.InputEvent.md)\>

### <a id="MicroEngine_Input_BufferedInputBackend_Emit_MicroEngine_Input_InputEvent__"></a> Emit\(in InputEvent\)

Queues <code class="paramref">e</code> for the engine and applies it to the polled view.

```csharp
protected void Emit(in InputEvent e)
```

#### Parameters

`e` [InputEvent](MicroEngine.Input.InputEvent.md)

### <a id="MicroEngine_Input_BufferedInputBackend_GetPadAxis_MicroEngine_Input_PadAxis_"></a> GetPadAxis\(PadAxis\)

Any gamepad axis. Sources that only know the left stick keep the default.

```csharp
public float GetPadAxis(PadAxis axis)
```

#### Parameters

`axis` [PadAxis](MicroEngine.Input.PadAxis.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_BufferedInputBackend_IsKeyDown_MicroEngine_Input_Key_"></a> IsKeyDown\(Key\)

Whether <code class="paramref">key</code> is held.

```csharp
public bool IsKeyDown(Key key)
```

#### Parameters

`key` [Key](MicroEngine.Input.Key.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_BufferedInputBackend_IsPadDown_MicroEngine_Input_PadButton_"></a> IsPadDown\(PadButton\)

Whether <code class="paramref">button</code> is held on the gamepad.

```csharp
public bool IsPadDown(PadButton button)
```

#### Parameters

`button` [PadButton](MicroEngine.Input.PadButton.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_BufferedInputBackend_ReleaseHeld_System_Double_"></a> ReleaseHeld\(double\)

Emits releases of every key, button and pointer button this backend reported held, as the window loses the
focus (the system will not send them).

```csharp
protected void ReleaseHeld(double time)
```

#### Parameters

`time` [double](https://learn.microsoft.com/dotnet/api/system.double)

</div>
