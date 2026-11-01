# Class Transition

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

A full-screen effect that covers the old scene (progress 0..0.5), swaps scenes at 0.5 and
reveals the new one (0.5..1). Subclasses draw a cover amount between 0 (clear) and 1 (covered).

```csharp
public abstract class Transition
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Transition](MicroEngine.Scenes.Transition.md)

## Constructors

### <a id="MicroEngine_Scenes_Transition__ctor_System_Single_"></a> Transition\(float\)

A full-screen effect that covers the old scene (progress 0..0.5), swaps scenes at 0.5 and
reveals the new one (0.5..1). Subclasses draw a cover amount between 0 (clear) and 1 (covered).

```csharp
protected Transition(float duration)
```

#### Parameters

`duration` [float](https://learn.microsoft.com/dotnet/api/system.single)

## Properties

### <a id="MicroEngine_Scenes_Transition_Cover"></a> Cover

0 = nothing covered, 1 = fully covered.

```csharp
public float Cover { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_Transition_Duration"></a> Duration

Length of the transition in seconds (at least 0.02).

```csharp
public float Duration { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_Transition_Elapsed"></a> Elapsed

Seconds played so far.

```csharp
public float Elapsed { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_Transition_Finished"></a> Finished

Whether the transition has played to the end.

```csharp
public bool Finished { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_Transition_Progress"></a> Progress

Elapsed time as a fraction of [Duration](MicroEngine.Scenes.Transition.md#MicroEngine_Scenes_Transition_Duration), from 0 to 1. The scenes swap at 0.5.

```csharp
public float Progress { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Scenes_Transition_Diamonds_System_Single_System_Nullable_MicroEngine_Color__"></a> Diamonds\(float, Color?\)

Growing diamonds sweep diagonally across the screen and shrink away.

```csharp
public static Transition Diamonds(float duration = 0.7, Color? color = null)
```

#### Parameters

`duration` [float](https://learn.microsoft.com/dotnet/api/system.single)

Total length in seconds.

`color` [Color](MicroEngine.Color.md)?

Cover colour; null for black.

#### Returns

 [Transition](MicroEngine.Scenes.Transition.md)

### <a id="MicroEngine_Scenes_Transition_Draw_MicroEngine_Graphics_IRenderer_"></a> Draw\(IRenderer\)

Draws the effect over the scenes, using [Cover](MicroEngine.Scenes.Transition.md#MicroEngine_Scenes_Transition_Cover).

```csharp
public abstract void Draw(IRenderer renderer)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

### <a id="MicroEngine_Scenes_Transition_Fade_System_Single_System_Nullable_MicroEngine_Color__"></a> Fade\(float, Color?\)

Fades to <code class="paramref">color</code> (black by default) and back.

```csharp
public static Transition Fade(float duration = 0.5, Color? color = null)
```

#### Parameters

`duration` [float](https://learn.microsoft.com/dotnet/api/system.single)

Total length in seconds.

`color` [Color](MicroEngine.Color.md)?

Cover colour; null for black.

#### Returns

 [Transition](MicroEngine.Scenes.Transition.md)

### <a id="MicroEngine_Scenes_Transition_Iris_System_Numerics_Vector2_System_Single_System_Nullable_MicroEngine_Color__"></a> Iris\(Vector2, float, Color?\)

A circle closes on <code class="paramref">center</code> (virtual pixels) and opens again.

```csharp
public static Transition Iris(Vector2 center, float duration = 0.8, Color? color = null)
```

#### Parameters

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Centre of the circle in virtual pixels.

`duration` [float](https://learn.microsoft.com/dotnet/api/system.single)

Total length in seconds.

`color` [Color](MicroEngine.Color.md)?

Cover colour; null for black.

#### Returns

 [Transition](MicroEngine.Scenes.Transition.md)

### <a id="MicroEngine_Scenes_Transition_Wipe_System_Single_System_Nullable_MicroEngine_Color__"></a> Wipe\(float, Color?\)

A panel slides in from the left, then leaves to the right.

```csharp
public static Transition Wipe(float duration = 0.6, Color? color = null)
```

#### Parameters

`duration` [float](https://learn.microsoft.com/dotnet/api/system.single)

Total length in seconds.

`color` [Color](MicroEngine.Color.md)?

Cover colour; null for black.

#### Returns

 [Transition](MicroEngine.Scenes.Transition.md)
