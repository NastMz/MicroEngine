#!/usr/bin/env node
// SDK versions (docs/releasing.md, "Versions"): what each one means, where it lives, and the documentation references
// that must follow the documented one.
//
//   prepared    MicroEngineVersion in Directory.Build.props: the version the code on this commit packs as
//   documented  sdk/version.json: the version the documentation, the samples and the install commands present
//   public      what nuget.org serves (immutable once pushed); the production site deploys only when the documented
//               version is public, so between merging a release and publishing it production keeps its deployment
//
//   node sdk/version.mjs status [--public] [--json]   the three versions; --public asks nuget.org
//   node sdk/version.mjs check                        every active reference names the documented version
//   node sdk/version.mjs release <version>            prepares a release: prepared = documented = <version>, and every
//                                                     active reference rewritten (never a release or migration note)
//   node sdk/version.mjs wait-public <version> [--timeout <seconds>]   until nuget.org serves all six packages
//   node sdk/version.mjs packages                     the package ids, in push order
//
// Active references are found by pattern (`references` below) in the documentation, the samples' project files and
// the contributor entry point; historical files (release and migration notes, decisions, evaluations, the generated
// API reference) are never read. In an active Markdown file, a paragraph after `<!-- sdk-version: history -->` is
// history too ("from 0.4.0-alpha.2 every change waits"). `check` also fails on any mention of a known SDK version (a
// release-notes heading) that is neither an active reference nor history, so a new page cannot hard-code a version
// that the next release would leave stale.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');

/** The six SDK packages, engine first: the tool and the template must never point at a version nuget.org lacks. */
export const packageIds = ['MicroEngine', 'MicroEngine.Sdl', 'MicroEngine.Raylib', 'MicroEngine.Desktop', 'MicroEngine.Cli', 'MicroEngine.Templates'];

const semver = '\\d+\\.\\d+\\.\\d+(?:-[0-9A-Za-z]+(?:\\.[0-9A-Za-z]+)*)?';
const isVersion = text => new RegExp(`^${semver}$`).test(text);

// Each pattern captures the version in one or more groups; `{V}` stands for a version.
const pattern = text => new RegExp(text.replaceAll('{V}', `(${semver})`), 'gd');
export const references = [
  { name: 'package reference', regex: pattern('Include="MicroEngine(?:\\.[A-Za-z]+)*" Version="{V}"') },
  { name: 'install command', regex: pattern('MicroEngine\\.[A-Za-z]+(?:::| --version ){V}') },
  { name: 'current version', regex: pattern('(?:current version is|the published SDK,) \\*\\*{V}\\*\\*') },
  { name: 'SDK title', regex: pattern('MicroEngine SDK {V}') },
  { name: 'tool output', regex: pattern("(?:prints |the tool's version \\()`{V}`|Engine +MicroEngine {V},") },
  { name: 'update instruction', regex: pattern('Replace `{V}` with') },
];

const historical = /^docs\/(guide\/release-notes\.md|migrations\.md|decisions\/|evaluations\/|api\/)/;
const scanned = file => (/^(AGENTS|README|CONTRIBUTING)\.md$/.test(file) || /^docs\/.*\.md$/.test(file) || /^samples\/.*\.csproj$/.test(file))
  && !historical.test(file);
const historyMarker = '<!-- sdk-version: history -->';

function fail(message) { throw new Error(message); }
const read = (root, file) => fs.readFileSync(path.join(root, file), 'utf8');

/** Repository-relative files (with '/') whose references follow the documented version. */
export function activeFiles(root = repoRoot) {
  const out = [];
  const walk = dir => {
    for (const entry of fs.readdirSync(path.join(root, dir), { withFileTypes: true })) {
      const file = dir ? `${dir}/${entry.name}` : entry.name;
      if (entry.isDirectory()) { if (!/^(\.|node_modules$|bin$|obj$|dist$)/.test(entry.name)) walk(file); }
      else if (scanned(file)) out.push(file);
    }
  };
  for (const top of ['', 'docs', 'samples']) {
    if (!fs.existsSync(path.join(root, top))) continue;
    if (top) walk(top);
    else for (const name of fs.readdirSync(root)) if (scanned(name)) out.push(name);
  }
  return out.sort();
}

