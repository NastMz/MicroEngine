#!/usr/bin/env node
// Importing it runs nothing: the rule below (publishedBuildRequired) is tested in sdk/tests/version.test.mjs.
// The documentation's code against the SDK packages: every sample the site quotes (the Gem Garden tutorial's four
// checkpoints, the focused examples, Hello Engine) is copied to a folder outside this repository and built with
// nothing but packages, then played headlessly with its input script.
//
//   node sdk/tests/docs.mjs [--keep]
//
// Two builds of each copy:
//
// - Required: against the SDK built from source. This commit's packages are packed into the copy's folder, the copy
//   references them at the version this commit packs, and NuGet takes every MicroEngine package from that folder only
//   (package source mapping) and everything else from nuget.org. It proves what a reader copies works with the
//   packages the next release will publish, including changes to public APIs that the published SDK does not have yet.
// - The published SDK: when the documented version (sdk/version.json) is on nuget.org, the same copies are built at
//   that version from nuget.org alone. It is a report (printed, never failing the check) while the release notes list
//   SDK changes under "## Next release": the documentation presents the next release (ADR 0048), so the samples may
//   use APIs the published version does not have yet. It is also a report while the documented version is
//   `reportOnlyVersion` (incompatible changes made after it was published). Otherwise it is required: with nothing
//   pending, every sample must build against the published SDK.
//
// Inside the repository the same projects build against the engine's source (samples/Directory.Build.targets), and
// tests/Samples.Tests checks their behaviour. The documented version must be on nuget.org or be the version this commit
// packs (a release pull request, or main between merging a release and publishing it).
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { documentedVersion, preparedVersion, publicState, isPublic, pendingChanges } from '../version.mjs';

// The last SDK version published before stage 5's breaking renames (the synthesizer's namespace, the tile mover's
// result type), which the samples already use. It is the anchor of the report-only rule, not a documentation version
// reference: never rewrite it when releasing. While sdk/version.json documents this version, the published-SDK build
// is a report; once sdk/version.json documents another version (the next release), that build is required again,
// automatically, with no change here.
export const reportOnlyVersion = '0.4.0-alpha.2';

/**
 * Whether the build of the samples from nuget.org at the `documented` version fails the check (false: a report only).
 * It is a report while `pending` (the release notes' "## Next release" content, `pendingChanges`) lists SDK changes:
 * the documentation presents the next release (ADR 0048), so the samples may use APIs the published version does not
 * have yet, and the build against this commit's packages is the one that must pass. With nothing pending it is
 * required, except while the documented version is `reportOnlyVersion`.
 */
export const publishedBuildRequired = (documented, pending = null) => !pending && documented !== reportOnlyVersion;

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const samples = [
  ['samples/GemGarden/part1-scenes', 'GemGarden.csproj', 'scripts/tour.script'],
  ['samples/GemGarden/part2-hero', 'GemGarden.csproj', 'scripts/tour.script'],
  ['samples/GemGarden/part3-garden', 'GemGarden.csproj', 'scripts/tour.script'],
  ['samples/GemGarden/part4-complete', 'GemGarden.csproj', 'scripts/tour.script'],
  ['samples/Examples', 'Examples.csproj', 'scripts/tour.script'],
  ['samples/HelloEngine', 'HelloEngine.csproj', 'hello.script'],
  ['samples/TiledLevel', 'TiledLevel.csproj', 'scripts/tour.script'],
];

const tail = text => text.trim().split('\n').slice(-4).join(' | ');

