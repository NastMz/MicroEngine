#!/usr/bin/env node
// What CI costs (docs/ci.md, "Measuring cost"). Billing is per job: GitHub rounds each job's minutes up to a whole
// minute and charges the runner's per-minute rate. Wall-clock duration of a run is not what is billed: parallel jobs
// add up, and a 10-second macOS job costs a full macOS minute.
//
//   node .github/scripts/ci-cost.mjs measure <run-id...> [--exclude <job name>] [--by-platform]
//       measured: jobs of finished runs from the GitHub API (needs `gh`, authenticated, or GH_TOKEN in Actions);
//       re-run attempts count once per job actually executed
//   node .github/scripts/ci-cost.mjs estimate
//       estimated: each change class through the previous and the current pipeline, from measured job durations
//
// Rates are list prices of standard GitHub-hosted runners, read on 2026-10-31 at
// https://docs.github.com/en/billing/reference/actions-runner-pricing. GitHub Free includes $12 of compute per month
// for private repositories (https://docs.github.com/en/billing/concepts/product-billing/github-actions); public
// repositories and self-hosted runners do not use it. Check both pages again before relying on these numbers.
import fs from 'node:fs';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

export const rates = { linux: 0.006, windows: 0.010, macos: 0.062 };
export const monthlyCredit = 12;

/** The billing class of a job from its runner labels; self-hosted runners are not billed. */
export function runnerOf(labels = []) {
  const text = labels.join(',').toLowerCase();
  if (text.includes('self-hosted')) return 'self-hosted';
  return text.includes('windows') ? 'windows' : text.includes('macos') ? 'macos' : 'linux';
}

export const billableMinutes = seconds => (seconds > 0 ? Math.ceil(seconds / 60) : 0);

