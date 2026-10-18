# Tileset semantics, visual depth and engine boundaries

Assessment2026-10-18, source baseline2a98562. Owner requested learning and recording,
not another Courtyard Key art pass. **Main cause: content interpretation/composition;
no rendering API expansion is justified by the current evidence.** No executable,
asset, scene, collision or acceptance criterion changes accompany this assessment.

## What the current implementation actually does

The game loads three complete ArMM1998 sheets and four Kenney icons, not a separate
image per terrain tile. `ImageSet` reuses seven context-owned leases across both rooms
and replay. Each tile draw selects a source rectangle from an existing sheet.
Independent ADV-ART-2 proof covers this cache, the257 managed regressions, native
course/reset/replay and source pixels; those results are retained, not rerun for docs.

| Observation | Classification / evidence |
| --- | --- |
| Large fields repeat one cell; terrain changes have hard rectangular edges | Content limitation: `AdventurePresentation.cs:80-97` deliberately repeats fixed cells. It does not select compatible edge/corner/transition roles. |
| Sheet contains terrain contours, corners, structural pieces and whole props | Interpretation gap: original Overworld/Inner sheets contain composable groups, but the archive includes no semantic atlas metadata. A16px grid is not proof that each16px cell is a standalone repeatable texture. |
| Partial final tiles crop the top-left source region | Content helper limitation: `Tile:119-127` maintains valid integer crops at2x; it does not choose an appropriate visual boundary piece. Valid cropping is not seamless composition. |
| Complete rugs and column regions already draw correctly | Existing capability: `Region:117-118`, rugs98-99 and column112 demonstrate multi-cell regions rather than compulsory cell-sized images. |
| Player always draws above scenery | Demo policy: draw sequence43-60 places architecture before player. The canvas does not require that order. No dynamic behind/in-front experience was tested. |
| Solids are one planar AABB set per current world | Actual current spatial contract: `StaticWorld` has no height, collision-plane or overpass relation. Visual elevation must not be inferred from that contract. |

Source paths: [presentation](../../../../examples/Adventure/AdventurePresentation.cs),
[model](../../../../examples/Adventure.Model/AdventureModel.cs),
[public drawing contracts](../../../../engine/Loop/Contracts.cs),
[spatial contract](../../../../engine/TwoD/StaticWorld.cs).
See the [original source notice](../../../../examples/Adventure/Assets/ArMM1998/NOTICE.md)
and [current screenshots](composition.md). No disputed tree regions are needed for these findings.

## Better composition already possible, without engine changes

- Identify terrain interior, straight boundary, external/internal corner and transition pieces
  from the actual art; manually arrange compatible neighbors instead of repeating an edge cell.
  Do not assume a universal3x3 layout, tile size or autotile bitmask from appearance alone.
- Draw intact multi-cell structures, or deliberate base/upper/roof components with consistent
  palette, scale and placement. Do not arbitrarily crop furniture into generic wall texture.
- Place transparent decoration and foreground regions after ground and, where appropriate,
  after the character. Split an object into base and upper art when that is what occlusion needs.
- Maintain consumer-owned anchors/feet, image offsets, footprint and ordering rules separately.
  Destinations already allow offsets; the full image rectangle need not equal collision bounds.
- For a small future demonstrator, explicitly order layers and sort comparable actors by
  ground-contact Y with deterministic ties. This is implementable by issuing existing draws in
  that order; it is not a verified general depth system or a universal solution for tall/concave art.

These are available techniques, **not a newly scheduled art correction or authorization to
invent geometry**. Courtyard Key and its current functional route remain unchanged. A test
counting distinct regions cannot establish edge continuity, semantic material correctness,
intentional perspective or visual quality. Prior technical PASS must not be read as that claim.

## Depth and logical elevation are not 3D commitments

2D can depict height through upper/base art, shadows, occlusion, render offsets and logical
surface state. Rendering order, visible height, collision footprint and traversable level are
different concepts. A roof may cover a player without blocking its feet; a bridge may require
different collision surfaces despite overlapping screen coordinates.

Current public region drawing and alpha composition can express manual painter ordering;
there is no explicit layer stack, depth key, pivot metadata, occlusion policy or elevation-aware
spatial query. Missing reusable policies are not proof that the canvas cannot draw the scene.
Separate existing2D worlds could be selected by consumer state, but that is an unimplemented
approach, not a promise of bridge/ramp correctness. Current StaticWorld rejects mutually
overlapping solids in one set and does not filter by height. Naive Y sorting alone cannot model
every roof, multi-level crossing or cyclic overlap; art segmentation and explicit policy may be needed.

## Deferred general needs, not implementation tasks

| Candidate / capability | Evidence needed before promotion | Minimal future validation |
| --- | --- | --- |
| Semantic atlas/content metadata — CAP-006/010 | Repeated authoring mistakes or another consumer needing region roles, margins, spacing, anchors, multi-cell extents and visual-vs-collision shapes | Import a tiny documented fixture; reject invalid references; preserve source pixels and roundtrip meaning. No format/importer chosen now. |
| Authored tile layers / optional terrain rules — CAP-004/010 | A concrete map that makes hand placement materially unreliable; explicit edge/corner/transition sets | Known adjacent terrain, inner/outer corners and multi-cell object remain correct at seams and viewport edges. Autotiling remains optional, not a universal Core rule. |
| Stable rendering order / visual depth — CAP-004/008 | A real actor passing both behind and in front of a tall prop | Pixel evidence for both orders, stable equal-depth ties, distinct feet/pivot/bounds and foreground overlays; no native/provider-specific types. |
| Logical surfaces / collision filtering — CAP-007/009/010 | A specified bridge/ramp/two-level crossing, not decorative height alone | Same screen projection on different surfaces does not collide incorrectly; same-level contacts/transitions remain correct and reject invalid state. No3D or mandatory physics system. |

No candidate fixes an observed engine rendering failure. None is an approved contract,
selected dependency, ready backlog implementation or a demand to redesign Core. Flip/rotation,
masking, batching/chunking and editors likewise require their own demonstrated use or measured
budget; do not append them simply because a tilemap could eventually use them.

## Verification and continuation

Read-only CodeGraph source inspection, original-sheet/capture comparison and independent
`platformer_horizon_map` assessment support the classification. The parent spot check confirms
the public destination/source contract and one-plane StaticWorld limitation. Existing native
proof establishes pixels/crop bounds/scale/lifetime, not terrain-edge compatibility or depth.
Documentation checks: local links, Git diff and protected executable/asset parity. No meaningful
runnable RED exists for this passive assessment; no new automated rendering/depth PASS claimed.

Do not keep improving this demo to exercise every candidate. Owner functional feedback stays
recorded; no new visual/fullH2 acceptance is inferred. Backlog has no other ready implementation
after this findings task. H3 remains the next roadmap scenario to refine from its existing
menu/turn/checkpoint tracers and a concrete consumer objective; these findings are inputs, not
mandatory tilemap/depth prerequisites. No artificial requirement to obtain another visual
approval merely to perform independent planning. Do not invent combat, maps or infrastructure
just to create a task. Autonomous execution resumes when a bounded roadmap packet is ready.
