#!/usr/bin/env node
// Builds the native package a player downloads for one demo and one target of packaging/targets.json
// (packaging/README.md). The same file is checked by verify.mjs, stored under its content key by
// site/scripts/demos.mjs and copied into a release; nothing is rebuilt after it was checked.
//
//   node packaging/package.mjs <Game> --target <id> [--out dist/packages]
//
// Writes <out>/<Game>-<target>.<zip|tar.gz|apk> and prints its path. Desktop packages are self-contained .NET
// single-file executables with SDL 3 and raylib next to them, the game's Content folder, the notices, README.txt and
// build-info.json, in a top folder <Game>/ (on macOS the executable is inside <Title>.app, ad-hoc signed).
// Android packages are APKs for arm64-v8a and x86_64 signed with the key in ANDROID_KEYSTORE (a keystore file),
// ANDROID_KEYSTORE_PASSWORD, ANDROID_KEY_ALIAS and ANDROID_KEY_PASSWORD (default: the store password); without
// them the build stops, unless --debug-key signs it with the machine's debug key for local checks (build-info says
// so, and a release refuses it).
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { registry, nativeKey, tar } from '../site/scripts/demos.mjs';
import { target, fail, PackageError, repoRoot, executable, nativeLibraries, installSteps, androidVersionCode, requiredFiles } from './targets.mjs';

const run = (command, args, options = {}) => execFileSync(command, args, { cwd: repoRoot, encoding: 'utf8', maxBuffer: 256 * 1024 * 1024, ...options });

/** One MSBuild property of a project, as the SDK evaluates it. */
function property(project, name, extra = []) {
  const output = run('dotnet', ['msbuild', project, `-getProperty:${name}`, ...extra]).trim();
  return output || fail(`${project} has no ${name}.`);
}

/** Files under `dir`, relative, with '/' separators. */
function walk(dir, base = dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap(entry => {
    const full = path.join(dir, entry.name);
    return entry.isDirectory() ? walk(full, base) : [path.relative(base, full).split(path.sep).join('/')];
  });
}

/** The game's content files (tracked, not code), relative to its Content folder. */
export function contentFiles(gameDir) {
  return run('git', ['ls-files', '--', `${gameDir}/Content`]).split('\n').filter(Boolean)
    .filter(file => !file.endsWith('.cs')).map(file => file.slice(`${gameDir}/Content/`.length)).sort();
}

function commitInfo() {
  try { return { commit: run('git', ['rev-parse', 'HEAD']).trim(), seconds: Number(run('git', ['show', '-s', '--format=%ct', 'HEAD']).trim()) }; }
  catch { return { commit: 'unknown', seconds: Math.floor(Date.now() / 1000) }; }
}

/** The .NET runtime pack of a RID in the NuGet cache: its licence and third-party notices travel with the package. */
function runtimePack(rid, version) {
  const cache = run('dotnet', ['nuget', 'locals', 'global-packages', '--list']).replace(/^.*?global-packages:\s*/s, '').trim();
  const dir = path.join(cache, `microsoft.netcore.app.runtime.${rid}`, version);
  for (const file of ['LICENSE.TXT', 'THIRD-PARTY-NOTICES.TXT']) if (!fs.existsSync(path.join(dir, file))) fail(`${path.join(dir, file)} is missing; the publish step should have restored it.`);
  return dir;
}

function copyNotices(folder, rid, dotnet) {
  const pack = runtimePack(rid, dotnet);
  fs.mkdirSync(path.join(folder, 'licenses'), { recursive: true });
  fs.copyFileSync(path.join(repoRoot, 'games', 'LICENSE'), path.join(folder, 'LICENSE.txt'));
  fs.copyFileSync(path.join(repoRoot, 'THIRD-PARTY-NOTICES.md'), path.join(folder, 'THIRD-PARTY-NOTICES.txt'));
  fs.copyFileSync(path.join(repoRoot, 'LICENSE'), path.join(folder, 'licenses', 'microengine-LICENSE.txt'));
  fs.copyFileSync(path.join(pack, 'LICENSE.TXT'), path.join(folder, 'licenses', 'dotnet-LICENSE.txt'));
  fs.copyFileSync(path.join(pack, 'THIRD-PARTY-NOTICES.TXT'), path.join(folder, 'licenses', 'dotnet-THIRD-PARTY-NOTICES.txt'));
}

