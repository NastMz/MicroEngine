// The supported platforms, as a game developer needs them: how a game runs there, where it is built, what
// `microengine package` produces and what players need. Functional limits only; keep in step with
// docs/guide/platforms.md and docs/guide/packaging.md. Rows describe the published SDK; `runtimeNext` says what the next
// SDK version changes there, and `status` uses the availability scale of capabilities.ts.
import type { Availability, Family } from './capabilities.ts';

export type Support = 'supported' | 'preview';

export const supportLabels: Record<Support, string> = {
  supported: 'Supported',
  preview: 'Preview',
};

export interface PlatformRow {
  id: string;
  name: string;
  support: Support;
  /** How far it is from a developer's hands (every row today: in the published SDK). */
  status: Availability;
  /** The subsystem family, for the family colour and icon. */
  family: Extract<Family, 'platform'>;
  /** The platform profile the game runs on there, in the published SDK. */
  runtime: string;
  /** What the next SDK version changes in `runtime`, when it changes it. */
  runtimeNext?: string;
  /** Where `microengine package` builds it, and what it produces. */
  build: string;
  /** What a player's device needs. */
  requires: string;
  /** Input devices the game receives. */
  input: string;
  /** Functional limits a developer should plan around. */
  notes: string;
  /** The id of the demos' downloadable target (packaging/targets.json), or how the demos are offered instead. */
  download: { target: string } | { none: string };
}

export const platforms: PlatformRow[] = [
  {
    id: 'windows', name: 'Windows', support: 'supported', status: 'available', family: 'platform',
    runtime: 'SDL 3 (Direct3D 11, OpenGL or Vulkan) · raylib · SDL software renderer',
    build: 'microengine package windows, on Windows: a portable ZIP with a self-contained executable',
    requires: 'Windows 10 or 11, 64-bit (x64)',
    input: 'Keyboard, mouse, gamepads',
    notes: 'Executables are not code-signed by the tool, so SmartScreen may warn until you sign them.',
    download: { target: 'windows-x64' },
  },
  {
    id: 'linux', name: 'Linux', support: 'supported', status: 'available', family: 'platform',
    runtime: 'SDL 3 (OpenGL) · raylib · SDL software renderer',
    build: 'microengine package linux, from any system: a portable tar.gz with a desktop entry and icon',
    requires: 'x86_64 with glibc 2.34 or later (Ubuntu 22.04, Debian 12, Fedora 35 or newer), X11 or Wayland, PulseAudio or PipeWire',
    input: 'Keyboard, mouse, gamepads',
    notes: 'The raylib alternative needs glibc 2.38 (Ubuntu 24.04 or newer).',
    download: { target: 'linux-x64' },
  },
  {
    id: 'macos', name: 'macOS', support: 'supported', status: 'available', family: 'platform',
    runtime: 'SDL 3 with Metal · raylib where OpenGL is available',
    build: 'microengine package macos, on a Mac: an application bundle, signed ad hoc or with your Developer ID',
    requires: 'macOS 14 or later on Apple silicon',
    input: 'Keyboard, mouse, gamepads',
    notes: 'Intel Macs are not packaged. Without Apple notarization, players allow the app once in Privacy & Security.',
    download: { target: 'macos-arm64' },
  },
  {
    id: 'android', name: 'Android', support: 'supported', status: 'available', family: 'platform',
    runtime: 'SDL 3 (OpenGL ES) with on-screen touch controls',
    build: 'microengine package android: an APK for arm64-v8a and x86_64, signed with your key',
    requires: 'Android 7.0 (API 24) or later, 64-bit',
    input: 'Touch controls, keyboards, gamepads',
    notes: 'APKs, not Android App Bundles: Google Play needs an .aab, which the tool does not build yet.',
    download: { target: 'android' },
  },
  {
    id: 'ios', name: 'iOS and iPadOS', support: 'preview', status: 'available', family: 'platform',
    runtime: 'SDL 3 with Metal and on-screen touch controls',
    build: 'microengine package ios on a Mac with Xcode: an .ipa signed with your Apple certificate (ios-simulator: a Simulator app)',
    requires: 'iOS 15 or later',
    input: 'Touch controls, keyboards, gamepads',
    notes: 'Installing on a device needs an Apple Developer account; App Store submission goes through Apple’s tools.',
    download: { none: 'No demo download: installing on an iPhone needs the developer’s Apple signing.' },
  },
  {
    id: 'browser', name: 'Web browsers', support: 'supported', status: 'available', family: 'platform',
    runtime: 'raylib compiled to WebAssembly',
    runtimeNext: 'SDL 3 compiled to WebAssembly, with the same page, input and touch controls; raylib becomes a desktop alternative only',
    build: 'microengine package web, from any system: a static site for any web server',
    requires: 'A browser with WebAssembly and WebGL: Chrome, Edge, Firefox, Safari, on desktops and phones',
    input: 'Keyboard, gamepads, touch controls',
    notes: 'Sound starts after the first click or key press, as browsers require; a hidden tab pauses the game.',
    download: { none: 'Play the demos in the browser on their pages.' },
  },
];
