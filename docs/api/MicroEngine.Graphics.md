# Namespace MicroEngine.Graphics

Drawing: the renderer contract, images in code and from PNG, sprites and atlases, animation, camera, parallax, particles, light, the pixel font.

Guide: [Graphics](../manual/rendering.md)  

### Classes

 [AnimationClip](MicroEngine.Graphics.AnimationClip.md)

An immutable sequence of sprites played at a fixed rate.

 [Animator](MicroEngine.Graphics.Animator.md)

Plays named clips; switching to the clip already playing does not restart it.

 [Art](MicroEngine.Graphics.Art.md)

Deterministic helpers for procedural pixel art: hash noise, ordered dithering and palette
ramps. Every sample game builds its sprites and tiles from these at load time.

 [BitmapFont](MicroEngine.Graphics.BitmapFont.md)

A proportional pixel font. The built-in face ("Micro 5x7") is authored in code so every game
has crisp, license-free text without loading files. Glyph widths are derived by trimming
empty columns, so narrow letters like 'i' take less space.

 [Camera2D](MicroEngine.Graphics.Camera2D.md)

A 2D camera whose [Position](MicroEngine.Graphics.Camera2D.md#MicroEngine_Graphics_Camera2D_Position) is the world point at the top-left of the view.
Supports smooth following with a dead zone, world bounds and trauma-based shake.

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
[CreateTexture](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_CreateTexture_MicroEngine_Graphics_PixelImage_); it also backs the built-in font.

 [PngEncoder](MicroEngine.Graphics.PngEncoder.md)

Writes 8-bit RGBA PNG files (no filtering), for backends that read frames back as raw pixels.

 [RendererExtensions](MicroEngine.Graphics.RendererExtensions.md)

Drawing helpers built on [IRenderer](MicroEngine.Graphics.IRenderer.md): sprites, outlines, ellipses, gradients and panels.

 [Sprite](MicroEngine.Graphics.Sprite.md)

A region of a texture with a pivot (origin) in source pixels.

 [SpriteAtlas](MicroEngine.Graphics.SpriteAtlas.md)

Named sprites packed into one texture, built by [Build](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_Build_MicroEngine_Graphics_IGraphicsDevice_). The atlas owns the texture:
dispose the atlas (usually in <code>Game.Unload</code>) when its sprites are no longer drawn.

 [SpriteAtlasBuilder](MicroEngine.Graphics.SpriteAtlasBuilder.md)

Packs many procedurally authored [PixelImage](MicroEngine.Graphics.PixelImage.md)s into one texture and
exposes them by name. Keeps draw calls batched and art code independent of layout.

 [Texture](MicroEngine.Graphics.Texture.md)

A GPU (or headless) texture owned by the active graphics device.

### Structs

 [DrawOptions](MicroEngine.Graphics.DrawOptions.md)

Per-draw sprite options. Origin is in source pixels and is also the rotation pivot.

 [PanelStyle](MicroEngine.Graphics.PanelStyle.md)

Colours of a panel drawn with [Panel](MicroEngine.Graphics.RendererExtensions.md#MicroEngine_Graphics_RendererExtensions_Panel_MicroEngine_Graphics_IRenderer_MicroEngine_RectF_MicroEngine_Graphics_PanelStyle_).

### Interfaces

 [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

Creates device resources. Separated so headless tests can provide fakes.

 [IRenderer](MicroEngine.Graphics.IRenderer.md)

Immediate-mode 2D drawing in virtual pixels. Coordinates go through the camera
pushed with [PushCamera](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushCamera_MicroEngine_Graphics_Camera2D_); with no camera they are screen space.

### Enums

 [BlendMode](MicroEngine.Graphics.BlendMode.md)

How drawn pixels combine with the canvas (see [SetBlendMode](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_SetBlendMode_MicroEngine_Graphics_BlendMode_)): <code>Alpha</code> (normal
transparency, the default), <code>Additive</code> (adds light: glows, sparks, fire) and <code>Multiply</code> (darkens: shadows, tints).
