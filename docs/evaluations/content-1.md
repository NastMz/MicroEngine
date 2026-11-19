# CONTENT-1: optional offline asset processing and reusable tooling, evaluated

Issue NastMz/MicroEngine#113. Record: [`odd/tasks/content-1.md`](../../odd/tasks/content-1.md). Decision:
[ADR 0060](../decisions/0060-optional-texture-files.md). Date: 2026-11-18. Builds on [ASSET-1](asset-1.md) (the asset
pipeline, ADR 0025), on the device contract of ARCH-1 and GRAPHICS-1, and on the measurement discipline of
[PERF-3](perf-3.md).

Evidence words are used strictly, as in PERF-3. **Measured** means run on the machine of section 1 and read from its
output. **Hypothesis** means a cause proposed for a measured effect and not isolated by an experiment. **Computed**
means arithmetic on known sizes. **Not run** means exactly that. No number is a promise: each is "this content, this
machine, this build".

**The result in one paragraph.** Loading images directly is fine for the games the SDK ships and stops being cheap
with high-resolution art: a 2048 × 2048 page takes 44 to 79 ms to become a texture and throws away three to four
times its pixels in garbage, and twelve of them make a loading screen of 0.8 s and 392 MB of graphics memory on the
default device. A third of that slack was in the loader itself and is fixed (the conversions around the decoder).
Beyond it, the format is the limit: the same pages stored as the device takes them load three to four times
faster as pixels and six to nine times faster as BC7 blocks, which also take a quarter of the graphics memory
and less disk than the PNG files. Decision: **no content build; integrate one optional piece**, texture files in
KTX 2.0 (`TextureFile`, `microengine textures`); **reject** offline mipmaps; **defer** universal textures (Basis
Universal), phones' formats, mip levels and the drawing API's use of the files, each with what reopens it
(ADR 0060). What was not evaluated is in section 11, first of all a browser.

## 1. Conditions

