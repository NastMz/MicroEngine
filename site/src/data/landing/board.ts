// The landing's socket board: each subsystem family, the contract its modules seat into, and every implementation with
// its real status. Status words follow site/design/README.md, section 7:
//   available  in the published SDK (sdk/version.json)
//   next       merged in the repository, arriving with the next SDK version (docs/migrations.md, "Next release")
//   repo       in the repository but in no package: drawn as an empty dashed socket, never as available
//   open       an extension point: a module of your own
// Keep it in step with docs/architecture.md and docs/guide/sdk.md; never mark something available before it ships.
import type { families } from '../../components/brand/geometry.mjs';

export type SlotStatus = 'available' | 'next' | 'repo' | 'open';

export interface BoardSlot { name: string; note: string; status: SlotStatus }

export interface BoardBay {
  family: keyof typeof families;
  /** The public type modules of this family implement or plug into, when there is one name for it. */
  contract?: string;
  slots: BoardSlot[];
  href: string;
}

export const kernel = {
  name: 'Kernel and core',
  note: 'the loop, time, scenes, saves, achievements, UI',
  href: '/docs/architecture/',
};

export const bays: BoardBay[] = [
  {
    family: 'platform',
    contract: 'IPlatform',
    href: '/docs/manual/backends/',
    slots: [
      { name: 'SDL 3', note: 'Windows, Linux, macOS, Android, iOS', status: 'available' },
      { name: 'raylib', note: 'desktop alternative; browsers in the published SDK', status: 'available' },
      { name: 'Headless', note: 'tests and scripted runs, no window', status: 'available' },
      { name: 'SDL 3 in browsers', note: 'replaces raylib on the web', status: 'next' },
      { name: 'Your platform', note: 'register a profile of your own', status: 'open' },
    ],
  },
  {
    family: 'graphics',
    contract: 'IRenderer',
    href: '/docs/manual/rendering/',
    slots: [
      { name: 'SDL_Renderer', note: 'Direct3D, Metal, Vulkan, OpenGL', status: 'available' },
      { name: 'raylib', note: 'OpenGL', status: 'available' },
      { name: 'Software renderer', note: 'runs without a GPU', status: 'available' },
      { name: 'SDL_GPU', note: 'shaders compiled ahead of time', status: 'repo' },
      { name: '3D: glTF meshes', note: 'a camera, meshes, one material', status: 'repo' },
    ],
  },
  {
    family: 'physics',
    href: '/docs/manual/world/',
    slots: [
      { name: 'TileMotion', note: 'moving bodies against tile maps', status: 'available' },
      { name: 'Tile physics world', note: 'bodies, sensors, ray and overlap queries', status: 'next' },
      { name: 'Box2D 3.2', note: 'a second physics backend', status: 'repo' },
    ],
  },
  {
    family: 'audio',
    contract: 'AudioMixer',
    href: '/docs/manual/audio/',
    slots: [
      { name: 'Synth', note: 'sound effects and music written in code', status: 'available' },
      { name: 'WAV and OGG', note: 'files from the content folder', status: 'available' },
    ],
  },
  {
    family: 'input',
    contract: 'InputMap',
    href: '/docs/manual/input/',
    slots: [
      { name: 'Keyboard, mouse, gamepads', note: 'bound to named actions', status: 'available' },
      { name: 'Touch controls', note: 'd-pad, joystick and buttons on phones', status: 'available' },
      { name: 'Input scripts', note: 'automated playthroughs drive the same actions', status: 'available' },
    ],
  },
  {
    family: 'tools',
    href: '/docs/sdk/',
    slots: [
      { name: 'microengine', note: 'new, run, package', status: 'available' },
      { name: 'dotnet new template', note: 'a game project ready to run', status: 'available' },
      { name: 'NuGet packages', note: 'engine, SDL and raylib platforms', status: 'available' },
    ],
  },
];

export const statusWords: Record<SlotStatus, string> = {
  available: 'Available',
  next: 'Next SDK version',
  repo: 'In the repository, not published',
  open: 'Extension point',
};
