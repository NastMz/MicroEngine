import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import { execFileSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { classify } from '../src/lib/capability.mjs';
import { gameIds } from '../scripts/demos.mjs';
import { exercises, exercisedGames } from '../src/components/demos/exercises.mjs';
import { checkDiagrams, checkDist, checkPublicContent, engineeringMaterial } from '../scripts/check-dist.mjs';
import { headerRules, headersFor } from './serve-dist.mjs';

const siteRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const desktop = { wasm: true, webgl: true, finePointer: true, hover: true, width: 1280, height: 800 };

test('capability: desktop browsers are offered the demos', () => {
  assert.deepEqual(classify(desktop), { verdict: 'desktop', reasons: [] });
});

test('capability: touch-only or small devices get a notice, not a block', () => {
  assert.deepEqual(classify({ ...desktop, finePointer: false, hover: false, width: 390, height: 844 }), { verdict: 'unvalidated', reasons: ['touch', 'small-screen'] });
  assert.deepEqual(classify({ ...desktop, width: 600 }), { verdict: 'unvalidated', reasons: ['small-screen'] });
});

test('capability: a package with touch controls is offered to phones and tablets', () => {
  assert.deepEqual(classify({ ...desktop, finePointer: false, hover: false, width: 390, height: 844, touchControls: true }), { verdict: 'desktop', reasons: [] });
  assert.equal(classify({ ...desktop, webgl: false, touchControls: true }).verdict, 'unsupported');
});

test('capability: a hardware key press upgrades a touch device (hybrids, tablets with keyboards)', () => {
  assert.equal(classify({ ...desktop, finePointer: false, hover: false, keyboardSeen: true }).verdict, 'desktop');
});

test('capability: no WebAssembly or WebGL means the demos cannot run', () => {
  assert.deepEqual(classify({ ...desktop, webgl: false }), { verdict: 'unsupported', reasons: ['webgl'] });
});

test('every game in the web registry has a page on the site', () => {
  const data = fs.readFileSync(path.join(siteRoot, 'src', 'data', 'games.ts'), 'utf8');
  const siteIds = [...data.matchAll(/^\s+id: '([A-Za-z0-9_]+)',$/gm)].map(m => m[1]);
  assert.deepEqual([...siteIds].sort(), [...gameIds].sort());
});

test('demo markers: every engine feature a demo is said to exercise appears in that game\'s code', () => {
  assert.deepEqual([...exercisedGames].sort(), [...gameIds].sort());
  const sources = dir => fs.readdirSync(dir, { withFileTypes: true }).flatMap(entry => {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) return ['bin', 'obj'].includes(entry.name) ? [] : sources(full);
    return entry.name.endsWith('.cs') ? [fs.readFileSync(full, 'utf8')] : [];
  });
  for (const id of gameIds) {
    const code = sources(path.join(siteRoot, '..', 'games', id)).join('\n');
    for (const feature of exercises(id)) {
      for (const pattern of [feature.evidence].flat()) assert.match(code, pattern, `${id}: "${feature.name}" has no ${pattern} in games/${id}`);
      assert.match(feature.href, /^\/docs\/manual\/[a-z-]+\/(#[a-z-]+)?$/, `${feature.id}: links to a manual page`);
    }
  }
  // A marker the code does not support is refused: Trail Duel moves on a grid without the engine's tile collision.
  assert.ok(!exercises('TrailDuel').some(f => f.id === 'collision'));
  assert.throws(() => exercises('NoSuchGame'), /unknown game/);
});

test('check-dist: reports a missing package file', () => {
  const dist = fs.mkdtempSync(path.join(os.tmpdir(), 'dist-'));
  const problems = checkDist(dist);
  assert.ok(problems.includes('missing index.html'));
  assert.ok(problems.includes('missing play/NeonRift/_framework/dotnet.js'));
  assert.ok(!checkDist(dist, { demos: false }).some(p => p.includes('play/')));
});

test('check-dist: every mermaid block of a public page is published as a diagram', () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'repo-'));
  const dist = fs.mkdtempSync(path.join(os.tmpdir(), 'dist-'));
  fs.writeFileSync(path.join(root, 'a.md'), '# A\n\n```mermaid\nflowchart TB\n  a --> b\n```\n\n```text\nnot a diagram\n```\n');
  fs.mkdirSync(path.join(dist, 'docs', 'a'), { recursive: true });
  const list = [{ source: 'a.md', slug: 'docs/a' }];
  fs.writeFileSync(path.join(dist, 'docs', 'a', 'index.html'), '<pre data-language="mermaid"><code>flowchart TB</code></pre>');
  assert.deepEqual(checkDiagrams(dist, { list, root }), ['docs/a/index.html has 0 of the 1 diagram(s) in a.md']);
  fs.writeFileSync(path.join(dist, 'docs', 'a', 'index.html'), '<pre class="mermaid">flowchart TB\n  a --&gt; b</pre>');
  assert.deepEqual(checkDiagrams(dist, { list, root }), []);
});

