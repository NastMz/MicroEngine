// Publishes Markdown files that live in the repository (docs/, web/, games/) as Starlight pages, without copying
// them: the repository file is the only source. This module holds the list of public pages and the pure functions
// that turn a repository Markdown file into a page; the Astro loader at the bottom wires them into the build.
//
// A public page is any file listed in `pages`. Everything else (engine-development guides, ADRs, evaluations, game
// READMEs, source files) stays in the repository: a link from a public page to such a file points to it in the public
// repository on GitHub, and a link to a file that does not exist is published as its plain text.
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

// Sidebar groups in order; each page belongs to one. `slug` is the page URL without slashes at the ends.
export const groups = [
  { id: 'intro', label: 'Introduction' },
  { id: 'start', label: 'Getting started' },
  { id: 'manual', label: 'Engine manual' },
  { id: 'examples', label: 'Examples' },
  { id: 'reference', label: 'Reference' },
  { id: 'credits', label: 'Demo asset credits' },
  { id: 'legal', label: 'Legal' },
];

export const pages = [
  { source: 'docs/guide/introduction.md', slug: 'docs', group: 'intro', label: 'What is MicroEngine' },
  { source: 'docs/guide/status.md', slug: 'docs/status', group: 'intro', label: 'Status and limitations' },
  { source: 'docs/architecture.md', slug: 'docs/architecture', group: 'intro', label: 'Architecture' },
  { source: 'docs/guide/platforms.md', slug: 'docs/platforms', group: 'intro', label: 'Platforms' },
  { source: 'docs/guide/getting-microengine.md', slug: 'docs/getting-started', group: 'start', label: 'Getting MicroEngine' },
  { source: 'docs/making-a-game.md', slug: 'docs/making-a-game', group: 'start', label: 'Making a game' },
  { source: 'docs/manual/lifecycle.md', slug: 'docs/manual/lifecycle', group: 'manual', label: 'Game lifecycle' },
  { source: 'docs/manual/scenes.md', slug: 'docs/manual/scenes', group: 'manual', label: 'Scenes' },
  { source: 'docs/manual/rendering.md', slug: 'docs/manual/rendering', group: 'manual', label: 'Rendering' },
  { source: 'docs/manual/input.md', slug: 'docs/manual/input', group: 'manual', label: 'Input' },
  { source: 'docs/manual/audio.md', slug: 'docs/manual/audio', group: 'manual', label: 'Audio' },
  { source: 'docs/manual/assets.md', slug: 'docs/manual/assets', group: 'manual', label: 'Assets' },
  { source: 'docs/manual/ui.md', slug: 'docs/manual/ui', group: 'manual', label: 'UI' },
  { source: 'docs/manual/persistence.md', slug: 'docs/manual/persistence', group: 'manual', label: 'Persistence' },
  { source: 'docs/manual/achievements.md', slug: 'docs/manual/achievements', group: 'manual', label: 'Achievements' },
  { source: 'docs/manual/backends.md', slug: 'docs/manual/backends', group: 'manual', label: 'Backends and platforms' },
  { source: 'docs/examples/hello-engine.md', slug: 'docs/examples/hello-engine', group: 'examples', label: 'Hello Engine' },
  { source: 'docs/examples/patterns.md', slug: 'docs/examples/patterns', group: 'examples', label: 'Usage patterns' },
  { source: 'docs/examples/testing.md', slug: 'docs/examples/testing', group: 'examples', label: 'Testing and scripts' },
  { source: 'docs/examples/embedding.md', slug: 'docs/examples/embedding', group: 'examples', label: 'The browser host' },
  { source: 'docs/reference/contracts.md', slug: 'docs/reference/contracts', group: 'reference', label: 'Public contracts' },
  { source: 'docs/reference/components.md', slug: 'docs/reference/components', group: 'reference', label: 'Components' },
  { source: 'docs/reference/configuration.md', slug: 'docs/reference/configuration', group: 'reference', label: 'Configuration' },
  { source: 'docs/reference/limitations.md', slug: 'docs/reference/limitations', group: 'reference', label: 'Known limitations' },
  { source: 'games/CourtyardKey/CREDITS.md', slug: 'docs/credits/courtyard-key', group: 'credits', label: 'Courtyard Key' },
  { source: 'games/TrailDuel/CREDITS.md', slug: 'docs/credits/trail-duel', group: 'credits', label: 'Trail Duel' },
  { source: 'games/NeonRift/CREDITS.md', slug: 'docs/credits/neon-rift', group: 'credits', label: 'Neon Rift' },
  { source: 'docs/guide/licensing.md', slug: 'docs/licensing', group: 'legal', label: 'Licensing' },
  { source: 'THIRD-PARTY-NOTICES.md', slug: 'docs/third-party-notices', group: 'legal', label: 'Third-party notices' },
];

// Site pages outside the documentation that the sidebar also lists (label, URL, group).
export const extraLinks = [{ label: 'Roadmap', link: '/roadmap/', group: 'intro' }];

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
  const { title, description, body } = parseDocument(fs.readFileSync(file, 'utf8'));
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
    items: [
      ...list.filter(p => p.group === group.id).map(p => ({ slug: p.slug, label: p.label })),
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
