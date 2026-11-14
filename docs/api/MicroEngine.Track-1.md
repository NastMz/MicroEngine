# Class Track<T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine](MicroEngine.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

An immutable value that changes over time through keys: `At` gives the value at any time, blended
between the two keys around it. It holds no playhead, so one track can be shared by everything that moves the same
way; a <code>Timeline</code> plays tracks, or call `At` with a time of your own.

```csharp
public sealed class Track<T>
```

#### Type Parameters

`T` 

The type of the animated value.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Track<T\>](MicroEngine.Track\-1.md)

## Constructors

### <a id="MicroEngine_Track_1__ctor_System_Func__0__0_System_Single__0__MicroEngine_Keyframe__0____"></a> Track\(Func<T, T, float, T\>, params Keyframe<T\>\[\]\)

Creates a track from its keys (copied) and the function that blends two values.

```csharp
public Track(Func<T, T, float, T> blend, params Keyframe<T>[] keys)
```

#### Parameters

`blend` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<T, T, [float](https://learn.microsoft.com/dotnet/api/system.single), T\>

Returns the value between its first argument (at 0) and its second (at 1), for example [Lerp](MicroEngine.MathX.md#MicroEngine_MathX_Lerp_System_Single_System_Single_System_Single_).
It receives the eased progress, which is below 0 or above 1 where a curve overshoots.

`keys` [Keyframe](MicroEngine.Keyframe\-1.md)<T\>\[\]

The keys, in order of time, each later than the one before: keys are not sorted, and two keys cannot share a time.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

No keys, a time that is infinite or not a number, or times that do not increase.

## Properties

### <a id="MicroEngine_Track_1_Duration"></a> Duration

The time of the last key, in seconds.

```csharp
public float Duration { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Track_1_Keys"></a> Keys

The keys, in order of time.

```csharp
public IReadOnlyList<Keyframe<T>> Keys { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Keyframe](MicroEngine.Keyframe\-1.md)<T\>\>

## Methods

### <a id="MicroEngine_Track_1_At_System_Single_"></a> At\(float\)

The value at <code class="paramref">time</code> seconds: the first key's value before it, the last key's value after it, a
key's own value at its time (whatever its curve), and between two keys their blend along the later key's
curve. A time that is not a number gives the first key's value.

```csharp
public T At(float time)
```

#### Parameters

`time` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 T

</div>
