# Class SpriteEffects

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

One-shot sprite animations: "play this clip once here, after a delay, mirrored, tinted, then go away". Impacts,
puffs of smoke, sparkles and explosions are each one call, with no object to keep and nothing to remove. A scene
has one in <code>Scene.Effects</code>, stepped and cleared with the scene; a game whose rules live outside a scene makes
its own and calls [Update](MicroEngine.Graphics.SpriteEffects.md#MicroEngine_Graphics_SpriteEffects_Update_System_Single_).

```csharp
public sealed class SpriteEffects : IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SpriteEffects](MicroEngine.Graphics.SpriteEffects.md)

#### Implements

[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

## Examples

```csharp
// when the shot lands
Effects.Play(atlas.Clip("explosion"), hit.Position);
Effects.Play(atlas.Clip("spark"), hit.Position + new Vector2(6, -4), delay: 0.08f, flipX: true, tint: Color.Yellow);

// in Draw
Effects.Draw(renderer);
```

## Remarks

<p>
An effect shows nothing during its delay, then the clip's frames at the clip's own timing (or at the rate given),
once: a looping clip plays one pass. It ends on the update its time reaches the clip's length. Effects draw in the
order they were played; into a [SortedRenderer](MicroEngine.Graphics.SortedRenderer.md) each draws at its own layer and depth.
</p>
<p>
Playing, stepping and drawing allocate nothing once the pool is as large as the most effects alive at once (it
grows by itself).
</p>

## Constructors

### <a id="MicroEngine_Graphics_SpriteEffects__ctor_System_Int32_"></a> SpriteEffects\(int\)

Creates an empty player.

```csharp
public SpriteEffects(int capacity = 32)
```

#### Parameters

`capacity` [int](https://learn.microsoft.com/dotnet/api/system.int32)

How many effects it holds before it grows.

## Properties

### <a id="MicroEngine_Graphics_SpriteEffects_Busy"></a> Busy

Whether any effect is waiting or playing: a turn-based rule waits for it before going on.

```csharp
public bool Busy { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SpriteEffects_Count"></a> Count

How many effects are waiting or playing.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_SpriteEffects_SnapToPixels"></a> SnapToPixels

Whether effects are drawn on whole pixels (their position rounded), as pixel art on a low-resolution canvas wants.
False by default: an effect is drawn where it was played.

```csharp
public bool SnapToPixels { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Graphics_SpriteEffects_Clear"></a> Clear\(\)

Removes every effect.

```csharp
public void Clear()
```

### <a id="MicroEngine_Graphics_SpriteEffects_Draw_MicroEngine_Graphics_IRenderer_"></a> Draw\(IRenderer\)

Draws the effects that are showing, in the order they were played. Into a [SortedRenderer](MicroEngine.Graphics.SortedRenderer.md) block
each is drawn at its own layer and depth, and the block's layer and depth are left as they were.

```csharp
public void Draw(IRenderer renderer)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

### <a id="MicroEngine_Graphics_SpriteEffects_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the effects to a state hash: how many there are and, for each, its time, its delay and its length. A rule
that waits for an effect ([Busy](MicroEngine.Graphics.SpriteEffects.md#MicroEngine_Graphics_SpriteEffects_Busy), [IsPlaying](MicroEngine.Graphics.SpriteEffects.md#MicroEngine_Graphics_SpriteEffects_IsPlaying_MicroEngine_Graphics_SpriteEffect_)) makes them part of what happens next.

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Graphics_SpriteEffects_IsPlaying_MicroEngine_Graphics_SpriteEffect_"></a> IsPlaying\(SpriteEffect\)

Whether <code class="paramref">effect</code> is still waiting or playing.

```csharp
public bool IsPlaying(SpriteEffect effect)
```

#### Parameters

`effect` [SpriteEffect](MicroEngine.Graphics.SpriteEffect.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SpriteEffects_Move_MicroEngine_Graphics_SpriteEffect_System_Numerics_Vector2_"></a> Move\(SpriteEffect, Vector2\)

Moves <code class="paramref">effect</code> to <code class="paramref">at</code>: an effect that follows something is moved to it each update.

```csharp
public bool Move(SpriteEffect effect, Vector2 at)
```

#### Parameters

`effect` [SpriteEffect](MicroEngine.Graphics.SpriteEffect.md)

`at` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

False when the effect has ended.

### <a id="MicroEngine_Graphics_SpriteEffects_Play_MicroEngine_Graphics_AnimationClip_System_Numerics_Vector2_System_Single_System_Boolean_System_Nullable_MicroEngine_Color__System_Nullable_System_Single__System_Int32_System_Single_"></a> Play\(AnimationClip, Vector2, float, bool, Color?, float?, int, float\)

Plays <code class="paramref">clip</code> once with its anchor at <code class="paramref">at</code>, then removes it.

```csharp
public SpriteEffect Play(AnimationClip clip, Vector2 at, float delay = 0, bool flipX = false, Color? tint = null, float? framesPerSecond = null, int layer = 0, float depth = 0)
```

#### Parameters

`clip` [AnimationClip](MicroEngine.Graphics.AnimationClip.md)

The frames and their timing ([Clip](MicroEngine.Graphics.SpriteAtlas.md#MicroEngine_Graphics_SpriteAtlas_Clip_System_String_System_Nullable_System_Boolean__)).

`at` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Where the frames' anchor goes, in the coordinates the effects are drawn in.

`delay` [float](https://learn.microsoft.com/dotnet/api/system.single)

Seconds before it shows; a negative value, an infinite one or one that is not a number is none.

`flipX` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Mirrors it around its anchor.

`tint` [Color](MicroEngine.Color.md)?

Colour multiplier; null for white (unchanged).

`framesPerSecond` [float](https://learn.microsoft.com/dotnet/api/system.single)?

Plays it at this rate instead of the clip's own timing.

`layer` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Its layer in a [SortedRenderer](MicroEngine.Graphics.SortedRenderer.md) block.

`depth` [float](https://learn.microsoft.com/dotnet/api/system.single)

Its depth within the layer in a [SortedRenderer](MicroEngine.Graphics.SortedRenderer.md) block.

#### Returns

 [SpriteEffect](MicroEngine.Graphics.SpriteEffect.md)

The effect, to ask about, move or stop; most callers ignore it.

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">framesPerSecond</code> is not a positive number.

### <a id="MicroEngine_Graphics_SpriteEffects_Stop_MicroEngine_Graphics_SpriteEffect_"></a> Stop\(SpriteEffect\)

Removes <code class="paramref">effect</code> now.

```csharp
public bool Stop(SpriteEffect effect)
```

#### Parameters

`effect` [SpriteEffect](MicroEngine.Graphics.SpriteEffect.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

False when the effect had already ended.

### <a id="MicroEngine_Graphics_SpriteEffects_Update_System_Single_"></a> Update\(float\)

Advances the effects by <code class="paramref">dt</code> seconds: a waiting effect counts its delay down, a playing one
its time up, and those that reached the end of their clip are removed. A step that is not a time (negative,
infinite, not a number) is ignored: time does not run backwards and every effect ends.

```csharp
public void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

</div>
