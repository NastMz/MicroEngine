# 0008 — Web packages: one host, gesture start, pinned toolchain

Status: Accepted (2026-10-30, branch `feat/web-support`). Builds on [0007](0007-browser-host.md).

## Context

WEB-0 ran Neon Rift in a browser with a host project of its own and a hand-installed toolchain. WEB-1 needs all
four demos as reproducible packages that a landing page can embed later. Three things had to be settled:
how one host serves several games, how a page starts a game so input and audio behave, and how saving behaves
when browser storage refuses a write.

## Decision

- **One host project, the game chosen at build time.** `web/Host` is the only host. `-p:Game=<Id>` selects an
  entry in `web/games.props`: a title, the game project and the expression that builds the game (the one its
  desktop `Program.cs` uses). MSBuild generates the entry point, the content manifest and `game.json` from it.
  No plugin system or runtime loading: each package contains exactly one game. Adding a demo is one entry.
- **`GameSession` owns the run lifecycle for frame-driven hosts.** Start builds a new game and platform, `Tick`
  runs one frame, the end of a run (quit, error, stop) shuts the game down and releases the platform. Restarting
  is a new `Start`, without reloading the page. Construction and load failures are reported, not thrown.
- **The game starts inside a user gesture.** The page loads the runtime and content, then waits for a click or
  key. `WebHost.Start` runs synchronously in that event, so raylib creates its `AudioContext` while the browser
  allows audio, and keys pressed before that never reach the game (the page sees them first and drops them).
- **Saving never stops a game.** `SaveStore.Save` returns false when the medium refuses a write (full disk,
  storage quota) instead of throwing into game code. A slot that exists but could not be read is not written
  by that store, because the game is then running with fresh progress that would replace the real one. The page
  tells the player when storage is blocked (a session-only store is used) or full.
- **Pinned, private toolchain.** `web/toolchain.env` pins the .NET SDK, the workload set (and so emscripten)
  and the raylib commit; `web/setup.sh` installs them in a private folder and rebuilds raylib with that
  emscripten; `web/build.sh` builds packages. Nothing changes the system .NET installation.
- **Packages are static folders.** `dist/web/<Game>/` holds `index.html`, the runtime and the content; it runs from
  any static server and is embedded with an iframe. The page reports its state to the embedding page with
  `postMessage`.

## Consequences

- Desktop launchers are unchanged; `GameRunner.Run` and `GameSession` both drive `GameLoop`.
- Code that called `SaveStore.Save` and caught exceptions keeps working; the catch is no longer needed.
- Raising the .NET SDK means updating `web/toolchain.env` together (SDK, workload set, runtime pack, emscripten)
  and rebuilding raylib, which `web/setup.sh` does when the stamp changes.
- The browser build uses invariant globalization (no ICU data); the games format only digits.
- Not decided: streaming assets instead of preloading them, an AudioWorklet audio backend for raylib, touch input.
