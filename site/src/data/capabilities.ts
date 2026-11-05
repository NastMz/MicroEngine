// What MicroEngine can do, one entry per module or capability, each with the subsystem family it belongs to and how far
// it is from a developer's hands. The site draws these as sockets (site/design/README.md, "Present and future"), so the
// status is the claim: keep it true to the published SDK (sdk/version.json) and the engine's own documentation.
//
//   available   in the published SDK, on nuget.org
//   next        merged into the engine, arriving with the next SDK version (docs label it "Next SDK version")
//   repository  in the engine's repository only: not in any published package, experimental
//   planned     not built: intended work or a long-term direction
//
// Sources: docs/guide/sdk.md (packages), docs/migrations.md "Next release" (what the next version changes),
// docs/architecture.md and the manual pages each entry links to. A project that is not packed (IsPackable false and
// bundled into no package) is `repository`, never `next`.
import published from '../../../sdk/version.json' with { type: 'json' };

export type Availability = 'available' | 'next' | 'repository' | 'planned';
export type Family = 'kernel' | 'graphics' | 'physics' | 'audio' | 'input' | 'platform' | 'tools';

/** The SDK version on nuget.org, which every `available` entry is in. */
export const publishedSdk: string = published.documented;

export const availabilityLabels: Record<Availability, { label: string; meaning: string }> = {
  available: { label: 'Available', meaning: `In the published SDK (${publishedSdk}), installed from nuget.org.` },
  next: { label: 'Next SDK version', meaning: 'Built into the engine, not published yet: arrives with the next SDK version.' },
  repository: { label: 'In the repository, not published', meaning: 'Source in the engine’s repository, in no published package. Experimental; it may change before it ships, if it ships.' },
  planned: { label: 'Planned', meaning: 'Not built. Intended work or a long-term direction, with no date and no promise.' },
};

/** The `status` the brand's `<Socket>` component takes for each availability (it shortens `repository` to `repo`). */
export const socketStatus: Record<Availability, 'available' | 'next' | 'repo' | 'planned'> = {
  available: 'available',
  next: 'next',
  repository: 'repo',
  planned: 'planned',
};

/** Family names as the site writes them (the same as the brand geometry's labels). */
export const familyLabels: Record<Family, string> = {
  kernel: 'Kernel',
  graphics: 'Graphics',
  physics: 'Physics',
  audio: 'Audio',
  input: 'Input',
  platform: 'Platform',
  tools: 'Tools and SDK',
};

export const families: Family[] = ['kernel', 'graphics', 'physics', 'audio', 'input', 'platform', 'tools'];

export interface Capability {
  /** Stable id, for keys and anchors. */
  id: string;
  family: Family;
  status: Availability;
  /** The module, as a socket names it. */
  name: string;
  /** The contract (socket) it seats in, when it has a public one. */
  contract?: string;
  /** One sentence: what it gives a game, with its functional limits. */
  summary: string;
  /** Upstream technologies it is built on, credited on the module. */
  builtOn?: string[];
  /** Public documentation page (a site path; anchors are checked by the build). */
  docs?: string;
}

