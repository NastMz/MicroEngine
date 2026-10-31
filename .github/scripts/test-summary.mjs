#!/usr/bin/env node
// Summarises `dotnet test --logger trx` results for the job summary: totals per test project, and every test that
// did not run on this system with the reason it gave (an [OSCondition] test on another OS, a native test without its
// display or sound server). Skips do not fail the job; failures already did in `dotnet test`.
//   node .github/scripts/test-summary.mjs <results-dir> [title]
import fs from 'node:fs';
import path from 'node:path';

const [dir, title = 'Tests'] = process.argv.slice(2);
const unescape = s => s.replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&quot;/g, '"').replace(/&apos;/g, "'").replace(/&amp;/g, '&');
const rows = [], skipped = [];
for (const file of fs.existsSync(dir) ? fs.readdirSync(dir).filter(f => f.endsWith('.trx')).sort() : []) {
  const trx = fs.readFileSync(path.join(dir, file), 'utf8');
  if (!/<UnitTestResult /.test(trx)) continue; // a project whose tests were all filtered out (native tests)
  const storage = trx.match(/<UnitTest [^>]*storage="([^"]+)"/)?.[1] ?? file;
  const count = outcome => (trx.match(new RegExp(`<UnitTestResult [^>]*outcome="${outcome}"`, 'g')) ?? []).length;
  rows.push(`| ${path.basename(storage, '.dll')} | ${count('Passed')} | ${count('Failed')} | ${count('NotExecuted') + count('Inconclusive')} |`);
  for (const [, attrs, body] of trx.matchAll(/<UnitTestResult ([^>]*outcome="(?:NotExecuted|Inconclusive)"[^>]*)>([\s\S]*?)<\/UnitTestResult>/g)) {
    const name = attrs.match(/testName="([^"]+)"/)?.[1];
    const reason = body.match(/<Message>([\s\S]*?)<\/Message>/)?.[1]?.trim() ?? 'no reason given';
    skipped.push(`- \`${unescape(name)}\`: ${unescape(reason)}`);
  }
}
const text = [`### ${title}`, '', '| Project | Passed | Failed | Skipped |', '| --- | --- | --- | --- |', ...rows, '',
  skipped.length ? `Skipped on this system (expected, not failures):\n\n${skipped.join('\n')}` : 'No test was skipped.', ''].join('\n');
console.log(text);
if (process.env.GITHUB_STEP_SUMMARY) fs.appendFileSync(process.env.GITHUB_STEP_SUMMARY, text + '\n');
