# Render targets and shaders

The drawing calls of a scene (`IRenderer`) cover sprites, shapes and text. The same renderer also draws into pictures
off screen, with any blend state, and through shaders the game wrote: three additions to the calls you already use,
with no second renderer to keep. This page shows how three techniques are built from them, and how a chain of
full-screen effects (ready ones, or your own) is put on a scene, the canvas or the window; each is an example in
`samples/Examples`.

| Technique | Built from | Shows on |
| --- | --- | --- |
| [Light that stops at walls](#light-that-stops-at-walls) | render targets, the built-in blend states | every platform |
| [Masks](#masks) | render targets, the built-in blend states | every platform |
| [Water with a shader of your own](#a-shader-of-your-own-water) | a material: a fragment shader compiled ahead of time, with named parameters | the `sdl-gpu` device; elsewhere the material's fallback |
| [Post-processing](#post-processing) | a chain of effects: ready ones, a shader of your own, an effect of your own | every platform; each effect says [what it draws without shaders](#what-each-effect-draws-without-shaders) |

Shaders run on the programmable device, which a game asks for with `Graphics = new GraphicsSettings { Device = "sdl-gpu" }`
in its settings, or a player with `--graphics sdl-gpu` ([Backends](../manual/backends.md)). The game's code does not
change with the device: a material [declares what it draws where shaders cannot run](#where-shaders-cannot-run).

## The three calls

| Call | What it does |
| --- | --- |
| `renderer.PushTarget(target)` … `PopTarget()` | Draws into a render target instead of the canvas |
| `renderer.PushState(state)` … `PopState()` | Draws with a blend state, a sampler and a material |
| `Assets.RenderTarget(name, width, height)`, `Assets.Material(path)` | Create a target and a material that an asset scope owns |

A `DrawState` names only what it changes; what it leaves out stays as it was:

```csharp
renderer.PushState(DrawState.Additive);                           // a blend state
renderer.PushState(DrawState.Opaque with { Material = water });   // a blend state and a material
renderer.PushState(new DrawState(Material: outline).Filtered(TextureFilter.Linear));
```

`DrawState.Opaque`, `Alpha`, `Additive` and `Multiply` are the four blend states every device has. States nest, like
cameras, and every push needs its pop in the same frame. `SetBlendMode` keeps working, inside a state too.

## Render targets

A render target is a picture that is drawn into and then drawn like a texture: `target.Texture` works with every
`Draw` call, as the source of a `Sprite` and as a material's texture.

```csharp
_minimap = Assets.RenderTarget("hud/minimap", 64, 64);    // once, in Enter or Load

renderer.PushTarget(_minimap);          // in Draw
renderer.Clear(Color.Black);
DrawWorldSmall(renderer);               // any drawing call
renderer.PopTarget();
renderer.Draw(_minimap.Texture, _minimap.Bounds, new Vector2(250, 6), Color.White);
```

- A target starts like a frame: the renderer's `Width` and `Height` are the target's, there is no camera, no clip and
  no state, and blending is `Alpha`. `PopTarget` returns to the canvas with the camera, clip, blending and state it
  had. Targets nest.
- A target keeps its pixels between frames until it is drawn into again; `Clear` starts it from a colour. Its pixels
  are premultiplied, like the canvas, so drawing it with `DrawState.Opaque` copies it exactly.
- A target cannot be drawn into itself. To change a picture with itself (a distortion, a blur), draw it into another
  target.
- `Assets.RenderTarget` gives a target its scope owns: the same name and size return the same target, and it is
  released with the scope. `Game.Graphics.CreateRenderTarget(width, height)` gives one you dispose yourself.

## Light that stops at walls

Each light is drawn into a target of its own: its glow, added, then the shadow of every wall, in black, over it. The
lights are added into a light map that starts at the ambient colour, and the light map multiplies the finished picture.

<!-- code: samples/Examples/Examples/OccludedLightExample.cs#lightmap -->
```csharp
r.PushTarget(_lights!);
r.Clear(Ambient);
r.PopTarget();
AddLight(r, _torch, 110, new Color(255, 217, 153));
AddLight(r, new Vector2(280, 150), 80, new Color(115, 204, 255));

// The light map multiplies the picture: full light keeps a pixel, the ambient colour darkens it.
r.PushState(DrawState.Multiply);
r.Draw(_lights!.Texture, Whole, Vector2.Zero, Color.White);
r.PopState();
```

A shadow is a quad from a wall's edge to far beyond the screen, straight away from the light; only the edges that face
away from the light cast one, so a wall keeps its own light:

<!-- code: samples/Examples/Examples/OccludedLightExample.cs#light -->
```csharp
/// <summary>One light: its glow, then the shadow of every wall in black over it, added to the light map.</summary>
private void AddLight(IRenderer r, Vector2 at, float radius, Color color)
{
    r.PushTarget(_one!);
    r.Clear(Color.Black);
    r.PushState(DrawState.Additive);
    r.Draw(_glow!, _glow!.Bounds, at - new Vector2(radius), color, new DrawOptions(Scale: new Vector2(radius / 32)));
    r.PopState();
    foreach (var wall in Walls)
    {
        Vector2 a = new(wall.X, wall.Y), b = new(wall.X + wall.Width, wall.Y), c = new(wall.X + wall.Width, wall.Y + wall.Height), d = new(wall.X, wall.Y + wall.Height);
        Shadow(r, at, a, b, new Vector2(0, -1));
        Shadow(r, at, b, c, new Vector2(1, 0));
        Shadow(r, at, c, d, new Vector2(0, 1));
        Shadow(r, at, d, a, new Vector2(-1, 0));
    }
    r.PopTarget();

    r.PushTarget(_lights!);
    r.PushState(DrawState.Additive);
    r.Draw(_one!.Texture, Whole, Vector2.Zero, Color.White);
    r.PopState();
    r.PopTarget();
}

/// <summary>
/// The shadow an edge casts: only edges that face away from the light cast one (so a wall keeps its own light), as
/// a quad from the edge to far beyond the screen, straight away from the light.
/// </summary>
private static void Shadow(IRenderer r, Vector2 light, Vector2 from, Vector2 to, Vector2 outward)
{
    if (Vector2.Dot(outward, from - light) <= 0) return;
    const float Far = 1000;
    Vector2 farFrom = from + Vector2.Normalize(from - light) * Far, farTo = to + Vector2.Normalize(to - light) * Far;
    r.FillTriangle(from, to, farTo, Color.Black);
    r.FillTriangle(from, farTo, farFrom, Color.Black);
}
```

The walls here are rectangles; any shape made of edges works the same way, a tile map's solid cells included. The cost
is one small target and a handful of draws per light. In a world with a camera, push the camera inside each target and
keep the targets the size of the canvas.

## Masks

A picture shown only where a mask allows, with the same four blend states: draw the picture, the mask (white where the
picture shows, black elsewhere) and the mask's opposite into targets; multiply the picture by the mask and the scene by
the opposite; add the two. Grey values in the mask give soft edges.

<!-- code: samples/Examples/Examples/MaskExample.cs#mask -->
```csharp
// The picture: anything the drawing API draws. Here, moving stripes.
r.PushTarget(_picture!);
r.Clear(Color.Hex("ef7d57"));
for (int i = -2; i < 12; i++)
    r.FillRect(new RectF(i * 32 + _time * 24 % 32, 0, 16, 180), Color.Hex(i % 2 == 0 ? "ffcd75" : "a7f070"));
r.PopTarget();

// The mask is white where the picture shows and black elsewhere; the cover is its opposite.
Shapes(r, _mask!, Color.Black, Color.White);
Shapes(r, _cover!, Color.White, Color.Black);

// Picture × mask: the picture where the mask is white, black elsewhere.
r.PushTarget(_picture!);
Over(r, _mask!, DrawState.Multiply);
r.PopTarget();
// Scene × cover: a black hole in the scene where the picture goes. Then the picture is added into the hole.
Over(r, _cover!, DrawState.Multiply);
Over(r, _picture!, DrawState.Additive);
```

<!-- code: samples/Examples/Examples/MaskExample.cs#helpers -->
```csharp
/// <summary>The mask's shapes, text and a circle, drawn into a target like anything else.</summary>
private void Shapes(IRenderer r, RenderTarget2D target, Color background, Color shape)
{
    r.PushTarget(target);
    r.Clear(background);
    Font.DrawCentered(r, "MASK", new Vector2(160, 70), shape, scale: 9);
    r.FillCircle(_spot, 26, shape);
    r.PopTarget();
}

/// <summary>Draws the whole of a target's picture over what is being drawn into, with a blend state.</summary>
private static void Over(IRenderer r, RenderTarget2D source, DrawState blend)
{
    r.PushState(blend);
    r.Draw(source.Texture, Whole, Vector2.Zero, Color.White);
    r.PopState();
}
```

A blend state of your own does the same in one step where the device has custom blending (`sdl-gpu` always does; SDL's
software renderer does not): with `using Dev = MicroEngine.Graphics.Devices;`,
`new DrawState(new Dev.BlendState(Dev.BlendFactor.Zero, Dev.BlendFactor.SrcAlpha, Dev.BlendOp.Add, Dev.BlendFactor.Zero, Dev.BlendFactor.SrcAlpha, Dev.BlendOp.Add))`
keeps of a target only what the mask drawn over it covers. A blend state the device does not have is refused when it
is pushed, with `GraphicsCapabilityException`. There is no stencil buffer.

## A shader of your own: water

A material is one HLSL file, the fragment stage, compiled ahead of time by [`microengine shaders`](cli.md#shaders)
from the game's `Shaders` folder into `Content/shaders`. The engine supplies the vertex stage. This one mirrors the
picture above a water line and pushes it sideways with a wave that travels with time:

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

In the game, the compiled file is a material, and the picture it reads is a render target. Both belong to an asset
scope, which releases them:

<!-- code: samples/Examples/Examples/WaterExample.cs#setup -->
```csharp
public override void Enter()
{
    _assets = Assets.CreateScope("water");
    // Compiled ahead of time by `microengine shaders` from Shaders/water.frag.hlsl into Content/shaders.
    _water = _assets.Material("shaders/water.frag.shader");
    _above = _assets.RenderTarget("water/above", 320, 180, TextureFilter.Nearest);
}

public override void Exit() => _assets?.Dispose();   // the material and the target go with their scope
```

The scene draws its world into the target, draws the target, then draws the water's part of it again through the
material. The shader's parameters are set by the names they have in the HLSL:

<!-- code: samples/Examples/Examples/WaterExample.cs#draw -->
```csharp
// The world is drawn into a target, so the shader can read it: sky, a sun, hills, a few posts and flat water.
r.PushTarget(_above!);
r.FillGradient(new RectF(0, 0, 320, Surface), Color.Hex("29366f"), Color.Hex("ef7d57"));
r.FillCircle(new Vector2(236, 64), 22, Color.Hex("ffcd75"));
r.FillEllipse(new Vector2(70, Surface + 6), 90, 46, Color.Hex("257179"));
r.FillEllipse(new Vector2(250, Surface + 10), 110, 36, Color.Hex("38b764"));
for (int i = 0; i < 5; i++) r.FillRect(new RectF(120 + i * 22, Surface - 26 - i % 2 * 8, 5, 30 + i % 2 * 8), Color.Hex("1a1c2c"));
r.FillRect(Water, Color.Hex("3b5dc9"));
r.PopTarget();
r.Draw(_above!.Texture, Whole, Vector2.Zero, Color.White);

// The shader's parameters, by the names they have in water.frag.hlsl.
_water!.Set("Tint", new ColorF(0.16f, 0.30f, 0.62f, 0.45f));
_water.Set("Time", _time);
_water.Set("Amplitude", _amplitude);
_water.Set("Frequency", 9f);
_water.Set("Speed", 3f);
_water.Set("Surface", Surface / 180);
// The water's part of the picture, drawn in its own place through the material.
r.PushState(DrawState.Opaque with { Material = _water });
r.Draw(_above.Texture, Water, new Vector2(0, Surface), Color.White);
r.PopState();
```

### Parameters

| Call | Sets |
| --- | --- |
| `material.Set("Time", 1.5f)` | A value of a `cbuffer`: a `float`, `Vector2`, `Vector3`, `Vector4`, `ColorF`, `Matrix4x4`, or any value of the size the shader declares |
| `material.SetTexture("Glow", texture)` | A texture the shader samples, with the texture's own filter or a sampler you give; a render target's texture works |

- The names are checked. A name the shader does not have, or a value of another size than the shader declares, throws
  `ArgumentException` with the shader's parameters listed: `The shader 'water.frag.hlsl' has no parameter named 'Tme'.
  Its parameters: Tint, Time, Amplitude, Frequency, Speed, Surface, Scene.`
- Values never set are zero. A texture the shader samples and that was never given is refused at the draw, by name.
- The first texture a shader declares is the texture of each draw (`Scene` above); it is not set on the material.
- A draw keeps the parameters the material had when the draw was made: set them, draw, set them again, draw, and each
  draw has its own. Draws with the same texture, state and parameters share one draw call.
- A material's draws mix with ordinary draws in a frame and land in the order they were made.

### What a fragment shader receives

| Input | What it is |
| --- | --- |
| `TEXCOORD0` | The texture coordinate of the draw |
| `TEXCOORD1` | The draw's colour, premultiplied (a sprite's tint, a shape's colour) |
| Texture and sampler 0 (`register(t0, space2)`, `register(s0, space2)`) | The texture of each draw, with the state's sampler; shapes draw a white texture |
| Textures from 1 on | The textures given to `SetTexture` |
| `cbuffer` blocks (`register(b0, space3)`) | The values given to `Set` |

Return a premultiplied colour, and multiply it by the draw's colour so tints and fades keep working.

### Where shaders cannot run

The default SDL device, its software renderer, raylib, browser builds and the headless platform run no shaders. A
material is still created there, checks its parameters the same way, and its draws are made without the shader, with
the material's fallback. `material.IsSupported` says which.

| Fallback | What the material's draws become |
| --- | --- |
| `MaterialFallback.Plain` (the default) | The draws as they are |
| `new MaterialFallback(Tint: colour)` | The draws multiplied by a colour |
| `new MaterialFallback(Blend: state)` | The draws with another blend state |
| `MaterialFallback.Hide` | Nothing: for an effect that means nothing without its shader |

```csharp
_heat = Assets.Material("shaders/heat.frag.shader", MaterialFallback.Hide);
_night = Assets.Material("shaders/night.frag.shader", new MaterialFallback(Tint: Color.Hex("5a6ab4")));
```

The water example draws flat water there, and says so with one check:
`if (!_water.IsSupported) Font.DrawCentered(...)`.

## Post-processing

A post-processing effect takes a finished picture and leaves a new one: a glow around what is bright, darker corners,
the lines of an old screen. A `PostProcess` is a chain of them. A scene has one, `PostProcess`: add effects to it and
the engine draws the scene into a picture of the canvas's size, runs it through the effects in order and draws the
result over the scenes under it. The scene's `Draw` does not change.

<!-- code: samples/Examples/Examples/PostProcessExample.cs#chain -->
```csharp
public override void Enter()
{
    _assets = Assets.CreateScope("post-processing");
    var fringe = _assets.Material("shaders/fringe.frag.shader");   // a fragment shader of the game's own
    fringe.Set("Amount", 0.004f);
    PostProcess
        .Add(new Bloom { Threshold = 0.55f, Strength = 1.4f, Radius = 6 })
        .Add(new ShaderEffect(fringe))
        .Add(_pixelate)
        .Add(new Vignette { Strength = 0.7f })
        .Add(new Scanlines { Strength = 0.18f });
    // What this device leaves out or simplifies, to tell the player.
    _support = string.Join(", ", PostProcess.SupportOn(Game.Graphics)
        .Where(e => e.Support != PostEffectSupport.Full)
        .Select(e => $"{e.Effect.GetType().Name.ToUpperInvariant()} {(e.Support == PostEffectSupport.None ? "OFF" : "REDUCED")}"));
}
```

<!-- code: samples/Examples/Examples/PostProcessExample.cs#draw -->
```csharp
public override void Draw(IRenderer r)
{
    // The picture, drawn as in any scene: a dark room with a few bright things in it. The chain does the rest.
    r.FillGradient(Whole, Color.Hex("1a1c2c"), Color.Hex("29366f"));
    for (int i = 0; i < 6; i++)
    {
        var at = new Vector2(40 + i * 48, 90 + MathF.Sin(_time * 1.5f + i) * 34);
        r.FillCircle(at, 7, Color.Hex(i % 2 == 0 ? "ffcd75" : "73eff7"));
    }
    r.FillRect(new RectF(0, 150, 320, 30), Color.Hex("333c57"));
    r.FillRect(new RectF(20, 146, 280, 4), Color.Hex("f4f4f4"));
}

// The interface is not part of the scene's picture: it stays sharp over the effects.
public override void DrawUi(IRenderer ui)
{
    Heading.Draw(ui, Font, "POST-PROCESSING", PostProcess.Enabled ? "CONFIRM: OFF   ACTION: PIXELS" : "CONFIRM: ON");
    if (_support.Length > 0) Font.DrawCentered(ui, "NO SHADERS HERE: " + _support, new Vector2(160, 16), Color.White);
}
```

The chain owns the render targets, textures and materials its effects need, sized to the picture, and the engine
releases them when the scene leaves the stack. `PostProcess.Enabled = false` (or every effect's `Enabled`) draws the
scene as it is, at no cost; `PostProcess.Get<Bloom>()` returns an effect to change its values while the game runs.

### Where a chain runs

| Chain | The picture | Resolution | The interface |
| --- | --- | --- | --- |
| `Scene.PostProcess` | what that scene's `Draw` draws; transparent where it draws nothing, so scenes under it show | the canvas's | not included |
| `Game.PostProcess` | the whole canvas: background, every visible scene, the transition, `DrawAboveScenes` | the canvas's | not included |
| `Game.ScreenPostProcess` | the whole window: the letterbox, the canvas scaled into its frame, the interface over it | the window's | included |

The screen's chain is for what belongs to the screen rather than to the world: a tube's curve and lines, a fade over
everything, a shader that needs the window's pixels. The example puts a tube over a scene and its widgets:

<!-- code: samples/Examples/Examples/ScreenEffectsExample.cs#screen -->
```csharp
public override void Enter()
{
    // One line of the tube per row of the canvas, whatever the window's size.
    _tube.Lines = Game.Height;
    Game.ScreenPostProcess.Add(_tube).Add(_corners);

    Widgets.Anchor = Anchor.Center;
    Widgets.Content = new Panel(new Label("THE INTERFACE IS PART\nOF THE SCREEN'S PICTURE"));
}

// The chain belongs to the game, not to this scene: take out what this scene put in.
public override void Exit()
{
    Game.ScreenPostProcess.Remove(_tube);
    Game.ScreenPostProcess.Remove(_corners);
}
```

The game's two chains belong to the game, so a scene that adds effects to them takes them out when it leaves. While
the screen's chain is active, captures (`--captures`, screenshots) are the window as it was presented, at the
window's size. A platform of your own takes part by offering `IScreenEffects`; on one that does not, the chain runs
on the canvas instead.

A chain of your own does the same for any part of a frame: `chain.Begin(renderer, Game.Graphics)`, the draws, then
`chain.End(renderer)`; or `chain.Apply(renderer, Game.Graphics, texture, destination)` for a picture that already
exists (a render target's texture). Dispose a chain of your own when you are done with it, between frames.

### The ready effects

| Effect | What it does | Values |
| --- | --- | --- |
| `ColorGrade` | brightness, contrast, saturation and a tint | `Brightness`, `Contrast`, `Saturation` (1 changes nothing), `Tint` |
| `Blur` | blurs the picture | `Radius` in pixels |
| `Bloom` | adds the blurred bright parts back, so lights glow | `Threshold` (0..1), `Strength`, `Radius` |
| `Vignette` | darkens towards the corners | `Strength`, `Radius`, `Softness`, `Color` |
| `Scanlines` | darkens every other line | `Strength`, `Lines` |
| `Crt` | a tube: curved glass, lines, colour stripes | `Curvature`, `Scanlines`, `Lines`, `Mask` |
| `ShaderEffect` | a pass through a material of your own | the material's parameters; `Prepare` for those that depend on the picture's size |

Pictures are premultiplied, like the canvas, and the effects keep a picture's transparency: a vignette on a scene that
covers part of the screen darkens that scene only.

### What each effect draws without shaders

Effects are made of the drawing API, so a chain runs everywhere. Shaders run on the `sdl-gpu` device only; on SDL's
default device and its software renderer, on raylib, in browser builds and on the headless platform, an effect with a
shader draws a simpler form made of render targets, blend states and linear sampling, or is left out. Each effect
reports which with `SupportOn(Game.Graphics)`, and `PostProcess.SupportOn` lists the whole chain (the example shows
it to the player):

| Effect | Where shaders run | Elsewhere |
| --- | --- | --- |
| `Vignette`, `Scanlines` | `Full` | `Full`: the same picture, pixel for pixel |
| `ColorGrade` | `Full` | `Full` with a tint and a brightness only; `Reduced` when `Contrast` or `Saturation` is not 1: those two are left out |
| `Blur` | `Full`: a Gaussian blur | `Reduced`: the picture halved and enlarged again with linear sampling, a boxier blur of about the same reach |
| `Bloom` | `Full`: what is brighter than `Threshold`, blurred, added | `Reduced`: the bright parts picked by multiplying the picture by itself (no exact threshold), blurred by halving |
| `Crt` | `Full` | `Reduced`: its lines only, flat and without stripes; `None` when `Scanlines` is 0 |
| `ShaderEffect` | `Full` | `None`: the picture passes through unchanged |

### A pass through a shader of your own

`ShaderEffect` runs the picture through a material: the picture is the draw's texture, in slot 0, and the shader's
output is the new picture. The example's separates red and blue towards the edges:

<!-- code: samples/Examples/Shaders/fringe.frag.hlsl -->
```hlsl
// A post-processing pass of the game's own: the red and blue of the picture pulled apart towards the edges, as a cheap
// lens does. The picture is the draw's texture, in slot 0.
// SDL_GPU binding rules (fragment stage): textures and samplers in space2, uniform buffers in space3.
Texture2D<float4> Picture : register(t0, space2);
SamplerState PictureSampler : register(s0, space2);

cbuffer Fringe : register(b0, space3)
{
    float Amount;  // how far apart the colours are at the edge, in texture coordinates
};

struct Input
{
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

float4 main(Input input) : SV_Target0
{
    float2 apart = (input.TexCoord - 0.5) * 2.0 * Amount;
    float4 c = Picture.Sample(PictureSampler, input.TexCoord);
    c.r = Picture.Sample(PictureSampler, input.TexCoord + apart).r;
    c.b = Picture.Sample(PictureSampler, input.TexCoord - apart).b;
    return c * input.Color;
}
```

The game owns the material (here an asset scope does); the chain only uses it. Create it in `Load`, or behind a
loading screen, when it is used during play: reading a shader and creating it on the device takes longer than a frame.

### An effect of your own

An effect derives from `PostEffect`: `SupportOn` says what it draws on a device, and `Apply` receives a `PostPass`
with the picture so far (`Source`), the renderer, and targets and textures the chain keeps for it between frames:

<!-- code: samples/Examples/Examples/PostProcessExample.cs#effect -->
```csharp
/// <summary>An effect written in the game: the picture in large pixels. It needs no shader, so it is the same everywhere.</summary>
public sealed class Pixelate : PostEffect
{
    /// <summary>The side of a large pixel, in pixels of the picture.</summary>
    public int Size { get; set; } = 4;

    public override PostEffectSupport SupportOn(IGraphicsDevice graphics) => PostEffectSupport.Full;

    protected override void Apply(PostPass pass)
    {
        // The picture shrunk into a target of the chain's (linear: each large pixel is the average of what it
        // covers), then enlarged again with nearest sampling as the new picture.
        var small = pass.Scratch("small", Math.Max(1, pass.Width / Size), Math.Max(1, pass.Height / Size), TextureFilter.Nearest);
        pass.Draw(pass.Source, small, DrawState.Opaque.Filtered(TextureFilter.Linear));
        pass.Replace(small.Texture, TextureFilter.Nearest);
    }
}
```

| `PostPass` | Use |
| --- | --- |
| `Source`, `Width`, `Height` | the picture so far, and its size |
| `Copy(material, tint)` | the picture through a material, texel for texel, as the new picture |
| `Replace(texture, filter, material, tint)` | another texture, stretched over the picture's size, as the new picture |
| `BeginOver()` ... `EndOver()` | draw on top of the picture (a glow added, a vignette multiplied in) |
| `Scratch(name, width, height, filter)` | a render target the chain keeps for this effect |
| `Image(name, version, state, create, filter)` | a texture the chain keeps for this effect, made again when `version` changes |
| `Draw(source, target, state, tint)` | one pass: a whole texture over a whole target |
| `ShadersRun` | whether materials run their shaders on this device |

The passes are part of the frame, so captures (`--captures`) show them.

## The device under the drawing API

`Game.Graphics` and the renderer are views of a graphics device on the SDL and raylib platforms, and a game that needs
more than this page can use the device directly ([the graphics device](../manual/rendering.md#the-graphics-device)):
`texture.Native` is the device texture behind any texture, `DeviceGraphics.Wrap` turns a device texture or render
target the game created into one the drawing API draws, and `Material2D.SpriteVertexShader` is the vertex stage for a
material built from shaders created on the device. None of it is needed for the techniques above.

## Limits

- Shaders run on the `sdl-gpu` device: Direct3D 12 or Vulkan on Windows, Vulkan on Linux (the Metal shaders are
  generated but have not been run on a Mac). Everywhere else a material draws with its fallback. Render targets and
  the four blend states work everywhere, browsers included.
- Compiling shaders needs Windows ([the tool's page](cli.md#shaders) says why); the compiled files load everywhere.
- A material is a fragment shader; the vertex stage is the engine's.
- Effects with a shader draw their simpler form, or nothing, where shaders do not run
  ([the table above](#what-each-effect-draws-without-shaders)); browser builds are among those places.
- A scene's chain and `Game.PostProcess` run at the canvas's resolution, before the canvas is scaled to the window,
  and do not include the interface; `Game.ScreenPostProcess` runs at the window's and includes it. The on-screen
  touch controls are drawn after every chain.
- A chain's picture has the size of what it is drawn into; use one chain per picture. Release a chain of your own
  between frames, like any texture.
- A pass has one colour target; there is no stencil buffer and no multisampling.
