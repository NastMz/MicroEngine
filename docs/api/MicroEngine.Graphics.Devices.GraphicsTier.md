# Enum GraphicsTier

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Capability tier. Tiers differ only in declared capabilities, never in API shape.

```csharp
public enum GraphicsTier
```

## Fields

`Tier0 = 0` 

Fixed function: built-in pipelines only (SDL_Renderer, raylib, the CPU reference rasterizer).



`Tier1 = 1` 

Programmable: custom shaders, depth (SDL_GPU on native).

</div>
