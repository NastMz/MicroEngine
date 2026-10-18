# ADV-ART-1 — licensed Courtyard Key presentation

Status: technically accepted2026-10-18 by root after independent S1-S4 PASS on892d8ff; [evidence](../evidence/adv-1/art.md). Owner subsequently evaluated the functional cycle positively but requested richer composition; ADV-ART-2 addresses that observation, not implicit visual acceptance.

Presentation-only refinement of [ADV-1](adventure.md), not a new gameplay horizon.
Engine, gameplay, input, collision, rooms, interactions and completion/replay remain unchanged.

Current bounded scene composition is specified by [ADV-ART-2](adventure-composition.md); its content palette supersedes the original floor/wall composition below without changing animation/gameplay.

## Bounded composition (ADV-ART-1 baseline)

Use original ArMM1998 character, Overworld and Inner sheets through existing public
DesktopImageDecoder, ImageSet, FrameAnimation and IImageRegionCanvas. Content coordinates
are consumer-owned, not atlas metadata or a general animation/facing engine contract.
Retain licensed Kenney lever/key/gem icons to preserve interaction legibility and old assets
and provenance as historical evidence. Do not render the disputed tree regions.

Tile outdoor grass (64,96,16,16), indoor floor (176,32,16,16), stone wall
(352,16,16,16), and closed gate (96,0,16,16), all at 2x, clipping boundary tiles
with positive integer source extents. Body24x24 remains unchanged; character16x32
is 2x, bottom-aligned/centered at Body.X-4,Body.Y-40. Four 120ms frames at
x=0/16/32/48; rows down0/right32/up64/left96. Actual resolved displacement selects
facing (horizontal wins diagonals); stationary preserves facing with first frame. Area
changes/run reset restore down/clock, without deriving direction from the teleport.
Renderer Advance tracks previous body/area; Draw remains pure with respect to game.

## Acceptance and proof

- Selected original bytes and SHA256 provenance, official CC0 publication and limitations retained.
- Managed regression of all four directions, actual motion/idle/reset and render purity/cache/disposal.
- Native actual PNG decode, source bounds, 2x tiles/avatar, gate appearance changes with topology,
  readable HUD, same route/key/altar/consumed reset/identical replay, bounded shared image leases.
- Existing model and all engine/old consumers remain byte-identical. Prior tests stay covered;
  independent fresh-context verification plus parent spot check. No new package/backend/API.
- Technical closure is not subjective acceptance; ask owner only about the new appearance.
