// The landing's own copy that is data rather than prose: the facts under the opening, the technologies MicroEngine
// builds on, what its graphics do today, and where the project stands. Every line follows the status rules of
// site/design/README.md, section 7: the published SDK is what nuget.org serves; work merged after it is "next SDK
// version"; what no package carries is "in the repository, not published". Keep it in step with docs/guide/sdk.md,
// docs/architecture.md and docs/migrations.md ("Next release").

/** The specification line under the opening: plain facts, no metrics. */
export const facts: { term: string; value: string }[] = [
  { term: 'Language', value: 'C# on .NET 10' },
  { term: 'Runs on', value: 'Windows, Linux, macOS, Android, iOS, browsers' },
  { term: 'Draws', value: '2D: pixel art today, HD 2D next' },
  { term: 'Licence', value: 'MIT, no royalties' },
];

/** Upstream technologies, drawn as credited modules: their names, never their logos, and only what MicroEngine uses. */
export const upstream: { name: string; role: string }[] = [
  { name: 'SDL 3', role: 'Windows, input, sound and rendering on desktops and phones' },
  { name: 'raylib', role: 'The alternative desktop platform, and browsers in the published SDK' },
  { name: '.NET 10', role: 'The runtime, the build, and WebAssembly for browsers' },
  { name: 'NVorbis', role: 'OGG music decoding' },
];

export type Status = 'available' | 'next' | 'repo';

/** What MicroEngine draws, by status. */
export const graphics: { contract: string; module: string; note: string; status: Status; empty?: boolean }[] = [
  { contract: 'IRenderer', module: 'Pixel art', note: 'crisp scaling, sprites, atlases, tile maps, particles, light, parallax', status: 'available' },
  { contract: 'Presentation.HighResolution2D', module: 'HD 2D', note: 'smooth scaling and TrueType fonts', status: 'next' },
  { contract: 'Graphics3D', module: '3D: glTF meshes', note: 'a 3D path over SDL_GPU', status: 'repo', empty: true },
];

/** Where the project stands: the published SDK, the next one, and what stays in the repository. */
export const ledger: { status: Status; title: string; items: string[] }[] = [
  {
    status: 'available',
    title: 'In the published SDK',
    items: [
      'Scenes, input actions, 2D graphics, synthesised and file audio, UI, saves, achievements',
      'SDL 3 on desktops and phones; raylib on desktops and in browsers',
      'The microengine tool, the dotnet new template and the engine packages on nuget.org',
    ],
  },
  {
    status: 'next',
    title: 'Next SDK version',
    items: [
      'SDL 3 as the browser platform',
      'The engine split into modules, one assembly each',
      'The scene world: entities, transforms, interpolation',
      'Physics worlds, with a tile backend',
      'The HD 2D presentation preset and TrueType fonts',
    ],
  },
  {
    status: 'repo',
    title: 'In the repository, not published',
    items: ['The 3D path: glTF meshes, a 3D camera and renderer', 'The SDL_GPU graphics device', 'The Box2D physics backend'],
  },
];

export const statusWords: Record<Status, string> = {
  available: 'Available',
  next: 'Next SDK version',
  repo: 'In the repository, not published',
};
