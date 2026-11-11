# Motion ideas for the landing, from the three films

Notes for the later redesign. Nothing here is implemented in `site/`. Each idea names where to see it and why it
would survive being driven by scroll instead of by time.

## Ready to drive by scroll

| Idea | See it in | On a page |
| --- | --- | --- |
| **The playhead is the scroll position.** A tape of steps moves under a fixed amber line; steps ahead are outlines, steps passed are solid | Every step, 4.8-9.6 s | The page's own progress indicator, or a section where scrolling plays a sequence. It is a pure function of one number, which is exactly what a scroll timeline gives |
| **A module seats when its section arrives.** One block slides into its bay and lands | Assembly, 10-11 s; Seated, 26-31 s | Each capability section seats its module on the board as it enters the viewport; scrolling back unseats it. The site's `<Mark seat />` already does this once |
| **Map rows resolve into pixels.** A highlight runs down the code and each row becomes a row of the sprite | Seated, 4-6 s | The "code first" section: scroll scrubs the highlight line by line, with the real sample beside its real output |
| **One step opens into its parts** | Every step, 9.6-16.8 s | An explanation that unfolds in place: a small element in a diagram expands into a labelled frame and closes again as the reader moves on |
| **The structure flattens into the mark** | Assembly, 46.4-48 s | A hero that starts as the board in depth and presses flat into the logo over the first screen of scroll |

## Transitions between sections

| Idea | See it in | On a page |
| --- | --- | --- |
| **A ground opens from an edge, never fades.** Graphite spreads from the playhead as a hard edge | Every step, 24 s and 38.4 s | Light and dark sections meet at a moving edge tied to scroll, so both themes keep full contrast at every position |
| **An object becomes the next ground.** The kernel bar grows until it is the paper of the next scene | Seated, 35.3-36 s; both claim scenes | A rule or bar at the end of a section expands to become the background of the next |
| **Dive into a detail.** The camera enters one pixel, which opens on the next world | Seated, 7.4-10 s | Pinned section: scroll zooms into a part of a figure until it fills the viewport and the next section is inside it |

## Depth and composition

| Idea | See it in | On a page |
| --- | --- | --- |
| **A plane turning about a fixed line.** The tape rotates into depth around the playhead | Every step, 24-25.4 s | A flat strip (frames, a roadmap, a list of modules) turns into perspective as it leaves the reading area. Cheap: one rotation and a perspective |
| **Flat-shaded blocks.** Three tones per colour (front, top, side), no gradients or shadows | Assembly, throughout | Depth for the board and sockets that stays inside the "Socket" rules |
| **Slow drift inside a shot, hard cut between.** | Assembly, 20-40 s | Parallax of a few degrees on pinned figures, with section changes as clean steps |

## Colour, type and identity

- **Amber is one thing per film:** the module, the underline, the playhead. On the page: the one live element of
  each section.
- **Three states drawn without colour:** outline (not yet), amber (now), solid ink (done) in Every step; solid,
  dashed and dotted sockets on the site; the dashed guides and the empty plane in Assembly. One system could
  serve both status and progress.
- **One line of display type at a time, in a fixed place, that never moves after it lands.** All three films. On a
  page: a pinned headline whose words change with scroll while the figure beside it moves.
- **The mark as a destination.** Each film ends by arriving at the mark through its own device. The landing could
  open on one of them and close on another.

## What would not translate

- Anything that depends on sound: the layers arriving with modules, the picture being the score.
- Constant-speed motion: a reader's scroll is not constant, so the tape works as position, not as a clock.
- Fast cuts on the beat (the play montage in Seated).
