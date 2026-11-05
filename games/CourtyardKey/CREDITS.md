# Courtyard Key asset credits

Courtyard Key is a hybrid game. Its ground, dungeon, props, hero, villagers, the slime, bat and knight, the Moss
Warden boss, items, music and sound effects are generated in code at startup. The village houses and the trees
under `Content/vale` and the seed-spitting heart-trap under `Content/monsters` come from the packs below.

## Zelda-like tilesets and sprites — ArMM1998

- Source: https://opengameart.org/content/zelda-like-tilesets-and-sprites
- Licence: [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) (public domain dedication), stated on the
  pack page. Credit is not required and is given here gladly. Checked 2026-11-05.

The file is an unedited copy. At load time `Visuals/PackArt.cs` cuts the house and the tree out of the sheet along
their own outlines and repaints them with the game's colour ramps, ink outline and shadow colour, so they share the
world's palette; it also shortens the house roof and hangs the shop's sign.

| File | Original in the pack | Used as |
| --- | --- | --- |
| `vale/overworld.png` | `gfx/Overworld.png` (the front of the wooden house, the round tree) | Elder Oswin's house, Marla's shop, every tree of the vale |

## Ninja Adventure Asset Pack — Pixel-boy and AAA

- Source: https://pixel-boy.itch.io/ninja-adventure-asset-pack
- Licence: [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) (public domain dedication),
  stated on the pack page and in the `LICENSE.txt` and `README.md` bundled in the download. Credit is not
  required and is given here gladly. Checked 2026-10-30.

The file is an unedited copy. At load time `Visuals/MonsterArt.cs` repaints the sheet with the game's
own colour ramps and ink outline, so it shares the world's palette.

| File | Original in the pack | Used as |
| --- | --- | --- |
| `monsters/spitter.png` | `Actor/Monster/HeartRed/SpriteSheet.png` | Seed-spitting heart-trap |