| | |
| --- | --- |
| CPU / RAM | AMD Ryzen 5 7600X (6 cores, 12 threads), 31 GB |
| GPU | NVIDIA GeForce RTX 4070 Ti SUPER (16 GB), driver 32.0.16.1088; the integrated AMD Radeon was not used |
| System | Windows 11 Pro 10.0.26300, x64 |
| Runtime | .NET 10.0.15 (SDK 10.0.404); JIT with tiered compilation; workstation concurrent GC |
| Engine | 0.6.0-alpha.1, branch `feat/content-1` from `37c264c`; Release |
| Devices | SDL's default device (`sdl-renderer`, Direct3D 11) and `sdl-gpu` (Direct3D 12), hidden window |
| Libraries | StbImageSharp 2.30.15 (the engine's decoder); BCnEncoder.Net 2.3.0 (2026-03-05, nuget.org), pinned |

**How idle the machine was.** `docs/evaluations/content-1/campaign.sh` shuts the build servers down and records,
before and after each group, the CPU time every other process used in five seconds (`idle.ps1`). The machine was
never idle: a desktop session (a browser, a chat client, peripheral software) and, during the first groups, another
session's programs used 6 to 15% of the machine throughout, which is not this session's to close. The figures are
therefore conservative rather than best cases, and every conclusion below rests on differences far larger than that
load: the loading screens were measured three times each, in separate processes, and their repeats are given as
ranges.

| Group | Idle check before / after (local time) | Other processes, CPU-seconds in 5 s (% of the machine) |
| --- | --- | --- |
| direct, as the SDK was (section 4) | 21:52:38 / 21:53:49 | 5.07 (8.4%) / 7.80 (13%) |
| direct, with this change (section 4.1) | 21:55:07 / 21:56:13 | 7.16 (11.9%) / 7.28 (12.1%) |
| candidates (section 7) | 21:56:23 / 22:02:56 | 9.20 (15.3%), a browser / 4.03 (6.7%) |
| loading screens, as the SDK was (section 5) | 21:33:57 / 21:38:15 | 3.86 (6.4%) / 6.87 (11.4%) |
| loading screens, with this change (section 5) | 22:03:55 / 22:08:22 | 9.16 (15.3%) / 7.04 (11.7%) |
| minification (section 8) | 22:08:33 | 7.49 (12.5%); deterministic arithmetic on the CPU, not a timing |

A first run of the loading screens with this change overlapped a build of this session and was discarded whole; the
table holds the run made afterwards with nothing else of this session running. No two groups ran at once.

## 2. What loads today, and what a game can reach

- **Images.** `Assets.Texture(path)` reads the file, `PixelImage.Decode` decodes it in managed code (StbImageSharp)
  into bytes and copies them into the image's `Color` array, `DeviceGraphics.CreateTexture` copies them back into
  bytes, multiplies colour by alpha and hands them to the device as `Rgba8Unorm`: four bytes a pixel on every device.
  `Assets.Preload` decodes on worker threads and uploads on the game's thread within a budget per frame.
- **The device contract** already names `Bc7`, `Etc2Rgba8` and `Astc4x4` and a `MipLevels` count. `sdl-gpu` maps BC7
  and ASTC to SDL_GPU formats and declares each for sampling where the adapter supports it (SDL_GPU has no ETC2);
  no device declares `GraphicsFeatures.Mipmaps`, and nothing uploads a level other than 0. SDL's default device and
  raylib's declare the 8-bit RGBA and BGRA formats only: SDL_Renderer's pixel formats hold no block-compressed format and its scale
  modes no mipmap ([SDL_PixelFormat](https://wiki.libsdl.org/SDL3/SDL_PixelFormat),
  [SDL_ScaleMode](https://wiki.libsdl.org/SDL3/SDL_ScaleMode)).
- **A game reaches the device** from a scene (`((DeviceGraphics)Game.Graphics).Device`) and draws device textures
  with a `Renderer2D` of its own into the canvas; that path is documented and tested (GRAPHICS-1). So before this
  work a game on `sdl-gpu` could already create a BC7 texture, if it brought its own encoder and its own file
  reader. The drawing API (`IRenderer.Draw`, `SpriteAtlas`, `Assets.Texture`) takes images only.
- **Everything else the store loads** was measured by ASSET-1 in milliseconds (atlases 1 to 2 ms, fonts 3 to 5 ms
  for a prepared size) and, for sound, here: a 1.9 MB WAV decodes in 1.3 ms, and music (up to 2.3 MB of OGG) is read
  in 0.3 to 0.5 ms and decoded while it plays. Nothing there is a load-time problem an offline step would solve.
- **The repository's own content** does not show any of this: 213 images, 4.4 MB in all, the largest 245 KB. The
  question is about games the demos are not, so the content had to be made.

## 3. The benchmark and its content

`benchmarks/ContentBench` draws its own content (no art is added to the repository) and measures on it. Three kinds
of 2D art, at 1024, 2048 and 4096 pixels square, written as PNG the way an art tool writes them (RGBA, the best
filter per row, default deflate; the engine's own PNG writer is for captures and gives larger files):

| File | PNG | Decoded | PNG / decoded | Transparent | Partly transparent |
| --- | ---: | ---: | ---: | ---: | ---: |
| `sprites`: a packed page of flat-shaded characters with outlines | 530 KB (2048), 2.2 MB (4096) | 16 MB, 64 MB | 3.2%, 3.4% | 52% | 2.1% |
| `painted`: an opaque backdrop, soft gradients with grain | 3.8 MB, 14.9 MB | 16 MB, 64 MB | 23.6%, 23.3% | 0% | 0% |
| `tiles`: a tileset of repeated textured tiles, some foliage | 4.2 MB, 11.7 MB | 16 MB, 64 MB | 26.5%, 18.3% | 8% | 8.6% |

The pictures were looked at (one of each kind at 1024). They are stand-ins: flat illustrated art compresses far
better than these `painted` and `tiles` pages, photographs worse.

Commands: `generate` (the content), `direct` (each stage of a direct load, per file: median of 7 after a warm-up,
bytes allocated on every thread, collections and pauses), `candidates` (the offline candidates: sizes, encoding
time, error, a mipmap chain), `level` (one loading screen in a process of its own: pages loaded at a 60 Hz cadence,
total time, the longest frame, garbage, collections, pauses, peak working set, and the video memory the operating
system accounts to the process, through DXGI's `QueryVideoMemoryInfo`), `minify` (section 8). Its tests
(`tests/Benchmarks.Tests/ContentBenchTests.cs`) keep the content the same on every run, the PNG files equal to the
pixels drawn, and the prototype decoder equal to the engine's.

## 4. Direct loading, measured (as the SDK was)

Default device, 2048 × 2048 and 4096 × 4096 pages (the 1024 pages scale the same way; every row is in the campaign's
output). Garbage is every byte allocated during the load.

| Page | Read | Decode | of it the decoder alone | Upload | of it the device alone | All (`Assets.Texture`) | Garbage | Garbage / pixels |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| sprites, 2048 | 0.1 ms | 30.7 ms | 25.9 ms | 16.8 ms | 2.9 ms | 45.3 ms | 50.6 MB | 3.2 |
| painted, 2048 | 0.4 ms | 52.4 ms | 52.7 ms | 11.2 ms | 3.4 ms | 78.5 ms | 66.9 MB | 4.2 |
| tiles, 2048 | 0.6 ms | 71.0 ms | 61.0 ms | 11.9 ms | 3.2 ms | 76.1 ms | 69.2 MB | 4.3 |
| sprites, 4096 | 0.3 ms | 114.4 ms | 109.2 ms | 62.8 ms | 38.5 ms | 226.8 ms | 202.9 MB | 3.2 |
| painted, 4096 | 3.2 ms | 234.2 ms | 207.5 ms | 55.4 ms | 45.7 ms | 299.4 ms | 266.6 MB | 4.2 |
| tiles, 4096 | 1.7 ms | 230.0 ms | 208.3 ms | 74.4 ms | 38.8 ms | 291.9 ms | 250.7 MB | 3.9 |

On `sdl-gpu` the same loads took 44 to 78 ms (2048) and 175 to 277 ms (4096): the device is not where the time goes.
Stages are medians of separate measurements, so a row's stages do not add up exactly to its last column.

The measurable shortcomings, in the order a game meets them:

1. **Time is the decoder's.** 11 to 19 ms per million pixels, most of it inside the PNG decoder (inflate and the row
   filters, in managed code). ASSET-1's "decoding a game-sized image takes milliseconds" holds for its images (512 ×
   1024 at most) and not for pages of HD art.
2. **Garbage is three to four times the pixels.** The decoder's output, its copy into the image's own array and the
   copy back into bytes for the device are 4 bytes a pixel each, plus the decoder's own buffers and the file. Every
   one of them is a large-object allocation.
3. **The conversions around the decoder cost as much as a small decode.** Uploading took 11 to 17 ms for a
   2048 × 2048 page where the device alone needed 3: the rest was a per-pixel copy into bytes and a division per
   channel to multiply by alpha.
4. **Graphics memory is four bytes a pixel**, with no other choice from an image.
5. **Art drawn smaller than half its size has no mipmaps to draw from** (section 8).

### 4.1 The slack that needed no infrastructure, removed

`PixelImage.ToRgba` now copies the pixels in one step, and `PremultipliedColor.PremultiplyInPlace` skips opaque and
fully transparent texels and multiplies without dividing. The results are the same bytes: a test checks every colour
value against every alpha (`Premultiply_IsTheNearestValueForEveryChannelAndAlpha`, 65,536 pairs, written before the
change and passing on the old code too), and the existing image tests compare `ToRgba` with decoded pixels.

| Upload (`CreateTexture`), default device | Before | After |
| --- | ---: | ---: |
| sprites, 2048 | 16.8 ms | 7.7 ms |
| painted, 2048 | 11.2 ms | 5.2 ms |
| tiles, 2048 | 11.9 ms | 5.6 ms |
| sprites, 4096 | 62.8 ms | 30.8 ms |
| painted, 4096 | 55.4 ms | 22.9 ms |
| tiles, 4096 | 74.4 ms | 28.5 ms |

On `sdl-gpu`: 16.1 to 8.3, 12.6 to 8.1, 12.7 to 9.8 ms (2048) and 64.9 to 36.8, 50.7 to 33.5, 50.1 to 39.6 ms
(4096). A whole load of a 2048 × 2048 page went from 44 to 79 ms to 35 to 72 ms across the two devices. Between two
runs of the same stage the medians moved by up to a fifth (the device-alone upload of a 4096 page read 38.5 ms in
one run and 14.0 ms in the next), so single rows are indications; the direction is the same in all twelve.

What is left of shortcomings 2 and 3 is in the core's texture loader, which makes an image to make a texture; see
the prototypes of section 6.

## 5. One loading screen

Twelve pages (four of each kind) loaded as a game would, each path in a fresh process, three rounds; the ranges are
the three rounds. "As the SDK was" is the base commit; the rest is this branch.

| Pages | Device | Path | On disk | Loading screen | Longest frame | Garbage | Gen 2 collections | GC pauses | Peak working set | Graphics memory |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 12 × 2048² | default | `Assets.Texture`, as the SDK was | 34 MB | 780 to 785 ms | the whole load | 747 MB | 11 to 12 | 1.5 to 3.6 ms | 435 to 474 MB | 392 MB |
| | default | `Assets.Preload`, as the SDK was | 34 MB | 388 to 453 ms | 33 to 39 ms | 749 MB | 3 to 4 | 1.3 to 2.0 ms | 911 to 972 MB | 392 MB |
| | default | `Assets.Texture` | 34 MB | 699 to 722 ms | the whole load | 747 MB | 11 to 12 | 1.3 to 3.9 ms | 435 to 474 MB | 392 MB |
| | default | `Assets.Preload` | 34 MB | 303 to 331 ms | 21 to 24 ms | 749 MB | 3 to 5 | 1.5 to 2.6 ms | 910 to 973 MB | 392 MB |
| | default | prototype `lean` | 34 MB | 641 to 659 ms | the whole load | 294 MB | 8 | 1.6 to 1.8 ms | 372 MB | 392 MB |
| | default | prototype `fast` | 34 MB | 470 to 504 ms | the whole load | 226 MB | 6 | 0.6 to 0.7 ms | 364 MB | 392 MB |
| | default | texture files of pixels | 37 MB | 204 to 217 ms | the whole load | 266 MB | 4 | 0.4 ms | 434 MB | 392 MB |
| | `sdl-gpu` | `Assets.Texture`, as the SDK was | 34 MB | 778 to 798 ms | the whole load | 747 MB | 10 to 12 | 1.5 to 3.8 ms | 353 to 416 MB | 196 MB |
| | `sdl-gpu` | `Assets.Texture` | 34 MB | 705 to 748 ms | the whole load | 747 MB | 11 to 12 | 1.7 to 4.2 ms | 352 to 366 MB | 196 MB |
| | `sdl-gpu` | `Assets.Preload` | 34 MB | 262 to 399 ms | 23 to 47 ms | 749 MB | 4 | 2.2 to 2.8 ms | 783 to 798 MB | 196 MB |
| | `sdl-gpu` | prototype `fast` | 34 MB | 490 to 506 ms | the whole load | 226 MB | 4 to 6 | 0.7 to 1.0 ms | 241 to 306 MB | 196 MB |
| | `sdl-gpu` | texture files of pixels | 37 MB | 219 to 224 ms | the whole load | 266 MB | 4 | 0.4 to 0.5 ms | 311 to 312 MB | 196 MB |
| | `sdl-gpu` | texture files of BC7 | 20 MB | 98 to 127 ms | the whole load | 88 MB | 7 to 8 | 0.6 to 1.0 ms | 150 to 163 MB | 52 MB |
| | `sdl-gpu` | the pixels, stored plainly | 192 MB | 83 to 87 ms | the whole load | 192 MB | 3 | 0.4 to 0.8 ms | 307 to 308 MB | 196 MB |
| | `sdl-gpu` | BC7 blocks, stored plainly | 48 MB | 27 to 28 ms | the whole load | 48 MB | 3 | 0.2 to 0.3 ms | 156 to 173 MB | 52 MB |
| 12 × 4096² | default | `Assets.Texture`, as the SDK was | 115 MB | 2867 to 3243 ms | the whole load | 2879 MB | 12 to 14 | 17.5 to 23.7 ms | 1565 to 1648 MB | 776 MB |
| | default | `Assets.Preload`, as the SDK was | 115 MB | 1325 to 1646 ms | 137 to 263 ms | 2881 MB | 3 to 5 | 3.7 to 8.2 ms | 3158 to 3610 MB | 776 MB |
| | default | `Assets.Texture` | 115 MB | 2569 to 2744 ms | the whole load | 2879 MB | 12 to 14 | 17.0 to 24.0 ms | 1565 to 1648 MB | 776 MB |
| | default | `Assets.Preload` | 115 MB | 871 to 997 ms | 82 to 98 ms | 2881 MB | 4 to 5 | 3.8 to 7.9 ms | 3342 to 3364 MB | 776 MB |
| | default | prototype `fast` | 115 MB | 1440 to 1465 ms | the whole load | 883 MB | 6 | 2.1 to 2.2 ms | 1230 MB | 776 MB |
| | default | texture files of pixels | 127 MB | 677 to 697 ms | the whole load | 1021 MB | 4 | 1.2 to 1.5 ms | 1470 MB | 776 MB |
| | `sdl-gpu` | `Assets.Texture` | 115 MB | 2625 to 2829 ms | the whole load | 2879 MB | 12 to 15 | 19.7 to 27.6 ms | 1067 to 1109 MB | 772 to 774 MB |
| | `sdl-gpu` | `Assets.Preload` | 115 MB | 724 to 841 ms | 90 to 96 ms | 2881 MB | 3 to 5 | 4.5 to 5.9 ms | 2620 to 2977 MB | 772 MB |
| | `sdl-gpu` | texture files of pixels | 127 MB | 712 to 739 ms | the whole load | 1021 MB | 4 | 1.4 to 2.9 ms | 868 to 869 MB | 772 to 773 MB |
| | `sdl-gpu` | texture files of BC7 | 70 MB | 316 to 328 ms | the whole load | 333 MB | 7 | 0.7 to 0.9 ms | 268 MB | 196 to 197 MB |
| | `sdl-gpu` | BC7 blocks, stored plainly | 192 MB | 79 to 91 ms | the whole load | 192 MB | 3 | 0.4 to 0.9 ms | 306 to 307 MB | 196 to 197 MB |

Every row of the three rounds, the other prototype rows included, is in the campaign's output
(`results-levels.md`). Every collection in these runs was a full one (the three generations count the same), as
expected of allocations that are all large objects.

Read from it:

- **The direct path, as the SDK was:** 0.8 s for twelve 2048 × 2048 pages and about 3 s for twelve 4096 × 4096
  ones, on one thread, with 3.9 times the pixels in garbage and 11 to 14 full collections. The conversions fixed here
  take a tenth off (about 710 ms and 2.6 to 2.8 s).
- **`Preload` shortens the screen and does not make it smooth.** 0.3 to 0.4 s and 0.7 to 1.0 s with this change, but
  its longest frame is 21 to 47 ms with 2048 pages and 82 to 98 ms with 4096 ones (33 to 46 and 137 to 349 ms as
  the SDK was): each upload is on the game's thread and takes more than a 60 Hz frame, so a loading animation
  stutters. It also holds every decoded page at once: 0.8 to 1.0 GB of working set for 192 MB of textures, 2.6 to
  3.4 GB for 768 MB.
- **Texture files of pixels** load 3.1 to 4.1 times faster than `Assets.Texture` on either device (204 to 224 ms
  against 699 to 748; 677 to 739 against 2569 to 2829), with a third of the garbage, at the same graphics memory and
  a tenth more disk. They work on the default device.
- **Texture files of BC7** load 5.6 to 9 times faster (98 to 127 ms; 316 to 328 ms), in a quarter of the graphics
  memory (52 MB against 196; 196 against 772) and three fifths of the disk, on the one device that samples them.
  Most of what is left is inflating: the same blocks stored plainly load in 27 to 28 ms and 79 to 91 ms, at the
  price of 48 and 192 MB on disk. `TextureFile` writes either; the tool deflates.
- **Graphics memory** is what the operating system accounts to the process (DXGI, local plus non-local), read
  after every texture was drawn and presented. On `sdl-gpu` it is the texels and a few megabytes. On the default
  device it was twice the texels for the 2048 pages (392 MB for 192 MB) and once for the 4096 pages; see the
  hypotheses of section 10.

## 6. Two prototypes of a better direct loader (in the benchmark only)

Both read the same PNG files. They are measured, not shipped (ADR 0060 defers them: they change the core's loaders).

- **`lean`: no intermediate image.** The decoder's bytes are multiplied by alpha in place and handed to the device.
  Garbage for the twelve 2048 × 2048 pages falls from 747 MB to 294 MB (1.5 times the pixels instead of 3.9), the
  loading screen from about 710 to about 650 ms.
- **`fast`: a PNG decoder that inflates with the runtime's zlib** (`ZLibStream`) and undoes each row's filter
  straight into the output, for the PNGs art tools write (8-bit RGB or RGBA, not interlaced), answering "not mine"
  for anything else. It gives the same pixels as the engine's decoder on every file of the benchmark (checked on
  every run and by a test) and decodes a 2048 × 2048 page in 13 to 36 ms where StbImageSharp takes 30 to 59
  (1.7 to 2.3 times faster; 54 to 115 ms against 115 to 237 at 4096). With `lean` on top the loading screen takes
  470 to 506 ms and 226 MB of garbage, and twelve 4096 pages 1.4 to 1.5 s instead of 2.6 to 2.8. **Hypothesis**, not isolated: the gain is the inflate, which in
  StbImageSharp is a managed port and in the runtime is native code; a browser build would show whether it holds
  where .NET is interpreted.

## 7. The candidates of an offline step

### 7.1 Survey

Primary sources, read 2026-11-18. "Ran here" says whether this evaluation executed it.

| Candidate | What it produces | What a game needs at run time | Licence | Maintenance | Fit | Ran here |
| --- | --- | --- | --- | --- | --- | --- |
| MonoGame content pipeline: MGCB, the MGCB Editor, `MonoGame.Content.Builder.Task`, and since 3.8.5 a content builder project ([why](https://docs.monogame.net/articles/getting_started/content_pipeline/why_content_pipeline.html), [MGCB](https://docs.monogame.net/articles/getting_started/tools/mgcb.html), [editor](https://docs.monogame.net/articles/getting_started/content_pipeline/using_mgcb_editor.html), [builder](https://monogame.net/blog/2025-11-06-MonoGame-Content-Builder/)) | `.xnb` files: textures as colour, DXT, PVRTC, ETC1 or RGBA4444 by platform, optional mipmaps, premultiplied alpha; audio through ffmpeg; effects; models through Assimp | MonoGame's `ContentManager` and framework types; raw PNG, WAV and MP3 also load without it | MS-PL ([nuget.org](https://www.nuget.org/packages/MonoGame.Framework.Content.Pipeline), 3.8.6.1, 2026-11-04) | Active | A container of its own, bound to MonoGame's types; sixteen package dependencies (Assimp, FreeType, MojoShader, Basisu, Crunch, DXC, FFmpeg, FFprobe, SharpDX, BCnEncoder.Net, LibKTX...), several of them native tools per build platform | No |
| Basis Universal ([repository](https://github.com/BinomialLLC/basis_universal)) | `.ktx2` or `.basis` in ETC1S, UASTC and newer modes: one file transcoded at load to BC7, ASTC, ETC2 and others | The transcoder (C++, one source file; a WebAssembly build exists) | Apache-2.0 | Active (v2.5) | The only "one file for every GPU"; needs native code in every game. .NET: `BasisUniversal.NET` 1.0.0 (2026-06-02, [nuget.org](https://www.nuget.org/packages/BasisUniversal.NET)), an early binding whose native package covers Android, iOS, Linux and macOS, not Windows or WebAssembly | No |
| KTX 2.0 and Khronos KTX-Software ([specification](https://registry.khronos.org/KTX/specs/2.0/ktxspec.v2.html), [repository](https://github.com/KhronosGroup/KTX-Software)) | The `.ktx2` container: any Vulkan format, mip levels, supercompression (BasisLZ, Zstandard, ZLIB); `ktx` creates, encodes, validates | A reader; `libktx` (C, with JavaScript/WebAssembly, Java and Python wrappers, no C#) or one's own | Apache-2.0 (the tools) | Active, a Khronos standard | An open container that holds every format the device contract names. .NET readers: `LibKTX` 1.1.0 (managed, Unlicense, 2026-11-17), `Ktx2Sharp` 1.0.2 (bindings to libktx, 2024) | The format: yes (the engine's own reader and writer). The tools: no |
| BCnEncoder.Net ([repository](https://github.com/Nominom/BCnEncoder.NET), [nuget.org](https://www.nuget.org/packages/BCnEncoder.Net)) | BC1 to BC7 blocks (raw, or KTX 1 and DDS files); decodes them too | Nothing: blocks go to the GPU | MIT or Unlicense | 2.3.0 on 2026-03-05; one author; its README lists performance as open work | Managed code, one managed dependency (CommunityToolkit.HighPerformance): runs wherever the tool runs. MonoGame's pipeline depends on it | Yes |
| astcenc ([repository](https://github.com/ARM-software/astc-encoder)) | ASTC blocks (`.astc`, KTX) | Nothing | Apache-2.0 | Active (Arm) | Native tool and library, prebuilt for Windows, macOS, Linux; no NuGet binding found | No |
| DirectXTex and `texconv` ([repository](https://github.com/microsoft/DirectXTex)) | DDS with BC1 to BC7, mipmaps | A DDS reader | MIT | Active (Microsoft) | C++, Windows first; DDS holds no ASTC or ETC2 and no supercompression. .NET: `DirectXTexNet` 1.0.7 (2024) | No |
| StbDxtSharp ([nuget.org](https://www.nuget.org/packages/StbDxtSharp)) | BC1 and BC3 blocks | Nothing | Not declared in the package | 1.10.3, 2020 | Managed; older formats (BC1 has one bit of alpha, BC3 visible banding on gradients), unmaintained | No |
| Zstandard for .NET: `ZstdSharp.Port` ([nuget.org](https://www.nuget.org/packages/ZstdSharp.Port)) | Smaller, faster supercompression than ZLIB | The library, in every game | MIT | 0.8.8, 2026-04-29 | Managed; a new dependency of the engine for a gain not measured here | No |
| Mipmaps: made by the device (`SDL_GenerateMipmapsForGPUTexture`, [SDL wiki](https://wiki.libsdl.org/SDL3/SDL_GenerateMipmapsForGPUTexture)), or on the CPU | Levels of a texture | A device that declares them | zlib (SDL) | — | Not possible for block formats on the GPU; no device of the SDK declares mipmaps | The CPU chain: yes. The SDL call: no |
| Texture packing ahead of time (TexturePacker, Aseprite, Free Texture Packer) | An atlas image and its JSON or XML | Nothing new: the engine reads those files (ASSET-1) | per tool | — | Already the engine's path; packing at load costs 2 ms for ten images (ASSET-1) | No (ASSET-1 did) |

Browsers: WebGL offers compressed formats only as optional extensions that depend on the hardware, "no specific
formats are required", and "this advantage doesn't translate to network bandwidth" compared with PNG or JPEG
([MDN](https://developer.mozilla.org/en-US/docs/Web/API/WebGL_API/Compressed_texture_formats)). SDL_GPU's formats
are a capability per adapter, to be asked with `SDL_GPUTextureSupportsFormat`
([SDL wiki](https://wiki.libsdl.org/SDL3/SDL_GPUTextureFormat)).

### 7.2 Measured: disk, encoding, fidelity

BC7 blocks of the premultiplied pixels, by BCnEncoder.Net; "KTX2" columns are the files `TextureFile` writes
(deflated). PSNR is the peak signal-to-noise ratio of the decoded blocks against the pixels, over the four channels.

| Page | PNG | KTX2, pixels | KTX2, BC7 | BC7 blocks, plain | Encode, balanced | PSNR | Encode, fast | PSNR |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| sprites, 2048 | 530 KB | 593 KB | 411 KB | 4.0 MB | 9.1 s | 54.6 dB | 8.5 s | 54.5 dB |
| painted, 2048 | 3.8 MB | 4.4 MB | 2.8 MB | 4.0 MB | 11.5 s | 57.6 dB | 11.3 s | 57.6 dB |
| tiles, 2048 | 4.2 MB | 4.2 MB | 1.8 MB | 4.0 MB | 25.8 s | 47.7 dB | 12.1 s | 47.3 dB |
| sprites, 4096 | 2.2 MB | 2.5 MB | 1.7 MB | 16.0 MB | 31.7 s | 54.7 dB | 27.8 s | 54.5 dB |
| painted, 4096 | 14.9 MB | 17.2 MB | 10.9 MB | 16.0 MB | 41.5 s | 58.1 dB | 40.2 s | 58.1 dB |
| tiles, 4096 | 11.7 MB | 12.0 MB | 5.0 MB | 16.0 MB | 90.4 s | 47.8 dB | 43.4 s | 47.4 dB |

- **Disk.** Deflated BC7 is smaller than the PNG for all three kinds (0.43 to 0.78 of it); the pixels deflated
  without PNG's row filters are 1.0 to 1.16 of it; plain BC7 is one byte a pixel, more than a PNG of flat art and
  about the same as a PNG of detailed art.
- **Encoding** is slow with this encoder: 9 to 26 s for a 2048 × 2048 page, 32 to 90 s for a 4096 × 4096 one, on
  twelve threads. Its `fast` setting halves detailed art and barely moves the rest. An earlier run of `balanced`
  gave 7.3, 9.4 and 29.2 s for the three 2048 pages: the same order. Native encoders are known to be much faster;
  none was run.
- **Fidelity.** 47.6 to 58.1 dB. On a real device (`sdl-gpu`, Direct3D 12), the Examples sample's 256 × 256 emblem
  (hard outlines, a translucent shadow, a gradient) drawn from its BC7 file came back with a mean error of 0.29 of
  255 per channel and a worst value of 52, on the edge between an outline and transparency
  (`TextureFileDeviceTests`). The example's capture on each device was looked at (the BC7 file on `sdl-gpu`, the
  image on the default device): the two pictures cannot be told apart at the drawn size.

## 8. Mipmaps

**Cost.** A whole chain made on the CPU (a box filter over premultiplied pixels, in parallel) took 4 to 14 ms for a
2048 × 2048 page and 7 to 8 ms for a 4096 × 4096 one (2 to 7 and 8 to 9 ms in an earlier run), and adds a third to
the texture's memory. An offline step would save those milliseconds and add a third to the file.

**What one level loses.** The engine's reference device (the written sampling rule, on the CPU) drew each 2048 page
at a half, a quarter and an eighth of its size with linear filtering while it scrolled one texel at a time; each
frame was compared with the exact area average, and the same frames were drawn from the level of a chain that
matches the scale (what a sampler with mipmaps reads at these scales). Errors are 8-bit levels; "flicker" is the mean
change of a pixel between two frames beyond the change of the exact picture.

| Page | Drawn at | One level: RMS error | worst pixel | flicker | Matching mip level: RMS error | worst pixel | flicker |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| sprites | 1/2 | 0.00 | 0 | 0.00 | 4.46 | 79 | 0.77 |
| sprites | 1/4 | 8.13 | 99 | 1.36 | 6.03 | 98 | 0.75 |
| sprites | 1/8 | 16.62 | 174 | 2.27 | 8.66 | 101 | 0.78 |
| painted | 1/2 | 0.00 | 0 | 0.00 | 0.40 | 2 | 0.10 |
| painted | 1/4 | 0.57 | 2 | 0.29 | 0.49 | 2 | 0.09 |
| painted | 1/8 | 1.77 | 86 | 0.33 | 0.62 | 16 | 0.08 |
| tiles | 1/2 | 0.00 | 0 | 0.00 | 3.62 | 65 | 0.93 |
| tiles | 1/4 | 6.16 | 99 | 1.72 | 4.26 | 76 | 0.53 |
| tiles | 1/8 | 13.68 | 154 | 2.49 | 6.58 | 90 | 0.42 |

- At one half, linear filtering of one level **is** the exact average (each pixel sits between four texels): a
  mipmap gains nothing there, and blurs slightly when the picture sits between two of its texels.
- Below one half, one level skips texels: at a quarter and an eighth its error is 1.2 to 2.9 times the mip
  level's and it flickers 1.8 to 6 times more while scrolling. That is the shimmer ASSET-1 listed as a limitation.
- It is a graphics capability, not a content one: no device declares `Mipmaps`, the default device cannot have
  them, and making them needs no offline step. The guidance of ASSET-1 (author art at no more than twice its drawn
  size) keeps a game out of the range where they matter.

## 9. What was delivered

- **`TextureFile`** (`src/MicroEngine.Graphics/Devices/TextureFile.cs`): reads and writes KTX 2.0 (section 7.1's
  bounds), `CanCreate`, `Create`. Its tests (`TextureFileTests`, 11) cover the round trip, the header, level index
  and descriptor byte by byte, a file written by hand as another tool might, fourteen refusals with their reasons,
  the exact pixels on the reference device, premultiplying a straight file, and the typed error. The descriptor
  follows the KTX 2.0 specification, sections 3.9 and 3.10, as fetched for this work: one correction came from
  reading it (the plane size is kept in a deflated file; an older convention zeroed it), and the sample fields of
  the BC7 and RGBA descriptors are from the Khronos data format's definitions as remembered, not validated.
- **`microengine textures`** (`src/MicroEngine.Cli/Textures.cs`), with `TexturesTests` (5): the pixels format is the
  image exactly, BC7 decodes within stated bounds, up-to-date files are left alone, `--check`, and every refusal.
- **The example** (`samples/Examples/Examples/TextureFileExample.cs`) with the emblem as an image and as a BC7 file
  made by the tool, in the list and the tour, and `TextureFileDeviceTests` (3, in a window): `sdl-gpu` samples the
  file close to the image; the default device refuses the blocks and draws a file of pixels exactly; the example
  draws the same picture on both devices, from the file on one and from the image on the other.
- **The conversions** of section 4.1.
- **The benchmark** and its tests; the campaign script.
- **Documentation**: the guide page "Textures made ahead of time", the tool's page, the assets and graphics pages,
  the release notes, the architecture page, ADR 0060.

## 10. What was demonstrated, what is hypothesis, what is perceived

**Demonstrated** (measured or run here): every figure of sections 4 to 8 on this machine; that `sdl-gpu` on
Direct3D 12 samples a BC7 `TextureFile` correctly; that SDL's default device refuses it with the typed error and
draws a file of pixels exactly; that the tool's output is read back by the engine; that the conversions give the
same bytes; that graphics memory for BC7 is a quarter (52 MB against 196 to 208 MB, as the operating system
accounts it).

**Hypothesis** (proposed, not isolated):

- That the prototype decoder's gain comes from the runtime's native inflate.
- That the default device held about twice the texels of the 2048 pages (392 MB for 192 MB) and about once those of
  the 4096 pages (776 MB for 768 MB) because of how SDL's Direct3D 11 renderer or the driver stages uploads; the
  figure is DXGI's local plus non-local usage for the process, the same for images and for texture files. It was
  not investigated; the comparison that matters (BC7 against RGBA on `sdl-gpu`) does not depend on it.
- That browsers suffer most from decoding (interpreted, one thread) and so gain most from files of pixels. Nothing
  was run in a browser.
- That a machine with little graphics memory fails or swaps with HD art at four bytes a pixel. Computed, not
  reproduced: the reference adapter has 16 GB.

**What a developer would perceive.** Nothing changes unless they opt in: images load as before, a few milliseconds
faster per large page. Opting in is one command and a dozen lines per scene that draws such a texture, through the
batcher rather than the drawing API, and sprites from an atlas need their rectangles by hand. Encoding BC7 is slow
enough to notice (a minute for a 4096 page).

**What a player would perceive.** With twelve 2048 pages: a loading pause of about 0.7 s becomes
about a tenth of a second on a desktop graphics card with `sdl-gpu`, below what reads as a pause, and 0.2 s on the
default device with files of pixels. With twelve 4096 pages: 2.6 to 2.8 s become a third of a second, or 0.7 s. On a machine with little graphics memory, a game that did not
fit may fit (computed). BC7's error is not visible in the emblem's capture at its drawn size (half size, in a 320 × 180 canvas shown
at three times); on other art, and at full size, it was not judged by eye.

## 11. Not run, and limitations

- **A browser, a phone, Linux, Vulkan, macOS, an integrated GPU**, for every measurement. The device tests run
  where CI runs them (Linux with a software Vulkan, where BC7 support decides whether the block test is
  inconclusive).
- **Real art and other encoders.** One encoder, three kinds of synthetic art.
- **Files from other tools**, and Khronos's validator on the files written here.
- **raylib's device** with a texture file (the contract makes `Rgba8Unorm` mandatory; not run).
- **ASTC and ETC2** end to end: the reader maps their formats (tested on headers), no data was sampled.
- **Fidelity by eye** beyond the emblem; alpha-heavy art such as smoke or glows.
- **Fonts, atlases, meshes, audio** as subjects of an offline step.
- **The site build and the SDK's external checks** that need packed packages (see the task record).
- **Peak working set** includes the process's own start-up and is a ceiling, not a model of a game.
- The loading screens load pages one after another on the game's thread (or through `Preload`); a game could do
  better or worse than either.

What reopens each deferred item is in ADR 0060.
