// Publishes Markdown files that live in the repository (docs/, web/, games/) as Starlight pages, without copying
// them: the repository file is the only source. This module holds the list of public pages and the pure functions
// that turn a repository Markdown file into a page; the Astro loader at the bottom wires them into the build.
//
// A public page is any file listed in `pages`: product documentation for people making games. Engineering material
// (decision records, evaluations, task records, CI and release procedures, the demos' build pipeline) stays in the
// repository for maintainers and is never published or linked: a public page that links to it fails the build. A
// link to any other repository file (source code, a sample) points to it on GitHub, and a link to a file that does
// not exist is published as its plain text.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// The repository root is the nearest folder above the working directory (or this file) holding MicroEngine.slnx.
// Bundled build chunks run from site/dist, so the module's own location is not a reliable anchor.
function findRoot(...starts) {
  for (const start of starts) {
    for (let dir = start; ; dir = path.dirname(dir)) {
      if (fs.existsSync(path.join(dir, 'MicroEngine.slnx'))) return dir;
      if (path.dirname(dir) === dir) break;
    }
  }
  throw new Error('cannot find the MicroEngine repository root (MicroEngine.slnx)');
}
export const repoRoot = findRoot(process.cwd(), path.dirname(fileURLToPath(import.meta.url)));

// Sidebar groups in order; each page belongs to one. `slug` is the page URL without slashes at the ends. The groups
// are learning paths: start, then the tutorial for beginners, then answers and examples, then the manual and the
// reference material for people who already build with the engine.
export const groups = [
  { id: 'intro', label: 'Introduction' },
  { id: 'start', label: 'Getting started' },
  { id: 'tutorial', label: 'Tutorial: Gem Garden' },
  { id: 'howto', label: 'How-to and examples' },
  { id: 'manual', label: 'Engine manual' },
  { id: 'platforms', label: 'Platforms and distribution' },
  { id: 'advanced', label: 'Advanced topics' },
  { id: 'api', label: 'API reference', collapsed: true },
  { id: 'credits', label: 'Demo asset credits', collapsed: true },
  { id: 'legal', label: 'Legal', collapsed: true },
];