/** The platform a job checks, from its name, and whether that platform is experimental. */
export function platformOf(name) {
  const rules = [[/android/i, 'Android'], [/\bios\b/i, 'iOS'], [/macos/i, 'macOS'], [/windows/i, 'Windows'],
    [/webassembly|build .* \(/i, 'WebAssembly'], [/chrome|firefox|site|browser/i, 'Browser'], [/linux/i, 'Linux']];
  return { platform: rules.find(([pattern]) => pattern.test(name))?.[1] ?? 'Orchestration', experimental: /experimental/i.test(name) };
}

/**
 * Billable minutes and cost of a run's jobs (GitHub API job objects of every attempt). A job copied unchanged into a
 * re-run attempt (same name and times) was not executed again and is counted once.
 */
export function summarize(jobs, { exclude = [] } = {}) {
  const seen = new Set(), rows = [];
  for (const job of jobs) {
    if (exclude.some(name => job.name === name || job.name.endsWith(` / ${name}`))) continue;
    const identity = `${job.name}|${job.started_at}|${job.completed_at}`;
    if (seen.has(identity)) continue;
    seen.add(identity);
    const seconds = job.started_at && job.completed_at ? (Date.parse(job.completed_at) - Date.parse(job.started_at)) / 1000 : 0;
    const ran = job.conclusion !== 'skipped' && seconds > 0;
    const runner = runnerOf(job.labels);
    const minutes = ran ? billableMinutes(seconds) : 0;
    rows.push({ name: job.name, runner, conclusion: job.conclusion ?? job.status, seconds: ran ? Math.round(seconds) : 0, minutes,
      cost: minutes * (rates[runner] ?? 0), ...platformOf(job.name) });
  }
  const minutes = { linux: 0, windows: 0, macos: 0 };
  for (const row of rows) if (row.runner in minutes) minutes[row.runner] += row.minutes;
  const cost = rows.reduce((sum, row) => sum + row.cost, 0);
  return { rows, minutes, cost, ran: rows.filter(r => r.minutes > 0).length, skipped: rows.filter(r => r.minutes === 0).length };
}

const dollars = n => `$${n.toFixed(3)}`;

export function report(run, summary, { byPlatform = false } = {}) {
  const wall = run.run_started_at && run.updated_at ? Math.round((Date.parse(run.updated_at) - Date.parse(run.run_started_at)) / 1000) : null;
  const lines = [`### Cost of run ${run.id} (${run.name ?? 'run'}, ${run.event ?? ''}, ${run.head_branch ?? ''})`, '',
    `Measured from the jobs of every attempt; billed per job, rounded up to whole minutes. Wall-clock duration ${wall === null ? 'unknown' : `${Math.floor(wall / 60)} min ${wall % 60} s`} (not what is billed).`, '',
    '| Job | Runner | Result | Duration | Billed minutes | Cost |', '| --- | --- | --- | --- | --- | --- |',
    ...summary.rows.map(r => `| ${r.name} | ${r.runner} | ${r.conclusion} | ${r.seconds ? `${r.seconds} s` : '—'} | ${r.minutes} | ${dollars(r.cost)} |`),
    '', `Billed: Linux ${summary.minutes.linux} min, Windows ${summary.minutes.windows} min, macOS ${summary.minutes.macos} min; estimated **${dollars(summary.cost)}** at list rates (${(100 * summary.cost / monthlyCredit).toFixed(1)}% of the $${monthlyCredit} monthly credit). Jobs executed: ${summary.ran}; skipped or not executed: ${summary.skipped}.`, ''];
  if (byPlatform) {
    const groups = new Map();
    for (const r of summary.rows) {
      const key = `${r.platform}${r.experimental ? ' (experimental)' : ''}`;
      const group = groups.get(key) ?? { results: new Set(), cost: 0 };
      group.results.add(r.conclusion); group.cost += r.cost;
      groups.set(key, group);
    }
    lines.push('| Platform | Results | Cost |', '| --- | --- | --- |',
      ...[...groups].map(([key, g]) => `| ${key} | ${[...g.results].join(', ')} | ${dollars(g.cost)} |`), '');
  }
  return lines.join('\n');
}

// Estimated: medians of measured job durations (seconds) from the runs named in docs/evaluations/infra-1.md;
// new jobs without a measurement are marked as such.
export const durations = {
  plan: [21, 'linux'], dotnetLinux: [75, 'linux'], dotnetWindows: [193, 'windows'], dotnetMacos: [39, 'macos'],
  nativeLinux: [203, 'linux'], buildDemo: [327, 'windows'], siteFull: [251, 'linux'], siteLayout: [100, 'linux'],
  siteBuildOnly: [100, 'linux'], deploy: [76, 'linux'], deployPromote: [100, 'linux'] /* not measured: deploy + promote */,
  result: [15, 'linux'] /* not measured */, macosPlatform: [37, 'macos'], android: [470, 'linux'], ios: [185, 'macos'],
  releaseStep: [45, 'linux'] /* not measured: preflight, draft, finish */,
};

/** Billable minutes per runner and cost of a list of job names from `durations`. */
export function estimate(jobs) {
  const minutes = { linux: 0, windows: 0, macos: 0 };
  for (const job of jobs) {
    const [seconds, runner] = durations[job] ?? (() => { throw new Error(`no duration for job "${job}"`); })();
    minutes[runner] += billableMinutes(seconds);
  }
  return { minutes, cost: Object.entries(minutes).reduce((sum, [runner, m]) => sum + m * rates[runner], 0), jobs: jobs.length };
}

// Jobs each change class runs: `before` from pipeline.yml and changes.mjs at b822702 (PLATFORM-2), `after` from
// ci.yml with the current planner, evidence and the ci demo policy. "PR" is one pull request run, "main" the run of
// its merge (up to date with main, so evidence from the pull request applies).
const W = n => Array(n).fill('buildDemo');
export const scenarios = [
  { name: 'Documentation only', before: { pr: ['plan', 'siteLayout', 'deploy'], main: ['plan', 'siteLayout', 'deploy'] },
    after: { pr: ['plan', 'siteLayout', 'deploy', 'result'], main: ['plan', 'deployPromote', 'result'] } },
  { name: "A game's tests", before: { pr: ['plan', 'dotnetLinux', 'dotnetWindows', 'siteBuildOnly', 'deploy'], main: ['plan', 'dotnetLinux', 'dotnetWindows', 'siteBuildOnly', 'deploy'] },
    after: { pr: ['plan', 'dotnetLinux', 'result'], main: ['plan', 'result'] } },
  { name: "A game's code (demo)", before: { pr: ['plan', 'dotnetLinux', 'dotnetWindows', 'nativeLinux', ...W(1), 'siteFull', 'deploy'], main: ['plan', 'dotnetLinux', 'dotnetWindows', 'nativeLinux', 'siteBuildOnly', 'deploy'] },
    after: { pr: ['plan', 'dotnetLinux', 'nativeLinux', ...W(1), 'siteFull', 'deploy', 'result'], main: ['plan', 'result'] } },
  { name: 'Engine core', before: { pr: ['plan', 'dotnetLinux', 'dotnetWindows', 'dotnetMacos', 'nativeLinux', ...W(4), 'siteFull', 'deploy'], main: ['plan', 'dotnetLinux', 'dotnetWindows', 'dotnetMacos', 'nativeLinux', 'siteBuildOnly', 'deploy'] },
    after: { pr: ['plan', 'dotnetLinux', 'dotnetWindows', 'nativeLinux', ...W(1), 'siteFull', 'deploy', 'result'], main: ['plan', 'result'] } },
  { name: 'CI infrastructure', before: { pr: ['plan', 'dotnetLinux', 'dotnetWindows', 'dotnetMacos', 'nativeLinux', 'siteFull', 'deploy'], main: ['plan', 'dotnetLinux', 'dotnetWindows', 'dotnetMacos', 'nativeLinux', 'siteFull', 'deploy'] },
    after: { pr: ['plan', 'dotnetLinux', 'dotnetWindows', 'nativeLinux', 'siteFull', 'deploy', 'result'], main: ['plan', 'deployPromote', 'result'] } },
  { name: 'Second push to a PR: docs fix after an engine change passed', before: { pr: ['plan', 'dotnetLinux', 'dotnetWindows', 'dotnetMacos', 'nativeLinux', 'siteFull', 'deploy'] },
    after: { pr: ['plan', 'siteFull', 'deploy', 'result'] } },
  { name: 'Mobile hosts (platforms/)', before: { pr: ['plan', 'siteBuildOnly', 'deploy', 'macosPlatform', 'android', 'ios'], main: ['plan', 'siteBuildOnly', 'deploy'] },
    after: { pr: ['plan', 'result'], main: ['plan', 'result'] } },
  { name: 'Platform workflow, per month', before: { scheduled: ['macosPlatform', 'android', 'ios', 'macosPlatform', 'android', 'ios', 'macosPlatform', 'android', 'ios', 'macosPlatform', 'android', 'ios'] },
    after: { scheduled: ['macosPlatform', 'android', 'ios'] } },
  { name: 'Full validation (manual, packages stored)', before: { manual: ['plan', 'dotnetLinux', 'dotnetWindows', 'dotnetMacos', 'nativeLinux', 'siteFull', 'deploy', 'macosPlatform', 'android', 'ios'] },
    after: { manual: ['releaseStep', 'dotnetLinux', 'dotnetWindows', 'nativeLinux', 'siteFull', 'macosPlatform', 'android', 'ios', 'releaseStep'] } },
  { name: 'Release (manual, packages stored)', before: { manual: [] },
    after: { manual: ['releaseStep', 'releaseStep', 'siteFull', 'deployPromote', 'releaseStep'] } },
];

export function estimateReport() {
  const lines = ['### Estimated cost per change class', '',
    'Estimated, not measured: job durations are medians of measured runs; billed minutes are each job rounded up. Before: pipeline.yml at b822702. After: ci.yml.', '',
    '| Change | Before: jobs, L/W/M min | Before cost | After: jobs, L/W/M min | After cost | Difference |', '| --- | --- | --- | --- | --- | --- |'];
  for (const s of scenarios) {
    const sum = side => Object.values(side).map(estimate).reduce((a, e) => ({ jobs: a.jobs + e.jobs, cost: a.cost + e.cost,
      minutes: { linux: a.minutes.linux + e.minutes.linux, windows: a.minutes.windows + e.minutes.windows, macos: a.minutes.macos + e.minutes.macos } }),
    { jobs: 0, cost: 0, minutes: { linux: 0, windows: 0, macos: 0 } });
    const [b, a] = [sum(s.before), sum(s.after)];
    const cell = e => `${e.jobs}, ${e.minutes.linux}/${e.minutes.windows}/${e.minutes.macos}`;
    const diff = b.cost ? `${a.cost <= b.cost ? '−' : '+'}${Math.abs(100 * (b.cost - a.cost) / b.cost).toFixed(0)}%` : 'new';
    lines.push(`| ${s.name} | ${cell(b)} | ${dollars(b.cost)} | ${cell(a)} | ${dollars(a.cost)} | ${diff} |`);
  }
  return lines.join('\n') + '\n';
}

function gh(args) {
  return JSON.parse(execFileSync('gh', ['api', ...args], { encoding: 'utf8', maxBuffer: 64 * 1024 * 1024 }));
}

function main([command, ...args]) {
  const values = name => args.flatMap((a, i) => (a === name ? [args[i + 1]] : []));
  if (command === 'measure') {
    const ids = args.filter((a, i) => /^\d+$/.test(a) && args[i - 1] !== '--exclude');
    if (!ids.length) throw new Error('measure needs at least one run id.');
    const repo = process.env.GITHUB_REPOSITORY ?? execFileSync('gh', ['repo', 'view', '--json', 'nameWithOwner', '-q', '.nameWithOwner'], { encoding: 'utf8' }).trim();
    let total = 0;
    for (const id of ids) {
      const run = gh([`repos/${repo}/actions/runs/${id}`]);
      const jobs = gh([`repos/${repo}/actions/runs/${id}/jobs?filter=all&per_page=100`]).jobs;
      const summary = summarize(jobs, { exclude: values('--exclude') });
      total += summary.cost;
      console.log(report(run, summary, { byPlatform: args.includes('--by-platform') }));
    }
    if (ids.length > 1) console.log(`Total of ${ids.length} runs: **${dollars(total)}** (measured durations, list rates).\n`);
  } else if (command === 'estimate') {
    console.log(estimateReport());
  } else {
    console.log('usage: ci-cost.mjs measure <run-id...> [--exclude <job name>] [--by-platform] | estimate');
    process.exitCode = command ? 1 : 0;
  }
}

if (process.argv[1] && fileURLToPath(import.meta.url) === fs.realpathSync(process.argv[1])) {
  try { main(process.argv.slice(2)); } catch (error) { console.error(`error: ${error.message}`); process.exit(1); }
}
