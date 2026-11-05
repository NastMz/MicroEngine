#!/usr/bin/env node
// Last step of `npm run build`: checks that site/dist is complete and safe to publish. A missing demo package,
// documentation page, sitemap or error page, a demo page without the demo games' licence notice, an internal file or
// a build secret fails the build instead of reaching Vercel.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { pages, repoRoot } from '../src/lib/repo-docs.mjs';
import { gameIds, noticeFiles } from './demos.mjs';

const siteRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');

/** Problems found in a built site folder; empty when it is complete. */
export function checkDist(dist, { demos = true, secrets = [] } = {}) {
  const problems = [];
  const need = file => { if (!fs.existsSync(path.join(dist, file))) problems.push(`missing ${file}`); };
  for (const file of ['index.html', '404.html', 'sitemap-index.xml', 'og.png', 'favicon.svg', 'pagefind/pagefind.js', 'demos/index.html', 'roadmap/index.html', 'platforms/index.html']) need(file);
  for (const page of pages) need(`${page.slug}/index.html`);
  for (const slug of ['meadow-run', 'courtyard-key', 'trail-duel', 'neon-rift']) need(`demos/${slug}/index.html`);
  if (demos) {
    need('play/demos.json');
    for (const id of gameIds) {
      for (const file of ['index.html', 'main.js', 'host.css', 'game.json', '_framework/dotnet.js', ...noticeFiles]) need(`play/${id}/${file}`);
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
  problems.push(...checkDiagrams(dist));
  problems.push(...checkLinks(dist));
  problems.push(...checkPublicContent(dist, { secrets }));
  return problems;
}

/**
 * Every link of a page to another page of the site must reach a built page, and every #anchor an element with that id.
 * Demo packages (/play/) are checked by checkDist itself; external links are not followed.
 */
export function checkLinks(dist) {
  const problems = [];
  const walk = dir => fs.existsSync(dir) ? fs.readdirSync(dir, { withFileTypes: true }).flatMap(e => e.isDirectory() ? walk(path.join(dir, e.name)) : [path.join(dir, e.name)]) : [];
  const html = walk(dist).filter(f => f.endsWith('.html') && !path.relative(dist, f).startsWith('play'));
  const ids = new Map();
  const idsOf = file => {
    if (!ids.has(file)) ids.set(file, new Set([...fs.readFileSync(file, 'utf8').matchAll(/\sid="([^"]+)"/g)].map(m => m[1])));
    return ids.get(file);
  };
  for (const file of html) {
    const rel = path.relative(dist, file).split(path.sep).join('/');
    const text = fs.readFileSync(file, 'utf8');
    for (const [, href] of text.matchAll(/<a\b[^>]*\shref="([^"]+)"/g)) {
      if (/^[a-z][a-z0-9+.-]*:|^\/\//i.test(href) || href.startsWith('/play/')) continue;
      const [rawPath, hash] = href.split('#');
      let target = file;
      if (rawPath) {
        if (!rawPath.startsWith('/')) continue;
        const clean = decodeURIComponent(rawPath).replace(/^\//, '');
        const candidates = [path.join(dist, clean), path.join(dist, clean, 'index.html')];
        target = candidates.find(c => fs.existsSync(c) && fs.statSync(c).isFile());
        if (!target) { problems.push(`${rel} links to ${href}, which is not a page of the site`); continue; }
      }
      if (hash && target.endsWith('.html') && !idsOf(target).has(decodeURIComponent(hash))) problems.push(`${rel} links to ${href}, but that page has no #${hash}`);
    }
  }
  return [...new Set(problems)];
}

/** Each ```mermaid block of a public page must reach the page as a diagram, not as a code sample. */
export function checkDiagrams(dist, { list = pages, root = repoRoot } = {}) {
  const problems = [];
  for (const page of list) {
    const source = path.join(root, page.source);
    const built = path.join(dist, page.slug, 'index.html');
    if (!fs.existsSync(source) || !fs.existsSync(built)) continue;
    const expected = fs.readFileSync(source, 'utf8').match(/^ {0,3}(`{3,}|~{3,})\s*mermaid\s*$/gm)?.length ?? 0;
    const found = fs.readFileSync(built, 'utf8').match(/<pre class="mermaid"[\s>]/g)?.length ?? 0;
    if (found !== expected) problems.push(`${page.slug}/index.html has ${found} of the ${expected} diagram(s) in ${page.source}`);
  }
  return problems;
}

// The engine is MIT and its repository is public, so links to it and "open source" are fine. The demo games are
// not MIT (games/LICENSE): every demo page must say they are all rights reserved, so no page presents a demo as
// open source software by omission.
const demoPage = /^demos\/[^/]+\/index\.html$/;
const reservedNotice = /all rights reserved/i;
const internalFile = /\.(md|cs|csproj|props|targets|pdb|map|sh)$/i;
// A user profile folder compiled into a package (debug paths, assertion file names) names the build machine's user.
// Hosted CI accounts (runner, runneradmin) are public: third-party assemblies such as Raylib-cs carry them.
const userPath = /[A-Za-z]:[\\/]Users[\\/](?!runneradmin[\\/])[^\\/\s"]+[\\/]|\/home\/(?!runner\/)[^/\s"]+\/|\/Users\/(?!runner\/)[^/\s"]+\//;
const textFile = /\.(html|js|mjs|css|json|xml|txt|svg)$/i;

/**
 * The site is product documentation for people making games: it never shows the project's engineering record. Finds
 * project code names (PLATFORM-3, SDK-1...), decision records, evaluations, task records or CI reports, and
 * "evidence"-style sections, in a built page.
 */
export function engineeringMaterial(html) {
  const found = new Set();
  const rules = [
    [/\b(?:PLATFORM|PRODUCT|INPUT|SDK|WEB|INFRA|PUBLIC|DIST)-\d\b/g, m => `project code name ${m}`],
    [/\bADRs?\s*\d{1,4}\b/g, m => `decision record ${m}`],
    [/docs\/(?:decisions|evaluations)\/[\w./-]*/g, m => `link to ${m}`],
    [/\bodd\/tasks\b[\w./-]*/g, m => `link to ${m}`],
    [/docs\/(?:ci|deployment)\.md/g, m => `link to ${m}`],
    [/<h[1-6][^>]*>[^<]*\b(?:Evidence|How we know|Validation history|What was validated)\b/gi, () => 'an evidence or validation-history section'],
  ];
  for (const [pattern, describe] of rules) for (const m of html.match(pattern) ?? []) found.add(describe(m));
  return [...found];
}

export function checkPublicContent(dist, { secrets = [] } = {}) {
  const problems = [];
  const walk = dir => fs.existsSync(dir) ? fs.readdirSync(dir, { withFileTypes: true }).flatMap(e => e.isDirectory() ? walk(path.join(dir, e.name)) : [path.join(dir, e.name)]) : [];
  for (const file of walk(dist)) {
    const rel = path.relative(dist, file).split(path.sep).join('/');
    if (internalFile.test(rel)) problems.push(`internal file published: ${rel}`);
    // Game packages are built by the engine's own pipeline: their files are checked for secrets and build paths.
    const inPackage = rel.startsWith('play/') && rel !== 'play/demos.json';
    const isText = textFile.test(rel);
    if (!isText && !secrets.length && !inPackage) continue;
    const content = fs.readFileSync(file, isText ? 'utf8' : 'latin1');
    for (const secret of secrets) if (secret && content.includes(secret)) problems.push(`a build secret appears in ${rel}`);
    if (userPath.test(content)) problems.push(`${rel} contains a build machine's user folder path`);
    if (inPackage || !isText) continue;
    if (demoPage.test(rel) && !reservedNotice.test(content)) problems.push(`${rel} does not say the demo game is all rights reserved`);
    if (rel.endsWith('.html')) for (const found of engineeringMaterial(content)) problems.push(`${rel} shows internal engineering material: ${found}`);
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
