# ADV-ART-1 — real adventure art

Current candidate: local `feat/adventure-art`, baseline `0a6e95f`. Technical verification
in progress; no subjective acceptance, push or main integration. [Contract](../../specs/adventure-art.md).

## Resource decision

| Candidate | Verified publication | Consequence / selection |
| --- | --- | --- |
| ArMM1998 | [OpenGameArt author page](https://opengameart.org/content/zelda-like-tilesets-and-sprites?page=1), CC0; actual gfx_3.zip inspected | Selected original character272x256, Overworld640x576, Inner640x400. Directional16x32 cells and16px tiles exercise existing atlas API without a new importer. No embedded license/metadata: publication declaration and content coordinates are explicit. |
| Legend of Faune | [Ansimuz](https://ansimuz.itch.io/legend-of-faune), CC0; independent assessment found named down/side/up walk sequences | Viable alternative, not imported. Side direction does not itself prove both horizontal directions; existing public API cannot mirror. No reason to add flipping or a second character pack for current scope. |
| Scribble Dungeons | [Kenney](https://kenney-assets.itch.io/scribble-dungeons), CC0 | Viable dungeon fallback, not imported. Publication does not establish four-direction walking character; mixing another environment is unnecessary. |

Independent research preferred Legend of Faune's explicit frame names; root selects the
owner's first option because its explicit four-direction atlas is sufficient and validates
source-region reuse. No sword/combat/cave/NPC/object-system scope is inferred from available art.
Existing Kenney lever/key/gem remain independently licensed, unchanged and easy to recognize.

The ArMM1998 publication includes an unverified tree-similarity concern and author disagreement.
No disputed tree region is rendered. Complete original sheets retain unused pixels, so this
is not a claim that third-party-rights questions disappear. See [source notice](../../../../examples/Adventure/Assets/ArMM1998/NOTICE.md)
and [hash manifest](../../../../examples/Adventure/Assets/ArMM1998/provenance.json).
[CC0](https://creativecommons.org/publicdomain/zero/1.0/) does not provide third-party clearance or warranty.
Reassess before wider use/external distribution; no external publication is authorized now.

## Reproduction and boundaries

```powershell
pwsh -NoProfile -File checks/VerifyAdventureAssets.ps1
dotnet test checks/Adventure.Contracts/Adventure.Contracts.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false
pwsh -NoProfile -File checks/AdventureNativeSmoke.ps1
dotnet run --project examples/Adventure/Adventure.csproj -c Release
```

Managed new expectations were observed RED3/3 before presentation changes, then GREEN12/12
including all9 unchanged model tests. A first native wrapper run correctly rejected its
outdated six-image marker; updated to seven actual original sheets/icons, retaining strict
count/lifecycle checks. A transparent candidate grass cell was rejected by the opaque native
reference check; selected a fully opaque grass cell and reran successfully. No test reduction.
Native new art, gateclosed/open original RGB, HUD,460step public course/key/altar/reset/identical
replay/seven cached image leases and seven productionCLI exits0/1/2 passed before independent review.

No engine/model/backend/dependency or old-game changes. No new public engine contract;
directional facing is local presentation inferred from resolved motion, not gameplay/facing API.
Point-sampled2x is a choice of this content, not a Core style rule. General atlas loading,
animation/reset, optional labels, scaling and ownership are reused. Native probe may reference
Raylib solely to read framebuffer evidence; production game contains no native/vendor access.

## Owner-only evaluation

After technical closure, evaluate only new art readability, directional movement appearance
and whether walls/gate/objects/passages are clear. Existing controls, puzzle and replay remain
unchanged. No requirement to rerun tests or accept fullH2/combat/future art. Main remains unchanged.