const guidePages = [
  { source: 'docs/guide/introduction.md', slug: 'docs', group: 'intro', label: 'What is MicroEngine' },
  { source: 'docs/guide/status.md', slug: 'docs/status', group: 'intro', label: 'What it offers today' },
  { source: 'docs/guide/sdk.md', slug: 'docs/sdk', group: 'intro', label: 'Versions and compatibility' },
  { source: 'docs/guide/release-notes.md', slug: 'docs/release-notes', group: 'intro', label: 'SDK release notes' },
  { source: 'docs/migrations.md', slug: 'docs/migrations', group: 'intro', label: 'Migration notes' },
  { source: 'docs/guide/downloads.md', slug: 'docs/downloads', group: 'intro', label: 'Playing the demos' },
  { source: 'docs/guide/getting-microengine.md', slug: 'docs/getting-started', group: 'start', label: 'Install the SDK' },
  { source: 'docs/guide/first-game.md', slug: 'docs/first-game', group: 'start', label: 'Your first game' },
  { source: 'docs/guide/cli.md', slug: 'docs/cli', group: 'start', label: 'The microengine tool' },
  { source: 'docs/guide/updating.md', slug: 'docs/updating', group: 'start', label: 'Updating the SDK' },
  { source: 'docs/tutorial/README.md', slug: 'docs/tutorial', group: 'tutorial', label: 'Overview' },
  { source: 'docs/tutorial/01-project.md', slug: 'docs/tutorial/project', group: 'tutorial', label: '1. Create the project' },
  { source: 'docs/tutorial/02-scenes.md', slug: 'docs/tutorial/scenes', group: 'tutorial', label: '2. Scenes' },
  { source: 'docs/tutorial/03-drawing.md', slug: 'docs/tutorial/drawing', group: 'tutorial', label: '3. Drawing the hero' },
  { source: 'docs/tutorial/04-input.md', slug: 'docs/tutorial/input', group: 'tutorial', label: '4. Input and movement' },
  { source: 'docs/tutorial/05-animation.md', slug: 'docs/tutorial/animation', group: 'tutorial', label: '5. Animation' },
  { source: 'docs/tutorial/06-garden.md', slug: 'docs/tutorial/garden', group: 'tutorial', label: '6. Tile map and camera' },
  { source: 'docs/tutorial/07-collisions.md', slug: 'docs/tutorial/collisions', group: 'tutorial', label: '7. Gems, slimes, collisions' },
  { source: 'docs/tutorial/08-sound.md', slug: 'docs/tutorial/sound', group: 'tutorial', label: '8. Sound and music' },
  { source: 'docs/tutorial/09-hud.md', slug: 'docs/tutorial/hud', group: 'tutorial', label: '9. Text and HUD' },
  { source: 'docs/tutorial/10-states.md', slug: 'docs/tutorial/states', group: 'tutorial', label: '10. Game states and menus' },
  { source: 'docs/tutorial/11-saving.md', slug: 'docs/tutorial/saving', group: 'tutorial', label: '11. Saving the best score' },
  { source: 'docs/tutorial/12-packaging.md', slug: 'docs/tutorial/packaging', group: 'tutorial', label: '12. Build and package' },
  { source: 'docs/tutorial/13-other-targets.md', slug: 'docs/tutorial/other-targets', group: 'tutorial', label: '13. Other platforms' },
  { source: 'docs/howto.md', slug: 'docs/howto', group: 'howto', label: 'How do I...?' },
  { source: 'docs/examples/focused.md', slug: 'docs/examples', group: 'howto', label: 'Focused examples' },
  { source: 'docs/examples/hello-engine.md', slug: 'docs/examples/hello-engine', group: 'howto', label: 'Hello Engine' },
  { source: 'docs/examples/patterns.md', slug: 'docs/examples/patterns', group: 'howto', label: 'Patterns from the demos' },
  { source: 'docs/manual/lifecycle.md', slug: 'docs/manual/lifecycle', group: 'manual', label: 'Game lifecycle and time' },
  { source: 'docs/manual/scenes.md', slug: 'docs/manual/scenes', group: 'manual', label: 'Scenes' },
  { source: 'docs/manual/rendering.md', slug: 'docs/manual/rendering', group: 'manual', label: 'Graphics' },
  { source: 'docs/manual/text.md', slug: 'docs/manual/text', group: 'manual', label: 'Text' },
  { source: 'docs/manual/input.md', slug: 'docs/manual/input', group: 'manual', label: 'Input' },
  { source: 'docs/manual/audio.md', slug: 'docs/manual/audio', group: 'manual', label: 'Audio' },
  { source: 'docs/manual/assets.md', slug: 'docs/manual/assets', group: 'manual', label: 'Assets' },
  { source: 'docs/manual/world.md', slug: 'docs/manual/world', group: 'manual', label: 'Tile maps and collision' },
  { source: 'docs/manual/ui.md', slug: 'docs/manual/ui', group: 'manual', label: 'UI widgets' },
  { source: 'docs/manual/persistence.md', slug: 'docs/manual/persistence', group: 'manual', label: 'Saving data' },
  { source: 'docs/manual/achievements.md', slug: 'docs/manual/achievements', group: 'manual', label: 'Achievements' },
  { source: 'docs/examples/testing.md', slug: 'docs/examples/testing', group: 'manual', label: 'Testing your game' },
  { source: 'docs/guide/platforms.md', slug: 'docs/platforms', group: 'platforms', label: 'Platforms' },
  { source: 'docs/guide/packaging.md', slug: 'docs/packaging', group: 'platforms', label: 'Packaging a game' },
  { source: 'docs/guide/app-identity.md', slug: 'docs/app-identity', group: 'platforms', label: 'Name, id, version and icon' },
  { source: 'docs/guide/signing.md', slug: 'docs/signing', group: 'platforms', label: 'Signing and secrets' },
  { source: 'docs/examples/embedding.md', slug: 'docs/examples/embedding', group: 'platforms', label: 'Publishing on the web' },
  { source: 'docs/reference/limitations.md', slug: 'docs/reference/limitations', group: 'platforms', label: 'Requirements and limitations' },
  { source: 'docs/architecture.md', slug: 'docs/architecture', group: 'advanced', label: 'Architecture' },
  { source: 'docs/manual/backends.md', slug: 'docs/manual/backends', group: 'advanced', label: 'Backends and platforms' },
  { source: 'docs/manual/extending.md', slug: 'docs/manual/extending', group: 'advanced', label: 'Extending the engine' },
  { source: 'docs/reference/configuration.md', slug: 'docs/reference/configuration', group: 'advanced', label: 'Configuration and launch options' },
  { source: 'docs/reference/contracts.md', slug: 'docs/reference/contracts', group: 'advanced', label: 'Public contracts' },
  { source: 'docs/reference/components.md', slug: 'docs/reference/components', group: 'advanced', label: 'Components' },
];

