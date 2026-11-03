# Class KeyChord

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

A key pressed while a modifier is held: <code>new KeyChord(Key.Enter, Key.LeftAlt, Key.RightAlt)</code> is Alt+Enter with
either Alt key. The chord is down while [Key](MicroEngine.Input.KeyChord.md#MicroEngine_Input_KeyChord_Key) and any one of [Modifiers](MicroEngine.Input.KeyChord.md#MicroEngine_Input_KeyChord_Modifiers) are down.

```csharp
public sealed record KeyChord : IEquatable<KeyChord>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[KeyChord](MicroEngine.Input.KeyChord.md)

#### Implements

[IEquatable<KeyChord\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Input_KeyChord__ctor_MicroEngine_Input_Key_MicroEngine_Input_Key___"></a> KeyChord\(Key, params Key\[\]\)

A chord of <code class="paramref">key</code> with any one of <code class="paramref">modifiers</code> (at least one).

```csharp
public KeyChord(Key key, params Key[] modifiers)
```

#### Parameters

`key` [Key](MicroEngine.Input.Key.md)

`modifiers` [Key](MicroEngine.Input.Key.md)\[\]

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

No modifier is given.

## Properties

### <a id="MicroEngine_Input_KeyChord_Key"></a> Key

The key that is pressed.

```csharp
public Key Key { get; }
```

#### Property Value

 [Key](MicroEngine.Input.Key.md)

### <a id="MicroEngine_Input_KeyChord_Modifiers"></a> Modifiers

The keys of which one must be held (both Alt keys, both Control keys...).

```csharp
public IReadOnlyList<Key> Modifiers { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Key](MicroEngine.Input.Key.md)\>

## Methods

### <a id="MicroEngine_Input_KeyChord__Clone__"></a> <Clone\>$\(\)

```csharp
public KeyChord <Clone>$()
```

#### Returns

 [KeyChord](MicroEngine.Input.KeyChord.md)

### <a id="MicroEngine_Input_KeyChord_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_KeyChord_Equals_MicroEngine_Input_KeyChord_"></a> Equals\(KeyChord?\)

```csharp
public bool Equals(KeyChord? other)
```

#### Parameters

`other` [KeyChord](MicroEngine.Input.KeyChord.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_KeyChord_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_KeyChord_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Input_KeyChord_op_Equality_MicroEngine_Input_KeyChord_MicroEngine_Input_KeyChord_"></a> operator ==\(KeyChord?, KeyChord?\)

```csharp
public static bool operator ==(KeyChord? left, KeyChord? right)
```

#### Parameters

`left` [KeyChord](MicroEngine.Input.KeyChord.md)?

`right` [KeyChord](MicroEngine.Input.KeyChord.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_KeyChord_op_Inequality_MicroEngine_Input_KeyChord_MicroEngine_Input_KeyChord_"></a> operator \!=\(KeyChord?, KeyChord?\)

```csharp
public static bool operator !=(KeyChord? left, KeyChord? right)
```

#### Parameters

`left` [KeyChord](MicroEngine.Input.KeyChord.md)?

`right` [KeyChord](MicroEngine.Input.KeyChord.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
