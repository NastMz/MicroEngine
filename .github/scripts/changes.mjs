#!/usr/bin/env node
// Decides which checks a change needs (docs/ci.md, "What runs for a change"). Each changed path maps to the
// checks that can catch a regression in it; a path no rule knows runs everything, so a new folder is never silently
// skipped. Demo packages are not decided here: their content keys (site/scripts/demos.mjs) already say which game
// must be rebuilt, and the workflow plays a rebuilt package in the browser whatever this planner says.
//
//   node .github/scripts/changes.mjs --base <sha> --head <sha> [--github-output]   checks for the files changed in between
//   node .github/scripts/changes.mjs --all [--github-output]                       every check (manual and scheduled runs)
//
// Checks (docs/ci.md, "What runs for a change"):
//   dotnet     headless .NET tests on Linux, the fast tier every later .NET job waits for
//   windows    the same tests on Windows: engine, backend and build changes (games are managed code Linux already ran)
//   native     SDL and raylib on Linux under Xvfb: native tests and scripted captures against their Linux references
//   browser    none | layout: every page at three widths | full: layout plus playing each demo; anything but none
//              also means the published site changed, so main deploys it
//   workflows  actionlint over every workflow
//   sdk        the SDK packages packed and checked, then used from outside the repository on Windows, Linux and macOS
//              (sdk.yml): every input of a package, the tool, the template or the external project test
//   macos      the headless .NET tests on macOS: informative (never a gate), and only started when the repository
//              uses free public runners (repository variable PUBLIC_RUNNERS=true); otherwise advisory
//   mobile     advisory only: printed as a recommendation to run platforms.yml or validate.yml; daily CI never
//              starts an Android emulator or an iOS simulator
//
// Scope (projects.mjs): the .NET project graph narrows `dotnet`, `windows` and `macos` to the test projects that
// depend on the changed files (output `tests`), and the native captures to the affected games (output `games`).
// Either is 'all' when a file is outside every project, so an unknown dependency never narrows a check.
import fs from 'node:fs';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { readGraph, affectedScope, scopeText } from './projects.mjs';

const levels = ['none', 'layout', 'full'];
export const everything = Object.freeze({ dotnet: true, windows: true, native: true, browser: 'full', workflows: true, sdk: true, macos: true, mobile: true });
const nothing = () => ({ dotnet: false, windows: false, native: false, browser: 'none', workflows: false, sdk: false, macos: false, mobile: false });

