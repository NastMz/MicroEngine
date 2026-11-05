import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { pages, parseDocument, resolveLink, rewriteLinks, buildEntry, sidebar, docImages, repoRoot, apiPages, extractRegion, checkQuotedCode, refreshQuotedCode, quotedBlocks } from '../src/lib/repo-docs.mjs';

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

test('links to public documents become site URLs; other repository files link to the public repository', () => {
  const root = fixture({ 'docs/decisions/0007.md': '# ADR', 'src/Engine/Game.cs': '', 'web/README.md': '# Web' });
  const opts = { list, root };
  const repo = 'https://github.com/NastMz/MicroEngine';
  assert.equal(resolveLink('docs/guide/intro.md', '../manual/scenes.md#stack', opts), '/docs/manual/scenes/#stack');
  assert.equal(resolveLink('docs/guide/intro.md', '../../web/README.md', opts), '/docs/manual/web-builds/');
  assert.throws(() => resolveLink('docs/guide/intro.md', '../decisions/0007.md', opts), /internal engineering material/, 'decision records are never linked');
  assert.throws(() => resolveLink('docs/guide/intro.md', '../evaluations/x.md', opts), /internal engineering material/);
  assert.throws(() => resolveLink('docs/guide/intro.md', '../../odd/tasks/x.md', opts), /internal engineering material/);
  assert.throws(() => resolveLink('docs/guide/intro.md', '../ci.md', opts), /internal engineering material/);
  assert.equal(resolveLink('docs/guide/intro.md', '../../src/Engine', opts), `${repo}/tree/main/src/Engine`);
  assert.equal(resolveLink('docs/guide/intro.md', '../../src/Missing.cs', opts), null, 'a file that does not exist is not linked');
  assert.equal(resolveLink('docs/guide/intro.md', 'https://example.com/a.md', opts), 'https://example.com/a.md');
  assert.equal(resolveLink('docs/guide/intro.md', '#local', opts), '#local');
  const markdown = 'See [the engine](../../src/Engine) and [gone](../../src/Gone.cs).';
  assert.equal(rewriteLinks('docs/guide/intro.md', markdown, opts), `See [the engine](${repo}/tree/main/src/Engine) and gone.`);
  assert.throws(() => rewriteLinks('docs/guide/intro.md', 'See [ADR 0007](../decisions/0007.md).', opts), /internal engineering material/);
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
  assert.equal(entry.data.editUrl, false, 'no "edit page" link: pages are edited through pull requests');
});

test('a listed page that is missing fails loudly', () => {
  const root = fixture({});
  assert.throws(() => buildEntry(list[0], { root, list }), /does not exist/);
});

test('the sidebar is the learning paths, keeps every listed page once, and links the roadmap', () => {
  const items = sidebar().flatMap(group => group.items);
  const slugs = items.filter(i => i.slug).map(i => i.slug);
  assert.equal(new Set(slugs).size, slugs.length, 'no page twice');
  assert.equal(slugs.length, pages.filter(p => p.sidebar !== false).length);
  assert.deepEqual(sidebar().map(g => g.label), [
    'Introduction', 'Getting started', 'Tutorial: Gem Garden', 'How-to and examples', 'Engine manual',
    'Platforms and distribution', 'Advanced topics', 'API reference', 'Demo asset credits', 'Legal',
  ]);
  assert.ok(sidebar()[0].items.some(i => i.link === '/roadmap/'));
  const tutorial = sidebar().find(g => g.label === 'Tutorial: Gem Garden').items.map(i => i.label);
  assert.equal(tutorial[0], 'Overview');
  assert.deepEqual(tutorial.slice(1).map(l => Number(l.split('.')[0])), [...Array(13).keys()].map(i => i + 1), 'steps in order, for the previous/next links');
});

test('API pages: the overview and namespaces are in the sidebar, type pages are published but not listed there', () => {
  const root = fixture({
    'docs/api/README.md': '# API reference\n\nOverview.\n',
    'docs/api/MicroEngine.Scenes.md': '# Namespace MicroEngine.Scenes\n\nScenes.\n',
    'docs/api/MicroEngine.Scenes.Scene.md': '# Class Scene\n\nA screen.\n',
  });
  const api = apiPages(root);
  assert.deepEqual(api.map(p => [p.slug, p.sidebar]), [
    ['docs/api', true], ['docs/api/microengine.scenes', true], ['docs/api/microengine.scenes.scene', false],
  ]);
  assert.equal(api[0].label, 'Overview');
});

const sample = [
  'public sealed class Demo',
  '{',
  '    #region load',
  '    public void Load()',
  '    {',
  '        #region inner',
  '        Run();',
  '        #endregion',
  '    }',
  '    #endregion',
  '}',
].join('\r\n');

