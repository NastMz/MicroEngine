# Class CompositeInput

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Several input sources read as one: a key or button is down when any source has it down; the stick or axis with the largest deflection wins.

```csharp
public sealed class CompositeInput : IInputSource
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CompositeInput](MicroEngine.Core.CompositeInput.md)

#### Implements

[IInputSource](MicroEngine.Input.IInputSource.md)

## Constructors

### <a id="MicroEngine_Core_CompositeInput__ctor_System_Collections_Generic_IReadOnlyList_MicroEngine_Input_IInputSource__"></a> CompositeInput\(IReadOnlyList<IInputSource\>\)

Several input sources read as one: a key or button is down when any source has it down; the stick or axis with the largest deflection wins.

```csharp
public CompositeInput(IReadOnlyList<IInputSource> sources)
```

#### Parameters

`sources` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[IInputSource](MicroEngine.Input.IInputSource.md)\>

## Properties

### <a id="MicroEngine_Core_CompositeInput_LeftStick"></a> LeftStick

Left stick, each axis in -1..1.

```csharp
public Vector2 LeftStick { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Core_CompositeInput_PadConnected"></a> PadConnected

Whether a gamepad is connected. Sources without gamepads keep the default.

```csharp
public bool PadConnected { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_CompositeInput_PadStyle"></a> PadStyle

The style of the first source with a connected gamepad.

```csharp
public GamepadStyle PadStyle { get; }
```

#### Property Value

 [GamepadStyle](MicroEngine.Input.GamepadStyle.md)

### <a id="MicroEngine_Core_CompositeInput_Pointer"></a> Pointer

The pointer of the first source whose pointer is present.

```csharp
public PointerState Pointer { get; }
```

#### Property Value

 [PointerState](MicroEngine.Input.PointerState.md)

### <a id="MicroEngine_Core_CompositeInput_Sources"></a> Sources

The sources read, in order.

```csharp
public IReadOnlyList<IInputSource> Sources { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[IInputSource](MicroEngine.Input.IInputSource.md)\>

## Methods

### <a id="MicroEngine_Core_CompositeInput_GetPadAxis_MicroEngine_Input_PadAxis_"></a> GetPadAxis\(PadAxis\)

Any gamepad axis. Sources that only know the left stick keep the default.

```csharp
public float GetPadAxis(PadAxis axis)
```

#### Parameters

`axis` [PadAxis](MicroEngine.Input.PadAxis.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Core_CompositeInput_IsKeyDown_MicroEngine_Input_Key_"></a> IsKeyDown\(Key\)

Whether <code class="paramref">key</code> is held.

```csharp
public bool IsKeyDown(Key key)
```

#### Parameters

`key` [Key](MicroEngine.Input.Key.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_CompositeInput_IsPadDown_MicroEngine_Input_PadButton_"></a> IsPadDown\(PadButton\)

Whether <code class="paramref">button</code> is held on the gamepad.

```csharp
public bool IsPadDown(PadButton button)
```

#### Parameters

`button` [PadButton](MicroEngine.Input.PadButton.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