test('vercel.json: hashed package files are immutable, /play/ is noindex, Git deployments are off', () => {
  const config = JSON.parse(fs.readFileSync(path.join(siteRoot, 'vercel.json'), 'utf8'));
  assert.equal(config.git?.deploymentEnabled, false, 'only the pipeline deploys (vercel deploy --prebuilt)');
  const rules = headerRules(config);
  const hashed = headersFor(rules, '/play/NeonRift/_framework/dotnet.native.um72e38wqy.js');
  assert.equal(hashed['Cache-Control'], 'public, max-age=31536000, immutable');
  assert.equal(hashed['X-Robots-Tag'], 'noindex');
  assert.equal(headersFor(rules, '/play/NeonRift/_framework/dotnet.js')['Cache-Control'], undefined, 'dotnet.js is not fingerprinted');
  assert.equal(headersFor(rules, '/play/NeonRift/Content/music/stage.ogg')['Cache-Control'], undefined);
  assert.equal(headersFor(rules, '/play/NeonRift/_framework/NeonRift.g4i6s41me9.wasm')['Content-Type'], 'application/wasm');
  assert.equal(headersFor(rules, '/_astro/index.Ab12.css')['Cache-Control'], 'public, max-age=31536000, immutable');
  assert.equal(headersFor(rules, '/docs/')['X-Robots-Tag'], undefined);
  assert.equal(headersFor(rules, '/docs/')['X-Content-Type-Options'], 'nosniff');
});

test('check-dist: repository links and the MIT engine are fine; demo pages need their notice; internal files, secrets and user paths are refused', () => {
  const dist = fs.mkdtempSync(path.join(os.tmpdir(), 'dist-'));
  fs.writeFileSync(path.join(dist, 'index.html'), '<a href="https://github.com/NastMz/MicroEngine">Source</a> Open source, MIT licensed');
  fs.writeFileSync(path.join(dist, 'notes.md'), '# internal');
  fs.mkdirSync(path.join(dist, 'demos', 'meadow-run'), { recursive: true });
  fs.mkdirSync(path.join(dist, 'demos', 'neon-rift'), { recursive: true });
  fs.writeFileSync(path.join(dist, 'demos', 'index.html'), 'All four demos');
  fs.writeFileSync(path.join(dist, 'demos', 'meadow-run', 'index.html'), 'Meadow Run. The engine is MIT.');
  fs.writeFileSync(path.join(dist, 'demos', 'neon-rift', 'index.html'), 'Neon Rift. Demo game: all rights reserved.');
  fs.mkdirSync(path.join(dist, 'play'));
  fs.writeFileSync(path.join(dist, 'play', 'x.js'), 'const t = "ghp_secretvalue123";');
  const problems = checkPublicContent(dist, { secrets: ['ghp_secretvalue123'] });
  assert.ok(!problems.some(p => p.startsWith('index.html')), 'links to the public repository and MIT claims for the engine are allowed');
  assert.ok(problems.includes('demos/meadow-run/index.html does not say the demo game is all rights reserved'));
  assert.ok(!problems.some(p => p.startsWith('demos/neon-rift/') || p.startsWith('demos/index.html')));
  assert.ok(problems.includes('internal file published: notes.md'));
  assert.ok(problems.includes('a build secret appears in play/x.js'));
  assert.ok(!problems.some(p => p.includes('ghp_secretvalue123')), 'the secret itself is never printed');
  fs.rmSync(path.join(dist, 'demos'), { recursive: true });
  fs.writeFileSync(path.join(dist, 'play', 'a.wasm'), Buffer.from('\0asm C:\\Users\\someone\\tools\\driver.c\0', 'latin1'));
  assert.ok(checkPublicContent(dist).includes("play/a.wasm contains a build machine's user folder path"));
  fs.writeFileSync(path.join(dist, 'play', 'a.wasm'), Buffer.from('\0asm /home/runner/work/x/obj/x.pdb\0', 'latin1'));
  assert.ok(!checkPublicContent(dist).some(p => p.includes('a.wasm')), 'public CI runner paths in third-party assemblies are allowed');
});

