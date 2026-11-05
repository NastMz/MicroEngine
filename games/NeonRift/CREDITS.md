# Neon Rift asset credits

Every file under `Content/` comes from the packs below. All are released under
[CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) (public domain dedication); credit is not
required and is given here gladly. Licences were checked on each OpenGameArt page and in the licence files
bundled with the packs (2026-10-30).

Files were only repackaged: animation frames were joined into horizontal strips (`hero/run.png` holds the
eight `run-N.png` frames side by side, and so on), and two music tracks were downmixed to mono 22.05 kHz to
keep the repository small. Pixels were not edited; the aiming arm poses are derived at load time in code.

## Art — Luis Zuno ([@ansimuz](https://ansimuz.com))

| Content | Pack | Source |
| --- | --- | --- |
| `hero/*`, `enemies/drone.png`, `enemies/turret.png`, `fx/shot*.png`, `fx/explosion-big.png`, `stages/city/*` | Warped City | https://opengameart.org/content/warped-city |
| `enemies/trooper-*.png`, `enemies/egg-*.png`, `fx/enemy-bullet.png`, `fx/explosion.png`, `stages/lab/tileset.png` | Warped City 2 | https://opengameart.org/content/warped-city-2 |
| `stages/grotto/*` | Warped Super Grotto Escape | https://opengameart.org/content/warped-super-grotto-escape-pack |
| `stages/lab/back.png`, `middle.png`, `front.png`, `tank-*.png`, `support.png` | Warped Sci-fi Lab | https://opengameart.org/content/warped-sci-fi-lab |
| `bosses/*` | Warped Vehicles | https://opengameart.org/content/warped-vehicles |
| `fx/bolt.png`, `charged.png`, `crossed.png`, `waveform.png`, `hit1-5.png` | Warped Shooting FX | https://opengameart.org/content/warped-shooting-fx |
| `enemies/skiff.png` | Warped Space Shooter | https://opengameart.org/content/warped-space-shooter |

## Music

| File | Track | Author | Source |
| --- | --- | --- | --- |
| `music/city.ogg` | Cyber Runner | Ansimuz | https://opengameart.org/content/cyber-runner-music |
| `music/grotto.ogg` | Up and Right | Ansimuz | https://opengameart.org/content/up-and-right |
| `music/lab.ogg` | Chiptune Run Jump Duck | Ansimuz | https://opengameart.org/content/chiptune-run-jump-duck |
| `music/boss.ogg` | Boss Fight 2 | Ansimuz | https://opengameart.org/content/boss-fight-2 |
| `music/title.wav`, `music/ending.wav` | Title Screen, Ending (Retro Game Music Pack) | Juhani Junkala | https://opengameart.org/content/5-chiptunes-action |

## Sound effects — Juhani Junkala

`sfx/*.wav` are single files from "The Essential Retro Video Game Sound Effects Collection [512 sounds]"
(https://opengameart.org/content/512-sound-effects-8-bit-style), renamed by use:

| File | Original |
| --- | --- |
| shot, rapid | Weapons/Single Shot Sounds/sfx_weapon_singleshot2, singleshot7 |
| spread | Weapons/Shotgun/sfx_weapon_shotgun1 |
| laser, enemy-shot | Weapons/Lasers/sfx_wpn_laser3, laser8 |
| cannon | Weapons/Cannon/sfx_wpn_cannon2 |
| hit, clank | General Sounds/Simple Damage Sounds/sfx_damage_hit3, Impacts/sfx_sounds_impact3 |
| explode, explode-medium, explode-big | Explosions/Short/sfx_exp_short_hard2, Medium Length/sfx_exp_medium3, Long/sfx_exp_long2 |
| player-death, robot-death | Death Screams/Human/sfx_deathscream_human3, Robot/sfx_deathscream_robot1 |
| jump, land | Movement/Jumping and Landing/sfx_movement_jump10, jump10_landing |
| powerup, one-up | General Sounds/Positive Sounds/sfx_sounds_powerup2, powerup6 |
| fanfare, victory | General Sounds/Fanfares/sfx_sounds_fanfare1, fanfare3 |
| menu-move, menu-select | General Sounds/Menu Sounds/sfx_menu_move1, sfx_menu_select1 |
| pause-in, pause-out | General Sounds/Pause Sounds/sfx_sounds_pause1_in, pause1_out |
| alarm | General Sounds/Alarms/Alarms/sfx_alarm_loop2 |

## Not used

The music bundled inside Warped City (by Pascal Belisle) carries an attribution-only licence and is not
included. The hero's climbing strip (Warped City) and the pulse shot (Warped Shooting FX) have no use in the game
and are not shipped.
