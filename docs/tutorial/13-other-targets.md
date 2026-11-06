# 13. Other platforms

Gem Garden never names a platform: `Program.cs` hands the game to a launcher, and the launcher (or the package's host)
picks the platform. The same compiled game therefore runs on another backend, in a browser and on a phone without a
code change. This step tries each.

## Another backend on the desktop

Desktop games carry two platforms: SDL 3 (the default) and raylib. Ask for raylib:

```bash
microengine run --platform raylib
microengine run -- --list-platforms
```

`--list-platforms` prints the platforms of the desktop launcher and why one cannot run on this machine:

```text
sdl: SDL 3: window, 2D renderer (Direct3D, Metal, OpenGL, Vulkan or software), input, touch and audio
raylib: raylib 6.0: GLFW window, OpenGL renderer, input and miniaudio sound
sdl-software: SDL 3 with its software renderer
headless: no window, devices or sound; one step per frame, for tests and CI
```

Players can choose too: `GemGarden.exe --platform sdl-software` runs on a machine whose graphics driver fails with the
default renderer. The `MICROENGINE_PLATFORM` environment variable does the same without a command-line option. Why two
backends, and how to add your own: [Backends and platforms](../manual/backends.md).

## In a browser

```bash
dotnet workload install wasm-tools
microengine package web
```

The first command installs .NET's WebAssembly build tools once per machine (on Windows, from an administrator
terminal if .NET was installed for all users). The package is a folder and a ZIP of the same files:
`dist/GemGarden-1.0.0-web/`, a static site with an `index.html` titled "Gem Garden", the icon as favicon, the .NET
runtime compiled to WebAssembly, SDL 3, the game and its `Content`.

- Upload the folder to any static host: GitHub Pages, Netlify, itch.io (as an HTML game), an S3 bucket, your own
  server. The server must send `.wasm` files as `application/wasm`.
- Opening `index.html` straight from disk does not work: browsers block the runtime's requests from `file://`. Test it
  through a local web server.
- The page waits for a click or key before starting the game, so the music can play; keys that would scroll the page
  stay with the game.
- The best score is saved in the browser's `localStorage`: no code change, `SaveStore` picks the browser's storage by
  itself. Private windows forget it when they close.
- On phones and tablets the touch controls of step 4 appear: the floating joystick and the A and II buttons.

Embedding the game in your own page with an iframe, and following its state from the page:
[Publishing on the web](../examples/embedding.md).

## On Android

Building an APK needs the `android` workload, an Android SDK (`ANDROID_HOME`) and Java 17 or newer;
`microengine doctor android` checks all three and says how to install what is missing.

```bash
microengine doctor android
microengine package android --debug-key
```

`--debug-key` signs the APK with this machine's debug key, which is enough to install it on your own devices:

```bash
adb install dist/GemGarden-1.0.0-android.apk
```

`adb` comes with the Android SDK's platform tools; you can also copy the APK to the phone and open it there. The game
runs on SDL 3 in landscape (`MicroEngineOrientation`), with the touch controls; a Bluetooth gamepad or keyboard works
too. When the player switches apps, the game pauses itself (see [step 10](10-states.md)).

To publish an APK, sign it with your own key instead of the debug key: create a keystore once, keep it safe and out of
the repository, and point `ANDROID_KEYSTORE` and its password variables at it ([Signing and secrets](../guide/signing.md)).
The tool builds APKs, which install directly and through stores that accept them; Google Play requires App Bundles
(`.aab`), which it does not build yet.

## macOS and iOS

Both are built on a Mac:

- `microengine package macos` builds a `.app` for Apple silicon in a ZIP, signed ad hoc or with your Developer ID.
- `microengine package ios-simulator` builds a `.app` to try in Xcode's Simulator; `microengine package ios` builds an
  `.ipa` for devices, signed with your Apple certificate and provisioning profile.

Requirements, signing and notarization: [Packaging a game](../guide/packaging.md) and
[Signing and secrets](../guide/signing.md).

## What you built

Gem Garden uses scenes as game states, sprites from files and from code, an animator, input actions with gamepad and
touch support, a tile map with collisions, a camera, particles, sound effects and music, a HUD, menus, a pause overlay,
a save file, and packages for several platforms. That is most of MicroEngine.

Where to go from here:

- Make the game yours: more levels (a `Level` per map, chosen on the title), a gem that grows the clock, slimes that
  chase the gardener.
- [Focused examples](../examples/focused.md): one screen per topic, including what the tutorial did not use (dialogue
  boxes, tweens and coroutines, lighting, custom input sources).
- [Testing your game](../examples/testing.md): play it automatically with input scripts and screenshots.
- [Achievements](../manual/achievements.md): unlockable achievements with banners and a list screen, in a few lines.
- The [engine manual](../manual/lifecycle.md) explains each subsystem in depth, and the [API reference](../api/README.md)
  lists every public type.
