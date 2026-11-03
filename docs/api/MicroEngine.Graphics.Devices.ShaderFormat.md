# Enum ShaderFormat

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Offline-compiled shader bytecode formats (produced by the tool from one HLSL source).

```csharp
[Flags]
public enum ShaderFormat
```

## Fields

`Dxil = 2` 

DXIL (Direct3D 12).



`Msl = 4` 

Metal Shading Language source (Metal).



`None = 0` 

No shader format (tier 0).



`SpirV = 1` 

SPIR-V (Vulkan).

</div>
