# 0060 — No content build; textures made ahead of time are optional KTX 2.0 files

Status: Accepted (2026-11-18). The SDK is in Alpha. Amends [ADR 0025](0025-asset-pipeline.md) (which left GPU
texture formats to a later build step) and uses none of the import seam reserved by
[ADR 0034](0034-content-handles-loaders-sources.md). Evidence: [CONTENT-1](../evaluations/content-1.md). Task record:
[`odd/tasks/content-1.md`](../../odd/tasks/content-1.md). Issue NastMz/MicroEngine#113.

## Context

MicroEngine ships a game's files as they are and decodes them at load (ADR 0025). The issue asks whether large HD
or 3D games need more: GPU-ready compressed textures, mipmaps, conversion or other preprocessing ahead of time, and
whether the SDK should integrate, prototype, defer or reject an optional pipeline, after establishing what the
direct loaders lack.

CONTENT-1 measured the direct loaders on high-resolution content drawn by code (three kinds of 2D art at 1024,
2048 and 4096 pixels square: a packed page of sprites over transparency, a painted backdrop, a tileset) and, on the
same content, the candidates of an offline step. On the reference machine (Ryzen 5 7600X, RTX 4070 Ti SUPER,
.NET 10, Windows 11), one loading screen of twelve 2048 × 2048 pages (192 MB of pixels), each path in a process of
its own, three rounds:

| Path | Device | Loading screen | Garbage | Graphics memory | On disk |
| --- | --- | ---: | ---: | ---: | ---: |
| `Assets.Texture`, as the SDK was | default (Direct3D 11) | 780 to 785 ms | 747 MB | 392 MB | 34 MB |
| `Assets.Texture`, with this change | default | 699 to 722 ms | 747 MB | 392 MB | 34 MB |
| `Assets.Preload`, with this change | default | 303 to 331 ms | 749 MB | 392 MB | 34 MB |
| Texture files of pixels | default | 204 to 217 ms | 266 MB | 392 MB | 37 MB |
| `Assets.Texture`, with this change | `sdl-gpu` (Direct3D 12) | 705 to 748 ms | 747 MB | 196 MB | 34 MB |
| Texture files of pixels | `sdl-gpu` | 219 to 224 ms | 266 MB | 196 MB | 37 MB |
| Texture files of BC7 blocks | `sdl-gpu` | 98 to 127 ms | 88 MB | 52 MB | 20 MB |

What the measurements say:

- **Decoding is most of a load, and the loader throws away three to four times the pixels it uploads.** A 2048 × 2048
  PNG took 44 to 79 ms to become a texture (a 4096 × 4096 one 175 to 300 ms), half to four fifths of it in the decoder, and
  allocated 51 to 69 MB for 16 MB of pixels. The conversions around the decoder (copying the image's pixels into
  bytes, multiplying them by alpha) took 8 to 14 ms of each 2048 × 2048 load where the device itself needed 3.
- **`Preload` halves the loading screen and does not keep it smooth with large pages.** It uploads on the game's
  thread: its longest frame was 33 to 46 ms with 2048 × 2048 pages and 137 to 349 ms with 4096 × 4096 ones (21 to
  47 and 82 to 98 ms with this change, still more than a 60 Hz frame), and holding every decoded image at once took
  the process to 0.8 to 1.0 GB (2048) and 2.6 to 3.6 GB (4096).
- **A texture stored as the device takes it loads without a decoder.** The same pixels deflated in a file loaded
  three to four times faster than the PNG, and BC7 blocks six to nine times faster, in a quarter of the graphics
  memory (measured through DXGI: 52 MB against 196 MB), from files smaller than the PNG files (20 MB against 34 MB
  for the twelve pages). Stored without deflating, the blocks load in 27 ms and take 48 MB on disk.
- **BC7 costs encoding time and a little fidelity.** The managed encoder took 9 to 26 s for a 2048 × 2048 page and
  32 to 93 s for a 4096 × 4096 one; the decoded blocks are 47.6 to 58.1 dB from the pixels (peak signal-to-noise
  ratio), and on a real device a picture with hard outlines over transparency came back with a mean error of 0.29
  of 255 and a worst pixel of 52, on an outline's edge.
- **Mipmaps need no offline step.** A whole chain takes 2 to 14 ms on the CPU for a 2048 × 2048 page. What they would
  buy is real below half size (linear filtering with one level is exact at one half; at a quarter and an eighth it
  flickers two to six times more than the matching mip level while the picture scrolls), and no device of the SDK
  declares them.
- **Only one device samples block formats.** SDL's default device (SDL_Renderer, which is also the browser's) has
  neither compressed formats nor mipmaps; raylib's device declares neither; `sdl-gpu` samples BC7 on desktop
  adapters, ASTC where the adapter has it, and has no ETC2.

