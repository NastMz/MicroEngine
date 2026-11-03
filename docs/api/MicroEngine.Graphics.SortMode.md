# Enum SortMode

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

The order in which a batch's draws reach the device ([Sort](MicroEngine.Graphics.BatchSettings.md#MicroEngine_Graphics_BatchSettings_Sort)).

```csharp
public enum SortMode
```

## Fields

`BackToFront = 2` 

Largest depth first (painter's order), then submission order.



`Deferred = 0` 

Submission order (the default): later draws cover earlier ones.



`FrontToBack = 3` 

Smallest depth first, then submission order.



`Texture = 1` 

Grouped by texture, then submission order: fewest draw calls, for draws that do not overlap.

</div>
