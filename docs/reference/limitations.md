# Known limitations

This is the list of known limitations of MicroEngine and its games, each with the evidence it comes from. It
covers missing features, configurations that were never validated, and measured weaknesses of the current
build. Items move off this list only when a change and its validation are recorded.

## Missing features

| Limitation | Evidence |
| --- | --- |
| No visual editor, level editor or asset tool; content is code or files loaded through `ContentFolder` | [ADR 0002](../decisions/0002-code-first-content.md), [ADR 0004](../decisions/0004-external-asset-files.md) |
| No published NuGet packages or project templates (`dotnet new`) yet: the engine is built from its public repository. `dotnet pack` already produces the four packages (SDK-1 is prepared, not published) | [SDK status](../guide/sdk.md) |
| Touch exists only as the SDL platform's generic on-screen controls (they press gamepad buttons); no per-game layouts, no touch in the browser builds | [Backends](../manual/backends.md#sdl-3) |
| No cloud saves; desktop saves are local files, browser saves are `localStorage` | [Persistence](../manual/persistence.md) |
| No ECS; plain entities sized for a few hundred objects per scene | [ADR 0003](../decisions/0003-scenes-entities-runtime.md), [Architecture](../architecture.md) |
| Input devices limited to keyboard, gamepad buttons and the left stick (`IInputSource`); no mouse, pointer or touch API for games (taps reach them as gamepad buttons) | [`src/MicroEngine/Input/Input.cs`](../../src/MicroEngine/Input/Input.cs) |

## Platforms not validated

