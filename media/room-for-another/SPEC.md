# Spec: "Room for another"

The second MicroEngine film. It stands beside "Seated" (`../seated`, the result); it does not replace it.

```text
GOAL:    The viewer leaves knowing MicroEngine is a kernel with sockets: what a game is made of is a choice of
         modules, shipped, replaced, their own or not built yet. Nobody should leave thinking "a pixel-art 2D engine".
OUTPUT:  HyperFrames, 1920 x 1080, 30 fps
TIME:    56 s x 30 = 1680 frames. 120 BPM: 15 frames per beat, 60 per bar. Bar N starts at 2 N seconds.
LOOK:    The site's "Socket" system built as an object in space: a lintel (the kernel) on walls, bays between
         them (the sockets), blocks that slide into the bays (the modules), screens standing on the lintel (what
         runs). CSS 3D, perspective 2600 px, flat fills only: front, top (+18% white) and side (-22% black).
         Graphite ground oklch(18% 0.01 260), floor oklch(21.5% 0.012 260), ink #f1eee9, amber #fbc044 only for
         the mark's module and one underline. Display: Bricolage Grotesque 600, 88 px (board) and 156 px (claim).
OBJECTS: shipped module = family colour, family icon and name (site/src/components/brand/geometry.mjs);
         another implementation = same colour, inverted face; windowless platform = same colour, three bars;
         your own = board ink with a band of the family colour;
         not built = a dashed outline that never enters its bay, and a dashed solid on the screen tagged
         "NOT BUILT YET"; unnamed future subsystems = neutral grey, no icon.
         Screens show only real output: play captured from the demos, and names of real passing tests.
MOTION:  a module travels 0.34 s (power2.in) and lands exactly on its beat; the board recoils 6 px.
         Inside a shot the camera only drifts (linear yaw, 2 to 3 degrees a second). A new configuration is
         always a cut to a new angle with the screen dark, then the module seats, then the screen comes on.
BANNED:  full-bleed gameplay; pixel art outside a screen; code that draws a sprite; a module changing while its
         screen is on (reads as hot swap); 3D shown as running; versions, platform or library names, counts;
         glow, particles, gradients; three cards in a row; a docked logo.
```

## Shots

| Time | Bars | Hero move | Reads | Sound | Hands off |
| --- | --- | --- | --- | --- | --- |
| 0-2 | 0 | close on one bay; the Platform module seats at 1.0 | - | bell, `seat`, the kick starts | cut |
| 2-4 | 1 | from above; Graphics seats at 3.0 | - | `seat`, the hats start | cut |
| 4-6 | 2 | low and frontal; Audio seats at 5.0 | - | `seat`, the bass starts | cut |
| 6-8 | 3 | pull back to the whole board; Physics, Input, Tools seat on the last three beats | - | snare build | the dark screen |
| 8-16 | 4-7 | the screen comes on; the camera travels round the board | "Every subsystem is a socket", "Use what ships" | full band, `screen-on` | cut |
| 16-20 | 8-9 | new angle, screen dark; another Platform module seats; the same game comes on | "Or choose another" | half-bar break, then full | cut |
| 20-24 | 10-11 | a windowless Platform module seats; test names fill the screen | "Even one with no window" | kick and ticks only | cut |
| 24-28 | 12-13 | two ink modules seat in Input and Audio; a game comes on | "Seat your own" | full band | cut |
| 28-32 | 14-15 | an outline drifts to the Graphics bay and stops outside; a dashed solid turns on the screen | "Or one that doesn't exist yet" | no drums, bell, `glide` | cut |
| 32-39.4 | 16-19 | the lintel grows both ways, new bays, unnamed modules, three screens, outlines at both ends | - | full band, `grow`, `riser-long` | paper rises |
| 40-45.4 | 20-22 | two sentences on paper | "Not a fixed set of features. An architecture to build on." | soaring lead | graphite rises |
| 46-48 | 23 | a two-bay structure builds, the camera comes to the front, depth presses out: it is the mark | - | snare build, `riser` | the empty right bay |
| 48-56 | 24-27 | the amber module arrives from the viewer's side and seats on the downbeat; lockup | "There is always room for another module" | `impact`, final chord, tail | hold |

## QA frames

0 (board visible), 1.03, 3.03, 5.03 (seated), 7.9 (six seated, screen dark), 12, 16.03 (screen dark, bay empty),
17.5, 22.5 (test names), 26.5, 30.5 (outline outside the bay), 36.5, 39, 43, 47.5 (flat mark, no module),
48.03 (seated), 52.
