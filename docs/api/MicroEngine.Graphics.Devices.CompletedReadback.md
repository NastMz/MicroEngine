# Class CompletedReadback

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

A readback whose pixels were copied when it was requested (devices that execute at submit).

```csharp
public sealed class CompletedReadback : ReadbackRequest
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ReadbackRequest](MicroEngine.Graphics.Devices.ReadbackRequest.md) ← 
[CompletedReadback](MicroEngine.Graphics.Devices.CompletedReadback.md)

#### Inherited Members

[ReadbackRequest.Wait\(\)](MicroEngine.Graphics.Devices.ReadbackRequest.md\#MicroEngine\_Graphics\_Devices\_ReadbackRequest\_Wait), 
[ReadbackRequest.Region](MicroEngine.Graphics.Devices.ReadbackRequest.md\#MicroEngine\_Graphics\_Devices\_ReadbackRequest\_Region), 
[ReadbackRequest.Format](MicroEngine.Graphics.Devices.ReadbackRequest.md\#MicroEngine\_Graphics\_Devices\_ReadbackRequest\_Format), 
[ReadbackRequest.IsCompleted](MicroEngine.Graphics.Devices.ReadbackRequest.md\#MicroEngine\_Graphics\_Devices\_ReadbackRequest\_IsCompleted), 
[ReadbackRequest.Pixels](MicroEngine.Graphics.Devices.ReadbackRequest.md\#MicroEngine\_Graphics\_Devices\_ReadbackRequest\_Pixels), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Constructors

### <a id="MicroEngine_Graphics_Devices_CompletedReadback__ctor_MicroEngine_Graphics_Devices_RectI_MicroEngine_Graphics_Devices_TextureFormat_System_Byte___"></a> CompletedReadback\(RectI, TextureFormat, byte\[\]\)

A completed request over pixels already read.

```csharp
public CompletedReadback(RectI region, TextureFormat format, byte[] pixels)
```

#### Parameters

`region` [RectI](MicroEngine.Graphics.Devices.RectI.md)

The region read.

`format` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

The format of <code class="paramref">pixels</code>.

`pixels` [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

Tightly packed pixels of the region.

## Properties

### <a id="MicroEngine_Graphics_Devices_CompletedReadback_IsCompleted"></a> IsCompleted

Always true.

```csharp
public override bool IsCompleted { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Graphics_Devices_CompletedReadback_Collect"></a> Collect\(\)

Backend: copies the completed data out and releases the staging memory (called once).

```csharp
protected override byte[] Collect()
```

#### Returns

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

### <a id="MicroEngine_Graphics_Devices_CompletedReadback_Wait"></a> Wait\(\)

Returns at once.

```csharp
public override void Wait()
```

</div>
