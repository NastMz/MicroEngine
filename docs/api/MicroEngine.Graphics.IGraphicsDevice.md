# Interface IGraphicsDevice

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

Creates device resources. Separated so headless tests can provide fakes.

```csharp
public interface IGraphicsDevice
```

## Methods

### <a id="MicroEngine_Graphics_IGraphicsDevice_CreateTexture_MicroEngine_Graphics_PixelImage_"></a> CreateTexture\(PixelImage\)

Uploads <code class="paramref">image</code> as a texture. The caller owns the texture and disposes it.

```csharp
Texture CreateTexture(PixelImage image)
```

#### Parameters

`image` [PixelImage](MicroEngine.Graphics.PixelImage.md)

#### Returns

 [Texture](MicroEngine.Graphics.Texture.md)
