# Enum DebugSpace

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Where a [DebugDraw](MicroEngine.Core.DebugDraw.md) item is placed.

```csharp
public enum DebugSpace
```

## Fields

`Screen = 1` 

Canvas pixels, above the scenes, whatever the camera.



`World = 0` 

World coordinates, under the current scene's [WorldCamera](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_WorldCamera) (canvas pixels when it has none).

</div>