test('public pages never show the engineering record', () => {
  assert.deepEqual(engineeringMaterial('<h1>Platforms</h1><p>Games run on SDK 0.4.0-alpha.1 with SDL 3.</p>'), []);
  const found = engineeringMaterial('<p>See PLATFORM-3 and <a href="https://github.com/x/blob/main/docs/decisions/0016-platform-profiles.md">ADR 0016</a>.</p><h2>Evidence</h2>');
  assert.ok(found.includes('project code name PLATFORM-3'));
  assert.ok(found.includes('decision record ADR 0016'));
  assert.ok(found.some(f => f.startsWith('link to docs/decisions/')));
  assert.ok(found.includes('an evidence or validation-history section'));
});

test('brand: the favicon and logo files are drawn from the brand geometry', async () => {
  const { mark, faviconSvg, fixed } = await import('../src/components/brand/geometry.mjs');
  const favicon = faviconSvg();
  assert.ok(favicon.includes(mark.board) && favicon.includes(mark.module));
  assert.match(favicon, /prefers-color-scheme:dark/);
  for (const colour of [fixed.light.board, fixed.light.module, fixed.dark.board, fixed.dark.module]) assert.ok(favicon.includes(colour));
  const { files } = await import('../design/tools/build-assets.mjs');
  for (const [name, svg] of Object.entries(files())) {
    const committed = fs.readFileSync(path.join(siteRoot, 'src/assets/brand', name), 'utf8').replace(/\r\n/g, '\n');
    assert.equal(committed, svg + '\n', `${name} is stale: run node design/tools/build-assets.mjs`);
  }
});

test('brand: the fixed colours of the favicon and Open Graph image equal their tokens', async () => {
  const { fixed } = await import('../src/components/brand/geometry.mjs');
  const { readTokens, resolve, parseOklch, toLinearSrgb } = await import('../scripts/contrast.mjs');
  const tokens = readTokens(fs.readFileSync(path.join(siteRoot, 'src/styles/tokens.css'), 'utf8'));
  const hex = (name, theme) => '#' + toLinearSrgb(parseOklch(resolve(tokens, tokens[name], theme)))
    .map(x => Math.min(1, Math.max(0, x)))
    .map(x => Math.round(255 * (x <= 0.0031308 ? 12.92 * x : 1.055 * x ** (1 / 2.4) - 0.055)).toString(16).padStart(2, '0'))
    .join('');
  const names = { board: '--color-ink', module: '--color-mark-module', paper: '--color-paper', paper2: '--color-paper-2',
    rule: '--color-rule', ink2: '--color-ink-2', ink3: '--color-ink-3' };
  for (const theme of ['light', 'dark']) {
    for (const [key, value] of Object.entries(fixed[theme])) assert.equal(value, hex(names[key], theme), `${theme}.${key}`);
  }
});

