# Class AudioEffect

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

A processor in a bus's effect chain ([Add](MicroEngine.Audio.AudioBus.md#MicroEngine_Audio_AudioBus_Add_MicroEngine_Audio_AudioEffect_)): it changes the bus's mix in place, one block at a
time, on the thread that renders audio. The built-in effects (<code>MicroEngine.Audio.Effects</code>) derive from it, and so
does an effect a game or a package writes.

```csharp
public abstract class AudioEffect
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AudioEffect](MicroEngine.Audio.AudioEffect.md)

#### Derived

[BiquadFilter](MicroEngine.Audio.Effects.BiquadFilter.md), 
[Compressor](MicroEngine.Audio.Effects.Compressor.md), 
[Delay](MicroEngine.Audio.Effects.Delay.md), 
[Limiter](MicroEngine.Audio.Effects.Limiter.md), 
[Reverb](MicroEngine.Audio.Effects.Reverb.md)

## Examples

```csharp
public sealed class Tremolo : AudioEffect
{
    private float _rate = 5, _liveRate = 5, _phase, _step;
    private int _channels;
    public Tremolo() : base(parameters: 1) { }
    public float Rate { get => _rate; set { _rate = value; SetParameter(0, value); } }
    protected override void Prepare(int sampleRate, int channels) => (_channels, _step) = (channels, MathF.Tau / sampleRate);
    protected override void OnParameter(int index, float value) => _liveRate = value;
    protected override void Process(Span<float> buffer)
    {
        for (int i = 0; i < buffer.Length; i += _channels)
        {
            float gain = 0.5f + 0.5f * MathF.Cos(_phase);
            _phase = (_phase + _step * _liveRate) % MathF.Tau;
            for (int c = 0; c < _channels; c++) buffer[i + c] *= gain;
        }
    }
}
```

## Remarks

<p>
Two threads touch an effect. The game's thread creates it, sets its properties and adds it to a bus. The rendering
thread calls [Process](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_Process_System_Span_System_Single__). They never share a field: a property setter calls
[SetParameter](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_SetParameter_System_Int32_System_Single_), which keeps the value for the getter and sends it to the rendering thread through the
mixer's command queue; the rendering thread receives it in [OnParameter](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_OnParameter_System_Int32_System_Single_) at the exact output frame of
the game step that set it, as a voice's volume does. No lock is involved, and the same calls give the same samples
whatever block sizes the sink renders.
</p>
<p>
What keeps the two apart, as rules of the contract: [Prepare](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_Prepare_System_Int32_System_Int32_) is called once in an effect's life, on the
game's thread, before the rendering thread has ever seen it; from then on only the rendering thread calls
[OnParameter](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_OnParameter_System_Int32_System_Single_), [Reset](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_Reset) and <code>Process</code>, also when the effect is taken off its bus and
added again, or moved to another bus (it is cleared with [Reset](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_Reset) where it enters a chain). An effect
belongs to the first mixer that plays it: it cannot be added to a bus of another one, whose rendering thread would
process it while the first may still do so. And <code>Process</code> never sets the effect's own properties: they are
the game's thread's.
</p>
<p>
[Process](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_Process_System_Span_System_Single__) must not allocate, lock, block or throw. An effect that throws is taken out of the
mix for good ([Faulted](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_Faulted)) and the bus goes on without it.
</p>

## Constructors

### <a id="MicroEngine_Audio_AudioEffect__ctor_System_Int32_"></a> AudioEffect\(int\)

Creates an effect with <code class="paramref">parameters</code> numbered parameters (0 for an effect without any).

```csharp
protected AudioEffect(int parameters = 0)
```

#### Parameters

`parameters` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">parameters</code> is negative.

## Properties

### <a id="MicroEngine_Audio_AudioEffect_Bypass"></a> Bypass

Whether the bus's mix passes the effect unchanged, from the current step's frame; its state is cleared
([Reset](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_Reset)) when it is switched back in.

```csharp
public bool Bypass { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioEffect_Error"></a> Error

What [Process](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_Process_System_Span_System_Single__) (or another call on the rendering thread) threw, or null.

```csharp
public Exception? Error { get; }
```

#### Property Value

 [Exception](https://learn.microsoft.com/dotnet/api/system.exception)?

### <a id="MicroEngine_Audio_AudioEffect_Faulted"></a> Faulted

Whether [Process](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_Process_System_Span_System_Single__) threw: the effect no longer takes part in the mix.

```csharp
public bool Faulted { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioEffect_SidechainBus"></a> SidechainBus

The bus whose mix the effect listens to besides its own (a compressor that turns the music down while a voice
line plays); null for none. With a bus named here, the mixer calls
[Process](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_Process_System_Span_System_Single__) with that bus's mix, after its effects and volume.

```csharp
protected string? SidechainBus { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

Set while the effect is on a bus: the named bus does not exist, or it is the effect's own bus or one it plays
into (its mix is not known before this one's).

## Methods

### <a id="MicroEngine_Audio_AudioEffect_GetParameter_System_Int32_"></a> GetParameter\(int\)

The value a property last gave parameter <code class="paramref">index</code> (the game's thread).

```csharp
protected float GetParameter(int index)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_AudioEffect_OnParameter_System_Int32_System_Single_"></a> OnParameter\(int, float\)

Rendering thread: parameter <code class="paramref">index</code> takes <code class="paramref">value</code> from the next frame
processed. Every parameter is delivered once when the effect enters a chain, then at each change.

```csharp
protected virtual void OnParameter(int index, float value)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_AudioEffect_Prepare_System_Int32_System_Int32_"></a> Prepare\(int, int\)

Called once in the effect's life, on the game's thread, the first time it is added to a bus of a device that
plays, before the rendering thread processes anything: the place to allocate delay lines and tables for the
mixer's format. It is not called again when the effect is removed and added again, so it never runs while the
rendering thread uses the effect. On a device without sound it is not called at all.

```csharp
protected virtual void Prepare(int sampleRate, int channels)
```

#### Parameters

`sampleRate` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Frames per second.

`channels` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Samples per frame, 1 or 2.

### <a id="MicroEngine_Audio_AudioEffect_Process_System_Span_System_Single__"></a> Process\(Span<float\>\)

Rendering thread: processes <code class="paramref">buffer</code> in place. The samples are interleaved by channel
(<code>buffer.Length / channels</code> frames) and blocks follow one another without gaps, of any length from one frame.

```csharp
protected abstract void Process(Span<float> buffer)
```

#### Parameters

`buffer` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

### <a id="MicroEngine_Audio_AudioEffect_Process_System_Span_System_Single__System_ReadOnlySpan_System_Single__"></a> Process\(Span<float\>, ReadOnlySpan<float\>\)

Rendering thread, for an effect with a [SidechainBus](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_SidechainBus): processes <code class="paramref">buffer</code> in place,
with the same frames of the named bus in <code class="paramref">sidechain</code>. The default ignores them.

```csharp
protected virtual void Process(Span<float> buffer, ReadOnlySpan<float> sidechain)
```

#### Parameters

`buffer` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

`sidechain` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

### <a id="MicroEngine_Audio_AudioEffect_Reset"></a> Reset\(\)

Rendering thread: forgets what the effect has heard (delay lines, filter memory), when it enters a chain or leaves bypass.

```csharp
protected virtual void Reset()
```

### <a id="MicroEngine_Audio_AudioEffect_SetParameter_System_Int32_System_Single_"></a> SetParameter\(int, float\)

Sets parameter <code class="paramref">index</code> from a property setter (the game's thread): the rendering thread
receives it in [OnParameter](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_OnParameter_System_Int32_System_Single_) at the current step's frame. A value that is not a number is ignored.

```csharp
protected void SetParameter(int index, float value)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Called from [Process](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_Process_System_Span_System_Single__): the effect is taken out of the mix ([Faulted](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_Faulted)).

</div>