The survey (MonoGame's content pipeline and its new content builder, Basis Universal, Khronos's KTX tools, ASTC and
BCn encoders, and the .NET libraries around them) is in the evaluation, with sources. Two facts from it shape the
decision: MonoGame's pipeline pays for itself with per-platform GPU formats and compiled effects, writes a container
of its own (XNB) and pulls sixteen dependencies, several of them native tools; and the one universal format (Basis
Universal in KTX 2.0) needs a native transcoder in every game, for which .NET has only a preview binding without
Windows or WebAssembly binaries.

## Scope of the evaluation

Evaluated, with measurements or runs: load time by stage, garbage and collections, peak memory, graphics memory and
disk size of the direct loaders and of the candidates, on Windows with Direct3D 11 (the default device) and
Direct3D 12 (`sdl-gpu`); BC7 sampled by a real device against the source image; encoding time and error of one
managed BC7 encoder; the cost of a mipmap chain on the CPU and what one level loses when art is drawn small, on the
reference device; the direct loaders of sounds and music (milliseconds: nothing to gain); what the candidates
depend on, from primary sources.

Not evaluated:

- **Browsers.** .NET runs interpreted and on one thread there, so decoding is expected to cost far more than on a
  desktop and `Preload` cannot spread it; that was not measured. Texture files of pixels work on the browser's device
  by construction (it is the default device) and were not run in a browser.
- **Phones.** ASTC and ETC2, which phones sample, were neither encoded nor sampled; the phone platforms are
  experiments, not supported platforms.
- **Machines with little memory.** The reference adapter has 16 GB. That a game runs out of graphics memory without
  block compression was computed (four bytes a pixel against one), not reproduced.
- **Vulkan and Linux**, for the measurements (the device tests run where CI runs them).
- **Real art.** The content is drawn by code to resemble three kinds of art; a studio's files may compress,
  decode and encode differently.
- **Fidelity by eye.** Error is reported as numbers; no art director judged BC7 against the source.
- **Fonts, atlases, meshes and audio ahead of time** (baked glyph pages, packed atlases, converted models,
  transcoded audio). ASSET-1 measured their loads in milliseconds; nothing here measured a gain from converting them.
- **Files written by other tools.** `TextureFile` follows the KTX 2.0 specification as read for this work; no file
  from Khronos's tools was read by it, and its own files were not run through Khronos's validator.

## Decision

**No content build, and no container of the engine's own.** A game is never required to run a conversion, and the
engine defines no file format for textures. This rejects, again and now with measurements, a pipeline in MonoGame's
shape: the direct loaders stay the default and every sample, template and demo keeps loading images.

**Integrate one optional piece: textures made ahead of time, as KTX 2.0 files.**

