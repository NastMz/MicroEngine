# 0001 — Rebuild the engine and games from a clean slate

Status: Accepted (2026-10-29)

## Context

The previous tree (tag `reference/pre-rebuild`) contained a deliberately minimal tracer engine:
a fixed-step loop, rectangle/image/label drawing, one AABB helper and a desktop adapter, wrapped in
an extensive approval-process documentation layer. Its three games were single-screen flows
(three short platformer courses, one lever/key room, a small RPG quest) drawn with flat rectangles
and a few third-party sprites. Baseline captures showed functional but not designed experiences.

The owner's new mission asks for a technically solid engine, three complete and differentiated
games with a deliberate visual identity, sound and feedback, proportional tests and documentation,
with explicit permission to rebuild anything.

## Decision

Rebuild on branch `feat/engine-rebuild`:

- A new core library with the systems the three genres share (scenes, cameras, tiles, motion,
  particles, animation, font, UI widgets, audio synthesis, saves, scripted runs).
- Three games rewritten from scratch on that core.
- The old tree is removed from the branch, not edited: it remains intact at the tag and on `main`.
- The previous approval-gated process docs are replaced by concise architecture, game-making and
  decision docs proportionate to the codebase.

## Consequences

- No API compatibility with the tracer engine; nothing outside this repository consumed it.
- Old evidence/decision history stays reachable through the tag.
- The engine grows only with features at least two games need, or that are clearly genre-neutral.
