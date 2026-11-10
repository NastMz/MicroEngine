# MicroEngine art direction: "Socket"

## Creative decisions

**In one sentence:** MicroEngine is drawn as a board of labelled sockets with modules seated in them, because that is
how the engine is built, so the identity can grow with the engine without changing its language.

### The main choices

| Choice | What it is | Why |
|---|---|---|
| One device: socket and module | The mark, family icons, the landing board, docs headers, platform and roadmap boards, the 404 page and the OG image all draw the same thing | Recognition comes from one product-true artifact repeated, not from decoration. A demo card, a heading or a diagram is recognisable on its own |
| Family colours | Seven families (kernel, graphics, physics, audio, input, platform, tools), one hue each; the kernel stays neutral | A colour means a kind of module and nothing else, so it carries information wherever it appears |
| Status drawn, coloured and written | Seated module = ships; dashed empty socket = in the repository only; dotted = planned; the words are always written | Present and future must never be confused; colour is reserved for what ships |
| Credited upstream modules | SDL 3, raylib, .NET, NVorbis, Box2D, glTF drawn as named modules seated in MicroEngine's sockets | The engine orchestrates proven technology; crediting it is the stance, not a footnote |
| Warm paper and graphite, amber signal | Two first-class themes from OKLCH tokens; code panels and the player stay graphite in both | A technical, warm tone that is neither SaaS gradient nor retro night screen |
| Type: Bricolage Grotesque, IBM Plex Sans and Mono | Roman display face, Plex for reading, Plex Mono for labels and code | Character without a pixel costume; mono labels read like a parts list |
| Real output next to code | Frame strips captured by the engine's headless runs; real screenshots, never mock-ups | The engine's own output is the most honest illustration |
| Plain labels, no fake chrome | Code under a label strip, no window dots, tabs or knobs; motion is a module seating once | Engineering shown as it is, mechanical and short |

### How they carry MicroEngine's philosophy

| Philosophy | How the identity shows it |
|---|---|
| Modularity | Every subsystem is a socket with a contract; swapping an implementation is literally reseating a module |
| Building on existing technology | Upstream libraries appear as credited modules seated in the board, never hidden and never the hero |
| Honesty about status | Only shipping modules are seated and coloured; repository-only work (3D, Box2D) is an empty dashed socket |
| Pixel-art origins, kept but not defining | The demos keep their pixel art inside their own frames; the mark keeps a 16-unit grid; the chrome has no pixel face or grid |
| Growth toward HD, 2.5D and 3D | A new capability is a new module in an existing socket. The 3D socket is already drawn, empty and dashed: the direction is visible, nothing is promised |

### How to extend it

| To add | Do this |
|---|---|
| A page | Build it from tokens and the brand components (Quick path below); open with a rail and a legend like the other pages; show capabilities with `<Socket>` or the board, never as feature cards; verify both themes at 320 to 1280 px |
| A capability | One entry in `src/data/capabilities.ts` (family, status, contract, upstream); the landing board, roadmap and counts follow |
| A family | A hue in `tokens.css` (`--family-<name>`, light and dark), its icon geometry in `src/components/brand/geometry.mjs`, its name in `scripts/contrast.mjs`; run the contrast check. Only when the engine gains a new kind of subsystem |
| A status | A `--status-<name>` token, a drawing that differs without colour (solid, dashed, dotted), its words in `brand/Socket.astro`, `docs/families.mjs`, `src/data/capabilities.ts` and `docs/status-phrases.mjs`, and a row in section 7. Keep the rule: colour only for what ships |
| A shipped feature | Change its status in the data; the socket fills in. Never draw a module seated before it is in a published package |

MicroEngine is drawn as **a board of labelled sockets with modules seated in them**. The kernel is the board; graphics,
physics, audio, input, platform hosts and tools are modules that seat into it and can be swapped. One device, the
socket and its module, is repeated everywhere: in the mark, the family icons, the diagrams, the status of every
capability, and the Open Graph image.

