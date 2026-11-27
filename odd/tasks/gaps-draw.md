# GAPS-DRAW: sprites and text a game draws every frame

Objective: close group C of NastMz/MicroEngine#218 (DEMO-GAPS-1), so a developer outside the repository draws HUD
numbers, prompts, one-shot effects, phased clips and frame data with no caches or players of their own. Branch
`feat/gaps-draw`; commits are local, nothing is pushed. Test runner: `dotnet test MicroEngine.slnx -c Release`.
Evaluation: [`docs/evaluations/gaps-draw.md`](../../docs/evaluations/gaps-draw.md). Decision:
[ADR 0076](../../docs/decisions/0076-text-effects-and-frame-data-without-game-code.md).

## Specs

Quoted fragments are the issue's text, verbatim (L1).

- S1 "Fonts draw only `string` and `ActionPrompts.Format` allocates on every call: a HUD number or a hint needs
  caches (115 lines in Trail Duel, 48 in Courtyard Key)." Acceptance: a frame that draws a changing score, a clock and
  a prompt allocates 0 bytes in the steady state; Trail Duel's HUD numbers draw through the new API with 0 bytes and
  the same quads as their strings; `ReadOnlySpan<char>` beside each `string` overload.
- S2 "No one-shot sprite effect: \"play this clip once here, delayed, flipped, tinted\" (`SpriteFx.cs` in Trail
  Duel); `Animator` is one clip on one object and particles have no clips." Acceptance: Trail Duel's effect list
  through the new system gives the same frames on the same updates as `SpriteFx`; zero allocation per effect in the
  steady state.
- S3 "`AnimationClip.FrameAt` has no per-instance frame offset and `AddFrames` no rate (fire bars, torches and coins
  stay hand-written in Meadow Run)." Acceptance: the three frame expressions through clips, the same indices for a
  table of times.
- S4 "A `.sheet` frame has an anchor only: no named points (a shoulder, a gun tip) and no hit boxes (Neon Rift scans
  pixels and keeps boxes in code)." Acceptance: points read from sheet data equal Neon Rift's pixel scan; existing
  sheets load unchanged; the format rule is stated.
- S5 "`Scanlines` has no phase (which row is dark first)." Acceptance: a parameter, clamped and NaN-safe.
- S6 "`microengine content names` lists a strip the sheet does not declare (`prop_tank-` in Neon Rift)." Acceptance:
  reproduced, the tool fixed if it is the tool.

## Tasks

| ID | Specs | Route | Status | Commit |
| --- | --- | --- | --- | --- |
| T1 Interpolated text, spans, atlas names, `Prompts.Cached`, `CachedText` | S1 | inline | done | 08f74d3e |
| T2 Clip frame offsets, `AddFrames` with a rate | S3 | inline | done | 934a0911 |
| T3 `Scanlines.Phase` | S5 | inline | done | 07324e45 |
| T4 `content names`: declared strips | S6 | inline | done | 4faba072, f1357aec (generated file) |
| T5 Points and boxes on frames (`.sheet`, packed atlas, Aseprite) | S4 | inline | done | 3ea60daa |
| T6 `SpriteEffects`, `Scene.Effects` | S2 | inline | done | 1d02ab80 |
| T7 Example, manual, release notes, ADR, evaluation, API pages | S1-S6 | inline | done | the commits after 1d02ab80 |

## Log

- L1 The issue's text, group C, verbatim: "### C. Sprites and text a game draws every frame (GAPS-DRAW) — Fonts draw
  only `string` and `ActionPrompts.Format` allocates on every call: a HUD number or a hint needs caches (115 lines in
  Trail Duel, 48 in Courtyard Key). — No one-shot sprite effect: \"play this clip once here, delayed, flipped,
  tinted\" (`SpriteFx.cs` in Trail Duel); `Animator` is one clip on one object and particles have no clips. —
  `AnimationClip.FrameAt` has no per-instance frame offset and `AddFrames` no rate (fire bars, torches and coins stay
  hand-written in Meadow Run). — A `.sheet` frame has an anchor only: no named points (a shoulder, a gun tip) and no
  hit boxes (Neon Rift scans pixels and keeps boxes in code). — `Scanlines` has no phase (which row is dark first). —
  `microengine content names` lists a strip the sheet does not declare (`prop_tank-` in Neon Rift)."
- L2 T1. The first run of the allocation tests failed with 24 bytes per formatted value: the compiler chose the
  generic `AppendFormatted<T>(T)` over `AppendFormatted(int, string? = null)` and the cast to `ISpanFormattable`
  boxed. Overloads without default parameters and a type test in the generic one removed it. The whole solution
  (games included) compiles against the new overloads with no source change.
- L3 T1. `ActionPrompts.Version` does not move when code binds or unbinds an action: `InputMap`
  (`src/MicroEngine.Input`, outside this unit's files) reports no change. So `Format` is left uncached and `Cached`
  documents `Invalidate()` after rebinding. Needed from the input map: a version that moves on `Bind`/`Unbind`; then
  `Version` can include it and `Format` and `PromptText` can be cached too.
- L4 T4. Fixing the tool makes `games/NeonRift/Art/GameContent.g.cs` stale, and
  `ContentTests.The_repositorys_content_passes_the_check_and_its_generated_names_are_current` fails until it is
  written again. The file is the tool's output; it was regenerated in its own commit (six lines, no game code names
  the constants). It is the one file under `games/` this unit touched.
- L5 T5. `tests/MicroEngine.Aseprite.Tests/AsepriteTests.cs` gained one test (the folder is not in the listed edit
  surface; the importer is). Its file writer writes one key per slice, so a slice that moves between frames is
  covered for the exported JSON only (`SpriteMarkTests`).
- L6 T6. Effects are hashed. The brief expected them outside the hash; `Busy` and `IsPlaying` let a rule wait for an
  effect (Trail Duel's battle does), so they are state.
- L7 T7. The tour capture `43-effects.png` was looked at: three bursts leave the caster's hand on the side it faces,
  the seven torches show different frames, the HUD shows the score right-aligned, the clock and the prompt.
- L8 Tests observed failing before their code: the text allocation and prompt tests (T1), the clip tests (did not
  compile, T2), the `content names` test (T4), the Aseprite slice test (T5). Written with their code and passing on
  the first run, so not seen failing: the scanline phase test (T3), `SpriteMarkTests` (T5), `SpriteEffectsTests` (T6).
