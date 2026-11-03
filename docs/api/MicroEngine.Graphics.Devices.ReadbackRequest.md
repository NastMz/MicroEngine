# Class ReadbackRequest

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

An asynchronous read of a render target region, completed by a fence. The data are the pixels of every command
list submitted before [ReadPixels](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_ReadPixels_MicroEngine_Graphics_Devices_RenderTarget_MicroEngine_Graphics_Devices_RectI_): rows top to bottom, tightly packed, in the target's
colour format.

```csharp
public abstract class ReadbackRequest
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ReadbackRequest](MicroEngine.Graphics.Devices.ReadbackRequest.md)

#### Derived

[CompletedReadback](MicroEngine.Graphics.Devices.CompletedReadback.md)

## Constructors

### <a id="MicroEngine_Graphics_Devices_ReadbackRequest__ctor_MicroEngine_Graphics_Devices_RectI_MicroEngine_Graphics_Devices_TextureFormat_"></a> ReadbackRequest\(RectI, TextureFormat\)

Creates the request.

```csharp
protected ReadbackRequest(RectI region, TextureFormat format)
```

#### Parameters

`region` [RectI](MicroEngine.Graphics.Devices.RectI.md)

`format` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

## Properties

### <a id="MicroEngine_Graphics_Devices_ReadbackRequest_Format"></a> Format

The pixel format of [Pixels](MicroEngine.Graphics.Devices.ReadbackRequest.md#MicroEngine_Graphics_Devices_ReadbackRequest_Pixels).

```csharp
public TextureFormat Format { get; }
```

#### Property Value

 [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

### <a id="MicroEngine_Graphics_Devices_ReadbackRequest_IsCompleted"></a> IsCompleted

True once the GPU finished the copy (polls the fence; never blocks).

```csharp
public abstract bool IsCompleted { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_ReadbackRequest_Pixels"></a> Pixels

The pixels; `InvalidOperationException` before [IsCompleted](MicroEngine.Graphics.Devices.ReadbackRequest.md#MicroEngine_Graphics_Devices_ReadbackRequest_IsCompleted).

```csharp
public ReadOnlySpan<byte> Pixels { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

### <a id="MicroEngine_Graphics_Devices_ReadbackRequest_Region"></a> Region

The region read.

```csharp
public RectI Region { get; }
```

#### Property Value

 [RectI](MicroEngine.Graphics.Devices.RectI.md)

## Methods

### <a id="MicroEngine_Graphics_Devices_ReadbackRequest_Collect"></a> Collect\(\)

Backend: copies the completed data out and releases the staging memory (called once).

```csharp
protected abstract byte[] Collect()
```

#### Returns

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

### <a id="MicroEngine_Graphics_Devices_ReadbackRequest_Wait"></a> Wait\(\)

Blocks until the copy finished.

```csharp
public abstract void Wait()
```

</div>
