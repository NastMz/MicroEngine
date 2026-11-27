# GAPS-DRAW: sprites and text a game draws every frame

Evaluation of 2026-11-26, on `main` at a4cbe289, for group C of NastMz/MicroEngine#218. It follows the method of
[CONSOLIDATION-1](consolidation-1.md), section 1 (A need to I validation), once per gap. Decision record:
[ADR 0076](../decisions/0076-text-effects-and-frame-data-without-game-code.md). Task record:
[`odd/tasks/gaps-draw.md`](../../odd/tasks/gaps-draw.md). No frame time was measured: every claim below is a count of
bytes, of frames or of lines, checked by a test.

## 1. Text drawn every frame without garbage — Extend

- **A, B.** Every game with a HUD. `IFont.Draw` and `Measure` took `string`; `ActionPrompts.Format` builds a string
  per call. Confirmed in `games/TrailDuel/Ui/NumberTexts.cs`, `CachedText.cs`, `Visuals/SpriteKeys.cs` (115 lines),
  `games/CourtyardKey/UI/TextCache.cs` (48), `games/NeonRift/Scenes/Ui.cs` (`NumberText`), Meadow Run's `NumberLabel`.
- **C.** (1) `ReadOnlySpan<char>` overloads and a buffer the game fills: free, three lines per text. (2) Number
  methods (`DrawNumber`): one value per call. (3) A C# interpolated string handler (the mechanism of
  `StringBuilder.Append($"...")` and `Debug.Assert`, in the language since C# 10, no dependency): the developer's
  existing line stops allocating. (4) FontStashSharp 1.3.10 (zlib, already the scalable fonts' rasterizer) draws and
  measures a `StringBuilder`, which a font reuses for span text. (5) `Dictionary.GetAlternateLookup<ReadOnlySpan<char>>`
  (.NET 9) finds a sprite or a shaped line by span.
- **D.** `Font.Draw(r, $"{hp}/{max}", at, color)`; `Font.DrawRight(r, $"${money}", edge, color)`;
  `atlas[$"t:water{mask}_{frame}"]`; `Font.Draw(r, Prompts.Cached("{jump}: JUMP"), ...)`.
- **E.** The engine formats what the language's interpolation formats, the same way (formats, alignment, the thread's
  culture). It does not choose a culture, cache numbers, or lay rich text out.
- **F.** `IFont` gains default members, so `MicroEngine.UI` and third-party fonts compile unchanged and the UI writer
  has `ReadOnlySpan<char>` beside each `string`. Nothing enters the state hash: drawing only.
- **G.** Three forms per text method (string, span, interpolated). The handler rents from `ArrayPool<char>`; a text
  that throws before it is drawn loses one rental to the collector. A value that is not `ISpanFormattable` still
  allocates through `ToString`.
- **Localized text with a changing number** (the hard case): `Localizer.Format` returns a new string per call and is
  not this unit's to change. `CachedText<TKey>` makes it one `Format` per change of the key `(number, Text.Version)`
  and nothing while it stays; a number that changes every frame costs one `Format` a frame, or nothing when the label
  and the number are drawn as two texts.
- **Prompts.** `Cached(template)` is correct for device, pad family, labels and `Invalidate`. It does not follow
  `InputMap.Bind` by code, because the input map reports no change (`Version` has the same limit today, and the manual
  already tells games to lay text out again on `Version`). Reported as needed outside this unit's files; `Format`
  stays uncached so nothing that is fresh today becomes stale.
- **I.** `InterpolatedTextTests`: the text equals the string for a table of values, formats, alignments and a Spanish
  culture; a frame with a changing score, a clock and a prompt allocates 0 bytes (600 frames, five rounds); Trail
  Duel's five HUD texts (number, `Lv`, `×`, `a/b`, `$`) allocate 0 bytes and draw the same quads as their strings for
  300 values; a TrueType font and a shaped font likewise. First run of the tests: 144 bytes per frame, a boxed `int`
  per value, because the compiler preferred the generic `AppendFormatted<T>` to an overload with a default
  parameter; fixed with exact overloads and a type test, and the test caught it.

## 2. One-shot sprite effects — Extend

- **A, B.** Any game with impacts. `Animator` is one clip on one object; particles have no clips; Trail Duel wrote
  `Scenes/SpriteFx.cs` (55 lines, 14 call sites), Neon Rift its explosions.
- **C.** A scene object per effect with an `Animator` (an object and a removal per puff); the particle system with
  textured particles (another data shape: many pieces, physics, no clip timing); a small player of clip instances
  (what the demo wrote: forty lines of state; no library was evaluated for it).
- **D.** `Effects.Play(atlas.Clip("thunder"), feet, delay: 0.08f, flipX: true)` and `Effects.Draw(r)`.
- **E.** One clip, once, at a point: no rotation, no scale, no physics, no sound. Following something is
  `Effects.Move(effect, at)` from the game's update.
- **F.** `SceneEffects` is a scene system (ADR 0060): stepped before `Update` on the scene's time, cleared when the
  scene leaves. In the state hash: `Busy` and `IsPlaying` let a rule wait for an effect, and Trail Duel's battle does.
  `SortedRenderer`: each effect at its layer and depth.
- **G.** A scene that reads `Effects` adds a system's name to its hash. The delay's remainder is not carried into the
  clip's time (the demo's rule, kept so its effects land on the same updates).
- **I.** `SpriteEffectsTests`: sixteen plays of Trail Duel's battle (delays 0.05 to 0.2 s, two rate overrides,
  mirrored, tinted), from the game's own `fx.sheet`, against a copy of `SpriteFx`: the same quads on each of 300
  updates and the same `Busy`. 0 bytes per frame once the pool has grown.

