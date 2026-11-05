// What each demo exercises in the engine, as family markers on the demos index and the demo pages. Each entry is a
// claim about the game's code, so each carries the evidence that makes it true: patterns that must all appear in the
// game's C# sources (games/<Id>/**/*.cs). tests/site.test.mjs fails when a pattern no longer matches, so a marker
// cannot outlive the code it describes. Only engine features the game really calls are listed; game logic the game
// writes itself (its own physics, its own menus) is not.

/** @typedef {'kernel' | 'graphics' | 'physics' | 'audio' | 'input'} Family */
/** @typedef {{ id: string, family: Family, name: string, href: string, evidence: RegExp | RegExp[] }} Feature */

/** @type {Record<string, Feature>} */
export const features = {
  saves: { id: 'saves', family: 'kernel', name: 'Saves and achievements', href: '/docs/manual/persistence/', evidence: [/\bSaveStore\b/, /\bAchievement\b/] },
  tiles: { id: 'tiles', family: 'graphics', name: 'Tile maps', href: '/docs/manual/world/', evidence: /\bTileLayer\b/ },
  particles: { id: 'particles', family: 'graphics', name: 'Particles', href: '/docs/manual/rendering/#particles', evidence: /\bParticleSystem\b/ },
  collision: { id: 'collision', family: 'physics', name: 'Tile collision', href: '/docs/manual/world/#moving-through-the-grid', evidence: /\bTileMotion\b/ },
  synth: { id: 'synth', family: 'audio', name: 'Synthesized sound', href: '/docs/manual/audio/#synthesized-sound-effects', evidence: /\bSynth\b/ },
  files: { id: 'files', family: 'audio', name: 'Sound files', href: '/docs/manual/audio/#sound-and-music-files', evidence: /\.(wav|ogg)"/ },
  actions: { id: 'actions', family: 'input', name: 'Input actions', href: '/docs/manual/input/', evidence: /\bInputMap\b/ },
  touch: { id: 'touch', family: 'input', name: 'Touch controls', href: '/docs/manual/input/#touch-controls', evidence: /\bTouchLayout\b/ },
};

/** Feature ids per game, in family order (kernel, graphics, physics, audio, input). */
const byGame = {
  MeadowRun: ['saves', 'tiles', 'particles', 'collision', 'synth', 'actions', 'touch'],
  CourtyardKey: ['saves', 'tiles', 'particles', 'collision', 'synth', 'actions', 'touch'],
  TrailDuel: ['saves', 'tiles', 'particles', 'synth', 'files', 'actions', 'touch'],
  NeonRift: ['saves', 'tiles', 'collision', 'files', 'actions', 'touch'],
};

/** The features a game exercises, in family order. */
export function exercises(gameId) {
  const ids = byGame[gameId];
  if (!ids) throw new Error(`exercises: unknown game ${gameId}`);
  return ids.map(id => features[id]);
}

export const exercisedGames = Object.keys(byGame);
