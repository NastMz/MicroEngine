# Enum TextureFilter

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

How a texture is sampled when it is drawn bigger or smaller than its pixels, or between them.

```csharp
public enum TextureFilter
```

## Fields

`Linear = 1` 

The four texels around each pixel, blended: smooth, high-resolution art and text.



`LinearMipmap = 2` 

[Linear](MicroEngine.Graphics.TextureFilter.md), with mip levels: the texture also keeps smaller copies of itself (each half the size of the
one before, a third more memory in all) and a draw that makes it smaller than its pixels samples the copies
nearest to that size, blended. Art drawn well below its size (a zoomed-out camera, a large picture on a small
canvas) then neither shimmers when it moves nor loses thin details. The levels are made when the texture is
created with this filter ([MipLevels](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_MipLevels)), on a graphics device with mip levels: <code>sdl-gpu</code>
and raylib on desktop. On every other device (SDL's default device and its software renderer, browser builds,
the headless platform) the texture has one level and is sampled as [Linear](MicroEngine.Graphics.TextureFilter.md).



`Nearest = 0` 

The texel under each pixel: hard pixel edges (pixel art).

</div>
