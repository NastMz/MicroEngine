// Tests for the cost tool: node --test .github/scripts/*.test.mjs
import test from 'node:test';
import assert from 'node:assert/strict';
import { runnerOf, billableMinutes, summarize, platformOf, estimate, scenarios, report } from './ci-cost.mjs';

const job = (name, labels, start, seconds, conclusion = 'success') => ({
  name, labels, conclusion, started_at: new Date(Date.parse('2026-10-31T10:00:00Z') + start * 1000).toISOString(),
  completed_at: new Date(Date.parse('2026-10-31T10:00:00Z') + (start + seconds) * 1000).toISOString(),
});

test('billing: per job, rounded up to whole minutes, at the runner rate', () => {
  assert.equal(billableMinutes(1), 1);
  assert.equal(billableMinutes(60), 1);
  assert.equal(billableMinutes(61), 2);
  assert.equal(billableMinutes(0), 0);
  assert.equal(runnerOf(['windows-latest']), 'windows');
  assert.equal(runnerOf(['macos-latest']), 'macos');
  assert.equal(runnerOf(['ubuntu-latest']), 'linux');
  assert.equal(runnerOf(['self-hosted', 'windows']), 'self-hosted');
});

test('a run: parallel jobs add up, skipped jobs cost nothing, a short macOS job costs a full minute', () => {
  const s = summarize([
    job('Plan', ['ubuntu-latest'], 0, 21), job('.NET tests (Linux)', ['ubuntu-latest'], 25, 75),
    job('.NET tests (Windows)', ['windows-latest'], 25, 193), job('macOS (experimental)', ['macos-latest'], 25, 10),
    { name: 'Deploy', labels: ['ubuntu-latest'], conclusion: 'skipped', started_at: null, completed_at: null },
  ]);
  assert.deepEqual(s.minutes, { linux: 3, windows: 4, macos: 1 });
  assert.equal(Number(s.cost.toFixed(3)), 0.120);
  assert.equal(s.ran, 4);
  assert.equal(s.skipped, 1);
  assert.match(report({ id: 1, run_started_at: '2026-10-31T10:00:00Z', updated_at: '2026-10-31T10:03:38Z' }, s), /Wall-clock duration 3 min 38 s \(not what is billed\)/);
});

test('re-run attempts: a job copied unchanged is counted once, a job executed again counts again', () => {
  const build = job('Build NeonRift', ['windows-latest'], 0, 342);
  const failed = job('Build MeadowRun', ['windows-latest'], 0, 68, 'failure');
  const retried = job('Build MeadowRun', ['windows-latest'], 900, 159);
  const s = summarize([build, failed, { ...build }, retried]);
  assert.equal(s.minutes.windows, 6 + 2 + 3);
});

test('the summary job can leave itself out', () => {
  const s = summarize([job('Plan', ['ubuntu-latest'], 0, 21), job('CI result', ['ubuntu-latest'], 30, 5)], { exclude: ['CI result'] });
  assert.deepEqual(s.rows.map(r => r.name), ['Plan']);
});

test('platforms come from job names; experimental ones are marked', () => {
  assert.deepEqual(platformOf('Experimental / Android (experimental; emulator)'), { platform: 'Android', experimental: true });
  assert.deepEqual(platformOf('Supported / Build NeonRift (WebAssembly)'), { platform: 'WebAssembly', experimental: false });
  assert.equal(platformOf('Supported / Site (Chrome and Firefox)').platform, 'Browser');
  assert.equal(platformOf('Supported / .NET tests (Windows)').platform, 'Windows');
  assert.equal(platformOf('Supported / Native Linux (raylib, Xvfb)').platform, 'Linux');
  assert.equal(platformOf('Experimental / iOS (experimental; simulator)').platform, 'iOS');
});

test('estimates: no class costs more, and the merge into main repeats nothing already passed', () => {
  const cost = side => Object.values(side).reduce((sum, jobs) => sum + estimate(jobs).cost, 0);
  for (const s of scenarios) {
    if (s.name.startsWith('Release')) continue; // did not exist before
    // Documentation was already cheap: the CI result job costs the minute main saves on the site.
    assert.ok(cost(s.after) <= cost(s.before) + 1e-9, s.name);
  }
  const engine = scenarios.find(s => s.name === 'Engine core');
  assert.ok(!engine.after.main.some(job => /dotnet|native|site|build/i.test(job)), 'main reuses the pull request evidence');
  assert.ok(!scenarios.some(s => Object.values(s.after).some(jobs => jobs.includes('dotnetMacos'))), 'no macOS in daily CI');
  assert.throws(() => estimate(['unknownJob']), /no duration/);
});
