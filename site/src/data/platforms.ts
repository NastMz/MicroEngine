// The public compatibility matrix: one row per official target, one cell per kind of evidence. A cell names where the
// evidence came from, never more: "hardware" means a real machine of that kind, "virtual" a hosted CI runner or a
// container (no real GPU, virtual display and sound server), "emulator" and "simulator" what they say. Sources:
// docs/evaluations/platform-3.md, docs/evaluations/product-1.md and docs/evaluations/input-1.md (§7b, played by hand).
// Update them together when a platform is validated.
export type Evidence = 'person' | 'hardware' | 'virtual' | 'emulator' | 'simulator' | 'none' | 'na';

export const evidenceLabels: Record<Evidence, { label: string; meaning: string }> = {
  person: { label: 'Played', meaning: 'A person played the games on real hardware of this kind.' },
  hardware: { label: 'Real hardware', meaning: 'Automated runs on a real machine of this kind (GPU, sound device).' },
  virtual: { label: 'CI / virtual', meaning: 'Automated runs on a hosted CI runner or in a container: no real GPU, a virtual display and sound server.' },
  emulator: { label: 'Emulator', meaning: 'Automated runs on the Android emulator (x86_64).' },
  simulator: { label: 'Simulator', meaning: 'Automated runs in Apple’s iOS simulator on a Mac. Not an iPhone.' },
  none: { label: 'Not checked', meaning: 'No evidence yet.' },
  na: { label: '—', meaning: 'Does not apply.' },
};

export interface Cell { level: Evidence; note: string }

export interface PlatformRow {
  id: string;
  name: string;
  platform: string;
  status: { label: string; tone: 'ok' | 'partial' | 'no' };
  build: Cell; tests: Cell; rendering: Cell; input: Cell; audio: Cell; lifecycle: Cell; hardware: Cell;
  /** The id of its downloadable target in packaging/targets.json, or why there is none. */
  download: { target: string } | { none: string };
}

export const columns: [keyof PlatformRow, string][] = [
  ['build', 'Build'], ['tests', 'Headless tests'], ['rendering', 'Rendering'], ['input', 'Input'], ['audio', 'Audio'],
  ['lifecycle', 'Lifecycle'], ['hardware', 'Real device'],
];

