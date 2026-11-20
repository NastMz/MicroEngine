# Class NavAgents

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Navigation](MicroEngine.Navigation.md)  
Assembly: MicroEngine.Navigation.dll  
Guide: [Navigation](../guide/navigation.md)  

The agents that walk a [NavGrid](MicroEngine.Navigation.NavGrid.md): each follows the route to where it was sent, slows down and stops
there, searches again when a change of the grid closes its way, keeps apart from the others and turns aside from
those it would run into, and never enters a wall or another body. It is what a game writes after it has a path;
what an agent wants stays the game's. Add it to a scene (<code>Systems.Add(new NavAgents(grid))</code>) and it is
stepped with the scene and is part of its state hash; or keep one and call [Step](MicroEngine.Navigation.NavAgents.md#MicroEngine_Navigation_NavAgents_Step_System_Single_).

```csharp
public sealed class NavAgents : SceneSystem, IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SceneSystem](MicroEngine.Scenes.SceneSystem.md) ← 
[NavAgents](MicroEngine.Navigation.NavAgents.md)

#### Implements

[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

#### Inherited Members

[SceneSystem.HashState\(StateHasher\)](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_HashState_MicroEngine_Kernel_StateHasher_), 
[SceneSystem.Scene](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Scene), 
[SceneSystem.Game](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Game), 
[SceneSystem.IsAttached](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_IsAttached), 
[SceneSystem.Order](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Order), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Examples

```csharp
var agents = Systems.Add(new NavAgents(grid));
var unit = agents.Add(spawn, new NavAgentSettings(radius: 5, speed: 80));
unit.MoveTo(click);                                   // an order
// each frame
r.FillCircle(unit.Position, unit.Radius, color);
if (unit.Status == NavAgentStatus.Arrived) Salute(unit);
```

## Remarks

<ul><li><b>Order.</b> Agents are stepped in the order they were added, and searches are served in the order they
were asked for. With the same orders in the same steps a run repeats exactly, on every machine.</li><li><b>Searches.</b> One step looks at [SearchBudget](MicroEngine.Navigation.NavAgents.md#MicroEngine_Navigation_NavAgents_SearchBudget) nodes at most, for all agents together: when
many are ordered at once on a large map, the rest wait their turn ([Planning](MicroEngine.Navigation.NavAgentStatus.md)) instead
of stalling the step.</li><li><b>Bodies.</b> An agent is a circle for the others and a square for walls. Agents are not obstacles in the
grid: block cells for things that stay.</li><li><b>Memory.</b> Stepping allocates nothing once the lists have grown.</li></ul>

## Constructors

### <a id="MicroEngine_Navigation_NavAgents__ctor_MicroEngine_Navigation_NavGrid_"></a> NavAgents\(NavGrid\)

A system of agents that walk <code class="paramref">grid</code>.

```csharp
public NavAgents(NavGrid grid)
```

#### Parameters

`grid` [NavGrid](MicroEngine.Navigation.NavGrid.md)

## Properties

### <a id="MicroEngine_Navigation_NavAgents_BlockedAfter"></a> BlockedAfter

After how many seconds of making almost no way an agent with an order is [IsBlocked](MicroEngine.Navigation.NavAgent.md#MicroEngine_Navigation_NavAgent_IsBlocked);
1 by default. Positive.

```csharp
public float BlockedAfter { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is not positive and finite.

### <a id="MicroEngine_Navigation_NavAgents_Count"></a> Count

How many agents there are.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_NavAgents_Expanded"></a> Expanded

How many nodes those searches looked at.

```csharp
public long Expanded { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Navigation_NavAgents_Grid"></a> Grid

The grid the agents walk.

```csharp
public NavGrid Grid { get; }
```

#### Property Value

 [NavGrid](MicroEngine.Navigation.NavGrid.md)

### <a id="MicroEngine_Navigation_NavAgents_SearchBudget"></a> SearchBudget

The most nodes all the searches of one step look at together (4096 by default; `MaxValue` for
no limit). Agents whose route is not known yet wait their turn. Positive.

```csharp
public int SearchBudget { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is not positive.

### <a id="MicroEngine_Navigation_NavAgents_Searches"></a> Searches

How many searches the agents began since the system was made (a measure of its work).

```csharp
public int Searches { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_NavAgents_Item_System_Int32_"></a> this\[int\]

The agent at <code class="paramref">index</code>, in the order they were added.

```csharp
public NavAgent this[int index] { get; }
```

#### Property Value

 [NavAgent](MicroEngine.Navigation.NavAgent.md)

## Methods

### <a id="MicroEngine_Navigation_NavAgents_Add_System_Numerics_Vector2_MicroEngine_Navigation_NavAgentSettings_"></a> Add\(Vector2, NavAgentSettings\)

Adds an agent at <code class="paramref">position</code>, with no order.

```csharp
public NavAgent Add(Vector2 position, NavAgentSettings settings)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`settings` [NavAgentSettings](MicroEngine.Navigation.NavAgentSettings.md)

#### Returns

 [NavAgent](MicroEngine.Navigation.NavAgent.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">position</code> is not finite.

### <a id="MicroEngine_Navigation_NavAgents_CanBlock_MicroEngine_RectF_"></a> CanBlock\(RectF\)

Whether <code class="paramref">area</code> can be blocked (a tower built, a gate shut) without harming the agents: no
body overlaps it, and every agent that walks to a destination it can reach keeps a way to it. Nothing is
changed: when it is true, block the area ([SetBlocked](MicroEngine.Navigation.NavGrid.md#MicroEngine_Navigation_NavGrid_SetBlocked_MicroEngine_RectF_System_Boolean_)) and the agents whose way it
closes go around. It does not know the game's own fixed places (the gate creeps will come from): ask
[Regions](MicroEngine.Navigation.NavAgents.md#MicroEngine_Navigation_NavAgents_Regions_System_Single_) for those. Agents that follow a field are not looked at.

```csharp
public bool CanBlock(RectF area)
```

#### Parameters

`area` [RectF](MicroEngine.RectF.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavAgents_Clear"></a> Clear\(\)

Removes every agent.

```csharp
public void Clear()
```

### <a id="MicroEngine_Navigation_NavAgents_Contains_MicroEngine_Navigation_NavAgent_"></a> Contains\(NavAgent\)

Whether <code class="paramref">agent</code> is in this system.

```csharp
public bool Contains(NavAgent agent)
```

#### Parameters

`agent` [NavAgent](MicroEngine.Navigation.NavAgent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavAgents_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the system's simulation state to the scene's state hash, after the scene's coroutines, tweens, objects and
widgets and before the scene's own [HashState](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_HashState_MicroEngine_Kernel_StateHasher_). The engine adds the system's type name
first. The default adds nothing.

```csharp
public override void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Navigation_NavAgents_Occupied_MicroEngine_RectF_"></a> Occupied\(RectF\)

Whether the body of some agent overlaps <code class="paramref">area</code>.

```csharp
public bool Occupied(RectF area)
```

#### Parameters

`area` [RectF](MicroEngine.RectF.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavAgents_Query_System_Numerics_Vector2_System_Single_System_Collections_Generic_List_MicroEngine_Navigation_NavAgent__"></a> Query\(Vector2, float, List<NavAgent\>\)

Adds to <code class="paramref">results</code> (without clearing it) the agents whose body reaches within
<code class="paramref">radius</code> of <code class="paramref">center</code>, in the order they were added, and returns how many:
the creeps in a tower's range, the units under a selection circle.

```csharp
public int Query(Vector2 center, float radius, List<NavAgent> results)
```

#### Parameters

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`results` [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[NavAgent](MicroEngine.Navigation.NavAgent.md)\>

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">center</code> is not finite, or <code class="paramref">radius</code> is negative or not finite.

### <a id="MicroEngine_Navigation_NavAgents_Regions_System_Single_"></a> Regions\(float\)

What agents of <code class="paramref">radius</code> can reach on the grid: the regions the system itself asks before it
searches, kept up to date with the grid. One is shared by every size that needs the same room.

```csharp
public NavRegions Regions(float radius)
```

#### Parameters

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [NavRegions](MicroEngine.Navigation.NavRegions.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">radius</code> is negative or not finite.

### <a id="MicroEngine_Navigation_NavAgents_Remove_MicroEngine_Navigation_NavAgent_"></a> Remove\(NavAgent\)

Removes <code class="paramref">agent</code>; it takes no more orders. Returns whether it was in the system.

```csharp
public bool Remove(NavAgent agent)
```

#### Parameters

`agent` [NavAgent](MicroEngine.Navigation.NavAgent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavAgents_Step_System_Single_"></a> Step\(float\)

Advances every agent by <code class="paramref">dt</code> seconds: searches the routes asked for, steers, moves and keeps
the bodies apart. A scene calls it for a system it holds; call it yourself for one kept outside a scene.

```csharp
public void Step(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">dt</code> is negative or not finite.

### <a id="MicroEngine_Navigation_NavAgents_Stop"></a> Stop\(\)

Called once when the system is removed or its scene leaves the stack, after the scene's
[Exit](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Exit): release what the system owns. [Scene](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Scene) is still readable.

```csharp
protected override void Stop()
```

### <a id="MicroEngine_Navigation_NavAgents_Update_System_Single_"></a> Update\(float\)

Advances the system by one fixed step, after the scene's coroutines, tweens and widgets and before the scene's
[Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_). Not called while the scene is not updated (under a blocking overlay, during
the covering half of a transition).

```csharp
protected override void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

The step the scene's [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) receives: the step on the world's clock once the scene has objects, the engine's step otherwise.

</div>
