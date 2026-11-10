# Spec: "Assembly"

The second MicroEngine film. It stands beside "Seated" (`../seated`, the result) and "Every step" (`../every-step`,
the loop); it replaces neither. It takes the place of an earlier second film, "Room for another", whose board of
walls and bays read as the first film redrawn in depth.

```text
GOAL:    The viewer leaves knowing MicroEngine is one kernel that pieces connect to, that a game is built on a
         chosen configuration of those pieces, and that another game is built on another, with pieces that ship,
         others in their place, or the viewer's own.
OUTPUT:  HyperFrames, 1920 x 1080, 30 fps
TIME:    54.4 s x 30 = 1632 frames. 150 BPM, two steps per beat: a step is 0.2 s (6 frames), a beat 0.4 s,
         a bar 1.6 s (48 frames). Bar N starts at 1.6 N seconds.
LOOK:    One medium: an exploded drawing on an assembly table. Solids float apart along their axes, joined to
         where they dock by dashed guides, and close into one object. Almost no perspective (5200 px), so parallel
         edges stay parallel, as in a technical drawing. CSS 3D, flat fills only: front, top (+20% white), side
         (-20% black). Paper #f8f6f2, table #ece8e1 with registration marks #cfc9bf, guides #8b8a86.
         The kernel is ink #171b20. Pieces that ship carry their family's colour and icon; a piece of your own is
         paper outlined in ink with a band of the family's colour; a piece of a new kind is neutral grey.
         Amber appears once, as the mark's module. Display: Bricolage Grotesque 600, 88 px, top left.
OBJECTS: the table; the kernel (300 x 140 x 300, never changes); twelve pieces of different shapes; a guide per
         piece; the plane (640 x 360 above the kernel, on a dashed stem) that shows what a configuration is for:
         real play captured from two different demos, and an empty dashed frame for the game that is not ours.
MOTION:  a piece arrives in 0.7 s and floats; docks in 0.17 s, landing exactly on its eighth note; springs apart in
         0.42 s. The plane appears only after the last piece has docked and goes before anything is undone.
         Inside a shot the camera only drifts; each new configuration starts with a cut to another side of the table.
BANNED:  walls, bays, sockets open at the bottom, a board (the first film and the retired one); a piece changing
         while the plane is up (reads as hot swap); one game turning into another; 3D shown as a capability;
         versions, platform or library names, counts; glow, particles, gradients, shadows.
```

## Shots

| Time | Bars | What happens | Reads | Sound |
| --- | --- | --- | --- | --- |
| 0-3.2 | 0-1 | the kernel comes down on the table and lands on the third beat | "One kernel" | bell, `land`, first kick |
| 3.2-9.6 | 2-5 | six pieces arrive, one every two beats, and float on their guides | "Pieces that connect to it" | a plucked note per piece |
| 10-11 | 6 | they dock, an eighth note apart | - | a run of eighths, `dock` x 6 |
| 11.2-19.2 | 7-11 | the plane rises: a game plays | "Assembled for one game" | full band, a line in E minor, `emerge` |
| 19.3-25.4 | 12-15 | the plane goes; cut; the configuration springs apart; three pieces leave, three others arrive; all dock | "Taken apart" | break, `apart`, plucks, run |
| 25.6-33.6 | 16-20 | the plane rises: another game plays | "Reassembled for another" | full band, another line, in G |
| 33.7-39.8 | 21-24 | apart; two pieces leave; two of your own and one of a new kind arrive; all dock | "With pieces of your own" | break, plucks, run |
| 40-44.8 | 25-27 | the plane rises, empty: a dashed frame | "YOUR GAME" (on the plane) | pad and bell, no drums |
| 44.9-46.4 | 28 | apart for the last time; every piece leaves, the kernel last | - | `apart`, `riser` |
| 46.4-48 | 29 | four ink parts dock into the mark's board; the camera comes to the front and the depth presses out | - | run, `dock` x 4 |
| 48-54.4 | 30-33 | the amber piece docks on the downbeat; lockup | "One kernel. Assembled your way." | `impact`, final chord, tail |

## QA frames

0 (kernel in the air), 0.83 (landed), 9.5 (six floating), 10.03 (first docked), 11.3 (plane up), 20.2 (apart, other
side), 25.7 (second plane), 33.9 (plane going, still assembled), 37.8 (own pieces floating), 42 (empty plane),
47.2 (board assembling), 48.03 (module docked), 53.
