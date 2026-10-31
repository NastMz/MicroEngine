# Trail Duel asset credits

Trail Duel is a hybrid game. Its world (tiles, buildings, ground detail), battle stages, interface,
music and sound effects are generated in code at startup. The creatures and trainers come from the
Openmon collection, and the battle effects and jingles from Ninja Adventure; both are listed below.

## Openmon collection — Screen Smith and commissioned artists

All files below come from Screen Smith's Openmon Asset Collection on itch.io. Each page states the
sprites are released under a [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) licence (all
but set 2 add "Use them however you want, no attribution needed") and that they were made by human
artists who transferred
full rights to Screen Smith (sets 1 and 2 include sprites bought from a fakemon artist, set 2 credits
https://x.com/onigin_pixelart). Only sets marked as made by human artists were used; the collection's
AI-generated sets and the sprites derived from Tuxemon's CC-BY-SA art were deliberately left out.
Checked 2026-10-30.

Creature files are single 64x64 cells cut from the sheets, otherwise unedited; the player's creature is
mirrored at load to face right, and two are recoloured at load so evolution lines read as one family:
Bouldox takes Pebblit's browns and Fizzbit takes Voltiger's charcoal and yellow (`Visuals/CreatureArt.cs`). Trainer sheets are unedited; the fisher is a hue-turned copy of one
character, made at load (`Visuals/CharacterArt.cs`).

| File | Source page | Cell (column, row of 64x64) |
| --- | --- | --- |
| `creatures/emberpup.png` | https://screensmith.itch.io/shibu-front-sprites-openmon-monster (`ShibuLine.png`) | 2, 3 |
| `creatures/blazehound.png` | same | 0, 2 |
| `creatures/puddlepip.png` | https://screensmith.itch.io/openmon-monster-sprites-set-4 (`Openmon Set 4.png`) | 1, 0 |
| `creatures/tidalotl.png` (Tidefang) | same | 0, 0 |
| `creatures/voltusk.png` (Voltiger) | same | 2, 1 |
| `creatures/sprigbun.png` (Sprigfawn) | https://screensmith.itch.io/openmon-monster-sprites-set-1 (`OpenmonSprites1.png`) | 2, 2 |
| `creatures/grovehare.png` (Grovestag) | same | 3, 2 |
| `creatures/puffmop.png` | same | 3, 1 |
| `creatures/pebblit.png` | same | 1, 0 |
| `creatures/fizzbit.png` | same | 1, 1 |
| `creatures/breezel.png` | https://screensmith.itch.io/openmon-monster-sprites-set-2-fakemon (`Openmon Set 2.png`) | 0, 0 |
| `creatures/gustowl.png` (Gustwing) | same | 1, 0 |
| `creatures/bouldox.png` | https://screensmith.itch.io/openmon-monster-sprites-set-3 (`OpenmonSet3.png`) | 3, 0 |
| `creatures/mossnail.png` (Mossback) | same | 2, 2 |
| `characters/trainers1.png` | https://screensmith.itch.io/openmon-npc-sprites-set-1 (`OpenmonSpriteSet1.png`) | whole sheet |
| `characters/trainers2.png` | same (`OpenmonSpriteSet2.png`) | whole sheet |

## Ninja Adventure Asset Pack — Pixel-boy and AAA

- Source: https://pixel-boy.itch.io/ninja-adventure-asset-pack
- Licence: [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) (public domain dedication),
  stated on the pack page and in the `LICENSE.txt` and `README.md` bundled in the download. Credit is not
  required and is given here gladly. Checked 2026-10-30.

Files were copied and renamed only; pixels and audio were not edited. Effect frames are sliced at load
time (`Visuals/FxArt.cs`); jingles are registered in `Sound/MusicBank.cs`.

| File | Original in the pack |
| --- | --- |
| `fx/flame.png` | `FX/Elemental/Flam/SpriteSheet.png` |
| `fx/explosion.png` | `FX/Elemental/Explosion/SpriteSheet.png` |
| `fx/splash.png` | `FX/Elemental/Water/SpriteSheet.png` |
| `fx/geyser.png` | `FX/Elemental/WaterPillar/SpriteSheet.png` |
| `fx/leaves.png` | `FX/Elemental/Plant/SpriteSheet.png` |
| `fx/thunder.png` | `FX/Elemental/Thunder/SpriteSheet.png` |
| `fx/rocks.png` | `FX/Elemental/Rock/SpriteSheet.png` |
| `fx/rock-spike.png` | `FX/Elemental/RockSpike/SpriteSheet.png` |
| `fx/gale.png` | `FX/Slash/SpriteSheetCircular.png` |
| `fx/claw.png` | `FX/Attack/Claw/SpriteSheet.png` |
| `fx/cut.png` | `FX/Attack/CutX/SpriteSheet.png` |
| `fx/boost.png` | `FX/Magic/Boost/SpriteSheet.png` |
| `fx/sparkle.png` | `FX/Magic/Spark/SpriteSheet.png` |
| `fx/smoke.png` | `FX/Smoke/Smoke/SpriteSheet.png` |
| `fx/spark-ring.png` | `FX/Magic/Circle/SpriteSheetSpark.png` |
| `fx/aura.png` | `FX/Magic/Aura/SpriteSheet.png` |
| `jingles/levelup.wav` | `Audio/Jingles/LevelUp2.wav` |
| `jingles/catch.wav` | `Audio/Jingles/Success3.wav` |
| `jingles/heal.wav` | `Audio/Jingles/Success4.wav` |
| `jingles/item.wav` | `Audio/Jingles/Secret1.wav` |
