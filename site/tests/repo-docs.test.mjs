import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { pages, parseDocument, resolveLink, rewriteLinks, buildEntry, sidebar, docImages, repoRoot } from '../src/lib/repo-docs.mjs';

const list = [
  { source: 'docs/guide/intro.md', slug: 'docs', group: 'intro', label: 'Intro' },
  { source: 'docs/manual/scenes.md', slug: 'docs/manual/scenes', group: 'manual', label: 'Scenes' },
  { source: 'web/README.md', slug: 'docs/manual/web-builds', group: 'manual', label: 'Web builds' },
];

function fixture(files) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'repo-docs-'));
  for (const [file, text] of Object.entries(files)) {
    fs.mkdirSync(path.dirname(path.join(root, file)), { recursive: true });
    fs.writeFileSync(path.join(root, file), text);
  }
  return root;
}

test('every public page exists in the repository and has a title', () => {
  for (const page of pages) {
    const entry = buildEntry(page);
    assert.ok(entry.data.title, `${page.source} has a title`);
    assert.ok(entry.data.description, `${page.source} has a description`);
  }
});

test('internal documents are not published', () => {
  const internal = /docs\/(decisions|evaluations)\/|experiments\/|odd\/|docs\/deployment\.md|docs\/legal\/|docs\/guide\/(getting-started|project-structure)\.md|web\/README\.md|games\/\w+\/README\.md/;
  assert.deepEqual(pages.filter(p => internal.test(p.source)).map(p => p.source), []);
  assert.equal(new Set(pages.map(p => p.slug)).size, pages.length, 'slugs are unique');
});

test('title comes from the first H1 and the description from the first paragraph', () => {
  const doc = parseDocument('# Scenes and **stacks**\n\nA scene is a `Scene` subclass that [updates](x.md) each step.\n\n## More\n');
  assert.equal(doc.title, 'Scenes and stacks');
  assert.equal(doc.description, 'A scene is a Scene subclass that updates each step.');
  assert.ok(!doc.body.includes('# Scenes'), 'the H1 is not repeated in the body');
  assert.throws(() => parseDocument('no heading'), /no "# Title"/);
});

test('links to public documents become site URLs; internal repository files are not linked', () => {
  const root = fixture({ 'docs/decisions/0007.md': '# ADR', 'src/Engine/Game.cs': '', 'web/README.md': '# Web' });
  const opts = { list, root };
  assert.equal(resolveLink('docs/guide/intro.md', '../manual/scenes.md#stack', opts), '/docs/manual/scenes/#stack');
  assert.equal(resolveLink('docs/guide/intro.md', '../../web/README.md', opts), '/docs/manual/web-builds/');
  assert.equal(resolveLink('docs/guide/intro.md', '../decisions/0007.md', opts), null);
  assert.equal(resolveLink('docs/guide/intro.md', '../../src/Engine', opts), null);
  assert.equal(resolveLink('docs/guide/intro.md', 'https://example.com/a.md', opts), 'https://example.com/a.md');
  assert.equal(resolveLink('docs/guide/intro.md', '#local', opts), '#local');
  const markdown = 'See [ADR 0007](../decisions/0007.md) and [`src/Engine/Game.cs`](../../src/Engine/Game.cs).';
  assert.equal(rewriteLinks('docs/guide/intro.md', markdown, opts), 'See ADR 0007 and `src/Engine/Game.cs`.');
});

test('links inside code are left alone', () => {
  const markdown = 'See [scenes](../manual/scenes.md).\n\n```md\n[scenes](../manual/scenes.md)\n```\n\nInline `[x](../manual/scenes.md)` stays.';
  const out = rewriteLinks('docs/guide/intro.md', markdown, { list, root: repoRoot });
  assert.match(out, /^See \[scenes\]\(\/docs\/manual\/scenes\/\)\./);
  assert.match(out, /```md\n\[scenes\]\(\.\.\/manual\/scenes\.md\)\n```/);
  assert.match(out, /`\[x\]\(\.\.\/manual\/scenes\.md\)`/);
});

test('images of public pages are served from their repository files', () => {
  const out = rewriteLinks('games/NeonRift/README.md', '![Title](screenshots/01-title.png)', { root: repoRoot });
  assert.equal(out, '![Title](/docs-assets/games/NeonRift/screenshots/01-title.webp)');
  const withImage = [{ source: 'games/NeonRift/README.md', slug: 'x', group: 'intro', label: 'x' }];
  assert.ok(docImages(withImage).includes('games/NeonRift/screenshots/01-title.png'));
});

test('editing a source file changes the published page (single source)', () => {
  const root = fixture({ 'docs/manual/scenes.md': '# Scenes\n\nFirst version.\n' });
  const page = list[1];
  assert.equal(buildEntry(page, { root, list }).data.description, 'First version.');
  fs.writeFileSync(path.join(root, page.source), '# Scene stack\n\nSecond version, see [intro](../guide/intro.md).\n');
  const entry = buildEntry(page, { root, list });
  assert.equal(entry.data.title, 'Scene stack');
  assert.match(entry.body, /\]\(\/docs\/\)/);
  assert.equal(entry.data.editUrl, false, 'no "edit page" link into the private repository');
});

test('a listed page that is missing fails loudly', () => {
  const root = fixture({});
  assert.throws(() => buildEntry(list[0], { root, list }), /does not exist/);
});

test('the sidebar keeps every page once, grouped in order, and links the roadmap', () => {
  const items = sidebar().flatMap(group => group.items);
  assert.equal(items.filter(i => i.slug).length, pages.length);
  assert.deepEqual(sidebar().map(g => g.label), ['Introduction', 'Getting started', 'Engine manual', 'Examples', 'Reference', 'Demo asset credits']);
  assert.ok(sidebar()[0].items.some(i => i.link === '/roadmap/'));
});
