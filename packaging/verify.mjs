#!/usr/bin/env node
// Checks the exact file a player downloads (packaging/README.md, "What a package check proves"): the archive itself,
// not the build folder it came from. Prints one line per level and writes the result as JSON (--record), which
// site/scripts/demos.mjs stores in the package's manifest.
//
//   node packaging/verify.mjs <archive> --game <Game> --target <id> [--key K] [--record FILE] [--no-run] [--audio optional] [--reference FILE] [--keep-captures DIR]
//
// Levels: integrity (SHA-256 and size), structure (every file a package needs, nothing unexpected, the content
// files of the game, no build-machine path), build-info (game, target, key, versions), dependencies (glibc on Linux,
// the signature on macOS, ABIs, signing and version on Android), then, unless --no-run, on this machine: platforms
// (the executable starts and SDL can run here), report (the platform that ran and whether audio opened) and rendered
// (the game's scripted scenarios from the package's executable, compared with the target's SDL reference).
// An Android package is run with platforms/android/device-checks.mjs on a running emulator or device.
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import crypto from 'node:crypto';
import { execFileSync, spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { registry, tar } from '../site/scripts/demos.mjs';
import { target, fail, PackageError, repoRoot, requiredFiles, checkBuildInfo, executable, nativeLibraries } from './targets.mjs';
import { contentFiles } from './package.mjs';

const sh = (command, args, options = {}) => execFileSync(command, args, { encoding: 'utf8', maxBuffer: 256 * 1024 * 1024, ...options });
const has = command => spawnSync(process.platform === 'win32' ? 'where' : 'which', [command], { stdio: 'ignore' }).status === 0;

function walk(dir, base = dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap(entry => {
    const full = path.join(dir, entry.name);
    return entry.isDirectory() ? walk(full, base) : [path.relative(base, full).split(path.sep).join('/')];
  });
}

async function extract(file, t, dir) {
  if (t.archive === 'tar.gz') return tar.extract({ file, cwd: dir, strict: true, preserveOwner: false });
  if (process.platform === 'darwin') return sh('ditto', ['-x', '-k', file, dir]);
  if (process.platform === 'win32') return sh(path.join(process.env.SystemRoot ?? 'C:\\Windows', 'System32', 'tar.exe'), ['-x', '-f', file, '-C', dir]);
  return sh('unzip', ['-q', file, '-d', dir]);
}

/** Entries of a ZIP-based file (APK) without extracting it. */
function listZip(file) {
  if (process.platform === 'win32') return sh(path.join(process.env.SystemRoot ?? 'C:\\Windows', 'System32', 'tar.exe'), ['-t', '-f', file]).split(/\r?\n/).filter(Boolean);
  return sh('unzip', ['-Z1', file]).split('\n').filter(Boolean);
}

/**
 * Paths of the machine that built and checks the package: its user profile, the checkout and, in CI, the runner's
 * workspace and temporary folder. Prebuilt third-party binaries (the .NET runtime, SDL, raylib) carry the paths of
 * their own build machines, which say nothing about ours, so generic runner prefixes are not searched.
 */
function machinePaths() {
  // A CI runner's home (/home/runner, /Users/runner) is also where SDL and .NET were built by their own projects.
  const home = process.env.GITHUB_ACTIONS === 'true' ? null : os.homedir();
  const paths = [home, repoRoot, process.env.GITHUB_WORKSPACE, process.env.RUNNER_TEMP].filter(p => p && p.length > 3);
  return [...new Set(paths.flatMap(p => [p, p.replace(/\\/g, '/')]))];
}

function scanForPaths(folder, files) {
  const needles = machinePaths().map(p => Buffer.from(p));
  return files.filter(f => {
    const data = fs.readFileSync(path.join(folder, f));
    return needles.some(n => data.includes(n));
  });
}

export async function verifyPackage(file, { game: gameId, target: targetId, key, run = true, audioOptional = false, reference, keepCaptures, log = console.log } = {}) {
  const t = target(targetId);
  // The target's reference, or one recorded for the machine that checks (a hosted runner without a GPU).
  const referenceFile = path.resolve(repoRoot, reference ?? t.reference);
  const game = registry().find(g => g.id === gameId) ?? fail(`unknown game "${gameId}".`);
  const record = { game: gameId, target: t.id, file: path.basename(file), checkedOn: `${process.platform}-${process.arch}${process.env.GITHUB_ACTIONS === 'true' ? ' (GitHub-hosted runner)' : ''}`, reference: path.relative(repoRoot, referenceFile).split(path.sep).join('/'), levels: {} };
  const level = (name, ok, detail) => {
    record.levels[name] = { ok, detail };
    log(`${ok ? 'PASS' : 'FAIL'} ${name}: ${detail}`);
    if (!ok) record.failed = true;
  };
  if (!fs.existsSync(file)) fail(`${file} does not exist.`);
  const data = fs.readFileSync(file);
  record.sha256 = crypto.createHash('sha256').update(data).digest('hex');
  record.size = data.length;
  level('integrity', data.length > 0, `${(data.length / 1048576).toFixed(1)} MB, SHA-256 ${record.sha256}`);
  const project = `games/${path.basename(game.dir)}/${path.basename(game.dir)}.csproj`;
  const versions = JSON.parse(sh('dotnet', ['msbuild', path.join(repoRoot, project), '-getProperty:Version', '-getProperty:MicroEngineVersion'])).Properties;
  const content = contentFiles(game.dir);

  if (t.os === 'android') {
    const infoFile = file.replace(/\.apk$/, '.build-info.json');
    const info = checkBuildInfo(fs.existsSync(infoFile) ? fs.readFileSync(infoFile, 'utf8') : '{}', { game: gameId, target: t.id, key, gameVersion: versions.Version, engineVersion: versions.MicroEngineVersion });
    record.buildInfo = info;
    level('build-info', true, `${info.title} ${info.gameVersion}, engine ${info.engineVersion}, versionCode ${info.versionCode}, signed with the ${info.signing} key`);
    const entries = new Set(listZip(file));
    const needed = ['AndroidManifest.xml', 'lib/arm64-v8a/libSDL3.so', 'lib/x86_64/libSDL3.so', ...content.map(c => `assets/Content/${c}`),
      'assets/licenses/LICENSE.txt', 'assets/licenses/microengine-LICENSE.txt', 'assets/licenses/THIRD-PARTY-NOTICES.txt'];
    const missing = needed.filter(e => !entries.has(e));
    const abis = [...new Set([...entries].filter(e => e.startsWith('lib/')).map(e => e.split('/')[1]))].sort();
    level('structure', missing.length === 0, missing.length ? `missing ${missing.slice(0, 6).join(', ')}` : `ABIs ${abis.join(', ')}; ${content.length} content files; notices`);
    const tools = process.env.ANDROID_HOME && path.join(process.env.ANDROID_HOME, 'build-tools');
    const version = tools && fs.existsSync(tools) ? fs.readdirSync(tools).sort().pop() : null;
    if (version) {
      const ext = process.platform === 'win32' ? '.bat' : '';
      const signer = sh(path.join(tools, version, `apksigner${ext}`), ['verify', '--print-certs', file], { shell: process.platform === 'win32' });
      const digest = signer.match(/certificate SHA-256 digest: ([0-9a-f]+)/)?.[1];
      const badging = sh(path.join(tools, version, `aapt${process.platform === 'win32' ? '.exe' : ''}`), ['dump', 'badging', file]);
      const code = badging.match(/versionCode='(\d+)'/)?.[1], name = badging.match(/versionName='([^']+)'/)?.[1], sdk = badging.match(/sdkVersion:'(\d+)'/)?.[1];
      // The launcher shows the launchable activity's label and icon (the application's may differ): both are the game's.
      const launcher = badging.match(/launchable-activity: name='[^']+'\s+label='([^']*)' icon='([^']*)'/);
      const identity = launcher?.[1] === info.title && launcher?.[2] === 'res/mipmap-anydpi-v26/appicon.xml';
      record.certificate = digest;
      level('dependencies', Boolean(digest) && code === info.versionCode && name === info.gameVersion && sdk === '24' && identity,
        `signature verified (certificate SHA-256 ${digest?.slice(0, 16)}…), versionCode ${code}, versionName ${name}, minSdk ${sdk}; launcher '${launcher?.[1]}' with ${launcher?.[2] || 'no icon'}`);
    } else {
      level('dependencies', false, 'ANDROID_HOME has no build-tools: apksigner and aapt are needed to check the signature and version');
    }
    if (run) {
      const out = fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()), 'microengine-verify-'));
      const device = spawnSync('node', [path.join(repoRoot, 'platforms/android/device-checks.mjs'), '--apk', file, '--game', gameId, '--out', out], { encoding: 'utf8' });
      log(device.stdout.trim());
      level('device', device.status === 0, device.status === 0 ? 'installed, started, rendered, input, touch and lifecycle on the device (lines above)' : `device-checks failed: ${device.stderr.trim().split('\n').pop()}`);
      const compared = spawnSync('node', [path.join(repoRoot, 'web/tests/desktop.mjs'), '--skip-run', '--games', gameId, '--out', out, '--reference', referenceFile], { encoding: 'utf8' });
      log(compared.stdout.trim());
      level('rendered', compared.status === 0, compared.stdout.trim().split('\n').pop());
      if (keepCaptures) fs.cpSync(out, keepCaptures, { recursive: true });
      fs.rmSync(out, { recursive: true, force: true });
    }
    return record;
  }

  const dir = fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()), 'microengine-verify-'));
  try {
    await extract(file, t, dir);
    const top = path.join(dir, gameId);
    const files = fs.existsSync(top) ? walk(top).sort() : [];
    const required = requiredFiles({ game: gameId, title: game.title, applicationId: game.applicationId, content }, t);
    const missing = required.filter(f => !files.includes(f));
    const allowed = new Set(required);
    // macOS adds the signature's own files inside the bundle.
    const unexpected = files.filter(f => !allowed.has(f) && !(t.os === 'macos' && f.startsWith(`${game.title}.app/Contents/_CodeSignature/`)));
    const outside = fs.readdirSync(dir).filter(f => f !== gameId);
    level('structure', missing.length === 0 && unexpected.length === 0 && outside.length === 0,
      missing.length ? `missing ${missing.slice(0, 6).join(', ')}` : unexpected.length ? `unexpected ${unexpected.slice(0, 6).join(', ')}` : outside.length ? `files outside ${gameId}/: ${outside.join(', ')}`
        : `${files.length} files in ${gameId}/: executable, ${nativeLibraries(t).join(' and ')}, ${content.length} content files, notices, README.txt, build-info.json`);
    const leaks = scanForPaths(top, files);
    level('no build-machine paths', leaks.length === 0, leaks.length ? `found in ${leaks.slice(0, 4).join(', ')}` : 'no user profile or checkout path in any file');
    const info = checkBuildInfo(fs.readFileSync(path.join(top, 'build-info.json'), 'utf8'), { game: gameId, target: t.id, key, gameVersion: versions.Version, engineVersion: versions.MicroEngineVersion });
    record.buildInfo = info;
    level('build-info', true, `${info.title} ${info.gameVersion}, engine ${info.engineVersion}, key ${info.key}, commit ${info.commit.slice(0, 12)}, .NET ${info.dotnet}`);

    const code = t.os === 'macos' ? path.join(top, `${game.title}.app`, 'Contents', 'MacOS') : top;
    const exe = path.join(code, executable(gameId, t));
    if (t.os === 'linux' && has('objdump')) {
      const needs = f => (sh('objdump', ['-T', path.join(code, f)]).match(/GLIBC_\d+\.\d+(\.\d+)?/g) ?? []).map(s => s.slice(6)).sort((a, b) => a.localeCompare(b, 'en', { numeric: true })).pop();
      const sdl = needs('libSDL3.so'), app = needs(executable(gameId, t)), raylib = needs('libraylib.so');
      const within = v => v.localeCompare(t.glibc, 'en', { numeric: true }) <= 0;
      record.glibc = { executable: app, sdl, raylib };
      level('dependencies', within(sdl) && within(app), `glibc needed: executable ${app}, SDL ${sdl} (promised: ${t.glibc}); raylib ${raylib}`);
    } else if (t.os === 'macos') {
      const verified = spawnSync('codesign', ['--verify', '--deep', '--strict', path.join(top, `${game.title}.app`)], { encoding: 'utf8' });
      const gatekeeper = spawnSync('spctl', ['--assess', '--type', 'execute', path.join(top, `${game.title}.app`)], { encoding: 'utf8' });
      // spctl prints "<path>: <verdict>" and its reason; the verdict and reason are what matter.
      record.gatekeeper = (gatekeeper.stderr || gatekeeper.stdout).trim().split(path.join(top, `${game.title}.app`)).join(`${game.title}.app`).split(/\r?\n/).join('; ');
      level('dependencies', verified.status === 0, `codesign --verify --deep --strict ${verified.status === 0 ? 'passed (ad-hoc signature)' : `failed: ${verified.stderr.trim()}`}; Gatekeeper: ${record.gatekeeper || 'accepted'}`);
    } else {
      level('dependencies', true, `self-contained .NET ${info.dotnet}; ${nativeLibraries(t).join(', ')} next to the executable`);
    }

    if (run) {
      const env = { ...process.env, MICROENGINE_PLATFORM: '' };
      const listed = spawnSync(exe, ['--list-platforms'], { encoding: 'utf8', env, cwd: os.tmpdir() });
      const sdlLine = listed.stdout.split('\n').find(l => l.startsWith('sdl:'));
      level('platforms', listed.status === 0 && Boolean(sdlLine), listed.status === 0 ? `started; ${sdlLine ? 'SDL can run here' : 'SDL is unavailable here'}` : `exit ${listed.status ?? listed.signal}: ${listed.stderr.trim()}`);
      const saves = fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()), 'microengine-saves-'));
      const saveVar = { MeadowRun: 'MEADOW_SAVE_DIR', CourtyardKey: 'COURTYARD_SAVE_DIR', TrailDuel: 'TRAIL_SAVE_DIR', NeonRift: 'RIFT_SAVE_DIR' }[gameId];
      const report = spawnSync(exe, ['--report', '--hidden', '--frames', '120'], { encoding: 'utf8', env: { ...env, [saveVar]: saves }, cwd: os.tmpdir(), timeout: 120000 });
      const platform = report.stdout.match(/^platform: (.*)$/m)?.[1], audio = report.stdout.match(/^audio: (.*)$/m)?.[1];
      record.platform = platform; record.audio = audio;
      // A machine without a sound device (hosted Windows runners) cannot show that audio opens: with --audio optional
      // that is recorded as not verified here, never as verified.
      const audioOk = audio === 'available' || (audioOptional && audio === 'unavailable');
      level('report', report.status === 0 && /^sdl\b/.test(platform ?? '') && audioOk,
        report.status === 0 ? `ran 120 updates on ${platform}; audio device ${audio}${audio !== 'available' && audioOptional ? ' (this machine has no sound device: audio not verified here)' : ''}`
          : `exit ${report.status ?? report.signal}: ${report.stderr.trim().split('\n').pop()}`);
      fs.rmSync(saves, { recursive: true, force: true });
      const out = fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()), 'microengine-verify-captures-'));
      const compared = spawnSync('node', [path.join(repoRoot, 'web/tests/desktop.mjs'), '--packages', dir, '--backend', 'sdl', '--games', gameId, '--out', out, '--reference', referenceFile], { encoding: 'utf8' });
      log(compared.stdout.trim());
      level('rendered', compared.status === 0, compared.stdout.trim().split('\n').pop() ?? `exit ${compared.status}`);
      if (keepCaptures) fs.cpSync(out, keepCaptures, { recursive: true });
      fs.rmSync(out, { recursive: true, force: true });
    }
  } finally {
    fs.rmSync(dir, { recursive: true, force: true });
  }
  return record;
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const args = process.argv.slice(2);
  const option = name => { const i = args.indexOf(`--${name}`); return i >= 0 ? args[i + 1] : undefined; };
  if (!args[0] || args[0].startsWith('--') || !option('game') || !option('target')) {
    console.log('usage: node packaging/verify.mjs <archive> --game <Game> --target <id> [--key K] [--record FILE] [--no-run] [--audio optional] [--reference FILE] [--keep-captures DIR]');
    process.exit(args[0] ? 1 : 0);
  }
  verifyPackage(path.resolve(args[0]), { game: option('game'), target: option('target'), key: option('key'), run: !args.includes('--no-run'), audioOptional: option('audio') === 'optional', reference: option('reference'), keepCaptures: option('keep-captures') && path.resolve(option('keep-captures')) })
    .then(record => {
      if (option('record')) fs.writeFileSync(option('record'), `${JSON.stringify(record, null, 2)}\n`);
      if (process.env.GITHUB_STEP_SUMMARY) {
        fs.appendFileSync(process.env.GITHUB_STEP_SUMMARY, [`### ${record.game} for ${record.target}: \`${record.file}\``, '', '| Level | Result | Detail |', '| --- | --- | --- |',
          ...Object.entries(record.levels).map(([name, l]) => `| ${name} | ${l.ok ? 'pass' : 'FAIL'} | ${String(l.detail).replace(/\|/g, '\\|').replace(/\n/g, ' ')} |`), '', ''].join('\n'));
      }
      process.exit(record.failed ? 1 : 0);
    })
    .catch(error => { console.error(`error: ${error instanceof PackageError ? error.message : error?.stack ?? error}`); process.exit(1); });
}
