# Review and delivery note, second film

Film: `renders/assembly.mp4`, 1920 x 1080, 30 fps, 54.4 s, H.264 + AAC stereo.

## Against the film it replaces, and the other two

| | Seated | Room for another (retired) | Assembly |
| --- | --- | --- | --- |
| Picture | a flat board of sockets | the same board as walls and bays | an exploded drawing around a kernel |
| A module is | a tile rising into a socket | a block sliding into a bay | a solid of its own shape docking on any side |
| A change of configuration | one module swapped under a running game | a cut to another shot of the same board | taken apart, pieces changed, assembled again |
| What comes of it | play, full frame | a screen on the board | a plane that appears only on a complete assembly |
| Ground | paper, then graphite | graphite | paper and a table |
| Tempo | 120 BPM | 120 BPM | 150 BPM |

## What is true and what is a drawing

- **Real:** the play on the plane (scripted runs of Neon Rift and Meadow Run, two different games), the score and
  effects (`MicroEngine.Audio`), the mark, wordmark, family icons, colours and fonts.
- **True of the engine, shown as a drawing:** a kernel that the subsystems connect to; implementations of a
  subsystem that can be chosen; a game or host bringing its own. The table and the solids are a picture of that.
- **Not claimed:** that these two games use the configurations drawn under them; that a piece can change while a
  game runs; anything 3D as a capability. The shapes of the pieces mean nothing but "a different implementation".

## Copy to approve

All lines are new: "One kernel", "Pieces that connect to it", "Assembled for one game", "Taken apart", "Reassembled
for another", "With pieces of your own", "YOUR GAME", "One kernel. Assembled your way." The film no longer ends on
"There is always room for another module", which Seated keeps.

## Measured (objective)

- `hyperframes check`: 0 errors. Two lint warnings kept on purpose (media timed inside a scene).
- File: 54.400 s, 1632 frames, 1920 x 1080, 30 fps. Web file 8.0 MB.
- Audio: -15.3 LUFS integrated, true peak -1.0 dBTP, no clipping.
- Sync: an onset within one frame of the kernel landing (0.8 s), of every dock in the three runs (10.0 to 11.0,
  24.4 to 25.4, 38.6 to 39.8 s), of each plane (11.2, 25.6, 40.0 s) and of each coming apart (20.0, 34.4, 45.2 s);
  the steepest moment of the file is at 48.06 s, the frame after the amber piece docks at 48.00 s.

## Looked at (by the author, on frames)

Snapshots of every shot and decoded frames of the render on both sides of each dock, plane and cut: the kernel is in
frame at 0, no piece moves while a plane is up, each plane is gone before its configuration comes apart, the copy
never overlaps a piece, the flattened board equals the mark.

## Needs a person

- **Listening.** The sound was measured, never heard.
- **Watching at speed.** Whether three assemblies in 45 s can be followed, and whether "Assembled for one game"
  reads as intended: a configuration chosen for a game, not a game produced by a configuration.
- **Judgement.** Whether the empty third plane lands as an invitation or as an anticlimax.

## Weakest shots

- 3.2-9.6 s: six arrivals at the same pace; the last two could come together.
- 40-44.8 s: the empty plane holds for three bars with little else moving.
- The pieces of your own differ from the shipped ones by colour and outline only.

## Rebuild

`node media/tools/films.mjs build assembly` (see `media/README.md`).
