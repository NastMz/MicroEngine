# GAME-POLISH: player experience, application identity and multiplatform quality

Date: 2026-11-01. Branch `feat/game-polish` (from `main` at d2c7081). Task record:
[`odd/tasks/game-polish.md`](../../odd/tasks/game-polish.md). Manuals: [Input](../manual/input.md),
[UI widgets](../manual/ui.md), [Application identity](../guide/app-identity.md).

Evidence words are used strictly: **run** means executed on the development machine (Windows 11) and its output or
captures looked at; **emulator** means run on the Android emulator on that machine (`Medium_Phone_API_36.1`, Android
16, x86_64) and looked at through screenshots and screen recordings; **browser** means the built-in Chromium browser
pane; **CI** means GitHub-hosted runners from this branch; **desk** means judged from source or documentation only.
No physical phone, iPhone, Mac or Linux desktop was used: nothing here claims a hand test on one.

## 1. Problems found and what was done

| # | Problem | Found by | Fixed | Where |
| --- | --- | --- | --- | --- |
| 1 | Android launcher listed demos as "MicroEngine" with Android's generic icon while the app info showed the right name | emulator: Courtyard Key and Trail Duel installed from pre-SDK-1 builds; `aapt dump badging` of those APKs: `launchable-activity label='MicroEngine' icon=''` | Builds from main already had the right label; the remaining gap was the icon. Updated in place by the new packages (same ids, same key); the SDK and demo package checks now read the launcher entry, the field that hid the defect | SDK tool, `sdk/tests/external.mjs`, `packaging/verify.mjs` |
| 2 | Launchers shrank the square icon onto a white plate (Android 8+), one density only | emulator (Neon Rift, SDK-1 build) | Adaptive and round icons, every density, background from the icon's edge or `MicroEngineIconBackground` | SDK tool |
| 3 | Black screen between the tap and the game; on Android the system splash ended ~0.5 s before SDL drew anything | emulator, screen recording | Start screen in the game's colour and image on every platform, Android 12+ launch screen held until the engine's first frame (8 s cap) | engine, SDK tool |
| 4 | Black bars around the start screen and the game on phones | emulator | `GameSettings.Letterbox`; the start screen uses its own colour | engine |
| 5 | Dialogue and hints named keys ("Press Z", "PRESS Z TO START", "Z: play X: title", HUD slots "Z"/"C", "Esc: back") whatever the device | run (captures), source | `ActionPrompts`, used by the four demos | engine, demos |
| 6 | Controls screens listed keys only, or keys and pad names mixed (Neon Rift), or had a separate hand-written touch list | run | One screen per demo built from prompts: keyboard, Xbox, PlayStation, Nintendo, touch | demos |
| 7 | Courtyard Key's Controls panel overflowed: key caps ran into their labels, the last row past the border | run (captures) | Columns sized from the measured text | Courtyard Key |
| 8 | Trail Duel's Controls panel: a long key list ran into the descriptions (after 6) | run | Description column after the widest key text | Trail Duel |
| 9 | `DialogueBox` silently dropped lines beyond `Lines` | source, documented as such | A long page continues on the next confirm | engine |
| 10 | `BitmapFont.Wrap` left a word wider than the line overflowing | source, documented | Broken between characters | engine |
| 11 | Achievement banners covered Courtyard Key's boss bar and room name | run (captures) | `AchievementToasts.Bottom`; Courtyard Key's banners rise from the bottom | engine, Courtyard Key |
| 12 | `AudioMixer.PlayMusic` restarted a track from the beginning when asked for it again while it faded out | source, then a failing test | The fade is cancelled and the track continues | engine |
| 13 | Touch controls looked the same in every demo | emulator | Per-demo looks through `TouchStyle` | engine, demos |
| 14 | Linux desktop entries did not name the window class, so an installed entry's icon did not belong to the running game | desk (freedesktop spec, SDL app id) | `StartupWMClass=<ApplicationId>` | SDK tool |
| 15 | The browser loading page was black with grey text before the game | browser | The game's colour, image, title, progress and optional branding, readable on any colour; web app manifest | browser host |
| 16 | Prompt text on the loading page and branding overlapped the page's Fullscreen button at phone width | browser, 375×812 | Branding raised above the page's buttons on touch screens | browser host |
| 17 | iOS packages built since SDK-1 could not be restored ("Ambiguous project name") and, once restored, stopped at start ("Attempting to JIT compile ... GameEntry:Create" in AOT-only mode) | CI (platform runs 41378384819, 41379731953) | The iOS host assembly is `<Game>.iOS`, so the app no longer holds two assemblies named after the game | SDK tool |

Audited and found fine: the four demos' scripted runs (36 scripts, every one exits 0 with isolated saves and its
debug start), pause menus, dialogue paging in the existing texts, Trail Duel's battle and shrine flows, Neon Rift's
stage and ending, Meadow Run's levels and credits. Not audited by ear: sound and music were reviewed in code only
(item 12); no duplicated or cut sounds were found there.

## 2. Technologies evaluated

