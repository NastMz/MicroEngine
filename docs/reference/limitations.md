# Known limitations

This is the list of known limitations of MicroEngine and its games, each with the evidence it comes from. It
covers missing features, configurations that were never validated, and measured weaknesses of the current
build. Items move off this list only when a change and its validation are recorded.

## Missing features

| Limitation | Evidence |
| --- | --- |
| No visual editor, level editor or asset tool; content is code or files loaded through `ContentFolder` | [ADR 0002](../decisions/0002-code-first-content.md), [ADR 0004](../decisions/0004-external-asset-files.md) |
| Not distributed to external developers: no NuGet package, project templates (`dotnet new`), installers or versioned releases; all are planned | [Getting MicroEngine](../guide/getting-microengine.md) |
| No dedicated touch controls in the demos | [Platforms](../guide/platforms.md#phones-and-tablets) |
| No cloud saves; desktop saves are local files, browser saves are `localStorage` | [Persistence](../manual/persistence.md) |
| No ECS; plain entities sized for a few hundred objects per scene | [ADR 0003](../decisions/0003-scenes-entities-runtime.md), [Architecture](../architecture.md) |
| Input devices limited to keyboard, gamepad buttons and the left stick (`IInputSource`); no mouse or touch input in the engine | [`src/MicroEngine/Input/Input.cs`](../../src/MicroEngine/Input/Input.cs) |

## Platforms not validated

| Limitation | Evidence |
| --- | --- |
| Linux desktop: automated checks pass (build, tests, native rendering, keyboard, audio output on virtual devices), but manual play on real hardware is pending, so it is not supported yet | [Platforms](../guide/platforms.md#desktop-validation) |
| macOS desktop: under evaluation, not supported | [Platforms](../guide/platforms.md#desktop-validation) |
| Linux on ARM: no native raylib library in `Raylib-cs` | [Platforms](../guide/platforms.md#support-table) |
| Gamepads on Linux and macOS: not covered by any check | [Platforms](../guide/platforms.md#desktop-validation) |
| Safari not tested | [Platforms](../guide/platforms.md#support-table) |
| Chromium-based browsers other than Chrome (for example Edge) not claimed; automated browser tests cover Chrome and Firefox only | [Platforms](../guide/platforms.md#support-table) |
| Phones and tablets: the demos are not validated on real mobile devices | [Platforms](../guide/platforms.md#phones-and-tablets) |
| Gamepad in the browser validated manually in one Chromium-based browser with one controller only; no claim for other controllers or browsers | [Platforms](../guide/platforms.md#gamepads) |
| Blocked site storage tested in Firefox only, not Chrome | [Web support](../evaluations/web-support.md#persistence) |
| Producing browser packages is not available to external developers yet | [Getting MicroEngine](../guide/getting-microengine.md) |

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
| No versioned or published API; contracts may change, through recorded design decisions | [Contracts](contracts.md) |

See also [Status and limitations](../guide/status.md) for a shorter summary and [Platforms](../guide/platforms.md)
for the support table.
