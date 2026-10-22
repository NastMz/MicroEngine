# ADR-0014 — Explicit game session flow over semantic input
Status: accepted2026-10-21 by root under technical delegation after independent session_flow_map design PASS and targeted ordering/surface corrections.
FR001/002,NFR001/002/004/005; no vision/platform/backend support change.

## Context
Meadow Run now needs start/pause/resume/restart, while current Desktop maps five fixed
semantic actions. Escape is nativewindow exit; up/down already express movement elsewhere.
Game must not call Raylib/Win32 or suspendenginepolling to implement pause.

## Decision proposal
Append `pause`=P and `restart`=R held/pressed actions to the existing Desktop semanticprofile;
existingfiveorder/aliases/coalescing unchanged, newactions last. This adapter only reports
input; it does not execute pause/restart, replace Loop lifecycle, or impose any game system.
Otherconsumers ignore unrelatedactions as they already do. Alternateproviders can emitsame
strings viaexistingpublic InputSnapshot. No nativekeytype or newAPI leaks into Core/game.
Meadow owns sessionphase/selection/reset and suppresses its simulation/animation whenpaused.
Defaultwindow/focus/Escape/loop/report/assetownership contracts remain unchanged.

## Alternatives and consequences
- Reuse Up/Down forpause: no adapterchange, but contradicts established directionalmeaning
  and makes menuinput accidental; rejected for this actualgameflow.
- Genericbinding/remapping API now: morefutureflexibility, but scope includes keycatalog,
  validation/serialization/remapping without a currentconfigrequirement; deferred until concrete.
- Opt-in sessioncontrols boolean: avoids emitting unusedstrings but introducesfeature-specific
  windowconfiguration instead of cohesiveinputmapping; not justified for two additiveactions.
- Nativepolling in game: rejects replaceableinfrastructure/publicboundary principles.

Risk: additive Desktopobservations onP/R differ from previouslyempty snapshots. Audit existing
consumers for unknownaction handling and retain their tests/native checks. No automatic engine
state changes fromthe actions. Existing backend-specificfixedprofile remains a documentedlimit,
not a claim of customizableinput. Significantfuturebindingneed must replace this decision through
review rather than append arbitrary gameactions indefinitely.
