# Interface IInputSource

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The raw device state a backend reports once per polled frame.

```csharp
public interface IInputSource
```

## Remarks

Engine backends deliver events instead ([IInputBackend](MicroEngine.Input.IInputBackend.md)); a polled source is still read every frame and
its changes become the same events, so a host's extra source or a test's hand-set input is one more device.

## Properties

### <a id="MicroEngine_Input_IInputSource_LeftStick"></a> LeftStick

Left stick, each axis in -1..1.

```csharp
Vector2 LeftStick { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Input_IInputSource_PadConnected"></a> PadConnected

Whether a gamepad is connected. Sources without gamepads keep the default.

```csharp
bool PadConnected { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_IInputSource_PadStyle"></a> PadStyle

The family of the connected gamepad, for button names in prompts; sources that cannot tell keep Xbox.

```csharp
GamepadStyle PadStyle { get; }
```

#### Property Value

 [GamepadStyle](MicroEngine.Input.GamepadStyle.md)

### <a id="MicroEngine_Input_IInputSource_Pointer"></a> Pointer

The mouse over the virtual canvas; sources without one keep the default (not present).

```csharp
PointerState Pointer { get; }
```

#### Property Value

 [PointerState](MicroEngine.Input.PointerState.md)

### <a id="MicroEngine_Input_IInputSource_Pointers"></a> Pointers

Every pointer: the mouse while over the window, each finger or pen on the screen. Sources without pointers keep the default (none).

```csharp
IReadOnlyList<PointerInfo> Pointers { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[PointerInfo](MicroEngine.Input.PointerInfo.md)\>

## Methods

### <a id="MicroEngine_Input_IInputSource_GetPadAxis_MicroEngine_Input_PadAxis_"></a> GetPadAxis\(PadAxis\)

Any gamepad axis. Sources that only know the left stick keep the default.

```csharp
float GetPadAxis(PadAxis axis)
```

#### Parameters

`axis` [PadAxis](MicroEngine.Input.PadAxis.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_IInputSource_IsKeyDown_MicroEngine_Input_Key_"></a> IsKeyDown\(Key\)

Whether <code class="paramref">key</code> is held.

```csharp
bool IsKeyDown(Key key)
```

#### Parameters

`key` [Key](MicroEngine.Input.Key.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_IInputSource_IsPadDown_MicroEngine_Input_PadButton_"></a> IsPadDown\(PadButton\)

Whether <code class="paramref">button</code> is held on the gamepad.

```csharp
bool IsPadDown(PadButton button)
```

#### Parameters

`button` [PadButton](MicroEngine.Input.PadButton.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
