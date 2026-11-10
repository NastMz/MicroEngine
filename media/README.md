# MicroEngine films

Three short films about MicroEngine, and everything needed to render them again. Each tells a different thing;
none replaces another.

| Film | Folder | Tells | Length |
| --- | --- | --- | --- |
| Seated | [`seated/`](seated/SPEC.md) | The result: code becomes a sprite, the sprite becomes games | 0:50 |
| Room for another | [`room-for-another/`](room-for-another/SPEC.md) | The architecture: a kernel, sockets, modules chosen and added | 0:56 |
| Every step | [`every-step/`](every-step/SPEC.md) | The loop: equal steps in the same order, and what happens in them | 0:55 |

The films are all rights reserved ([`LICENSE`](LICENSE)); the tools that build them are MIT, like the engine.

## What is here, and what is not

Kept in Git: each film's compositions (`index.html`, `compositions/`), its score and effects as code (`audio/`), its
documents (`SPEC.md` the shot list, `STORYLINES.md` the directions considered, `REVIEW.md` what was checked), the
catalog ([`films.json`](films.json)) and the tools. [`LANDING-MOTION.md`](LANDING-MOTION.md) lists what the films
gave the site's landing.

Not kept in Git, because every one of them is regenerated: fonts and brand files (copied from `site/`), audio
(rendered by `MicroEngine.Audio`), footage (captured from the demos' scripted runs), renders and posters. The rendered
videos live in a GitHub release of their own.

## Building a film

Needs the .NET SDK of the repository, Node 22, FFmpeg on `PATH` and Chrome; `npm ci` in `site/` first, for the fonts.

```bash
node media/tools/films.mjs build every-step
```

`build` runs four steps, each also a command of its own (`prepare`, `audio`, `footage`, `render`), for one film or,
without a name, for all three:

| Step | Does | Writes (ignored by Git) |
| --- | --- | --- |
| `prepare` | Copies the fonts from the site's packages and writes the outlined wordmark from the site's brand files | `<film>/assets/fonts`, `<film>/assets/brand` |
| `audio` | Renders the score and effects with the engine's `Song` and `Synth`; for Every step also embeds the score's steps in the film, which draws them | `<film>/assets/audio` |
| `footage` | Plays the demos with their own scripts and captures every other update | `.cache/footage`, `<film>/assets/footage` |
| `render` | Checks the film, renders the master with HyperFrames, encodes the web file and takes the poster frame | `<film>/renders/<film>.master.mp4`, `<film>.mp4`, `<film>.png` |

The films are rendered with [HyperFrames](https://hyperframes.heygen.com) 0.9.30 (the version is pinned in
`tools/films.mjs`); to work on one, `npm run dev` in its folder opens the preview.

Films are not rendered in CI. A film is rebuilt when its sources change or a new one is made; a change anywhere else
in the repository never renders video.

## Publishing

The web files are assets of one GitHub release, `films-1`, which is neither a demo release (`v*`) nor an SDK release
(`sdk-v*`) and is never marked as the latest. The site plays them from there, and only when a visitor presses play.

```bash
node media/tools/films.mjs publish
```

uploads `<film>/renders/<film>.mp4` for the three films, replacing the assets of the same name. A film's address
(`https://github.com/NastMz/MicroEngine/releases/download/films-1/<film>.mp4`) does not change when it is replaced.
When a film changes its poster, copy `<film>/renders/<film>.png` to `site/src/assets/films/`.

A fourth film is a new folder, an entry in `films.json`, a poster in the site's assets, and `publish`.