function readme(info, t) {
  const eol = t.os === 'windows' ? '\r\n' : '\n';
  return [
    `${info.title} ${info.gameVersion} for ${t.label} (${t.detail})`,
    `A demo game built with MicroEngine ${info.engineVersion}: https://github.com/NastMz/MicroEngine`,
    '',
    'Requirements',
    `  ${t.requirements}`,
    '',
    'Running it',
    ...installSteps(t, info.game, info.title).map((step, i) => `  ${i + 1}. ${step}`),
    '',
    'Options: --platform raylib runs it on raylib instead of SDL 3; --platform sdl-software uses SDL\'s CPU renderer',
    '(for machines without a working GPU driver); --list-platforms lists them. F11 or Alt+Enter toggles fullscreen.',
    `Controls and the game's story: https://github.com/NastMz/MicroEngine/tree/main/games/${info.game}`,
    '',
    'Licence',
    `  ${info.title} is not open source: it is all rights reserved (LICENSE.txt). You may play it and keep this copy;`,
    '  you may not redistribute, sell or republish it. MicroEngine itself is MIT licensed',
    '  (licenses/microengine-LICENSE.txt). Third-party components and the demo\'s CC0 art and audio are listed in',
    '  THIRD-PARTY-NOTICES.txt and the game\'s CREDITS.md in the repository.',
    '',
    `Build: package key ${info.key}, commit ${info.commit}, ${info.target}, .NET ${info.dotnet}.`,
    '',
  ].join(eol);
}

/** Info.plist of the macOS application bundle. */
function infoPlist(info) {
  const escape = s => s.replace(/&/g, '&amp;').replace(/</g, '&lt;');
  const entries = {
    CFBundleDevelopmentRegion: 'en', CFBundleExecutable: info.game, CFBundleIdentifier: `dev.microengine.${info.game.toLowerCase()}`,
    CFBundleInfoDictionaryVersion: '6.0', CFBundleName: info.title, CFBundleDisplayName: info.title, CFBundlePackageType: 'APPL',
    CFBundleShortVersionString: info.gameVersion, CFBundleVersion: info.gameVersion, LSMinimumSystemVersion: '14.0',
    LSApplicationCategoryType: 'public.app-category.games',
  };
  return ['<?xml version="1.0" encoding="UTF-8"?>',
    '<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">',
    '<plist version="1.0">', '<dict>',
    ...Object.entries(entries).map(([k, v]) => `  <key>${k}</key>\n  <string>${escape(v)}</string>`),
    '  <key>NSHighResolutionCapable</key>\n  <true/>', '</dict>', '</plist>', ''].join('\n');
}

/** Archives `<stage>/<top>` into `file`. */
async function archive(stage, top, file, t) {
  fs.rmSync(file, { force: true });
  if (t.archive === 'tar.gz') {
    // Fixed order, owner and modes, no timestamps: the executable and the shared libraries are 0755, everything else
    // 0644, whatever file system the package was staged on (Windows has no executable bit).
    const files = walk(path.join(stage, top)).sort().map(f => `${top}/${f}`);
    const executables = new Set([executable(top, t), ...nativeLibraries(t)].map(f => `${top}/${f}`));
    // The mode keeps its file-type bits: node-tar reads the entry type from them.
    const filter = (entry, stat) => {
      if (stat.isFile()) stat.mode = (stat.mode & ~0o7777) | (executables.has(entry.replace(/\\/g, '/')) ? 0o755 : 0o644);
      return true;
    };
    await tar.create({ gzip: { level: 9 }, file, cwd: stage, portable: true, noMtime: true, filter }, files);
  } else if (process.platform === 'darwin') {
    // ditto keeps the bundle's signature and extended attributes as Finder's own compression would.
    run('ditto', ['-c', '-k', '--sequesterRsrc', '--keepParent', path.join(stage, top), file]);
  } else if (process.platform === 'win32') {
    // Windows' own bsdtar writes ZIP files.
    run(path.join(process.env.SystemRoot ?? 'C:\\Windows', 'System32', 'tar.exe'), ['-a', '-c', '-f', file, '-C', stage, top]);
  } else {
    run('zip', ['-q', '-r', '-X', file, top], { cwd: stage });
  }
}