## 3. Clips a game drives — Extend

- **A, B.** Meadow Run kept `(int)(time * 12 + i) % 2` (fire bars), `(int)(time * 9 + Position.X) % 3` (torches) and
  `(int)(t * 8 + i) % count` (the title's coins): `FrameAt(time + i / 12f)` is another number at a frame boundary. And
  `AddFrames` had no rate, so a strip drawn in code needed its frame names listed to be a clip.
- **D, H.** `clip.FrameAt(time, frameOffset: i)`; `builder.AddFrames("torch", frames, 9)`. The offset is in frames, a
  float, and is added inside the truncation, which is the demos' expression bit for bit.
- **I.** `ClipPhaseTests`: the three expressions against `FrameIndexAt` for 7,200 float times each (two minutes at
  1/60 s), every link, six torch positions, two coin strips: equal indices. A negative time used to index before the
  first frame; it wraps or clamps now.
- **Not done:** an `Animator` that starts mid-clip (no demo case; `FrameAt` with an offset covers shared clocks).

## 4. Data on a sheet's frames — Extend

- **A, B.** Neon Rift scans pixels for the shoulder and the gun tip (`Assets.AddRig`) and keeps hit boxes in code.
- **C.** Aseprite slices (bounds, pivot, keys per frame) are the artist's tool for exactly this and were read only as
  sprites; TexturePacker has no such data; Spine and DragonBones have attachments and bounding boxes but are whole
  skeletal runtimes. So: the engine's own two typed kinds (point, box), fed by the `.sheet` file and by Aseprite.
- **D.** `position + frame.Point("muzzle", flipX)`; `frame.Box("hit", flipX)`.
- **E.** Named points and axis-aligned boxes per frame. No polygons, no text or number user data, no per-clip data
  (a clip's frames carry theirs).
- **F.** `.sheet` (ADR 0066), the packed atlas JSON, trimming, `content check` (errors come from the reader), the
  Aseprite package and Aseprite's exported JSON. Format rule: no version number; optional properties only.
- **G.** A point is a position (a pixel's corner), so the mirror of pixel `x` is one pixel off unless the sheet writes
  the centre; documented.
- **I.** `SpriteMarkTests`: Neon Rift's scan, computed in the test from `hero/shoot.png` and `hero/run-shoot.png`,
  equals the points a test sheet declares for the nine gun poses (shoulder, arm length, tip); trimming, packing to a
  file and loading again keep every point and box; an existing sheet reads as before.

## 5. `Scanlines` phase — Extend

`Phase` (0 to 1 of a line). Neon Rift's lines start on row 0: `Phase = 0.5`. Tested on the headless and reference
rasterizers. `Crt` keeps its own lines (its shader has no phase); not asked.

## 6. `microengine content names` — the tool was wrong

Reproduced from `games/NeonRift`: the tool inferred strips from names (`prop_tank-1..3` is a run) and stripped every
trailing digit, so the strip `hit1` (frames `hit10`, `hit11`) was never found. The sheet is right. A `.sheet` now
lists the strips it declares. Neon Rift's generated file changes by six lines (five strips gained, one lost; no code
names them).

## What the demos can delete in their second pass

`NumberTexts`, `CachedText`, `CachedPrompt`, `NumberText`, `NumberLabel` (interpolated draws and `Prompts.Cached`),
`SpriteKeys` (`atlas[$"..."]`), `SpriteFx` (`Scene.Effects` with `SnapToPixels`), the three frame expressions,
`Assets.AddRig`'s scan (points in `sprites.sheet`), `Ui.Scanlines` (`Scanlines { Phase = 0.5f }`).

## Corrections after an independent verification (2026-11-27)

The verification confirmed the text against main over nine cultures, the demos' 263 captures and 36 traces byte for
byte, the effects, the clips, the sheet data and the content tool, and found one blocker and several defects, all
fixed in one batch:

- Call shapes that compiled on main did not: a span with a width or a format in an interpolation (fixed: an exact
  overload); an `await` or a `dynamic` value in one (cannot be bound by a handler that is a ref struct: in the
  migration notes). A file of every call shape now guards them.
- `bitmapFont.MeasureWidth($"...")` and `family.Measure($"...", size)` bound to the `string` method of the type and
  allocated: every text method of every font type now has the three forms, and a table test calls each with each.
- An enum and a nullable number were boxed per value: enums are written from names found once, `int?`, `long?`,
  `float?` and `double?` directly; so are `decimal`, `TimeSpan`, `DateTime` and `Guid`. Another struct is still boxed,
  and the manual says so.
- `SpriteEffects`: a step or a delay that is not a time hung `Busy` or threw; they are ignored.
- `FrameIndexAt` rounds down across 0 (the comment said truncation): kept, stated, in the migration notes.
- `content check` let a point at `1e40` or four hundred pixels from its frame pass: an error and a warning.
- `ActionPrompts.Version` now follows `InputMap.BindingsVersion`, so the limit recorded in section 1 is gone for
  rebinding; the keyboard layout remains.
- A kept `InterpolatedText` drawn twice returned its buffer to the shared pool twice: the buffers are the handler's
  own now and a second use is refused.
- A run nobody plays formats with the invariant culture: set once per run on the game's thread (a set per frame
  allocated 480 bytes in sixty frames and failed the loop's allocation test), the interface culture left alone (the
  localization's fallback reads it, and its test failed when it was changed).
