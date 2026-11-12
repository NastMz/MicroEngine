// Which subsystem family a documentation page belongs to, and the socket card some manual pages open with. The docs
// Markdown is shared with the repository and carries no frontmatter, so the mapping lives here, keyed by page slug
// (src/lib/repo-docs.mjs). A page with no entry gets no family marker: a family colour means a kind of module, so a
// page that is not about one module (the introduction, the how-to list, UI widgets) stays neutral.
// See site/design/README.md, "Documentation".

/** Family per documentation page slug. */
export const pageFamilies = {
  // Engine manual
  'docs/manual/lifecycle': 'kernel',
  'docs/manual/scenes': 'kernel',
  'docs/manual/rendering': 'graphics',
  'docs/manual/text': 'graphics',
  'docs/manual/input': 'input',
  'docs/manual/audio': 'audio',
  // Content is a core service (Game.Assets), not a module of its own.
  'docs/manual/assets': 'kernel',
  'docs/manual/world': 'physics',
  // Where save slots live is the platform's storage.
  'docs/manual/persistence': 'platform',
  'docs/examples/testing': 'tools',
  // Getting started
  'docs/getting-started': 'tools',
  'docs/cli': 'tools',
  'docs/updating': 'tools',
  // Tutorial chapters: the subsystem each one introduces.
  'docs/tutorial/scenes': 'kernel',
  'docs/tutorial/drawing': 'graphics',
  'docs/tutorial/input': 'input',
  'docs/tutorial/animation': 'graphics',
  'docs/tutorial/garden': 'graphics',
  'docs/tutorial/collisions': 'physics',
  'docs/tutorial/sound': 'audio',
  'docs/tutorial/hud': 'graphics',
  'docs/tutorial/saving': 'platform',
  'docs/tutorial/packaging': 'tools',
  'docs/tutorial/other-targets': 'platform',
  // Platforms and distribution
  'docs/platforms': 'platform',
  'docs/packaging': 'tools',
  'docs/app-identity': 'platform',
  'docs/signing': 'tools',
  'docs/examples/embedding': 'platform',
  'docs/reference/limitations': 'platform',
  // Advanced topics
  'docs/architecture': 'kernel',
  'docs/manual/backends': 'platform',
  'docs/manual/extending': 'kernel',
  'docs/reference/configuration': 'kernel',
  'docs/reference/contracts': 'kernel',
  'docs/reference/components': 'kernel',
};

/** Family of an API reference page, from the namespace in its slug (docs/api/microengine.<namespace>...). */
const apiNamespaceFamilies = {
  kernel: 'kernel', core: 'kernel', scenes: 'kernel',
  graphics: 'graphics',
  physics: 'physics', world: 'physics',
  audio: 'audio',
  input: 'input',
  platform: 'platform', hosting: 'platform', sdl: 'platform', raylib: 'platform', desktop: 'platform',
};

export function familyOf(slug) {
  if (pageFamilies[slug]) return pageFamilies[slug];
  const api = /^docs\/api\/microengine\.([a-z0-9]+)/.exec(slug);
  return api ? apiNamespaceFamilies[api[1]] : undefined;
}

// Socket cards: the contract a manual page documents and the modules that seat in it, with their status written out.
// Modules are named by the technology they run on (upstream technologies are drawn as credited modules, see the art
// direction). `available` is in the published SDK, `repo` is in the repository and in no package (drawn as an empty
// dashed socket). Keep these in step with docs/manual/backends.md.
const shipped = (...names) => names.map(([name, role]) => ({ name, role, status: 'available' }));

export const socketCards = {
  'docs/manual/rendering': {
    contract: 'IGraphicsDevice',
    modules: [
      ...shipped(['SDL 3', '2D renderer'], ['SDL_GPU', 'opt-in desktop device'], ['raylib', 'alternative platform'], ['Headless', 'tests']),
    ],
  },
  'docs/manual/audio': { contract: 'IAudioDevice', modules: shipped(['SDL 3', 'desktops, phones, browsers'], ['raylib', 'alternative platform'], ['Silent', 'tests']) },
  'docs/manual/input': { contract: 'IInputSource', modules: shipped(['SDL 3', 'keys, mouse, gamepads'], ['raylib', 'keys, mouse, gamepads'], ['Headless', 'tests']) },
  'docs/manual/backends': { contract: 'IPlatform', modules: shipped(['SDL 3', 'desktops, phones, browsers'], ['raylib', 'alternative platform'], ['Headless', 'tests']) },
  'docs/manual/world': {
    contract: 'IPhysicsWorld2D',
    note: { text: 'Physics worlds', href: '#physics-worlds' },
    modules: [
      { name: 'Tiles', role: 'tile collision', status: 'available' },
      { name: 'Box2D', role: 'optional package', status: 'available' },
    ],
  },
};

export const statusText = {
  available: 'Available',
  repo: 'In the repository, not published',
  planned: 'Planned',
};
