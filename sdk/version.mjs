#!/usr/bin/env node
// SDK versions (docs/releasing.md, "Versions"): what each one means, where it lives, and the documentation references
// that must follow the documented one.
//
//   prepared    MicroEngineVersion in Directory.Build.props: the version the code on this commit packs as
//   documented  sdk/version.json: the version the documentation, the samples and the install commands present
//   public      what nuget.org serves (immutable once pushed); the production site deploys only when the documented
//               version is public, so between merging a release and publishing it production keeps its deployment
//   pending     the release notes' "## Next release" section has content: SDK changes merged since the documented
//               version. The documentation describes them as published, so production keeps its deployment until
//               the release that ships them (docs/releasing.md, "Documentation and releases")
//
//   node sdk/version.mjs status [--public] [--json]   the three versions and pending; --public asks nuget.org
//   node sdk/version.mjs check                        every active reference names the documented version
//   node sdk/version.mjs release <version>            prepares a release: prepared = documented = <version>, every
//                                                     "## Next release" section renamed to the version, and every
//                                                     active reference rewritten (never an older release or migration note)
//   node sdk/version.mjs wait-public <version> [--timeout <seconds>]   until nuget.org serves all six packages
//   node sdk/version.mjs packages                     the package ids, in push order
//   node sdk/version.mjs release-plan --sha <commit> [--event <event>]   what sdk-release.yml has left to do
//                                                     (nothing while .github/phase-freeze exists: publication freeze)
//
// Active references are found by pattern (`references` below) in the documentation, the samples' project files and
// the contributor entry point; historical files (release and migration notes, decisions, evaluations, the generated
// API reference) are never read. In an active Markdown file, a paragraph after `<!-- sdk-version: history -->` is
// history too ("from 0.4.0-alpha.2 every change waits"). `check` also fails on any mention of a known SDK version (a
// release-notes heading) that is neither an active reference nor history, so a new page cannot hard-code a version
// that the next release would leave stale.
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
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

// Publication freeze (docs/ci.md, "Publication freeze"): the same file .github/scripts/freeze.mjs reads (its tests
// check the two agree). Read here, not imported: some sdk-release jobs check out sdk/ alone.
export const freezeFile = '.github/phase-freeze';
/** Why publication is frozen at `root` (the freeze file's first line that is neither blank nor a comment), or null. */
export function freezeReason(root = repoRoot) {
  const file = path.join(root, freezeFile);
  if (!fs.existsSync(file)) return null;
  return fs.readFileSync(file, 'utf8').split(/\r?\n/).map(l => l.trim()).find(l => l && !l.startsWith('#')) ?? 'publication frozen';
}

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

/** The notes that hold a "## Next release" section: what merged on main since the documented version, written as published. */
export const notesFiles = ['docs/guide/release-notes.md', 'docs/migrations.md'];
const nextHeading = /^## Next release[ \t]*\r?$/m;

