// The public roadmap. There are no dates on purpose. Every item has an availability (how far it is from a developer's
// hands, the same scale as capabilities.ts) and the subsystem family it belongs to:
//   available   in the published SDK on nuget.org
//   next        merged into the engine, arriving with the next SDK version
//   repository  in the engine's repository only, in no published package (experimental)
//   planned     not built; `horizon` says how firm: intended next work, research with an open outcome, or a
//               long-term direction that is not a commitment
// `stages` and `roadmap` are the whole picture. `completed`, `next` and `vision` are the older views of the same data.
import { publishedSdk, type Availability, type Family } from './capabilities.ts';

export type { Availability, Family } from './capabilities.ts';

/** How firm a planned item is. Also the badge of the older track views. */
export type RoadmapStatus = 'done' | 'planned' | 'exploring' | 'vision';

export const statusLabels: Record<RoadmapStatus, { label: string; meaning: string }> = {
  done: { label: 'Completed', meaning: 'Built and verified; available in the published SDK.' },
  planned: { label: 'Planned', meaning: 'Intended next work. Scope may change; no date is promised.' },
  exploring: { label: 'Exploring', meaning: 'Research. The result may change the plan or be dropped.' },
  vision: { label: 'Vision', meaning: 'Long-term direction. Not available and not a commitment.' },
};

export interface RoadmapItem {
  text: string;
  family: Family;
  status: Availability;
  /** For `planned` items: how firm the intention is. */
  horizon?: Exclude<RoadmapStatus, 'done'>;
  /** For `planned` items: the track (`RoadmapTrack.title`) it belongs to. */
  track?: string;
}

/** The four stages in reading order, each with its heading and what it promises. */
export const stages: { status: Availability; title: string; summary: string }[] = [
  {
    status: 'available',
    title: 'Available today',
    summary: `In the published SDK, ${publishedSdk}, and in the four demos.`,
  },
  {
    status: 'next',
    title: 'Next SDK version',
    summary: 'Built and merged into the engine, not published yet. It arrives with the next SDK version, a breaking Alpha whose migration notes say what changes.',
  },
  {
    status: 'repository',
    title: 'In the repository, not published',
    summary: 'Experiments that live in the engine’s source and in no package. They prove where the architecture can go; they are not features you can install.',
  },
  {
    status: 'planned',
    title: 'Planned and further out',
    summary: 'Not built. Planned work may change; exploring and vision items may change or be dropped.',
  },
];

const available: RoadmapItem[] = [
  { text: '2D engine in C#/.NET 10, Alpha 0.x', family: 'kernel', status: 'available' },
  { text: 'Four complete demo games', family: 'tools', status: 'available' },
  { text: 'Procedural and file-based assets', family: 'graphics', status: 'available' },
  { text: 'Scenes, rendering, audio, input and UI', family: 'kernel', status: 'available' },
  { text: 'Swept tile collision with one-way platforms', family: 'physics', status: 'available' },
  { text: 'Saves (persistence) and achievements', family: 'kernel', status: 'available' },
  { text: 'SDL 3 platform on Windows, Linux, macOS, Android and iOS', family: 'platform', status: 'available' },
  { text: 'raylib in WebAssembly for browsers, and as a desktop alternative', family: 'platform', status: 'available' },
  { text: 'Named platform profiles and replaceable subsystems behind IPlatform', family: 'platform', status: 'available' },
  { text: 'On-screen touch controls and suspend/resume on phones', family: 'input', status: 'available' },
  { text: 'Analog input and per-game touch controls (d-pad, joystick, buttons) on phones and in mobile browsers', family: 'input', status: 'available' },
  { text: 'Native demo downloads for Windows, Linux, macOS and Android', family: 'tools', status: 'available' },
  { text: `SDK ${publishedSdk} on nuget.org: engine packages, the microengine tool and a dotnet new template`, family: 'tools', status: 'available' },
  { text: 'Per-game name, id, version and icon on every platform', family: 'tools', status: 'available' },
  { text: 'This site and its documentation', family: 'tools', status: 'available' },
];

