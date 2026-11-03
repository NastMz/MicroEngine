#!/usr/bin/env node
// Checks the SDK packages themselves (docs/guide/sdk.md, "How the SDK is checked"), before anything installs them:
// one version, MIT metadata, a README, the dependencies each package must (and must not) have, the files that make
// it work, and no build-machine path or demo file inside.
//
//   node sdk/tests/packages.mjs <feed folder>
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import zlib from 'node:zlib';
import { fileURLToPath } from 'node:url';

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const feed = path.resolve(process.argv[2] ?? path.join(repo, 'dist', 'sdk-feed'));
const version = fs.readFileSync(path.join(repo, 'Directory.Build.props'), 'utf8').match(/<MicroEngineVersion>([^<]+)</)[1];

let failed = 0;
const check = (name, ok, detail = '') => {
  console.log(`${ok ? 'PASS' : 'FAIL'} ${name}${detail ? `: ${detail}` : ''}`);
  if (!ok) failed++;
};

/** The entries of a ZIP file (a .nupkg) as { name, data }, read with zlib only. */
function unzip(file) {
  const buf = fs.readFileSync(file);
  let end = buf.length - 22;
  while (end >= 0 && buf.readUInt32LE(end) !== 0x06054b50) end--;
  const count = buf.readUInt16LE(end + 10);
  let at = buf.readUInt32LE(end + 16);
  const entries = [];
  for (let i = 0; i < count; i++) {
    const method = buf.readUInt16LE(at + 10), size = buf.readUInt32LE(at + 20), nameLength = buf.readUInt16LE(at + 28);
    const extra = buf.readUInt16LE(at + 30), comment = buf.readUInt16LE(at + 32), local = buf.readUInt32LE(at + 42);
    const name = buf.toString('utf8', at + 46, at + 46 + nameLength);
    const start = local + 30 + buf.readUInt16LE(local + 26) + buf.readUInt16LE(local + 28);
    const raw = buf.subarray(start, start + size);
    entries.push({ name, data: method === 8 ? zlib.inflateRawSync(raw) : raw });
    at += 46 + nameLength + extra + comment;
  }
  return entries;
}

const expected = {
  // The kernel and the platform contracts are assemblies of their own but not packages: MicroEngine carries them and
  // their documentation.
  MicroEngine: { deps: ['StbImageSharp', 'FontStashSharp'], files: ['lib/net10.0/MicroEngine.dll', 'lib/net10.0/MicroEngine.Kernel.dll', 'lib/net10.0/MicroEngine.Kernel.xml', 'lib/net10.0/MicroEngine.Platform.dll', 'lib/net10.0/MicroEngine.Platform.xml', 'build/MicroEngine.targets', 'buildTransitive/MicroEngine.targets', 'buildTransitive/MicroEngine.props'] },
  'MicroEngine.Sdl': { deps: ['MicroEngine', 'ppy.SDL3-CS', 'NVorbis'], files: ['lib/net10.0/MicroEngine.Sdl.dll'] },
  'MicroEngine.Raylib': { deps: ['MicroEngine', 'Raylib-cs'], files: ['lib/net10.0/MicroEngine.Raylib.dll', 'native/browser-wasm/raylib.a', 'buildTransitive/MicroEngine.Raylib.props'] },
  'MicroEngine.Desktop': { deps: ['MicroEngine', 'MicroEngine.Sdl', 'MicroEngine.Raylib'], files: ['lib/net10.0/MicroEngine.Desktop.dll'] },
  'MicroEngine.Cli': { deps: [], files: ['tools/net10.0/any/MicroEngine.Cli.dll', 'tools/net10.0/any/Hosts/android/Host.csproj', 'tools/net10.0/any/Hosts/ios/Host.csproj', 'tools/net10.0/any/Hosts/web/Host.csproj', 'tools/net10.0/any/Notices/THIRD-PARTY-NOTICES.txt', 'tools/net10.0/any/Notices/microengine-LICENSE.txt', 'tools/net10.0/any/Notices/raylib-LICENSE.txt'] },
  'MicroEngine.Templates': { deps: [], files: ['content/MicroEngineGame/.template.config/template.json', 'content/MicroEngineGame/MyGame.csproj', 'content/MicroEngineGame/Content/hero.png'] },
};
// Paths of this machine and checkout that must never be inside a package.
const machine = [repo, repo.replace(/\\/g, '/'), os.homedir(), os.homedir().replace(/\\/g, '/')].filter(p => p.length > 3);

