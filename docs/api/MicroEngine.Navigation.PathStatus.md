# Enum PathStatus

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Navigation](MicroEngine.Navigation.md)  
Assembly: MicroEngine.Navigation.dll  
Guide: [Navigation](../guide/navigation.md)  

How a search ended.

```csharp
public enum PathStatus
```

## Fields

`Found = 0` 

The path reaches the goal and no cheaper one exists.



`Incomplete = 2` 

The search stopped at [MaxExpansions](MicroEngine.Navigation.PathFinder.md#MicroEngine_Navigation_PathFinder_MaxExpansions) before knowing. The path leads to the nearest node found so far.



`Unreachable = 1` 

No path reaches the goal. The path leads to the reachable node the estimate puts nearest to it.

</div>