| Candidate | What it offers | Verdict |
| --- | --- | --- |
| SDL 3 `SDL_GetGamepadType` | The connected pad's family (Xbox, PlayStation, Switch) | **Used** for prompt names on SDL |
| Browser gamepad ids | Vendor ids in the pad's name (`054c` Sony, `057e` Nintendo) | **Used** on raylib (browsers) through the name |
| Controller prompt image packs (for example CC0 glyph sets) | Images for every button of every pad | **Not used**: an asset dependency in every game for a few glyphs; key caps, face buttons and touch rings are drawn with the built-in font and shapes, and `PromptText.DrawGlyph` lets a game draw its own |
| Android 12 SplashScreen (platform API, `windowSplashScreen*` theme attributes, a pre-draw listener) | System launch screen in the app's colours, kept until the app is ready | **Used**. AndroidX `core-splashscreen` would add a Java dependency for what the platform API does on 12+; older versions show the window background |
| iOS `UILaunchScreen` (Info.plist, iOS 14+) | Launch colour and image from the asset catalog, no storyboard | **Used** (desk and CI build; no device) |
| Launch storyboards (iOS) | Arbitrary layouts | Not needed for a colour and an image |
| Web app manifest | Name, icon, colours, orientation when installed to a home screen | **Used** |
| Adaptive icons (Android 8+) | Launcher-masked icons with a background layer | **Used** |
| freedesktop `StartupWMClass` | Matches a window to its desktop entry | **Used** |

## 3. Engine and SDK, reusable by every game

- `ActionPrompts` (`Game.Prompts`): device, bindings, remapping, touch layout, pad family; `Format`, `Text`, `TextAll`,
  `Directions`, `SetLabel`, `Override`; the `prompts` script command. `PromptText` glyphs. `IInputSource.PadStyle`.
- `DialogueBox`: paging and `Prompts`. `BitmapFont.Wrap`: long words. `○ □ △` glyphs.
- `TouchStyle`, `ITouchSkin`, `DefaultTouchSkin`, `TouchControlView`, `OverlayShapes`, `IOverlayCanvas.Image` (SDL,
  raylib, recording canvas). Hit testing, finger tracking and actions untouched.
- Start screen: `AppIdentity.Splash*`, `StartScreen`, `RunOptions.StartScreen` and `FirstFrame`,
  `GameSettings.Letterbox`, `IPlatform.Letterbox`; `MicroEngineSplash`, `SplashColor`, `SplashImage`,
  `SplashBranding`, `IconBackground` (checked by the build: ME0006-ME0008).
- `PixelImage.Resized`, `FitInside`; `AchievementToasts.Bottom`; `AudioMixer.PlayMusic` fade fix.
- SDK tool: Android adaptive and round icons, launch theme, held launch screen; iOS launch screen; browser start
  screen and manifest; Linux `StartupWMClass`.

Compatibility: no public member was removed or changed incompatibly. New interface members have defaults. Behaviour
changes (dialogue paging, long-word wrapping, the start screen on by default, the music fade) are in the
[migration notes](../migrations.md#next-release); the SDK version was not changed and nothing was published.

## 4. Application identity by platform

| Platform | Name | Icon | Other metadata | Evidence |
| --- | --- | --- | --- | --- |
| Windows | `ProductName`/`FileDescription` "Meadow Run", window title | executable icon read back from the `.exe` | version 1.0.0 | run: packaged, version info and icon extracted |
| Linux | desktop entry `Name`, window app id (SDL) | `icon.png`, window icon | `StartupWMClass`, id file name | run: packaged, entry read; not run on a Linux desktop here |
| macOS | bundle name, Dock | `AppIcon.icns` | bundle id, version, ad hoc signature | unchanged by this branch; checked by the SDK test on CI |
| Android | launcher, recents, app info | adaptive and round icons | package id unchanged (update installs over earlier builds), launch screen | emulator, `aapt` |
| iOS | home screen | asset catalog icon | bundle id, `UILaunchScreen` | CI: the simulator app installs, starts, renders, plays sound, takes scripted touches and resumes after suspension (platform run 41381187401); not on an iPhone |
| Web | tab title | favicon, manifest icon | manifest name, colours, orientation; start screen | browser (loading page and start); the tool's web build needs the `wasm-tools` workload, not installed here: CI |

The four demos keep distinct names, ids (`dev.microengine.<game>`) and icons; their start-screen colours come from
their own palettes.

## 5. Tests

| Suite | Result |
| --- | --- |
| `MicroEngine.Tests` | 200 passed, 1 skipped (prompts, device changes, remapping, pad families, scripts, glyphs, dialogue paging, long words, styles, skins, images, hit areas unchanged, start screen settings, order and opt-out, image resizing, the music fade) |
| Meadow Run, Courtyard Key, Trail Duel, Neon Rift | 47, 31, 88, 33 passed |
| Samples, desktop, raylib | 9, 8 passed; raylib 1 passed, 4 skipped (native window not available) |
| Site build and content checks | 204 pages, quoted code blocks equal their sources |
| Visual references | only intended captures re-recorded: 8 per desktop reference (Windows, Linux raylib and SDL, macOS), 26 on Android and in the iOS simulator (touch names); every other capture unchanged |
| Platform run 41381187401 | macOS, Android (four games), iOS simulator (four games: installed, started, rendered, audio, touch, lifecycle), web demos on emulated phones |

## 6. Limitations and follow-ups

- iOS ran in the simulator on CI only; nothing ran on an iPhone, and the launch screen was not looked at.
- Linux and macOS identity were not looked at on those desktops here.
- Sound was reviewed in code, not listened to.
- Start-up on the emulator takes about 4 s from launch to Meadow Run's title (logcat); the start screen covers it.
  Worth measuring on a phone in PERF-1.
- The Android launch screen shows the adaptive icon in Android's circle; a game wanting another shape sets
  `MicroEngineSplashImage`.
- Prompt names for pads on raylib come from the pad's name; unknown pads use Xbox names.
