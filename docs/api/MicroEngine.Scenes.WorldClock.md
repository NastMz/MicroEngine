# Class WorldClock

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.Scene.dll  
Guide: [Scenes](../manual/scenes.md)  

A world's own time: a time scale and freezes (hitstop) on top of the engine's fixed step. It advances as part of its
world's step ([BeginStep](MicroEngine.Scenes.SceneWorld.md#MicroEngine_Scenes_SceneWorld_BeginStep_System_Int64_System_Single_)), once per step: a world that does not step (paused, under an
overlay) consumes no time and no freeze.

```csharp
public sealed class WorldClock
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[WorldClock](MicroEngine.Scenes.WorldClock.md)

## Constructors

### <a id="MicroEngine_Scenes_WorldClock__ctor"></a> WorldClock\(\)

```csharp
public WorldClock()
```

## Properties

### <a id="MicroEngine_Scenes_WorldClock_Delta"></a> Delta

Scaled length of the latest step (0 while frozen or at scale 0).

```csharp
public float Delta { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_WorldClock_FreezeRemaining"></a> FreezeRemaining

Seconds of freeze left. Steps subtract their unscaled length while it is positive, so the last frozen step may
leave it slightly below zero (the whole step was frozen); it is not frozen when it is 0 or less.

```csharp
public float FreezeRemaining { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_WorldClock_Frozen"></a> Frozen

Whether the next step is frozen.

```csharp
public bool Frozen { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_WorldClock_Scale"></a> Scale

Multiplies the step (0 pauses, 0.5 is slow motion); 1 by default.

```csharp
public float Scale { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is negative.

### <a id="MicroEngine_Scenes_WorldClock_Time"></a> Time

Scaled seconds this world has advanced (freezes and scale applied).

```csharp
public double Time { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

## Methods

### <a id="MicroEngine_Scenes_WorldClock_Freeze_System_Single_"></a> Freeze\(float\)

Freezes the world for at least <code class="paramref">seconds</code> of unscaled step time (the longer freeze wins). Hitstop is a freeze.

```csharp
public void Freeze(float seconds)
```

#### Parameters

`seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_WorldClock_Unfreeze"></a> Unfreeze\(\)

Ends a freeze now.

```csharp
public void Unfreeze()
```

</div>
