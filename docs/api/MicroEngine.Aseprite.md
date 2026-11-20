# Namespace MicroEngine.Aseprite

<div data-pagefind-weight="0.3">

Aseprite files as sprite atlases (optional package): layers flattened, tags as clips with their durations and direction, slices as sprites, through the game's assets.

Guide: [Aseprite files](../manual/assets.md#aseprite-files)  

### Classes

 [AsepriteAssets](MicroEngine.Aseprite.AsepriteAssets.md)

Aseprite files (<code>.aseprite</code>, <code>.ase</code>) as sprite sheets, read as the artist saved them: no export step. Each
frame is the picture Aseprite shows (visible layers flattened with their blend modes and opacity), named
"<code>file</code> 0", "<code>file</code> 1"... like Aseprite's own sheet export; each frame tag is a clip with every frame's
duration, its direction and whether it repeats; each slice is a sprite of its own, named after it.

</div>
