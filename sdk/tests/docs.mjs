#!/usr/bin/env node
// The documentation's code against the published SDK: every sample the site quotes (the Gem Garden tutorial's four
// checkpoints, the focused examples, Hello Engine) is copied to a folder outside this repository and built with nothing
// but nuget.org, at the package version its project file names, then played headlessly with its input script.
//
//   node sdk/tests/docs.mjs [--keep]
//
// Inside the repository the same projects build against the engine's source (samples/Directory.Build.targets), and
// tests/Samples.Tests checks their behaviour; this script proves what a reader copies works with the packages they
// install. It also checks that the version the samples reference is the one the documentation presents as published.
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const samples = [
  ['samples/GemGarden/part1-scenes', 'GemGarden.csproj', 'scripts/tour.script'],
  ['samples/GemGarden/part2-hero', 'GemGarden.csproj', 'scripts/tour.script'],
  ['samples/GemGarden/part3-garden', 'GemGarden.csproj', 'scripts/tour.script'],
  ['samples/GemGarden/part4-complete', 'GemGarden.csproj', 'scripts/tour.script'],
  ['samples/Examples', 'Examples.csproj', 'scripts/tour.script'],
  ['samples/HelloEngine', 'HelloEngine.csproj', 'hello.script'],
];

let failed = 0;
const check = (name, ok, detail = '') => {
  console.log(`${ok ? 'PASS' : 'FAIL'} ${name}${detail ? `: ${detail}` : ''}`);
  if (!ok) failed++;
  return ok;
};
const tail = text => text.trim().split('\n').slice(-4).join(' | ');

// The version the documentation presents as published (Install the SDK), and the one each sample references.
const published = fs.readFileSync(path.join(repo, 'docs/guide/getting-microengine.md'), 'utf8').match(/current version is \*\*([^*]+)\*\*/)?.[1];
check('the documentation names the published version', Boolean(published), published);

const work = fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()), 'microengine-docs-'));
if (work.startsWith(repo)) throw new Error('the copies must live outside the repository');
console.log(`samples copied to ${work}, packages from nuget.org only`);
fs.writeFileSync(path.join(work, 'NuGet.config'), `<?xml version="1.0" encoding="utf-8"?>
<configuration><packageSources><clear /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>
`);
// Saves the samples write go to the copy's folder on Linux and macOS (XDG_DATA_HOME, HOME); Windows keeps its own
// local application data folder, where the samples' small save files (GemGarden, MicroEngineExamples) land.
const env = { ...process.env, NUGET_PACKAGES: path.join(work, 'nuget'), DOTNET_CLI_HOME: path.join(work, 'home'), XDG_DATA_HOME: path.join(work, 'data'), DOTNET_NOLOGO: '1', DOTNET_CLI_TELEMETRY_OPTOUT: '1' };
const run = (args, cwd) => {
  const r = spawnSync('dotnet', args, { cwd, env, encoding: 'utf8', maxBuffer: 256 * 1024 * 1024 });
  return { code: r.status, out: `${r.stdout ?? ''}${r.stderr ?? ''}` };
};

try {
  for (const [folder, project, script] of samples) {
    const name = folder.replace('samples/', '');
    const target = path.join(work, name.replaceAll('/', '-'));
    fs.cpSync(path.join(repo, folder), target, { recursive: true, filter: src => !/[\\/](bin|obj|dist)$/.test(src) });
    const csproj = fs.readFileSync(path.join(target, project), 'utf8');
    const version = csproj.match(/<PackageReference Include="MicroEngine\.Desktop" Version="([^"]+)"/)?.[1];
    if (!check(`${name}: references MicroEngine.Desktop ${published}`, version === published && !/ProjectReference/.test(csproj), version ?? 'no package reference')) continue;
    let r = run(['build', project, '-c', 'Release', '-v', 'q', '-nologo'], target);
    if (!check(`${name}: builds against nuget.org`, r.code === 0, r.code === 0 ? '' : tail(r.out))) continue;
    r = run(['run', '--project', project, '-c', 'Release', '--no-build', '--', '--headless', '--script', path.join(target, script)], target);
    check(`${name}: plays its input script headlessly`, r.code === 0 && /done: \d+ updates/.test(r.out), tail(r.out));
  }
} finally {
  if (process.argv.includes('--keep')) console.log(`kept ${work}`);
  else fs.rmSync(work, { recursive: true, force: true });
}
console.log(failed ? `${failed} check(s) failed` : 'all checks passed');
process.exit(failed ? 1 : 0);