async function desktop(game, t, out, { key, commit }) {
  if (t.os === 'macos' && process.platform !== 'darwin') fail('macOS packages are built on macOS (the app is signed with codesign).');
  const project = path.join(repoRoot, `games/${game.project}`);
  const gameVersion = property(project, 'Version');
  const engineVersion = property(project, 'MicroEngineVersion');
  const dotnet = property(project, 'BundledNETCoreAppPackageVersion');
  const stage = fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()), 'microengine-package-'));
  const publish = path.join(stage, 'publish');
  run('dotnet', ['publish', project, '-c', 'Release', '-r', t.rid, '--self-contained', '-p:PublishSingleFile=true',
    '-p:EnableCompressionInSingleFile=true', '-p:MicroEngineNativePackage=true', '-o', publish, '-nologo', '-v', 'quiet'], { stdio: ['ignore', 'pipe', 'inherit'] });
  const info = { schema: 1, game: game.id, title: game.title, gameVersion, engineVersion, target: t.id, runtime: t.rid, key, commit, dotnet, signing: t.os === 'macos' ? 'ad-hoc' : 'none' };
  const top = path.join(stage, game.id);
  fs.mkdirSync(top);
  const exe = executable(game.id, t);
  // Anything else the publish step leaves (debug files, a stray deps.json) would be an unchecked file.
  const expected = new Set([exe, ...nativeLibraries(t)]);
  const unexpected = fs.readdirSync(publish).filter(f => !expected.has(f) && f !== 'Content');
  if (unexpected.length) fail(`${game.id}: unexpected files from dotnet publish: ${unexpected.join(', ')}.`);
  if (t.os === 'macos') {
    const contents = path.join(top, `${game.title}.app`, 'Contents');
    fs.mkdirSync(path.join(contents, 'MacOS'), { recursive: true });
    fs.mkdirSync(path.join(contents, 'Resources'), { recursive: true });
    for (const f of expected) fs.cpSync(path.join(publish, f), path.join(contents, 'MacOS', f));
    if (fs.existsSync(path.join(publish, 'Content'))) fs.cpSync(path.join(publish, 'Content'), path.join(contents, 'Resources', 'Content'), { recursive: true });
    fs.writeFileSync(path.join(contents, 'Info.plist'), infoPlist(info));
    // Without a Developer ID certificate the app is signed ad hoc: Apple silicon runs only signed code, and the
    // signature also seals the bundle. Libraries first, then the bundle.
    for (const lib of nativeLibraries(t)) run('codesign', ['--force', '--sign', '-', '--timestamp=none', path.join(contents, 'MacOS', lib)]);
    run('codesign', ['--force', '--sign', '-', '--timestamp=none', path.join(top, `${game.title}.app`)]);
  } else {
    fs.cpSync(publish, top, { recursive: true });
  }
  copyNotices(top, t.rid, dotnet);
  fs.writeFileSync(path.join(top, 'README.txt'), readme(info, t));
  fs.writeFileSync(path.join(top, 'build-info.json'), `${JSON.stringify(info, null, 2)}\n`);
  const missing = requiredFiles({ game: game.id, title: game.title, content: contentFiles(game.dir) }, t).filter(f => !fs.existsSync(path.join(top, f)));
  if (missing.length) fail(`${game.id} ${t.id}: the package lacks ${missing.join(', ')}.`);
  const file = path.join(out, `${game.id}-${t.id}.${t.archive}`);
  await archive(stage, game.id, file, t);
  fs.rmSync(stage, { recursive: true, force: true });
  return { file, info };
}

