#!/usr/bin/env node
// The SDK's acceptance test (docs/guide/sdk.md, "How the SDK is checked"): a game made the way an outside developer
// makes one, in a folder outside this repository, from the packages a release would publish, and nothing else.
//
//   node sdk/tests/external.mjs --feed dist/sdk-feed [--targets windows,linux,web,android] [--device] [--keep]
//
// 1. installs `microengine` from the feed (dotnet tool install), into a private tool folder;
// 2. creates a game with `microengine new` (which installs MicroEngine.Templates from the feed);
// 3. gives it its own title, id, version and icon, builds and runs it: an input script moves the hero, and the captures
//    before and after must differ (input, drawing, a sprite loaded from Content);
// 4. packages it with `microengine package` for each target this machine can build, checks the metadata in each
//    package and runs desktop packages; --device also installs and starts the APK on a connected Android device;
// 5. checks the failures a developer meets: invalid id, missing or invalid icon, missing signing key, a target this
//    system cannot build, an unknown target, a missing workload.
// The game's folder never names this repository: NuGet sees only the feed and nuget.org, with its own package cache.
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import zlib from 'node:zlib';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const args = process.argv.slice(2);
const option = name => { const i = args.indexOf(`--${name}`); return i >= 0 ? args[i + 1] : undefined; };
const feed = path.resolve(option('feed') ?? path.join(repo, 'dist', 'sdk-feed'));
const version = fs.readFileSync(path.join(repo, 'Directory.Build.props'), 'utf8').match(/<MicroEngineVersion>([^<]+)</)[1];
const host = { win32: 'windows', linux: 'linux', darwin: 'macos' }[process.platform];
// By default: this system's own desktop target, Linux (cross-built from anywhere) and the web when the workload is here.
const hasWasm = /^\s*wasm-tools\s/m.test(spawnSync('dotnet', ['workload', 'list'], { encoding: 'utf8' }).stdout ?? '');
const targets = (option('targets') ?? [host, 'linux', ...(hasWasm ? ['web'] : [])].join(',')).split(',').filter((t, i, all) => t && all.indexOf(t) === i);
const exe = process.platform === 'win32' ? '.exe' : '';

let failed = 0;
const webPlays = [];
const check = (name, ok, detail = '') => {
  console.log(`${ok ? 'PASS' : 'FAIL'} ${name}${detail ? `: ${detail}` : ''}`);
  if (!ok) failed++;
  return ok;
};

if (!fs.existsSync(feed) || !fs.readdirSync(feed).some(f => f.startsWith(`MicroEngine.Cli.${version}.`))) {
  console.error(`error: ${feed} holds no MicroEngine.Cli ${version} package; run: dotnet pack MicroEngine.slnx -c Release -o ${feed}`);
  process.exit(2);
}
const work = fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()), 'microengine-external-'));
if (work.startsWith(repo)) throw new Error('the external project must live outside the repository');
console.log(`external project in ${work}, SDK ${version} from ${feed}`);
const env = { ...process.env, NUGET_PACKAGES: path.join(work, 'nuget'), DOTNET_CLI_HOME: path.join(work, 'home'), DOTNET_NOLOGO: '1', DOTNET_CLI_TELEMETRY_OPTOUT: '1' };
for (const secret of ['ANDROID_KEYSTORE', 'ANDROID_KEYSTORE_PASSWORD', 'ANDROID_KEY_ALIAS', 'ANDROID_KEY_PASSWORD', 'MICROENGINE_IOS_CODESIGN_KEY', 'MICROENGINE_IOS_PROVISIONING_PROFILE']) delete env[secret];
delete env.MICROENGINE_DOTNET;
fs.writeFileSync(path.join(work, 'NuGet.config'), `<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="sdk-feed" value="${feed}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
`);

const run = (command, list, { cwd = work, extra = {} } = {}) => {
  const r = spawnSync(command, list, { cwd, env: { ...env, ...extra }, encoding: 'utf8', maxBuffer: 256 * 1024 * 1024, shell: false });
  return { code: r.status, out: `${r.stdout ?? ''}${r.stderr ?? ''}`, error: r.error };
};
const tail = text => text.trim().split('\n').slice(-6).join(' | ');

