# Class Animator

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

Plays named clips; switching to the clip already playing does not restart it.

```csharp
public sealed class Animator
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Animator](MicroEngine.Graphics.Animator.md)

## Constructors

### <a id="MicroEngine_Graphics_Animator__ctor"></a> Animator\(\)

```csharp
public Animator()
```

## Properties

### <a id="MicroEngine_Graphics_Animator_Current"></a> Current

The clip playing, or null before the first clip is added.

```csharp
public AnimationClip? Current { get; }
```

#### Property Value

 [AnimationClip](MicroEngine.Graphics.AnimationClip.md)?

### <a id="MicroEngine_Graphics_Animator_CurrentName"></a> CurrentName

Name of the clip playing, or null before the first clip is added.

```csharp
public string? CurrentName { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_Animator_Finished"></a> Finished

Whether a non-looping clip has reached its end. Always false for looping clips.

```csharp
public bool Finished { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Animator_Frame"></a> Frame

The frame to draw now.

```csharp
public Sprite Frame { get; }
```

#### Property Value

 [Sprite](MicroEngine.Graphics.Sprite.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

No clip was added.

### <a id="MicroEngine_Graphics_Animator_Speed"></a> Speed

Playback speed multiplier (1 by default). Keep it at 0 or above.

```csharp
public float Speed { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_Animator_Time"></a> Time

Seconds played in the current clip, scaled by [Speed](MicroEngine.Graphics.Animator.md#MicroEngine_Graphics_Animator_Speed); reset to 0 when another clip starts.

```csharp
public float Time { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Graphics_Animator_Add_System_String_MicroEngine_Graphics_AnimationClip_"></a> Add\(string, AnimationClip\)

Registers <code class="paramref">clip</code> under <code class="paramref">name</code> (replacing a clip with that name). The first clip added starts playing.

```csharp
public Animator Add(string name, AnimationClip clip)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`clip` [AnimationClip](MicroEngine.Graphics.AnimationClip.md)

#### Returns

 [Animator](MicroEngine.Graphics.Animator.md)

This animator, for chaining.

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">clip</code> is null.

### <a id="MicroEngine_Graphics_Animator_Play_System_String_System_Boolean_"></a> Play\(string, bool\)

Switches to the clip <code class="paramref">name</code> from its first frame. Playing the current clip again does nothing unless <code class="paramref">restart</code> is true.

```csharp
public void Play(string name, bool restart = false)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`restart` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No clip has that name.

### <a id="MicroEngine_Graphics_Animator_Update_System_Single_"></a> Update\(float\)

Advances the current clip by <code class="paramref">dt</code> seconds times [Speed](MicroEngine.Graphics.Animator.md#MicroEngine_Graphics_Animator_Speed).

```csharp
public void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)
