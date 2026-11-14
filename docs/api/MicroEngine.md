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

 [MicroEngine.Tiled](MicroEngine.Tiled.md)

 [MicroEngine.UI](MicroEngine.UI.md)

 [MicroEngine.World](MicroEngine.World.md)

### Classes

 [Ease](MicroEngine.Ease.md)

Standard easing curves mapping 0..1 to 0..1 (some overshoot).

 [MathX](MicroEngine.MathX.md)

Small numeric helpers that games repeatedly need for feel and motion.

 [Track<T\>](MicroEngine.Track\-1.md)

An immutable value that changes over time through keys: `At` gives the value at any time, blended
between the two keys around it. It holds no playhead, so one track can be shared by everything that moves the same
way; a <code>Timeline</code> plays tracks, or call `At` with a time of your own.

 [Track](MicroEngine.Track.md)

Creates the usual kinds of `Track`1`.

### Structs

 [ColorF](MicroEngine.ColorF.md)

A floating-point RGBA colour in linear light, for blending, lighting and anything that adds or scales colours.
Channels are not clamped (values above 1 are brighter than white). [Color](MicroEngine.Color.md) is the 8-bit sRGB form used
to store and name colours; convert with [FromSrgb](MicroEngine.ColorF.md#MicroEngine_ColorF_FromSrgb_MicroEngine_Color_) and [ToSrgb](MicroEngine.ColorF.md#MicroEngine_ColorF_ToSrgb).

 [Color](MicroEngine.Color.md)

An 8-bit-per-channel, non-premultiplied RGBA color.

 [Keyframe<T\>](MicroEngine.Keyframe\-1.md)

One key of a `Track`1`: the value the track has at a time.

 [RectF](MicroEngine.RectF.md)

Axis-aligned rectangle in world or screen units (Y grows downward).

</div>
