# Enum CommandKind

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Kind of a recorded command (backend-facing; see [Command](MicroEngine.Graphics.Devices.Command.md) for the fields each kind uses).

```csharp
public enum CommandKind
```

## Fields

`BeginPass = 0` 

[Pass](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_Pass).



`Draw = 9` 

[Count](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_Count) vertices from [First](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_First), [Instances](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_Instances).



`DrawIndexed = 10` 

[Count](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_Count) indices from [First](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_First), [BaseVertex](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_BaseVertex), [Instances](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_Instances).



`EndPass = 1` 

No fields.



`SetIndices = 7` 

[Resource](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_Resource) ([GpuBuffer](MicroEngine.Graphics.Devices.GpuBuffer.md)), [IndexSize](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_IndexSize).



`SetPipeline = 4` 

[Resource](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_Resource) is the [Pipeline](MicroEngine.Graphics.Devices.Pipeline.md).



`SetScissor = 3` 

[Rect](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_Rect) (already clipped to the target).



`SetTexture = 5` 

[Slot](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_Slot), [Resource](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_Resource) ([Texture](MicroEngine.Graphics.Devices.Texture.md)), [Sampler](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_Sampler).



`SetUniforms = 8` 

[Stage](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_Stage), [Slot](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_Slot), payload.



`SetVertices = 6` 

[Resource](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_Resource) ([GpuBuffer](MicroEngine.Graphics.Devices.GpuBuffer.md)).



`SetViewport = 2` 

[Rect](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_Rect).



`UpdateBuffer = 11` 

[Resource](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_Resource) ([GpuBuffer](MicroEngine.Graphics.Devices.GpuBuffer.md)), byte offset in [First](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_First), payload.



`UpdateTexture = 12` 

[Resource](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_Resource) ([Texture](MicroEngine.Graphics.Devices.Texture.md)), [Rect](MicroEngine.Graphics.Devices.Command.md#MicroEngine_Graphics_Devices_Command_Rect) region, payload.

</div>
