#!/usr/bin/env node
// Builds the native package a player downloads for one demo and one target of packaging/targets.json
// (packaging/README.md). The demos are packaged like any game made with the SDK: by `microengine package`
// (src/MicroEngine.Cli, docs/guide/packaging.md), reading each demo's identity from its project file. This script adds
// only what MicroEngine's release pipeline needs: the content key recorded in build-info.json and the file names the
// store expects. The same file is checked by verify.mjs, stored under its content key by site/scripts/demos.mjs and
// copied into a release; nothing is rebuilt after it was checked.
//
//   node packaging/package.mjs <Game> --target <id> [--out dist/packages] [--debug-key]
//
// Writes <out>/<Game>-<target>.<zip|tar.gz|apk> and prints its path. Android packages are signed with the key in
// ANDROID_KEYSTORE, ANDROID_KEYSTORE_PASSWORD, ANDROID_KEY_ALIAS and ANDROID_KEY_PASSWORD (default: the store
// password); without them the build stops, unless --debug-key signs it with the machine's debug key for local checks
// (build-info says so, and a release refuses it).
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { registry, nativeKey } from '../site/scripts/demos.mjs';
import { target, fail, PackageError, repoRoot } from './targets.mjs';

const run = (command, args, options = {}) => execFileSync(command, args, { cwd: repoRoot, encoding: 'utf8', maxBuffer: 256 * 1024 * 1024, ...options });

/** The game's content files (tracked, not code), relative to its Content folder. */
export function contentFiles(gameDir) {
  return run('git', ['ls-files', '--', `${gameDir}/Content`]).split('\n').filter(Boolean)
    .filter(file => !file.endsWith('.cs')).map(file => file.slice(`${gameDir}/Content/`.length)).sort();
}

let tool;
/** Runs `microengine` built from this checkout (built once per process), so the demos use the tool the SDK ships. */
export function microengine(args, options = {}) {
  if (!tool) {
    run('dotnet', ['build', 'src/MicroEngine.Cli', '-c', 'Release', '-nologo', '-v', 'quiet'], { stdio: ['ignore', 'pipe', 'inherit'] });
    tool = path.join(repoRoot, 'src', 'MicroEngine.Cli', 'bin', 'Release', 'net10.0', 'MicroEngine.Cli.dll');
  }
  return run('dotnet', [tool, ...args], { stdio: ['ignore', 'pipe', 'inherit'], ...options });
}

/** Builds one package; returns { file, info } with the archive path and its build-info.json contents. */
export async function buildPackage(gameId, targetId, { out = path.join(repoRoot, 'dist', 'packages'), debugKey = false } = {}) {
  const t = target(targetId);
  const game = registry().find(g => g.id === gameId) ?? fail(`unknown game "${gameId}" (games/<Id>/<Id>.csproj).`);
  fs.mkdirSync(out, { recursive: true });
  const { key } = nativeKey(gameId, t.id);
  const stage = fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()), 'microengine-package-'));
  try {
    const name = t.os === 'android' ? 'android' : t.os;
    const output = microengine(['package', name, '--project', game.project, '--out', stage, '--key', key, ...(debugKey ? ['--debug-key'] : [])]);
    const built = output.match(/^package: (.+)$/m)?.[1]?.trim() ?? fail(`${gameId} ${t.id}: microengine printed no package.`);
    const file = path.join(out, `${gameId}-${t.id}.${t.archive}`);
    fs.copyFileSync(built, file);
    let info;
    if (t.os === 'android') {
      info = JSON.parse(fs.readFileSync(built.replace(/\.apk$/, '.build-info.json'), 'utf8'));
      fs.writeFileSync(path.join(out, `${gameId}-${t.id}.build-info.json`), `${JSON.stringify(info, null, 2)}\n`);
    } else {
      info = {};
    }
    return { file, info: { ...info, key, target: t.id } };
  } finally {
    fs.rmSync(stage, { recursive: true, force: true });
  }
}

if (process.argv[1] && path.resolve(process.argv[1]) === path.resolve(new URL(import.meta.url).pathname.replace(/^\/(\w:)/, '$1'))) {
  const args = process.argv.slice(2);
  const option = name => { const i = args.indexOf(`--${name}`); return i >= 0 ? args[i + 1] : undefined; };
  const game = args[0];
  if (!game || game.startsWith('--') || !option('target')) {
    console.log('usage: node packaging/package.mjs <Game> --target <windows-x64|linux-x64|macos-arm64|android> [--out DIR] [--debug-key]');
    process.exit(game ? 1 : 0);
  }
  buildPackage(game, option('target'), { out: option('out') && path.resolve(option('out')), debugKey: args.includes('--debug-key') })
    .then(({ file, info }) => console.log(`${file}\n${game} (${info.target}), key ${info.key}`))
    .catch(error => { console.error(`error: ${error instanceof PackageError ? error.message : error?.stack ?? error}`); process.exit(1); });
}
