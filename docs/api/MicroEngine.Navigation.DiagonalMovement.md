# Enum DiagonalMovement

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Navigation](MicroEngine.Navigation.md)  
Assembly: MicroEngine.Navigation.dll  
Guide: [Navigation](../guide/navigation.md)  

How an agent may move between the cells of a [NavGrid](MicroEngine.Navigation.NavGrid.md).

```csharp
public enum DiagonalMovement
```

## Fields

`Always = 2` 

Diagonally whenever the cell arrived at is walkable, corners included: for agents that are points.



`Never = 0` 

Up, down, left and right only.



`NoCornerCutting = 1` 

Diagonally too, but never between two cells when one of the cells beside the step is blocked: an agent as large as a cell does not clip a wall's corner. The default.

</div>
