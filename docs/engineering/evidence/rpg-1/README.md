# RPG-1 — Trail Duel

First bounded H3 integration: public exploration/portals, trainer interaction, menu,
game-owned deterministic combat, result and return. [Contract](../../specs/rpg-encounter.md),
[ADR0013](../../decisions/0013-consumer-rpg-encounter.md), backlogME064/065/066.
Technical candidate awaiting independent final proof; product/fullH3 acceptance unobserved.

## Run and owner-only experience

```powershell
dotnet run --project C:\Proyectos\MicroEngine\examples\Rpg\Rpg.csproj -c Release
```

WASD/arrows move. The cyan right passage leads from Camp to Meadow; the left passage
returns. Both areas remain freely explorable, no forced visit sequence. In Camp move onto
the blue trainer/sentinel marker and press a fresh Space. Up/down select Attack, Guard or
Retreat; fresh Space confirms. One confirmation resolves the player action and, unless the
opponent was defeated or you retreated, the response. Turn/HP/menu state are visible.

- Three Attacks win: player2HP, opponent0HP, turn3.
- Two Attacks then two Guards lose: player0HP, opponent2HP, turn4.
- Retreat ends without damage or advancing the turn.
- At result, fresh Space returns at the exact exploration position. Defeat/retreat permit
  a fresh challenge with fullHP; victory clears that encounter, but exploration continues.
- Escape closes. Held Space is not repeated confirmation. No saving, capture or RPG content library.

Only assess new experiential evidence: discoverability of challenge/commands, HP/turn/result
clarity, movement freeze during combat and return-to-exploration feel. Technical checks below
are agent-owned, not instructions to repeat tests. Art remains deliberately bounded; no new
map composition/semantic/assisted-visual tools or depth systems are part of this slice.

## Technical reproduction and current evidence

```powershell
dotnet test checks/Rpg.Contracts/Rpg.Contracts.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false
pwsh -NoProfile -File checks/RpgNativeSmoke.ps1
pwsh -NoProfile -File checks/VerifyAdventureAssets.ps1
```

Author modelRED13/13 against runnable stubs ->GREEN13/13 plus held/case regression14/14;
presentationRED6/6 -> combined20/20GREEN. Native actualPNG source2x, numericHP/bar pixels,
selected-row highlight, visible result/return glyphs, public portals and all3combat outcomes,
frozen/resumed position, retry/fullHP, one-shot victory, shared4leases and clean native closePASS.
ProductionCLI7cases with exact0/1/2 exitsPASS, invalid flags before native acquisition;
fresh build0warnings/errors. Independent baseline/horizon verification still pending.

## Reuse and boundaries

Engine unchanged. Public Explorer assembly supplies existing movement/clamps/portals, while
RpgSession strips its interaction edges and owns only encounter/menu/HP/progression rules.
TurnSession/old checkpoint formats remain untouched. New snapshot is a game value, not a
public engine combat/storage contract. New public model, native probe and renderer tests
use the same public interface; no production native/vendor/reflection/friend access.

Presentation reuses public FrameLoop/input/canvases, ImageSet, FrameAnimation and Desktop PNG
decoder. Original ArMM1998 sheets and Kenney gem are build-linked from existing Adventure
content (no Adventure binary reference or copied source blobs). Existing manifests/notices
and hashes remain authoritative. This explicit consumer content-path dependency is not a
general asset package/loader or a promise that every backend provides image regions/labels.
Windows validation only; SDK/package/target/budget policy unchanged. One window/cache across
exploration, battle, result and retries. Combat has no random/types/team/capture/inventory/
abilities/status effects/quests/persistence/audio/menu framework or new spatial/layer API.

## What the slice validates

Existing navigation can coexist with a game-owned modal flow without rebuilding movement,
runtime, input retention or resource lifetime. Fresh-edge consumption and terminal ordering
prevent menu-opening attacks, duplicate turns, post-win retaliation and result-return movement
leaks. Pixel/route tests are objective evidence, not combat balance or product acceptance.
Future battle/content/metadata needs must arise from demonstrated requirements, not Pokemon
feature imitation. F014/F015 and visual-tool candidates remain deferred.
