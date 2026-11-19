# CONTENT-1: research optional offline asset processing and reusable tooling

Objective: decide with measurements whether MicroEngine integrates, prototypes, defers or rejects an optional offline
asset pipeline, and deliver what that decision needs for an external developer. Issue NastMz/MicroEngine#113. Branch
`feat/content-1` (from `37c264c`). Delivery strategy: `ask-on-risk` (no push, pull request or release without the
owner). Test runner: `dotnet test MicroEngine.slnx -c Release`. Evaluation:
[`docs/evaluations/content-1.md`](../../docs/evaluations/content-1.md). Decision:
[ADR 0060](../../docs/decisions/0060-optional-texture-files.md).

## Specs

- S1 "Establish measurable shortcomings of current direct loaders before introducing infrastructure."
- S2 "Evaluate reusable tools, including MonoGame's content tooling and independent C#/native libraries, explaining
  generated formats and runtime dependencies."
- S3 "Compare load times, disk size, CPU allocations/GC, GPU memory, native/web/mobile packaging, licenses and
  maintenance."
- S4 "Preserve direct-file loading by default; do not require a proprietary build artifact."
- S5 Done when: "Evidence supports a decision to integrate, prototype, defer or reject an **optional** pipeline. No
  mandatory implementation is approved here."
- S6 The brief that opened this task (the project's rule for research issues): judge by whether an external developer
  could build that game today with the published SDK, not by what the four demos use; research ends in the smallest
  coherent optional piece where the measurements support one, and in a recorded defer or reject, with what reopens it,
  where they do not; the decision is an ADR, Status Accepted; the evaluation states what it covered and what it did
  not, and separates what was demonstrated, what is a hypothesis and what a developer or a player would perceive;
  `games/**` is not changed; nothing is pushed, published or released.

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Map the direct loaders, the device contract and what a game can reach today | S1 | inline | done | L2; evaluation §2 |
| T2 `benchmarks/ContentBench`: procedural HD content, the direct loaders measured (time, disk, allocations and GC, texture memory) | S1 S3 | inline | done | `9d0c1f3`; `Benchmarks.Tests` 57/57, the four new ones included; evaluation §3 to §6 (L3) |
| T3 Survey reusable tools from primary sources; measure the candidates that run here | S2 S3 | inline | done | evaluation §7, §8 (L4) |
| T4 Decision; the smallest optional piece the measurements support, with tests | S4 S5 S6 | inline | done | `eccf29f` (the conversions), `e88ed64` (`TextureFile`), `c459b86` (`microengine textures`), `7935936` (the example, the device tests); RED by mutation (L5, L6) |
| T5 Evaluation, ADR, guide, release notes, architecture, decision index | S5 S6 | inline | done | the documentation commit (L7) |
| T6 Verification commands | S6 | inline | done | L8 |

Routes: every task inline. No writer trigger held (each task depended on the previous one's result), and timings
needed the machine to this session's one campaign at a time, which rules out parallel work.

## Log

- L1 (2026-11-18) The issue, verbatim:

  > CONTENT-1 — Research optional offline asset processing and reusable tooling
  >
  > ## Opportunity
  >
  > The Asset Store covers loading, sharing and resource lifetimes, not necessarily advanced build-time processing.
  > Large HD/3D games may benefit from GPU-ready compressed textures, mipmaps, conversion and other optional
  > preprocessing.
  >
  > ## Investigate
  >
  > - Establish measurable shortcomings of current direct loaders before introducing infrastructure.
  > - Evaluate reusable tools, including MonoGame's content tooling and independent C#/native libraries, explaining
  >   generated formats and runtime dependencies.
  > - Compare load times, disk size, CPU allocations/GC, GPU memory, native/web/mobile packaging, licenses and
  >   maintenance.
  > - Preserve direct-file loading by default; do not require a proprietary build artifact.
  >
  > ## Done when
  >
  > Evidence supports a decision to integrate, prototype, defer or reject an **optional** pipeline. No mandatory
  > implementation is approved here.
  >
  > **Status:** Idea / research candidate.

  The brief that opened this task adds S6.
- L2 (2026-11-18) Mapping. An image becomes a texture through four buffers of four bytes a pixel (the decoder's
  output, its copy into the image's `Color` array, a copy back into bytes, premultiplied in place). The device
  contract already names BC7, ETC2 and ASTC and a mip level count; `sdl-gpu` maps BC7 and ASTC and declares them per
  adapter; no device declares mipmaps and nothing uploads a level other than 0; SDL's default device and raylib's
  declare the 8-bit RGBA and BGRA formats only. A scene reaches the device and draws device textures with a
  `Renderer2D` of its own (GRAPHICS-1), so a BC7 texture was already possible for a game that brought an encoder and
  a reader. The repository's own images are 4.4 MB in all: the demos cannot show the problem. ADR 0034 reserves an
  import seam (`IAssetImporter`) that nothing calls.
- L3 (2026-11-18) The benchmark and the direct loaders. `benchmarks/ContentBench` draws three kinds of HD art at
  three sizes and writes PNG as an art tool does. A 2048 × 2048 page took 44 to 79 ms to load and 51 to 69 MB of
  garbage; twelve of them 780 ms and 392 MB of graphics memory on the default device (DXGI). Of an upload's 11 to
  17 ms the device needed 3. Two prototypes in the benchmark: no intermediate image (garbage 3.9 to 1.5 times the
  pixels) and a PNG decoder on the runtime's inflate (1.7 to 2.3 times faster, the same pixels). The machine was
  never idle (other sessions' and the desktop's programs, 6 to 15%); recorded beside each group.
- L4 (2026-11-18) Survey and candidates. MonoGame's pipeline writes a container of its own and has sixteen
  dependencies; Basis Universal needs a native transcoder for which .NET has an early binding without Windows or
  WebAssembly binaries; KTX 2.0 is an open container; BCnEncoder.Net is a managed BC7 encoder (and a dependency of
  MonoGame's pipeline). Measured: BC7 at 47.6 to 58.1 dB, 9 to 26 s a 2048 page, files smaller than the PNG when
  deflated; a mipmap chain in 2 to 14 ms on the CPU; one level is exact at half size and flickers 1.8 to 6 times
  more than a mip level below it.
- L5 (2026-11-18) Decision (ADR 0060): no content build; integrate optional texture files in KTX 2.0; reject
  offline mipmaps; defer the rest with what reopens each. `TextureFile` was written test-first (the tests failed to
  compile, then all passed on the first build). The CLI's first BC7 expectation of mine was wrong (a worst error of
  6 levels on a picture whose colour changes in three directions inside each block; BC7 gave 11), replaced by a
  bound on alpha, a worst value and a signal-to-noise ratio. On the device the worst pixel was 52 of 255, on an
  outline's edge, with a mean of 0.29; the bound says so. The first loading-screen run with the change overlapped a
  build of mine and was discarded.
- L6 (2026-11-18) Three corrections made by evidence. (1) The KTX specification, read after the writer was
  written, says a deflated file keeps `bytesPlane0`; the writer zeroed it (an older convention); the test's
  expectation was corrected with the code. (2) RED by mutation (seven mutations, each caught and restored) left the
  restored sources with their old dates, so the next solution build kept the mutated binaries and the first full
  test run failed on three of them; the sources were touched and everything rebuilt before the run that counts.
  (3) That run's one real failure: the example loaded its image through `Assets.Texture` when its scene opened, and
  the engine reported a load during play (the gallery's hitch test caught the message on the shared error output);
  the scene now makes and owns both of its textures.
- L7 (2026-11-18) Documentation: the evaluation, ADR 0060, the guide page "Textures made ahead of time" with its
  code quoted from the example, the tool's page, the assets and graphics pages, the release notes, the
  architecture page, the decision index, AGENTS, the third-party notices, `docs/api` regenerated (one new page, one
  changed). Not done, with reasons: `docs/migrations.md` (nothing breaks).
- L8 (2026-11-18) Verification on the final tree. `dotnet build MicroEngine.slnx -c Release`: 0 warnings, 0 errors.
  `dotnet test MicroEngine.slnx -c Release`: 15 test projects, 2,374 passed, 121 skipped by their own conditions on
  this machine (98 of them the Box2D backend's), 0 failed, no test host aborted; the three device tests of the
  texture file ran in a window and passed. `node site/scripts/snippets.mjs`: 188 quoted blocks match.
  `node site/scripts/api-reference.mjs --check`: 493 pages, up to date. `node sdk/version.mjs check`: every active
  reference names 0.6.0-alpha.1. The site's repository-documentation tests: 16 of 17 (the other needs the site's
  packages, not installed here). Not run: the site build and its checks of the built pages, the SDK's
  external-project checks (`sdk/tests`: packing the SDK and the tool, building the samples outside the repository),
  packaging, and anything in a browser, on a phone, on Linux or on macOS. Scratch (kept): the campaign's content and
  outputs under the temporary folder named in the task's report.
- L9 (2026-11-18) Independent verification (a separate clone; risk items 3 and 5) and its one correction batch.
  Demonstrated and unchanged: both shared conversions give the base's bytes (65,536 pairs, odd lengths); the tool
  packs with its two encoder assemblies and the installed tool converts both formats; `ktx-parse` 2.0.0 accepts
  eleven files written here and `Parse` reads three it wrote; ten existing commands print what the base printed.
  Blocker, fixed with `Parse_RefusesASizeItCannotHold_BeforeReservingItsMemory` (failed first): `TextureFile.Parse`
  counted a level's bytes in 32 bits, so a header of about a hundred bytes announcing 30000 × 30000 threw an
  overflow error instead of the documented one, 32768 × 32768 and 65536 × 65536 were accepted as empty textures, and
  a deflated 16384 × 16384 reserved a gigabyte before reading anything. The count is now 64-bit, a level larger than
  an array is refused, and a deflated level that announces more than deflate can produce from its stored bytes is
  refused before its memory is reserved. Advisories fixed: two images that differ only by extension became one file
  silently (now refused, nothing written; `WhatCannotBeConverted_IsRefusedWithTheReason` failed first); `Color`'s
  byte layout, which `PixelImage.ToRgba` relies on, is pinned by `AColorIsFourBytesInRedGreenBlueAlphaOrder`; the
  guide's device table no longer says Vulkan samples BC7 without asking the device; the encoding times agree across
  the ADR, the evaluation and the guide; the ADR and evaluation say which other reader was run. Left as they are: a
  BC7 run that fails on one image keeps the files it had already written; a trailing unknown option reports that it
  needs a value (the shared option parser); raylib and browsers with a file of pixels rest on the device contract
  (`Rgba8Unorm` is mandatory), not on a run.
