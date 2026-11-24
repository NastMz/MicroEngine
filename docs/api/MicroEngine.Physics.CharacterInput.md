# Struct CharacterInput

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics](../manual/physics.md)  

What a character wants to do in one step: the game fills it from input, an AI from its plan. The controller never
reads input devices.

```csharp
public readonly struct CharacterInput : IEquatable<CharacterInput>
```

#### Implements

[IEquatable<CharacterInput\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Physics_CharacterInput__ctor_System_Single_System_Boolean_System_Boolean_System_Boolean_"></a> CharacterInput\(float, bool, bool, bool\)

What a character wants to do in one step: the game fills it from input, an AI from its plan. The controller never
reads input devices.

```csharp
public CharacterInput(float Move, bool JumpPressed = false, bool JumpHeld = false, bool Drop = false)
```

#### Parameters

`Move` [float](https://learn.microsoft.com/dotnet/api/system.single)

The sideways intent, from -1 (left) to 1 (right); values beyond are clamped.

`JumpPressed` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Jump was pressed in this step.

`JumpHeld` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Jump is held: a rising jump keeps its full height while it is.

`Drop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Fall through the one-way platform the character stands on (it wins over a jump there, and does nothing on solid ground).

## Properties

### <a id="MicroEngine_Physics_CharacterInput_Drop"></a> Drop

Fall through the one-way platform the character stands on (it wins over a jump there, and does nothing on solid ground).

```csharp
public bool Drop { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterInput_JumpHeld"></a> JumpHeld

Jump is held: a rising jump keeps its full height while it is.

```csharp
public bool JumpHeld { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterInput_JumpPressed"></a> JumpPressed

Jump was pressed in this step.

```csharp
public bool JumpPressed { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterInput_Move"></a> Move

The sideways intent, from -1 (left) to 1 (right); values beyond are clamped.

```csharp
public float Move { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Physics_CharacterInput_Deconstruct_System_Single__System_Boolean__System_Boolean__System_Boolean__"></a> Deconstruct\(out float, out bool, out bool, out bool\)

```csharp
public void Deconstruct(out float Move, out bool JumpPressed, out bool JumpHeld, out bool Drop)
```

#### Parameters

`Move` [float](https://learn.microsoft.com/dotnet/api/system.single)

`JumpPressed` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`JumpHeld` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Drop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterInput_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterInput_Equals_MicroEngine_Physics_CharacterInput_"></a> Equals\(CharacterInput\)

```csharp
public bool Equals(CharacterInput other)
```

#### Parameters

`other` [CharacterInput](MicroEngine.Physics.CharacterInput.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterInput_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_CharacterInput_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Physics_CharacterInput_op_Equality_MicroEngine_Physics_CharacterInput_MicroEngine_Physics_CharacterInput_"></a> operator ==\(CharacterInput, CharacterInput\)

```csharp
public static bool operator ==(CharacterInput left, CharacterInput right)
```

#### Parameters

`left` [CharacterInput](MicroEngine.Physics.CharacterInput.md)

`right` [CharacterInput](MicroEngine.Physics.CharacterInput.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_CharacterInput_op_Inequality_MicroEngine_Physics_CharacterInput_MicroEngine_Physics_CharacterInput_"></a> operator \!=\(CharacterInput, CharacterInput\)

```csharp
public static bool operator !=(CharacterInput left, CharacterInput right)
```

#### Parameters

`left` [CharacterInput](MicroEngine.Physics.CharacterInput.md)

`right` [CharacterInput](MicroEngine.Physics.CharacterInput.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
