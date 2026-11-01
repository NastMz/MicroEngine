// The native targets of packaging/targets.json and the rules every package of one follows (packaging/README.md):
// file names, the layout a player extracts, and what build-info.json must say. Shared by package.mjs, verify.mjs
// and site/scripts/demos.mjs, so building, checking and releasing agree on one description.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

export const packagingDir = path.dirname(fileURLToPath(import.meta.url));
export const repoRoot = path.dirname(packagingDir);
export const { targets } = JSON.parse(fs.readFileSync(path.join(packagingDir, 'targets.json'), 'utf8'));

class PackageError extends Error {}
export { PackageError };
export const fail = message => { throw new PackageError(message); };

/** The target with this id; fails for an unknown one. */
export function target(id) {
  return targets.find(t => t.id === id) ?? fail(`unknown target "${id}"; known targets: ${targets.map(t => t.id).join(', ')} (packaging/targets.json).`);
}

/** Extension of a target's archive, without the dot. */
export const extension = t => t.archive;

/**
 * The file a player downloads: `<Game>-<game version>-<target>.<ext>`, for example `NeonRift-1.0.0-windows-x64.zip`.
 * Release assets use this name, so a downloaded file says which game, version and system it is for.
 */
export function downloadName(game, gameVersion, t) {
  if (!/^[A-Za-z0-9]+$/.test(game)) fail(`"${game}" is not a game id.`);
  if (!/^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$/.test(gameVersion)) fail(`"${gameVersion}" is not a version.`);
  return `${game}-${gameVersion}-${t.id}.${extension(t)}`;
}

/** The executable's file name inside a desktop package. */
export const executable = (game, t) => (t.os === 'windows' ? `${game}.exe` : game);

/** Notices every package carries (the demo is all rights reserved, the engine MIT, the rest third-party). */
export const noticeFiles = ['LICENSE.txt', 'THIRD-PARTY-NOTICES.txt', 'licenses/microengine-LICENSE.txt', 'licenses/dotnet-LICENSE.txt', 'licenses/dotnet-THIRD-PARTY-NOTICES.txt'];

/** Native libraries next to a desktop executable: SDL 3 (the default platform) and raylib (`--platform raylib`). */
export function nativeLibraries(t) {
  if (t.os === 'windows') return ['SDL3.dll', 'raylib.dll'];
  if (t.os === 'linux') return ['libSDL3.so', 'libraylib.so'];
  if (t.os === 'macos') return ['libSDL3.dylib', 'libraylib.dylib'];
  return [];
}

/**
 * Files a desktop package must contain, relative to its top folder `<Game>/`: the executable and its libraries
 * (inside `<Title>.app/Contents/MacOS` on macOS), the content files, the notices, README.txt and build-info.json.
 */
export function requiredFiles({ game, title, content = [] }, t) {
  if (t.os === 'android') return [];
  const app = t.os === 'macos' ? `${title}.app/Contents/` : '';
  const code = t.os === 'macos' ? `${app}MacOS/` : '';
  const data = t.os === 'macos' ? `${app}Resources/` : '';
  return [
    `${code}${executable(game, t)}`,
    ...nativeLibraries(t).map(lib => `${code}${lib}`),
    ...(t.os === 'macos' ? [`${app}Info.plist`] : []),
    ...content.map(file => `${data}Content/${file}`),
    ...noticeFiles, 'README.txt', 'build-info.json',
  ];
}

/** Install steps of a target for one game, with <Game> and <Title> filled in. */
export const installSteps = (t, game, title) => t.install.map(step => step.replaceAll('<Game>', game).replaceAll('<Title>', title));

/**
 * Android's versionCode for a build: minutes between 2026-01-01 and the build's commit, so a later demo package
 * installs over an earlier one (Android refuses only a lower code). It fits Android's limit (2100000000) for
 * thousands of years.
 */
export function androidVersionCode(commitSeconds) {
  const minutes = Math.floor((commitSeconds - Date.UTC(2026, 0, 1) / 1000) / 60);
  if (!Number.isInteger(minutes) || minutes < 1) fail(`commit time ${commitSeconds} is before 2026-01-01.`);
  return minutes;
}

/** Checks a package's build-info.json against what it should describe; returns the parsed object. */
export function checkBuildInfo(text, expected) {
  let info;
  try { info = JSON.parse(text); } catch { fail('build-info.json is not JSON.'); }
  for (const [field, value] of Object.entries(expected)) {
    if (value !== undefined && info[field] !== value) fail(`build-info.json says ${field} "${info[field]}", expected "${value}".`);
  }
  for (const field of ['game', 'title', 'gameVersion', 'engineVersion', 'target', 'key', 'commit', 'dotnet']) {
    if (typeof info[field] !== 'string' || !info[field]) fail(`build-info.json has no ${field}.`);
  }
  return info;
}