- `TextureFile` (`MicroEngine.Graphics`) reads and writes KTX 2.0: one 2D texture in RGBA8, BC7, ETC2 RGBA8 or
  ASTC 4 × 4, levels stored plainly or deflated (ZLIB), the premultiplied-alpha flag of the data format descriptor.
  `Create(device)` uploads level 0; a device that does not sample the format refuses with
  `GraphicsCapabilityException`, as for any undeclared capability, and `CanCreate` asks first. It is managed code
  with no new dependency (the runtime's own zlib), beside `CompiledShader`, which it mirrors: a file made ahead of
  time by the tool, parsed by the contract's assembly, created on a device.
- `microengine textures` converts a folder of images into those files: BC7 blocks (the default) or the image's own
  pixels, premultiplied, deflated, skipping files that are up to date. The BC7 encoder (BCnEncoder.Net, managed,
  MIT or Unlicense) ships with the tool only.
- The files are drawn through the 2D batcher (`Renderer2D`), the documented way to draw a device texture from a
  scene. The "A texture made ahead of time" example draws one where the device samples it and the image elsewhere.
- KTX 2.0 because it is an open standard that holds every format the contract names, mip levels, a lossless
  supercompression the runtime already has, and the premultiplied flag; DDS has no ASTC, no supercompression and is
  Microsoft's.

**Fix what the direct loaders wasted without any infrastructure.** `PixelImage.ToRgba` copies the pixels in one step
and `PremultipliedColor.PremultiplyInPlace` skips opaque and transparent texels and no longer divides: the same
bytes, and a 2048 × 2048 upload that took 11 to 17 ms takes 5 to 10 (the loading screen above went from about 780
to about 710 ms).

**Reject mipmaps made offline.** They cost milliseconds at load; whether the SDK draws with mipmaps at all is a
graphics decision (a device capability, a sampler, the drawing API), not a content one.

**Defer, each with what reopens it:**

1. **The drawing API's use of texture files** (`Assets.Texture` choosing a texture file beside an image, sprites and
   atlases from one, lifetimes through scopes). It needs the core's texture type to wrap a device texture. Reopens
   when a game draws its sprites from texture files and writes the same batcher code in every scene, or with the
   substantial demo game (NastMz/MicroEngine#114) if it ships HD art.
2. **A texture loader that makes no intermediate image, and a faster PNG decoder.** Both were prototyped in the
   benchmark: decoding straight to device bytes cuts the garbage of a load from 3.9 to 1.5 times the pixels, and a
   decoder that inflates with the runtime's zlib is 1.7 to 2.3 times faster than the current one on these files
   (the loading screen above in about 490 ms instead of about 710). They change the core's loaders and add a decoder ADR 0025
   removed, so they are their own change. Reopens with any report of loading time on images, and first of all with
   a measurement in a browser.
3. **Basis Universal (one file for every GPU).** Reopens when phones or browsers become targets for
   block-compressed textures and a transcoder exists for .NET on every platform the SDK packages, WebAssembly
   included, or the engine carries one.
4. **ASTC and ETC2 from the tool**, with the phone platforms: when they are supported platforms and a device samples
   those formats there.
5. **Mip levels of a file**: when a device declares `GraphicsFeatures.Mipmaps`. `TextureFile` already holds them.
6. **Zstandard**: when the runtime the SDK targets includes it, or a file from another tool needs it.
7. **A conversion driven by the project** (the `IAssetImporter` seam of ADR 0034: settings beside each source,
   conversion during `build` and `package`). Today the developer runs a command and commits or scripts the result,
   as with shaders. Reopens when games convert enough files that the manual step is forgotten or slow.

## Alternatives considered

- **MonoGame's content pipeline, or one like it.** A content project, a builder, XNB files read by a content
  manager: a proprietary artifact the issue excludes, tied to MonoGame's framework types, with sixteen package
  dependencies and native tools per build platform. Its texture processor is the part worth having, and it is
  BCnEncoder.Net, the library adopted here.
- **Basis Universal in KTX 2.0 as the only format.** One file for every device, smaller files; a native transcoder
  in every game on every platform, transcoding time at load, and no .NET binding fit to ship. The measured gain
  on the device that can use block formats needs none of it.
- **DDS.** Simpler to write and widely read by desktop tools; no ASTC or ETC2, no supercompression (the BC7 pages
  would be 48 MB on disk instead of 20), no premultiplied flag outside the DX10 header.
- **A raw dump of the device bytes** (the fastest load measured: nothing to inflate). A format of the engine's own;
  KTX 2.0 stores the same bytes plainly when a file is written without deflating (`TextureFile.ToBytes(deflate:
  false)`), which the tool does not offer yet: four times the disk for BC7, five for pixels.
- **An optional package** (`MicroEngine.Content.Textures`). The reader is 250 lines without a dependency and belongs
  with the device contract it creates textures on; a package would add versions, packing and checks for nothing a
  game needs separately. The encoder, which is the dependency, stays in the tool.
- **Mipmaps and compression decided by the engine at load** (generate, or compress on the GPU). BC7 cannot be
  encoded at load in useful time (seconds a page); mipmaps can, and are deferred with the capability.
- **Defer everything.** The measurements showed a path that already worked at the device level; leaving an external
  developer to write a KTX reader and find an encoder would have left the capability unused.

## Consequences

- A game with a lot of HD art on `sdl-gpu` can cut its texture memory to a quarter and its loading to between a
  sixth and a ninth, with one command and a dozen lines; a game on the default device can cut its loading with files of
  pixels. Both write batcher code to draw them, and neither gets them through `Assets` or atlases yet.
- The `microengine` tool gains a dependency (BCnEncoder.Net 2.3.0 and CommunityToolkit.HighPerformance 8.4.0, both
  managed and permissive, listed in `THIRD-PARTY-NOTICES.md`). No SDK package gains one.
- `MicroEngine.Graphics` gains a public type and a file format it must keep reading. The format is Khronos's, so its
  definition is not the engine's to change.
- BC7 files are lossy and their encoding is slow with a managed encoder; a studio with thousands of pages will want
  a native encoder, whose output `TextureFile` reads if it is KTX 2.0 within the bounds above (not verified with one).
- The `Examples` sample ships one more picture, as an image and as a texture file made by the tool; a test fails if
  the device draws it differently from the image.
- The benchmark (`benchmarks/ContentBench`) keeps the measurements repeatable, and holds the two prototypes the
  deferred items rest on.
- What the measurements do not cover is listed above, first of all a browser.
