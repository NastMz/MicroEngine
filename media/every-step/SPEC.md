# Spec: "Every step"

Third MicroEngine film. It stands beside "Seated" (`../seated`, the result) and "Assembly"
(`../assembly`, the architecture); it replaces neither.

```text
GOAL:    The viewer leaves with the engine's stance on time: the loop keeps the beat and the order, the same
         steps give the same game, and everything that happens inside a step belongs to the person making the game.
OUTPUT:  HyperFrames, 1920 x 1080, 30 fps
TIME:    55.2 s x 30 = 1656 frames. 100 BPM, two steps per beat: a step is 0.3 s (9 frames), a beat 0.6 s,
         a bar 2.4 s (72 frames). Bar N starts at 2.4 N seconds.
LOOK:    One medium: a tape of steps under a playhead, drawn on a canvas as a pure function of time.
         The picture is the score: every cell is a step of a voice of the song that is playing (assets/score.json).
         Paper #f8f6f2 with ink #171b20 and amber #dd9314 (the light mark's colours); for bars 10 to 15 graphite
         #111418 with ink #f1eee9 and amber #fbc044 (the dark mark's). Nothing else: no family colours, no gradients.
         A step not played is an outline; playing, amber; played, solid ink. Squares only, on a 150 px step.
         Display: Bricolage Grotesque 600, 96 px, top left, one line at a time. Labels: IBM Plex Mono 500, 34 px.
OBJECTS: the playhead (amber: a block the size of the mark's module, then a line, then the block again);
         the tape (rows of steps and a ruler of equal ticks); one step opened (a frame with three parts);
         two lanes and an equals sign; the mark and wordmark (assets/brand, light versions).
MOTION:  the tape moves at one constant speed for the whole film (500 px a second): nothing eases, because the
         subject is a clock. Everything else eases in and out over 0.5 to 1.4 s and starts on a bar line.
         One take, no cuts. Grounds never fade: graphite opens from the playhead as a hard edge and closes back.
BANNED:  gameplay, code, sockets and modules seating (the other two films); 3D objects; a number of steps per
         second; colour as decoration; fades between grounds; text that moves; glow, particles, gradients.
```

## Shots (one take)

| Time | Bars | What happens | Reads | Sound |
| --- | --- | --- | --- | --- |
| 0-4.8 | 0-1 | one amber block beats with the kick | "A game is made of time" | kick on every beat |
| 4.8-9.6 | 2-3 | the block thins into a playhead; a tape of equal steps runs under it; rows join as voices join | "Cut into equal steps" | hats, then bass |
| 9.6-16.8 | 4-6 | the step under the playhead opens into a frame; its three parts light in order, twice slowly, twice fast | "Each one in the same order", INPUT, UPDATE, DRAW | drums stop; three bell notes per cycle, `open`, `close` |
| 16.8-24 | 7-9 | the tape splits into two lanes that light identically | "Play the same steps again", "Get the same game" | drums, bass, arpeggio, `split` |
| 24-38.4 | 10-15 | graphite opens from the playhead; the tape turns into depth, eight rows, the steps to come receding | "The loop owns time", "What happens in it is yours" | full band and lead, `turn` |
| 38.4-45.6 | 16-18 | graphite closes; rows fold away; the tape fades; the playhead is a block again, beating | - | band thins to a kick, `riser` |
| 45.6-55.2 | 19-22 | the mark's board comes down over the block: the step was the mark's module. Lockup | "The loop owns time. The rest is yours." | `impact`, final chord, tail |

## QA frames

0 (block and line), 6.5 (two rows), 10.5 (first part amber), 15.1, 18.5 (two identical lanes), 24.45 (hard edge),
29.5 (depth), 38.9 (closing edge), 43.5 (block alone), 45.57 (board in flight), 45.63 (landed), 50.
