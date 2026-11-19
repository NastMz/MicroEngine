# Struct PointerState

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The mouse (or another pointer) over the game's virtual canvas: where it is, which buttons are held, how far it and
the wheel moved in this update, and which buttons went down or up in it.

```csharp
public readonly struct PointerState : IEquatable<PointerState>
```

#### Implements

[IEquatable<PointerState\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Input_PointerState__ctor_System_Boolean_System_Numerics_Vector2_System_Boolean_System_Boolean_System_Boolean_"></a> PointerState\(bool, Vector2, bool, bool, bool\)

The mouse (or another pointer) over the game's virtual canvas: where it is, which buttons are held, how far it and
the wheel moved in this update, and which buttons went down or up in it.

```csharp
public PointerState(bool Present, Vector2 Position, bool Primary, bool Secondary, bool Middle)
```

#### Parameters

`Present` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the pointer is over the window at all.

`Position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Position in virtual canvas pixels (may lie outside the canvas, in the letterbox).

`Primary` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the primary button is held (or a finger or pen touches).

`Secondary` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the secondary button is held.

`Middle` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the middle button is held.

## Properties

### <a id="MicroEngine_Input_PointerState_Back"></a> Back

Whether the back button on the side of the mouse is held.

```csharp
public bool Back { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerState_Buttons"></a> Buttons

Every button held, as one value.

```csharp
public PointerButtons Buttons { get; }
```

#### Property Value

 [PointerButtons](MicroEngine.Input.PointerButtons.md)

### <a id="MicroEngine_Input_PointerState_Delta"></a> Delta

How far the pointer moved since the previous update, in virtual canvas pixels. While the cursor is locked
([Relative](MicroEngine.Input.ICursorControl.md#MicroEngine_Input_ICursorControl_Relative)) the position stays and this is the mouse's own motion.

```csharp
public Vector2 Delta { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Input_PointerState_Forward"></a> Forward

Whether the forward button on the side of the mouse is held.

```csharp
public bool Forward { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerState_Kind"></a> Kind

Whether this is the mouse, a finger or a pen.

```csharp
public PointerKind Kind { get; init; }
```

#### Property Value

 [PointerKind](MicroEngine.Input.PointerKind.md)

### <a id="MicroEngine_Input_PointerState_Middle"></a> Middle

Whether the middle button is held.

```csharp
public bool Middle { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerState_Position"></a> Position

Position in virtual canvas pixels (may lie outside the canvas, in the letterbox).

```csharp
public Vector2 Position { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Input_PointerState_Present"></a> Present

Whether the pointer is over the window at all.

```csharp
public bool Present { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerState_PressedButtons"></a> PressedButtons

The buttons that went down in this update. A map computes it once per update ([Pointer](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Pointer)); a
raw source leaves it empty.

```csharp
public PointerButtons PressedButtons { get; init; }
```

#### Property Value

 [PointerButtons](MicroEngine.Input.PointerButtons.md)

### <a id="MicroEngine_Input_PointerState_Primary"></a> Primary

Whether the primary button is held (or a finger or pen touches).

```csharp
public bool Primary { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerState_ReleasedButtons"></a> ReleasedButtons

The buttons that went up in this update (see [PressedButtons](MicroEngine.Input.PointerState.md#MicroEngine_Input_PointerState_PressedButtons)).

```csharp
public PointerButtons ReleasedButtons { get; init; }
```

#### Property Value

 [PointerButtons](MicroEngine.Input.PointerButtons.md)

### <a id="MicroEngine_Input_PointerState_Secondary"></a> Secondary

Whether the secondary button is held.

```csharp
public bool Secondary { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerState_Wheel"></a> Wheel

How far the wheel turned since the previous update: Y positive away from the player (scroll up), X positive to
the right, in notches (fractions for smooth wheels and touchpads).

```csharp
public Vector2 Wheel { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Methods

### <a id="MicroEngine_Input_PointerState_Deconstruct_System_Boolean__System_Numerics_Vector2__System_Boolean__System_Boolean__System_Boolean__"></a> Deconstruct\(out bool, out Vector2, out bool, out bool, out bool\)

```csharp
public void Deconstruct(out bool Present, out Vector2 Position, out bool Primary, out bool Secondary, out bool Middle)
```

#### Parameters

`Present` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Primary` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Secondary` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Middle` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerState_Down_MicroEngine_Input_PointerButton_"></a> Down\(PointerButton\)

Whether <code class="paramref">button</code> is held. Also true in the update after a click that started and ended within one frame.

```csharp
public bool Down(PointerButton button)
```

#### Parameters

`button` [PointerButton](MicroEngine.Input.PointerButton.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerState_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerState_Equals_MicroEngine_Input_PointerState_"></a> Equals\(PointerState\)

```csharp
public bool Equals(PointerState other)
```

#### Parameters

`other` [PointerState](MicroEngine.Input.PointerState.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerState_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_PointerState_Pressed_MicroEngine_Input_PointerButton_"></a> Pressed\(PointerButton\)

True during the first update after <code class="paramref">button</code> went down (as [Pointer](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Pointer) reports it).

```csharp
public bool Pressed(PointerButton button)
```

#### Parameters

`button` [PointerButton](MicroEngine.Input.PointerButton.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerState_Released_MicroEngine_Input_PointerButton_"></a> Released\(PointerButton\)

True during the first update after <code class="paramref">button</code> went up (as [Pointer](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Pointer) reports it).

```csharp
public bool Released(PointerButton button)
```

#### Parameters

`button` [PointerButton](MicroEngine.Input.PointerButton.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerState_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Input_PointerState_op_Equality_MicroEngine_Input_PointerState_MicroEngine_Input_PointerState_"></a> operator ==\(PointerState, PointerState\)

```csharp
public static bool operator ==(PointerState left, PointerState right)
```

#### Parameters

`left` [PointerState](MicroEngine.Input.PointerState.md)

`right` [PointerState](MicroEngine.Input.PointerState.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerState_op_Inequality_MicroEngine_Input_PointerState_MicroEngine_Input_PointerState_"></a> operator \!=\(PointerState, PointerState\)

```csharp
public static bool operator !=(PointerState left, PointerState right)
```

#### Parameters

`left` [PointerState](MicroEngine.Input.PointerState.md)

`right` [PointerState](MicroEngine.Input.PointerState.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