This folder is design material for people working on the site. The site does not publish it
(`src/lib/repo-docs.mjs` lists the only Markdown it publishes, and this file is not on that list).

## Quick path

| You need | Use |
|---|---|
| A colour, a size, a font | A token from `src/styles/tokens.css`. Never a literal value. |
| The logo in a page | `<Lockup />` or `<Mark />` from `src/components/brand/` |
| The logo as a file | `src/assets/brand/` (`mark-*.svg`, `lockup-*.svg`; light, dark, mono) |
| A subsystem, a capability, a backend | `<Socket family=… module=… status=… />`, or `empty` for anything not published |
| A family icon | `<FamilyIcon family="physics" />` |
| A diagram | Inline SVG with `class="illustration"` plus `<Callout />` |
| A code panel | `<CodeLabel file=… lang=… />` before the block; the block is a `.code-surface` |
| What code produces | `<FrameStrip frames=… caption=… />` with real captures |
| Anything that scrolls sideways | It must take focus (`tabindex="0"` and a label) so a keyboard can scroll it; docs tables and diagrams get it in `docs/scroll-regions.mjs`; `npm run test:browser` fails otherwise |
| A status on its own | `<span data-status="repo"><span class="status-mark"></span>In the repository, not published</span>` |

Checks: `node scripts/contrast.mjs` (WCAG AA for every token pair, both themes) and `npm test` (it also fails when the
logo files or the favicon colours drift from their sources).

## 1. Concept

### Why "Socket"

The research (Godot, Unity, Unreal, Defold, MonoGame, Bevy, raylib, LÖVE, Stride, FNA, PICO-8, Flax, Heaps; and
outside games Zig, Rust, Astro, Linear, Stripe docs, Teenage Engineering, Playdate, Tailwind) gave four findings:

1. **Recognition comes from one product-true artifact repeated everywhere** (the PICO-8 cartridge, the Playdate crank,
   a labelled anatomy drawing, the raylib cheatsheet), and from marks built as geometric constructions that hold at
   16 px in one colour.
2. **Show the engine's own output** next to the code that made it (Zig's test output, Tailwind's and Stripe's
   side-by-side docs, Bevy's live examples).
3. **Keep present and future visibly apart** (Godot's priorities page, Bevy's "still in development").
4. **A stance beats adjectives.** FNA says "do it right or not at all"; raylib calls itself spartan.

MicroEngine's own particularity is architectural: a small kernel that orchestrates existing technology (SDL, raylib,
Box2D, .NET, WebAssembly) behind one contract, with subsystems that can be replaced. A socket is the honest picture of
that: the contract is the socket, an implementation is the module, and an empty socket is a place where something is
not yet available. The device carries the product's truth, so it can be repeated without becoming decoration.

### Rejected directions

| Direction | Idea | Why not |
|---|---|---|
| Fixed Step | The engine's fixed-step loop as the visual system: frame strips, ticks, timelines | Austere and niche alone; it survives as the documentation device (the frame strip) |
| Resolution | Pixel art resolving into HD, 2.5D and 3D across the page | Promises 3D before it ships, which the site must not do |
| Standing on shoulders | Upstream technologies drawn as the hero | Credits others over MicroEngine's own value; survives as credited modules |
| Pixel workbench (the previous site) | Pixel display face, amber on a night grid | Reads as a retro pixel-art tool; it boxed the engine into its first demos |

### Traps this direction avoids

Generic SaaS (dark hero, gradient glow, three icon cards); stock Starlight with an accent colour; retro costume (pixel
font everywhere, CRT, PICO-8 cosplay); hardware cosplay of Teenage Engineering (the copy stays plain, there are no fake
knobs or screws); showing 3D or unpublished backends as available.

## 2. The mark

![Mark exploration sheet: three constructions at 16, 32, 64 and 256 px, one colour and full colour, light and dark](explorations/mark-sheet.webp)

### Construction

Drawn on a 16-unit grid; every edge falls on a whole unit, so the 16 px favicon is pixel-exact.

