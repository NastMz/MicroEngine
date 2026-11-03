# Class SeededRandom

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule)  

A `Random` that draws exactly what <code>new Random(seed)</code> draws, and whose state can take part in the
per-step state hash ([IStateHashed](MicroEngine.Kernel.IStateHashed.md)): its seed, how many values it drew and a running hash of them. Two
runs that drew the same values from the same seed hash the same; one extra or different draw changes the hash.

```csharp
public sealed class SeededRandom : Random, IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Random](https://learn.microsoft.com/dotnet/api/system.random) ← 
[SeededRandom](MicroEngine.Kernel.SeededRandom.md)

#### Implements

[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

#### Inherited Members

[Random.Next\(\)](https://learn.microsoft.com/dotnet/api/system.random.next\#system\-random\-next), 
[Random.Next\(int\)](https://learn.microsoft.com/dotnet/api/system.random.next\#system\-random\-next\(system\-int32\)), 
[Random.Next\(int, int\)](https://learn.microsoft.com/dotnet/api/system.random.next\#system\-random\-next\(system\-int32\-system\-int32\)), 
[Random.NextInt64\(\)](https://learn.microsoft.com/dotnet/api/system.random.nextint64\#system\-random\-nextint64), 
[Random.NextInt64\(long\)](https://learn.microsoft.com/dotnet/api/system.random.nextint64\#system\-random\-nextint64\(system\-int64\)), 
[Random.NextInt64\(long, long\)](https://learn.microsoft.com/dotnet/api/system.random.nextint64\#system\-random\-nextint64\(system\-int64\-system\-int64\)), 
[Random.NextSingle\(\)](https://learn.microsoft.com/dotnet/api/system.random.nextsingle), 
[Random.NextDouble\(\)](https://learn.microsoft.com/dotnet/api/system.random.nextdouble), 
[Random.NextBytes\(byte\[\]\)](https://learn.microsoft.com/dotnet/api/system.random.nextbytes\#system\-random\-nextbytes\(system\-byte\(\)\)), 
[Random.NextBytes\(Span<byte\>\)](https://learn.microsoft.com/dotnet/api/system.random.nextbytes\#system\-random\-nextbytes\(system\-span\(\(system\-byte\)\)\)), 
[Random.GetItems<T\>\(ReadOnlySpan<T\>, Span<T\>\)](https://learn.microsoft.com/dotnet/api/system.random.getitems\#system\-random\-getitems\-1\(system\-readonlyspan\(\(\-0\)\)\-system\-span\(\(\-0\)\)\)), 
[Random.GetItems<T\>\(T\[\], int\)](https://learn.microsoft.com/dotnet/api/system.random.getitems\#system\-random\-getitems\-1\(\-0\(\)\-system\-int32\)), 
[Random.GetItems<T\>\(ReadOnlySpan<T\>, int\)](https://learn.microsoft.com/dotnet/api/system.random.getitems\#system\-random\-getitems\-1\(system\-readonlyspan\(\(\-0\)\)\-system\-int32\)), 
[Random.Shuffle<T\>\(T\[\]\)](https://learn.microsoft.com/dotnet/api/system.random.shuffle\#system\-random\-shuffle\-1\(\-0\(\)\)), 
[Random.Shuffle<T\>\(Span<T\>\)](https://learn.microsoft.com/dotnet/api/system.random.shuffle\#system\-random\-shuffle\-1\(system\-span\(\(\-0\)\)\)), 
[Random.GetString\(ReadOnlySpan<char\>, int\)](https://learn.microsoft.com/dotnet/api/system.random.getstring), 
[Random.GetHexString\(int, bool\)](https://learn.microsoft.com/dotnet/api/system.random.gethexstring\#system\-random\-gethexstring\(system\-int32\-system\-boolean\)), 
[Random.GetHexString\(Span<char\>, bool\)](https://learn.microsoft.com/dotnet/api/system.random.gethexstring\#system\-random\-gethexstring\(system\-span\(\(system\-char\)\)\-system\-boolean\)), 
[Random.Shared](https://learn.microsoft.com/dotnet/api/system.random.shared), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Remarks

The game's own generator (<code>Game.Random</code>) is one. Use one wherever simulation draws from a generator of its own
(<code>new SeededRandom(seed)</code> instead of <code>new Random(seed)</code>) and add it from the owner's <code>HashState</code>.

## Constructors

### <a id="MicroEngine_Kernel_SeededRandom__ctor_System_Int32_"></a> SeededRandom\(int\)

Creates a generator that draws the sequence of <code>new Random(<code class="paramref">seed</code>)</code>.

```csharp
public SeededRandom(int seed)
```

#### Parameters

`seed` [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Properties

### <a id="MicroEngine_Kernel_SeededRandom_Draws"></a> Draws

How many values (or byte buffers) were drawn so far.

```csharp
public long Draws { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Kernel_SeededRandom_Seed"></a> Seed

The seed the generator started from.

```csharp
public int Seed { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Kernel_SeededRandom_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the seed, the number of draws and the running hash of the values drawn.

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Kernel_SeededRandom_Next"></a> Next\(\)

```csharp
public override int Next()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Kernel_SeededRandom_Next_System_Int32_"></a> Next\(int\)

```csharp
public override int Next(int maxValue)
```

#### Parameters

`maxValue` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Kernel_SeededRandom_Next_System_Int32_System_Int32_"></a> Next\(int, int\)

```csharp
public override int Next(int minValue, int maxValue)
```

#### Parameters

`minValue` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`maxValue` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Kernel_SeededRandom_NextBytes_System_Byte___"></a> NextBytes\(byte\[\]\)

```csharp
public override void NextBytes(byte[] buffer)
```

#### Parameters

`buffer` [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

### <a id="MicroEngine_Kernel_SeededRandom_NextBytes_System_Span_System_Byte__"></a> NextBytes\(Span<byte\>\)

```csharp
public override void NextBytes(Span<byte> buffer)
```

#### Parameters

`buffer` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

### <a id="MicroEngine_Kernel_SeededRandom_NextDouble"></a> NextDouble\(\)

```csharp
public override double NextDouble()
```

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Kernel_SeededRandom_NextInt64"></a> NextInt64\(\)

```csharp
public override long NextInt64()
```

#### Returns

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Kernel_SeededRandom_NextInt64_System_Int64_"></a> NextInt64\(long\)

```csharp
public override long NextInt64(long maxValue)
```

#### Parameters

`maxValue` [long](https://learn.microsoft.com/dotnet/api/system.int64)

#### Returns

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Kernel_SeededRandom_NextInt64_System_Int64_System_Int64_"></a> NextInt64\(long, long\)

```csharp
public override long NextInt64(long minValue, long maxValue)
```

#### Parameters

`minValue` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`maxValue` [long](https://learn.microsoft.com/dotnet/api/system.int64)

#### Returns

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Kernel_SeededRandom_NextSingle"></a> NextSingle\(\)

```csharp
public override float NextSingle()
```

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Kernel_SeededRandom_Sample"></a> Sample\(\)

```csharp
protected override double Sample()
```

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

</div>
