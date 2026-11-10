# Spec: "Seated"

```text
GOAL:    The viewer leaves knowing MicroEngine is a small kernel that games sit on and modules seat into, and
         that there is room for more.
OUTPUT:  HyperFrames, 1920 x 1080, 30 fps
TIME:    50 s x 30 = 1500 frames. 120 BPM: 15 frames per beat, 60 per bar. Bar N starts at 2 N seconds.
LOOK:    The site's "Socket" system (site/design/README.md), tokens copied from site/src/styles/tokens.css.
         Paper oklch(97.5% 0.006 85); graphite oklch(18% 0.01 260); ink on graphite #f1eee9; amber #fbc044 only
         for the module of the mark and one underline. Family hues only on family modules.
         Display: Bricolage Grotesque 600, -0.02em, 120-168 px. Labels and code: IBM Plex Mono 28-34 px.
         Body: IBM Plex Sans 44 px. Flat fills, no gradients, shadows, grain or glow.
         Pixel art appears only inside the games' own frames, scaled with nearest-neighbour.
OBJECTS: code panel (graphite, a label strip, real lines of samples/HelloEngine/Program.cs);
         the star sprite (9 x 9 cells in the sample's palette on the sample's background #1d2b53);
         game footage (captured by tools/capture-footage.mjs from scripted runs of the four demos);
         the board (the mark's construction, widened: a kernel bar, walls, sockets open at the bottom);
         modules (family colour, family icon from site/src/components/brand/geometry.mjs, one-unit gap);
         the mark and wordmark (assets/brand, outlined, never redrawn).
MOTION:  arrive expo.out / power3.out; camera power3.inOut; zoom-through and seating power2.in-power3.in;
         a module travels 0.3 s and lands exactly on the beat; text never moves after it lands.
BANNED:  dark void with small floating things; glow, particles, gradients; fade-and-slide-up on everything;
         three cards in a row; a docked logo; feature lists; versions, platform names, counts, upstream names;
         invented UI or any shot posing as a feature demo; pixel fonts in the chrome.
```

## Shots

| Time | Bars | Scene | Hero move | Reads | Sound | Hands off |
| --- | --- | --- | --- | --- | --- | --- |
| 0-4 | 0-1 | `code` | the string map arrives line by line | "Code first." | bell notes, a tick per line | to the same panel |
| 4-7.4 | 2-3 | `code` | each row of the map resolves into a row of pixels | "= the sprite it draws" | pad and arpeggio enter, `resolve` | the star |
| 7.4-10 | 3-4 | `code` | camera dives into one yellow pixel | - | snare build, `riser` | the pixel fills the frame |
| 10-20 | 5-9 | `worlds` | seven cuts of real play, on the bar then on the beat | "Real games, captured as they run" | full drums, lead | cut on the downbeat |
| 20-22 | 10 | `board` | the game pulls back into a screen | - | `pullback` | the screen stands on a stem |
| 22-26 | 11-12 | `board` | the kernel bar and its sockets draw | "Underneath, a small kernel" | pad and bell only | empty sockets |
| 26-32 | 13-15 | `board` | six modules seat, one per bar half | "Every subsystem is a socket" | four-on-the-floor, `seat` x 6 | a full board |
| 32-35.3 | 16-17 | `board` | one module drops out, another seats; the game never stops | "Swap a module. The game plays on." | `lift`, `seat`, `riser-long` | the kernel bar grows into paper |
| 36-42 | 18-20 | `claim` | three lines land on three downbeats | "A game engine that orchestrates, not reinvents." | full drums, soaring lead | graphite rises |
| 42-44 | 21 | `mark` | the mark's board draws | - | snare build, `riser` | the empty socket |
| 44-50 | 22-24 | `mark` | the module seats on the downbeat; the wordmark wipes in | "There is always room for another module." | `impact`, final chord, tail | hold |

## QA frames

0.0 (not empty), 3.5, 6.5 (star complete), 9.7 (mid-dive), 10.1, 12.5, 17.5, 20.6, 24.5, 28.5 (three modules),
32.5 (socket empty, game playing), 35.7 (bar growing), 38.5, 43.5, 44.2, 47.5.
