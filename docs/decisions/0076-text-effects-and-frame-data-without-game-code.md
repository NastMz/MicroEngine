# 0076 — Text, one-shot effects and frame data a game draws every frame without code of its own

Status: Accepted (2026-11-26). The SDK is in Alpha: new public members carry no compatibility commitment. Evidence:
[GAPS-DRAW](../evaluations/gaps-draw.md). Issue NastMz/MicroEngine#218, group C. Extends
[ADR 0066](0066-content-production-as-data-and-optional-tools.md) (the sheet format), [ADR 0060](0060-scene-systems-and-module-lifecycle.md) (a new scene system) and
[ADR 0071](0071-post-processing-window-stage-mip-levels-draw-order.md) (`Scanlines`).

## Context

The four demos were moved onto the consolidated engine without changing a pixel, and kept private code for things
any game draws every frame: caches of strings so a HUD number makes no garbage (115 lines in Trail Duel, 48 in
Courtyard Key, a `NumberText` in Neon Rift, a `NumberLabel` in Meadow Run), a player of one-shot clips (`SpriteFx`,
55 lines), frame expressions with a start frame per instance (`(int)(time * 12 + i) % 2`), a pixel scan that finds a
shoulder and a gun tip, hit boxes in code, 135 rectangles a frame because `Scanlines` darkens the other row.

Fonts took only `string`. `ActionPrompts.Format` builds its text on every call. `Animator` is one clip on one object
and `ParticleSystem` has no clips. A `.sheet` frame had an anchor and nothing else.

## Decision

