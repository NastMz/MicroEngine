# Struct LoopRegion

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

The part of a sound that repeats when it loops, in frames of the sound itself (not of the output):
[Start](MicroEngine.Audio.LoopRegion.md#MicroEngine_Audio_LoopRegion_Start) is the first frame of the loop and [End](MicroEngine.Audio.LoopRegion.md#MicroEngine_Audio_LoopRegion_End) the frame after its last. What comes before
[Start](MicroEngine.Audio.LoopRegion.md#MicroEngine_Audio_LoopRegion_Start) plays once.

```csharp
public readonly struct LoopRegion : IEquatable<LoopRegion>
```

#### Implements

[IEquatable<LoopRegion\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Audio_LoopRegion__ctor_System_Int64_System_Int64_"></a> LoopRegion\(long, long\)

The part of a sound that repeats when it loops, in frames of the sound itself (not of the output):
[Start](MicroEngine.Audio.LoopRegion.md#MicroEngine_Audio_LoopRegion_Start) is the first frame of the loop and [End](MicroEngine.Audio.LoopRegion.md#MicroEngine_Audio_LoopRegion_End) the frame after its last. What comes before
[Start](MicroEngine.Audio.LoopRegion.md#MicroEngine_Audio_LoopRegion_Start) plays once.

```csharp
public LoopRegion(long Start, long End = 0)
```

#### Parameters

`Start` [long](https://learn.microsoft.com/dotnet/api/system.int64)

The first frame of the loop; negative counts as 0.

`End` [long](https://learn.microsoft.com/dotnet/api/system.int64)

The frame after the loop's last one; 0, or anything not after <code class="paramref">Start</code> or past the sound's end, means the sound's end.

## Properties

### <a id="MicroEngine_Audio_LoopRegion_End"></a> End

The frame after the loop's last one; 0, or anything not after <code class="paramref">Start</code> or past the sound's end, means the sound's end.

```csharp
public long End { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Audio_LoopRegion_Start"></a> Start

The first frame of the loop; negative counts as 0.

```csharp
public long Start { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

## Methods

### <a id="MicroEngine_Audio_LoopRegion_Deconstruct_System_Int64__System_Int64__"></a> Deconstruct\(out long, out long\)

```csharp
public void Deconstruct(out long Start, out long End)
```

#### Parameters

`Start` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`End` [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Audio_LoopRegion_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_LoopRegion_Equals_MicroEngine_Audio_LoopRegion_"></a> Equals\(LoopRegion\)

```csharp
public bool Equals(LoopRegion other)
```

#### Parameters

`other` [LoopRegion](MicroEngine.Audio.LoopRegion.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_LoopRegion_FromSeconds_System_Double_System_Double_System_Int32_"></a> FromSeconds\(double, double, int\)

A region given in seconds of a sound sampled at <code class="paramref">sampleRate</code>; an end of 0 or less means the sound's end. Values that are not numbers count as 0.

```csharp
public static LoopRegion FromSeconds(double start, double end, int sampleRate)
```

#### Parameters

`start` [double](https://learn.microsoft.com/dotnet/api/system.double)

`end` [double](https://learn.microsoft.com/dotnet/api/system.double)

`sampleRate` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [LoopRegion](MicroEngine.Audio.LoopRegion.md)

### <a id="MicroEngine_Audio_LoopRegion_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_LoopRegion_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Audio_LoopRegion_op_Equality_MicroEngine_Audio_LoopRegion_MicroEngine_Audio_LoopRegion_"></a> operator ==\(LoopRegion, LoopRegion\)

```csharp
public static bool operator ==(LoopRegion left, LoopRegion right)
```

#### Parameters

`left` [LoopRegion](MicroEngine.Audio.LoopRegion.md)

`right` [LoopRegion](MicroEngine.Audio.LoopRegion.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_LoopRegion_op_Inequality_MicroEngine_Audio_LoopRegion_MicroEngine_Audio_LoopRegion_"></a> operator \!=\(LoopRegion, LoopRegion\)

```csharp
public static bool operator !=(LoopRegion left, LoopRegion right)
```

#### Parameters

`left` [LoopRegion](MicroEngine.Audio.LoopRegion.md)

`right` [LoopRegion](MicroEngine.Audio.LoopRegion.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
