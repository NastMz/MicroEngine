# ADR 0015: MIT engine, all-rights-reserved demo games

## Status

Accepted (owner decision, 2026-10-31). Supersedes the licensing model of the
[distribution proposal](../legal/distribution-proposal.md), the [SDK licence draft](../legal/microengine-sdk-license-draft.md)
and the [source-available proposal](../legal/source-available-proposal.md). Audit and publication steps:
[PUBLIC-1](../evaluations/public-1.md).

## Context

MicroEngine is being prepared for a public repository. A proprietary SDK licence, then a source-available licence,
were studied; both need custom legal text, a contributor licence agreement and enforcement, and both keep developers
from treating the engine as a normal open source dependency. The owner accepts that anyone may fork and sell the
engine, and wants to discourage only the removal of copyright notices. The four demo games, on the other hand, are
finished products whose names, content and code the owner wants to keep.

## Decision

- **Engine under the standard MIT License**, unmodified, in the root `LICENSE`: `src/`, engine tests, CI, web host,
  site code, docs, platform hosts and `samples/` (HelloEngine stays freely reusable). No extra restriction.
- **Demo games all rights reserved** in `games/LICENSE`: Meadow Run, Courtyard Key, Trail Duel and Neon Rift, their
  test projects and builds. Reading, building and running them to learn, and reusing ideas and techniques, are
  allowed; redistributing, selling or republishing them under another name are not.
- **Third-party files keep their own licences.** The image and audio files listed in each game's `CREDITS.md`
  (CC0 1.0) are excluded from the games' notice; libraries are listed in `THIRD-PARTY-NOTICES.md`.
- **Notices travel with builds.** Every browser package ships `LICENSE.txt` (the game's notice),
  `licenses/microengine-LICENSE.txt` (MIT for the engine inside it) and the third-party notices; both licence files
  are inputs of every demo key.
- **Contributions are inbound = outbound** (MIT; GitHub Terms D.6), with no CLA.

## Consequences

- The site, documentation and contributor files describe MicroEngine as open source and the demos as reserved; the
  site build checks that every demo page carries the reserved notice instead of forbidding the words "MIT" and
  "open source".
- GitHub detects the root licence as MIT for the whole repository; the README's licence table and `games/LICENSE`
  carry the exception, and each game's README repeats it.
- Changing the licence files changes every demo key, so every package is rebuilt once.
- The MIT `LICENSE` that the first 68 commits of the rebuild still carried (2026-10-29/30) no longer contradicts the
  engine's licence, but it does contradict the demos': all four games first appear in those commits (Meadow Run
  9c6f14d, Courtyard Key 9e231da, Trail Duel 08bd6a8, Neon Rift e57d28a) with only the MIT file at the root and no
  `games/LICENSE`. Publishing those snapshots would show the demos next to an MIT grant; PUBLIC-1 therefore
  rebuilds the public history from `71115bc` in this repository (PUBLIC-1, section 3).