async function android(game, t, out, { key, commit, seconds, debugKey }) {
  const project = path.join(repoRoot, 'platforms', 'android');
  const props = [`-p:Game=${game.id}`];
  const gameVersion = property(path.join(repoRoot, `games/${game.project}`), 'Version');
  const engineVersion = property(project, 'MicroEngineVersion', props);
  const dotnet = property(project, 'BundledNETCoreAppPackageVersion', props);
  const env = process.env;
  const signing = env.ANDROID_KEYSTORE ? 'release' : debugKey ? 'debug' : fail('Android packages need ANDROID_KEYSTORE, ANDROID_KEYSTORE_PASSWORD and ANDROID_KEY_ALIAS (packaging/README.md, "Android signing"); --debug-key signs a local test build with the debug key instead.');
  const stage = fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()), 'microengine-package-'));
  const versionCode = androidVersionCode(seconds);
  const args = ['publish', project, '-c', 'Release', ...props, '-p:MicroEngineNativePackage=true', `-p:ApplicationVersion=${versionCode}`,
    `-p:ApplicationDisplayVersion=${gameVersion}`, '-o', path.join(stage, 'publish'), '-nologo', '-v', 'quiet'];
  if (env.ANDROID_HOME) args.push(`-p:AndroidSdkDirectory=${env.ANDROID_HOME}`);
  if (env.JAVA_HOME) args.push(`-p:JavaSdkDirectory=${env.JAVA_HOME}`);
  if (signing === 'release') {
    if (!env.ANDROID_KEYSTORE_PASSWORD || !env.ANDROID_KEY_ALIAS) fail('ANDROID_KEYSTORE is set, but ANDROID_KEYSTORE_PASSWORD or ANDROID_KEY_ALIAS is missing.');
    // Passwords are read from the environment by the Android build (env:NAME), so they never appear in a command line.
    args.push('-p:AndroidKeyStore=true', `-p:AndroidSigningKeyStore=${path.resolve(env.ANDROID_KEYSTORE)}`, `-p:AndroidSigningKeyAlias=${env.ANDROID_KEY_ALIAS}`,
      '-p:AndroidSigningStorePass=env:ANDROID_KEYSTORE_PASSWORD', `-p:AndroidSigningKeyPass=env:${env.ANDROID_KEY_PASSWORD ? 'ANDROID_KEY_PASSWORD' : 'ANDROID_KEYSTORE_PASSWORD'}`);
  }
  run('dotnet', args, { stdio: ['ignore', 'pipe', 'inherit'] });
  const apk = fs.readdirSync(path.join(stage, 'publish')).find(f => f.endsWith('-Signed.apk')) ?? fail(`${game.id}: dotnet publish produced no signed APK.`);
  const info = { schema: 1, game: game.id, title: game.title, gameVersion, engineVersion, target: t.id, runtime: t.rid, key, commit, dotnet, signing,
    versionCode: String(versionCode), applicationId: `dev.microengine.${game.id.toLowerCase()}` };
  const file = path.join(out, `${game.id}-${t.id}.apk`);
  fs.copyFileSync(path.join(stage, 'publish', apk), file);
  fs.writeFileSync(path.join(out, `${game.id}-${t.id}.build-info.json`), `${JSON.stringify(info, null, 2)}\n`);
  fs.rmSync(stage, { recursive: true, force: true });
  return { file, info };
}

/** Builds one package; returns { file, info } with the archive path and its build-info.json contents. */
export async function buildPackage(gameId, targetId, { out = path.join(repoRoot, 'dist', 'packages'), debugKey = false } = {}) {
  const t = target(targetId);
  const game = registry().find(g => g.id === gameId) ?? fail(`unknown game "${gameId}" (web/games.props).`);
  const project = fs.readFileSync(path.join(repoRoot, 'web', 'games.props'), 'utf8').match(new RegExp(`'${gameId}'">[\\s\\S]*?<GameProject>([^<]+)</GameProject>`))[1];
  fs.mkdirSync(out, { recursive: true });
  const { key } = nativeKey(gameId, t.id);
  const { commit, seconds } = commitInfo();
  const context = { key, commit, seconds, debugKey };
  return t.os === 'android' ? android({ ...game, project }, t, out, context) : desktop({ ...game, project }, t, out, context);
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
    .then(({ file, info }) => console.log(`${file}\n${game} ${info.gameVersion} (${info.target}), engine ${info.engineVersion}, key ${info.key}, signing ${info.signing}`))
    .catch(error => { console.error(`error: ${error instanceof PackageError ? error.message : error?.stack ?? error}`); process.exit(1); });
}
