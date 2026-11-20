# Enum NavAgentStatus

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Navigation](MicroEngine.Navigation.md)  
Assembly: MicroEngine.Navigation.dll  
Guide: [Navigation](../guide/navigation.md)  

What an agent of [NavAgents](MicroEngine.Navigation.NavAgents.md) is doing.

```csharp
public enum NavAgentStatus
```

## Fields

`Arrived = 3` 

Its order ended: it stands at its destination, as near it as it could get, or beside those that arrived before it.



`Idle = 0` 

It has no order: it stands where it is.



`Moving = 2` 

It walks its route, or follows its field.



`Planning = 1` 

It has an order and no route yet: its route is being searched, and it stands meanwhile.

</div>
