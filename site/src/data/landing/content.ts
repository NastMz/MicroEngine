// The landing's own copy that is data rather than prose: the facts under the opening, the technologies MicroEngine
// builds on, and the extension points drawn on the socket board. What the engine can do, and the status of each
// module, is not here: it comes from src/data/capabilities.ts, the site's single list. Keep these lines in step with
// docs/guide/sdk.md and docs/architecture.md.
import type { Family } from '../capabilities.ts';

/** The specification line under the opening: plain facts, no metrics. */
export const facts: { term: string; value: string }[] = [
  { term: 'Language', value: 'C# on .NET 10' },
  { term: 'Runs on', value: 'Windows, Linux, macOS, Android, iOS, browsers' },
  { term: 'Draws', value: '2D: pixel art and HD' },
  { term: 'Licence', value: 'MIT, no royalties' },
];

/** Upstream technologies, drawn as credited modules: their names, never their logos, and only what MicroEngine uses. */
export const upstream: { name: string; role: string }[] = [
  { name: 'SDL 3', role: 'Windows, input, sound and rendering on desktops and phones' },
  { name: 'raylib', role: 'The alternative desktop platform, and browsers in the published SDK' },
  { name: '.NET 10', role: 'The runtime, the build, and WebAssembly for browsers' },
  { name: 'NVorbis', role: 'OGG music decoding' },
];

/** Open sockets on the board: places where a game or host seats a module of its own. */
export const openSockets: { family: Family; name: string; note: string }[] = [
  { family: 'platform', name: 'Your platform', note: 'a profile of your own, offered with --platform' },
];

/** The capability ids the graphics section draws as sockets, in order: what ships, what is next, what is not published. */
export const graphicsIds = ['renderer-2d', 'hd-presentation', 'ttf-fonts', '3d-path'];