export function preparedVersion(root = repoRoot) {
  return read(root, 'Directory.Build.props').match(/<MicroEngineVersion>([^<]+)<\/MicroEngineVersion>/)?.[1] ?? fail('Directory.Build.props has no MicroEngineVersion.');
}

export function documentedVersion(root = repoRoot) {
  const version = JSON.parse(read(root, 'sdk/version.json')).documented;
  return isVersion(version ?? '') ? version : fail(`sdk/version.json: "${version}" is not a version.`);
}

/** Every SDK version with a section in the release notes (`## <version>`). */
export function releasedVersions(root = repoRoot) {
  return [...read(root, 'docs/guide/release-notes.md').matchAll(new RegExp(`^## (${semver})\\s*$`, 'gm'))].map(m => m[1]);
}

/** Character ranges of the paragraphs marked as history in a Markdown text. */
function historyRanges(text) {
  const ranges = [];
  let at = text.indexOf(historyMarker);
  while (at >= 0) {
    const start = at + historyMarker.length;
    const end = text.slice(start).search(/\n[ \t\r]*\n/); // a blank line; CRLF in a Windows checkout
    ranges.push([start, end < 0 ? text.length : start + end + 1]);
    at = text.indexOf(historyMarker, start);
  }
  return ranges;
}

/** Active references in one text: { name, version, start, end, line }. */
export function findReferences(text) {
  const found = [];
  for (const { name, regex } of references) {
    for (const match of text.matchAll(regex)) {
      for (let g = 1; g < match.length; g++) {
        if (match[g] === undefined) continue;
        const [start, end] = match.indices[g];
        found.push({ name, version: match[g], start, end, line: text.slice(0, start).split('\n').length });
      }
    }
  }
  return found.sort((a, b) => a.start - b.start);
}

/** Problems that would leave the documentation out of step with the documented version. */
export function check(root = repoRoot) {
  const problems = [];
  const documented = documentedVersion(root), prepared = preparedVersion(root), released = releasedVersions(root);
  if (!released.includes(documented)) problems.push(`docs/guide/release-notes.md has no "## ${documented}" section for the documented version.`);
  const known = [...new Set([...released, documented, prepared])];
  for (const file of activeFiles(root)) {
    const text = read(root, file);
    const refs = findReferences(text);
    const history = file.endsWith('.md') ? historyRanges(text) : [];
    const inside = (at, ranges) => ranges.some(([s, e]) => at >= s && at < e);
    for (const ref of refs) {
      if (inside(ref.start, history)) continue;
      if (ref.version !== documented) problems.push(`${file}:${ref.line}: ${ref.name} names ${ref.version}, the documented version is ${documented} (node sdk/version.mjs release ${documented}).`);
    }
    for (const version of known) {
      const mention = new RegExp(`(?<![0-9A-Za-z.-])${version.replaceAll('.', '\\.')}(?![0-9A-Za-z]|\\.[0-9A-Za-z])`, 'g');
      for (const m of text.matchAll(mention)) {
        if (refs.some(r => m.index >= r.start && m.index < r.end) || inside(m.index, history)) continue;
        const line = text.slice(0, m.index).split('\n').length;
        problems.push(`${file}:${line}: ${version} is neither an active reference (sdk/version.mjs, references) nor marked "${historyMarker}".`);
      }
    }
  }
  return problems;
}

/** Sets prepared and documented to `version` and rewrites every active reference; returns the files it changed. */
export function release(version, root = repoRoot) {
  if (!isVersion(version ?? '')) fail(`"${version}" is not a version (0.4.0-alpha.3, 1.0.0).`);
  const changed = [];
  const write = (file, text) => { if (text !== read(root, file)) { fs.writeFileSync(path.join(root, file), text); changed.push(file); } };
  write('Directory.Build.props', read(root, 'Directory.Build.props').replace(/<MicroEngineVersion>[^<]+<\/MicroEngineVersion>/, `<MicroEngineVersion>${version}</MicroEngineVersion>`));
  const json = JSON.parse(read(root, 'sdk/version.json'));
  write('sdk/version.json', `${JSON.stringify({ ...json, documented: version }, null, 2)}\n`);
  for (const file of activeFiles(root)) {
    const text = read(root, file);
    const history = file.endsWith('.md') ? historyRanges(text) : [];
    let out = '', at = 0;
    for (const ref of findReferences(text)) {
      if (history.some(([s, e]) => ref.start >= s && ref.start < e)) continue;
      out += text.slice(at, ref.start) + version;
      at = ref.end;
    }
    write(file, out + text.slice(at));
  }
  return changed;
}

