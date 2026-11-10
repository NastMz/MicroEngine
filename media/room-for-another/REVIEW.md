# Review and delivery note, second film

Film: `renders/room-for-another.mp4`, 1920 x 1080, 30 fps, 56.0 s, H.264 + AAC stereo.
The first film and its sources: `../seated`.

## What changed against "Seated"

| Seated | Room for another |
| --- | --- |
| Opens on code that draws a pixel star | Opens inside the board: empty sockets in depth, a module seating |
| 10 s of full-frame 2D play | Play only on a screen that stands on the board, about a tenth of the frame |
| A flat diagram | The board as a dimensional object; the camera moves through it |
| "Swap a module. The game plays on." under a running game | Each configuration is a new shot: screen dark, module seats, screen on |
| Six fixed families | The board grows new bays with unnamed modules |
| No future | A dashed outline that never seats, tagged "NOT BUILT YET" |
| "A game engine that orchestrates, not reinvents." | "Not a fixed set of features. An architecture to build on." |

## What is real and what is a drawing

- **Real:** the play on the screens (scripted runs of the demos), the test names (output of
  `dotnet test tests/MeadowRun.Tests`, which plays the game with no window), the score and effects
  (`MicroEngine.Audio`), the mark, wordmark, icons, colours and fonts.
- **True of the engine, shown as a diagram:** a host choosing another platform module for the same game; a platform
  with no window; a game registering modules of its own. The board is a picture of the architecture, not a recording.
- **Not a capability, and drawn so:** the outline at the Graphics bay and the dashed solid. Nothing 3D is shown
  running. The grey modules in the new bays stand for subsystems that do not exist.

## Copy to approve

Two lines are new and not taken from published material: "Not a fixed set of features." and "An architecture to
build on." (from the owner's brief), and the chain "Use what ships / Or choose another / Even one with no window /
Seat your own / Or one that doesn't exist yet". "Every subsystem is a socket" and "There is always room for another
module" are the project's own.

## Measured (objective)

- `hyperframes check`: passes with no errors. Kept on purpose: `first_frame_empty` (the audit does not see the 3D
  faces; frame 0 shows the board, checked by eye), `text_below_floor` (module names and test names are small in
  the wide shots; the copy itself is 88 px), `text_occluded` (test lines before they are revealed).
- File: 56.000 s, 1680 frames, 1920 x 1080, 30 fps.
- Audio: -15.1 LUFS integrated, true peak -1.1 dBTP, no clipping.
- Sync: an onset within one frame of every seat (1.0, 3.0, 5.0, 6.5, 7.0, 7.5, 16.5, 20.5, 24.5, 25.0, 34.0 to
  35.5 s) and of the claim's lines (40, 42 s); the steepest moment of the file is at 48.06 s, the frame after the
  module seats in the mark.

## Looked at (by the author, on frames)

Snapshots of every shot and decoded frames of the render on both sides of each cut: no module changes while its
screen is on, the outline never enters its bay, the flattened structure equals the mark, no copy is covered.

## Needs a person

- **Listening.** The sound was measured, never heard: balance, whether the layers arriving with the first three
  modules are audible as such, whether the risers are harsh.
- **Watching at speed.** Whether a first-time viewer follows the four configurations in 4 s each, and whether the
  "no window" shot reads without knowing what a test run is.
- **The positioning judgement itself:** does a stranger say "modular, extensible" and not "2D engine"?

## Weakest shots

- 20-24 s: the windowless platform is the hardest idea in the film and has the smallest picture.
- 8-16 s: eight seconds on one slow move; the two lines carry it.
- The modules differ by face only; a different silhouette per implementation would read faster.

## Rebuild

`node media/tools/films.mjs build room-for-another` (see `media/README.md`).
