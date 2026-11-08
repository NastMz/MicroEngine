# Namespace MicroEngine.Graphics

<div data-pagefind-weight="0.3">

Drawing: the renderer contract, images in code and from PNG, sprites and atlases, animation, camera, parallax, particles, light, the pixel font.

Guide: [Graphics](../manual/rendering.md)  

### Namespaces

 [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)

### Classes

 [AnimationClip](MicroEngine.Graphics.AnimationClip.md)

An immutable sequence of sprites played at a fixed rate, or with a duration per frame (Aseprite timing).

 [Animator](MicroEngine.Graphics.Animator.md)

Plays named clips; switching to the clip already playing does not restart it.

 [Art](MicroEngine.Graphics.Art.md)

Deterministic helpers for procedural pixel art: hash noise, ordered dithering and palette
ramps. Every sample game builds its sprites and tiles from these at load time.

 [AtlasAnimation](MicroEngine.Graphics.AtlasAnimation.md)

A named run of frames (an Aseprite frame tag).

 [AtlasData](MicroEngine.Graphics.AtlasData.md)

The description of an atlas image written by a packing tool, read without any graphics device: the JSON of
TexturePacker, Free Texture Packer and Aseprite (hash or array of frames; Aseprite's frame durations and tags), and the
Starling/Sparrow XML of TexturePacker, ShoeBox and Kenney's packs. Trimmed frames keep their place in the untrimmed
frame; rotated frames are refused (turn rotation off in the tool).

 [AtlasOptions](MicroEngine.Graphics.AtlasOptions.md)

How atlas files are read.

 [BitmapFont](MicroEngine.Graphics.BitmapFont.md)

A proportional pixel font. The built-in face ("Micro 5x7") is authored in code so every game
has crisp, license-free text without loading files. Glyph widths are derived by trimming
empty columns, so narrow letters like 'i' take less space.

 [Camera2D](MicroEngine.Graphics.Camera2D.md)

A 2D camera whose [Position](MicroEngine.Graphics.Camera2D.md#MicroEngine_Graphics_Camera2D_Position) is the world point at the top-left of the view.
Supports smooth following with a dead zone, world bounds and trauma-based shake.

 [DeviceCanvas](MicroEngine.Graphics.DeviceCanvas.md)

A game's canvas on a graphics device, as backends built on the device contract use it: the canvas render target, the
textures ([Graphics](MicroEngine.Graphics.DeviceCanvas.md#MicroEngine_Graphics_DeviceCanvas_Graphics)) and the drawing API ([Renderer](MicroEngine.Graphics.DeviceCanvas.md#MicroEngine_Graphics_DeviceCanvas_Renderer)) over the 2D batcher, the composition of
the canvas into the window as the presentation says, and captures read back from the canvas.

 [DeviceGraphics](MicroEngine.Graphics.DeviceGraphics.md)

The textures of the drawing API ([IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)) made on a graphics device of the device contract: each
texture is a device texture whose straight-alpha pixels are premultiplied when they are uploaded, and its
[Filter](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Filter) chooses the sampler it is drawn with. Backends built on a device use it as their
[Graphics](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_Graphics).

 [DeviceRenderer](MicroEngine.Graphics.DeviceRenderer.md)

The drawing API ([IRenderer](MicroEngine.Graphics.IRenderer.md)) as a façade over the 2D batcher ([Renderer2D](MicroEngine.Graphics.Renderer2D.md)) on a graphics
device: every call is recorded into the batcher, and the frame reaches the device in as few draw calls as the draws'
textures and states allow. Straight-alpha colours are premultiplied on the way in, so the blend modes keep their
written equations (see [SetBlendMode](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_SetBlendMode_MicroEngine_Graphics_BlendMode_)).

 [DeviceTexture](MicroEngine.Graphics.DeviceTexture.md)

A texture of the drawing API backed by a device texture ([DeviceGraphics](MicroEngine.Graphics.DeviceGraphics.md)).

 [FontExtensions](MicroEngine.Graphics.FontExtensions.md)

Layout and drawing helpers that work with any [IFont](MicroEngine.Graphics.IFont.md).

 [FontOptions](MicroEngine.Graphics.FontOptions.md)

How a [ScalableFont](MicroEngine.Graphics.ScalableFont.md) turns outlines into pixels.

 [LightMap](MicroEngine.Graphics.LightMap.md)

Darkness with point lights for caves, dungeons and night scenes. Each frame the game clears the
map, adds lights in world space and draws it over the finished scene:

<ol><li>a coarse brightness grid ([CellSize](MicroEngine.Graphics.LightMap.md#MicroEngine_Graphics_LightMap_CellSize) pixels, quantised to [Levels](MicroEngine.Graphics.LightMap.md#MicroEngine_Graphics_LightMap_Levels) bands)
is multiplied over the screen, toward [Shade](MicroEngine.Graphics.LightMap.md#MicroEngine_Graphics_LightMap_Shade) where it is dark;</li><li>lights with a glow colour add a soft radial halo on top.</li></ol>

Rows of equal cells merge into single rectangles, so a full screen costs a few hundred quads.
With [Darkness](MicroEngine.Graphics.LightMap.md#MicroEngine_Graphics_LightMap_Darkness) 1 and lights of brightness 0 it becomes a flat colour grade plus glows.

 [ParallaxLayer](MicroEngine.Graphics.ParallaxLayer.md)

A background image that scrolls slower (or faster) than the camera and repeats horizontally.
Draw it in screen space (no camera pushed). [Factor](MicroEngine.Graphics.ParallaxLayer.md#MicroEngine_Graphics_ParallaxLayer_Factor) 0 is fixed to the screen, 1 moves
with the world. [Y](MicroEngine.Graphics.ParallaxLayer.md#MicroEngine_Graphics_ParallaxLayer_Y) is the screen row of the image's top edge; the optional
[VerticalFactor](MicroEngine.Graphics.ParallaxLayer.md#MicroEngine_Graphics_ParallaxLayer_VerticalFactor) lets tall worlds shift it as the camera rises or falls.

 [ParticleSpec](MicroEngine.Graphics.ParticleSpec.md)

How a burst of particles looks and moves. Ranges are (min, max).

 [ParticleSystem](MicroEngine.Graphics.ParticleSystem.md)

A pooled particle system for dust, sparks, confetti, smoke and the like.

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

A CPU-side RGBA image. Games author procedural art with it and hand it to
[CreateTexture](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_CreateTexture_MicroEngine_Graphics_PixelImage_MicroEngine_Graphics_TextureFilter_); it also backs the built-in font.

 [PngEncoder](MicroEngine.Graphics.PngEncoder.md)

Writes 8-bit RGBA PNG files (no filtering), for backends that read frames back as raw pixels.

 [Presentation](MicroEngine.Graphics.Presentation.md)

How the game's canvas reaches the window, one independent choice per field: how its scale is rounded
([Rounding](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Rounding)), what happens to the window's other shape ([Aspect](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Aspect)), how it is sampled when it is
scaled ([Upscale](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Upscale)), the sampler textures get when they choose none ([DefaultSampler](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_DefaultSampler)), the size
factor of captures ([CaptureScale](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_CaptureScale)), whether desktop windows open at whole factors
([WholeFactorWindows](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_WholeFactorWindows)) and whether 2D cameras snap to whole pixels by default
([CameraPixelSnap](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_CameraPixelSnap)). [PixelArt](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_PixelArt) and [HighResolution2D](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_HighResolution2D) are presets: start from
one and change any field with <code>with</code>.

 [Renderer2D](MicroEngine.Graphics.Renderer2D.md)

The engine's 2D batcher ([IRenderer2D](MicroEngine.Graphics.IRenderer2D.md)) over one graphics device.

 [RendererExtensions](MicroEngine.Graphics.RendererExtensions.md)

Drawing helpers built on [IRenderer](MicroEngine.Graphics.IRenderer.md): sprites, outlines, ellipses, gradients and panels.

 [ScalableFont](MicroEngine.Graphics.ScalableFont.md)

A TrueType or OpenType font (TTF, OTF with TrueType or CFF outlines) that draws text at any size. Glyphs are
rasterized the first time they are drawn at a size and kept in shared glyph pages (textures), so later frames only
draw quads; [Prepare](MicroEngine.Graphics.ScalableFont.md#MicroEngine_Graphics_ScalableFont_Prepare_System_String_System_Single___) rasterizes ahead, so the first frames of play do no font work.

 [ShapedFontSize](MicroEngine.Graphics.ShapedFontSize.md)

A [ShapedFont](MicroEngine.Graphics.ShapedFont.md) at one size: an [IFont](MicroEngine.Graphics.IFont.md) for widgets, layout and drawing.

 [ShapedFont](MicroEngine.Graphics.ShapedFont.md)

A TrueType or OpenType font that shapes its text: letters take the forms their neighbours call for (Arabic's joined
forms, Indic conjuncts, ligatures), marks sit where the font places them, and right-to-left scripts (Arabic, Hebrew)
run right to left. HarfBuzz chooses the glyphs and their positions; each glyph is rasterized the first time it is
drawn at a size and kept in glyph pages (textures), so later frames only draw quads.

 [SizedFont](MicroEngine.Graphics.SizedFont.md)

A [ScalableFont](MicroEngine.Graphics.ScalableFont.md) at one size: an [IFont](MicroEngine.Graphics.IFont.md) for widgets, layout and drawing.

 [SpriteAtlasBuilder](MicroEngine.Graphics.SpriteAtlasBuilder.md)

Packs many procedurally authored [PixelImage](MicroEngine.Graphics.PixelImage.md)s into one texture and
exposes them by name. Keeps draw calls batched and art code independent of layout.

 [SpriteAtlas](MicroEngine.Graphics.SpriteAtlas.md)

Named sprites packed into one texture, built by [Build](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_Build_MicroEngine_Graphics_IGraphicsDevice_System_Nullable_MicroEngine_Graphics_TextureFilter__). The atlas owns the texture:
dispose the atlas (usually in <code>Game.Unload</code>) when its sprites are no longer drawn.

 [Sprite](MicroEngine.Graphics.Sprite.md)

A region of a texture with a pivot (origin) in source pixels.

 [Texture](MicroEngine.Graphics.Texture.md)

A GPU (or headless) texture owned by the active graphics device.

### Structs

 [AtlasRegion](MicroEngine.Graphics.AtlasRegion.md)

One named rectangle of an atlas image, as a packing tool described it.

 [BatchSettings](MicroEngine.Graphics.BatchSettings.md)

The settings of one batch, from [Begin](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_Begin_MicroEngine_Graphics_BatchSettings__) to [End](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_End).

 [DrawOptions](MicroEngine.Graphics.DrawOptions.md)

Per-draw sprite options. Origin is in source pixels and is also the rotation pivot.

 [NineSlice](MicroEngine.Graphics.NineSlice.md)

An image that stretches to any size without distorting its frame: the part of [Source](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Source) within
[Left](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Left), [Top](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Top), [Right](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Right) and [Bottom](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Bottom) pixels of its sides is the
border, drawn at its size at the corners and stretched along the edges; the rest is the centre, stretched to fill.
Draw one with [DrawNineSlice](MicroEngine.Graphics.RendererExtensions.md#MicroEngine_Graphics_RendererExtensions_DrawNineSlice_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_NineSlice_MicroEngine_RectF_System_Nullable_MicroEngine_Color__), or set it as a [Image](MicroEngine.Graphics.PanelStyle.md#MicroEngine_Graphics_PanelStyle_Image).

 [PanelStyle](MicroEngine.Graphics.PanelStyle.md)

The look of a panel drawn with [Panel](MicroEngine.Graphics.RendererExtensions.md#MicroEngine_Graphics_RendererExtensions_Panel_MicroEngine_Graphics_IRenderer_MicroEngine_RectF_MicroEngine_Graphics_PanelStyle_): a box of colours, or an image of the game's
own ([Image](MicroEngine.Graphics.PanelStyle.md#MicroEngine_Graphics_PanelStyle_Image)).

 [RenderState](MicroEngine.Graphics.RenderState.md)

The per-draw state of the 2D batcher ([State](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_State)): blend, sampler and scissor.

 [Renderer2DStatistics](MicroEngine.Graphics.Renderer2DStatistics.md)

Deterministic counts of the last batch ([Statistics](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_Statistics)), the same on every device and run.

 [ShapedGlyph](MicroEngine.Graphics.ShapedGlyph.md)

One glyph of a shaped line.

### Interfaces

 [IFont](MicroEngine.Graphics.IFont.md)

Anything that measures and draws text at one size: the built-in [BitmapFont](MicroEngine.Graphics.BitmapFont.md) (at scale 1) and a
[ScalableFont](MicroEngine.Graphics.ScalableFont.md) at a size ([At](MicroEngine.Graphics.ScalableFont.md#MicroEngine_Graphics_ScalableFont_At_System_Single_)). UI widgets and layout code take an
[IFont](MicroEngine.Graphics.IFont.md), so the same code serves pixel-art and HD text.

 [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

Creates device resources. Separated so headless tests can provide fakes.

 [IRenderer2D](MicroEngine.Graphics.IRenderer2D.md)

The 2D renderer over the graphics device, implemented once in core: it writes vertices and indices into device
buffers and draws them with the built-in [Textured](MicroEngine.Graphics.Devices.BuiltInPipeline.md) pipeline only, so it runs unchanged
on every tier. Colours (tints, vertex colours) are premultiplied, and the default blend state is premultiplied alpha.

 [IRenderer](MicroEngine.Graphics.IRenderer.md)

Immediate-mode 2D drawing in virtual pixels. Coordinates go through the camera
pushed with [PushCamera](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushCamera_MicroEngine_Graphics_Camera2D_); with no camera they are screen space.

### Enums

 [AnimationDirection](MicroEngine.Graphics.AnimationDirection.md)

The order an animation plays its frames in (Aseprite's tag directions).

 [AspectMode](MicroEngine.Graphics.AspectMode.md)

What happens when the window's shape differs from the canvas's ([Aspect](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Aspect)).

 [BlendMode](MicroEngine.Graphics.BlendMode.md)

How drawn pixels combine with the canvas (see [SetBlendMode](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_SetBlendMode_MicroEngine_Graphics_BlendMode_)): <code>Alpha</code> (normal
transparency, the default), <code>Additive</code> (adds light: glows, sparks, fire) and <code>Multiply</code> (darkens: shadows, tints).

 [ScaleRounding](MicroEngine.Graphics.ScaleRounding.md)

How the canvas's scale to the window is rounded ([Rounding](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Rounding)).

 [SortMode](MicroEngine.Graphics.SortMode.md)

The order in which a batch's draws reach the device ([Sort](MicroEngine.Graphics.BatchSettings.md#MicroEngine_Graphics_BatchSettings_Sort)).

 [SpriteFlip](MicroEngine.Graphics.SpriteFlip.md)

Mirrors a sprite's texels within its quad ([Draw](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_Draw_MicroEngine_Graphics_Devices_Texture_MicroEngine_RectF_System_Numerics_Matrix3x2__MicroEngine_ColorF_System_Single_MicroEngine_Graphics_SpriteFlip_)).

 [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

How a texture is sampled when it is drawn bigger or smaller than its pixels, or between them: <code>Nearest</code> keeps
hard pixel edges (pixel art), <code>Linear</code> blends neighbouring pixels (smooth, high-resolution art and text).

</div>
