# ADV-ART-2 — bounded authored room composition

Presentation-only continuation of [ADV-ART-1](adventure-art.md); owner positively evaluated
the functional cycle, but requested a finite visual composition pass. Root authorizes this
local content design under existing public contracts; no new engine architecture decision.

## Composition and limits

Keep the exact640x360 rooms, solids, sensors, movement, interactions and replay of ADV-1.
Reuse only the original licensed ArMM1998 sheets and Kenney interaction icons already present.
No new import, package, Core change, map format, editor, tilemap API or game mechanics.
No tree regions; original sheet provenance caveat remains.

Courtyard: varied grass cells and flat garden patches; an eastbound dirt route connects the
spawn/lever/gate/passage, with a short north/south branch around the lever. Stone inlays mark
the passage and lever; clipped vegetation dresses existing boundary solids only. Shrine:
stone floor field, contrasting floor bands and two complete flat rugs identify key and altar;
stone column art aligns with existing pillar, wall friezes occupy existing solid strips.
Volumetric structures must not imply new collision in open walkable space. Ground decals
are explicitly nonblocking. Gameplay icons/player/HUD draw after all scenery and remain visible.

All source rectangles remain integer positive pixels at2x. Fixed consumer methods own
palette/coordinates/layers; no generalized composition framework. One existing image cache,
seven totalleases, samewindow. Existing directional animation and reset semantics unchanged.

## Proof and stopping

New composition regression observed RED before source; retain previous source bounds/scale,
render purity/cache/disposal, all9 model tests and native460step route/replay/CLI exits.
Check distinct full source cells (clipped boundary sizes alone do not prove terrain variety),
pillar alignment, player-last layering, actual native path/floor RGB and source snapshots.
Independent verification checks no protected code/assetbytes changed and no mechanics/API
expansion. Assess screenshots for accidental crops, invisible interactions or misleading props;
technical checks do not claim aesthetic acceptance. Record pipeline/content lessons and known
limits, then stop at the owner's subjective new-composition assessment, not another polish horizon.
