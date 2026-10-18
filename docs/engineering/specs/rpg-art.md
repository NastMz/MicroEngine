# RPG-ART-1 — selected art for exploration and battle

Status: approved2026-10-18 by root after independent checkpoint_design_review PASS, before replacement presentation. Interior-cell regression advisory adopted. Successor only to RPG1's art section,
not its functional contract. [Selection](../evidence/rpg-1/assets.md); ME067/068.

## Content, not engine systems

Selected original packed Tiny Town1.1 and Tiny Dungeon1.0 atlases, each192x176/12x11
cells16x16/no spacing, plus existing ArMM character272x256. Unpacked203x186 sheets use
1px spacing and are NOT the selected source. The common license is CC0-1.0; pack release
numbers are not license versions. Original per-file/archive hashes, license notices and
reference metadata are checked in before presentation. Three used GPU images replace four
previous art images; strict cache/lifetime tests continue to require the complete selected set.

Public ImageSet/PNG decode/source regions/FrameAnimation/Fill/Label suffice. No engine API,
model, old consumer, SDK/dependency, HP/actions/input/outcome, UI mechanics or capability change.
Own Rpg/Assets bundle removes its build-content dependency on Adventure; originals remain
unchanged in Adventure. No runtime downloads, metadata parser, autotiling, editor or depth system.

## Explicit selected roles

- Hero: retained ArMM16x32 four-direction/120ms real walk frames and exact existing destination
  offsets; preserves tested animation, not pretending static Tiny tiles are animation frames.
- Town: grass interior cell(0,0), sparse flowers(1,0), complete dirt3x3region at(0,16,48,48).
  Manually authored corner/straight-edge/interior selection expands the3x3 terrain to integral
  32px destination tiles. This private game-specific terrain patch is not a public tilemap API.
- Dungeon: static red monster originaltile0110, packed(column2,row9), source(32,144,16,16).
  Exploration at(320,250,32,32); battle at(448,160,96,96). Different scale/context, same creature
  representation initially; no assumption that future world/battler art must match resolution.
- Camp/Meadow remain existing open navigable rooms. Town ground/routes/flat flower patches only;
  houses/interiors/furniture are available in candidate packs, not new mechanics or misleading
  freestanding solid props. Do not introduce a wall where Explorer has no collider.
- Battle: compound Town dirt/grass border at(16,80,640,224), clearly different arrangement from
  exploration; original hero back portrait and selected monster. Existing numericHP/bars,
  selected action/turn/result/return UI stays general Fill+Label, above background art.
- VFX/status sprites not required by current rules. Do not invent debuffs, attack effects,
  new moves, or fake animation just because candidate archives contain them.

## Acceptance and proof

Selection documents all6candidates, actual archive/PNG/metadata/license availability and
selection tradeoffs before art construction, independent design review then delegated approval.
New public draw-capture tests assert exact packed source roles/corners/edges and aspect choices,
existing hero motion/idle/reset, all three selected images acquired once, no game-state mutation
and single releases. Same fourteen public game-model tests and six renderer cases retained;
content-dependent source names/counts change explicitly, not weakened ownership criteria.
Native original PNG/sourcepixels, visible creature versus former gem, terrain corner/interior,
UIHP/selection/result/readability, all3outcomes/retry/return/CLI7 remain verified. Resourcecount
must be exactly3 across modes/areas/retries. Independent original/Gitblobhash verification,
protected engine/model/oldconsumer/config parity, full managed regression and representative
native checks. No aesthetic or fullH3 acceptance inferred; stop at new presentation experience.