/** The body of a file's "## Next release" section (up to the next `## ` heading), or null when it has none. */
function nextSection(text) {
  const heading = text.match(nextHeading);
  if (!heading) return null;
  const start = heading.index + heading[0].length;
  const end = text.slice(start).search(/^## /m);
  return text.slice(start, end < 0 ? text.length : start + end);
}

/**
 * Whether main holds SDK changes nuget.org does not serve yet: the release notes' "## Next release" section has
 * content. The documentation already describes them as published, so production waits for the release that ships them.
 */
export function pendingChanges(root = repoRoot) {
  return Boolean(nextSection(read(root, 'docs/guide/release-notes.md'))?.trim());
}

/** The anchor a site heading gets for a version: 0.5.0-alpha.1 -> 050-alpha1. */
export const versionAnchor = version => version.toLowerCase().replace(/[^0-9a-z-]/g, '');

/** Character ranges of the paragraphs marked as history in a Markdown text: the marker alone on its line, above them. */
function historyRanges(text) {
  const ranges = [];
  for (const marker of text.matchAll(/^[ \t]*<!-- sdk-version: history -->[ \t]*\r?$/gm)) {
    const start = marker.index + marker[0].length;
    const end = text.slice(start).search(/\n[ \t\r]*\n/); // a blank line; CRLF in a Windows checkout
    ranges.push([start, end < 0 ? text.length : start + end + 1]);
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
  for (const file of notesFiles) {
    if (!fs.existsSync(path.join(root, file))) continue;
    const next = nextSection(read(root, file));
    if (next !== null && !next.trim()) problems.push(`${file}: the "## Next release" section is empty; remove the heading until something is written under it.`);
  }
  const nextMigration = fs.existsSync(path.join(root, 'docs/migrations.md')) && nextSection(read(root, 'docs/migrations.md'))?.trim();
  if (nextMigration && !pendingChanges(root)) problems.push('docs/migrations.md has a "## Next release" section but docs/guide/release-notes.md has none: list the change in the release notes too.');
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

/**
 * Sets prepared and documented to `version`, turns each "## Next release" section into "## <version>" (and links to
 * it into links to the version), and rewrites every active reference; returns the files it changed. Refuses a new
 * version with nothing under "## Next release" in the release notes: a release always ships something. Running it
 * again for the version just prepared changes nothing.
 */
export function release(version, root = repoRoot) {
  if (!isVersion(version ?? '')) fail(`"${version}" is not a version (0.4.0-alpha.3, 1.0.0).`);
  const pending = pendingChanges(root), known = releasedVersions(root).includes(version);
  if (pending && known) fail(`docs/guide/release-notes.md already has a "## ${version}" section; choose a new version for "## Next release".`);
  if (!pending && !known) fail(`Nothing to release: docs/guide/release-notes.md has no "## Next release" section with content. Every change that ships in the SDK is listed there.`);
  const changed = [];
  const write = (file, text) => { if (text !== read(root, file)) { fs.writeFileSync(path.join(root, file), text); changed.push(file); } };
  if (pending) {
    const anchor = versionAnchor(version);
    const relink = text => text.replace(/((?:release-notes|migrations)\.md|\()#next-release\b/g, `$1#${anchor}`);
    for (const file of notesFiles) {
      if (!fs.existsSync(path.join(root, file))) continue;
      write(file, relink(read(root, file)).replace(nextHeading, `## ${version}`));
    }
    for (const file of activeFiles(root).filter(f => f.endsWith('.md') && !notesFiles.includes(f))) write(file, relink(read(root, file)));
  }
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
  return [...new Set(changed)];
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

/**
 * What an sdk-release run has left to do (.github/workflows/sdk-release.yml, preflight). `state`:
 *   documented, prepared  the versions of the commit the run is for (`sha`)
 *   packages              { [id]: served by nuget.org at `documented` }
 *   release               the GitHub release sdk-v<documented>: null, or { draft, target } (target: the commit drafted)
 *   notes                 the release notes have a section for `documented`
 *   event                 'workflow_run' (after ci passed on main) or 'workflow_dispatch' (by hand)
 *   isAncestor(commit)    `commit` is in the history of `sha`
 *   frozen                why publication is frozen on `sha` (freezeReason), or null; when set, nothing else is read
 * Returns { action, lines }: frozen (publication freeze: nothing is checked, drafted, pushed or deployed), none (nothing to release), release (check, draft, publish), publish (finish the existing
 * draft: push what nuget.org lacks, publish the GitHub release), finish (verify nuget.org, deploy the site). Throws
 * when no run can continue without a person: the message says what to do.
 */
export function releasePlan({ documented, prepared, packages, release, notes, event, sha, isAncestor, frozen }) {
  if (frozen) {
    return { action: 'frozen', lines: [`Frozen: publication is frozen (${freezeFile}: ${frozen}). Nothing is checked, drafted, pushed to nuget.org or deployed, whatever the state of ${documented}. To release, delete ${freezeFile} in a pull request (docs/releasing.md, "Publication freeze").`] };
  }
  const onNuget = packageIds.filter(id => packages[id]).length, all = onNuget === packageIds.length;
  const shown = !release ? 'none' : release.draft ? `draft of ${release.target.slice(0, 12)}` : 'published';
  const lines = [`nuget.org has ${onNuget} of ${packageIds.length} packages; GitHub release sdk-v${documented}: ${shown}`];
  const done = (action, line) => ({ action, lines: [...lines, line] });
  if (documented !== prepared) {
    // MicroEngineVersion moved on for the next release while the documentation still presents a published one.
    if (!all) fail(`The documentation presents ${documented}, which nuget.org does not serve, but this commit packs ${prepared}: nothing can publish it. Run node sdk/version.mjs release <version> in a pull request.`);
    return done('none', `Nothing to release: ${documented} is published; this commit packs ${prepared}, which nothing documents yet.`);
  }
  if (!notes) fail(`docs/guide/release-notes.md has no "## ${documented}" section.`);
  if (release && !release.draft) {
    if (!all) fail(`sdk-v${documented} is published on GitHub but nuget.org lacks ${packageIds.filter(id => !packages[id]).join(', ')}: push them by hand from the release's files (docs/releasing.md, "Recovering").`);
    return event === 'workflow_run' ? done('none', `Nothing to release: ${documented} is published.`)
      : done('finish', 'Published: verifying nuget.org and deploying the site again.');
  }
  if (onNuget > 0 || (release?.draft && release.target === sha)) {
    // Publishing started, or this very commit is drafted: finish that draft. Its files were checked; never rebuilt.
    if (!release) fail(`nuget.org has ${onNuget} of ${packageIds.length} packages of ${documented} but there is no draft sdk-v${documented} to finish from: push the rest by hand (docs/releasing.md, "Recovering").`);
    if (!isAncestor(release.target)) fail(`The draft sdk-v${documented} was made from ${release.target}, which is not in the history of ${sha}.`);
    return done('publish', "Publishing the draft (the `nuget` environment's reviewers approve).");
  }
  // Nothing is public: check and draft this commit, replacing a draft of an earlier one (a fix merged after it).
  if (release) lines.push(`The draft of ${release.target.slice(0, 12)} is replaced by one of this commit.`);
  return done('release', 'Checking and drafting this commit, then publishing after approval.');
}

async function githubRelease(tag, env = process.env) {
  const base = `${env.GITHUB_API_URL ?? 'https://api.github.com'}/repos/${env.GITHUB_REPOSITORY}`;
  // Drafts have no tag yet, so releases are listed rather than looked up by tag; drafts need a token that can write.
  for (let page = 1; ; page++) {
    const response = await fetch(`${base}/releases?per_page=100&page=${page}`, { headers: { Authorization: `Bearer ${env.GH_TOKEN ?? env.GITHUB_TOKEN}`, Accept: 'application/vnd.github+json' } });
    if (!response.ok) fail(`GitHub releases: HTTP ${response.status}`);
    const list = await response.json();
    const found = list.find(r => r.tag_name === tag);
    if (found) return { draft: found.draft, target: found.target_commitish };
    if (list.length < 100) return null;
  }
}

async function main([command, ...args]) {
  const value = name => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : undefined; };
  if (command === 'status') {
    const status = { prepared: preparedVersion(), documented: documentedVersion(), pending: pendingChanges() };
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
  } else if (command === 'release-plan') {
    // In sdk-release.yml: the state of the documented version, then releasePlan. Writes version and action to
    // GITHUB_OUTPUT, the explanation to stdout (Markdown for the run summary).
    const sha = value('--sha') ?? fail('release-plan needs --sha.');
    const documented = documentedVersion();
    const frozen = freezeReason();
    const summary = plan => console.log([`### SDK ${documented} from \`${sha.slice(0, 12)}\``, '', ...plan.lines.map(l => `- ${l}`), ''].join('\n'));
    const output = plan => { if (process.env.GITHUB_OUTPUT) fs.appendFileSync(process.env.GITHUB_OUTPUT, `version=${documented}\naction=${plan.action}\n`); };
    if (frozen) {
      // Stops before asking nuget.org or GitHub: a freeze never depends on them answering.
      const plan = releasePlan({ documented, frozen });
      summary(plan);
      output(plan);
      return;
    }
    const git = gitArgs => spawnSync('git', gitArgs, { cwd: repoRoot, encoding: 'utf8' });
    const release = await githubRelease(`sdk-v${documented}`);
    const plan = releasePlan({
      documented, prepared: preparedVersion(), packages: await publicState(documented), release,
      notes: releasedVersions().includes(documented), event: value('--event') ?? 'workflow_dispatch', sha,
      isAncestor: commit => git(['merge-base', '--is-ancestor', commit, sha]).status === 0,
    });
    summary(plan);
    if (plan.action === 'publish' && release.target !== sha) {
      console.log([`The draft was made from ${release.target.slice(0, 12)}; main has moved on since:`, '', '```', git(['log', '--oneline', `${release.target}..${sha}`]).stdout.trim(), '```', ''].join('\n'));
    }
    output(plan);
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