/** Which packages nuget.org serves at `version`: { [id]: boolean }. Throws when nuget.org cannot be asked. */
export async function publicState(version, { fetchJson = defaultFetchJson } = {}) {
  const state = {};
  for (const id of packageIds) {
    const index = await fetchJson(`https://api.nuget.org/v3-flatcontainer/${id.toLowerCase()}/index.json`);
    state[id] = Boolean(index?.versions?.includes(version.toLowerCase()));
  }
  return state;
}

async function defaultFetchJson(url) {
  const response = await fetch(url, { headers: { 'Cache-Control': 'no-cache' } });
  if (response.status === 404) return null;
  if (!response.ok) throw new Error(`${url}: HTTP ${response.status}`);
  return response.json();
}

export const isPublic = state => Object.values(state).every(Boolean);

/** Waits until nuget.org serves every package at `version`; returns the last state. */
export async function waitPublic(version, { timeoutSeconds = 1800, intervalSeconds = 30, fetchJson, sleep = s => new Promise(r => setTimeout(r, s * 1000)), log = () => {} } = {}) {
  const deadline = Date.now() + timeoutSeconds * 1000;
  for (;;) {
    const state = await publicState(version, { fetchJson }).catch(error => { log(`nuget.org: ${error.message}`); return null; });
    if (state && isPublic(state)) return state;
    if (Date.now() >= deadline) fail(`nuget.org does not serve ${state ? packageIds.filter(id => !state[id]).join(', ') : 'the packages'} at ${version} after ${timeoutSeconds} s.`);
    if (state) log(`waiting for ${packageIds.filter(id => !state[id]).join(', ')} ${version}`);
    await sleep(intervalSeconds);
  }
}

async function main([command, ...args]) {
  const value = name => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : undefined; };
  if (command === 'status') {
    const status = { prepared: preparedVersion(), documented: documentedVersion() };
    if (args.includes('--public')) {
      const state = await publicState(status.documented);
      status.documentedPublic = isPublic(state);
      status.packages = state;
    }
    if (args.includes('--json')) console.log(JSON.stringify(status));
    else for (const [k, v] of Object.entries(status)) console.log(`${k}: ${typeof v === 'object' ? JSON.stringify(v) : v}`);
  } else if (command === 'check') {
    const problems = check();
    for (const p of problems) console.log(`FAIL ${p}`);
    console.log(problems.length ? `${problems.length} problem(s)` : `every active reference names ${documentedVersion()}`);
    process.exitCode = problems.length ? 1 : 0;
  } else if (command === 'release') {
    const changed = release(args[0]);
    for (const file of changed) console.log(`updated ${file}`);
    const problems = check();
    for (const p of problems) console.log(`FAIL ${p}`);
    if (problems.length) process.exitCode = 1;
  } else if (command === 'wait-public') {
    const timeout = Number(value('--timeout') ?? 1800);
    await waitPublic(args[0], { timeoutSeconds: timeout, log: message => console.log(message) });
    console.log(`nuget.org serves every package at ${args[0]}`);
  } else if (command === 'packages') {
    console.log(packageIds.join(' '));
  } else {
    console.log('usage: version.mjs status [--public] [--json] | check | release <version> | wait-public <version> [--timeout s] | packages');
    process.exitCode = command ? 1 : 0;
  }
}

if (process.argv[1] && fileURLToPath(import.meta.url) === fs.realpathSync(process.argv[1])) {
  main(process.argv.slice(2)).catch(error => { console.error(`error: ${error.message}`); process.exit(2); });
}
