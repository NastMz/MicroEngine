# Namespace MicroEngine

<div data-pagefind-weight="0.3">

Basic value types: colours, rectangles, maths and easing helpers.

Guide: [Graphics](../manual/rendering.md)  

### Namespaces

 [MicroEngine.Achievements](MicroEngine.Achievements.md)

 [MicroEngine.Audio](MicroEngine.Audio.md)

 [MicroEngine.Content](MicroEngine.Content.md)

 [MicroEngine.Core](MicroEngine.Core.md)

 [MicroEngine.Desktop](MicroEngine.Desktop.md)

 [MicroEngine.Graphics](MicroEngine.Graphics.md)

 [MicroEngine.Hosting](MicroEngine.Hosting.md)

 [MicroEngine.Input](MicroEngine.Input.md)

 [MicroEngine.Kernel](MicroEngine.Kernel.md)

 [MicroEngine.Physics](MicroEngine.Physics.md)

 [MicroEngine.Platform](MicroEngine.Platform.md)

 [MicroEngine.Raylib](MicroEngine.Raylib.md)

 [MicroEngine.Saves](MicroEngine.Saves.md)

 [MicroEngine.Scenes](MicroEngine.Scenes.md)

 [MicroEngine.Sdl](MicroEngine.Sdl.md)

 [MicroEngine.UI](MicroEngine.UI.md)

 [MicroEngine.World](MicroEngine.World.md)

### Classes

 [Ease](MicroEngine.Ease.md)

Standard easing curves mapping 0..1 to 0..1 (some overshoot).

 [MathX](MicroEngine.MathX.md)

Small numeric helpers that games repeatedly need for feel and motion.

### Structs

 [ColorF](MicroEngine.ColorF.md)

A floating-point RGBA colour in linear light, for blending, lighting and anything that adds or scales colours.
Channels are not clamped (values above 1 are brighter than white). [Color](MicroEngine.Color.md) is the 8-bit sRGB form used
to store and name colours; convert with [FromSrgb](MicroEngine.ColorF.md#MicroEngine_ColorF_FromSrgb_MicroEngine_Color_) and [ToSrgb](MicroEngine.ColorF.md#MicroEngine_ColorF_ToSrgb).

 [Color](MicroEngine.Color.md)

An 8-bit-per-channel, non-premultiplied RGBA color.

 [RectF](MicroEngine.RectF.md)

Axis-aligned rectangle in world or screen units (Y grows downward).

</div>
