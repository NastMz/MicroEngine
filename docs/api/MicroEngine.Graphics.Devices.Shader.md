# Class Shader

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

A custom shader stage.

```csharp
public abstract class Shader : GraphicsResource, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GraphicsResource](MicroEngine.Graphics.Devices.GraphicsResource.md) ← 
[Shader](MicroEngine.Graphics.Devices.Shader.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

#### Inherited Members

[GraphicsResource.Dispose\(\)](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_Dispose), 
[GraphicsResource.Release\(\)](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_Release), 
[GraphicsResource.Expire\(\)](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_Expire), 
[GraphicsResource.MarkLost\(\)](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_MarkLost), 
[GraphicsResource.Device](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_Device), 
[GraphicsResource.Name](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_Name), 
[GraphicsResource.Handle](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_Handle), 
[GraphicsResource.IsDisposed](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_IsDisposed), 
[GraphicsResource.IsLost](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_IsLost), 
[GraphicsResource.OwnedBy](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_OwnedBy), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Constructors

### <a id="MicroEngine_Graphics_Devices_Shader__ctor_MicroEngine_Graphics_Devices_IGraphicsDevice_MicroEngine_Graphics_Devices_ShaderDescription__"></a> Shader\(IGraphicsDevice, in ShaderDescription\)

Creates the shader object.

```csharp
protected Shader(IGraphicsDevice device, in ShaderDescription description)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

`description` [ShaderDescription](MicroEngine.Graphics.Devices.ShaderDescription.md)

## Properties

### <a id="MicroEngine_Graphics_Devices_Shader_Format"></a> Format

Bytecode format.

```csharp
public ShaderFormat Format { get; }
```

#### Property Value

 [ShaderFormat](MicroEngine.Graphics.Devices.ShaderFormat.md)

### <a id="MicroEngine_Graphics_Devices_Shader_Resources"></a> Resources

Reflected resource counts (validated against bindings at draw recording).

```csharp
public ShaderResources Resources { get; }
```

#### Property Value

 [ShaderResources](MicroEngine.Graphics.Devices.ShaderResources.md)

### <a id="MicroEngine_Graphics_Devices_Shader_Stage"></a> Stage

Stage.

```csharp
public ShaderStage Stage { get; }
```

#### Property Value

 [ShaderStage](MicroEngine.Graphics.Devices.ShaderStage.md)

</div>
