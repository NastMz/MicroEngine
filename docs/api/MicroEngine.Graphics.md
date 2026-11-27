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
frame; frames the JSON says are rotated are marked ([Rotated](MicroEngine.Graphics.AtlasRegion.md#MicroEngine_Graphics_AtlasRegion_Rotated)) and turned upright when the
atlas is loaded with its image; rotated frames of the XML format are refused (turn rotation off in the tool).
Aseprite's slices (<code>meta.slices</code>) become data of the frames: each slice a box of its name on the frames it
covers, and its pivot a point of the same name.

 [AtlasOptions](MicroEngine.Graphics.AtlasOptions.md)

How atlas files are read.

 [BitmapFont](MicroEngine.Graphics.BitmapFont.md)

A proportional pixel font. The built-in face ("Micro 5x7") is authored in code so every game
has crisp, license-free text without loading files. Glyph widths are derived by trimming
empty columns, so narrow letters like 'i' take less space.

 [Bloom](MicroEngine.Graphics.Bloom.md)

Bloom: the bright parts of the picture, blurred, added back over it, so light sources glow.
[Full](MicroEngine.Graphics.PostEffectSupport.md) where shaders run (what is brighter than [Threshold](MicroEngine.Graphics.Bloom.md#MicroEngine_Graphics_Bloom_Threshold), with a
Gaussian blur). Elsewhere ([Reduced](MicroEngine.Graphics.PostEffectSupport.md)) the bright parts are picked by multiplying the
picture by itself, without an exact threshold, and blurred by halving: a softer, wider glow.

 [Blur](MicroEngine.Graphics.Blur.md)

Blurs the picture. [Full](MicroEngine.Graphics.PostEffectSupport.md) where shaders run: a Gaussian blur, across then down.
Elsewhere ([Reduced](MicroEngine.Graphics.PostEffectSupport.md)) the picture is shrunk by halves with linear sampling and
enlarged again, which blurs by about the same radius with a boxier look.

 [Camera2D](MicroEngine.Graphics.Camera2D.md)

A 2D camera whose [Position](MicroEngine.Graphics.Camera2D.md#MicroEngine_Graphics_Camera2D_Position) is the world point at the top-left of the view.
Supports smooth following with a dead zone, world bounds and trauma-based shake.

 [CanvasPresenter](MicroEngine.Graphics.CanvasPresenter.md)

Draws a game's canvas into the window as its [Presentation](MicroEngine.Graphics.Presentation.md) says, prescale included
([Prescale](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Prescale)): the window cleared to the letterbox colour, the canvas into its frame, and the
UI view over both. With a prescale the canvas is first enlarged by a whole factor with nearest sampling into a target
this presenter owns, and that enlargement is what the presentation's upscale sampler samples. Platforms built on a
graphics device present their frames with it.

 [ColorGrade](MicroEngine.Graphics.ColorGrade.md)

Colour grading: brightness, contrast around mid grey, saturation and a tint, applied in that order.
[Full](MicroEngine.Graphics.PostEffectSupport.md) where shaders run. Elsewhere contrast and saturation are left out
([Reduced](MicroEngine.Graphics.PostEffectSupport.md)): the tint and the brightness still apply, made with blending.

 [Crt](MicroEngine.Graphics.Crt.md)

The picture as a cathode-ray tube shows it: the glass bulges ([Curvature](MicroEngine.Graphics.Crt.md#MicroEngine_Graphics_Crt_Curvature)), the lines of the picture
show ([Scanlines](MicroEngine.Graphics.Crt.md#MicroEngine_Graphics_Crt_Scanlines)) and so do the phosphor's coloured stripes ([Mask](MicroEngine.Graphics.Crt.md#MicroEngine_Graphics_Crt_Mask)).
[Full](MicroEngine.Graphics.PostEffectSupport.md) where shaders run. Elsewhere only the lines are drawn
([Reduced](MicroEngine.Graphics.PostEffectSupport.md)): the picture stays flat and has no stripes.

 [DeviceCanvas](MicroEngine.Graphics.DeviceCanvas.md)

A game's canvas on a graphics device, as backends built on the device contract use it: the canvas render target, the
textures ([Graphics](MicroEngine.Graphics.DeviceCanvas.md#MicroEngine_Graphics_DeviceCanvas_Graphics)) and the drawing API ([Renderer](MicroEngine.Graphics.DeviceCanvas.md#MicroEngine_Graphics_DeviceCanvas_Renderer)) over the 2D batcher, the composition of
the canvas into the window as the presentation says, and captures read back from the canvas.

 [DeviceGraphics](MicroEngine.Graphics.DeviceGraphics.md)

The textures, render targets and materials of the drawing API ([IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)) made on a graphics
device of the device contract: each texture is a device texture whose straight-alpha pixels are premultiplied when
they are uploaded, and its [Filter](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Filter) chooses the sampler it is drawn with. A device texture or
render target the game made itself becomes a texture of the drawing API with [Wrap](MicroEngine.Graphics.DeviceGraphics.md#MicroEngine_Graphics_DeviceGraphics_Wrap_MicroEngine_Graphics_Devices_Texture_System_Nullable_MicroEngine_Graphics_TextureFilter__System_Boolean_).
Backends built on a device use it as their [Graphics](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_Graphics).

 [DeviceRenderer](MicroEngine.Graphics.DeviceRenderer.md)

The drawing API ([IRenderer](MicroEngine.Graphics.IRenderer.md)) as a façade over the 2D batcher ([Renderer2D](MicroEngine.Graphics.Renderer2D.md)) on a graphics
device: every call is recorded into the batcher, and the frame reaches the device in as few draw calls as the draws'
textures and states allow. Straight-alpha colours are premultiplied on the way in, so the blend modes keep their
written equations (see [SetBlendMode](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_SetBlendMode_MicroEngine_Graphics_BlendMode_)). States and materials ([PushState](MicroEngine.Graphics.DeviceRenderer.md#MicroEngine_Graphics_DeviceRenderer_PushState_MicroEngine_Graphics_DrawState__)) become
the batcher's state, and a pushed render target ([PushTarget](MicroEngine.Graphics.DeviceRenderer.md#MicroEngine_Graphics_DeviceRenderer_PushTarget_MicroEngine_Graphics_RenderTarget2D_)) becomes the batch's target, so a frame
that uses them is still one batch in recording order.

 [DeviceTexture](MicroEngine.Graphics.DeviceTexture.md)

A texture of the drawing API backed by a device texture ([DeviceGraphics](MicroEngine.Graphics.DeviceGraphics.md)): an uploaded image, a render
target's picture, or a device texture the game wrapped.

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

 [Material2D](MicroEngine.Graphics.Material2D.md)

A fragment shader of the game's own with its parameters, that draws go through instead of the built-in pipeline:
push it with a state ([PushState](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushState_MicroEngine_Graphics_DrawState__), [Material](MicroEngine.Graphics.DrawState.md#MicroEngine_Graphics_DrawState_Material)) and every draw made until
the pop (sprites, shapes, text, in any camera, with the state's blend and sampler) goes through the shader. A game
gets one from its assets (<code>Assets.Material("shaders/water.frag.shader")</code>) or from its graphics device
([CreateMaterial](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_CreateMaterial_MicroEngine_Graphics_Devices_CompiledShader_MicroEngine_Graphics_MaterialFallback_)); the engine supplies the vertex stage
([SpriteVertexShader](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_SpriteVertexShader)), so a material needs only the fragment shader, compiled ahead of time by
<code>microengine shaders</code>.

 [PackedAtlas](MicroEngine.Graphics.PackedAtlas.md)

Sprites packed into one picture by [Pack](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_Pack_System_Boolean_System_Boolean_), without a graphics device: the picture,
where each sprite lies in it, and the clips. [ToJson](MicroEngine.Graphics.PackedAtlas.md#MicroEngine_Graphics_PackedAtlas_ToJson_System_String_) writes the description packing tools write, so the
pair (a PNG and a JSON file) is an ordinary atlas for <code>Assets.Atlas</code>. The frames' points and boxes are written
with each frame and read back.

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

 [PostEffect](MicroEngine.Graphics.PostEffect.md)

One full-screen effect of a [PostProcess](MicroEngine.Graphics.PostProcess.md) chain: it receives the picture so far and leaves a new one.
The ready effects are [ColorGrade](MicroEngine.Graphics.ColorGrade.md), [Blur](MicroEngine.Graphics.Blur.md), [Bloom](MicroEngine.Graphics.Bloom.md), [Vignette](MicroEngine.Graphics.Vignette.md),
[Scanlines](MicroEngine.Graphics.Scanlines.md), [Crt](MicroEngine.Graphics.Crt.md) and [ShaderEffect](MicroEngine.Graphics.ShaderEffect.md) (a material of the game's own); a game
derives its own from this class. Pictures are premultiplied, like the canvas, and an effect keeps their alpha
unless changing it is its purpose.

 [PostPass](MicroEngine.Graphics.PostPass.md)

What an effect works with while it runs ([Apply](MicroEngine.Graphics.PostEffect.md#MicroEngine_Graphics_PostEffect_Apply_MicroEngine_Graphics_PostPass_)): the picture so far, the renderer, and the
chain's own targets and textures, which the chain keeps between frames and releases itself.

 [PostProcess](MicroEngine.Graphics.PostProcess.md)

A chain of full-screen effects over a picture: what was drawn between [Begin](MicroEngine.Graphics.PostProcess.md#MicroEngine_Graphics_PostProcess_Begin_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_IGraphicsDevice_System_Nullable_MicroEngine_Color__) and [End](MicroEngine.Graphics.PostProcess.md#MicroEngine_Graphics_PostProcess_End_MicroEngine_Graphics_IRenderer_System_Boolean_)
(or a picture given to [Apply](MicroEngine.Graphics.PostProcess.md#MicroEngine_Graphics_PostProcess_Apply_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_IGraphicsDevice_MicroEngine_Graphics_Texture_MicroEngine_RectF_System_Boolean_)) goes through every enabled effect in order, and the result is drawn
where the picture would have been. The chain owns the render targets, textures and built-in materials its effects
need, sized to the picture, and releases them with [Dispose](MicroEngine.Graphics.PostProcess.md#MicroEngine_Graphics_PostProcess_Dispose). One chain can run on pictures of
different sizes, in one frame too: it keeps what it made for each size, and releases what no run has used for
[KeptForRuns](MicroEngine.Graphics.PostProcess.md#MicroEngine_Graphics_PostProcess_KeptForRuns) runs (so a chain may run up to that many times in one frame). A scene has one for its own picture
(<code>Scene.PostProcess</code>), and a game has one for the canvas and one for the window (<code>Game.PostProcess</code>,
<code>Game.ScreenPostProcess</code>); the engine begins, ends and releases those.

 [Presentation](MicroEngine.Graphics.Presentation.md)

How the game's canvas reaches the window, one independent choice per field: how its scale is rounded
([Rounding](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Rounding)), what happens to the window's other shape ([Aspect](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Aspect)), how it is sampled when it is
scaled ([Upscale](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Upscale)), the whole factor it is enlarged by first ([Prescale](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Prescale)), the sampler textures get when they choose none ([DefaultSampler](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_DefaultSampler)), the size
factor of captures ([CaptureScale](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_CaptureScale)), whether desktop windows open at whole factors
([WholeFactorWindows](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_WholeFactorWindows)) and whether 2D cameras snap to whole pixels by default
([CameraPixelSnap](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_CameraPixelSnap)). [PixelArt](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_PixelArt) and [HighResolution2D](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_HighResolution2D) are presets: start from
one and change any field with <code>with</code>.

 [RenderTarget2D](MicroEngine.Graphics.RenderTarget2D.md)

A picture off screen that a renderer draws into ([PushTarget](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushTarget_MicroEngine_Graphics_RenderTarget2D_)) and that is then drawn like any
texture: [Texture](MicroEngine.Graphics.RenderTarget2D.md#MicroEngine_Graphics_RenderTarget2D_Texture) is a texture of the drawing API, so it works with every <code>Draw</code> overload, as the
source of a [Sprite](MicroEngine.Graphics.Sprite.md) and as a material's texture. Create one with
[CreateRenderTarget](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_CreateRenderTarget_System_Int32_System_Int32_MicroEngine_Graphics_TextureFilter_), or through a game's assets
(<code>Assets.RenderTarget</code>), which release it with their scope. A target keeps its pixels between frames until it
is drawn into again; they are premultiplied, like the canvas.

 [Renderer2D](MicroEngine.Graphics.Renderer2D.md)

The engine's 2D batcher ([IRenderer2D](MicroEngine.Graphics.IRenderer2D.md)) over one graphics device.

 [RendererExtensions](MicroEngine.Graphics.RendererExtensions.md)

Drawing helpers built on [IRenderer](MicroEngine.Graphics.IRenderer.md): sprites, outlines, ellipses, gradients and panels.

 [ScalableFont](MicroEngine.Graphics.ScalableFont.md)

A TrueType or OpenType font (TTF, OTF with TrueType or CFF outlines) that draws text at any size. Glyphs are
rasterized the first time they are drawn at a size and kept in shared glyph pages (textures), so later frames only
draw quads; [Prepare](MicroEngine.Graphics.ScalableFont.md#MicroEngine_Graphics_ScalableFont_Prepare_System_String_System_Single___) rasterizes ahead, so the first frames of play do no font work.

 [Scanlines](MicroEngine.Graphics.Scanlines.md)

Darkens every other line of the picture, as a television's lines show. Made of a texture and the multiply blend
state, so it is [Full](MicroEngine.Graphics.PostEffectSupport.md) and the same on every device, and it keeps the picture's transparency.

 [ShaderEffect](MicroEngine.Graphics.ShaderEffect.md)

A full-screen pass through a material of the game's own (a fragment shader compiled by <code>microengine shaders</code>,
loaded with <code>Assets.Material</code>): the picture is the draw's texture, in slot 0, and the shader's output is the
new picture. [Full](MicroEngine.Graphics.PostEffectSupport.md) where shaders run; [None](MicroEngine.Graphics.PostEffectSupport.md) elsewhere,
where the picture passes through unchanged (the material's own fallback is not used). The game owns the material.

 [ShapedFontSize](MicroEngine.Graphics.ShapedFontSize.md)

A [ShapedFont](MicroEngine.Graphics.ShapedFont.md) at one size: an [IFont](MicroEngine.Graphics.IFont.md) for widgets, layout and drawing.

 [ShapedFont](MicroEngine.Graphics.ShapedFont.md)

A TrueType or OpenType font that shapes its text: letters take the forms their neighbours call for (Arabic's joined
forms, Indic conjuncts, ligatures), marks sit where the font places them, and right-to-left scripts (Arabic, Hebrew)
run right to left. HarfBuzz chooses the glyphs and their positions; each glyph is rasterized the first time it is
drawn at a size and kept in glyph pages (textures), so later frames only draw quads.

 [SheetClip](MicroEngine.Graphics.SheetClip.md)

A named animation of a sprite sheet: frames by name, how long each shows, and how they play.

 [SheetFrame](MicroEngine.Graphics.SheetFrame.md)

One frame of a sprite sheet as pixels, before it is packed into an atlas.

 [SizedFont](MicroEngine.Graphics.SizedFont.md)

A [ScalableFont](MicroEngine.Graphics.ScalableFont.md) at one size: an [IFont](MicroEngine.Graphics.IFont.md) for widgets, layout and drawing.

 [SortedRenderer](MicroEngine.Graphics.SortedRenderer.md)

Draws in an order that does not depend on the order of the calls: between [Begin](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_Begin_MicroEngine_Graphics_IRenderer_) and [End](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_End)
every draw is kept with the [Layer](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_Layer) and [Depth](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_Depth) of that moment, and [End](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_End) sends
them to the renderer by layer (lower first), then by depth (smaller first, so a larger depth covers), then in the
order they were made. It is a renderer itself, so sprites, fonts, tiles and particles draw into it as into any other,
on every platform. Objects standing on the ground cover each other correctly with their feet's Y as the depth.

 [SpriteAtlasBuilder](MicroEngine.Graphics.SpriteAtlasBuilder.md)

Packs many procedurally authored [PixelImage](MicroEngine.Graphics.PixelImage.md)s into one texture and
exposes them by name. Keeps draw calls batched and art code independent of layout.

 [SpriteAtlas](MicroEngine.Graphics.SpriteAtlas.md)

Named sprites packed into one texture, built by [Build](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_Build_MicroEngine_Graphics_IGraphicsDevice_System_Nullable_MicroEngine_Graphics_TextureFilter__). The atlas owns the texture:
dispose the atlas (usually in <code>Game.Unload</code>) when its sprites are no longer drawn.

 [SpriteEffects](MicroEngine.Graphics.SpriteEffects.md)

One-shot sprite animations: "play this clip once here, after a delay, mirrored, tinted, then go away". Impacts,
puffs of smoke, sparkles and explosions are each one call, with no object to keep and nothing to remove. A scene
has one in <code>Scene.Effects</code>, stepped and cleared with the scene; a game whose rules live outside a scene makes
its own and calls [Update](MicroEngine.Graphics.SpriteEffects.md#MicroEngine_Graphics_SpriteEffects_Update_System_Single_).

 [SpriteSheet](MicroEngine.Graphics.SpriteSheet.md)

Frames and clips read from a description of sprite sheets, as pixels: what an artist's tool or a <code>.sheet</code> file
says about one or more images (cell size, named strips, pivots, animation timing), ready to be packed into a
[SpriteAtlas](MicroEngine.Graphics.SpriteAtlas.md) with [AddSheet](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_AddSheet_MicroEngine_Graphics_SpriteSheet_). <code>Assets.Atlas("art/enemies.sheet")</code>
does both steps; <code>Assets.Sheet(path)</code> gives the frames to change or to pack with others.

 [Sprite](MicroEngine.Graphics.Sprite.md)

A region of a texture with a pivot (origin) in source pixels.

 [Texture](MicroEngine.Graphics.Texture.md)

A texture of the drawing API, owned by the game's graphics device: a picture [IRenderer](MicroEngine.Graphics.IRenderer.md) draws, with the
filter it is sampled with. On a platform with a graphics device it is a view of a device texture
([Native](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Native)), whatever made it: an image, a font page, a render target or a texture the game created on the
device itself. The headless platform keeps textures without a device.

 [Vignette](MicroEngine.Graphics.Vignette.md)

Darkens the picture towards its corners (or multiplies it towards [Color](MicroEngine.Graphics.Vignette.md#MicroEngine_Graphics_Vignette_Color)). Made of a texture and the
multiply blend state, so it is [Full](MicroEngine.Graphics.PostEffectSupport.md) and the same on every device, and it keeps the
picture's transparency.

### Structs

 [AtlasRegion](MicroEngine.Graphics.AtlasRegion.md)

One named rectangle of an atlas image, as a packing tool described it.

 [AtlasTrim](MicroEngine.Graphics.AtlasTrim.md)

What trimming cut from a sprite: where the kept pixels lay in the whole image, and both sizes.

 [BatchSettings](MicroEngine.Graphics.BatchSettings.md)

The settings of one batch, from [Begin](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_Begin_MicroEngine_Graphics_BatchSettings__) to [End](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_End).

 [CachedText<TKey\>](MicroEngine.Graphics.CachedText\-1.md)

A text built from a value and built again only when the value changes: for text that cannot be written as an
interpolated string where it is drawn (a localized message with a number in it, a wrapped paragraph, a text a
widget keeps). While the value stays the same, [Get](MicroEngine.Graphics.md) returns the same string and allocates nothing; a
change costs one call of the format.

 [DrawOptions](MicroEngine.Graphics.DrawOptions.md)

Per-draw sprite options. Origin is in source pixels and is also the rotation pivot.

 [DrawState](MicroEngine.Graphics.DrawState.md)

What a renderer draws with between [PushState](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushState_MicroEngine_Graphics_DrawState__) and [PopState](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PopState): a blend
state, a sampler and a material. A part left null keeps what the renderer was drawing with, so a state names only
what it changes.

 [InterpolatedText](MicroEngine.Graphics.InterpolatedText.md)

Text built from an interpolated string without making a string: where a font, a sprite atlas or another engine call
takes an [InterpolatedText](MicroEngine.Graphics.InterpolatedText.md), <code>$"{hp}/{max}"</code> is written into a buffer the engine reuses, drawn or
looked up, and the buffer goes back, so a number shown every frame makes no garbage.

 [MaterialFallback](MicroEngine.Graphics.MaterialFallback.md)

What a material's draws become on a graphics device that cannot run its shader (one without
[Shaders](MicroEngine.Graphics.Devices.GraphicsFeatures.md): SDL's default device and its software renderer, raylib, browser builds and
the headless platform). There, every draw made under the material is drawn the usual way, without the shader, with
this fallback applied; the game's code is the same on every device.

 [NineSlice](MicroEngine.Graphics.NineSlice.md)

An image that stretches to any size without distorting its frame: the part of [Source](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Source) within
[Left](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Left), [Top](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Top), [Right](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Right) and [Bottom](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Bottom) pixels of its sides is the
border, drawn at its size at the corners and stretched along the edges; the rest is the centre, stretched to fill.
Draw one with [DrawNineSlice](MicroEngine.Graphics.RendererExtensions.md#MicroEngine_Graphics_RendererExtensions_DrawNineSlice_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_NineSlice_MicroEngine_RectF_System_Nullable_MicroEngine_Color__), or set it as a [Image](MicroEngine.Graphics.PanelStyle.md#MicroEngine_Graphics_PanelStyle_Image).

 [PanelStyle](MicroEngine.Graphics.PanelStyle.md)

The look of a panel drawn with [Panel](MicroEngine.Graphics.RendererExtensions.md#MicroEngine_Graphics_RendererExtensions_Panel_MicroEngine_Graphics_IRenderer_MicroEngine_RectF_MicroEngine_Graphics_PanelStyle_): a box of colours, or an image of the game's
own ([Image](MicroEngine.Graphics.PanelStyle.md#MicroEngine_Graphics_PanelStyle_Image)).

 [RenderState](MicroEngine.Graphics.RenderState.md)

The per-draw state of the 2D batcher ([State](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_State)): blend, sampler, scissor and material.

 [Renderer2DStatistics](MicroEngine.Graphics.Renderer2DStatistics.md)

Deterministic counts of the last batch ([Statistics](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_Statistics)), the same on every device and run.

 [ShapedGlyph](MicroEngine.Graphics.ShapedGlyph.md)

One glyph of a shaped line.

 [SpriteBox](MicroEngine.Graphics.SpriteBox.md)

A named rectangle of a frame: a hit box, a hurt box, the part that is solid. Written in a <code>.sheet</code> file
(<code>"boxes": { "hit": [10, 2, 6, 4] }</code>) or drawn in Aseprite as a slice.

 [SpriteEffect](MicroEngine.Graphics.SpriteEffect.md)

An effect played by [Play](MicroEngine.Graphics.SpriteEffects.md#MicroEngine_Graphics_SpriteEffects_Play_MicroEngine_Graphics_AnimationClip_System_Numerics_Vector2_System_Single_System_Boolean_System_Nullable_MicroEngine_Color__System_Nullable_System_Single__System_Int32_System_Single_): asks whether it still plays, moves it or stops it. The default value is no effect.

 [SpritePoint](MicroEngine.Graphics.SpritePoint.md)

A named point of a frame: a shoulder, a gun tip, where a hat sits. Written in a <code>.sheet</code> file
(<code>"points": { "muzzle": [14, 3] }</code>) or drawn in Aseprite as the pivot of a slice.

### Interfaces

 [IFont](MicroEngine.Graphics.IFont.md)

Anything that measures and draws text at one size: the built-in [BitmapFont](MicroEngine.Graphics.BitmapFont.md) (at scale 1) and a
[ScalableFont](MicroEngine.Graphics.ScalableFont.md) at a size ([At](MicroEngine.Graphics.ScalableFont.md#MicroEngine_Graphics_ScalableFont_At_System_Single_)). UI widgets and layout code take an
[IFont](MicroEngine.Graphics.IFont.md), so the same code serves pixel-art and HD text.

 [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

Creates device resources. Separated so headless tests can provide fakes.

 [IRenderer2D](MicroEngine.Graphics.IRenderer2D.md)

The 2D renderer over the graphics device, implemented once in core: it writes vertices and indices into device
buffers and draws them with the built-in [Textured](MicroEngine.Graphics.Devices.BuiltInPipeline.md) pipeline, so it runs unchanged on
every tier, or, for the draws recorded with a [Material](MicroEngine.Graphics.RenderState.md#MicroEngine_Graphics_RenderState_Material), with that material's shaders on a
device that has custom shaders. Colours (tints, vertex colours) are premultiplied, and the default blend state is
premultiplied alpha.

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

 [PostEffectSupport](MicroEngine.Graphics.PostEffectSupport.md)

What a post-processing effect does on a graphics device ([SupportOn](MicroEngine.Graphics.PostEffect.md#MicroEngine_Graphics_PostEffect_SupportOn_MicroEngine_Graphics_IGraphicsDevice_)).

 [ScaleRounding](MicroEngine.Graphics.ScaleRounding.md)

How the canvas's scale to the window is rounded ([Rounding](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Rounding)).

 [SortMode](MicroEngine.Graphics.SortMode.md)

The order in which a batch's draws reach the device ([Sort](MicroEngine.Graphics.BatchSettings.md#MicroEngine_Graphics_BatchSettings_Sort)).

 [SpriteFlip](MicroEngine.Graphics.SpriteFlip.md)

Mirrors a sprite's texels within its quad ([Draw](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_Draw_MicroEngine_Graphics_Devices_Texture_MicroEngine_RectF_System_Numerics_Matrix3x2__MicroEngine_ColorF_System_Single_MicroEngine_Graphics_SpriteFlip_)).

 [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

How a texture is sampled when it is drawn bigger or smaller than its pixels, or between them.

</div>
