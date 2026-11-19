# Textures made ahead of time

A game loads its images as they are: `Assets.Texture("hero.png")` decodes the file and uploads its pixels, four bytes
each, on every device ([Assets](../manual/assets.md)). That is the default and stays enough for most games. A game with
a lot of high-resolution art can also convert images ahead of time into **texture files**: KTX 2.0 files (`.ktx2`, the
Khronos texture container) that hold a texture as the graphics device takes it. They are optional, the game can ship
them beside its images, and nothing in the engine requires them.

A texture file gives two things an image cannot:

| | An image file | A texture file of pixels (`rgba`) | A texture file of BC7 blocks (`bc7`) |
| --- | --- | --- | --- |
| At load | decoded, then uploaded | inflated, then uploaded | inflated, then uploaded |
| On the device | 4 bytes a pixel | 4 bytes a pixel | 1 byte a pixel |
| A 2048 × 2048 page | 16 MB | 16 MB | 4 MB |
| The picture | exact | exact | close to the image, not equal |
| Drawn by | every device | every device | devices that sample BC7 |

Reach for them when a level's art takes long to load or takes more graphics memory than the machines you target have;
a game of pixel art, or of a few HD pages, gains nothing it would notice.

## Converting

[`microengine textures`](cli.md#textures) converts every image under a folder:

```bash
microengine textures                                                    # Textures/ to Content/textures/, as BC7
microengine textures --source Content/pictures --out Content/textures   # images the game ships too
microengine textures --format rgba                                      # the image's own pixels, for every device
```

Each `name.png` (or JPEG, BMP, TGA, GIF, PSD) becomes `name.ktx2` at the same place under the output folder, with its
colour already multiplied by its alpha, as the device draws it, and its bytes deflated. Commit the files or make them
in your build; a file is converted again only when its image is newer or another format is asked for.

- **`bc7`** stores 4 × 4 blocks of pixels in 16 bytes each. The picture is close to the image, not equal: smooth
  areas and flat colour keep within a level or two of 255, and the largest differences fall on hard edges between a
  dark outline and transparency. The image's width and height must be multiples of 4. Encoding is slow: tens of
  seconds for a 2048 × 2048 page and a minute or more for a 4096 × 4096 one on a desktop processor. `--quality fast`
  about halves that for detailed art, at a small cost in fidelity; `balanced` is the default and `best` searches
  longer.
- **`rgba`** stores the image's pixels exactly, any size. It saves no memory; it saves the decoding.

## Loading and drawing

`TextureFile.Parse` reads the file and `Create` makes the texture on the graphics device. A device that does not
sample the file's format refuses with `GraphicsCapabilityException`, as for any capability it does not declare, and
`CanCreate` asks first, so a game that ships the image too draws that instead (`Dev` is the namespace
`MicroEngine.Graphics.Devices`):

<!-- code: samples/Examples/Examples/TextureFileExample.cs#load -->
```csharp
private Renderer2D? _batcher;
private Dev.Texture? _ready;   // the .ktx2 file, on a device that samples its format
private Texture? _image;       // the image file, everywhere else

public override void Enter()
{
    if (Game.Graphics is DeviceGraphics graphics)
    {
        var file = Dev.TextureFile.Parse(Assets.Bytes("textures/emblem.ktx2"));
        if (file.CanCreate(graphics.Device.Capabilities))
        {
            _ready = file.Create(graphics.Device);   // the file's bytes go to the device as they are
            _batcher = new Renderer2D(graphics.Device);
        }
    }
    if (_ready is null) _image = Game.Graphics.CreateTexture(Assets.Image("pictures/emblem.png"), TextureFilter.Linear);
}

public override void Exit()   // the scene made both textures, so it releases them
{
    _batcher?.Dispose();
    _ready?.Dispose();
    _image?.Dispose();
}
```

The texture is a texture of the [graphics device](../manual/rendering.md#the-graphics-device), drawn with a 2D batcher
of your own (`Renderer2D`) into the canvas, like the render targets of
[Render targets and shaders](render-targets-and-shaders.md#reaching-the-device-from-a-scene): `Flush()` puts what
the scene drew so far into the canvas, and the batch lands over it.

<!-- code: samples/Examples/Examples/TextureFileExample.cs#draw -->
```csharp
if (r is DeviceRenderer canvas && _ready is not null)
{
    canvas.Flush();   // the background is in the canvas now; the texture lands over it
    _batcher!.Begin(new BatchSettings(canvas.Target, Sampler: Dev.SamplerDescription.LinearClamp));
    _batcher.Draw(_ready, new RectF(0, 0, _ready.Width, _ready.Height), Matrix3x2.CreateScale(0.5f) * Matrix3x2.CreateTranslation(Place), ColorF.White);
    _batcher.End();
}
else r.Draw(_image!, _image!.Bounds, Place, Color.White, new DrawOptions(Scale: new Vector2(0.5f)));
```

The batcher draws any part of the texture (`source`), with any transform, tint, blend state and sampler, so the frames
of an atlas stored as a texture file are drawn from their rectangles (`AtlasData` reads the packing tool's file).
Dispose the texture when its scene ends: it belongs to the code that created it, not to `Assets`.

## Which device draws what

| Device | `rgba` files | `bc7` files |
| --- | --- | --- |
| SDL's default device (`sdl-renderer`), browsers included | yes | no |
| `sdl-gpu` on Direct3D 12 or Vulkan with a desktop graphics card | yes | yes |
| `sdl-gpu` on other adapters (phones, some integrated or software adapters) | yes | where `Capabilities.Supports(TextureFormat.Bc7, TextureUsage.Sampled)` says so |
| raylib | yes | no |
| The headless platform | no graphics device: draw the image |

`TextureFile.CanCreate(device.Capabilities)` is the one check a game needs. A game for desktop graphics cards that
asks for `sdl-gpu` can ship BC7 files alone; a game that also runs on the default device or in browsers ships its
images and uses the files where they are drawn.

## Files from other tools

A texture file is an ordinary KTX 2.0 file. `TextureFile.Parse` reads KTX 2.0 within these bounds, whichever tool
wrote it: one 2D texture; RGBA8, BC7, ETC2 RGBA8 or ASTC 4 × 4; levels stored plainly or deflated (ZLIB). A format tagged sRGB is read as its plain format: the bytes are used as an image
file's are. `TextureFile.Alpha` says whether the file declares its colour multiplied by alpha; pixels that are not are
multiplied when the texture is created, and blocks are drawn as they are stored, so convert those with premultiplied
alpha.

## Limits

- Texture files are drawn through `Renderer2D`. The drawing calls of a scene (`IRenderer.Draw`), `SpriteAtlas` and
  `Assets.Texture` take images.
- One level is uploaded. A file may hold mip levels; no device draws with mipmaps, so art drawn at less than half its
  size still shimmers, whatever its file.
- Files compressed with Zstandard and Basis Universal files (BasisLZ, UASTC) are refused with a message that says so.
- BC7 is the block format `microengine textures` writes. ETC2 and ASTC files from other tools are read; no SDL device
  samples ETC2, and ASTC is sampled where the adapter declares it.
- A BC7 file whose width or height is not a multiple of 4 is refused by Direct3D 12; the converter refuses to write
  one.