test('brand: headings use the display face from the tokens, and no pixel face is loaded', () => {
  const styles = path.join(siteRoot, 'src/styles');
  for (const file of fs.readdirSync(styles)) assert.doesNotMatch(fs.readFileSync(path.join(styles, file), 'utf8'), /Pixelify/i, file);
  assert.match(fs.readFileSync(path.join(styles, 'tokens.css'), 'utf8'), /--font-display: 'Bricolage Grotesque'/);
  assert.match(fs.readFileSync(path.join(styles, 'fonts.css'), 'utf8'), /@fontsource\/bricolage-grotesque\/latin-600\.css/);
});

test('content: every capability and roadmap item has an availability and a subsystem family', async () => {
  const { capabilities, families, availabilityLabels } = await import('../src/data/capabilities.ts');
  const { roadmap, stages } = await import('../src/data/roadmap.ts');
  const { platforms } = await import('../src/data/platforms.ts');
  const statuses = Object.keys(availabilityLabels);
  assert.deepEqual(statuses, ['available', 'repository', 'planned']);
  assert.deepEqual(stages.map(s => s.status), statuses);
  for (const item of [...capabilities, ...roadmap, ...platforms]) {
    const name = item.name ?? item.text;
    assert.ok(statuses.includes(item.status), `${name}: status ${item.status}`);
    assert.ok(families.includes(item.family), `${name}: family ${item.family}`);
  }
  for (const status of statuses) assert.ok(roadmap.some(i => i.status === status), `the roadmap has ${status} items`);
  assert.equal(new Set(capabilities.map(c => c.id)).size, capabilities.length, 'capability ids are unique');
});

test('content: the site names the published SDK version and never presents 3D or Box2D as available', async () => {
  const { publishedSdk, capabilities } = await import('../src/data/capabilities.ts');
  const { roadmap, completed } = await import('../src/data/roadmap.ts');
  const published = JSON.parse(fs.readFileSync(path.join(siteRoot, '..', 'sdk', 'version.json'), 'utf8')).documented;
  assert.equal(publishedSdk, published);
  const data = ['roadmap.ts', 'platforms.ts', 'capabilities.ts', 'distribution.ts']
    .map(f => fs.readFileSync(path.join(siteRoot, 'src', 'data', f), 'utf8')).join('\n');
  assert.doesNotMatch(data, /\d+\.\d+\.\d+-alpha\.\d+/, 'versions are read, never written into the data');
  assert.ok(completed.some(t => t.includes(`SDK ${published} on nuget.org`)));
  const shipping = [...capabilities.filter(c => c.status === 'available').map(c => `${c.name} ${c.summary}`),
    ...roadmap.filter(i => i.status === 'available').map(i => i.text)];
  for (const text of shipping) assert.doesNotMatch(text, /\b3D\b|Box2D|glTF|visual editor/i, text);
  for (const id of ['3d-path', 'box2d']) assert.equal(capabilities.find(c => c.id === id)?.status, 'repository', id);
});

test('content: capability documentation links point at published pages', async () => {
  const { capabilities } = await import('../src/data/capabilities.ts');
  const { pages } = await import('../src/lib/repo-docs.mjs');
  const published = new Set(pages.map(p => `/${p.slug}/`));
  for (const { id, docs } of capabilities) if (docs) assert.ok(published.has(docs.split('#')[0]), `${id}: ${docs}`);
});

test('landing: the platform catalog excerpt quotes CreateCatalog only, and the landing data names real capabilities', async () => {
  const { excerpt } = await import('../src/lib/source.mjs');
  const index = fs.readFileSync(path.join(siteRoot, 'src/pages/index.astro'), 'utf8');
  assert.ok(index.includes('excerpt(catalogFile, /public static PlatformCatalog CreateCatalog/, /^\\s*$/)'), 'the landing quotes the catalog up to its blank line');
  const { code } = excerpt('src/MicroEngine.Desktop/DesktopLauncher.cs', /public static PlatformCatalog CreateCatalog/, /^\s*$/);
  assert.match(code, /\.Add\(HeadlessPlatform\.Profile\);$/);
  assert.doesNotMatch(code, /public static int Run/);
  const { capabilities } = await import('../src/data/capabilities.ts');
  const { graphicsIds } = await import('../src/data/landing/content.ts');
  for (const id of graphicsIds) assert.ok(capabilities.some(c => c.id === id), `graphicsIds: unknown capability ${id}`);
});