const closingPages = [
  { source: 'games/CourtyardKey/CREDITS.md', slug: 'docs/credits/courtyard-key', group: 'credits', label: 'Courtyard Key' },
  { source: 'games/TrailDuel/CREDITS.md', slug: 'docs/credits/trail-duel', group: 'credits', label: 'Trail Duel' },
  { source: 'games/NeonRift/CREDITS.md', slug: 'docs/credits/neon-rift', group: 'credits', label: 'Neon Rift' },
  { source: 'docs/guide/licensing.md', slug: 'docs/licensing', group: 'legal', label: 'Licensing' },
  { source: 'THIRD-PARTY-NOTICES.md', slug: 'docs/third-party-notices', group: 'legal', label: 'Third-party notices' },
];

/**
 * The API reference: docs/api holds one generated page per namespace and per public type (site/scripts/api-reference.mjs,
 * from the engine's assemblies and XML comments) and an overview (README.md). The overview and the namespaces are in
 * the sidebar; type pages are reached from their namespace and from each other.
 */
export function apiPages(root = repoRoot) {
  const dir = path.join(root, 'docs', 'api');
  if (!fs.existsSync(dir)) return [];
  const entries = fs.readdirSync(dir).filter(f => f.endsWith('.md')).sort().map(f => {
    const name = f.slice(0, -3);
    const isIndex = name === 'README';
    const isNamespace = /^#\s+Namespace\b/m.test(fs.readFileSync(path.join(dir, f), 'utf8').slice(0, 200));
    return {
      source: `docs/api/${f}`,
      slug: isIndex ? 'docs/api' : `docs/api/${name.toLowerCase()}`,
      group: 'api',
      label: isIndex ? 'Overview' : name,
      sidebar: isIndex || isNamespace,
    };
  });
  return [...entries.filter(e => e.label === 'Overview'), ...entries.filter(e => e.label !== 'Overview')];
}

export const pages = [...guidePages, ...apiPages(), ...closingPages];

// Site pages outside the documentation that the sidebar also lists (label, URL, group).
export const extraLinks = [{ label: 'Supported platforms', link: '/platforms/', group: 'intro' }, { label: 'Roadmap', link: '/roadmap/', group: 'intro' }];

const imageExtensions = new Set(['.png', '.jpg', '.jpeg', '.gif', '.webp', '.svg', '.avif']);
const toPosix = p => p.split(path.sep).join('/');

/** Images that public pages show are served at build time from their repository path, as lossless WebP. */
export const docImageUrl = source => `/docs-assets/${source.replace(/\.[a-z]+$/i, '')}.webp`;

