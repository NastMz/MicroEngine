#!/usr/bin/env node
// Evidence that a check already passed for exactly the content it reads (docs/ci.md, "Evidence"). Each check has a
// key: a hash of the Git blobs of every file the check can read, chosen by exclusion so a new folder is an input
// until a rule says otherwise. When a check passes, its job uploads a tiny artifact named after the key; a later run
// whose key is the same (a pull request that only changed documentation since, or the merge of an up-to-date pull
// request into main) finds it and skips the check instead of repeating it.
//
//   node .github/scripts/evidence.mjs plan --checks dotnet,windows,native [--extra <check>=<text> ...]
//                                          [--site-extra <text>] [--github-output]
//       prints each check's key and the run that already passed it; outputs key_<check> and reused_<check>.
//       --extra adds text to one check's key: what the check covers beyond its input files (the test projects and
//       games the project graph selected, the demo packages and browser suites of the site), so evidence of a
//       narrower run is never taken for a wider one
//   node .github/scripts/evidence.mjs record <check> <key> <file>
//       writes the evidence file the job uploads as artifact evidence-<check>-<key>
//
// Evidence is accepted only when it is unexpired, was produced by a run of this repository's own code (never a fork
// pull request, whose workflows and tests are untrusted and could upload an artifact under any name) and by a commit
// whose .github folder is identical to this one: the key covers .github too, so evidence from another version of the
// workflows can never be used. Any failure to look it up (API error, unknown commit or repository) runs the check.
import fs from 'node:fs';
import crypto from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const always = file => /^(odd|experiments)\//.test(file);
// Markdown is documentation unless a game ships it as content.
const docs = file => /^docs\//.test(file) || (/\.md$/i.test(file) && !/\/Content\//.test(file));
// Host templates of the microengine tool are compiled only into packages, never by the .NET tests.
const dotnetSkips = file => always(file) || docs(file) || /^(site|platforms|web|sdk)\//.test(file) || /^src\/MicroEngine\.Cli\/Hosts\//.test(file);

