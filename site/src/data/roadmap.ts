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
  '2D engine in C#/.NET 10',
  'Four complete demo games',
  'Procedural and file-based assets',
  'Scenes, rendering, audio, input and UI',
  'Saves (persistence)',
  'Achievements',
  'Desktop backend with raylib',
  'WebAssembly builds in desktop browsers',
  'One shared web host for every game',
  'This site and its documentation',
];

export const next: RoadmapTrack[] = [
  {
    title: 'SDK distribution',
    status: 'planned',
    summary: 'Make MicroEngine usable without access to its source code.',
    items: ['NuGet packages', 'Templates for C# projects', 'Installation without cloning a repository', 'Versioning and releases'],
  },
  {
    title: 'Engine robustness',
    status: 'planned',
    summary: 'Audit behaviour under real-world conditions.',
    items: ['Variable frame rates and the simulation', 'Fixed timestep, and interpolation where it helps', 'Physics and mechanics consistency under load', 'Web audio and behaviour in background tabs'],
  },
  {
    title: 'Platform compatibility',
    status: 'planned',
    summary: 'Verify platforms the technology should already reach.',
    items: ['Validation on Linux and macOS', 'Validation in Safari', 'Research into additional backends (exploring)'],
  },
  {
    title: 'Mobile experience',
    status: 'exploring',
    summary: 'Find out what playing MicroEngine games on touch screens needs.',
    items: ['Touch control research', 'Virtual joysticks and buttons', 'Integration with the engine’s action-based input', 'Adapting and validating the demos on mobile devices'],
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
