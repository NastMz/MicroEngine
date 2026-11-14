# Class Timeline

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

A playhead that drives several `Track`1`s together and raises named events at set times: a sequence
of movements with the moments the game reacts to (a footstep, the frame a swing lands). Advance it with
[Update](MicroEngine.Scenes.Timeline.md#MicroEngine_Scenes_Timeline_Update_System_Single_), like an <code>Animator</code>; several timelines over different values play as layers.

```csharp
public sealed class Timeline : IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Timeline](MicroEngine.Scenes.Timeline.md)

#### Implements

[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

## Constructors

### <a id="MicroEngine_Scenes_Timeline__ctor_System_Single_System_Boolean_"></a> Timeline\(float, bool\)

Creates a timeline at time 0.

```csharp
public Timeline(float duration, bool loop = false)
```

#### Parameters

`duration` [float](https://learn.microsoft.com/dotnet/api/system.single)

Its length in seconds; positive.

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it starts over at the end; otherwise it stops there.

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">duration</code> is not a positive number.

## Properties

### <a id="MicroEngine_Scenes_Timeline_Duration"></a> Duration

Length of one pass, in seconds.

```csharp
public float Duration { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_Timeline_Finished"></a> Finished

Whether a timeline that does not loop is at its end, played or moved there with [Seek](MicroEngine.Scenes.Timeline.md#MicroEngine_Scenes_Timeline_Seek_System_Single_); it stays
finished until [Seek](MicroEngine.Scenes.Timeline.md#MicroEngine_Scenes_Timeline_Seek_System_Single_) moves it back. Always false for a looping one.

```csharp
public bool Finished { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_Timeline_Loop"></a> Loop

Whether the timeline starts over at the end; otherwise it stops there.

```csharp
public bool Loop { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_Timeline_Speed"></a> Speed

Playback speed multiplier: 1 by default, 0 pauses, 2 plays twice as fast.

```csharp
public float Speed { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

Set to a negative value, infinity or NaN (timelines do not play backwards).

### <a id="MicroEngine_Scenes_Timeline_Time"></a> Time

The playhead, in seconds from the start: 0 to [Duration](MicroEngine.Scenes.Timeline.md#MicroEngine_Scenes_Timeline_Duration).

```csharp
public float Time { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Scenes_Timeline_Add__1_MicroEngine_Track___0__System_Action___0__"></a> Add<T\>\(Track<T\>, Action<T\>\)

Plays <code class="paramref">track</code> on this timeline: each update, <code class="paramref">apply</code> receives its value at the playhead.

```csharp
public Timeline Add<T>(Track<T> track, Action<T> apply)
```

#### Parameters

`track` [Track](MicroEngine.Track.md)<T\>

`apply` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<T\>

#### Returns

 [Timeline](MicroEngine.Scenes.Timeline.md)

This timeline, for chaining.

#### Type Parameters

`T` 

### <a id="MicroEngine_Scenes_Timeline_At_System_Single_System_String_"></a> At\(float, string\)

Adds the event <code class="paramref">name</code> at <code class="paramref">time</code> seconds. An event at 0 fires in the first
update and, when looping, each time the timeline starts over; an event at [Duration](MicroEngine.Scenes.Timeline.md#MicroEngine_Scenes_Timeline_Duration) fires when
the timeline ends and, when looping, at the end of each pass, before the events at 0 of the next one.

```csharp
public Timeline At(float time, string name)
```

#### Parameters

`time` [float](https://learn.microsoft.com/dotnet/api/system.single)

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [Timeline](MicroEngine.Scenes.Timeline.md)

This timeline, for chaining.

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">time</code> is outside 0..[Duration](MicroEngine.Scenes.Timeline.md#MicroEngine_Scenes_Timeline_Duration).

### <a id="MicroEngine_Scenes_Timeline_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the timeline's state to the per-step state hash: its playhead, its speed and whether it started playing from there.

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Scenes_Timeline_Seek_System_Single_"></a> Seek\(float\)

Moves the playhead to <code class="paramref">time</code> seconds (kept within 0..[Duration](MicroEngine.Scenes.Timeline.md#MicroEngine_Scenes_Timeline_Duration), on a looping
timeline too) and applies the tracks there, raising no events, neither the ones it jumps over nor one at that
time; <code>Seek(0)</code> plays the timeline again. The next update that advances raises the events from that time
on, one exactly there included. Moving a timeline that does not loop to its end finishes it: the tracks are
applied there and nothing more plays, the events at the end included, until it is moved back.

```csharp
public void Seek(float time)
```

#### Parameters

`time` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">time</code> is NaN.

### <a id="MicroEngine_Scenes_Timeline_Update_System_Single_"></a> Update\(float\)

Advances the playhead by <code class="paramref">dt</code> seconds times [Speed](MicroEngine.Scenes.Timeline.md#MicroEngine_Scenes_Timeline_Speed), applies every track and raises
the events it reached; an update longer than a looping timeline raises the events of every pass it covered, up
to 1,000 passes (a longer update skips the passes in between and ends at the same place). With nothing to
advance (a pause, 0 seconds) it only applies the tracks and raises nothing. A finished timeline does nothing,
so the game may change the values afterwards.

```csharp
public void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">dt</code> is negative, infinite or NaN (timelines do not play backwards).

### <a id="MicroEngine_Scenes_Timeline_Reached"></a> Reached

Raised with an event's name when the playhead reaches its time, after the tracks were applied for that update.
Events fire in time order (in the order they were added for equal times), once per pass.

```csharp
public event Action<string>? Reached
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

</div>
