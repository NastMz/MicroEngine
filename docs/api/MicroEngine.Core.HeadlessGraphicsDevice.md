# Class HeadlessGraphicsDevice

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

A graphics device for tests: textures keep their size but hold no pixels, and creations are counted.

```csharp
public sealed class HeadlessGraphicsDevice : IGraphicsDevice
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[HeadlessGraphicsDevice](MicroEngine.Core.HeadlessGraphicsDevice.md)

#### Implements

[IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

## Constructors

### <a id="MicroEngine_Core_HeadlessGraphicsDevice__ctor"></a> HeadlessGraphicsDevice\(\)

```csharp
public HeadlessGraphicsDevice()
```

## Properties

### <a id="MicroEngine_Core_HeadlessGraphicsDevice_TexturesCreated"></a> TexturesCreated

Number of textures created so far.

```csharp
public int TexturesCreated { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Core_HeadlessGraphicsDevice_CreateTexture_MicroEngine_Graphics_PixelImage_"></a> CreateTexture\(PixelImage\)

Uploads <code class="paramref">image</code> as a texture. The caller owns the texture and disposes it.

```csharp
public Texture CreateTexture(PixelImage image)
```

#### Parameters

`image` [PixelImage](MicroEngine.Graphics.PixelImage.md)

#### Returns

 [Texture](MicroEngine.Graphics.Texture.md)
