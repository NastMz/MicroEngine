# Class Track

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine](MicroEngine.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

Creates the usual kinds of `Track`1`.

```csharp
public static class Track
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Track](MicroEngine.Track.md)

## Methods

### <a id="MicroEngine_Track_Color_MicroEngine_Keyframe_MicroEngine_Color____"></a> Color\(params Keyframe<Color\>\[\]\)

A track of colours, blended per channel; a curve that overshoots stops at the key's colour.

```csharp
public static Track<Color> Color(params Keyframe<Color>[] keys)
```

#### Parameters

`keys` [Keyframe](MicroEngine.Keyframe\-1.md)<[Color](MicroEngine.Color.md)\>\[\]

The keys, in order of time.

#### Returns

 [Track](MicroEngine.Track\-1.md)<[Color](MicroEngine.Color.md)\>

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

No keys, a time that is infinite or not a number, or times that do not increase.

### <a id="MicroEngine_Track_Float_MicroEngine_Keyframe_System_Single____"></a> Float\(params Keyframe<float\>\[\]\)

A track of numbers (a position on one axis, an angle, a scale, an opacity), blended linearly.

```csharp
public static Track<float> Float(params Keyframe<float>[] keys)
```

#### Parameters

`keys` [Keyframe](MicroEngine.Keyframe\-1.md)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>\[\]

The keys, in order of time.

#### Returns

 [Track](MicroEngine.Track\-1.md)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

No keys, a time that is infinite or not a number, or times that do not increase.

### <a id="MicroEngine_Track_Step__1_MicroEngine_Keyframe___0____"></a> Step<T\>\(params Keyframe<T\>\[\]\)

A track that holds each key's value until the next key, for values with nothing in between: a sprite, a name, a
flag. The keys' curves are not used.

```csharp
public static Track<T> Step<T>(params Keyframe<T>[] keys)
```

#### Parameters

`keys` [Keyframe](MicroEngine.Keyframe\-1.md)<T\>\[\]

The keys, in order of time.

#### Returns

 [Track](MicroEngine.Track\-1.md)<T\>

#### Type Parameters

`T` 

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

No keys, a time that is infinite or not a number, or times that do not increase.

### <a id="MicroEngine_Track_Vector2_MicroEngine_Keyframe_System_Numerics_Vector2____"></a> Vector2\(params Keyframe<Vector2\>\[\]\)

A track of points or sizes, blended linearly.

```csharp
public static Track<Vector2> Vector2(params Keyframe<Vector2>[] keys)
```

#### Parameters

`keys` [Keyframe](MicroEngine.Keyframe\-1.md)<[Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)\>\[\]

The keys, in order of time.

#### Returns

 [Track](MicroEngine.Track\-1.md)<[Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)\>

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

No keys, a time that is infinite or not a number, or times that do not increase.

</div>
