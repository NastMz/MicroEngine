# Enum FramePhase

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

The phases of a frame, in the order they run. The four fixed phases ([PreFixed](MicroEngine.Kernel.FramePhase.md),
[FixedUpdate](MicroEngine.Kernel.FramePhase.md), [Physics](MicroEngine.Kernel.FramePhase.md), [PostFixed](MicroEngine.Kernel.FramePhase.md)) run once per fixed step, zero or more times
a frame as time calls for ([TimeSettings](MicroEngine.Kernel.TimeSettings.md)); every other phase runs once per frame.

```csharp
public enum FramePhase
```

## Fields

`BeginFrame = 0` 

First, once per frame: bookkeeping before anything reads the frame (finishing background loads).



`EndFrame = 11` 

Last, once per frame.



`Extract = 8` 

Once per frame: copying what will be drawn out of the simulation.



`FixedUpdate = 3` 

Once per fixed step: the simulation (scenes and game rules).



`Input = 1` 

Once per frame, after the platform's events were read: input that follows the display rate (touch controls).



`LateUpdate = 7` 

Once per frame, after [Update](MicroEngine.Kernel.FramePhase.md): what must see everything updated first.



`Physics = 4` 

Once per fixed step, after [FixedUpdate](MicroEngine.Kernel.FramePhase.md): physics and collision.



`PostFixed = 5` 

Once per fixed step, last: what follows the simulated state (audio, toasts, saving, resetting input edges).



`PreFixed = 2` 

Once per fixed step, before the simulation: sampling the input the step reads.



`Present = 10` 

Once per frame: presenting the drawn frame.



`Render = 9` 

Once per frame: drawing.



`Update = 6` 

Once per frame, after the frame's steps, at the display's rate: camera smoothing, UI animation, interpolation.

</div>