/** Repository paths of the images used by the public pages. */
export function docImages(list = pages, root = repoRoot) {
  const found = new Set();
  for (const page of list) {
    const file = path.join(root, page.source);
    if (!fs.existsSync(file)) continue;
    for (const [, target] of fs.readFileSync(file, 'utf8').matchAll(/!\[[^\]]*\]\(\s*<?([^)\s>]+)/g)) {
      if (/^[a-z][a-z0-9+.-]*:/i.test(target)) continue;
      const clean = path.posix.normalize(path.posix.join(path.posix.dirname(page.source), decodeURI(target.split('#')[0])));
      if (imageExtensions.has(path.extname(clean).toLowerCase()) && fs.existsSync(path.join(root, clean))) found.add(clean);
    }
  }
  return [...found].sort();
}

/**
 * Repository paths that are engineering material, not product documentation: decision records, evaluations, task
 * records, experiments, CI and release procedures, the demos' packaging and platform checks.
 */
export const internalPath = file => /^(docs\/(decisions|evaluations)\/|docs\/(ci|deployment)\.md$|odd\/|experiments\/|packaging\/|platforms\/|sdk\/|\.github\/|AGENTS\.md$)/.test(file);

/** The public repository; links to files that are not site pages point into its default branch. */
export const repositoryUrl = 'https://github.com/NastMz/MicroEngine';

/** Site URL of a public repository file, or undefined when the file is not a site page. */
export function pageUrl(source, list = pages) {
  const page = list.find(p => p.source === source);
  return page && `/${page.slug}/`;
}

/**
 * Where a link written in `source` (a repository-relative path) should point on the site: the page of a public
 * document, the converted image for a repository image, the link unchanged when it is external or an anchor, the
 * file or folder in the public repository for any other repository path, or null when that path does not exist.
 */
export function resolveLink(source, target, { list = pages, root = repoRoot, image = false } = {}) {
  if (!target || target.startsWith('#') || /^[a-z][a-z0-9+.-]*:/i.test(target) || target.startsWith('//')) return target;
  const [rawPath, hash = ''] = target.split('#');
  const anchor = hash ? `#${hash}` : '';
  const resolved = rawPath.startsWith('/')
    ? path.posix.normalize(rawPath.slice(1))
    : path.posix.normalize(path.posix.join(path.posix.dirname(source), decodeURI(rawPath)));
  if (resolved.startsWith('..')) return target; // outside the repository: leave it alone
  const clean = resolved.replace(/\/$/, '');
  if (internalPath(clean) && !pageUrl(clean, list)) throw new Error(`${source} links to ${clean}, which is internal engineering material; public pages must not link to it.`);
  const url = pageUrl(clean, list);
  if (url) return url + anchor;
  const absolute = path.join(root, clean);
  if (image && imageExtensions.has(path.extname(clean).toLowerCase()) && fs.existsSync(absolute)) return docImageUrl(clean);
  if (!fs.existsSync(absolute)) return null;
  return `${repositoryUrl}/${fs.statSync(absolute).isDirectory() ? 'tree' : 'blob'}/main/${clean}${anchor}`;
}