test('shell: no pixel grid background and no offset button shadows remain', () => {
  const css = fs.readFileSync(path.join(siteRoot, 'src/styles/site.css'), 'utf8');
  assert.doesNotMatch(css, /--color-grid|background-size: 32px/);
  assert.doesNotMatch(css, /box-shadow: \d+px \d+px 0/);
  assert.doesNotMatch(fs.readFileSync(path.join(siteRoot, 'src/styles/brand.css'), 'utf8'), /sprite-code/);
});

test('films: three independent films, each with its sources and poster, published outside the engine and demo releases', () => {
  const repo = path.resolve(siteRoot, '..');
  const catalog = JSON.parse(fs.readFileSync(path.join(repo, 'media/films.json'), 'utf8'));
  assert.equal(catalog.films.length, 3);
  assert.equal(new Set(catalog.films.map(f => f.slug)).size, 3);
  assert.equal(new Set(catalog.films.map(f => f.tells)).size, 3, 'each film tells a different thing');
  // The films' release is a series of its own: never a demo release (v*) or an SDK release (sdk-v*).
  assert.match(catalog.release, /^films-\d+$/);
  assert.equal(catalog.repository, 'NastMz/MicroEngine');
  for (const film of catalog.films) {
    for (const file of ['index.html', 'SPEC.md', 'STORYLINES.md', 'REVIEW.md', 'audio/Program.cs'])
      assert.ok(fs.existsSync(path.join(repo, 'media', film.slug, file)), `${film.slug}: ${file}`);
    assert.ok(fs.existsSync(path.join(siteRoot, 'src/assets/films', `${film.slug}.png`)), `${film.slug}: poster`);
    assert.ok(film.seconds > 0 && film.seconds < 90 && film.title && film.story);
    // No film is described as a version of another.
    assert.doesNotMatch(`${film.title} ${film.tells} ${film.story}`, /\b(version|v2|v3|replaces|remake|updated)\b/i);
  }
});

test('films: nothing rendered or regenerable is kept in Git', () => {
  const repo = path.resolve(siteRoot, '..');
  const tracked = execFileSync('git', ['ls-files', 'media'], { cwd: repo, encoding: 'utf8' }).split('\n').filter(Boolean);
  // Before the first commit of the folder there is nothing to list; the ignore rules are checked either way.
  const binary = tracked.filter(file => /\.(mp4|mov|webm|wav|ogg|mp3|woff2?|ttf|otf)$/i.test(file));
  assert.deepEqual(binary, [], 'videos, audio and fonts are regenerated, not committed');
  const ignore = fs.readFileSync(path.join(repo, 'media/.gitignore'), 'utf8');
  for (const rule of ['*/renders/', '*/assets/audio/', '*/assets/footage/', '*/assets/fonts/', '.cache/']) assert.ok(ignore.includes(rule), rule);
});