- **Board**, the kernel bar (3 units tall) with three walls (2 units wide) and two sockets (5 units wide) open at the
  bottom. Read together, it is a lowercase **m**.
- **Module**, 3 × 8 units, seated in the right socket with a **one-unit gap** on its sides and top, resting on the
  baseline. The left socket stays empty: there is always room for another module.
- The drawing spans y 2..14; lockups crop to that box so it aligns with the wordmark's capitals.

Source of truth: `src/components/brand/geometry.mjs`. The files in `src/assets/brand/` are generated from it by
`node design/tools/build-assets.mjs`; a test fails if they drift.

### Why this one

Three constructions were drawn and rasterised at 16, 32, 64 and 256 px (`explorations/`):

| Candidate | Reading | Verdict |
|---|---|---|
| A: corner module, a square with a block pulled out of its corner | Strong at 16 px | Rejected: reads as a generic "copy" or "layers" icon; says "detached", not "seated" |
| **B: m-socket**, the kernel bar with two sockets, one module seated | "m" for MicroEngine, a board, a seated module and an empty socket in one shape | **Chosen**: holds at 16 px in one colour (the 1 px gap survives), and tells the product's story |
| C: bracket socket, `[■]` | Code-first, legible | Rejected as the mark: generic code glyph, no letter, no board. Kept as the Tools and SDK family icon |

### Versions

| File or component | Use |
|---|---|
| `<Mark />`, `<Lockup />` | In pages: colours from tokens, so they follow the theme |
| `mark-light.svg`, `lockup-light.svg` | On light backgrounds, outside the site |
| `mark-dark.svg`, `lockup-dark.svg` | On dark backgrounds, outside the site |
| `mark-mono.svg`, `lockup-mono.svg` | One colour (`currentColor`), for print, embossing, single-ink use |
| `/favicon.svg` | The mark with fixed colours for the browser's light and dark schemes |
| `/og.png` | Built at build time from the lockup and the socket board (`src/pages/og.png.ts`) |

The wordmark is Bricolage Grotesque SemiBold at -0.02 em tracking. In files it is outlined
(`src/assets/brand/outlines.json`, made by `design/tools/outline-text.mjs`), so it does not depend on installed fonts.

### Usage rules

- **Clear space:** at least the module's width (3/16 of the mark's width) on every side; in a lockup, the gap between
  mark and wordmark is 0.3 em and nothing enters it.
- **Minimum size:** the mark at 16 px; the lockup at 14 px font size. Below that, use the mark alone.
- **Colour:** board in ink, module in amber, on paper. Or everything in one colour. Never a family colour on the
  module (that would claim the mark is one subsystem), never the board in amber.
- **Do:** place it on plain paper or graphite; let the module animate into its socket once (`<Mark seat />`).
- **Don't:** rotate, outline, add shadows or gradients, fill the empty socket, separate the module from the board,
  put it on a busy screenshot without a solid plate behind it, set "MicroEngine" in another face beside it.

## 3. Colour

Colours are OKLCH tokens in `src/styles/tokens.css`, each written once as `light-dark(light, dark)`. Every text and
background pair is checked against WCAG AA in both themes by `scripts/contrast.mjs` (4.5:1 text, 3:1 focus ring), and
every colour is checked to lie inside sRGB so the computed ratio is the one the browser shows.

### Two first-class themes, the landing included

| Theme | Neutral base | Character |
|---|---|---|
| Light | warm paper `oklch(97.5% 0.006 85)`, graphite ink | A printed manual: diagrams and labels read like a product sheet |
| Dark | graphite `oklch(18% 0.01 260)`, warm paper ink | The workbench at night, continuity with the previous site |