// Rewrites inline link and image targets outside code (fenced blocks and `inline code` are left untouched). A link to
// a missing file keeps its text and loses the link.
export function rewriteLinks(source, markdown, options = {}) {
  let fence = null;
  return markdown.split('\n').map(line => {
    const opener = line.match(/^\s{0,3}(`{3,}|~{3,})/);
    if (fence) {
      if (opener && opener[1][0] === fence[0] && opener[1].length >= fence.length && !line.trim().slice(opener[1].length).trim()) fence = null;
      return line;
    }
    if (opener) { fence = opener[1]; return line; }
    // One pass over code spans and links: a code span on its own is kept as it is, while a link whose text holds
    // code (`[`Game.cs`](...)`, common in the docs) is still a link.
    return line.replace(
      /(`+)[^`]*?\1|(!?)\[((?:[^\][]|\[[^\]]*\])*)\]\(\s*<?([^)\s>]+)>?((?:\s+"[^"]*")?)\s*\)/g,
      (match, ticks, bang, text, target, title) => {
        if (ticks) return match;
        const url = resolveLink(source, target, { ...options, image: bang === '!' });
        if (url === null) return text;
        return `${bang}[${text}](${url}${title})`;
      },
    );
  }).join('\n');
}

// Quoted code. A fenced block right after a marker line quotes a file of the repository, or one `#region name` of it:
//
//   <!-- code: samples/GemGarden/part4-complete/MainGame.cs#load -->
//   ```csharp title="MainGame.cs"
//   ...
//   ```
//
// The block must equal the file (or region) exactly, minus the `#region`/`#endregion` lines and the common indentation;
// the build fails otherwise, so the documentation can only show code that is in a sample the tests compile and run.
// `node site/scripts/snippets.mjs --write` rewrites every quoted block from its file.
const markerPattern = /^<!--\s*code:\s*([^\s#]+)(?:#([\w-]+))?\s*-->\s*$/;
const regionLine = /^\s*#(?:end)?region\b/;

/** The text of `#region name` of a C# file (or the whole file), without region lines and common indentation. */
export function extractRegion(text, region, file = 'the file') {
  const lines = text.replace(/^﻿/, '').replace(/\r\n/g, '\n').split('\n');
  let body = lines;
  if (region) {
    const start = lines.findIndex(l => l.trim() === `#region ${region}`);
    if (start < 0) throw new Error(`${file} has no "#region ${region}"`);
    let depth = 0, end = -1;
    for (let i = start + 1; i < lines.length && end < 0; i++) {
      if (/^\s*#region\b/.test(lines[i])) depth++;
      else if (/^\s*#endregion\b/.test(lines[i])) { if (depth === 0) end = i; else depth--; }
    }
    if (end < 0) throw new Error(`${file}: "#region ${region}" has no #endregion`);
    body = lines.slice(start + 1, end);
  }
  body = body.filter(l => !regionLine.test(l)).map(l => l.trimEnd());
  while (body.length && !body[0]) body.shift();
  while (body.length && !body.at(-1)) body.pop();
  const indent = Math.min(...body.filter(Boolean).map(l => l.match(/^ */)[0].length));
  return body.map(l => l.slice(Math.min(indent, l.match(/^ */)[0].length))).join('\n');
}

/** Every quoted block of a Markdown text: where its code lines are, what it quotes, and what it shows. */
export function quotedBlocks(markdown) {
  const lines = markdown.replace(/\r\n/g, '\n').split('\n');
  const blocks = [];
  for (let i = 0; i < lines.length; i++) {
    const marker = lines[i].match(markerPattern);
    if (!marker) continue;
    const fence = lines[i + 1]?.match(/^(`{3,}|~{3,})/);
    if (!fence) throw new Error(`line ${i + 1}: a "code:" marker must be followed directly by a fenced code block`);
    let close = i + 2;
    while (close < lines.length && !lines[close].startsWith(fence[1])) close++;
    blocks.push({ line: i + 1, file: marker[1], region: marker[2], start: i + 2, end: close, code: lines.slice(i + 2, close).join('\n') });
    i = close;
  }
  return blocks;
}

/** Differences between the quoted blocks of `source` and the files they quote, as messages. */
export function checkQuotedCode(source, markdown, root = repoRoot) {
  const problems = [];
  for (const block of quotedBlocks(markdown)) {
    const file = path.join(root, block.file);
    const what = `${block.file}${block.region ? `#${block.region}` : ''}`;
    if (!fs.existsSync(file)) { problems.push(`${source}:${block.line} quotes ${block.file}, which does not exist`); continue; }
    let expected;
    try { expected = extractRegion(fs.readFileSync(file, 'utf8'), block.region, block.file); }
    catch (e) { problems.push(`${source}:${block.line} ${e.message}`); continue; }
    if (expected !== block.code.replace(/[ \t]+$/gm, '')) problems.push(`${source}:${block.line} the block quoting ${what} differs from the file (node site/scripts/snippets.mjs --write updates it)`);
  }
  return problems;
}

/** The Markdown with every quoted block replaced by the current text of what it quotes. */
export function refreshQuotedCode(markdown, root = repoRoot) {
  const lines = markdown.replace(/\r\n/g, '\n').split('\n');
  for (const block of quotedBlocks(markdown).reverse()) {
    const code = extractRegion(fs.readFileSync(path.join(root, block.file), 'utf8'), block.region, block.file);
    lines.splice(block.start, block.end - block.start, ...code.split('\n'));
  }
  return lines.join('\n');
}

const plain = text => text
  .replace(/!\[[^\]]*\]\([^)]*\)/g, '')
  .replace(/\[([^\]]*)\]\([^)]*\)/g, '$1')
  .replace(/[`*_]/g, '')
  .replace(/\s+/g, ' ')
  .trim();

/** Splits a repository Markdown file into title (its first H1), description (first paragraph) and body. */
export function parseDocument(markdown) {
  const text = markdown.replace(/^﻿/, '').replace(/\r\n/g, '\n');
  const match = text.match(/^\s*#\s+(.+?)\s*#*\s*$/m);
  if (!match) throw new Error('the document has no "# Title" heading');
  const body = text.slice(match.index + match[0].length).replace(/^\n+/, '');
  const paragraph = body.split(/\n\s*\n/).find(block => block.trim() && !/^\s*(#|```|\||[-*] |\d+\. |<|!\[)/.test(block)) ?? '';
  let description = plain(paragraph);
  if (description.length > 200) description = `${description.slice(0, 197).replace(/\s+\S*$/, '')}…`;
  return { title: plain(match[1]), description, body };
}

/** Builds the page entry for one public file: what the loader stores, before Markdown rendering. */
export function buildEntry(page, { root = repoRoot, list = pages } = {}) {
  const file = path.join(root, page.source);
  if (!fs.existsSync(file)) throw new Error(`public page ${page.source} does not exist (listed in site/src/lib/repo-docs.mjs)`);
  const markdown = fs.readFileSync(file, 'utf8');
  const stale = checkQuotedCode(page.source, markdown, root);
  if (stale.length) throw new Error(`quoted code is out of date:\n  ${stale.join('\n  ')}`);
  const { title, description, body } = parseDocument(markdown);
  return {
    id: page.slug,
    file,
    body: rewriteLinks(page.source, body, { list, root }),
    data: {
      title,
      description,
      editUrl: false,
      sidebar: { label: page.label },
    },
  };
}

/** Starlight sidebar built from the page list, in list order, followed by the group's extra site links. */
export function sidebar(list = pages, extra = extraLinks) {
  return groups.map(group => ({
    label: group.label,
    ...(group.collapsed ? { collapsed: true } : {}),
    items: [
      ...list.filter(p => p.group === group.id && p.sidebar !== false).map(p => ({ slug: p.slug, label: p.label })),
      ...extra.filter(l => l.group === group.id).map(({ label, link }) => ({ label, link })),
    ],
  }));
}

/** Astro content loader for the Starlight `docs` collection. In `astro dev`, editing a source reloads its page. */
export function repoDocsLoader(list = pages) {
  return {
    name: 'microengine-repo-docs',
    async load({ store, parseData, renderMarkdown, generateDigest, watcher, logger }) {
      const sync = async page => {
        const entry = buildEntry(page, { list });
        const data = await parseData({ id: entry.id, data: entry.data, filePath: entry.file });
        store.set({
          id: entry.id,
          data,
          body: entry.body,
          filePath: toPosix(path.relative(process.cwd(), entry.file)),
          digest: generateDigest(entry.body + JSON.stringify(data)),
          rendered: await renderMarkdown(entry.body, { fileURL: new URL(`file:///${toPosix(entry.file).replace(/^\//, '')}`) }),
        });
      };
      store.clear();
      for (const page of list) await sync(page);
      if (!watcher) return;
      const byFile = new Map(list.map(p => [path.join(repoRoot, p.source), p]));
      watcher.add([...byFile.keys()]);
      watcher.on('change', async changed => {
        const page = byFile.get(path.resolve(changed));
        if (!page) return;
        logger.info(`Reloading ${page.source}`);
        await sync(page);
      });
    },
  };
}