test('landing: no film is loaded with the page, and motion is off without the script or under reduced motion', () => {
  const read = file => fs.readFileSync(path.join(siteRoot, file), 'utf8');
  const filmsComponent = read('src/components/landing/Films.astro');
  // The player has no source until a card is pressed, and never preloads.
  assert.match(filmsComponent, /<video class="film-video" controls playsinline preload="none"><\/video>/);
  assert.match(filmsComponent, /video\.removeAttribute\('src'\)/);
  for (const page of ['src/pages/index.astro', 'src/pages/films.astro']) {
    assert.doesNotMatch(read(page), /<video|<source|autoplay/i, `${page}: no video element of its own`);
  }
  // Every motion rule of the landing is scoped to html.scroll-live, which the script sets only without reduced motion.
  const scroll = read('src/scripts/scroll.ts');
  assert.match(scroll, /live = !reduced\.matches;\s*root\.classList\.toggle\('scroll-live', live\)/);
  for (const file of ['src/pages/index.astro', 'src/components/landing/CodeToSprite.astro', 'src/components/landing/SocketBoard.astro', 'src/styles/site.css']) {
    // Rule by rule: whatever reads the scroll's progress or a reveal's state is selected through html.scroll-live.
    const css = read(file).replace(/\/\*[\s\S]*?\*\//g, '');
    for (const rule of css.split('}')) {
      const [selector, body = ''] = rule.split('{').slice(-2);
      const linked = /var\(--(p|step)[,)]/.test(body) || /\[data-reveal\]|\[data-seen\]/.test(selector ?? '');
      if (linked) assert.match(selector, /scroll-live/, `${file}: a scroll-linked rule outside html.scroll-live: ${selector.trim().slice(0, 90)}`);
    }
  }
  // The scroll is the only timeline: no animation library came with it. (The journey's 3D is another matter.)
  const dependencies = Object.keys(JSON.parse(read('package.json')).dependencies);
  assert.deepEqual(dependencies.filter(name => /gsap|scrolltrigger|lenis|framer|motion|aos|locomotive/i.test(name)), []);
});

test('journey: plain chapters by default, and the 3D only where it can run and was not declined', () => {
  const read = file => fs.readFileSync(path.join(siteRoot, file), 'utf8');
  const controller = read('src/scripts/journey.ts');
  // Reduced motion, data saving, little memory or no WebGL leave the page as plain chapters.
  for (const guard of ["matchMedia('(prefers-reduced-motion: reduce)').matches", 'saveData', 'deviceMemory', "getContext('webgl2')"]) assert.ok(controller.includes(guard), guard);
  // The 3D library is requested only then: imported in one module, which is loaded on demand.
  assert.match(controller, /await import\('\.\/journey\/scene\.ts'\)/);
  assert.doesNotMatch(controller, /from 'three'/);
  for (const file of ['src/pages/index.astro', 'src/components/landing/Journey.astro', 'src/scripts/scroll.ts']) assert.doesNotMatch(read(file), /from 'three'/, file);
  // Every chapter is real markup with its words, has a still for both themes, and says when it is on stage.
  const journey = read('src/components/landing/Journey.astro');
  const chapters = [...journey.matchAll(/\{ id: '(\w+)', from: ([\d.]+), to: ([\d.]+)/g)].map(m => ({ id: m[1], from: Number(m[2]), to: Number(m[3]) }));
  assert.equal(chapters.length, 6);
  chapters.forEach((chapter, i) => {
    assert.ok(chapter.from < chapter.to && (i === 0 || chapter.from > chapters[i - 1].to), `${chapter.id}: chapters follow one another`);
    for (const theme of ['light', 'dark']) assert.ok(fs.existsSync(path.join(siteRoot, 'src/assets/journey', `${chapter.id}-${theme}.png`)), `${chapter.id}-${theme}.png`);
  });
  assert.equal(chapters[0].from, 0);
  assert.equal(chapters.at(-1).to, 1);
  // The live layout exists only under html.journey-live, which the controller sets after the scene was created.
  const css = journey.slice(journey.indexOf('<style>')).replace(/\/\*[\s\S]*?\*\//g, '');
  for (const rule of css.split('}')) {
    const [selector, body = ''] = rule.split('{').slice(-2);
    if (/position: (sticky|absolute)|1000s?vh/.test(body) && !/\.words > |\.chapter\.flip/.test(selector)) assert.match(selector, /journey-live/, `a live-only rule outside html.journey-live: ${selector.trim().slice(0, 80)}`);
  }
  // The opening claim is the page's one h1 and the way past the journey is in its first chapter.
  assert.equal((journey.match(/<h1/g) || []).length, 1);
  assert.match(journey, /href="#develop">Skip the tour</);
});
