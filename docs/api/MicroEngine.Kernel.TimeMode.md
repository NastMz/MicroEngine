# Enum TimeMode

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

How the loop advances time ([Mode](MicroEngine.Kernel.TimeSettings.md#MicroEngine_Kernel_TimeSettings_Mode)).

```csharp
public enum TimeMode
```

## Fields

`Deterministic = 1` 

Time is the loop's own: every frame stands for exactly one fixed step, whatever the wall clock says, so a run with
the same input produces the same steps on any machine at any speed (scripted runs, tests, captures).



`RealTime = 0` 

Time follows the wall clock: each frame adds the elapsed time the loop measured (at most
[MaxFrameDelta](MicroEngine.Kernel.TimeSettings.md#MicroEngine_Kernel_TimeSettings_MaxFrameDelta)), and the simulation runs as many fixed steps as that time calls for.

</div>