const nextVersion: RoadmapItem[] = [
  { text: 'The engine as separate modules, composed once for every host (desktop, browser, phones, tests)', family: 'kernel', status: 'next' },
  { text: 'A scene world: entities with ids, transform hierarchies and attachments', family: 'kernel', status: 'next' },
  { text: 'Drawing between fixed steps at interpolated positions, for smooth motion at any display rate', family: 'kernel', status: 'next' },
  { text: 'HD canvases with smooth scaling and texture filtering', family: 'graphics', status: 'next' },
  { text: 'TrueType and OpenType fonts at any size', family: 'graphics', status: 'next' },
  { text: 'A graphics device contract, and an opt-in SDL_GPU device on desktops with shaders compiled ahead of time', family: 'graphics', status: 'next' },
  { text: 'Physics worlds behind one contract, with the engine’s tile backend', family: 'physics', status: 'next' },
  { text: 'One audio mixer on every platform', family: 'audio', status: 'next' },
  { text: 'Input as device events with players and layers, rebinding and text input', family: 'input', status: 'next' },
  { text: 'SDL 3 in browsers (WebAssembly), replacing raylib there; raylib stays a desktop alternative', family: 'platform', status: 'next' },
  { text: 'The microengine shaders command (compiling needs Windows)', family: 'tools', status: 'next' },
];

const repository: RoadmapItem[] = [
  { text: 'A minimal 3D path on the SDL_GPU device: glTF meshes, a perspective camera, depth and one lit material', family: 'graphics', status: 'repository' },
  { text: 'A Box2D physics backend behind the same physics contract (optional, never published)', family: 'physics', status: 'repository' },
];

export interface RoadmapTrack {
  title: string;
  status: RoadmapStatus;
  family: Family;
  summary: string;
  items: string[];
}

export const next: RoadmapTrack[] = [
  {
    title: 'SDK distribution',
    status: 'planned',
    family: 'tools',
    summary: 'The next steps for the SDK, which is published on nuget.org.',
    items: ['Android App Bundles for Google Play', 'Notarized macOS and signed iOS builds exercised with a real Apple account', 'Windows code signing in microengine package'],
  },
  {
    title: 'Engine robustness',
    status: 'planned',
    family: 'kernel',
    summary: 'How games behave under real-world conditions. Drawing between fixed steps arrives with the next SDK version.',
    items: ['Web audio and behaviour in background tabs'],
  },
  {
    title: 'Mobile experience',
    status: 'exploring',
    family: 'input',
    summary: 'Each game has its own touch controls with safe areas and feedback; what remains is polish. Text input arrives with the next SDK version.',
    items: ['Hide controls during dialogues', 'Haptics on iOS', 'A player setting for control size and opacity'],
  },
];

export const vision: RoadmapTrack[] = [
  {
    title: 'Visual editor',
    status: 'vision',
    family: 'tools',
    summary: 'Tools around the code-first engine. Nothing of it exists today.',
    items: ['Project management', 'Scene editing', 'Property inspector', 'Asset management', 'Visual development tools', 'Distribution with an installer'],
  },
  {
    title: 'Towards HD, 2.5D and 3D',
    status: 'vision',
    family: 'graphics',
    summary: 'The published SDK is 2D. A minimal 3D path in the repository proves the architecture can carry 3D; a 3D renderer and 3D games are a direction, not a commitment.',
    items: ['A 3D renderer beyond the minimal path: more lights, skinning and animation', '2.5D and 3D games'],
  },
];

const planned: RoadmapItem[] = [...next, ...vision].flatMap(track =>
  track.items.map(text => ({ text, family: track.family, status: 'planned' as const, horizon: track.status as Exclude<RoadmapStatus, 'done'>, track: track.title })));

/** Every roadmap item, in stage order. */
export const roadmap: RoadmapItem[] = [...available, ...nextVersion, ...repository, ...planned];

/** The available items as plain text (the older "Available today" list). */
export const completed: string[] = available.map(item => item.text);
