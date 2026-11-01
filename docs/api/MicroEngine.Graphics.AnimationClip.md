# Class AnimationClip

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

An immutable sequence of sprites played at a fixed rate.

```csharp
public sealed class AnimationClip
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AnimationClip](MicroEngine.Graphics.AnimationClip.md)

## Constructors

### <a id="MicroEngine_Graphics_AnimationClip__ctor_System_Collections_Generic_IEnumerable_MicroEngine_Graphics_Sprite__System_Single_System_Boolean_"></a> AnimationClip\(IEnumerable<Sprite\>, float, bool\)

Creates a clip; the frames are copied.

```csharp
public AnimationClip(IEnumerable<Sprite> frames, float framesPerSecond, bool loop = true)
```

#### Parameters

`frames` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[Sprite](MicroEngine.Graphics.Sprite.md)\>

The frames, for example from [Frames](MicroEngine.Graphics.SpriteAtlas.md#MicroEngine_Graphics_SpriteAtlas_Frames_System_String_).

`framesPerSecond` [float](https://learn.microsoft.com/dotnet/api/system.single)

Playback rate; must be positive.

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the clip repeats.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">frames</code> is empty.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">framesPerSecond</code> is not positive.

## Properties

### <a id="MicroEngine_Graphics_AnimationClip_Duration"></a> Duration

Length of one pass through the frames, in seconds.

```csharp
public float Duration { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_AnimationClip_Frames"></a> Frames

The frames in playing order (at least one).

```csharp
public IReadOnlyList<Sprite> Frames { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Sprite](MicroEngine.Graphics.Sprite.md)\>

### <a id="MicroEngine_Graphics_AnimationClip_FramesPerSecond"></a> FramesPerSecond

Playback rate in frames per second.

```csharp
public float FramesPerSecond { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_AnimationClip_Loop"></a> Loop

Whether the clip starts over after its last frame; otherwise it holds the last frame.

```csharp
public bool Loop { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Graphics_AnimationClip_FrameAt_System_Single_"></a> FrameAt\(float\)

The frame shown <code class="paramref">time</code> seconds after the clip started (0 or more).

```csharp
public Sprite FrameAt(float time)
```

#### Parameters

`time` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [Sprite](MicroEngine.Graphics.Sprite.md)

</div>