// A PNG of the given size (RGBA), so the test makes its own icons without image tools.
function png(width, height, pixel) {
  const crcTable = Array.from({ length: 256 }, (_, n) => { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; return c >>> 0; });
  const crc = buf => { let c = 0xffffffff; for (const b of buf) c = crcTable[(c ^ b) & 0xff] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; };
  const chunk = (type, data) => {
    const out = Buffer.alloc(12 + data.length);
    out.writeUInt32BE(data.length, 0);
    out.write(type, 4, 'ascii');
    data.copy(out, 8);
    out.writeUInt32BE(crc(out.subarray(4, 8 + data.length)), 8 + data.length);
    return out;
  };
  const header = Buffer.alloc(13);
  header.writeUInt32BE(width, 0); header.writeUInt32BE(height, 4); header[8] = 8; header[9] = 6;
  const raw = Buffer.alloc((width * 4 + 1) * height);
  for (let y = 0; y < height; y++) for (let x = 0; x < width; x++) Buffer.from(pixel(x, y)).copy(raw, y * (width * 4 + 1) + 1 + x * 4);
  return Buffer.concat([Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]), chunk('IHDR', header), chunk('IDAT', zlib.deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]);
}

try {
  // 1. The tool, as a developer installs it.
  const tools = path.join(work, 'tools');
  let r = run('dotnet', ['tool', 'install', 'MicroEngine.Cli', '--version', version, '--tool-path', tools]);
  if (!check('install the microengine tool from the feed', r.code === 0, tail(r.out))) throw new Error('stop');
  const me = path.join(tools, `microengine${exe}`);
  r = run(me, ['--version']);
  check('microengine --version', r.code === 0 && r.out.trim() === version, r.out.trim());
  r = run(me, ['--help']);
  check('microengine --help lists the commands', r.code === 0 && /new <Name>/.test(r.out) && /package <target>/.test(r.out) && /doctor/.test(r.out));
  r = run(me, ['doctor']);
  check('microengine doctor reports every target', /package android/.test(r.out) && /package web/.test(r.out), tail(r.out));

  // 2. A new game from the public template.
  r = run(me, ['new', 'Sample', '--id', 'com.example.sample', '--title', 'Sample Game']);
  if (!check('microengine new Sample (installs MicroEngine.Templates)', r.code === 0, tail(r.out))) throw new Error('stop');
  const game = path.join(work, 'Sample');
  const projectFile = path.join(game, 'Sample.csproj');
  let project = fs.readFileSync(projectFile, 'utf8');
  check('the project references the published packages only', project.includes(`<PackageReference Include="MicroEngine.Desktop" Version="${version}" />`) && !/ProjectReference/.test(project));
  const leaks = fs.readdirSync(game, { recursive: true }).filter(f => fs.statSync(path.join(game, f)).isFile())
    .filter(f => /\.(cs|csproj|json|md)$/.test(f) && fs.readFileSync(path.join(game, f), 'utf8').includes(repo));
  check('no file of the game names this repository', leaks.length === 0, leaks.join(', '));
  r = run('dotnet', ['new', 'microengine-game', '--help']);
  check('dotnet new microengine-game is installed', r.code === 0 && /--app-id/.test(r.out));

  // 3. Its own identity, then build and run.
  fs.writeFileSync(path.join(game, 'custom-icon.png'), png(256, 256, (x, y) => [x, y, 160, 255]));
  project = project.replace('<MicroEngineIcon>icon.png</MicroEngineIcon>', '<MicroEngineIcon>custom-icon.png</MicroEngineIcon>').replace('<Version>0.1.0</Version>', '<Version>1.2.3</Version>');
  fs.writeFileSync(projectFile, project);
  r = run(me, ['info'], { cwd: game });
  check('microengine info shows the identity', r.code === 0 && /Title\s+Sample Game/.test(r.out) && /Id\s+com\.example\.sample/.test(r.out) && /Version\s+1\.2\.3/.test(r.out) && /custom-icon\.png/.test(r.out), tail(r.out));
  r = run(me, ['build'], { cwd: game });
  check('microengine build', r.code === 0, tail(r.out));
  fs.writeFileSync(path.join(game, 'move.script'), '10 capture before.png\n12 hold right 40\n60 capture after.png\n62 quit\n');
  const captures = path.join(work, 'captures');
  r = run(me, ['run', '-c', 'Release', '--platform', 'sdl-software', '--', '--hidden', '--script', path.join(game, 'move.script'), '--captures', captures], { cwd: game });
  const before = path.join(captures, 'before.png'), after = path.join(captures, 'after.png');
  const moved = fs.existsSync(before) && fs.existsSync(after) && !fs.readFileSync(before).equals(fs.readFileSync(after));
  check('microengine run: scripted input moves the hero (captures differ), sprite from Content drawn', r.code === 0 && /done: \d+ updates/.test(r.out) && moved, tail(r.out));

  // 4. Packages.
  const dist = path.join(game, 'dist');
  for (const target of targets) {
    const extra = target === 'android' ? ['--debug-key'] : [];
    r = run(me, ['package', target, ...extra], { cwd: game });
    const file = r.out.match(/^package: (.+)$/m)?.[1]?.trim();
    if (!check(`microengine package ${target}`, r.code === 0 && file && fs.existsSync(file), tail(r.out))) continue;
    checkPackage(target, file, dist);
  }

  // 5. Failures a developer meets, each with an actionable message.
  const original = fs.readFileSync(projectFile, 'utf8');
  const expectFailure = (name, edit, command, pattern) => {
    if (edit) fs.writeFileSync(projectFile, edit(original));
    const result = run(me, command, { cwd: game });
    fs.writeFileSync(projectFile, original);
    check(`fails clearly: ${name}`, result.code !== 0 && pattern.test(result.out), tail(result.out));
  };
  expectFailure('invalid application id', p => p.replace('com.example.sample', 'com.example.my-game'), ['build'], /ApplicationId 'com\.example\.my-game' is not a valid application id/);
  expectFailure('missing icon', p => p.replace('custom-icon.png', 'missing.png'), ['build'], /MicroEngineIcon 'missing\.png' does not exist/);
  fs.writeFileSync(path.join(game, 'wide.png'), png(300, 200, () => [255, 0, 0, 255]));
  expectFailure('icon that is not square', p => p.replace('custom-icon.png', 'wide.png'), ['build'], /300x200 pixels; it must be square/);
  fs.writeFileSync(path.join(game, 'not-png.png'), 'not an image');
  expectFailure('icon that is not a PNG', p => p.replace('custom-icon.png', 'not-png.png'), ['build'], /is not a PNG file/);
  expectFailure('Android package without a signing key', null, ['package', 'android'], /signed with your own key[\s\S]*ANDROID_KEYSTORE/);
  expectFailure('Android package without an application id', p => p.replace(/\s*<ApplicationId>[^<]+<\/ApplicationId>/, ''), ['package', 'android', '--debug-key'], /need an application id/);
  expectFailure('unknown target', null, ['package', 'playstation'], /unknown target 'playstation'/);
  if (process.platform !== 'darwin') {
    expectFailure('macOS package on another system', null, ['package', 'macos'], /built on a Mac/);
    expectFailure('iOS package on another system', null, ['package', 'ios-simulator'], /built on a Mac with Xcode/);
  } else {
    expectFailure('iOS device package without Apple signing', null, ['package', 'ios'], /MICROENGINE_IOS_CODESIGN_KEY/);
  }
  if (process.platform !== 'win32') expectFailure('Windows package on another system', null, ['package', 'windows'], /built on Windows/);
  const workloads = run('dotnet', ['workload', 'list']).out;
  if (!/^\s*wasm-tools\s/m.test(workloads)) expectFailure('web package without the wasm-tools workload', null, ['package', 'web'], /wasm-tools/);
  const outside = run(me, ['build'], { cwd: work });
  check('fails clearly: no project in the folder', outside.code === 2 && /no game project/.test(outside.out), tail(outside.out));
  for (const folder of webPlays) await playInBrowser(folder);
} catch (e) {
  if (e.message !== 'stop') { console.error(e); failed++; }
} finally {
  if (args.includes('--keep')) console.log(`kept ${work}`);
  else fs.rmSync(work, { recursive: true, force: true });
}
console.log(failed ? `${failed} check(s) failed` : 'all checks passed');
process.exit(failed ? 1 : 0);

