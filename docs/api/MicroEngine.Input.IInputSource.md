# Interface IInputSource

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

The raw device state a backend reports once per polled frame.

```csharp
public interface IInputSource
```

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

### <a id="MicroEngine_Input_IInputSource_Pointer"></a> Pointer

The mouse over the virtual canvas; sources without one keep the default (not present).

```csharp
PointerState Pointer { get; }
```

#### Property Value

 [PointerState](MicroEngine.Input.PointerState.md)

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