// First matching rule wins. `checks` lists what the path needs; 'all' means everything.
const build = /^(Directory\.Build\.(props|targets)|global\.json|NuGet\.config|MicroEngine\.slnx|\.editorconfig|\.gitattributes)$/;
export const rules = [
  { pattern: /^\.github\/(workflows\/(ci|checks|deploy-site|sdk)\.yml|scripts\/)/, checks: 'all', why: 'daily CI itself' },
  { pattern: build, checks: 'all', why: 'build settings' },
  { pattern: /^\.github\/workflows\/platforms\.yml$/, checks: ['workflows', 'mobile'], why: 'platform workflow' },
  { pattern: /^\.github\/workflows\//, checks: ['workflows'], why: 'manual workflow' },
  { pattern: /^platforms\//, checks: ['mobile'], why: 'mobile hosts' },
  // Publication freeze (freeze.mjs): lifting it on main deploys the site main accumulated meanwhile.
  { pattern: /^\.github\/phase-freeze$/, checks: ['layout'], why: 'publication freeze' },
  { pattern: /^\.github\//, checks: [], why: 'other repository settings' },
  { pattern: /^src\/MicroEngine\.Cli\/Hosts\/web\//, checks: ['full', 'sdk'], why: 'browser host (microengine package web)' },
  { pattern: /^src\/MicroEngine\.Cli\/Hosts\//, checks: ['mobile', 'sdk'], why: 'phone hosts (microengine package android, ios)' },
  { pattern: /^src\/MicroEngine\.Cli\//, checks: ['dotnet', 'windows', 'sdk'], why: 'microengine tool' },
  { pattern: /^templates\//, checks: ['dotnet', 'sdk'], why: 'project templates' },
  // The documented samples' check (docs.mjs, .NET Linux) and the version tool it reads; Plan runs the tool's tests.
  { pattern: /^sdk\/(version\.|tests\/(docs|version)\.)/, checks: ['dotnet'], why: 'documented SDK version and samples check' },
  { pattern: /^sdk\//, checks: ['sdk'], why: 'SDK checks' },
  { pattern: /^src\/MicroEngine\.Raylib\//, checks: ['dotnet', 'windows', 'native', 'sdk', 'macos', 'mobile'], why: 'raylib backend' },
  { pattern: /^src\/MicroEngine\.(Sdl|Desktop)\//, checks: ['dotnet', 'windows', 'native', 'sdk', 'macos', 'mobile'], why: 'SDL backend and desktop platforms' },
  { pattern: /^src\//, checks: ['dotnet', 'windows', 'native', 'sdk', 'macos'], why: 'engine' },
  { pattern: /^games\/LICENSE$/, checks: ['layout'], why: 'demo games licence (packaged: demo keys rebuild)' },
  { pattern: /^games\/.*\.md$/i, checks: ['layout'], why: 'published game page' },
  { pattern: /^games\/[^/]+\/screenshots\//, checks: ['layout'], why: 'screenshots on the site' },
  { pattern: /^games\//, checks: ['dotnet', 'native'], why: 'game' },
  { pattern: /^tests\/MicroEngine\.(Raylib|Desktop)\.Tests\//, checks: ['dotnet', 'windows', 'native', 'macos'], why: 'backend tests' },
  { pattern: /^tests\/MicroEngine\.Tests\//, checks: ['dotnet', 'windows', 'macos'], why: 'engine tests' },
  { pattern: /^(tests|samples)\//, checks: ['dotnet'], why: 'game tests and samples' },
  { pattern: /^web\/(toolchain\.env|setup\.sh|build-raylib-wasm\.sh)$/, checks: ['full', 'sdk'], why: 'web toolchain (MicroEngine.Raylib carries the web raylib)' },
  { pattern: /^web\/tests\/package(-lock)?\.json$/, checks: ['native', 'sdk'], why: 'browser driver (desktop captures, the SDK test plays its web package)' },
  { pattern: /^web\/tests\//, checks: ['native'], why: 'desktop capture scripts' },
  { pattern: /^web\/serve\.mjs$/, checks: ['full', 'sdk'], why: 'local web server (site checks, SDK test)' },
  { pattern: /^web\/.*\.md$/i, checks: ['layout'], why: 'published web page' },
  { pattern: /^(web|site)\//, checks: ['full'], why: 'web host and site' },
  { pattern: /^(odd|experiments)\//, checks: [], why: 'task records and experiments' },
  { pattern: /^THIRD-PARTY-NOTICES\.md$/, checks: ['layout', 'sdk'], why: 'third-party notices (shipped in the packages)' },
  { pattern: /\.md$/i, checks: ['layout'], why: 'documentation' },
  { pattern: /^LICENSE$/, checks: ['layout', 'sdk'], why: 'licence (shipped in the packages)' },
];

export function ruleFor(file) {
  return rules.find(rule => rule.pattern.test(file));
}

/** The checks needed by a list of changed paths (repository-relative, '/' separators). */
export function planChecks(files) {
  const checks = nothing();
  const raise = level => { if (levels.indexOf(level) > levels.indexOf(checks.browser)) checks.browser = level; };
  for (const file of files) {
    const rule = ruleFor(file);
    if (!rule || rule.checks === 'all') return { ...everything };
    for (const check of rule.checks) {
      if (check === 'layout' || check === 'full') raise(check);
      else checks[check] = true;
    }
  }
  return checks;
}

/** The test projects and games a change can affect, from the files whose rules need .NET or native checks. */
export function planScope(files, graph) {
  const relevant = files.filter(file => {
    const rule = ruleFor(file);
    return !rule || rule.checks === 'all' || rule.checks.includes('dotnet') || rule.checks.includes('native');
  });
  return affectedScope(graph, relevant);
}

function changedFiles(base, head) {
  const out = execFileSync('git', ['diff', '--name-only', '--no-renames', `${base}...${head}`], { encoding: 'utf8' });
  return out.split('\n').filter(Boolean);
}

function main(args) {
  const value = name => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : undefined; };
  const base = value('--base'), head = value('--head') ?? 'HEAD';
  let files = null, reason;
  if (args.includes('--all')) reason = 'full run requested';
  else if (!base || /^0+$/.test(base)) reason = 'no base commit to compare with';
  else {
    try { files = changedFiles(base, head); }
    catch (error) { reason = `cannot compare with ${base} (${error.message.split('\n')[0]})`; }
  }
  const checks = files ? planChecks(files) : { ...everything };
  const scope = files ? planScope(files, readGraph()) : { tests: 'all', games: 'all' };
  const lines = ['### Checks for this change', ''];
  if (files) {
    lines.push(`${files.length} changed file(s) since \`${base.slice(0, 12)}\`.`, '', '| Path | Rule |', '| --- | --- |');
    for (const file of files.slice(0, 50)) lines.push(`| \`${file}\` | ${ruleFor(file)?.why ?? 'unknown path: everything'} |`);
    if (files.length > 50) lines.push(`| … | ${files.length - 50} more |`);
  } else lines.push(`Everything runs: ${reason}.`);
  lines.push('', '| Check | Runs |', '| --- | --- |', ...Object.entries(checks).map(([name, on]) => `| ${name} | ${on === true ? 'yes' : on === false ? 'no' : on} |`), '');
  const describe = value => (value === 'all' ? 'all' : value.length ? value.map(v => `\`${v}\``).join(', ') : 'none');
  lines.push(`Project graph: .NET tests ${describe(scope.tests)}; native captures ${describe(scope.games)}.`, '');
  const advisory = [checks.macos && 'macOS', checks.mobile && 'Android and iOS'].filter(Boolean);
  if (advisory.length) lines.push(`Advisory: this change concerns ${advisory.join(', ')}. Daily CI runs macOS tests only on public runners (informative); run the platforms or validate workflow when it matters.`, '');
  console.log(lines.join('\n'));
  if (args.includes('--github-output')) {
    const output = [...Object.entries(checks).map(([name, on]) => `${name}=${on}`), `tests=${scopeText(scope.tests)}`, `games=${scopeText(scope.games)}`].join('\n') + '\n';
    if (process.env.GITHUB_OUTPUT) fs.appendFileSync(process.env.GITHUB_OUTPUT, output);
    else process.stdout.write(output);
  }
}

if (process.argv[1] && fileURLToPath(import.meta.url) === fs.realpathSync(process.argv[1])) main(process.argv.slice(2));