function checkPackage(target, file, dist) {
  const id = 'Sample';
  if (['windows', 'linux', 'macos'].includes(target)) {
    const into = fs.mkdtempSync(path.join(dist, 'extract-'));
    const tar = process.platform === 'win32' ? path.join(process.env.SystemRoot ?? 'C:\\Windows', 'System32', 'tar.exe') : 'tar';
    const x = target === 'macos' ? run('ditto', ['-x', '-k', file, into]) : run(tar, ['-xf', file, '-C', into]);
    if (!check(`${target}: the package extracts`, x.code === 0, tail(x.out))) return;
    const top = path.join(into, id);
    const info = JSON.parse(fs.readFileSync(path.join(top, 'build-info.json'), 'utf8'));
    check(`${target}: build-info names the game, its version and the SDK`, info.game === id && info.title === 'Sample Game' && info.gameVersion === '1.2.3' && info.engineVersion === version && info.applicationId === 'com.example.sample', JSON.stringify(info));
    const has = rel => fs.existsSync(path.join(top, rel));
    const code = target === 'macos' ? path.join(top, 'Sample Game.app', 'Contents', 'MacOS') : top;
    check(`${target}: executable, content, notices and README`, fs.existsSync(path.join(code, `${id}${target === 'windows' ? '.exe' : ''}`)) && has('README.txt') && has('THIRD-PARTY-NOTICES.txt') && has('licenses/microengine-LICENSE.txt')
      && (target === 'macos' ? has('Sample Game.app/Contents/Resources/Content/hero.png') : has('Content/hero.png')));
    if (target === 'linux') {
      const entry = has('com.example.sample.desktop') ? fs.readFileSync(path.join(top, 'com.example.sample.desktop'), 'utf8') : '';
      check('linux: desktop entry and icon', /Name=Sample Game/.test(entry) && has('icon.png') && fs.readFileSync(path.join(top, 'icon.png')).readUInt32BE(16) === 256);
    }
    if (target === 'macos') {
      const plist = fs.readFileSync(path.join(top, 'Sample Game.app', 'Contents', 'Info.plist'), 'utf8');
      check('macos: Info.plist and icon', /<string>com\.example\.sample<\/string>/.test(plist) && /<string>Sample Game<\/string>/.test(plist) && /CFBundleIconFile/.test(plist) && has('Sample Game.app/Contents/Resources/AppIcon.icns'));
      const sig = run('codesign', ['--verify', '--deep', '--strict', path.join(top, 'Sample Game.app')]);
      check('macos: the app is signed', sig.code === 0, tail(sig.out));
    }
    if (target === 'windows' && process.platform === 'win32') {
      const ps = run('powershell', ['-NoProfile', '-Command', `(Get-Item '${path.join(top, `${id}.exe`)}').VersionInfo.ProductName`]);
      check('windows: the executable carries the game\'s name', ps.out.trim() === 'Sample Game', ps.out.trim());
    }
    if (target === host) {
      const ran = run(path.join(code, `${id}${exe}`), ['--platform', 'sdl-software', '--hidden', '--frames', '30', '--report']);
      check(`${target}: the packaged game runs`, ran.code === 0 && /done: 30 updates/.test(ran.out), tail(ran.out));
    }
  } else if (target === 'web') {
    const folder = file.replace(/\.zip$/, '');
    const index = fs.readFileSync(path.join(folder, 'index.html'), 'utf8');
    const meta = JSON.parse(fs.readFileSync(path.join(folder, 'game.json'), 'utf8'));
    check('web: page title, favicon, game.json and content', /<title>Sample Game<\/title>/.test(index) && /rel="icon" href="icon.png"/.test(index) && meta.title === 'Sample Game'
      && fs.existsSync(path.join(folder, 'icon.png')) && fs.existsSync(path.join(folder, 'Content', 'hero.png')) && fs.existsSync(path.join(folder, '_framework')));
    webPlays.push(folder);
  } else if (target === 'android') {
    const sdk = process.env.ANDROID_HOME;
    const tools = sdk && path.join(sdk, 'build-tools');
    const latest = tools && fs.existsSync(tools) ? fs.readdirSync(tools).sort().pop() : null;
    if (!latest) return check('android: aapt is available to read the APK', false, 'set ANDROID_HOME');
    const badging = run(path.join(tools, latest, `aapt${exe}`), ['dump', 'badging', file]).out;
    check('android: package id, launcher name, icon and version', /package: name='com\.example\.sample'/.test(badging) && /application-label:'Sample Game'/.test(badging) && /application-icon-\d+:'res\/mipmap/.test(badging) && /versionName='1\.2\.3'/.test(badging), tail(badging));
    if (args.includes('--device')) {
      const adb = path.join(sdk, 'platform-tools', `adb${exe}`);
      const installed = run(adb, ['install', '-r', file]);
      run(adb, ['logcat', '-c']);
      run(adb, ['shell', 'am', 'start', '-W', '-n', 'com.example.sample/dev.microengine.GameActivity']);
      spawnSync(process.execPath, ['-e', 'setTimeout(() => {}, 8000)']);
      const log = run(adb, ['logcat', '-d', '-s', 'MicroEngine']).out;
      check('android: the APK installs and the game starts on the device', installed.code === 0 && /microengine: start Sample/.test(log) && !/microengine: error/.test(log), tail(log));
      run(adb, ['uninstall', 'com.example.sample']);
    }
  }
}

/**
 * Serves the web package like any static host (web/serve.mjs) and plays it in Chrome: the page loads, the game starts
 * on the player's gesture and keeps updating without errors. Skipped when Chrome or puppeteer-core is not installed.
 */
async function playInBrowser(folder) {
  const chrome = [process.env.CHROME_PATH, 'C:/Program Files/Google/Chrome/Application/chrome.exe', '/usr/bin/google-chrome', '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'].find(p => p && fs.existsSync(p));
  let puppeteer;
  try {
    const { createRequire } = await import('node:module');
    const loaded = createRequire(path.join(repo, 'web', 'tests', 'package.json'))('puppeteer-core');
    puppeteer = loaded.default ?? loaded;
  } catch { puppeteer = null; }
  if (!chrome || !puppeteer) return console.log('skip web: the page is not played (needs Chrome and npm ci in web/tests)');
  const root = path.dirname(folder), port = 8800 + Math.floor(Math.random() * 100);
  const { spawn } = await import('node:child_process');
  const server = spawn(process.execPath, [path.join(repo, 'web', 'serve.mjs'), root, String(port)], { stdio: 'ignore' });
  const browser = await puppeteer.launch({ executablePath: chrome, headless: true, args: ['--autoplay-policy=no-user-gesture-required'] });
  try {
    await new Promise(r => setTimeout(r, 1000));
    const page = await browser.newPage();
    const errors = [];
    page.on('pageerror', e => errors.push(e.message));
    await page.goto(`http://localhost:${port}/${path.basename(folder)}/`);
    const state = async () => page.evaluate(() => window.microengine?.state ?? 'loading');
    const until = async (want, ms) => { const end = Date.now() + ms; while (Date.now() < end) { if (want.includes(await state())) return true; await new Promise(r => setTimeout(r, 200)); } return false; };
    const ready = await until(['ready'], 60_000);
    if (ready) await page.click('#play');
    const playing = ready && await until(['playing'], 30_000);
    await new Promise(r => setTimeout(r, 2000));
    const title = await page.title();
    check('web: the page loads in Chrome and the game plays', playing && (await state()) === 'playing' && title === 'Sample Game' && errors.length === 0, `${await state()}; title ${title}; ${errors.join(' | ') || 'no page errors'}`);
  } finally {
    await browser.close();
    server.kill();
  }
}
