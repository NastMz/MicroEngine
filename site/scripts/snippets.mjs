#!/usr/bin/env node
// Checks (or, with --write, updates) the code blocks of the public pages that quote a sample file:
//
//   node site/scripts/snippets.mjs           lists every block that differs from its file; exit code 1 if any
//   node site/scripts/snippets.mjs --write   rewrites those blocks from the files
//
// A quoted block is a fenced block right after a `<!-- code: path#region -->` line (see src/lib/repo-docs.mjs). The
// site build runs the same check, so a page cannot be published with code that is not in a compiled, tested sample.
import fs from 'node:fs';
import path from 'node:path';
import { checkQuotedCode, pages, quotedBlocks, refreshQuotedCode, repoRoot } from '../src/lib/repo-docs.mjs';

const write = process.argv.includes('--write');
let problems = 0, blocks = 0;
for (const page of pages) {
  const file = path.join(repoRoot, page.source);
  if (!fs.existsSync(file)) continue;
  const markdown = fs.readFileSync(file, 'utf8');
  blocks += quotedBlocks(markdown).length;
  const stale = checkQuotedCode(page.source, markdown);
  if (!stale.length) continue;
  if (write) {
    const eol = markdown.includes('\r\n') ? '\r\n' : '\n';
    fs.writeFileSync(file, refreshQuotedCode(markdown).replace(/\n/g, eol));
    console.log(`updated ${page.source}`);
  } else {
    for (const message of stale) console.error(message);
    problems += stale.length;
  }
}
console.log(`${blocks} quoted code blocks in ${pages.length} pages${problems ? `, ${problems} out of date` : ''}`);
process.exit(problems ? 1 : 0);
