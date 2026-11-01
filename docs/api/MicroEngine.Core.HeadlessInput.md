# Class HeadlessInput

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Keys, a gamepad and a pointer that tests set by hand.

```csharp
public sealed class HeadlessInput : IInputSource
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[HeadlessInput](MicroEngine.Core.HeadlessInput.md)

#### Implements

[IInputSource](MicroEngine.Input.IInputSource.md)

## Constructors

### <a id="MicroEngine_Core_HeadlessInput__ctor"></a> HeadlessInput\(\)

```csharp
public HeadlessInput()
```

## Properties

### <a id="MicroEngine_Core_HeadlessInput_LeftStick"></a> LeftStick

Left stick, each axis in -1..1.

```csharp
public Vector2 LeftStick { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Core_HeadlessInput_PadConnected"></a> PadConnected

Whether a gamepad is connected. Sources without gamepads keep the default.

```csharp
public bool PadConnected { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_HeadlessInput_Pointer"></a> Pointer

The mouse over the virtual canvas; sources without one keep the default (not present).

```csharp
public PointerState Pointer { get; set; }
```

#### Property Value

 [PointerState](MicroEngine.Input.PointerState.md)

## Methods

### <a id="MicroEngine_Core_HeadlessInput_GetPadAxis_MicroEngine_Input_PadAxis_"></a> GetPadAxis\(PadAxis\)

Any gamepad axis. Sources that only know the left stick keep the default.

```csharp
public float GetPadAxis(PadAxis axis)
```

#### Parameters

`axis` [PadAxis](MicroEngine.Input.PadAxis.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Core_HeadlessInput_IsKeyDown_MicroEngine_Input_Key_"></a> IsKeyDown\(Key\)

Whether <code class="paramref">key</code> is held.

```csharp
public bool IsKeyDown(Key key)
```

#### Parameters

`key` [Key](MicroEngine.Input.Key.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_HeadlessInput_IsPadDown_MicroEngine_Input_PadButton_"></a> IsPadDown\(PadButton\)

Whether <code class="paramref">button</code> is held on the gamepad.

```csharp
public bool IsPadDown(PadButton button)
```

#### Parameters

`button` [PadButton](MicroEngine.Input.PadButton.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_HeadlessInput_Set_MicroEngine_Input_Key_System_Boolean_"></a> Set\(Key, bool\)

Presses or releases a key.

```csharp
public void Set(Key key, bool down)
```

#### Parameters

`key` [Key](MicroEngine.Input.Key.md)

`down` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_HeadlessInput_Set_MicroEngine_Input_PadButton_System_Boolean_"></a> Set\(PadButton, bool\)

Presses or releases a gamepad button.

```csharp
public void Set(PadButton button, bool down)
```

#### Parameters

`button` [PadButton](MicroEngine.Input.PadButton.md)

`down` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_HeadlessInput_Set_MicroEngine_Input_PadAxis_System_Single_"></a> Set\(PadAxis, float\)

Sets a gamepad axis (sticks -1..1, triggers 0..1).

```csharp
public void Set(PadAxis axis, float value)
```

#### Parameters

`axis` [PadAxis](MicroEngine.Input.PadAxis.md)

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Core_HeadlessInput_SetLeftStick_System_Numerics_Vector2_"></a> SetLeftStick\(Vector2\)

Sets both axes of the left stick.

```csharp
public void SetLeftStick(Vector2 value)
```

#### Parameters

`value` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)
