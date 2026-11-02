# R3 — Validation, testing and packaging research for MicroEngine (ARCH-1, Phase I)

Industry research, read-only. Context read from the repository: `docs/ci.md`, ADR 0011 (per-system visual
fingerprints: size, exact hash, 32 x 18 mean-colour grid, fail above 3/255 per cell), ADR 0019 (one package per
backend, one shared `MicroEngineVersion`), ADR 0021 (`microengine` tool, hosts as templates), ADR 0023 (opt-in
`FrameProfiler`, display-paced time). Every material claim cites a URL; "not found" marks where a primary source
could not be confirmed.

---

## 1. Visual and rendering validation

### 1.1 Why pixel-exact comparison fails across GPUs, drivers and backends

- Rasterisation rules, anti-aliasing, blending rounding, texture filtering and floating-point precision differ between
  conforming implementations; conformance suites themselves therefore compare with thresholds and position
  tolerance rather than exact equality. dEQP/Vulkan CTS ship `intThresholdPositionDeviationCompare` (per-channel
  integer thresholds plus a per-axis position-deviation window) and `fuzzyCompare` (with a hard-coded minimum error of 4)
  ([tcuImageCompare.hpp](https://fuchsia.googlesource.com/third_party/vulkan-cts/+/refs/tags/vulkan-cts-1.1.5.2/framework/common/tcuImageCompare.hpp),
  [tcuImageCompare.cpp](https://fuchsia.googlesource.com/third_party/vulkan-cts/+/refs/tags/vulkan-cts-1.1.5.2/framework/common/tcuImageCompare.cpp),
  [tcuFuzzyImageCompare.cpp](https://android.googlesource.com/platform/external/deqp/+/68ec5efeb/framework/common/tcuFuzzyImageCompare.cpp)).
  Even the CTS hand-tunes thresholds (one commit raises 0.02 to 0.03:
  [deqp 3f450ec](https://android.googlesource.com/platform/external/deqp/+/3f450ec%5E%21/)) and has found its fuzzy
  thresholds "too low due to randomness" ([vulkan-cts commit](https://fuchsia.googlesource.com/third_party/vulkan-cts/+/refs/tags/opengl-es-cts-3.2.4.0%5E)).
- Software rasterisers differ from GPUs too: the Vulkan tutorial on CI warns SwiftShader rounds differently from real
  GPUs so pixel-exact comparisons may be off ([Vulkan CI render validation](https://docs.vulkan.org/tutorial/latest/ML_Inference/Desktop_Applications/04_ci_render_validation.html));
  a wgpu-based project reports lavapipe anti-aliasing differs from Metal goldens and widens a budget only there
  ([fenestra-shell testing](https://docs.rs/fenestra-shell/0.21.0/fenestra_shell/testing/)).
- Exactness is achievable only under tight constraints: Godot's rendering output was byte-reproducible single-threaded
  but varied slightly multithreaded ([godot-benchmarks #34](https://github.com/godotengine/godot-benchmarks/issues/34)).
  MicroEngine's own ADR 0011 already reached the same conclusion (Windows and Linux are never compared pixel for pixel).
- Yee and Newman's production-testing paper names the cost of pixel diffs directly: tiny invisible misalignments cause
  false positives and constant reference churn ([Yee 2004 talk](https://history.siggraph.org/wp-content/uploads/2022/12/2004-Talks-Yee_A-Perceptual-Metric-for-Production-Testing.pdf),
  [paper page](https://backiee.wasmer.app/http_pdiff_sourceforge_net/metric_html)).

### 1.2 Metrics

| Metric | What it measures | Strength | Weakness | Source |
| --- | --- | --- | --- | --- |
| Exact hash | byte equality | free, zero ambiguity | breaks on any driver change | ADR 0011 |
| Per-channel threshold + pixel count (WPT `fuzzy`, dEQP threshold) | max channel delta and how many pixels exceed it | simple, explainable, declarable per test | no perception; AA edges need high counts | [WPT reftests](https://web-platform-tests.org/writing-tests/reftests.html) |
| Position-deviation compare (dEQP) | match within N pixels | absorbs rasterisation-rule shifts | more CPU, can mask small geometry bugs | [tcuImageCompare](https://fuchsia.googlesource.com/third_party/vulkan-cts/+/refs/tags/vulkan-cts-1.1.5.2/framework/common/tcuImageCompare.hpp) |
| pixelmatch | per-pixel perceptual colour distance (OKLab/HyAB in current versions), threshold 0.1, AA pixels excluded by default (Vyšniauskas detector) | fast, AA-aware, tiny | per-pixel only; percentages computed by the caller | [mapbox/pixelmatch](https://github.com/mapbox/pixelmatch) |
| Yee pdiff | VDP-derived visibility model (LAB, scotopic correction) | designed for render-farm regression testing | older, parameters (FOV, gamma) to tune | [Yee 2004](https://history.siggraph.org/wp-content/uploads/2022/12/2004-Talks-Yee_A-Perceptual-Metric-for-Production-Testing.pdf), [perceptualdiff(1)](https://manpages.org/perceptualdiff) |
| SSIM / MS-SSIM | structural similarity (luminance, contrast, structure), MS over scales | well-known, robust to noise | tuned for natural-image quality, less for crisp 2D sprites/text | [Wang et al. 2004](https://ece.uwaterloo.ca/~z70wang/publications/ssim.html), [MS-SSIM 2003](https://www.cns.nyu.edu/~lcv/pubs/makeAbs.php?loc=Wang03), [UT LIVE](https://live.ece.utexas.edu/research/quality/ssim/) |
| Butteraugli | psychovisual distance + spatial map | good for compression | repository archived; authors call it a research tool tuned to a small quality range | [google/butteraugli](https://github.com/google/butteraugli) |
| NVIDIA FLIP (LDR-FLIP) | per-pixel map of differences seen when flipping between images; viewing-distance aware; pool as mean or histogram | built for rendering vs reference; used by wgpu | heavier; HDR needs HDR-FLIP | [NVIDIA FLIP](https://research.nvidia.com/publication/flip), [paper](https://research.nvidia.com/sites/default/files/node/3260/FLIP_Paper.pdf), [blog](https://developer.nvidia.com/blog/flip-a-difference-evaluator-for-alternating-images) |

Production precedent for FLIP: wgpu's test harness compares with `nv_flip`, pools the error map into a histogram, and
offers `ComparisonType::Mean(f32)` or `Percentile { percentile, threshold }`, suggesting values in [0.01, 0.1]; an
empty check list fails; on failure it writes `-actual.png` and a false-colour `-difference.png` named by backend,
adapter and driver ([wgpu tests/src/image.rs](https://raw.githubusercontent.com/gfx-rs/wgpu/trunk/tests/src/image.rs)).
Caveat seen in the same file: a missing or invalid reference is silently replaced by the current output, which is a
policy MicroEngine should not copy (it turns a lost baseline into a pass).

### 1.3 How real projects test rendering

- **Skia Gold / Chromium.** Baselines live in a service, not in Git, keyed by configuration (OS, architecture,
  backend CPU/GL/Vulkan); images are triaged positive, negative or untriaged; short ignore rules cover
  re-baselining ([Skia Gold](https://skia.org/docs/dev/testing/skiagold)). Default is exact matching **with multiple
  baselines per test**; inexact matching (fuzzy diff, Sobel filter) is opt-in per test via `matching_algorithm`,
  parameters are found with a search tool (`binary_search`, `local_minima`), and images that pass inexactly are
  auto-approved; trybot approvals are scoped to the CL until merged
  ([Chromium pixel testing with Gold](https://chromium.googlesource.com/chromium/src/+/HEAD/docs/gpu/gpu_pixel_testing_with_gold.md)).
  Some Chromium pixel tests check only a few pixels with handwritten `expected_colors` so they work on every GPU
  ([Chromium GPU testing](https://chromium.googlesource.com/chromium/src/+/HEAD/docs/gpu/gpu_testing.md)).
  Gold's auto-triage of non-exact matches records the algorithm as triage author
  ([buildbot commit](https://skia.googlesource.com/buildbot/+/68437497a17744fceb3c6daf9c710c5a70d84316)).
- **WPT / browser reftests.** Render a test and a reference built *without the feature under test*, compare
  pixel-for-pixel; `match`/`mismatch` links; `<meta name=fuzzy content="maxDifference=15;totalPixels=300">` with
  inclusive ranges and per-reference values ([WPT reftests](https://web-platform-tests.org/writing-tests/reftests.html)).
  The reftest idea removes stored baselines entirely: two renders by the same backend are compared.
- **wgpu.** Image tests per example with FLIP (above); GPU CI on hosted runners installs WARP, DXC, Agility SDK and Mesa on
  Windows, Mesa and the Vulkan SDK on Linux, and prints the adapters found (`.gpuconfig`)
  ([wgpu ci.yml](https://raw.githubusercontent.com/gfx-rs/wgpu/trunk/.github/workflows/ci.yml)); the README marks DX12
  over WARP on Windows as passing ([wgpu mirror README](https://gitee.com/Mirror711/wgpu)). wgpu's history also shows
  llvmpipe/lavapipe pain in CI (EGL context errors, segfaults on old Mesa) ([wgpu #1551](https://github.com/gfx-rs/wgpu/issues/1551)).
- **Mesa CI.** Conformance suites (VK-GL-CTS, Piglit) plus application **trace replay** on hardware farms (LAVA,
  bare-metal); per-driver expectation files including `-flakes.txt`; target of at most one spurious failure per farm per
  week; public traces fail the pipeline, restricted ones run allowed-to-fail ([Mesa CI docs](https://docs.mesa3d.org/ci/index.html)).
  Trace replay compares a **checksum per device** of the final frame; images are stored only on mismatch; formats
  apitrace, RenderDoc, GFXReconstruct ([tracie README mirror](https://android.googlesource.com/platform/external/mesa3d/+/01121c7/.gitlab-ci/tracie/),
  [local traces](https://fuchsia.googlesource.com/third_party/mesa/+/refs/heads/main/docs/ci/local-traces.rst)).
- **ANGLE / dEQP.** dEQP GLES2/3/3.1/EGL binaries run against each ANGLE backend, including SwiftShader-Vulkan, with
  per-configuration expectation files ([ANGLE + dEQP](https://android.googlesource.com/platform/external/angle/+/70ecc80f03/doc/dEQP.md)).
  VK-GL-CTS originated from dEQP and logs results (with images) as `.qpa` ([VK-GL-CTS](https://github.com/KhronosGroup/VK-GL-CTS)).
- **Filament.** `test/renderdiff`: golden images in a separate repository (`google/filament-assets`), updated by
  pushing to a branch there; a viewer script shows a PR's results ([Filament renderdiff](https://google.github.io/filament/dup/test_ci_renderdiff.html)).
- **Bevy.** An external example runner screenshots changed examples on Linux/Vulkan, macOS/Metal, iOS and Android
  and compares against the previous run, uploading to Pixel Eagle; Windows DX12 and web in progress
  ([Bevy example tester](https://example-runs.bevy.org/about.html), [runner commit](https://www.github.com/TheBevyFlock/bevy-example-runner/commit/e32e792658b0ebb79c9989596be1c6e76cebdd81));
  in-engine `CiTestingConfig` scripts camera moves and screenshots ([bevy #21243](https://github.com/bevyengine/bevy/pull/21243), [bevy_dev_tools](https://docs.rs/bevy_dev_tools)).
  Comparison algorithm not documented (not found).
- **Godot.** No formal visual-regression proposal found; the closest is a checksum script in godot-benchmarks
  ([#34](https://github.com/godotengine/godot-benchmarks/issues/34)).
- **MonoGame.** Visual tests are `Game`s with a `FrameCompareComponent` and `IFrameComparer`s producing a 0–1
  similarity against a shared `Constants.StandardRequiredSimilarity`; first run writes captured frames for adoption;
  XOR diffs on failure ([MonoGame testing wiki](https://github.com/dellis1972/MonoGame/wiki/MonoGame-Testing)). Old
  source; current class names not verified. FNA: no test approach found.
- **Unity Graphics Test Framework.** `ImageAssert` plus automatic collection/deployment of reference images
  ([com.unity.testframework.graphics](https://github.com/Unity-Technologies/com.unity.testframework.graphics)); details
  of per-platform layout not confirmed from the page fetched.
- **bgfx.** No primary source on screenshot tests found.

### 1.4 Geometric invariants and property tests

Images need not be compared against a stored image to catch most regressions:

- Chromium's `expected_colors` (sample a few known regions) is GPU-portable by construction
  ([gpu_testing.md](https://chromium.googlesource.com/chromium/src/+/HEAD/docs/gpu/gpu_testing.md)).
- WPT reftests compare two renders of the same backend, so cross-GPU differences cancel
  ([WPT reftests](https://web-platform-tests.org/writing-tests/reftests.html)).
- dEQP's position-deviation compare formalises "same coverage within N pixels"
  ([tcuImageCompare](https://fuchsia.googlesource.com/third_party/vulkan-cts/+/refs/tags/vulkan-cts-1.1.5.2/framework/common/tcuImageCompare.hpp)).

Engine-level invariants that follow from these (inference, no single source): non-background bounding box of a sprite
within ±1 px of its transform; coverage (count of non-clear pixels) within a band; colour histogram buckets;
mismatch tests ("drawing X must change the frame"); equivalence tests ("a sprite drawn at (x,y) equals a sprite drawn
at (0,0) under a camera offset of (−x,−y)"). These run on every backend without baselines.

### 1.5 Golden policies seen in practice

| Policy | Example |
| --- | --- |
| Per-config baselines, multiple accepted per test, triage | Skia Gold ([docs](https://skia.org/docs/dev/testing/skiagold), [Chromium](https://chromium.googlesource.com/chromium/src/+/HEAD/docs/gpu/gpu_pixel_testing_with_gold.md)) |
| Per-device checksums, images on mismatch only | Mesa traces ([tracie](https://android.googlesource.com/platform/external/mesa3d/+/01121c7/.gitlab-ci/tracie/)) |
| Tolerance declared in the test, with ranges | WPT fuzzy ([reftests](https://web-platform-tests.org/writing-tests/reftests.html)) |
| One shared similarity constant | MonoGame ([wiki](https://github.com/dellis1972/MonoGame/wiki/MonoGame-Testing)) |
| Goldens from one reference platform; budget widened only on the software rasteriser | fenestra-shell ([docs](https://docs.rs/fenestra-shell/0.21.0/fenestra_shell/testing/)) |
| Goldens in a separate asset repo | Filament ([renderdiff](https://google.github.io/filament/dup/test_ci_renderdiff.html)) |
| Previous-run comparison (change detection, not correctness) | Bevy ([example tester](https://example-runs.bevy.org/about.html)) |

---

## 2. Testing game engines in general

### 2.1 Determinism, lockstep and replay

- Floating-point determinism holds only with one compiler, one instruction set and controlled settings; it may not
  even hold between debug and release ([Gaffer: floating point determinism](https://gafferongames.com/post/floating_point_determinism),
  [Gaffer: deterministic lockstep](https://gafferongames.com/post/deterministic_lockstep/)). Cross-platform lockstep is
  possible with constraints and tooling ([Frontier GDC 2024](https://media.gdcvault.com/gdc2024/Slides/GDC+slide+presentations/Pollard_Bradley_CrossPlatformDeterminism+2024-03-26+09.34.19.pdf)).
- Factorio: a mode computes a CRC of the whole map every tick; replay throws at the first mismatching tick and dumps a
  readable save for diffing ([FFF #47](https://factorio.com/blog/post/fff-47)); multiplayer compares CRCs per tick
  ([FFF #55](https://cf-www.factorio.com/blog/post/fff-55)); integration tests are their main determinism regression
  tool, plus floating-point-consistency unit tests across architectures ([FFF #60](https://factorio.com/blog/post/fff-60));
  a threading desync from 2017 surfaced only when simulating fewer cores ([FFF #415](https://direct.factorio.com/blog/post/fff-415)).

Implication: hash *simulation state* per step (not pixels) and keep the first-divergence report. MicroEngine's fixed
60 Hz headless scripts are already the right substrate.

### 2.2 Performance regression detection

- Mozilla Perfherder: two-sample t-test over ~12 revisions before and ~11 after as confidence, then a magnitude
  threshold (default 2%, 5% for noisy suites); human sheriffs triage; very noisy tests get removed
  ([perftest in a nutshell](https://firefox-source-docs.mozilla.org/testing/perfdocs/perftest-in-a-nutshell.html),
  [sheriffing](https://wiki.mozilla.org/TestEngineering/Performance/Sheriffing), [Alert FAQ](https://wiki.mozilla.org/Performance_sheriffing/Alert_FAQ),
  [dataset paper](https://arxiv.org/pdf/2503.16332)). A 2026 study reports notable false-positive and miss rates and
  blames normality/equal-variance assumptions ([arXiv 2606.18377](https://arxiv.org/pdf/2606.18377)).
- MongoDB replaced a ~10% fixed threshold with E-Divisive change-point detection; useful notifications went from 1% to
  67% without missing the threshold system's real changes ([Daly et al. ICPE 2020](https://arxiv.org/pdf/2003.00584));
  follow-ups: Hunter ([arXiv 2301.03034](https://arxiv.org/pdf/2301.03034)), open-sourced as Apache Otava
  ([arXiv 2505.06758](https://arxiv.org/pdf/2505.06758v1)).
- Implication: wall-clock frame time on shared hosted runners is a trend signal, not a PR gate. Deterministic counters
  (allocations per frame, GC counts, draw calls, steps per frame) are exact and can gate; MicroEngine's `FrameProfiler`
  already records allocations and collections (ADR 0023).

### 2.3 Audio

- Offline rendering into a buffer is the standard testable path (Web Audio `OfflineAudioContext` tests in WPT:
  [WPT offlineaudiocontext](https://searchfox.org/mozilla-central/source/testing/web-platform/tests/webaudio/the-audio-api/the-offlineaudiocontext-interface);
  old Blink layout tests assert "Rendered data matches expected results":
  [Chromium test](https://chromium.googlesource.com/chromium/src/+/8e783c4e29d927ba7e139ef46d9adff1f38a8923/third_party/WebKit/LayoutTests/webaudio/offlineaudiocontext-promise-basic-expected.txt)).
  Numeric tolerances for buffer comparison: not found in primary sources.
- pluginval renders a plugin with known state and compares against a reference WAV, emits JUnit/JSON, and runs
  headless with `--skip-gui-tests` ([pluginval](https://github.com/Tracktion/pluginval), [v2.0.0-beta3](https://github.com/Tracktion/pluginval/releases/tag/v2.0.0-beta3)).
- Spectral comparison (FFT band energy, onset times) is the natural tolerance for resampled/mixed output (inference).
  MicroEngine already records audio from a virtual PulseAudio sink in native tests (ADR 0011).

### 2.4 Input latency

Hardware click-to-photon measurement (NVIDIA LDAT: luminance sensor and instrumented mouse; built into Reflex
monitors) ([LDAT](https://developer.nvidia.com/nvidia-latency-display-analysis-tool), [reviewer toolkit](https://www.nvidia.com/en-us/geforce/news/nvidia-reviewer-toolkit/)).
Not CI-able; in CI only the engine-visible part (input event timestamp to present call, in frames) can be checked.

### 2.5 Conformance kits for pluggable backends

- `Microsoft.Extensions.DependencyInjection.Specification.Tests`: xUnit suite a third-party container references and
  subclasses, overriding `CreateServiceProvider`; used by Autofac, DryIoc, Castle Windsor
  ([NuGet](https://nuget.org/packages/Microsoft.Extensions.DependencyInjection.Specification.Tests/10.0.0-preview.1.25080.5)).
- EF Core `Specification.Tests` / `Relational.Specification.Tests`: provider writers subclass each test class, may opt
  out of classes not ready ([writing a provider](https://learn.microsoft.com/ef/core/providers/writing-a-provider)).
- Khronos CTS: one suite, many implementations, expectations per implementation ([VK-GL-CTS](https://github.com/KhronosGroup/VK-GL-CTS),
  [Mesa CI](https://docs.mesa3d.org/ci/index.html)).
- pluginval strictness levels 1–10, default 5 as "host compatibility" floor ([pluginval](https://github.com/Tracktion/pluginval)).

---

## 3. .NET packaging for engines with native dependencies

- **RID-specific assets.** Native binaries go in `runtimes/{rid}/native`, managed RID-specific ones in
  `runtimes/{rid}/lib/{tfm}`; if compatible `runtimes` assets exist, `lib` is ignored at runtime
  ([NuGet multi-targeting](https://learn.microsoft.com/en-us/nuget/create-packages/supporting-multiple-target-frameworks)).
- **Bindings vs natives split.** ppy ships one package per SDL library (`ppy.SDL3-CS`, `_image`, `_ttf`, `_mixer`), with
  bindings regenerated from upstream and natives built by a workflow, covering win/osx/linux variants, iOS and Android
  (API 24+) ([ppy/SDL3-CS](https://github.com/ppy/SDL3-CS)). Silk.NET binding packages depend on separate
  `Ultz.Native.*` native packages (e.g. `Silk.NET.GLFW` → `Ultz.Native.GLFW`) ([Silk.NET.GLFW](https://packages.nuget.org/packages/Silk.NET.GLFW/2.22.0),
  [Ultz.Native.GLFW](https://www.nuget.org/packages/ultz.native.glfw), [Silk.NET #436](https://github.com/dotnet/Silk.NET/issues/436)).
- **WebAssembly.** Native code joins the .NET wasm runtime through `NativeFileReference` (C/C++ or Emscripten
  objects/archives), linked by Emscripten at build; prebuilt archives must use the **same Emscripten version** as the
  runtime; NuGet `browser-wasm` natives are not referenced automatically
  ([Blazor wasm native deps](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-native-dependencies)); a
  CoreCLR-wasm relink path requires `-fwasm-exceptions` and `-msimd128` ([dotnet/runtime #126946](https://github.com/dotnet/runtime/pull/126946)).
- **Android/iOS.** Optional SDK workloads installed with `dotnet workload install`, version-pinnable with `@`
  ([dotnet workload install](https://learn.microsoft.com/dotnet/core/tools/dotnet-workload-install)).
- **NativeAOT/trimming.** `IsAotCompatible` turns on trim, single-file and AOT analyzers and implies `IsTrimmable`;
  Microsoft recommends both the library flag and a trimming test app ([prepare libraries for trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming));
  the GDK guidance for games prefers source generators over reflection ([GDK NativeAOT for gaming](https://learn.microsoft.com/en-us/gaming/gdk/docs/gdk-dev/pc-dev/tutorials/get-started-with-custom-engine/native-aot-for-gaming?view=gdk-2604));
  MonoGame's console guide calls reflection the main obstacle ([MonoGame consoles](https://docs.monogame.net/articles/getting_started/preparing_for_consoles.html),
  [MonoGame #8104](https://www.Github.com/MonoGame/MonoGame/issues/8104)).
- **How others distribute.** MonoGame: `MonoGame.Framework.<Platform>` packages plus `MonoGame.Content.Builder.Task`
  and dotnet tools (`dotnet-mgcb`, editor) ([MGCB task](https://www.nuget.org/packages/MonoGame.Content.Builder.Task/),
  [3.8 packages](https://community.monogame.net/t/monogame-3-8-prerelease-packages-are-up-on-nuget/12708)).
  FNA: official natives as `fnalibs` (last 2021) and a community fork with NuGet ([fnalibs](https://www.nuget.org/packages/fnalibs),
  [FNA.NET.NativeAssets](https://www.nuget.org/packages/FNA.NET.NativeAssets)); FNA's own guidance not confirmed. Stride:
  many packages versioned in lockstep (`Stride.Engine` depends on `Stride.Rendering`, `Stride.Shaders.Compiler` at the
  same version, net10.0) ([Stride.Engine](https://www.nuget.org/packages/Stride.Engine/)). Murder: no NuGet package,
  git submodule (snapshot, [mirror](https://gitee.com/long33/murder)).
- **Abstractions vs implementations.** `Microsoft.Extensions.Logging.Abstractions` defines interfaces implemented by
  `Microsoft.Extensions.Logging` and others ([NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions));
  library guidance: minimise dependencies, avoid upper bounds, always set minimums
  ([library guidance: dependencies](https://learn.microsoft.com/en-us/dotNET/standard/library-guidance/dependencies)).
- **Versioning many packages.** Central Package Management (`Directory.Packages.props`, NuGet 6.2+) centralises
  consumer versions ([CPM wiki](https://github.com/NuGet/Home/wiki/Centrally-managing-NuGet-package-versions),
  [overview](https://timdeschryver.dev/blog/directorypackagesprops-a-solution-to-unify-your-nuget-package-versions));
  lockstep producer versioning as in Stride and `Microsoft.Extensions.*`.

---

## 4. CI shapes for GPU-less runners

| Rasteriser | API | Status | Source |
| --- | --- | --- | --- |
| lavapipe (Mesa) | Vulkan | Khronos-recognised Vulkan 1.3 (2022) | [Phoronix](https://www.phoronix.com/news/Lavapipe-Vulkan-1.3-Official) |
| llvmpipe (Mesa) | OpenGL | OpenGL 4.5 conformant | [Khronos](https://www.khronos.org/news/permalink/mesas-llvmpipe-is-opengl-4.5-conformant) |
| SwiftShader | Vulkan | conformant Vulkan 1.3, tested with dEQP, ICD via loader | [google/swiftshader](https://github.com/google/swiftshader) |
| WARP | D3D11/D3D12 | "fully conformant" software rasteriser; D3D12 FL 12_0/12_1, newer previews add SM 6.7 | [Learn: WARP](https://learn.microsoft.com/en-us/windows/win32/direct3darticles/directx-warp), [DirectX blog](https://devblogs.microsoft.com/directx/announcing-warp-preview-with-shader-model-6-7-support/) |
| Mesa on Windows | llvmpipe/lavapipe | built by Mesa CI | [Mesa MR 9518](https://gitlab.freedesktop.org/mesa/mesa/-/merge_requests/9518) |
| raylib `rlsw` | raylib's rlgl (GL 1.1-style) | new in raylib 6.0, CPU only, no user code change, 30–60 fps for basic apps; SDL/RGFW/DRM backends adapted | [raylib HISTORY](https://raw.githubusercontent.com/raysan5/raylib/master/HISTORY.md) |

SDL3 GPU enumerates software Vulkan drivers such as lavapipe unless
`SDL_PROP_GPU_DEVICE_CREATE_VULKAN_REQUIRE_HARDWARE_ACCELERATION_BOOLEAN` is set
([SDL_CreateGPUDeviceWithProperties](https://manpages.debian.org/testing/libsdl3-doc/SDL_CreateGPUDeviceWithProperties.3.en.html)),
so an SDL_GPU backend can run on hosted Linux runners with Mesa installed; one user reports SDL_GPU 2D output
pixel-identical to a software reference on D3D12 and Vulkan for deterministic scenes (anecdotal:
[Simutrans forum](https://forum.simutrans.com/index.php?msg=213807)). wgpu is the precedent for installing WARP and Mesa
on hosted GitHub runners ([ci.yml](https://raw.githubusercontent.com/gfx-rs/wgpu/trunk/.github/workflows/ci.yml)).

Matrix cost control seen: Mesa limits runtime (~10 min per farm, `DEQP_FRACTION` to run a fraction) and quarantines
flakes in expectation files ([Mesa CI](https://docs.mesa3d.org/ci/index.html)); Chromium/Skia separate exact-match
per-config baselines from tolerance (Gold). MicroEngine already runs once per content with evidence reuse (`docs/ci.md`).

---

## 5. Recommendations for MicroEngine

### 5.1 Validation tiers

| Tier | What | Runs | Gate | Comparison |
| --- | --- | --- | --- | --- |
| T0 Simulation | headless scripted playthroughs; hash of simulation state per step; replay and first-divergence report (Factorio) | every PR, every OS | yes | exact per OS; cross-OS exact only where proven (floats: Gaffer) |
| T1 Draw stream | record the backend-neutral command list the engine submits (sprites, transforms, blend, text runs) per frame; golden text files | every PR, one OS | yes | exact (it is engine output, no driver involved) — the Mesa-trace idea applied one layer up |
| T2 Reference renderer | the headless platform's CPU rasteriser, integer/fixed math, renders T1 streams | every PR | yes | exact or WPT-style fuzzy declared per test; this is the single "truth" image |
| T3 Real backends on software rasterisers | SDL renderer/SDL_GPU on lavapipe (Linux) and WARP (Windows); raylib on llvmpipe or `rlsw` | PR when a backend or renderer changes | yes | FLIP mean + percentile against T2 output, plus invariants (bbox, coverage, histogram, reftest equivalences) |
| T4 Real devices | GPUs, macOS/Metal, Android, iOS, browsers | scheduled/manual (`platforms.yml`) | no (report) | Gold-style: per-config baselines, multiple accepted, triage |

Tradeoffs: T1 adds a recording seam to the renderer interface (it also documents the backend contract and is cheap);
T2 is extra code to maintain but gives one truth image instead of N per-OS references; T3 depends on Mesa/WARP
versions, so pin them and print adapter/driver in results (wgpu `.gpuconfig`).

### 5.2 Metric and tolerance policy

- Keep ADR 0011's grid fingerprint as the cheap smoke test (it fits 340 KB, catches gross regressions).
- Add FLIP (LDR) for backend-vs-reference with wgpu's two-number policy (mean and a high percentile); start in
  [0.01, 0.1] and tune on observed spread; pixelmatch-style per-pixel counts for crisp 2D/text where FLIP is too lenient.
- Tolerance lives in the test, as ranges (WPT), never a global constant edited to make CI green (MonoGame's single
  constant is the anti-example for multi-backend); widening a tolerance requires a recorded reason.
- A missing baseline is a failure that writes the candidate for adoption (MonoGame), never a silent pass (wgpu's
  fallback).
- Baselines keyed by (backend, adapter, OS); allow several accepted digests per key (Gold); store images outside Git
  history if they grow (Filament's assets repo), keep hashes in Git (Mesa checksums).
- Always emit actual, expected, false-colour diff and the adapter/driver string.

### 5.3 Backend conformance kit

Publish `MicroEngine.Platform.Conformance` (xUnit, like DI/EF specification tests): abstract classes a backend
subclasses by providing a factory; suites for window/lifecycle, input (scripted events), audio (offline buffer +
reference WAV with spectral tolerance, pluginval-style), timing (`DisplayRate`, pacing), and rendering (T1→backend
render vs T2, invariants). Add capability reporting so unsupported features skip with a reason, an expectations file
per backend for known failures/flakes (Mesa/ANGLE), and strictness levels (pluginval) so a third-party backend can
claim "level N". Tradeoff: the kit becomes a published contract (versioned with the SDK, Alpha caveats apply).

### 5.4 Performance and latency

Gate on deterministic counters (allocations, GC, draw calls, steps per frame) from `FrameProfiler`; track wall time
with change-point detection (E-Divisive/Otava) on a dedicated or consistently-sized runner, alerting, not blocking;
measure input-to-present in frames in-engine; hardware click-to-photon only manually.

### 5.5 Packaging and modules

- Keep core without natives (ADR 0019). Split each backend into bindings/managed backend and a `*.Native` package
  with `runtimes/{rid}/native` (ppy, Silk.NET/Ultz.Native) so natives can be updated, replaced or omitted (e.g. a game
  that ships its own SDL build). Consider reusing `ppy.SDL3-CS` natives instead of building them, after checking its
  RID coverage and license.
- If a renderer abstraction grows, a separate `MicroEngine.Abstractions`/`Rendering.Abstractions` package lets
  third-party backends depend on contracts only (Microsoft.Extensions pattern); cost: one more package and a
  binary-compat surface.
- Browser natives: ship as `NativeFileReference` items injected by the `microengine` tool, pinned to the runtime's
  Emscripten version, checked in CI.
- Mark every library `IsAotCompatible` and keep a trimmed/AOT test app in CI; prefer source generators over reflection.
- Version all packages in lockstep from `MicroEngineVersion` (already); tell consumers to use CPM; publish the
  conformance kit at the same version.

---

## Sources

- https://android.googlesource.com/platform/external/angle/+/70ecc80f03/doc/dEQP.md
- https://android.googlesource.com/platform/external/deqp/+/3f450ec%5E%21/
- https://android.googlesource.com/platform/external/deqp/+/68ec5efeb/framework/common/tcuFuzzyImageCompare.cpp
- https://android.googlesource.com/platform/external/mesa3d/+/01121c7/.gitlab-ci/tracie/
- https://arxiv.org/pdf/2003.00584
- https://arxiv.org/pdf/2301.03034
- https://arxiv.org/pdf/2503.16332
- https://arxiv.org/pdf/2505.06758v1
- https://arxiv.org/pdf/2606.18377
- https://backiee.wasmer.app/http_pdiff_sourceforge_net/metric_html
- https://cf-www.factorio.com/blog/post/fff-55
- https://chromium.googlesource.com/chromium/src/+/8e783c4e29d927ba7e139ef46d9adff1f38a8923/third_party/WebKit/LayoutTests/webaudio/offlineaudiocontext-promise-basic-expected.txt
- https://chromium.googlesource.com/chromium/src/+/HEAD/docs/gpu/gpu_pixel_testing_with_gold.md
- https://chromium.googlesource.com/chromium/src/+/HEAD/docs/gpu/gpu_testing.md
- https://community.monogame.net/t/monogame-3-8-prerelease-packages-are-up-on-nuget/12708
- https://devblogs.microsoft.com/directx/announcing-warp-preview-with-shader-model-6-7-support/
- https://developer.nvidia.com/blog/flip-a-difference-evaluator-for-alternating-images
- https://developer.nvidia.com/nvidia-latency-display-analysis-tool
- https://direct.factorio.com/blog/post/fff-415
- https://docs.mesa3d.org/ci/index.html
- https://docs.monogame.net/articles/getting_started/preparing_for_consoles.html
- https://docs.rs/bevy_dev_tools
- https://docs.rs/fenestra-shell/0.21.0/fenestra_shell/testing/
- https://docs.vulkan.org/tutorial/latest/ML_Inference/Desktop_Applications/04_ci_render_validation.html
- https://ece.uwaterloo.ca/~z70wang/publications/ssim.html
- https://example-runs.bevy.org/about.html
- https://factorio.com/blog/post/fff-47
- https://factorio.com/blog/post/fff-60
- https://firefox-source-docs.mozilla.org/testing/perfdocs/perftest-in-a-nutshell.html
- https://forum.simutrans.com/index.php?msg=213807
- https://fuchsia.googlesource.com/third_party/mesa/+/refs/heads/main/docs/ci/local-traces.rst
- https://fuchsia.googlesource.com/third_party/vulkan-cts/+/refs/tags/opengl-es-cts-3.2.4.0%5E
- https://fuchsia.googlesource.com/third_party/vulkan-cts/+/refs/tags/vulkan-cts-1.1.5.2/framework/common/tcuImageCompare.cpp
- https://fuchsia.googlesource.com/third_party/vulkan-cts/+/refs/tags/vulkan-cts-1.1.5.2/framework/common/tcuImageCompare.hpp
- https://gafferongames.com/post/deterministic_lockstep/
- https://gafferongames.com/post/floating_point_determinism
- https://gitee.com/Mirror711/wgpu
- https://gitee.com/long33/murder
- https://github.com/KhronosGroup/VK-GL-CTS
- https://github.com/NuGet/Home/wiki/Centrally-managing-NuGet-package-versions
- https://github.com/Tracktion/pluginval
- https://github.com/Tracktion/pluginval/releases/tag/v2.0.0-beta3
- https://github.com/Unity-Technologies/com.unity.testframework.graphics
- https://github.com/bevyengine/bevy/pull/21243
- https://github.com/dellis1972/MonoGame/wiki/MonoGame-Testing
- https://github.com/dotnet/Silk.NET/issues/436
- https://github.com/dotnet/runtime/pull/126946
- https://github.com/gfx-rs/wgpu/issues/1551
- https://github.com/godotengine/godot-benchmarks/issues/34
- https://github.com/google/butteraugli
- https://github.com/google/swiftshader
- https://github.com/mapbox/pixelmatch
- https://github.com/ppy/SDL3-CS
- https://gitlab.freedesktop.org/mesa/mesa/-/merge_requests/9518
- https://google.github.io/filament/dup/test_ci_renderdiff.html
- https://history.siggraph.org/wp-content/uploads/2022/12/2004-Talks-Yee_A-Perceptual-Metric-for-Production-Testing.pdf
- https://learn.microsoft.com/dotnet/core/tools/dotnet-workload-install
- https://learn.microsoft.com/ef/core/providers/writing-a-provider
- https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-native-dependencies
- https://learn.microsoft.com/en-us/dotNET/standard/library-guidance/dependencies
- https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming
- https://learn.microsoft.com/en-us/gaming/gdk/docs/gdk-dev/pc-dev/tutorials/get-started-with-custom-engine/native-aot-for-gaming?view=gdk-2604
- https://learn.microsoft.com/en-us/nuget/create-packages/supporting-multiple-target-frameworks
- https://learn.microsoft.com/en-us/windows/win32/direct3darticles/directx-warp
- https://live.ece.utexas.edu/research/quality/ssim/
- https://manpages.debian.org/testing/libsdl3-doc/SDL_CreateGPUDeviceWithProperties.3.en.html
- https://manpages.org/perceptualdiff
- https://media.gdcvault.com/gdc2024/Slides/GDC+slide+presentations/Pollard_Bradley_CrossPlatformDeterminism+2024-03-26+09.34.19.pdf
- https://nuget.org/packages/Microsoft.Extensions.DependencyInjection.Specification.Tests/10.0.0-preview.1.25080.5
- https://packages.nuget.org/packages/Silk.NET.GLFW/2.22.0
- https://raw.githubusercontent.com/gfx-rs/wgpu/trunk/.github/workflows/ci.yml
- https://raw.githubusercontent.com/gfx-rs/wgpu/trunk/tests/src/image.rs
- https://raw.githubusercontent.com/raysan5/raylib/master/HISTORY.md
- https://research.nvidia.com/publication/flip
- https://research.nvidia.com/sites/default/files/node/3260/FLIP_Paper.pdf
- https://searchfox.org/mozilla-central/source/testing/web-platform/tests/webaudio/the-audio-api/the-offlineaudiocontext-interface
- https://skia.googlesource.com/buildbot/+/68437497a17744fceb3c6daf9c710c5a70d84316
- https://skia.org/docs/dev/testing/skiagold
- https://timdeschryver.dev/blog/directorypackagesprops-a-solution-to-unify-your-nuget-package-versions
- https://web-platform-tests.org/writing-tests/reftests.html
- https://wiki.mozilla.org/Performance_sheriffing/Alert_FAQ
- https://wiki.mozilla.org/TestEngineering/Performance/Sheriffing
- https://www.Github.com/MonoGame/MonoGame/issues/8104
- https://www.cns.nyu.edu/~lcv/pubs/makeAbs.php?loc=Wang03
- https://www.github.com/TheBevyFlock/bevy-example-runner/commit/e32e792658b0ebb79c9989596be1c6e76cebdd81
- https://www.khronos.org/news/permalink/mesas-llvmpipe-is-opengl-4.5-conformant
- https://www.nuget.org/packages/FNA.NET.NativeAssets
- https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions
- https://www.nuget.org/packages/MonoGame.Content.Builder.Task/
- https://www.nuget.org/packages/Stride.Engine/
- https://www.nuget.org/packages/fnalibs
- https://www.nuget.org/packages/ultz.native.glfw
- https://www.nvidia.com/en-us/geforce/news/nvidia-reviewer-toolkit/
- https://www.phoronix.com/news/Lavapipe-Vulkan-1.3-Official