| Limitation | Evidence |
| --- | --- |
| Windows: the SDL builds (now the default) have not been played by hand; their rendering and audio device were checked on real hardware | [Platforms](../guide/platforms.md#desktop-validation) |
| Linux desktop: checked on hosted runners and in Ubuntu 22.04, Debian 12 and Fedora 41 containers (software rendering, virtual display and sound server); no real GPU, Wayland session or person yet | [Platforms](../guide/platforms.md#desktop-validation) |
| macOS: checked on hosted runners (paravirtual GPU, Metal); no person has played on a Mac, no system keyboard check | [Platforms](../guide/platforms.md#desktop-validation) |
| Android: all four games checked on the x86_64 emulator only; no phone or tablet, arm64 never run | [Platforms](../guide/platforms.md#phones-and-tablets) |
| iOS: two games checked in the simulator only; touch not checked; no iPhone or iPad | [Platforms](../guide/platforms.md#phones-and-tablets) |
| Linux on ARM: no native raylib library in `Raylib-cs` | [Platforms](../guide/platforms.md#support-table) |
| Gamepads on Linux and macOS: not covered by any check | [Platforms](../guide/platforms.md#desktop-validation) |
| Safari not tested | [Platforms](../guide/platforms.md#support-table) |
| Chromium-based browsers other than Chrome (for example Edge) not claimed; automated browser tests cover Chrome and Firefox only | [Platforms](../guide/platforms.md#support-table) |
| Browsers on phones and tablets: the web demos are not validated there and have no touch controls | [Platforms](../guide/platforms.md#phones-and-tablets) |
| Gamepad in the browser validated manually in one Chromium-based browser with one controller only; no claim for other controllers or browsers | [Platforms](../guide/platforms.md#gamepads) |
| Blocked site storage tested in Firefox only, not Chrome | [Web support](../evaluations/web-support.md#persistence) |
| Producing browser packages is not available to external developers yet | [Getting MicroEngine](../guide/getting-microengine.md) |

## Distribution of the demos

| Limitation | Evidence |
| --- | --- |
| No iOS download: a public iPhone build needs Apple signing (a paid Apple Developer membership); a simulator build cannot run on a device | [Downloads](../guide/downloads.md#ios) |
| macOS packages are signed ad hoc, not notarized: macOS blocks the first launch until **Open Anyway** is chosen in Privacy & Security | [Downloads](../guide/downloads.md#macos) |
| Windows packages are not code-signed: SmartScreen may warn about an unknown publisher | [Downloads](../guide/downloads.md#windows) |
| Linux packages need glibc 2.34 or later (raylib alternative: 2.38) | [packaging/README.md](../../packaging/README.md#linux-distributions) |
| Android: the demo publisher is not registered with Google; in Brazil, Indonesia, Singapore and Thailand certified devices install the APK only through the advanced flow for unverified developers or `adb` (worldwide from 2027, as announced) | [Downloads](../guide/downloads.md#android) |
| Android APKs are offered only once the release signing key is configured as repository secrets | [packaging/README.md](../../packaging/README.md#android-signing) |
| No Intel Mac, 32-bit or ARM Linux packages | [packaging/targets.json](../../packaging/targets.json) |
| Hosted Windows runners have no sound device and no GPU: the CI check of a Windows package proves start, rendering on WARP and exit; audio was checked on real hardware | [PRODUCT-1](../evaluations/product-1.md) |
| Desktop packages carry about 150 KB of unused code in browser builds and the raylib library in native ones (`--platform raylib`) | [PLATFORM-3](../evaluations/platform-3.md#6-remaining-limitations) |

## Not yet done by a person

| Limitation | Evidence |
| --- | --- |
| Browser keyboard input in the automated tests comes from automation (CDP and WebDriver BiDi); the demos were also validated manually in the browser | [Platforms](../guide/platforms.md#browser) |
| No continuous human playthrough to the end of each game in a browser; endings were reached by autopilot (Neon Rift) and developer checkpoints with scripts (the others) | [Web support](../evaluations/web-support.md#results-per-game) |

Whole playthroughs from a new game to the credits are covered by the games' headless `PlaythroughTests`.

## Measured weaknesses

| Limitation | Evidence |
| --- | --- |
| Neon Rift's first browser download is 11.9 MB with Brotli (16.4 MB raw), mostly OGG music | [Web support](../evaluations/web-support.md#size-and-performance) |
| raylib's web audio uses the deprecated `ScriptProcessorNode` (a console warning in Chrome); sound stops while the tab is hidden, and the game pauses with it | [Web support](../evaluations/web-support.md#audio) |
| Below 12 drawn frames per second the five-steps-per-frame cap slows the game instead of skipping, on desktop and in browsers | [Web support](../evaluations/web-support.md#size-and-performance), [Architecture](../architecture.md#the-frame) |
| One Firefox scripted-capture run under heavy machine load diverged from desktop in three games; repeated alone, all captures matched twice. Cause not found | [Web support](../evaluations/web-support.md#chrome-and-firefox) |
| F11 cannot toggle the game's fullscreen in a browser (it stays the browser's own); use Alt+Enter or the Fullscreen button | [web/README.md](../../web/README.md#how-the-host-works) |
| Chrome's audio-blocked path was not observed for real under automation (CDP navigation counts as user activation) | [Web support](../evaluations/web-support.md#audio) |

## Storage limits in browsers

| Limitation | Evidence |
| --- | --- |
| `localStorage` holds about 5 MB per origin, shared with everything on that origin | [web/README.md](../../web/README.md#saves-in-the-browser) |
| Browsers may evict site data under storage pressure; private windows discard it; Safari may delete script-written storage of sites not visited for seven days | [web/README.md](../../web/README.md#saves-in-the-browser) |

When storage is full or blocked, the game keeps running and the page says progress could not be saved or will
not be kept; `SaveStore` never overwrites a slot it could not read
([Persistence](../manual/persistence.md)).

## API stability

| Limitation | Evidence |
| --- | --- |
| SDK Alpha (0.x): public APIs may change incompatibly between Alpha versions; changes are recorded in decisions and [migration notes](../migrations.md) | [SDK status](../guide/sdk.md) |

See also [Status and limitations](../guide/status.md) for a shorter summary and [Platforms](../guide/platforms.md)
for the support table.
