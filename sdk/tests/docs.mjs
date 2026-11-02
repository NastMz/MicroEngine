#!/usr/bin/env node
// The documentation's code against the published SDK: every sample the site quotes (the Gem Garden tutorial's four
// checkpoints, the focused examples, Hello Engine) is copied to a folder outside this repository and built with nothing
// but nuget.org, at the package version its project file names, then played headlessly with its input script.
//
//   node sdk/tests/docs.mjs [--keep]
//
// Inside the repository the same projects build against the engine's source (samples/Directory.Build.targets), and
// tests/Samples.Tests checks their behaviour; this script proves what a reader copies works with the packages they
// install. The version is the documented one (sdk/version.json, sdk/version.mjs check keeps the pages in step). Before
// it is published (a release pull request, or main between merging a release and publishing it) the samples are
// built against the packages this commit packs, with nuget.org for everything else: what the release will publish.
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { documentedVersion, preparedVersion, publicState, isPublic } from '../version.mjs';

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

// The version the documentation presents (sdk/version.json), which each sample must reference.
const published = documentedVersion(repo);
const onNuget = isPublic(await publicState(published));
if (!onNuget && published !== preparedVersion(repo)) {
  check(`the documented version ${published} is on nuget.org or is the version this commit packs (${preparedVersion(repo)})`, false);
  process.exit(1);
}

const work = fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()), 'microengine-docs-'));
if (work.startsWith(repo)) throw new Error('the copies must live outside the repository');
let localFeed = '';
if (!onNuget) {
  // Not published yet: the packages of this commit, packed into the copy's folder (never a shared package cache).
  const feed = path.join(work, 'feed');
  const r = spawnSync('dotnet', ['pack', path.join(repo, 'MicroEngine.slnx'), '-c', 'Release', '-o', feed, '-v', 'q', '-nologo'], { encoding: 'utf8', maxBuffer: 256 * 1024 * 1024 });
  if (!check(`${published} is not on nuget.org yet: packs this commit's packages`, r.status === 0, r.status === 0 ? feed : tail(`${r.stdout}${r.stderr}`))) process.exit(1);
  localFeed = `<add key="this commit" value="${feed}" />`;
}
console.log(`samples copied to ${work}, packages from ${onNuget ? 'nuget.org only' : "this commit's packages and nuget.org"}`);
fs.writeFileSync(path.join(work, 'NuGet.config'), `<?xml version="1.0" encoding="utf-8"?>
<configuration><packageSources><clear />${localFeed}<add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>
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
    if (!check(`${name}: builds against ${onNuget ? 'nuget.org' : 'the packages to publish'}`, r.code === 0, r.code === 0 ? '' : tail(r.out))) continue;
    r = run(['run', '--project', project, '-c', 'Release', '--no-build', '--', '--headless', '--script', path.join(target, script)], target);
    check(`${name}: plays its input script headlessly`, r.code === 0 && /done: \d+ updates/.test(r.out), tail(r.out));
  }
} finally {
  if (process.argv.includes('--keep')) console.log(`kept ${work}`);
  else fs.rmSync(work, { recursive: true, force: true });
}
console.log(failed ? `${failed} check(s) failed` : 'all checks passed');
process.exit(failed ? 1 : 0);