- **Text.** An interpolated string handler, `InterpolatedText`, and `ReadOnlySpan<char>` overloads beside every
  `string` overload that draws or measures: `IFont` (default members, so a font of another kind keeps compiling and
  copies the text), `BitmapFont`, `SizedFont`/`ScalableFont`, `ShapedFontSize`, and `FontExtensions`. The developer
  writes `Font.Draw(r, $"{hp}/{max}", at, color)`: the compiler picks the handler, the text is written into a pooled
  buffer and the buffer is returned by the callee. The text equals what the same interpolation gives as a `string`
  (formats, alignment, the thread's culture), so code that already wrote interpolated strings stops allocating when
  it is compiled again and draws the same pixels. `DrawRight` is added because right alignment needed the text twice.
- **Names.** `SpriteAtlas` has an indexer by span and by `InterpolatedText`, and `TryGet`.
- **Prompts.** `ActionPrompts.Cached(template)` keeps the text of `Format` until `Version` moves. `InputMap` has a
  `BindingsVersion` that every change of a binding moves (saved bindings included), and `Version` includes it and the
  touch layout, so a kept hint follows a rebinding by itself. `Format` and `PromptText` are not cached: they read the
  keyboard layout's labels on every call, and a layout the player switches while the game runs has no signal.
- **The buffer of an interpolated text** is one per thread and per level of nesting, kept by the handler, not rented
  from a shared pool: a text knows the use count of its level, so a copy used twice is refused
  (`ObjectDisposedException`) and cannot return a buffer twice.
- **A run nobody plays formats with the invariant culture** (amends [ADR 0073](0073-saves-of-a-run-nobody-plays.md):
  the culture joins the saves among what such a run does not take from the machine). The handler keeps the thread's
  culture, as strings do; an unattended run sets `CultureInfo.CurrentCulture` to the invariant culture once, before
  the game loads, and gives the thread its own back when the game shuts down (the host does it again however the run
  ended). Once per run, not per frame: setting a thread's culture allocates, and the loop's frames allocate nothing.
  `CurrentUICulture` is not touched: where a platform names no language, the localization reads it, and a test holds
  that. A played run keeps the player's culture.
- **Text that cannot be interpolated in place.** `CachedText<TKey>` rebuilds a text when its key changes. A localized
  message with a number costs one `Localizer.Format` per change of the number or the language.
- **One-shot effects.** `SpriteEffects` (2D graphics assembly) plays clips once: position, delay, flip, tint, a rate,
  a layer and a depth for a `SortedRenderer` block, a handle to ask about, move or stop. `Scene.Effects` is a scene
  system (`SceneEffects`) that steps it on the scene's time, hashes it and clears it. Its rules are those of the
  demo's player: the delay counts down first, time counts up after it, the effect ends on the update
  `time * rate >= frames`.
- **The effects are in the state hash.** They step on the fixed step and `Busy`/`IsPlaying` are public, so a rule can
  wait for them (Trail Duel's battle does); what a rule can wait for is state.
- **Clips.** `AnimationClip.FrameIndexAt(time, frameOffset)` and `FrameAt(time, frameOffset)`; at a fixed rate the
  index is `(int)(time * FramesPerSecond + frameOffset)`, the demos' expression. `SpriteAtlasBuilder.AddFrames` with a
  rate adds the clip.
- **Frame data.** `SpritePoint` and `SpriteBox` on `SheetFrame`, `AtlasRegion` and `Sprite`; `Sprite.Point`,
  `Sprite.Box`, `TryPoint`, `TryBox` measure from the anchor and mirror with `flipX`. A `.sheet` takes `points` and
  `boxes` on a named image, a strip and a sprite object: one value for every frame or one entry per frame. Aseprite
  slices (the file and the exported JSON) become a box and, with a pivot, a point, per frame by key. The packed atlas
  JSON writes them in each frame entry and reads them back; trimming moves them with the anchor.
- **Format rule.** The sheet format has no version number. It grows by optional properties: a file written before
  reads the same; a file that uses a property an older reader does not know is refused by name (the reader already
  refuses unknown properties). A change that would alter the meaning of an existing property is not allowed without a
  version field.
- **`Scanlines.Phase`**, 0 to 1 of a line, clamped, a NaN ignored: 0.5 darkens the first row.
- **`microengine content names`** lists the strips a `.sheet` declares (`SpriteSheet.Strips`); sources that declare
  none keep the naming rule.

## Alternatives

- **A `DrawNumber(int)` family.** Covers one value and one format per call; "12/20", "Lv7" and "0:07.3" need
  composing calls and measuring pieces. The handler covers them with the syntax developers already write.
- **A `StringBuilder` or a text buffer type the game fills.** Zero garbage, but three lines and an object per text;
  kept available through the span overloads.
- **Caching `Format` inside `ActionPrompts`.** The better experience, declined until the input map reports binding
  changes: a stale prompt after a rebinding is worse than a string per call.
- **Effects as scene objects with an `Animator`.** An object, a component and a removal per puff: the code the demos
  avoided by writing `SpriteFx`.
- **Effects in the particle system.** Particles are many pieces with physics; an effect is one clip at one place.
  Different data, different draw.
- **Effects outside the state hash.** Cheaper for purely visual use, wrong the first time a rule waits on `Busy`.
- **Points in a side file or user data strings.** A second file to keep in step with the sheet; typed points and
  boxes in the sheet are checked by `content check` with the frames they belong to.
- **A version number in `.sheet`.** Nothing changed meaning; a number would have refused every existing file or meant
  nothing.

## Consequences

- Every `font.Draw(r, $"...")` in a game compiles to the handler: no source change, the same text, no string.
- `IFont` has four more members with defaults; a font of another kind that wants zero garbage writes the two span
  members.
- A scene that reads `Effects` gains a system: its type name and its effects enter the scene's state hash.
- `content names` output changes for sheets (declared strips); generated files are refreshed once.
- An interpolated string holding an `await` or a `dynamic` value does not compile against the handler; the migration
  notes give the fix. `tests/MicroEngine.Tests/Text/TextCallShapeTests.cs` holds every call shape games use, so a
  change to the overloads that breaks one fails the build.
- A game stepped by a test (`InitializeForTest` on the headless platform) has no end, so the test's thread keeps the
  invariant formatting culture after it.
- Not done: rotation and scale of an effect; a signal for a keyboard layout switched while the game runs (then
  `Format` and `PromptText` can be cached too); frame data of other kinds.
