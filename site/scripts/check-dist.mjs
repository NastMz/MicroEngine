#!/usr/bin/env node
// Last step of `npm run build`: checks that site/dist is complete and safe to publish. A missing demo package,
// documentation page, sitemap or error page, a link into the private repository, an internal file or a build secret
// fails the build instead of reaching Vercel.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { pages } from '../src/lib/repo-docs.mjs';
import { gameIds } from './demos.mjs';

const siteRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');

/** Problems found in a built site folder; empty when it is complete. */
export function checkDist(dist, { demos = true, secrets = [] } = {}) {
  const problems = [];
  const need = file => { if (!fs.existsSync(path.join(dist, file))) problems.push(`missing ${file}`); };
  for (const file of ['index.html', '404.html', 'sitemap-index.xml', 'og.png', 'favicon.svg', 'pagefind/pagefind.js', 'demos/index.html', 'roadmap/index.html']) need(file);
  for (const page of pages) need(`${page.slug}/index.html`);
  for (const slug of ['meadow-run', 'courtyard-key', 'trail-duel', 'neon-rift']) need(`demos/${slug}/index.html`);
  if (demos) {
    need('play/demos.json');
    for (const id of gameIds) {
      for (const file of ['index.html', 'main.js', 'host.css', 'game.json', '_framework/dotnet.js']) need(`play/${id}/${file}`);
      const framework = path.join(dist, 'play', id, '_framework');
      if (fs.existsSync(framework)) {
        const files = fs.readdirSync(framework);
        if (!files.some(f => f.endsWith('.wasm'))) problems.push(`play/${id}/_framework has no .wasm files`);
        if (files.some(f => /\.(br|gz)$/.test(f))) problems.push(`play/${id}/_framework still has .br/.gz copies`);
      }
      const content = path.join(dist, 'play', id, 'content.txt');
      if (fs.existsSync(content)) {
        for (const line of fs.readFileSync(content, 'utf8').split('\n').map(s => s.trim().replaceAll('\\', '/')).filter(Boolean)) {
          if (!fs.existsSync(path.join(dist, 'play', id, 'Content', line))) problems.push(`play/${id}/Content/${line} is listed in content.txt but missing`);
        }
      }
    }
  }
  // No page may link to a demo package directly: packages load only from the player, on demand.
  const home = fs.existsSync(path.join(dist, 'index.html')) ? fs.readFileSync(path.join(dist, 'index.html'), 'utf8') : '';
  if (/["']\/play\//.test(home)) problems.push('the home page references /play/ (demo packages must load on demand)');
  problems.push(...checkPublicContent(dist, { secrets }));
  return problems;
}

// What must never be published: links into the private repository, statements that MicroEngine is open source or
// MIT licensed, repository-only instructions, internal source files, and the value of any build secret. Claims are
// looked for in the site's own pages only: bundled third-party code (Pagefind) keeps its licence notices, as it must.
const ownPages = /\.(html|xml)$/;
const forbiddenText = [
  [/github\.com\/NastMz/i, 'a link to the private repository', /./],
  [/\bgit clone\b/i, 'a "git clone" instruction', /./],
  [/MicroEngine\.slnx/, 'a reference to the private solution file', /./],
  [/\bopen[- ]source\b/i, 'an "open source" claim', ownPages],
  [/\bMIT[- ]licen[cs]/i, 'an MIT licence claim', ownPages],
];
const internalFile = /\.(md|cs|csproj|props|targets|pdb|map|sh)$/i;
const textFile = /\.(html|js|mjs|css|json|xml|txt|svg)$/i;

export function checkPublicContent(dist, { secrets = [] } = {}) {
  const problems = [];
  const walk = dir => fs.existsSync(dir) ? fs.readdirSync(dir, { withFileTypes: true }).flatMap(e => e.isDirectory() ? walk(path.join(dir, e.name)) : [path.join(dir, e.name)]) : [];
  for (const file of walk(dist)) {
    const rel = path.relative(dist, file).split(path.sep).join('/');
    if (internalFile.test(rel)) problems.push(`internal file published: ${rel}`);
    // Game packages are built by the engine's own pipeline; their binaries are checked for secrets only.
    const inPackage = rel.startsWith('play/') && rel !== 'play/demos.json';
    const isText = textFile.test(rel);
    if (!isText && !secrets.length) continue;
    const content = fs.readFileSync(file, isText ? 'utf8' : 'latin1');
    for (const secret of secrets) if (secret && content.includes(secret)) problems.push(`a build secret appears in ${rel}`);
    if (inPackage || !isText) continue;
    for (const [pattern, what, files] of forbiddenText) if (files.test(rel) && pattern.test(content)) problems.push(`${rel} contains ${what}`);
  }
  return problems;
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const demos = process.env.SITE_DEMOS !== 'none';
  // Build secrets are compared by value and never printed.
  const secrets = [process.env.DEMOS_TOKEN, process.env.GITHUB_TOKEN].filter(s => s && s.length >= 8);
  const problems = checkDist(path.join(siteRoot, 'dist'), { demos, secrets });
  if (problems.length) {
    console.error(`error: the built site is incomplete:\n  ${problems.join('\n  ')}`);
    process.exit(1);
  }
  console.log(`dist: complete (${pages.length} documentation pages, ${demos ? `${gameIds.length} demo packages` : 'no demo packages: SITE_DEMOS=none'})`);
}