export const platforms: PlatformRow[] = [
  {
    id: 'windows', name: 'Windows x64', platform: 'SDL 3 (default) · raylib · SDL software',
    status: { label: 'Runs on hardware', tone: 'ok' },
    build: { level: 'virtual', note: 'Hosted Windows runner on engine changes; local builds' },
    tests: { level: 'virtual', note: 'Engine and four games, every engine change' },
    rendering: { level: 'hardware', note: '138 scripted captures; Direct3D 11, OpenGL and Vulkan identical; the four ZIP packages reproduce them' },
    input: { level: 'person', note: 'Keyboard and gamepads played by hand with raylib and with the SDL downloads (an Xbox controller)' },
    audio: { level: 'person', note: 'Heard with raylib; SDL’s device opens and plays (packages included)' },
    lifecycle: { level: 'na', note: 'Desktop windows are not suspended' },
    hardware: { level: 'person', note: 'Windows 11 PC with a GPU; the four downloads played by hand' },
    download: { target: 'windows-x64' },
  },
  {
    id: 'linux', name: 'Linux x64', platform: 'SDL 3 (default) · raylib · SDL software',
    status: { label: 'Runs on hardware', tone: 'ok' },
    build: { level: 'virtual', note: 'Ubuntu runner on every engine change' },
    tests: { level: 'virtual', note: 'Engine and four games, every engine change' },
    rendering: { level: 'virtual', note: '138 captures on Mesa (software) under Xvfb; the package reproduces them on Ubuntu 22.04, Debian 12 and Fedora 41' },
    input: { level: 'virtual', note: 'System keyboard through the X server (raylib); scripted input on SDL' },
    audio: { level: 'virtual', note: 'Virtual PulseAudio: a played tone recorded and recognised (raylib); SDL’s device opens' },
    lifecycle: { level: 'na', note: 'Desktop windows are not suspended' },
    hardware: { level: 'person', note: 'Ubuntu PC: the four downloads played by hand; Wayland not identified' },
    download: { target: 'linux-x64' },
  },
  {
    id: 'macos', name: 'macOS (Apple silicon)', platform: 'SDL 3 with Metal (default); raylib needs an OpenGL GPU',
    status: { label: 'Checked, not yet played', tone: 'partial' },
    build: { level: 'virtual', note: 'Hosted macOS 26 arm64 runner' },
    tests: { level: 'virtual', note: 'Headless tests on the hosted runner' },
    rendering: { level: 'virtual', note: '138 captures on SDL against a macOS reference (paravirtual GPU)' },
    input: { level: 'none', note: 'Scripted input only; no system keyboard check' },
    audio: { level: 'virtual', note: 'SDL’s device opens on the runner' },
    lifecycle: { level: 'na', note: 'Desktop windows are not suspended' },
    hardware: { level: 'none', note: 'No Mac with a person or a real GPU yet' },
    download: { target: 'macos-arm64' },
  },
  {
    id: 'android', name: 'Android', platform: 'SDL 3 (the Android host)',
    status: { label: 'Runs on a phone', tone: 'ok' },
    build: { level: 'virtual', note: 'APKs for arm64-v8a and x86_64' },
    tests: { level: 'na', note: 'Game rules are tested on the desktop' },
    rendering: { level: 'emulator', note: 'All four games, 138 captures (x86_64 emulator)' },
    input: { level: 'person', note: 'Touch controls and a Bluetooth PlayStation controller played on a phone; taps, drags and two thumbs also checked on the emulator' },
    audio: { level: 'emulator', note: 'SDL’s device opens; not listened to' },
    lifecycle: { level: 'emulator', note: 'Home suspends the game, returning resumes it' },
    hardware: { level: 'person', note: 'Redmi Note 14 Pro (arm64, Android 16): the four APKs played by hand; no tablet' },
    download: { target: 'android' },
  },
  {
    id: 'ios', name: 'iOS', platform: 'SDL 3 with Metal (the iOS host)',
    status: { label: 'Simulator only', tone: 'partial' },
    build: { level: 'virtual', note: 'Simulator app on a hosted Mac' },
    tests: { level: 'na', note: 'Game rules are tested on the desktop' },
    rendering: { level: 'simulator', note: 'Two games, 25 captures within the SDL reference' },
    input: { level: 'simulator', note: 'Scripted touches on the simulator’s control layout; it cannot be sent system touches by script' },
    audio: { level: 'simulator', note: 'SDL’s device opens' },
    lifecycle: { level: 'simulator', note: 'Another app suspends the game; returning resumes it' },
    hardware: { level: 'none', note: 'No iPhone or iPad has run it' },
    download: { none: 'Installing on an iPhone needs Apple signing; a simulator build cannot run on a device' },
  },
  {
    id: 'browser', name: 'Browsers (WebAssembly)', platform: 'raylib compiled to WebAssembly',
    status: { label: 'Played in Chrome, Firefox and Edge', tone: 'ok' },
    build: { level: 'virtual', note: 'Pinned toolchain on a hosted runner' },
    tests: { level: 'na', note: 'Same game code as the desktop' },
    rendering: { level: 'virtual', note: '138 captures byte-identical to desktop raylib, Chrome and Firefox' },
    input: { level: 'person', note: 'Keyboard (automated and by hand); one gamepad by hand in a Chromium browser; touch played in Chrome on an Android phone and checked with real multi-touch in emulated mobile Chrome' },
    audio: { level: 'person', note: 'After the first click or key; stops in a hidden tab' },
    lifecycle: { level: 'virtual', note: 'Saves persist; a hidden page suspends the game' },
    hardware: { level: 'person', note: 'Desktop Chrome and Firefox; Edge on the desktop and Chrome on an Android 16 phone played by hand; Safari not validated' },
    download: { none: 'Plays on the demo pages; nothing to install' },
  },
];
