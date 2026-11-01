# Enum Wave

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.dll  
Guide: [Audio](../manual/audio.md)  

Oscillator waveforms for [SfxParams](MicroEngine.Audio.SfxParams.md) and [Instrument](MicroEngine.Audio.Instrument.md): <code>Square</code>, <code>Triangle</code>, <code>Saw</code>,
<code>Sine</code>, <code>Noise</code> (a random value held for each cycle, so its frequency sets the colour of the noise), and the
thinner pulse waves <code>Pulse25</code> and <code>Pulse12</code> (25% and 12.5% duty cycle).

```csharp
public enum Wave
```

## Fields

`Noise = 4` 

`Pulse12 = 6` 

`Pulse25 = 5` 

`Saw = 2` 

`Sine = 3` 

`Square = 0` 

`Triangle = 1`
