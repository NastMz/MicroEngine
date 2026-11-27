# Namespace MicroEngine.Aseprite

<div data-pagefind-weight="0.3">

Aseprite files as sprite atlases (optional package): layers flattened, tags as clips with their durations and direction, slices as sprites, through the game's assets.

Guide: [Aseprite files](../manual/assets.md#aseprite-files)  

### Classes

 [AsepriteAssets](MicroEngine.Aseprite.AsepriteAssets.md)

Aseprite files (<code>.aseprite</code>, <code>.ase</code>) as sprite sheets, read as the artist saved them: no export step. Each
frame is the picture Aseprite shows (visible layers flattened with their blend modes and opacity), named
"<code>file</code> 0", "<code>file</code> 1"... like Aseprite's own sheet export; each frame tag is a clip with every frame's
duration, its direction and whether it repeats; each slice is a sprite of its own, named after it, and data of the
frames it covers: a box of its name ([Box](MicroEngine.Graphics.Sprite.md#MicroEngine_Graphics_Sprite_Box_System_String_System_Boolean_)) and, when it has a pivot, a point of its name
([Point](MicroEngine.Graphics.Sprite.md#MicroEngine_Graphics_Sprite_Point_System_String_System_Boolean_)), each following the slice's keys from frame to frame.

</div>
