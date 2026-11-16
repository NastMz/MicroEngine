# Render targets and shaders

The drawing calls of a scene (`IRenderer`) cover sprites, shapes, text and three blend modes. Under them sits the
[graphics device](../manual/rendering.md#the-graphics-device), and a game can use it directly for techniques of its
own: drawing into pictures off screen, combining them with blend states, and drawing through shaders it wrote. The
engine ships none of these effects; this page shows how four are built, each as an example in `samples/Examples`.

| Technique | Built from | Runs on |
| --- | --- | --- |
| [Light that stops at walls](#light-that-stops-at-walls) | render targets, the built-in blend states | every graphics device: SDL (both devices, the software renderer and browsers included) and raylib |
| [Masks](#masks) | render targets, the built-in blend states | every graphics device |
| [Water with a shader of your own](#a-shader-of-your-own-water) | a material: shaders compiled ahead of time, with parameters | the `sdl-gpu` device |
| [Post-processing in several passes](#several-passes-post-processing) | materials and render targets | the `sdl-gpu` device |

Shaders need the programmable device, which a game asks for with `Graphics = new GraphicsSettings { Device = "sdl-gpu" }`
in its settings, or a player with `--graphics sdl-gpu` ([Backends](../manual/backends.md)). A game that wants to run
everywhere keeps a plain path for the other devices, as the examples do; the checks are one line each.

## Reaching the device from a scene

Three objects are enough:

- `Game.Graphics` is a `DeviceGraphics` on the SDL and raylib platforms; its `Device` is the graphics device the game's
  textures live on, and `Device.Capabilities` says what it can do (`Has(GraphicsFeatures.Shaders)`, for example).
- The renderer a scene's `Draw` receives is a `DeviceRenderer` there. `Flush()` sends to the device what the frame drew
  so far, so what you draw next lands after it, and `Target` is the canvas as a render target.
- A `Renderer2D` of your own (`new Renderer2D(device)`) is the 2D batcher: it draws textures and triangles into any
  render target, with any blend state, sampler, transform and, on a device with shaders, material.

The headless platform draws without a graphics device, so `Game.Graphics` is not a `DeviceGraphics` there: check, as
the examples do, and tests of these techniques run in a window (`--hidden` keeps it off the screen).

## Render targets

A render target is a picture on the device that can be drawn into and then drawn like a texture
(`device.CreateRenderTarget(new RenderTargetDescription(width, height))`; its `Color` is the texture). Three rules:

- Everything in a target is premultiplied, like the canvas: a batch's `Clear` and tints are `ColorF` values already
  multiplied by their alpha.
- A pass cannot read the target it draws into. To change the canvas with itself (a distortion, a blur), copy it into
  a target first and draw from the copy.
- The canvas can be replaced between frames (an expanding presentation resizes it): keep the renderer, and read its
  `Target` each frame.

A batch into a target is `Begin(new BatchSettings(target, ...))`, draws, `End()`. `BatchSettings` carries the blend
state, the sampler, a view matrix (a camera's `WorldToView`), a clear colour, a scissor and a viewport. The drawing
calls you already know work on a target too: `new DeviceRenderer(batcher, graphics, target)` is an `IRenderer` that
draws into it between its `Begin()` and `End()`, so sprites, shapes and fonts can go off screen.

## Light that stops at walls

Each light is drawn into a target of its own: its glow, added, then the shadow of every wall, in black, over it. The
lights are added into a light map that starts at the ambient colour, and the light map multiplies the finished picture.
It uses only the four blend states every device has (`Opaque`, `AlphaPremultiplied`, `Additive`, `Multiply`).

<!-- code: samples/Examples/Examples/OccludedLightExample.cs#lightmap -->
```csharp
canvas.Flush();   // the room is in the canvas now; what follows lands after it

_batcher.Begin(new BatchSettings(_lights!, Clear: Ambient));
_batcher.End();
AddLight(_torch, 110, new ColorF(1.0f, 0.85f, 0.6f, 1));
AddLight(new Vector2(280, 150), 80, new ColorF(0.45f, 0.8f, 1.0f, 1));

// The light map multiplies the picture: full light keeps a pixel, the ambient colour darkens it.
_batcher.Begin(new BatchSettings(canvas.Target, Blend: Dev.BlendState.Multiply));
_batcher.Draw(_lights!.Color!, Whole, Matrix3x2.Identity, ColorF.White);
_batcher.End();
```

A shadow is a quad from a wall's edge to far beyond the screen, straight away from the light; only the edges that face
away from the light cast one, so a wall keeps its own light:

<!-- code: samples/Examples/Examples/OccludedLightExample.cs#light -->
```csharp
/// <summary>One light: its glow, then the shadow of every wall in black over it, added to the light map.</summary>
private void AddLight(Vector2 at, float radius, ColorF color)
{
    var glow = ((DeviceTexture)_glow!).Native;
    _batcher!.Begin(new BatchSettings(_one!, Blend: Dev.BlendState.Additive, Sampler: Dev.SamplerDescription.LinearClamp, Clear: ColorF.Black));
    _batcher.Draw(glow, new RectF(0, 0, 64, 64), Matrix3x2.CreateScale(radius / 32) * Matrix3x2.CreateTranslation(at - new Vector2(radius)), color);
    _batcher.State = _batcher.State with { Blend = Dev.BlendState.Opaque };
    foreach (var wall in Walls)
    {
        Vector2 a = new(wall.X, wall.Y), b = new(wall.X + wall.Width, wall.Y), c = new(wall.X + wall.Width, wall.Y + wall.Height), d = new(wall.X, wall.Y + wall.Height);
        Shadow(at, a, b, new Vector2(0, -1));
        Shadow(at, b, c, new Vector2(1, 0));
        Shadow(at, c, d, new Vector2(0, 1));
        Shadow(at, d, a, new Vector2(-1, 0));
    }
    _batcher.End();

    _batcher.Begin(new BatchSettings(_lights!, Blend: Dev.BlendState.Additive));
    _batcher.Draw(_one!.Color!, Whole, Matrix3x2.Identity, ColorF.White);
    _batcher.End();
}

/// <summary>
/// The shadow an edge casts: only edges that face away from the light cast one (so a wall keeps its own light), as
/// a quad from the edge to far beyond the screen, straight away from the light.
/// </summary>
private void Shadow(Vector2 light, Vector2 from, Vector2 to, Vector2 outward)
{
    if (Vector2.Dot(outward, from - light) <= 0) return;
    const float Far = 1000;
    Vector2 farFrom = from + Vector2.Normalize(from - light) * Far, farTo = to + Vector2.Normalize(to - light) * Far;
    _batcher!.DrawGeometry(null,
        [new(from, default, ColorF.Black), new(to, default, ColorF.Black), new(farTo, default, ColorF.Black), new(farFrom, default, ColorF.Black)],
        [0, 1, 2, 0, 2, 3]);
}
```

`((DeviceTexture)texture).Native` is the device texture behind a texture of the drawing API. The walls here are
rectangles; any shape made of edges works the same way, a tile map's solid cells included. The cost is one small target
and two batches per light. In a world with a camera, give each batch the camera's transform
(`View: camera.WorldToView`) and keep the targets the size of the canvas.

## Masks

A picture shown only where a mask allows, with the same four blend states: draw the picture, the mask (white where the
picture shows, black elsewhere) and the mask's opposite into targets; multiply the picture by the mask and the scene by
the opposite; add the two. Grey values in the mask give soft edges.

<!-- code: samples/Examples/Examples/MaskExample.cs#mask -->
```csharp
canvas.Flush();   // the scene is in the canvas now

// The picture: anything the drawing API draws. Here, moving stripes.
_pictureRenderer!.Begin();
_pictureRenderer.Clear(Color.Hex("ef7d57"));
for (int i = -2; i < 12; i++)
    _pictureRenderer.FillRect(new RectF(i * 32 + _time * 24 % 32, 0, 16, 180), Color.Hex(i % 2 == 0 ? "ffcd75" : "a7f070"));
_pictureRenderer.End();

// The mask is white where the picture shows and black elsewhere; the cover is its opposite.
Shapes(_maskRenderer!, Color.Black, Color.White);
Shapes(_coverRenderer!, Color.White, Color.Black);

// Picture × mask: the picture where the mask is white, black elsewhere.
Over(_picture!, _mask!, Dev.BlendState.Multiply);
// Scene × cover: a black hole in the scene where the picture goes. Then the picture is added into the hole.
Over(canvas.Target, _cover!, Dev.BlendState.Multiply);
Over(canvas.Target, _picture!, Dev.BlendState.Additive);
```

<!-- code: samples/Examples/Examples/MaskExample.cs#helpers -->
```csharp
/// <summary>The mask's shapes, text and a circle, drawn like anything else.</summary>
private void Shapes(DeviceRenderer renderer, Color background, Color shape)
{
    renderer.Begin();
    renderer.Clear(background);
    Font.DrawCentered(renderer, "MASK", new Vector2(160, 70), shape, scale: 9);
    renderer.FillCircle(_spot, 26, shape);
    renderer.End();
}

/// <summary>Draws the whole of a target's picture over another target with a blend state.</summary>
private void Over(Dev.RenderTarget target, Dev.RenderTarget source, Dev.BlendState blend)
{
    _batcher!.Begin(new BatchSettings(target, Blend: blend));
    _batcher.Draw(source.Color!, Whole, Matrix3x2.Identity, ColorF.White);
    _batcher.End();
}
```

Where the device declares `GraphicsFeatures.CustomBlend` (`sdl-gpu` always does; SDL's software renderer does not),
one blend state does the same in a single step:
`new BlendState(BlendFactor.Zero, BlendFactor.SrcAlpha, BlendOp.Add, BlendFactor.Zero, BlendFactor.SrcAlpha, BlendOp.Add)`
keeps of a target only what the mask drawn over it covers. A blend state the device does not have is refused with
`GraphicsCapabilityException` when the batch reaches the device (its `End`), so ask `Capabilities.Has` first. There is
no stencil buffer.

## A shader of your own: water

A shader is two HLSL files, one per stage, compiled ahead of time by [`microengine shaders`](cli.md#shaders) from the
game's `Shaders` folder into `Content/shaders`. The vertex stage of a 2D shader is always the same, so a game has one
and shares it:

<!-- code: samples/Examples/Shaders/sprite.vert.hlsl -->
```hlsl
// The vertex stage every 2D shader of the examples shares: the batcher's vertices (position in target pixels, texture
// coordinate, premultiplied colour) and one matrix that takes pixels to clip space.
// SDL_GPU binding rules (vertex stage): uniform buffers in space1; inputs use TEXCOORD<location>.
cbuffer Projection : register(b0, space1)
{
    float4x4 PixelsToClip;
};

struct Input
{
    float2 Position : TEXCOORD0;
    float2 TexCoord : TEXCOORD1;
    float4 Color : TEXCOORD2;
};

struct Output
{
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
    float4 Position : SV_Position;
};

Output main(Input input)
{
    Output output;
    output.TexCoord = input.TexCoord;
    output.Color = input.Color;
    output.Position = mul(PixelsToClip, float4(input.Position, 0.0, 1.0));
    return output;
}
```

The fragment stage is the technique. This one mirrors the picture above a water line and pushes it sideways with a
wave that travels with time:

<!-- code: samples/Examples/Shaders/water.frag.hlsl -->
```hlsl
// Water: the picture above the surface, mirrored about it and pushed sideways by a sine wave that travels with time,
// then mixed with the water's colour. The quad covers the water; its texture coordinates are its own place in the
// picture, so Surface (the v coordinate of the water line) is all the shader needs to find the mirror image.
// SDL_GPU binding rules (fragment stage): textures and samplers in space2, uniform buffers in space3.
Texture2D<float4> Scene : register(t0, space2);
SamplerState SceneSampler : register(s0, space2);

cbuffer Water : register(b0, space3)
{
    float4 Tint;      // the water's colour; its alpha is how much of it covers the reflection
    float Time;       // seconds
    float Amplitude;  // sideways push, in texture coordinates
    float Frequency;  // waves per unit of v
    float Speed;      // radians per second
    float Surface;    // v of the water line
    float3 Unused;
};

struct Input
{
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

float4 main(Input input) : SV_Target0
{
    float depth = input.TexCoord.y - Surface;
    float wave = sin(depth * Frequency * 6.2831853 + Time * Speed) * Amplitude * saturate(depth * 8.0);
    float4 mirrored = Scene.Sample(SceneSampler, float2(input.TexCoord.x + wave, Surface - depth));
    return float4(lerp(mirrored.rgb, Tint.rgb, Tint.a), 1.0) * input.Color;
}
```

In the game, the two compiled files become shaders on the device, and the pair becomes a `Material2D`:

<!-- code: samples/Examples/Examples/WaterExample.cs#setup -->
```csharp
public override void Enter()
{
    // Shaders are an optional feature of a graphics device: ask, and keep a plain path for devices without them.
    if (Game.Graphics is not DeviceGraphics graphics || !graphics.Device.Capabilities.Has(Dev.GraphicsFeatures.Shaders)) return;
    var device = graphics.Device;
    _batcher = new Renderer2D(device);
    // Compiled ahead of time by `microengine shaders` from Shaders/*.hlsl into Content/shaders.
    _vertex = Dev.CompiledShader.Parse(Assets.Bytes("shaders/sprite.vert.shader")).Create(device);
    _fragment = Dev.CompiledShader.Parse(Assets.Bytes("shaders/water.frag.shader")).Create(device);
    _water = new Material2D(_vertex, _fragment);
    _above = device.CreateRenderTarget(new Dev.RenderTargetDescription(320, 180));
}

public override void Exit()
{
    _above?.Dispose();
    _water?.Dispose();
    _fragment?.Dispose();
    _vertex?.Dispose();
    _batcher?.Dispose();
}
```

The shader's parameters are a struct with the layout of its `cbuffer`: the same fields in the same order, with the
block's padding (a `float3` or `float4` never straddles a 16-byte boundary in HLSL, so pad to keep fields aligned).

<!-- code: samples/Examples/Examples/WaterExample.cs#parameters -->
```csharp
/// <summary>The shader's parameters, laid out as its <c>cbuffer Water</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
private struct WaterParameters
{
    public ColorF Tint;
    public float Time, Amplitude, Frequency, Speed;
    public float Surface;
    public Vector3 Unused;
}
```

A batch with the material draws everything through the shaders: sprites and geometry, in any transform, with the
batch's blend state, sampler and scissor.

<!-- code: samples/Examples/Examples/WaterExample.cs#draw -->
```csharp
canvas.Flush();   // what was drawn so far is in the canvas
// A pass cannot read the target it draws into: copy the picture, then draw the water from the copy.
_batcher!.Begin(new BatchSettings(_above!, Blend: Dev.BlendState.Opaque));
_batcher.Draw(canvas.Target.Color!, Whole, Matrix3x2.Identity, ColorF.White);
_batcher.End();

_water.SetUniforms(new WaterParameters
{
    Tint = new ColorF(0.16f, 0.30f, 0.62f, 0.45f),
    Time = _time, Amplitude = _amplitude, Frequency = 9, Speed = 3,
    Surface = Surface / 180,
});
// The water's part of the copy, drawn in its own place through the material.
_batcher.Begin(new BatchSettings(canvas.Target, Blend: Dev.BlendState.Opaque) { Material = _water });
_batcher.Draw(_above!.Color!, Water, Matrix3x2.CreateTranslation(0, Surface), ColorF.White);
_batcher.End();
```

### What a material's shaders receive

| Input | What it is |
| --- | --- |
| Vertices | `Vertex2D`: position in target pixels relative to the viewport (`TEXCOORD0`), texture coordinate (`TEXCOORD1`), premultiplied colour (`TEXCOORD2`) |
| Vertex uniform slot 0 | The matrix that takes those pixels to clip space, when the vertex shader declares a uniform block |
| Fragment texture slot 0 | The texture of each draw, with the state's sampler |
| Fragment texture slots 1 and up | The textures given to `material.SetTexture(slot, texture, sampler)` |
| Fragment uniform slots | The data given to `material.SetUniforms(data, slot)` |

- A material is part of the batcher's state: `BatchSettings.Material` for a whole batch, or
  `batcher.PushState(batcher.State with { Material = material })` and `PopState()` around some draws. Draws with and
  without a material mix in one batch and land in the order they were recorded.
- A draw keeps the parameters the material had when the draw was recorded: set them, draw, set them again, draw, and
  each draw has its own. Draws with the same texture, state and parameters share one draw call.
- A draw with a material whose shader declares a uniform block or a texture that was never set is refused at the draw
  (`InvalidOperationException` naming the slot); a slot the shader does not declare is refused when it is set.
- The material creates what the device needs for each blend state and target format it is drawn with, and releases it
  when disposed. It does not own the shaders: dispose them yourself, after the material.
- The drawing calls can go through a material too. `IRenderer` over a batcher of your own keeps the batcher's
  material, so sprites, atlases and fonts are drawn by your shader:

```csharp
var effects = new DeviceRenderer(batcher, graphics, canvas.Target);   // once
canvas.Flush();
effects.Begin();
batcher.State = batcher.State with { Material = material };
effects.Draw(sprite, position);          // any IRenderer call
effects.End();
```

## Several passes: post-processing

A pass is one draw of a whole picture into another target through a material. A chain is passes one after another,
each reading the target the one before wrote. The example blurs the bright parts of the finished picture at half size
and adds them back, with darker corners and scanlines:

<!-- code: samples/Examples/Examples/PostProcessExample.cs#chain -->
```csharp
canvas.Flush();
// 1. The finished picture, copied where the passes can read it.
Pass(canvas.Target.Color!, _scene!, null);
// 2. Its bright parts blurred across, at half size. 3. That, blurred down.
_blur.SetUniforms(new BlurParameters { Step = new Vector2(1f / 160, 0), Threshold = 0.55f });
Pass(_scene!.Color!, _across!, _blur);
_blur.SetUniforms(new BlurParameters { Step = new Vector2(0, 1f / 90) });
Pass(_across!.Color!, _glow!, _blur);
// 4. The picture and the glow together, back into the canvas.
_look!.SetUniforms(new LookParameters { GlowStrength = 1.4f, Vignette = 0.9f, Scanlines = 0.18f, Lines = 180 });
_look.SetTexture(1, _glow!.Color!, Dev.SamplerDescription.LinearClamp);
Pass(_scene.Color!, canvas.Target, _look);
```

<!-- code: samples/Examples/Examples/PostProcessExample.cs#pass -->
```csharp
/// <summary>One pass: the whole of <paramref name="source"/> drawn over the whole of <paramref name="target"/>, through a material or as it is.</summary>
private void Pass(Dev.Texture source, Dev.RenderTarget target, Material2D? material)
{
    _batcher!.Begin(new BatchSettings(target, Blend: Dev.BlendState.Opaque, Sampler: Dev.SamplerDescription.LinearClamp) { Material = material });
    _batcher.Draw(source, new RectF(0, 0, source.Width, source.Height),
        Matrix3x2.CreateScale(target.Width / (float)source.Width, target.Height / (float)source.Height), ColorF.White);
    _batcher.End();
}
```

The last pass reads two textures: the picture, which is the draw's texture, and the glow, which the material holds in
slot 1.

A scene's passes cover what that scene drew. For passes over the whole frame, every scene and the transition between
them, override `Game.DrawAboveScenes`: it runs once the scenes are drawn, with the same renderer.

```csharp
protected override void DrawAboveScenes(IRenderer renderer)
{
    if (renderer is not DeviceRenderer canvas || _look is null) return;
    canvas.Flush();
    // ... the same passes, from canvas.Target and back into it
}
```

The passes change the canvas, so captures (`--captures`) show them, and the interface a scene draws in `DrawUi` stays
sharp over them at the window's resolution.

## Limits

- Shaders run on the `sdl-gpu` device: Direct3D 12 or Vulkan on Windows, Vulkan on Linux (the Metal shaders are
  generated but have not been run on a Mac). The default SDL device, the software renderer, raylib and browser builds
  have none; there, `CompiledShader.Create` throws `GraphicsCapabilityException`, and a game checks
  `Capabilities.Has(GraphicsFeatures.Shaders)` to keep a plain path. Render targets and the four blend states work on
  all of them, browsers included.
- Compiling shaders needs Windows ([the tool's page](cli.md#shaders) says why); the compiled files load everywhere.
- A parameter block is matched to its shader by layout, not by name: a struct that does not follow the `cbuffer`
  gives wrong values, not an error.
- A pass has one colour target; there is no stencil buffer, no multisampling and no mipmap generation.
- The passes run at the canvas's resolution. The window's own composition (the canvas scaled into the window) has no
  shader stage.
