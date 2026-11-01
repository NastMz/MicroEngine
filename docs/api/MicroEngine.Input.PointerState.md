# Struct PointerState

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

The mouse (or another pointer) over the game's virtual canvas.

```csharp
public readonly struct PointerState : IEquatable<PointerState>
```

#### Implements

[IEquatable<PointerState\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Input_PointerState__ctor_System_Boolean_System_Numerics_Vector2_System_Boolean_System_Boolean_System_Boolean_"></a> PointerState\(bool, Vector2, bool, bool, bool\)

The mouse (or another pointer) over the game's virtual canvas.

```csharp
public PointerState(bool Present, Vector2 Position, bool Primary, bool Secondary, bool Middle)
```

#### Parameters

`Present` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the pointer is over the window at all.

`Position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Position in virtual canvas pixels (may lie outside the canvas, in the letterbox).

`Primary` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Secondary` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Middle` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Properties

### <a id="MicroEngine_Input_PointerState_Middle"></a> Middle

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

### <a id="MicroEngine_Input_PointerState_Primary"></a> Primary

```csharp
public bool Primary { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerState_Secondary"></a> Secondary

```csharp
public bool Secondary { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

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
