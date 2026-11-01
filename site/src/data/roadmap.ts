// The public roadmap. Status words are a promise level, not a schedule: there are no dates on purpose.
// done: shipped and verified · planned: intended next work · exploring: research with an open outcome ·
// vision: long-term direction, not a commitment.
export type RoadmapStatus = 'done' | 'planned' | 'exploring' | 'vision';

export const statusLabels: Record<RoadmapStatus, { label: string; meaning: string }> = {
  done: { label: 'Completed', meaning: 'Built and verified; available in the engine today.' },
  planned: { label: 'Planned', meaning: 'Intended next work. Scope may change; no date is promised.' },
  exploring: { label: 'Exploring', meaning: 'Research. The result may change the plan or be dropped.' },
  vision: { label: 'Vision', meaning: 'Long-term direction. Not available and not a commitment.' },
};

export interface RoadmapTrack { title: string; status: RoadmapStatus; summary: string; items: string[] }

export const completed: string[] = [
  '2D engine in C#/.NET 10, Alpha 0.x',
  'Four complete demo games',
  'Procedural and file-based assets',
  'Scenes, rendering, audio, input and UI',
  'Saves (persistence) and achievements',
  'SDL 3 platform on Windows, Linux, macOS, Android and iOS',
  'raylib in WebAssembly for browsers, and as a desktop alternative',
  'Named platform profiles and replaceable subsystems behind IPlatform',
  'On-screen touch controls and suspend/resume on phones',
  'Native demo downloads, checked as downloaded',
  'This site and its documentation',
];

export const next: RoadmapTrack[] = [
  {
    title: 'SDK-1: packages',
    status: 'planned',
    summary: 'Make MicroEngine easy to add to a game without building it from the repository. The layout is prepared (docs/guide/sdk.md).',
    items: ['NuGet packages: the core and one package per backend', 'dotnet new templates', 'Versioned SDK releases with migration notes', 'A default platform without installing every backend'],
  },
  {
    title: 'Real devices',
    status: 'planned',
    summary: 'Turn automated evidence into played evidence.',
    items: ['Play the SDL builds by hand on Windows, Linux and a Mac', 'An arm64 Android phone', 'An iPhone, with Apple signing for a distributable build', 'Safari and browsers on phones'],
  },
  {
    title: 'Engine robustness',
    status: 'planned',
    summary: 'Audit behaviour under real-world conditions.',
    items: ['Variable frame rates and the simulation', 'Fixed timestep, and interpolation where it helps', 'Web audio and behaviour in background tabs'],
  },
  {
    title: 'Mobile experience',
    status: 'exploring',
    summary: 'The phone hosts run the games with generic on-screen controls; what a good touch experience needs is still open.',
    items: ['Control layouts per game', 'Screen safe areas (notches)', 'Text input, haptics', 'Touch controls in the browser builds'],
  },
];

export const vision: RoadmapTrack[] = [
  {
    title: 'Visual editor',
    status: 'vision',
    summary: 'Tools around the code-first engine. Nothing of it exists today.',
    items: ['Project management', 'Scene editing', 'Property inspector', 'Asset management', 'Visual development tools', 'Distribution with an installer'],
  },
  {
    title: 'Towards 3D',
    status: 'vision',
    summary: 'MicroEngine is 2D only. 3D is a research direction.',
    items: ['3D rendering research', 'Evaluation of physics libraries', 'An architecture independent of specific technologies'],
  },
];
