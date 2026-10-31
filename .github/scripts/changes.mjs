#!/usr/bin/env node
// Decides which checks a change needs (docs/deployment.md, "What runs for a change"). Each changed path maps to the
// checks that can catch a regression in it; a path no rule knows runs everything, so a new folder is never silently
// skipped. Demo packages are not decided here: their content keys (site/scripts/demos.mjs) already say which game
// must be rebuilt, and the workflow plays a rebuilt package in the browser whatever this planner says.
//
//   node .github/scripts/changes.mjs --base <sha> --head <sha> [--github-output]   checks for the files changed in between
//   node .github/scripts/changes.mjs --all [--github-output]                       every check (manual and scheduled runs)
//
// Checks: dotnet (headless .NET tests: Linux first, then Windows), macos (the same on macOS, not blocking), native
// (raylib on Linux under Xvfb: native tests and scripted captures), browser (none | layout: every page at three widths
// | full: layout plus playing each demo), mobile (the experimental Android and iOS hosts, platforms.yml only).
import fs from 'node:fs';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const levels = ['none', 'layout', 'full'];
export const everything = Object.freeze({ dotnet: true, macos: true, native: true, browser: 'full', mobile: true });
const nothing = () => ({ dotnet: false, macos: false, native: false, browser: 'none', mobile: false });

// First matching rule wins. `checks` lists what the path needs; 'all' means everything.
const build = /^(Directory\.Build\.(props|targets)|global\.json|NuGet\.config|MicroEngine\.slnx|\.editorconfig|\.gitattributes)$/;
export const rules = [
  { pattern: /^\.github\/(workflows\/pipeline\.yml|scripts\/)/, checks: 'all', why: 'the pipeline itself' },
  { pattern: build, checks: 'all', why: 'build settings' },
  { pattern: /^\.github\/workflows\/platforms\.yml$|^platforms\//, checks: ['mobile'], why: 'mobile hosts' },
  { pattern: /^\.github\//, checks: [], why: 'other repository settings' },
  { pattern: /^src\/MicroEngine\.Raylib\//, checks: ['dotnet', 'macos', 'native', 'mobile'], why: 'raylib backend' },
  { pattern: /^src\//, checks: ['dotnet', 'macos', 'native'], why: 'engine' },
  { pattern: /^games\/.*\.md$/i, checks: ['layout'], why: 'published game page' },
  { pattern: /^games\//, checks: ['dotnet', 'native'], why: 'game' },
  { pattern: /^tests\/MicroEngine\.Raylib\.Tests\//, checks: ['dotnet', 'macos', 'native'], why: 'backend tests' },
  { pattern: /^tests\/MicroEngine\.Tests\//, checks: ['dotnet', 'macos'], why: 'engine tests' },
  { pattern: /^(tests|samples)\//, checks: ['dotnet'], why: 'game tests and samples' },
  { pattern: /^web\/tests\//, checks: ['native'], why: 'desktop capture scripts' },
  { pattern: /^web\/.*\.md$/i, checks: ['layout'], why: 'published web page' },
  { pattern: /^(web|site)\//, checks: ['full'], why: 'web host and site' },
  { pattern: /^(odd|experiments)\//, checks: [], why: 'task records and experiments' },
  { pattern: /\.md$/i, checks: ['layout'], why: 'documentation' },
  { pattern: /^LICENSE$/, checks: ['layout'], why: 'licence' },
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
  const lines = ['### Checks for this change', ''];
  if (files) {
    lines.push(`${files.length} changed file(s) since \`${base.slice(0, 12)}\`.`, '', '| Path | Rule |', '| --- | --- |');
    for (const file of files.slice(0, 50)) lines.push(`| \`${file}\` | ${ruleFor(file)?.why ?? 'unknown path: everything'} |`);
    if (files.length > 50) lines.push(`| … | ${files.length - 50} more |`);
  } else lines.push(`Everything runs: ${reason}.`);
  lines.push('', '| Check | Runs |', '| --- | --- |', ...Object.entries(checks).map(([name, on]) => `| ${name} | ${on === true ? 'yes' : on === false ? 'no' : on} |`), '');
  console.log(lines.join('\n'));
  if (args.includes('--github-output')) {
    const output = Object.entries(checks).map(([name, on]) => `${name}=${on}`).join('\n') + '\n';
    if (process.env.GITHUB_OUTPUT) fs.appendFileSync(process.env.GITHUB_OUTPUT, output);
    else process.stdout.write(output);
  }
}

if (process.argv[1] && fileURLToPath(import.meta.url) === fs.realpathSync(process.argv[1])) main(process.argv.slice(2));