test('quoted code: a region is extracted without region lines and common indentation', () => {
  assert.equal(extractRegion(sample, 'load'), 'public void Load()\n{\n    Run();\n}');
  assert.equal(extractRegion(sample, 'inner'), 'Run();');
  assert.equal(extractRegion(sample), 'public sealed class Demo\n{\n    public void Load()\n    {\n        Run();\n    }\n}', 'the whole file');
  assert.throws(() => extractRegion(sample, 'missing', 'Demo.cs'), /Demo\.cs has no "#region missing"/);
});

test('quoted code: a block that differs from its file fails the page, and refreshing fixes it', () => {
  const root = fixture({ 'samples/Demo.cs': sample });
  const good = 'Intro.\n\n<!-- code: samples/Demo.cs#inner -->\n```csharp title="Demo.cs"\nRun();\n```\n';
  const stale = good.replace('Run();\n```', 'Walk();\n```');
  assert.deepEqual(checkQuotedCode('docs/page.md', good, root), []);
  assert.match(checkQuotedCode('docs/page.md', stale, root)[0], /docs\/page\.md:3 the block quoting samples\/Demo\.cs#inner differs/);
  assert.equal(refreshQuotedCode(stale, root), good);
  assert.match(checkQuotedCode('docs/page.md', good.replace('Demo.cs#', 'Gone.cs#'), root)[0], /does not exist/);
  const page = { source: 'docs/page.md', slug: 'docs/page', group: 'intro', label: 'Page' };
  fs.mkdirSync(path.join(root, 'docs'), { recursive: true });
  fs.writeFileSync(path.join(root, 'docs/page.md'), `# Page\n\n${stale}`);
  assert.throws(() => buildEntry(page, { root, list: [page] }), /quoted code is out of date/);
});

test('every quoted block of the published pages matches its sample', () => {
  const problems = pages.flatMap(p => checkQuotedCode(p.source, fs.readFileSync(path.join(repoRoot, p.source), 'utf8')));
  assert.deepEqual(problems, []);
  const quoted = pages.reduce((n, p) => n + quotedBlocks(fs.readFileSync(path.join(repoRoot, p.source), 'utf8')).length, 0);
  assert.ok(quoted >= 100, `the tutorial, how-to and examples quote their code (${quoted} blocks)`);
});

test('check-dist: a link to a page or an anchor that the site does not have fails the build', async () => {
  const { checkLinks } = await import('../scripts/check-dist.mjs');
  const dist = fixture({
    'docs/index.html': '<a href="/docs/a/">a</a> <a href="/docs/a/#here">here</a> <a href="https://example.com/x">x</a> <a href="#top">top</a><h1 id="top">t</h1>',
    'docs/a/index.html': '<h2 id="here">Here</h2> <a href="/docs/missing/">gone</a> <a href="/docs/#nowhere">nowhere</a>',
  });
  assert.deepEqual(checkLinks(dist).sort(), [
    'docs/a/index.html links to /docs/#nowhere, but that page has no #nowhere',
    'docs/a/index.html links to /docs/missing/, which is not a page of the site',
  ]);
});

test('docs families: every mapped page is published, socket cards name families that exist, API pages follow their namespace', async () => {
  const { pageFamilies, socketCards, familyOf } = await import('../src/components/docs/families.mjs');
  const { families } = await import('../src/components/brand/geometry.mjs');
  const slugs = new Set(pages.map(p => p.slug));
  for (const [slug, family] of Object.entries(pageFamilies)) {
    assert.ok(slugs.has(slug), `${slug} is not a published page`);
    assert.ok(families[family], `${slug}: unknown family ${family}`);
  }
  for (const slug of Object.keys(socketCards)) assert.ok(familyOf(slug), `${slug} has a socket card but no family`);
  assert.equal(familyOf('docs/api/microengine.audio.audioclip'), 'audio');
  assert.equal(familyOf('docs/api/microengine.ui'), undefined);
  assert.equal(familyOf('docs'), undefined);
});

test('docs status phrases: written statuses become status tags, outside code and headings only', async () => {
  const { markStatusPhrases } = await import('../src/components/docs/status-phrases.mjs');
  const tag = (status, text) => `<span class="status-tag" data-status="${status}"><span class="status-mark" aria-hidden="true"></span>${text}</span>`;
  assert.equal(markStatusPhrases('<p>Next SDK version (not published yet: arrives with the next SDK version): x</p>'),
    `<p>${tag('next', 'Next SDK version')} (not published yet: arrives with the next SDK version): x</p>`);
  assert.equal(markStatusPhrases('<p>In the repository only, not in a package yet (x).</p>'),
    `<p>${tag('repo', 'In the repository only, not in a package yet')} (x).</p>`);
  assert.equal(markStatusPhrases('<p><strong>Not in a published SDK yet</strong>: y</p>'),
    `<p><strong>${tag('next', 'Not in a published SDK yet')}</strong>: y</p>`);
  const untouched = '<h2 id="a">Next SDK version: x</h2><pre><code>next SDK version: y</code></pre><p>arrives with the next SDK version.</p>';
  assert.equal(markStatusPhrases(untouched), untouched);
});