async function main() {
  let failed = 0;
  let reported = 0;
  // A check that fails the run, or (required = false) one whose failure is only reported.
  const check = (name, ok, detail = '', required = true) => {
    const label = ok ? 'PASS' : required ? 'FAIL' : 'REPORT';
    console.log(`${label} ${name}${detail ? `: ${detail}` : ''}`);
    if (!ok) { if (required) failed++; else reported++; }
    return ok;
  };
  // The version the documentation presents (sdk/version.json), which each sample must reference, and the version this
  // commit packs.
  const documented = documentedVersion(repo);
  const prepared = preparedVersion(repo);
  const onNuget = isPublic(await publicState(documented));
  if (!onNuget && documented !== prepared) {
    check(`the documented version ${documented} is on nuget.org or is the version this commit packs (${prepared})`, false);
    process.exit(1);
  }

  const work = fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()), 'microengine-docs-'));
  if (work.startsWith(repo)) throw new Error('the copies must live outside the repository');
  const nugetOrg = '<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />';

  // Each build gets its own folder, NuGet configuration and package cache, so the source-built packages and the published
  // ones (which may share a version number) never meet.
  function environment(folder) {
    // Saves the samples write go to the copy's folder on Linux and macOS (XDG_DATA_HOME, HOME); Windows keeps its own
    // local application data folder, where the samples' small save files (GemGarden, MicroEngineExamples) land.
    return { ...process.env, NUGET_PACKAGES: path.join(folder, 'nuget'), DOTNET_CLI_HOME: path.join(folder, 'home'), XDG_DATA_HOME: path.join(folder, 'data'), DOTNET_NOLOGO: '1', DOTNET_CLI_TELEMETRY_OPTOUT: '1' };
  }
  const run = (args, cwd, env) => {
    const r = spawnSync('dotnet', args, { cwd, env, encoding: 'utf8', maxBuffer: 256 * 1024 * 1024 });
    return { code: r.status, out: `${r.stdout ?? ''}${r.stderr ?? ''}` };
  };

  // Copies every sample into `folder`, points its MicroEngine.Desktop reference at `version`, builds it and plays its
  // script. `what` names the build in each line; `required` = false only reports failures.
  function buildAll({ folder, nugetConfig, version, what, required }) {
    fs.mkdirSync(folder, { recursive: true });
    fs.writeFileSync(path.join(folder, 'NuGet.config'), nugetConfig);
    const env = environment(folder);
    for (const [sample, project, script] of samples) {
      const name = sample.replace('samples/', '');
      const target = path.join(folder, name.replaceAll('/', '-'));
      fs.cpSync(path.join(repo, sample), target, { recursive: true, filter: src => !/[\\/](bin|obj|dist)$/.test(src) });
      const file = path.join(target, project);
      const csproj = fs.readFileSync(file, 'utf8');
      const referenced = csproj.match(/<PackageReference Include="MicroEngine\.Desktop" Version="([^"]+)"/)?.[1];
      // The repository's samples reference the documented version (what a reader installs).
      if (!check(`${name}: references MicroEngine.Desktop ${documented}`, referenced === documented && !/ProjectReference/.test(csproj), referenced ?? 'no package reference')) continue;
      // Every SDK package it references (MicroEngine.Desktop, MicroEngine.Text.Shaping for the examples, MicroEngine.Tiled for the Tiled level) moves to the version under test.
      if (version !== referenced) fs.writeFileSync(file, csproj.replaceAll(new RegExp(`(<PackageReference Include="MicroEngine(?:\\.[A-Za-z]+)*" Version=")${referenced.replaceAll('.', '\\.')}"`, 'g'), `$1${version}"`));
      let r = run(['build', project, '-c', 'Release', '-v', 'q', '-nologo'], target, env);
      if (!check(`${name}: builds against ${what}`, r.code === 0, r.code === 0 ? '' : tail(r.out), required)) continue;
      r = run(['run', '--project', project, '-c', 'Release', '--no-build', '--', '--headless', '--script', path.join(target, script)], target, env);
      check(`${name}: plays its input script headlessly (${what})`, r.code === 0 && /done: \d+ updates/.test(r.out), tail(r.out), required);
    }
  }

  function buildBoth() {
    // 1. The SDK built from source: this commit's packages, packed into the copy's folder (never a shared package cache).
    const source = path.join(work, 'source');
    const feed = path.join(source, 'feed');
    const pack = spawnSync('dotnet', ['pack', path.join(repo, 'MicroEngine.slnx'), '-c', 'Release', '-o', feed, '-v', 'q', '-nologo'], { encoding: 'utf8', maxBuffer: 256 * 1024 * 1024 });
    if (!check(`packs this commit's packages (${prepared})`, pack.status === 0, pack.status === 0 ? feed : tail(`${pack.stdout}${pack.stderr}`))) return;
    console.log(`samples copied to ${work}`);
    buildAll({
      folder: source,
      version: prepared,
      what: `the SDK built from source (${prepared})`,
      required: true,
      nugetConfig: `<?xml version="1.0" encoding="utf-8"?>
  <configuration>
    <packageSources><clear /><add key="this commit" value="${feed}" />${nugetOrg}</packageSources>
    <packageSourceMapping>
      <packageSource key="this commit"><package pattern="MicroEngine" /><package pattern="MicroEngine.*" /></packageSource>
      <packageSource key="nuget.org"><package pattern="*" /></packageSource>
    </packageSourceMapping>
  </configuration>
  `,
    });

    // 2. The published SDK, when the documented version is on nuget.org.
    if (onNuget) {
      const pending = pendingChanges(repo);
      const reportOnly = !publishedBuildRequired(documented, pending);
      if (reportOnly) console.log(pending
        ? `the release notes list SDK changes for the next release, which the samples may use: the build against the published SDK ${documented} below is a report and does not fail this check`
        : `the published SDK ${documented} predates APIs the samples use: its build below is a report and does not fail this check`);
      buildAll({
        folder: path.join(work, 'published'),
        version: documented,
        what: `nuget.org (${documented})`,
        required: !reportOnly,
        nugetConfig: `<?xml version="1.0" encoding="utf-8"?>
  <configuration><packageSources><clear />${nugetOrg}</packageSources></configuration>
  `,
      });
    } else {
      console.log(`${documented} is not on nuget.org yet: the SDK built from source is the version to publish`);
    }
  }

  try {
    buildBoth();
  } finally {
    if (process.argv.includes('--keep')) console.log(`kept ${work}`);
    else fs.rmSync(work, { recursive: true, force: true });
  }
  if (reported) console.log(`${reported} published-SDK check(s) reported, not required`);
  console.log(failed ? `${failed} check(s) failed` : 'all required checks passed');
  process.exit(failed ? 1 : 0);
}

// Run as a script (node sdk/tests/docs.mjs), not when a test imports the rule above.
if (process.argv[1] && fs.realpathSync(process.argv[1]) === fs.realpathSync(fileURLToPath(import.meta.url))) await main();