/** Which files can affect each check. */
export const scopes = {
  dotnet: file => !dotnetSkips(file),
  windows: file => !dotnetSkips(file),
  native: file => !dotnetSkips(file) || /^web\/tests\//.test(file),
  // The site reads its own sources, the repository's Markdown, the games' screenshots and the web host files; game
  // code reaches it only as packages, which the caller adds to the key (--site-extra).
  site: file => !always(file) && !/^(src|tests|samples|platforms)\//.test(file)
    && (!/^games\//.test(file) || /\.md$/i.test(file) || /^games\/[^/]+\/screenshots\//.test(file)),
  // The packages read the engine, the tool, the template, the build settings, the web toolchain and the notices they
  // ship; the external project test also reads web/ (its server and browser driver). Games, tests, samples and the
  // documentation never reach a package.
  sdk: file => !always(file) && !/^(docs|site|platforms|games|tests|samples)\//.test(file)
    && (!/\.md$/i.test(file) || /^(src|templates|sdk)\//.test(file) || file === 'THIRD-PARTY-NOTICES.md'),
};

export const isInput = (check, file) => (scopes[check] ?? fail(`unknown check "${check}"`))(file);
export const artifactName = (check, key) => `evidence-${check}-${key}`;
function fail(message) { throw new Error(message); }

const git = (args, root) => execFileSync('git', args, { cwd: root, encoding: 'utf8', maxBuffer: 64 * 1024 * 1024, stdio: ['ignore', 'pipe', 'ignore'] });

/** The key of a check at a commit: SHA-256 (first 20 hex digits) over its name, extra text and input blobs. */
export function checkKey(check, { root = process.cwd(), rev = 'HEAD', extra = '' } = {}) {
  const lines = git(['ls-tree', '-r', '-z', rev], root).split('\0').filter(Boolean)
    .map(line => { const [meta, file] = line.split('\t'); return { blob: meta.split(' ')[2], file }; })
    .filter(({ file }) => isInput(check, file))
    .sort((a, b) => (a.file < b.file ? -1 : a.file > b.file ? 1 : 0))
    .map(({ blob, file }) => `${blob} ${file}`);
  const text = ['microengine-evidence 1', `check ${check}`, `extra ${extra}`, ...lines].join('\n');
  return crypto.createHash('sha256').update(text).digest('hex').slice(0, 20);
}

function workflowsTree(rev, root) {
  try { return git(['rev-parse', `${rev}:.github`], root).trim(); }
  catch {
    try { git(['fetch', '--quiet', '--no-tags', '--depth=1', 'origin', rev], root); return git(['rev-parse', `${rev}:.github`], root).trim(); }
    catch { return null; }
  }
}

/** The run that already passed `check` for `key`, or null. `api(url)` returns parsed JSON of the GitHub REST API. */
export async function findEvidence(check, key, { root = process.cwd(), api }) {
  let listing;
  try { listing = await api(`/actions/artifacts?name=${encodeURIComponent(artifactName(check, key))}&per_page=20`); }
  catch { return null; }
  const ours = workflowsTree('HEAD', root);
  for (const artifact of listing?.artifacts ?? []) {
    const run = artifact.workflow_run ?? {};
    const sha = run.head_sha;
    if (artifact.expired || !sha || !ours) continue;
    if (!run.repository_id || run.head_repository_id !== run.repository_id) continue;
    if (workflowsTree(sha, root) === ours) return { run: artifact.workflow_run.id, commit: sha };
  }
  return null;
}

function githubApi(env = process.env) {
  const base = `${env.GITHUB_API_URL ?? 'https://api.github.com'}/repos/${env.GITHUB_REPOSITORY}`;
  return async url => {
    const response = await fetch(base + url, { headers: { Authorization: `Bearer ${env.GITHUB_TOKEN}`, Accept: 'application/vnd.github+json', 'X-GitHub-Api-Version': '2022-11-28' } });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return response.json();
  };
}

async function main([command, ...args]) {
  const value = name => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : undefined; };
  if (command === 'plan') {
    const checks = (value('--checks') ?? '').split(',').filter(Boolean);
    const extras = { site: value('--site-extra') ?? '' };
    args.forEach((arg, i) => {
      if (arg !== '--extra') return;
      const [check, ...text] = (args[i + 1] ?? '').split('=');
      if (!check || !text.length) fail(`--extra needs <check>=<text>, not "${args[i + 1]}".`);
      extras[check] = text.join('=');
    });
    const api = process.env.GITHUB_TOKEN && process.env.GITHUB_REPOSITORY ? githubApi() : async () => ({ artifacts: [] });
    const out = [], lines = ['### Evidence of passed checks', '', '| Check | Key | Already passed in |', '| --- | --- | --- |'];
    for (const check of checks) {
      const key = checkKey(check, { extra: extras[check] ?? '' });
      const found = await findEvidence(check, key, { api });
      out.push(`key_${check}=${key}`, `reused_${check}=${found ? found.run : ''}`);
      lines.push(`| ${check} | \`${key}\` | ${found ? `run ${found.run} (${found.commit.slice(0, 12)})` : 'not yet: runs if needed'} |`);
    }
    console.log(lines.join('\n') + '\n');
    if (args.includes('--github-output')) fs.appendFileSync(process.env.GITHUB_OUTPUT ?? '/dev/stdout', out.join('\n') + '\n');
  } else if (command === 'record') {
    const [check, key, file] = args;
    if (!check || !key || !file) fail('record needs <check> <key> <file>.');
    const record = { check, key, commit: process.env.GITHUB_SHA ?? null, run: process.env.GITHUB_RUN_ID ?? null, at: new Date().toISOString() };
    fs.writeFileSync(file, `${JSON.stringify(record, null, 2)}\n`);
  } else {
    console.log('usage: evidence.mjs plan --checks a,b [--site-extra TEXT] [--github-output] | record <check> <key> <file>');
    process.exitCode = command ? 1 : 0;
  }
}

if (process.argv[1] && fileURLToPath(import.meta.url) === fs.realpathSync(process.argv[1])) {
  main(process.argv.slice(2)).catch(error => { console.error(`error: ${error.message}`); process.exit(1); });
}