for (const [id, want] of Object.entries(expected)) {
  const file = path.join(feed, `${id}.${version}.nupkg`);
  if (!fs.existsSync(file)) { check(`${id} ${version}`, false, `${file} is missing`); continue; }
  const entries = unzip(file);
  const names = new Set(entries.map(e => e.name));
  const nuspec = entries.find(e => e.name.endsWith('.nuspec')).data.toString('utf8');
  const deps = [...nuspec.matchAll(/<dependency id="([^"]+)" version="([^"]+)"/g)].map(m => ({ id: m[1], version: m[2] }));
  check(`${id}: version ${version}, MIT, README, repository`, nuspec.includes(`<version>${version}</version>`) && /<license type="expression">MIT<\/license>/.test(nuspec)
    && /<readme>README.md<\/readme>/.test(nuspec) && names.has('README.md') && /<repository type="git" url="https:\/\/github.com\/NastMz\/MicroEngine"/.test(nuspec));
  const ids = [...new Set(deps.map(d => d.id))].sort();
  check(`${id}: depends on ${want.deps.join(', ') || 'nothing'}`, JSON.stringify(ids) === JSON.stringify([...want.deps].sort()), ids.join(', ') || 'none');
  const own = deps.filter(d => d.id.startsWith('MicroEngine'));
  check(`${id}: engine dependencies at the same version`, own.every(d => d.version === version || d.version === `[${version}, )`), own.map(d => `${d.id} ${d.version}`).join(', '));
  const missing = want.files.filter(f => !names.has(f));
  check(`${id}: the files that make it work`, missing.length === 0, missing.join(', '));
  const demo = [...names].filter(n => /(^|\/)(games|MeadowRun|CourtyardKey|TrailDuel|NeonRift)(\/|\.)/.test(n));
  check(`${id}: no demo game file`, demo.length === 0, demo.slice(0, 4).join(', '));
  const leaks = entries.filter(e => machine.some(p => e.data.includes(Buffer.from(p)))).map(e => e.name);
  // Paths are mapped away only in CI builds (ContinuousIntegrationBuild, src/Directory.Build.props): a local pack checks
  // the same only with -p:MicroEngineRelease=true, as the SDK workflow packs.
  check(`${id}: no build-machine path`, leaks.length === 0,
    leaks.length ? `${leaks.slice(0, 4).join(', ')} (a local pack needs -p:MicroEngineRelease=true, as in CI)` : '');
  if (id !== 'MicroEngine.Cli' && id !== 'MicroEngine.Templates') check(`${id}: symbols package`, fs.existsSync(path.join(feed, `${id}.${version}.snupkg`)));
  if (id === 'MicroEngine') {
    const symbols = path.join(feed, `${id}.${version}.snupkg`);
    const pdbs = fs.existsSync(symbols) ? unzip(symbols).map(e => e.name) : [];
    check(`${id}: symbols of the bundled kernel`, pdbs.includes('lib/net10.0/MicroEngine.Kernel.pdb'), pdbs.join(', '));
    check(`${id}: symbols of the bundled platform contracts`, pdbs.includes('lib/net10.0/MicroEngine.Platform.pdb'), pdbs.join(', '));
  }
}
const templates = path.join(feed, `MicroEngine.Templates.${version}.nupkg`);
const template = !fs.existsSync(templates) ? "" : unzip(templates).find(e => e.name === 'content/MicroEngineGame/MyGame.csproj')?.data.toString('utf8') ?? '';
check('MicroEngine.Templates: the game references this SDK version', template.includes(`<PackageReference Include="MicroEngine.Desktop" Version="${version}" />`));
const tool = unzip(path.join(feed, `MicroEngine.Cli.${version}.nupkg`)).find(e => e.name.endsWith('DotnetToolSettings.xml'))?.data.toString('utf8') ?? '';
check('MicroEngine.Cli: installs the command microengine', /<Command Name="microengine"/.test(tool));
console.log(failed ? `${failed} check(s) failed` : 'all package checks passed');
process.exit(failed ? 1 : 0);
