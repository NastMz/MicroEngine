# Evaluation: building a run & gun on MicroEngine

Neon Rift (`games/NeonRift`) was built to test how the engine handles a genre none of the first three
games covered: a forward-scrolling run & gun with eight-way aiming, dense projectiles, boss arenas and,
for the first time, art and audio taken from existing asset packs instead of generated in code.

**Summary.** The core loop, collision, scenes, input and test tooling carried the genre with no changes.
Every gap was in *content intake*: the engine could not read a single image or sound file. Four small,
genre-neutral additions closed that gap (about 370 lines of engine code and 205 lines of tests). The remaining friction is
listed below with what would fix it; none blocked the game.

## What the game used, and how well it fit

| Engine feature | Use in Neon Rift | Fit |
| --- | --- | --- |
| `GameRunner` fixed step, `InputScript`, `--hidden --captures` | Deterministic runs, every screenshot and visual check | Excellent; scripted captures plus an autopilot made whole-stage review cheap |
| `HeadlessPlatform`, `InitializeForTest` | 25 game tests, including loading every real asset and reaching play from the title | Excellent; the managed PNG decoder means tests load the real files |
| `TileLayer`, `TileGrid`, `TileMotion` | Ground, one-way ledges with drop-through (`ignoreOneWay`), walls, lava (`Hazard`), open pits (`OutsideRule`) | Very good; no change needed |
| `Camera2D` | Forward-only scrolling and arena lock (game code), shake | Good; scroll rules are game-specific and stayed in the game |
| `SceneManager`, transitions, overlays | Title, stage card, play, pause overlay, game over, ending | Very good |
| `InputMap` | Added `jump`, `fire`, `lock` actions | Very good |
| `SpriteAtlasBuilder`, `DrawPivoted`, blend modes | 1024×1024 atlas; rotating gun arm; additive hit flashes, shots and glows | Good after adding `AddGrid` |
| `EntityList<T>` | Enemies, shots, pickups, effects with deferred spawning | Good; see the drawing note below |
| `BitmapFont`, `SelectionList`, `Panel` | HUD, menus, controls panel | Adequate; see the font note below |
| `AudioMixer` | 24 WAV effects, 6 music tracks with crossfades, pause | Good after adding file registration |

Not used: `Synth`/`Song` (replaced by recorded audio), `LightMap`, `ParticleSystem` (the packs provide
animated explosion and hit sprites), `DialogueBox`, `SaveStore`, `Autotile`.

## Modifications made to the engine

All are genre-neutral, covered in `tests/MicroEngine.Tests/ContentLoadingTests.cs` and recorded in
[ADR 0004](../decisions/0004-external-asset-files.md):

1. **File loading.** `PixelImage.FromPng/Load` (managed decoder: 8-bit grey, RGB, RGBA, 1–8-bit palette,
   tRNS); `AudioClip.FromWav` (PCM 8/16/24-bit); `IAudioDevice.LoadMusicStream` so compressed music is
   decoded by the backend (raylib) while headless runs stay silent; `AudioMixer.AddSoundFile/AddMusicFile`;
   `ContentFolder` for paths next to the executable with errors that name the missing file.
2. **Sprite sheets.** `SpriteAtlasBuilder.AddGrid` slices regular grids/strips into numbered frames.
3. **Image transforms.** `Crop`, `ClearRect`, `OpaqueBounds` and nearest-neighbour `Rotated` let a game
   derive poses the artist did not draw. The pack's heroine only aims forward; Neon Rift cuts her gun
   arm off the body at load time and rotates it, giving eight aim directions without editing pixels.
4. **Parallax.** `ParallaxLayer` draws a horizontally repeating background at a scroll factor.

The other three games build and their tests pass unchanged (60 engine, 24 Courtyard Key, 42 Meadow Run,
53 Trail Duel, 25 Neon Rift).

## Limitations found

Ordered by how much they cost during development. "Game-side today" is how Neon Rift worked around it.

| Limitation | Game-side today | Suggested engine change |
| --- | --- | --- |
| Asset-pack tilesets are rarely clean autotile sheets (two of three packs ship irregular "chunks") | `Tiles.cs` cuts 16×16 cells by pixel coordinates and picks them by neighbour rules | A data-driven tile rule set (top/face/fill/edge per id) would remove most of that code |
| No Tiled (`.tmj`) import; Warped City ships a Tiled map | Stages authored with a cursor-based `StageBuilder` | A small Tiled JSON reader for layers and objects |
| No white "flash" draw: tint only multiplies | `Actor.DrawFlashing` redraws the sprite additively | A `DrawOptions` fill/flash colour, or a silhouette cache per sprite |
| `Entity.Draw(IRenderer)` has no access to game assets | `Actor.Render(r, assets)` and manual iteration in `StageView` | Let `EntityList.Draw` take a draw callback, or document the pattern |
| Timers outside scenes: `CoroutineRunner` lives on `Scene`, but the testable simulation (`Stage`) is not a scene | A 10-line scheduler in `Stage` | A standalone `Timeline`/scheduler type usable by any object |
| Built-in font is tiny; scaled 3–5× it is blocky and there is no file font | Drop shadow + additive glow for titles | Load bitmap fonts from a PNG + glyph table |
| Compressed music is decoded only by the backend | Headless tests check registration, not decoding; a desktop run proves decoding | Acceptable trade-off; a managed OGG decoder would need a dependency |
| No persistence of the high score | Kept per session | `SaveStore` already exists; a game choice, not an engine gap |
| No guard on atlas size versus GPU texture limits | A game test asserts the atlas stays ≤ 4096 px | `SpriteAtlasBuilder.Build` could warn past a configurable limit or split pages |

Candidates for promotion once a second game needs them: the forward-scrolling camera with an arena lock,
camera-triggered spawn lists, the ground `Body` helper, and the autopilot-playthrough test pattern
(a bot driving the real stage until it is cleared caught an impossible lava jump and an unreachable boss
turret during development).

## Verification status

- `dotnet build MicroEngine.slnx -c Release`: clean (warnings are errors). `dotnet test`: 204 tests pass.
- Visual checks: scripted and autopilot captures of every stage, all three bosses, stage clear, stage
  cards, title, controls, game over and ending were inspected; the screenshots in the game README come
  from those runs.
- Audio: all files register; music loads through raylib without decode errors. The mix was **not**
  listened to (every automated run is muted or headless), and 60 fps feel on real hardware was not
  measured.