**Decision: the landing and demo pages get both themes**, following the reader's system, and the documentation's
theme switch (stored by Starlight as `starlight-theme`) applies to them too. Reasons: the owner asked that the landing
and the documentation not look like two projects, and the docs already switch; a manual-like light theme is where the
socket diagrams read best; and dark-only was part of the "retro tool at night" perception the redesign leaves behind.
Exceptions, by rule: **code panels and game stages stay graphite in both themes** (`.code-surface`, `.astro-code`,
`[data-stage]` set `color-scheme: dark`, so every token inside resolves to its dark value). Code uses a dark
highlighting theme, and a game's picture is its own light.

### Roles

| Role | Tokens | Rule |
|---|---|---|
| Neutral | `--color-paper`, `-paper-2`, `-paper-3`, `--color-rule`, `--color-ink`, `-ink-2`, `-ink-3`, `-ink-4` | 95% of every page. `ink-4` is never text |
| Signal | `--color-accent` (text), `--color-accent-fill` (buttons, the mark's module), `--color-accent-ink`, `--color-focus` | MicroEngine's own voice: links, the primary action, the module in the mark |
| Families | `--family-kernel`, `-graphics`, `-physics`, `-audio`, `-input`, `-platform`, `-tools` | Mean a kind of module, nothing else. Never decoration, never a status |
| Status | `--status-available`, `-repo`, `-planned` | See section 7 |
| Verdicts | `--color-ok`, `-partial`, `-no` | Compatibility answers (player, platform tables) |
| Games | `--color-meadow`, `-courtyard`, `-trail`, `-neon` | Kept from each game's palette; only inside that demo's own frame |

Family hues: graphics violet 295, physics orange 45-50, audio rose 355, input blue 245, platform teal 195, tools
chartreuse 125; the kernel is neutral because it is the board. A family colour always travels with its icon and its
written name, so colour is never the only signal. A socket's fill is its family colour mixed into the paper at
`--tint` (12%); text on it stays in the family colour or ink, and both pairs are in the contrast check.

## 4. Typography

| Face | Role | Weights shipped | Licence |
|---|---|---|---|
| Bricolage Grotesque | Display: page titles, section heads, the wordmark | 600 (Latin) | SIL OFL 1.1 |
| IBM Plex Sans | Reading text, UI | 400, 600 (Latin) | SIL OFL 1.1 |
| IBM Plex Mono | Code, panel legends, labels, step numbers | 400, 500 (Latin) | SIL OFL 1.1 |

All self-hosted through @fontsource, Latin subset, only the weights above (`src/styles/fonts.css`). Bricolage was
chosen against Space Grotesk (ubiquitous in developer tooling), Schibsted Grotesk (too plain to carry a brand beside
Plex) and Archivo Expanded (strong "equipment plate" voice, but its width axis costs a 90 KB variable file). Bricolage's
ink traps and open forms give character at display sizes and stay sober at 18 px; its grotesk skeleton sits well over
Plex.

- Headings are roman, never italic; emphasis is weight or colour.
- Display tracking `--tracking-display` (-0.02 em), leading `--leading-tight`.
- Legends are Plex Mono, uppercase, `--tracking-label` (0.06 em), `--text-xs`: the voice of a label on a module.
- Scale: `--text-xs` 13 px to `--text-display` (fluid, 40 to 76 px), roughly 1.25 between steps.
- Pixel faces are not part of the brand. A game may use its own inside its screenshots.

## 5. Layout and grid

- Page: `--page-max` 76 rem, side gutter `--gutter` (16 px at 320 px wide, 40 px on desktop), reading measure 66 ch.
- Spacing comes from the 4 px scale (`--space-2xs` 4 px to `--space-4xl` 104 px). Sections breathe at `--space-3xl`.
- Shapes are square-cornered (`--radius-sm` 2 px), with hairline rules. The **gap** between a module and its socket
  is `--rule-gap` (2 px): the same gap as in the mark. Use it for anything seated in anything (a frame in a strip, a
  module in a bay).
- Compositions read like a board: things line up on a shared rail (the kernel bar), modules sit on it, labels sit
  above what they name. Avoid centred hero stacks and three-card rows.
- Mobile: no horizontal page scroll at 320, 375, 414 and 768 px; wide drawings and frame strips scroll inside their own
  box; grid tracks with images use `minmax(0, 1fr)`; tap targets at least `--tap` (44 px) on coarse pointers.

## 6. Illustration

![The brand kit rendered in the light theme](explorations/kit-light.webp)

- Orthographic, flat, hairline: no perspective, no gradients, no glow, no drop shadows.
- A board line (2 px ink), sockets (1 px rule colour), modules (family tint with a 1 px family stroke).
- Parts are named with **numbered callouts**: a 4 px square anchor on the part, a hairline leader that runs vertically
  then horizontally, an 18 px numbered square and a mono legend (`<Callout />`). Like the parts list of a manual.
- Upstream technologies (SDL, raylib, Box2D, .NET, WebAssembly, glTF) are drawn as **credited modules**: their name on
  the module, never their logo, and only where MicroEngine really uses them.
- Every illustration is inline SVG with a `<title>` (or `aria-labelledby`) that says what it shows; the legends are real
  text.
- Family icons (`<FamilyIcon />`) use the mark's construction: a board in the text colour, one module in the family
  colour, a one-unit gap.

## 7. Present and future

The rule: **only what ships gets colour.** The drawing says the status first; the colour repeats it; the words are
always written.

| Status | Drawing | Colour | Words |
|---|---|---|---|
| Available (in the published SDK) | Solid module seated in a solid socket; solid square marker | `--status-available` | "Available" |
| In the repository, not published (experimental) | **Empty dashed socket**, module named inside in mono; dashed marker | `--status-repo` (neutral) | "In the repository, not published" |
| Planned | Empty dotted socket; dotted marker | `--status-planned` (neutral) | "Planned" |

`<Socket empty status="repo" module="Box2D" />` is how an unpublished backend appears. The SDL_GPU device ships in
the `MicroEngine.Sdl` package (desktops, opt-in), so it is a seated module with the available status:
`<Socket family="graphics" module="SDL_GPU" status="available" />`. There is no status between available and in the
repository: the site describes the SDK that contains it as published. The 3D path (Graphics3D), Box2D physics
and any other unpublished system is never drawn as a seated module until it ships in the SDK. The Open Graph image
shows four seated families that ship today and one empty dashed socket, unlabelled.

## 8. Motion

- Mechanical and short: `--dur-fast` 120 ms, `--dur-mid` 220 ms, `--ease-out`; seating uses `--ease-seat`, a small
  overshoot, so a module "clicks" into place.
- The signature motion is the module sliding into its socket (`<Mark seat />`, once, on load). A socket's module lifts
  2 px on hover, as if about to be swapped.
- Nothing loops. On the landing, motion is tied to the scroll position ([Landing in motion](#landing-in-motion)):
  a reader who does not scroll sees a still page, and every other page stays still.
- Under `prefers-reduced-motion: reduce` every animation and transition is removed (components check it; `site.css`
  also shortens all transitions globally).

## 9. Code and screenshots

- **Code** sits on graphite in both themes, under a `<CodeLabel>` strip: the file path, an optional family icon and
  note, the language. It is a label, like the legend on a module, not a window: no title bar, no traffic-light dots,
  no fake tabs.
- Quoted code equals its sample region (`<!-- code: path#region -->`, checked by the site build).
- **What code produces** is shown with `<FrameStrip>`: real frames captured by the engine's scripted, headless runs,
  numbered 01, 02, 03 in mono above each frame, with alt text for each and a caption that says what the run did.
- **Screenshots** are real captures, pixel-sharp (`image-rendering: pixelated` for pixel-art games), in a hairline
  frame, never tilted, never in a device mock-up, never under a gradient.

## 10. Pixel-art origins

The four demos are pixel-art games and stay exactly that: their screenshots, their colours (the game tokens) and their
own fonts live inside their frames. The brand around them is not pixel art: no pixel display face in the chrome, no
pixel grid as identity. The mark's construction keeps a quiet trace of the origin (whole units on a 16-grid, crisp at
16 px), and the landing's sprite-from-code figure can stay as one example of what the engine draws, not as the
costume of the whole site. The site shows pixel art as *one thing MicroEngine renders*, so it can later show HD and
other output with the same frames and strips.

## 11. Licences

| Asset | Licence | Credit |
|---|---|---|
| Bricolage Grotesque (shipped, and outlined in the logo files and Open Graph image) | SIL OFL 1.1 | Copyright 2022 The Bricolage Grotesque Project Authors |
| IBM Plex Sans, IBM Plex Mono (shipped, Plex outlined in the Open Graph image) | SIL OFL 1.1 | Copyright 2017, 2019 IBM Corp. |
| The mark, family icons, components, illustrations, Open Graph image | MIT, with the rest of the site | MicroEngine |
| Game screenshots used in strips and figures | All rights reserved (the demos), plus the CC0 assets in each game's `CREDITS.md` | Each demo page carries its notice |

The fonts are listed in `THIRD-PARTY-NOTICES.md` (section "This website"), which holds the OFL text. The outlined
glyphs in `src/assets/brand/outlines.json` are a use of the fonts under the OFL (documents made with the fonts are
not bound by it); they are credited the same way.

## 12. Exploration evidence

`explorations/` keeps the three mark candidates as SVG, the comparison sheet (16/32/64/256 px, one colour and full
colour, light and dark, rasterised at real size and enlarged) and the brand kit rendered in both themes.

## Notes for the units that apply this

- **Shell and landing:** use `<Lockup />` (already in the header) and the Socket board for the architecture story;
  move the hero code figure to `.code-surface` and delete `.sprite-code` from `brand.css`; the 32 px grid background is
  a leftover of the pixel workbench and is yours to keep or drop.
- **Demos and player:** game tones stay inside each demo's frame; the player stage is graphite in both themes.
- **Docs:** Starlight's variables already map onto the tokens for both schemes (`starlight.css`); code blocks are
  Expressive Code, not `.astro-code`, so give them the `CodeLabel` treatment there.
- **Content:** anything not in the published SDK is an empty dashed socket with its status written out.

## Landing in motion

The landing tells MicroEngine in three acts, each taken from one of the films (`media/`), and each a picture the
reader moves by scrolling. This section replaces the earlier rule that nothing moves on scroll; every other rule holds.

- **One mechanism.** `src/scripts/scroll.ts` gives each `[data-scroll]` element its progress through the window as
  `--p` (and `--step`, the same in whole steps), and marks `[data-reveal]` elements with `data-seen`. CSS does
  everything else. No animation library; no scroll timeline API, which not every supported browser has.
- **A function of position, never of time.** Scrolling back plays everything backwards. Nothing takes the scroll over:
  the one held section (`.act-pin`) is a tall section with a sticky child, only on screens at least 64 rem wide and
  44 rem tall.
- **Finished by default.** Motion styles apply only under `html.scroll-live`, which the script sets unless the reader
  asked for reduced motion. Without the script, or with reduced motion, every element is in its final state.
- **Act one, code** (`CodeToSprite.astro`, from Seated): the sample's string map beside the sprite it draws; a reading
  line steps down the map and each row of the sprite appears with its row of code.
- **Act two, the board** (`Colonnade.astro` in the opening, from Room for another; then the socket board): the mark's
  construction as flat-shaded boxes, three tones a colour, no gradients or shadows. The camera comes round as the
  reader leaves the opening. Further down, each bay's modules seat as the bay arrives.
- **Act three, the loop** (`FrameTape.astro`, from Every step): real frames move under a fixed amber playhead; frames
  passed are full, frames to come are faint. `.page-tape`, the ruler at the top of the window, is the same picture for
  the page itself: a tick per section and the playhead at the scroll position.
- **The inverted ground** (`.ground.flip`): a section in the other theme's colours (`color-scheme` flipped, so every
  token resolves the other way in both themes). It never fades in: its edge crosses the page as it arrives.
- **Amber stays one thing per picture:** the reading line, the playhead, the play control.
- **The films** (`Films.astro`, `/films/`): three cards with a frame of each film; a film's file is requested only
  when its card is pressed, and released when the player closes. Never describe one film as a version of another.
- Checks: `tests/site.test.mjs` (the films' data, no video in the built landing) and `tests/browser.mjs` (no film
  requested before a card is pressed; the layout checks at four widths).

## Landing and shell

Added by the shell and landing unit; this section extends the rules above, it changes none of them.

- **Background:** plain paper. The 32 px grid of the pixel workbench is gone; drawings, rules and modules carry the
  identity (the grid token has been removed from `tokens.css`).
- **Header:** the lockup left, the navigation right in Plex Mono. Under 48 rem the navigation takes its own line under
  the lockup, never wraps, and spreads its five links across the width. The current page has a short amber bar under
  its name: a module, not an underline. The skip link stays the first Tab stop.
- **Footer:** opens with a 2 px ink line (the board's edge), on `--color-paper-2`: the lockup and the statement
  ("A small kernel, modules you can swap, games written in C#."), the links in three groups, then the licence line with
  the demos' "all rights reserved".
- **Buttons:** flat and square-cornered; they press 1 px on `:active`. The old 4 px offset shadow is gone.
- **The socket board** (`src/components/landing/SocketBoard.astro`, `BoardSlot.astro`): the mark's construction at page
  size. The kernel is the solid ink bar at the top with its own modules in sockets cut into it; the other families hang
  from it in paper bays separated by 2 px ink walls, open at the bottom like the mark. Each module is a tinted module in a
  hairline socket (seated: available), an empty dashed socket (in the repository) or an empty dotted socket
  (planned), always with its status written under it. It reads `src/data/capabilities.ts` and nothing else, so the board
  changes when the capability list does. In the dark theme the board inverts with the mark (light board, dark bays).
- **The opening drawing** (`BoardDrawing.astro`): the same construction as an SVG parts drawing, five sockets with
  four shipping families seated and one empty dashed socket for 3D, named by three `<Callout>`s. The modules seat once
  on load, one after another (`--ease-seat`), and not at all under reduced motion.
- **Credited modules:** upstream technologies on the landing are mono names in a neutral module (ink-3 stroke on
  paper-2) inside a hairline socket, with their role beside them: no family colour, because they are not MicroEngine's
  families.
- **`DemoCard`** (`src/components/DemoCard.astro`): the screenshot sits one gap under a 2 px rule in the game's colour,
  then a plate with a game-coloured square and genre label, the title in the display face, the tagline and how its
  content is made. The game's colour stays inside the card; the card is paper and rules. Hover lifts it 2 px and turns
  its border to the game's colour. The default slot adds lines (sizes, downloads) to the plate.
- **Code panels** on the landing are `.code-surface` figures: `<CodeLabel>` then the block, both graphite in both
  themes. The block takes `--color-paper-2` as its background so it matches the label.

## Documentation

The Starlight documentation is drawn with the same tokens, type and devices as the landing (T4). Legibility comes
first: body text in ink at `--text-md` with `--leading-body`, a 46 rem column, square shapes and hairline rules.

| Part | Where | Treatment |
|---|---|---|
| Starlight variables and components | `src/styles/starlight.css` | Every `--sl-*` colour maps onto a token in both themes; header on a 2px ink rail; sidebar group names as mono legends; the current page seated like a module (amber tint, 1px amber edge); h1 and h2 in the display face, h2 on a hairline rail; tables as parts lists (mono heads over a 2px ink rule); blockquote notes with a 2px ink edge; pagination as labelled cards; the search dialog on paper |
| Header | `src/components/DocsSiteTitle.astro` | The lockup, a "Docs" legend, then Demos and Roadmap |
| Family legend | `src/components/docs/PageTitle.astro` | Above each title: the page's family icon and name, then its sidebar group. The family comes from `src/components/docs/families.mjs`, keyed by slug (the Markdown has no frontmatter); API pages take the family of their namespace. Pages about no single module stay neutral |
| Socket card | `src/components/docs/SocketCard.astro` | Under the title of the Graphics, Audio, Input, Tile maps and collision, and Backends pages: the contract as the socket, the modules that seat in it, each with its status written. Not published: an empty dashed bay |
| Written statuses | `src/components/docs/status-phrases.mjs`, via the `MarkdownContent` override | "In the repository, not published" (and "in the repository only, not in a package") in the text gets the status marker and colour; never inside code or headings |
| Code | `ec.config.mjs` + `starlight.css` | Expressive Code with one dark theme (`github-dark-dimmed`) in both site themes, on `--color-paper-2` under `color-scheme: dark`. The strip above is the `CodeLabel` idea: a 2px ink rail, the file name on the left, the language on the right (added by a small plugin). Shell blocks get the same strip, no terminal window. Syntax colours are lifted to 5.5:1 (`minSyntaxHighlightingColorContrast`), comments included |
| API reference | `starlight.css` only | Generated pages keep the same treatment; member headings with an anchor set in mono |

Family mapping decisions: content (assets) is drawn as **kernel**, because `Game.Assets` is a core service rather than a
module; persistence as **platform**, because where saves live is the host's storage; packaging, signing and the tool
as **tools**; UI widgets and achievements stay neutral.

Platforms, roadmap and 404 pages: the platforms page is one rail ("your game") with six platform modules seated under
it, then each platform as a parts list; the roadmap is two boards, **Present** (solid ink rail: available, seated
modules in their family colours) and **Future** (dashed rail: repository items as empty dashed bays in
mono, planned items as dotted outlines grouped by track); the 404 is an empty dashed socket.

## Demos and players

Added by the demos unit; this section extends the rules above, it changes none of them.

- **The demos are proof, not the product.** The index opens with "Proof, built with MicroEngine" and lays the four
  `DemoCard`s under a rail, like the modules of the platforms page; each demo page names the engine first
  ("built with MicroEngine") and lists what the game exercises before anything else.
- **Family markers** (`src/components/demos/Exercises.astro`, data in `exercises.mjs`): each engine feature a game's
  code calls is a small module, the family icon beside its written name in mono, on the family tint. On a demo page
  each marker links to the manual page that explains the feature; in a card it does not (the card is one link). A
  marker is a claim about the game's code: each carries the patterns that must appear in `games/<Id>/**/*.cs`, and
  `npm test` fails when one stops matching. Never add a marker for something the game writes itself.
- **Where the game colour goes:** the player's 2 px top rule, the rule over each numbered screenshot, and the
  `DemoCard`s of the other demos. The page head, the downloads, the parts list and the breadcrumbs are brand paper and
  ink. The visitor's own system in the downloads is marked in amber (the brand's signal), not the game's colour.
- **The player** is one graphite object in both themes (`media-surface` on the whole player): the stage and the
  toolbar strip seated one gap apart on a rule-coloured board. Before Play the poster stays clear and the start plate
  sits in its bottom-left corner (legend "Web build · size", the Play button with a drawn triangle, a note); under
  40 rem the note is dropped so the plate fits the 16:9 poster. No gradient over the screenshot.
- **State panels** are plates over a scrim: a mono legend with a square marker, the title in the display face, the
  message, the actions. The plate's top rule and legend carry the verdict colour: neutral (checking, missing),
  `--color-partial` (made for a keyboard), `--color-no` (unsupported, error). The words always say it too.
- **Focus** inside the player is the 3 px focus ring; on an amber primary button it is drawn in ink, because the
  focus colour on graphite is too close to the amber fill. Toolbar controls are at least 36 px (44 px on coarse
  pointers).
- **Demo page parts** follow the platforms page: each part under a 2 px ink rail, its name in a 16 rem column from
  64 rem; compatibility uses the square verdict markers; facts are a mono-labelled parts list. Screenshots are
  numbered 01, 02... like a frame strip, in two columns, because they are a playthrough in order.
