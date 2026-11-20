# Class AudioBus

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

A named mix stage: the sounds and buses under it are mixed together, pass through its effects
([Add](MicroEngine.Audio.AudioBus.md#MicroEngine_Audio_AudioBus_Add_MicroEngine_Audio_AudioEffect_)) and are scaled by its [Volume](MicroEngine.Audio.AudioBus.md#MicroEngine_Audio_AudioBus_Volume). Buses form a tree under "master": a bus is created
under a parent that already exists and is never moved, so there is no cycle.

```csharp
public sealed class AudioBus
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AudioBus](MicroEngine.Audio.AudioBus.md)

## Remarks

A bus without effects costs nothing: its sounds are added straight into the bus above it. With an effect, the bus is
mixed into a buffer of its own first (a submix), processed there, scaled by its volume and added to the bus above.

## Properties

### <a id="MicroEngine_Audio_AudioBus_Effects"></a> Effects

The bus's effects, in the order the mix passes through them.

```csharp
public IReadOnlyList<AudioEffect> Effects { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[AudioEffect](MicroEngine.Audio.AudioEffect.md)\>

### <a id="MicroEngine_Audio_AudioBus_Name"></a> Name

The bus's name.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Audio_AudioBus_Parent"></a> Parent

The bus it plays into; null for "master".

```csharp
public AudioBus? Parent { get; }
```

#### Property Value

 [AudioBus](MicroEngine.Audio.AudioBus.md)?

### <a id="MicroEngine_Audio_AudioBus_Volume"></a> Volume

Gain of everything on the bus, clamped to 0..4; it changes at the frame of the current step, at once (use
[FadeTo](MicroEngine.Audio.AudioBus.md#MicroEngine_Audio_AudioBus_FadeTo_System_Single_System_Single_) for a change without a step in the sound). A value that is not a number is ignored.

```csharp
public float Volume { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Audio_AudioBus_Add_MicroEngine_Audio_AudioEffect_"></a> Add\(AudioEffect\)

Adds <code class="paramref">effect</code> at the end of the chain, from the current step's frame; returns the bus, to add another.

```csharp
public AudioBus Add(AudioEffect effect)
```

#### Parameters

`effect` [AudioEffect](MicroEngine.Audio.AudioEffect.md)

#### Returns

 [AudioBus](MicroEngine.Audio.AudioBus.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The effect is already on a bus.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The effect listens to a bus that does not exist, to this bus or to one this bus plays into.

### <a id="MicroEngine_Audio_AudioBus_ClearEffects"></a> ClearEffects\(\)

Takes every effect out of the chain.

```csharp
public void ClearEffects()
```

### <a id="MicroEngine_Audio_AudioBus_FadeTo_System_Single_System_Single_"></a> FadeTo\(float, float\)

Moves [Volume](MicroEngine.Audio.AudioBus.md#MicroEngine_Audio_AudioBus_Volume) to <code class="paramref">volume</code> (clamped to 0..4) over <code class="paramref">seconds</code>, frame
by frame from the current step's frame; [Volume](MicroEngine.Audio.AudioBus.md#MicroEngine_Audio_AudioBus_Volume) reads the new value at once. A volume that is not a
number is ignored; 0 seconds or less changes it at once.

```csharp
public void FadeTo(float volume, float seconds)
```

#### Parameters

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

`seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_AudioBus_Insert_System_Int32_MicroEngine_Audio_AudioEffect_"></a> Insert\(int, AudioEffect\)

Adds <code class="paramref">effect</code> at <code class="paramref">index</code> of the chain (0: first), from the current step's frame.

```csharp
public AudioBus Insert(int index, AudioEffect effect)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`effect` [AudioEffect](MicroEngine.Audio.AudioEffect.md)

#### Returns

 [AudioBus](MicroEngine.Audio.AudioBus.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The effect is already on a bus.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">index</code> is not in 0..<code>Effects.Count</code>.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The effect listens to a bus that does not exist, to this bus or to one this bus plays into.

### <a id="MicroEngine_Audio_AudioBus_Remove_MicroEngine_Audio_AudioEffect_"></a> Remove\(AudioEffect\)

Takes <code class="paramref">effect</code> out of the chain, from the current step's frame (what it still held, a reverb's tail, is not heard).

```csharp
public bool Remove(AudioEffect effect)
```

#### Parameters

`effect` [AudioEffect](MicroEngine.Audio.AudioEffect.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it was on this bus.

</div>
