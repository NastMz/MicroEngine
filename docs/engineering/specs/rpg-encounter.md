# RPG-1 — Trail Duel, first bounded H3 scenario

Status: approved2026-10-18 by root after independent checkpoint_design_review PASS, before source; ME064, C-003/FR001/002/NFR001/002/004/005/006.
Two exploration rooms, one trainer encounter, three commands, three outcomes and return.
Not a Pokemon clone, general battle/UI system or full H3 acceptance.

## Reuse and ownership

New examples/Rpg.Model references public RoomExplorer.Model and TwoD; examples/Rpg references
its own model and Desktop. Explorer owns existing normalized180units/s movement/clamps/portals.
RpgSession owns encounter/input/menu/results; never modify Explorer, TurnSession or engine.
TurnSession's four-phase completion is not a combat contract and will not be retrofitted.
No RNG/types/capture/party/inventory/quests/save/audio/editor/tilemap/depth/new backend.
Existing CP/CF/CL checkpoints remain unchanged and are not claimed to save RPG combat.

## Exploration and transitions

RpgSession exposes read-only Room/X/Y/Body, Mode{Exploration,Battle,Result}, SelectedAction
{Attack,Guard,Retreat}, BattleSnapshot(PlayerHp,OpponentHp,Turn,Outcome), EncounterCleared,
EncounterNumber. Body24x32; Explorer starts Courtyard(40,164). Courtyard is labeled Camp,
Hall is Meadow. Existing portal opening/full-body bounds/spawns are reused exactly; a room
change consumes the update. Input is validated null first, positive delta<=100ms, before mutation.

Strip exact lowercase jump edges before forwarding exploration input to Explorer so its
marker-toggle policy cannot leak into RPG progression. After movement, a fresh exact jump
while strictly overlapping public Explorer marker bounds in Camp opens battle unless cleared.
Opening consumes the update, resets HP/turn/action, increments EncounterNumber; it never attacks.
Camp marker represents a nonblocking trainer interaction zone, not a new obstacle. No forced
visit order or encounter-on-random-steps; both rooms remain explorable before and after combat.

## Deterministic game-owned combat

Start player12HP/opponent10HP/turn0/selectedAttack/OutcomeNone. During Battle exploration
position/room freeze. Process ordered exact pressed up/down edges with wrap through3actions;
Up selects the preceding action, Down the following, wrapping Attack/Guard/Retreat; held inputs, wrong-case/unknown edges do nothing. First exact jump resolves one selected
command and consumes remaining edges/movement, even if repeated in the same snapshot.

- Attack: increment turn, opponent loses4HP clamped0. If zero, Victory immediately; no retaliation.
  Otherwise player loses5HP clamped0; zero produces Defeat.
- Guard: increment turn, no opponent damage; player loses1HP clamped0; zero produces Defeat.
- Retreat: no HP/turn mutation, outcomeRetreated, no opponent retaliation.

Terminal command enters Result; snapshot remains immutable to clients, victory alone sets
EncounterCleared. No further navigation/HP/turn changes in Result. Fresh jump acknowledges
result and returns Exploration at exact saved room/position, consuming that entire update.
No same-edge restart or movement leak. Defeat/retreat allow a later fresh interaction retry
with full initialHP; victory permanently disables this encounter for that session while
exploration/portals continue. Separate instances independent. No public state injection/reset.

Known routes: Attack3 => Victory, player2/opponent0/turn3; Attack2 then Guard2 => Defeat,
player0/opponent2/turn4; Retreat immediately =>12/10/0. Battle selection persists per encounter.
Menus and instant player+opponent resolution demonstrate turn ownership, not combat balance.

## Presentation, process and art

Desktop672x480, world origin(16,96), existing640x360 world; top64HUD and room border separate
from world. Character32x64 at(12+X,64+Y), keeping oversized head inside window at all legal
positions. Existing character sheet four16x32frames/120ms and directional rows; actual resolved
motion only, reset on mode/room/encounter changes, no battle walking clock. Trainer/marker,
Camp/Meadow palette and simple intact terrain regions, not another art composition experiment.

Battle/result screen: player portrait and existing gem_blue as generic Training Sentinel,
visible numericHP plus bars, turn number,3text commands with selected row, explicit outcome
and Space:return prompt. Uses public image regions, optional labels and Fill only. This
particular game requires IImageRegionCanvas and ILabelCanvas; no universal provider mandate.
One ImageSet/window; four used images character/Overworld/Inner/gem_blue across all modes,
including retries. Build links unchanged original Adventure/Assets content with existing
CC0 manifests/notices, no binary Adventure dependency/new art/download or proprietary Pokemon IP.
Content link is a deliberate consumer file dependency, not an engine asset manager.

Production CLI: optional --smoke N1..600 and --fail-update only; invalid/duplicate flags exit2
and usage before asset/native setup, clean smoke0, intentional update failure1, clean native close.
Scripted input belongs to separate native probe, not production flags. SDK/net10/dependencies unchanged.

## Acceptance and proof packet

ME064: independent design review and delegated ADR0013/RPG1 approval before source.
ME065: public runnable tests RED then GREEN for exploration/portals, final-position strict
interaction, consumed opening, navigation wrap/order/case/duplicates/held/first-confirmation,
Attack/Guard/Retreat timing/HP/terminal retaliation suppression, all3outcomes, result freeze/
consumed return, retry versus one-shot victory, invalid atomicity and instance isolation.
Keep all257baseline tests. New renderer tests check purity, sourcebounds, modes/HP/menu/
result labels and imagecache/one-time disposal/directional clock; no old tests weakened.

ME066: separate public-input driver visits Meadow/returns Camp, opens encounter, proves each
outcome on independent sessions, exact position resume, retry/fullHP and victory non-reentry;
native framebuffer verifies original PNG/HP/selected UI/outcome text, resource reuse, cleanclose
and7productionCLIcases. Fresh serial Release existing6suites+Rpg, all existing15nativeharnesses
retained unchanged; run newRpg and representative Adventure/Turn regression harnesses. Independent
fresh scratch/public boundary/sourceasset hashes/protected engine/oldconsumer/config parity.
Parent spotcheck, one bounded correction/recheck if blockers, no invented proof.
Objective technical acceptance delegated; stop at new playable movement/interaction/menu/turn/
result clarity assessment. Owner tileset evaluation closed, candidate metadata/layers/depth/
surfaces/tooling remain deferred. No extra H3 scope invented after this finite slice.

RPG1technicalacceptance2026-10-18: independentS1-S5PASS on eb595a8,277managed/3freshnative/assetandprotectedsourceparityPASS, no blockers. Rootobjectiveacceptance only; [finalevidence](../evidence/rpg-1/README.md). No new product/fullH3acceptance inferred.
