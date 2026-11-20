# Class NavAgent

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Navigation](MicroEngine.Navigation.md)  
Assembly: MicroEngine.Navigation.dll  
Guide: [Navigation](../guide/navigation.md)  

One agent of [NavAgents](MicroEngine.Navigation.NavAgents.md): a body with a position that takes orders ([MoveTo](MicroEngine.Navigation.NavAgent.md#MicroEngine_Navigation_NavAgent_MoveTo_System_Numerics_Vector2_),
[Follow](MicroEngine.Navigation.NavAgent.md#MicroEngine_Navigation_NavAgent_Follow_MicroEngine_Navigation_DistanceField_), [Stop](MicroEngine.Navigation.NavAgent.md#MicroEngine_Navigation_NavAgent_Stop)) and tells how it is doing ([Status](MicroEngine.Navigation.NavAgent.md#MicroEngine_Navigation_NavAgent_Status)). The system creates it
([Add](MicroEngine.Navigation.NavAgents.md#MicroEngine_Navigation_NavAgents_Add_System_Numerics_Vector2_MicroEngine_Navigation_NavAgentSettings_)) and moves it each step; the game keeps the reference, reads
[Position](MicroEngine.Navigation.NavAgent.md#MicroEngine_Navigation_NavAgent_Position) to draw, and removes it when the thing it stands for is gone.

```csharp
public sealed class NavAgent
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[NavAgent](MicroEngine.Navigation.NavAgent.md)

## Properties

### <a id="MicroEngine_Navigation_NavAgent_DesiredVelocity"></a> DesiredVelocity

The velocity the agent wants in this step, after steering around the others: what a game that moves the body
itself ([MovedByGame](MicroEngine.Navigation.NavAgentSettings.md#MicroEngine_Navigation_NavAgentSettings_MovedByGame)) applies. Zero when the agent stands.

```csharp
public Vector2 DesiredVelocity { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Navigation_NavAgent_Destination"></a> Destination

The point of the latest [MoveTo](MicroEngine.Navigation.NavAgent.md#MicroEngine_Navigation_NavAgent_MoveTo_System_Numerics_Vector2_).

```csharp
public Vector2 Destination { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Navigation_NavAgent_IsBlocked"></a> IsBlocked

Whether the agent has an order and made almost no way for a while ([BlockedAfter](MicroEngine.Navigation.NavAgents.md#MicroEngine_Navigation_NavAgents_BlockedAfter)):
somebody stands in the only passage, or a wall the grid does not know holds it. It keeps trying, and searches
its route again once; the game decides whether to give up ([Stop](MicroEngine.Navigation.NavAgent.md#MicroEngine_Navigation_NavAgent_Stop)) or order something else.

```csharp
public bool IsBlocked { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavAgent_Path"></a> Path

How the search for the agent's route ended: [Found](MicroEngine.Navigation.PathStatus.md) when the route reaches
[Destination](MicroEngine.Navigation.NavAgent.md#MicroEngine_Navigation_NavAgent_Destination), [Unreachable](MicroEngine.Navigation.PathStatus.md) when it leads as near as the agent gets, and
[Searching](MicroEngine.Navigation.PathStatus.md) while a route is being searched.

```csharp
public PathStatus Path { get; }
```

#### Property Value

 [PathStatus](MicroEngine.Navigation.PathStatus.md)

### <a id="MicroEngine_Navigation_NavAgent_Position"></a> Position

Where the body's centre is. Setting it moves the body there at once (a teleport, or the place the game moved
it to when it is [MovedByGame](MicroEngine.Navigation.NavAgentSettings.md#MicroEngine_Navigation_NavAgentSettings_MovedByGame)); its order stands.

```csharp
public Vector2 Position { get; set; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The position is not finite.

### <a id="MicroEngine_Navigation_NavAgent_Radius"></a> Radius

The body's radius ([Radius](MicroEngine.Navigation.NavAgentSettings.md#MicroEngine_Navigation_NavAgentSettings_Radius)).

```csharp
public float Radius { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Navigation_NavAgent_Route"></a> Route

The points the agent still walks through, in order; empty when it has no route.

```csharp
public IReadOnlyList<Vector2> Route { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)\>

### <a id="MicroEngine_Navigation_NavAgent_Settings"></a> Settings

How the agent moves. It may be replaced while the agent walks.

```csharp
public NavAgentSettings Settings { get; set; }
```

#### Property Value

 [NavAgentSettings](MicroEngine.Navigation.NavAgentSettings.md)

### <a id="MicroEngine_Navigation_NavAgent_Status"></a> Status

What the agent is doing.

```csharp
public NavAgentStatus Status { get; }
```

#### Property Value

 [NavAgentStatus](MicroEngine.Navigation.NavAgentStatus.md)

### <a id="MicroEngine_Navigation_NavAgent_Tag"></a> Tag

Free for the game: the unit, enemy or object this agent stands for.

```csharp
public object? Tag { get; set; }
```

#### Property Value

 [object](https://learn.microsoft.com/dotnet/api/system.object)?

### <a id="MicroEngine_Navigation_NavAgent_Velocity"></a> Velocity

The velocity the body moved with in the latest step, in units per second: zero when it stands. What others
pushed it by is not counted.

```csharp
public Vector2 Velocity { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Methods

### <a id="MicroEngine_Navigation_NavAgent_Follow_MicroEngine_Navigation_DistanceField_"></a> Follow\(DistanceField\)

Orders the agent to follow <code class="paramref">field</code> to its goal: each step it heads for the cell the field
gives ([Next](MicroEngine.Navigation.DistanceField.md#MicroEngine_Navigation_DistanceField_Next_System_Int32_)), with no search of its own, and arrives in the goal's cell or beside
those that arrived before it. For many agents with one goal. The game computes the field again when the goal
or the grid changes; the agents read whatever it holds.

```csharp
public void Follow(DistanceField field)
```

#### Parameters

`field` [DistanceField](MicroEngine.Navigation.DistanceField.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The agent was removed from its system.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">field</code> measures another grid.

### <a id="MicroEngine_Navigation_NavAgent_MoveTo_System_Numerics_Vector2_"></a> MoveTo\(Vector2\)

Orders the agent to walk to <code class="paramref">point</code>. Its route is searched by the system's next step (later
when many agents wait and [SearchBudget](MicroEngine.Navigation.NavAgents.md#MicroEngine_Navigation_NavAgents_SearchBudget) is small); an agent that already walks keeps
its previous route until the new one is known. A point that cannot be reached sends it as near as it gets.
Calling it every step with a target that moves costs a search only when the target enters another cell.

```csharp
public void MoveTo(Vector2 point)
```

#### Parameters

`point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The agent was removed from its system.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">point</code> is not finite.

### <a id="MicroEngine_Navigation_NavAgent_Stop"></a> Stop\(\)

Ends the agent's order: it stands where it is, [Idle](MicroEngine.Navigation.NavAgentStatus.md).

```csharp
public void Stop()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The agent was removed from its system.

</div>
