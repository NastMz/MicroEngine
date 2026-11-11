# Review and delivery note, third film

Film: `renders/every-step.mp4`, 1920 x 1080, 30 fps, 55.2 s, H.264 + AAC stereo.
The other two films: `../seated` and `../assembly`.

## The three films

| | Seated | Assembly | Every step |
| --- | --- | --- | --- |
| Tells | the result | the architecture | the stance on time |
| Subject | code, sprites, games | kernel, sockets, modules | the loop, the step, the order |
| Picture | flat paper and graphite, real footage | an exploded drawing on a table, family colours | a tape and a playhead, ink and amber only |
| Form | scenes joined object to object | one table, a cut per configuration | one take, no cuts |
| Tempo | 120 BPM | 150 BPM | 100 BPM |
| Ground at the end | graphite | paper | paper |
| The mark | a module seats in the board | the board is assembled from parts | the board closes over a step |
| Last line | "There is always room for another module" | "One kernel. Assembled your way." | "The loop owns time. The rest is yours." |

## What is true and what is a picture

- **True of the engine:** time is cut into fixed steps; a frame runs input, the fixed steps and one draw, in that
  order; scripted and headless runs are deterministic, which is how the demos are tested
  (`docs/manual/lifecycle.md`, `docs/architecture.md`). "The loop owns time" is the documentation's sentence.
- **Real:** the score and effects are rendered by `MicroEngine.Audio`, and the tape shows that score's own steps.
- **A picture, not a capability:** the tape, the playhead, the depth and the two lanes are an abstraction. Nothing
  on screen is a tool, an editor or a view the engine has. No future capability is shown.
- **A simplification to know about:** the opened step shows INPUT, UPDATE, DRAW as three equal parts. In the engine
  a frame may run more than one fixed step before its single draw. "Get the same game" holds for the same build,
  inputs and seed.

## Copy to approve

New lines written for this film: "A game is made of time", "Cut into equal steps", "Each one in the same order",
"Play the same steps again", "Get the same game", "What happens in it is yours", "The rest is yours."
From the project: "The loop owns time".

## Measured (objective)

- `hyperframes check`: passes with no errors. Kept on purpose: `dead_stretch` warnings, which come from the audit
  not seeing motion inside the canvas; the tape moves in every frame.
- File: 55.200 s, 1656 frames, 1920 x 1080, 30 fps.
- Audio: -16.0 LUFS integrated, true peak -1.0 dBTP, no clipping.
- Sync: the picture is drawn from the same step data as the song, on a 0.3 s grid. Detected onsets sit on that grid
  (median distance 0.04 s, the detector's own resolution); onsets at 0.6, 4.8, 9.6, 16.8 and 24.0 s match the bar
  lines where the picture changes, and the impact is at 45.63 s, one frame after the board lands at 45.60 s.
- Text: display lines are 96 px, labels 34 px, the last line 44 px; every line holds at least 2.8 s.

## Looked at (by the author, on frames)

Snapshots across the take and decoded frames of the render: frame 0 is not empty, both grounds keep full contrast
through the wipe, the lanes are identical, the block at 45.5 s sits exactly where the mark's module lands.

## Needs a person

- **Listening.** The sound was measured, never heard: whether the slower tempo carries 55 s, and the balance of the
  lead in bars 10 to 15.
- **Watching at speed.** Whether the constant 500 px a second of the tape is comfortable for 40 s, and whether the
  opened step reads in its 7 s.
- **Judgement.** Whether a film this spare belongs beside the other two, and the new copy.

## Weakest shots

- 0-4.8 s: one block and one line for two bars; honest to the idea, but the quietest opening of the three.
- 33.6-38.4 s: two bars with no new event beyond the music.
- The equals sign between the lanes is small against the tape.

## Rebuild

`node media/tools/films.mjs build every-step` (see `media/README.md`).
