# Struct Keyframe<T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine](MicroEngine.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

One key of a `Track`1`: the value the track has at a time.

```csharp
public readonly struct Keyframe<T> : IEquatable<Keyframe<T>>
```

#### Type Parameters

`T` 

The type of the animated value.

#### Implements

[IEquatable<Keyframe<T\>\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Keyframe_1__ctor_System_Single__0_System_Func_System_Single_System_Single__"></a> Keyframe\(float, T, Func<float, float\>?\)

One key of a `Track`1`: the value the track has at a time.

```csharp
public Keyframe(float Time, T Value, Func<float, float>? Ease = null)
```

#### Parameters

`Time` [float](https://learn.microsoft.com/dotnet/api/system.single)

When the track reaches <code class="paramref">Value</code>, in seconds.

`Value` T

The value at <code class="paramref">Time</code>.

`Ease` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[float](https://learn.microsoft.com/dotnet/api/system.single), [float](https://learn.microsoft.com/dotnet/api/system.single)\>?

The curve of the way from the previous key to this one (an [Ease](MicroEngine.Ease.md) function or your own);
null is linear. The first key's curve is not used.

## Properties

### <a id="MicroEngine_Keyframe_1_Ease"></a> Ease

The curve of the way from the previous key to this one (an [Ease](MicroEngine.Ease.md) function or your own);
null is linear. The first key's curve is not used.

```csharp
public Func<float, float>? Ease { get; init; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[float](https://learn.microsoft.com/dotnet/api/system.single), [float](https://learn.microsoft.com/dotnet/api/system.single)\>?

### <a id="MicroEngine_Keyframe_1_Time"></a> Time

When the track reaches <code class="paramref">Value</code>, in seconds.

```csharp
public float Time { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Keyframe_1_Value"></a> Value

The value at <code class="paramref">Time</code>.

```csharp
public T Value { get; init; }
```

#### Property Value

 T

## Methods

### <a id="MicroEngine_Keyframe_1_Deconstruct_System_Single___0__System_Func_System_Single_System_Single___"></a> Deconstruct\(out float, out T, out Func<float, float\>?\)

```csharp
public void Deconstruct(out float Time, out T Value, out Func<float, float>? Ease)
```

#### Parameters

`Time` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Value` T

`Ease` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[float](https://learn.microsoft.com/dotnet/api/system.single), [float](https://learn.microsoft.com/dotnet/api/system.single)\>?

### <a id="MicroEngine_Keyframe_1_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Keyframe_1_Equals_MicroEngine_Keyframe__0__"></a> Equals\(Keyframe<T\>\)

```csharp
public bool Equals(Keyframe<T> other)
```

#### Parameters

`other` [Keyframe](MicroEngine.Keyframe\-1.md)<T\>

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Keyframe_1_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Keyframe_1_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Keyframe_1_op_Equality_MicroEngine_Keyframe__0__MicroEngine_Keyframe__0__"></a> operator ==\(Keyframe<T\>, Keyframe<T\>\)

```csharp
public static bool operator ==(Keyframe<T> left, Keyframe<T> right)
```

#### Parameters

`left` [Keyframe](MicroEngine.Keyframe\-1.md)<T\>

`right` [Keyframe](MicroEngine.Keyframe\-1.md)<T\>

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Keyframe_1_op_Inequality_MicroEngine_Keyframe__0__MicroEngine_Keyframe__0__"></a> operator \!=\(Keyframe<T\>, Keyframe<T\>\)

```csharp
public static bool operator !=(Keyframe<T> left, Keyframe<T> right)
```

#### Parameters

`left` [Keyframe](MicroEngine.Keyframe\-1.md)<T\>

`right` [Keyframe](MicroEngine.Keyframe\-1.md)<T\>

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
