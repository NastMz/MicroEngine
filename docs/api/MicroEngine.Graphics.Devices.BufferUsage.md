# Enum BufferUsage

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Buffer usage. Uniform data is not a buffer: it is pushed with [SetUniforms](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_SetUniforms_MicroEngine_Graphics_Devices_ShaderStage_System_Int32_System_ReadOnlySpan_System_Byte__).

```csharp
[Flags]
public enum BufferUsage
```

## Fields

`Index = 2` 

Bound with [SetIndices](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_SetIndices_MicroEngine_Graphics_Devices_GpuBuffer_MicroEngine_Graphics_Devices_IndexSize_).



`Vertex = 1` 

Bound with [SetVertices](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_SetVertices_MicroEngine_Graphics_Devices_GpuBuffer_).

</div>
