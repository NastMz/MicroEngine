// The four demo games as the site presents them. Screenshots are read from each game's own `screenshots/` folder
// (the same captures its README uses), so the site never keeps a second copy. Text summarises the game READMEs;
// controls are the keyboard rows of those READMEs, the input verified in browsers by automated tests.
import type { ImageMetadata } from 'astro';

export interface Shot { src: ImageMetadata; alt: string }

export interface DemoGame {
  id: 'MeadowRun' | 'CourtyardKey' | 'TrailDuel' | 'NeonRift';
  slug: string;
  title: string;
  genre: string;
  tagline: string;
  summary: string[];
  content: { kind: 'Procedural' | 'Hybrid' | 'Asset files'; detail: string };
  controls: [action: string, keys: string][];
  colorToken: string;
  /** Site page listing the game's third-party assets, when it has any. */
  credits?: string;
  shots: Shot[];
}

const files = import.meta.glob<{ default: ImageMetadata }>('../../../games/*/screenshots/*.png', { eager: true });

function shots(id: string, title: string): Shot[] {
  return Object.entries(files)
    .filter(([path]) => path.includes(`/games/${id}/screenshots/`))
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([path, module]) => {
      const name = path.split('/').pop()!.replace(/\.png$/, '').replace(/^\d+[-_]/, '').replace(/[-_]/g, ' ');
      return { src: module.default, alt: `${title} screenshot: ${name}` };
    });
}

const games: Omit<DemoGame, 'shots'>[] = [
  {
    id: 'MeadowRun',
    slug: 'meadow-run',
    title: 'Meadow Run',
    genre: 'Side-scrolling platformer',
    tagline: 'Every pixel and every note is generated in code.',
    summary: [
      'Grumbold the Cinder Mole King has stolen the Sunstone. Pip, a round sprout spirit, runs from the sunny meadow through crystal caves and cloud heights to the ember castle to bring it home.',
      'Meadow Run is fully procedural: sprites, tiles, sound effects and music are all generated at startup, so the game ships no asset files.',
    ],
    content: { kind: 'Procedural', detail: 'No asset files: art, sound and music are code.' },
    controls: [
      ['Walk', 'Arrows / A D'],
      ['Jump (hold to jump higher)', 'Z, Space, J'],
      ['Run / throw ember seeds', 'X, C, K, L, Left Shift'],
      ['Drop through a plank', 'Down + Jump'],
      ['Pause', 'Esc, P'],
      ['Menus', 'Arrows, Z/Enter to confirm, X to go back'],
    ],
    colorToken: '--color-meadow',
  },
  {
    id: 'CourtyardKey',
    slug: 'courtyard-key',
    title: 'Courtyard Key',
    genre: 'Top-down action adventure',
    tagline: 'Screen-by-screen exploration, a sword and a boomerang.',
    summary: [
      'The bells beneath the old ruins rang again. Elder Oswin hands Wren his old sword: cross the vale, descend into the Sunken Courtyard and take the Courtyard Key back from the Moss Warden.',
      'A hybrid: the textured ground, the lit dungeon, the hero, villagers, most enemies, the boss and all audio are generated in code; the village houses, the trees and one monster are hand-drawn CC0 pieces recoloured in the game’s palette when it loads.',
    ],
    content: { kind: 'Hybrid', detail: 'Procedural ground, dungeon, cast and audio; CC0 houses, trees and one monster.' },
    controls: [
      ['Move', 'Arrows / WASD'],
      ['Sword, talk, read, open chests, confirm', 'Z / Space / Enter'],
      ['Throw the Wind Boomerang (once found)', 'C / X'],
      ['Pause: inventory, quest items, map', 'Esc / P'],
    ],
    colorToken: '--color-courtyard',
    credits: '/docs/credits/courtyard-key/',
  },
  {
    id: 'TrailDuel',
    slug: 'trail-duel',
    title: 'Trail Duel',
    genre: 'Creature-collecting RPG',
    tagline: 'Turn-based battles, captures, evolutions and a rival.',
    summary: [
      'Leave Sprout Town with a partner creature, walk Route 1 and Hollow Wood, beat your rival twice and earn the title of Trail Champion by defeating Master Odessa in Brookhaven’s hall.',
      'A hybrid: the village comes from a hand-drawn CC BY 4.0 tileset, colour-graded with drawn shadows when it loads; creatures, trainers and battle effects are hand-drawn CC0 sprites; tall grass, ledges, the town’s landmark buildings, battle stages, the interface and music are made in code to match.',
    ],
    content: { kind: 'Hybrid', detail: 'CC BY 4.0 village tileset, CC0 creatures and trainers, procedural stages and music.' },
    controls: [
      ['Walk, move cursors', 'Arrows / WASD'],
      ['Confirm, talk, read signs, advance text', 'Z / Space / Enter'],
      ['Back (hold while walking to run)', 'X / Backspace'],
      ['Start menu', 'Esc / P'],
      ['Show or hide the full move card in battle', 'C'],
    ],
    colorToken: '--color-trail',
    credits: '/docs/credits/trail-duel/',
  },
  {
    id: 'NeonRift',
    slug: 'neon-rift',
    title: 'Neon Rift',
    genre: 'Run & gun',
    tagline: 'Neon rooftops, a drowned grotto and a flying dreadnought.',
    summary: [
      'The Halcyon corporation is assembling the Leviathan under its tower district. Vela, a courier with a pulse pistol, crosses the neon rooftops, the drowned grotto and the assembly lab to take it down before it launches.',
      'Neon Rift is built from existing CC0 asset packs (sprite sheets, tilesets, WAV effects and OGG music) loaded through the engine’s file loaders. Its music makes it the largest download of the four.',
    ],
    content: { kind: 'Asset files', detail: 'CC0 sprite sheets, tilesets, WAV and OGG.' },
    controls: [
      ['Move', 'Arrows / WASD'],
      ['Jump (release early for a short hop)', 'Z, Space, J'],
      ['Fire (hold for auto-fire)', 'X, K'],
      ['Aim up / diagonals', 'Up, Up + direction, Down + direction while walking'],
      ['Kneel / aim down in the air', 'Down'],
      ['Drop through a ledge', 'Down + Jump'],
      ['Plant feet to aim diagonally', 'C, L, Left Shift (hold)'],
      ['Pause', 'Esc, P'],
    ],
    colorToken: '--color-neon',
    credits: '/docs/credits/neon-rift/',
  },
];

export const demoGames: DemoGame[] = games.map(game => ({ ...game, shots: shots(game.id, game.title) }));

export const gameBySlug = (slug: string) => demoGames.find(game => game.slug === slug);