export const capabilities: Capability[] = [
  // Kernel: the board every module seats in.
  {
    id: 'loop-scenes', family: 'kernel', status: 'available', name: 'Loop and scenes', contract: 'Game',
    summary: 'A fixed-step loop, a scene stack with overlays and transitions, coroutines and tweens.',
    docs: '/docs/manual/scenes/',
  },
  {
    id: 'saves-achievements', family: 'kernel', status: 'available', name: 'Saves and achievements',
    summary: 'JSON saves on the device or in the browser, and per-game achievements with banners and a list screen.',
    docs: '/docs/manual/persistence/',
  },
  {
    id: 'modules', family: 'kernel', status: 'next', name: 'Engine modules', contract: 'HostBuilder',
    summary: 'The engine split into modules with one composition that every host (desktop, browser, phones, tests) runs.',
    docs: '/docs/architecture/',
  },
  {
    id: 'scene-world', family: 'kernel', status: 'next', name: 'Scene world', contract: 'IWorld',
    summary: 'Entities with ids, transform hierarchies and attachments, drawn between fixed steps at interpolated positions.',
    docs: '/docs/manual/scenes/#the-scene-world',
  },

  // Graphics.
  {
    id: 'renderer-2d', family: 'graphics', status: 'available', name: '2D renderer', contract: 'IRenderer',
    summary: 'Sprites, atlases, animation, tile layers, a 2D camera, parallax, particles, a light map and a pixel font, in a virtual canvas.',
    docs: '/docs/manual/rendering/',
  },
  {
    id: 'hd-presentation', family: 'graphics', status: 'next', name: 'HD presentation', contract: 'Presentation',
    summary: 'HD canvases with smooth scaling and texture filtering beside the pixel-art default.',
    docs: '/docs/manual/rendering/#hd-canvases-and-smooth-scaling',
  },
  {
    id: 'ttf-fonts', family: 'graphics', status: 'next', name: 'TrueType and OpenType fonts', contract: 'ScalableFont',
    summary: 'TTF and OTF fonts loaded from content and drawn at any size.',
    docs: '/docs/manual/text/#scalable-fonts-ttf-and-otf',
  },
  {
    id: 'graphics-device', family: 'graphics', status: 'next', name: 'Graphics device contract', contract: 'IGraphicsDevice',
    summary: 'One device contract under the 2D API, for engine modules, tools and backend authors; each device declares what it supports.',
    builtOn: ['SDL 3'],
    docs: '/docs/manual/rendering/#the-graphics-device',
  },
  {
    id: 'sdl-gpu', family: 'graphics', status: 'next', name: 'SDL_GPU device', contract: 'IGraphicsDevice',
    summary: 'An opt-in programmable device on desktops (Direct3D 12, Vulkan, Metal): custom shaders compiled ahead of time and depth. Browsers and phones keep SDL’s 2D renderer.',
    builtOn: ['SDL 3'],
    docs: '/docs/manual/rendering/#the-graphics-device',
  },
  {
    id: '3d-path', family: 'graphics', status: 'repository', name: '3D path',
    summary: 'A minimal path that proves 3D on the same device as 2D: glTF meshes, a perspective camera, depth and one lit material. Not a 3D renderer, and in no package.',
    builtOn: ['glTF 2.0'],
    docs: '/docs/manual/rendering/#the-3d-path',
  },

  // Physics.
  {
    id: 'tile-collision', family: 'physics', status: 'available', name: 'Tile collision', contract: 'TileMotion',
    summary: 'Swept movement of a box through tile maps, with one-way platforms; movement rules stay game code.',
    docs: '/docs/manual/world/',
  },
  {
    id: 'physics-worlds', family: 'physics', status: 'next', name: 'Physics worlds', contract: 'IPhysicsWorld2D',
    summary: 'A common physics contract with the engine’s tile backend: kinematic bodies, collision layers, ray casts, sensors and moving solids.',
    docs: '/docs/manual/world/#physics-worlds',
  },
  {
    id: 'box2d', family: 'physics', status: 'repository', name: 'Box2D backend', contract: 'IPhysicsWorld2D',
    summary: 'An optional backend with dynamic bodies, rotation and circles, polygons and capsules, behind the same contract. Not part of any published package.',
    builtOn: ['Box2D'],
    docs: '/docs/manual/world/#the-box2d-backend',
  },

  // Audio.
  {
    id: 'audio', family: 'audio', status: 'available', name: 'Sound and music', contract: 'IAudioDevice',
    summary: 'A retro synthesizer and step-sequencer music made in code, WAV effects and streamed OGG music, with music fades.',
    builtOn: ['NVorbis'],
    docs: '/docs/manual/audio/',
  },
  {
    id: 'mixer', family: 'audio', status: 'next', name: 'One mixer on every platform',
    summary: 'Every platform plays through the engine’s own mixer over a PCM sink, so sound behaves the same everywhere.',
    docs: '/docs/manual/audio/#one-mixer-on-every-platform',
  },

  // Input.
  {
    id: 'actions', family: 'input', status: 'available', name: 'Input actions', contract: 'InputSystem',
    summary: 'Games read actions, never keys: keyboard, gamepads and per-game on-screen touch controls, digital and analog.',
    docs: '/docs/manual/input/',
  },
  {
    id: 'input-events', family: 'input', status: 'next', name: 'Devices, players and layers',
    summary: 'Input as a stream of device events with ids, players and layers, rebinding and text input.',
    docs: '/docs/manual/input/',
  },

  // Platform: hosts that run the same game everywhere.
  {
    id: 'sdl', family: 'platform', status: 'available', name: 'SDL 3 platform', contract: 'IPlatform',
    summary: 'Windows, Linux, macOS, Android and iOS (preview).',
    builtOn: ['SDL 3'],
    docs: '/docs/platforms/',
  },
  {
    id: 'raylib', family: 'platform', status: 'available', name: 'raylib platform', contract: 'IPlatform',
    summary: 'The browser platform of the published SDK (WebAssembly) and an alternative on desktops. From the next SDK version it is a desktop alternative only.',
    builtOn: ['raylib'],
    docs: '/docs/manual/backends/',
  },
  {
    id: 'sdl-browser', family: 'platform', status: 'next', name: 'SDL 3 in browsers', contract: 'IPlatform',
    summary: 'Browser packages run on SDL 3 compiled to WebAssembly, with the same page, input and touch controls.',
    builtOn: ['SDL 3', 'WebAssembly'],
    docs: '/docs/platforms/',
  },
  {
    id: 'headless', family: 'platform', status: 'available', name: 'Headless platform', contract: 'IPlatform',
    summary: 'Runs a game with no window or sound device, deterministically, for automated tests.',
    docs: '/docs/manual/backends/',
  },

  // Tools and SDK.
  {
    id: 'cli', family: 'tools', status: 'available', name: 'microengine tool',
    summary: 'Create a game from the template, run it and package it for every platform with its own name, id, version and icon.',
    builtOn: ['.NET 10'],
    docs: '/docs/cli/',
  },
  {
    id: 'testing', family: 'tools', status: 'available', name: 'Scripted runs and captures',
    summary: 'Headless runs, scripted input and screenshot captures for automated tests of a game.',
    docs: '/docs/examples/testing/',
  },
  {
    id: 'shaders', family: 'tools', status: 'next', name: 'Shader compiler', contract: 'microengine shaders',
    summary: 'Compiles HLSL shaders ahead of time for the SDL_GPU device. Compiling needs Windows.',
    docs: '/docs/cli/#shaders',
  },
  {
    id: 'editor', family: 'tools', status: 'planned', name: 'Visual editor',
    summary: 'A long-term direction, not a commitment: MicroEngine is code-first and has no editor today.',
  },
];

/** Entries of one family, in the order above. */
export const byFamily = (family: Family): Capability[] => capabilities.filter(c => c.family === family);

/** Entries of one availability, in the order above. */
export const byStatus = (status: Availability): Capability[] => capabilities.filter(c => c.status === status);
