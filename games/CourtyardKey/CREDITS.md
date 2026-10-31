# Courtyard Key asset credits

Courtyard Key is a hybrid game. Its world, tiles, props, hero, villagers, the slime, bat and knight,
the Moss Warden boss, items, music and sound effects are generated in code at startup. The
seed-spitting heart-trap under `Content/monsters` comes from the pack below.

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
