#!/usr/bin/env node
// Generates the API reference pages (docs/api/*.md) from the engine's assemblies and their XML comments, with DocFX
// (a .NET Foundation tool, pinned in .config/dotnet-tools.json), then adapts its Markdown to the site:
//
//   node site/scripts/api-reference.mjs           regenerates docs/api
//   node site/scripts/api-reference.mjs --check   fails when docs/api differs from what the source would generate
//
// The pages are committed so the site builds without .NET; --check keeps them in step with the source. Descriptions
// come from the XML comments (`///`) of the public API: improve a page by improving the comment.
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const out = path.join(repo, 'docs', 'api');
const check = process.argv.includes('--check');
const assemblies = ['MicroEngine.Kernel', 'MicroEngine', 'MicroEngine.Sdl', 'MicroEngine.Raylib', 'MicroEngine.Desktop'];
const version = fs.readFileSync(path.join(repo, 'Directory.Build.props'), 'utf8').match(/<MicroEngineVersion>([^<]+)</)[1];

// Where each namespace is explained in the manual: every page of the namespace links its guide.
const guides = {
  'MicroEngine': ['Graphics', '../manual/rendering.md'],
  'MicroEngine.Audio': ['Audio', '../manual/audio.md'],
  'MicroEngine.Core': ['Game lifecycle and time', '../manual/lifecycle.md'],
  'MicroEngine.Desktop': ['Backends and platforms', '../manual/backends.md'],
  'MicroEngine.Graphics': ['Graphics', '../manual/rendering.md'],
  'MicroEngine.Input': ['Input', '../manual/input.md'],
  'MicroEngine.Kernel': ['Game lifecycle and time', '../manual/lifecycle.md'],
  'MicroEngine.Raylib': ['Backends and platforms', '../manual/backends.md'],
  'MicroEngine.Scenes': ['Scenes', '../manual/scenes.md'],
  'MicroEngine.Sdl': ['Backends and platforms', '../manual/backends.md'],
  'MicroEngine.UI': ['UI widgets', '../manual/ui.md'],
  'MicroEngine.World': ['Tile maps and collision', '../manual/world.md'],
};
// More precise guides for types whose namespace guide is too broad.
const typeGuides = {
  'MicroEngine.Core.SaveStore': ['Saving data', '../manual/persistence.md'],
  'MicroEngine.Core.ISaveStorage': ['Saving data', '../manual/persistence.md'],
  'MicroEngine.Core.FileSaveStorage': ['Saving data', '../manual/persistence.md'],
  'MicroEngine.Core.ContentFolder': ['Assets', '../manual/assets.md'],
  'MicroEngine.Core.IPlatform': ['Backends and platforms', '../manual/backends.md'],
  'MicroEngine.Core.PlatformProfile': ['Backends and platforms', '../manual/backends.md'],
  'MicroEngine.Core.PlatformCatalog': ['Backends and platforms', '../manual/backends.md'],
  'MicroEngine.Core.PlatformRequest': ['Backends and platforms', '../manual/backends.md'],
  'MicroEngine.Core.HeadlessPlatform': ['Testing your game', '../examples/testing.md'],
  'MicroEngine.Core.InputScript': ['Testing your game', '../examples/testing.md'],
  'MicroEngine.Core.GameLauncher': ['Configuration and launch options', '../reference/configuration.md'],
  'MicroEngine.Core.AppIdentity': ['Name, id, version and icon', '../guide/app-identity.md'],
  'MicroEngine.Graphics.BitmapFont': ['Text', '../manual/text.md'],
  'MicroEngine.UI.AchievementToasts': ['Achievements', '../manual/achievements.md'],
  'MicroEngine.UI.AchievementsScene': ['Achievements', '../manual/achievements.md'],
};
const namespaceSummaries = {
  'MicroEngine': 'Basic value types: colours, rectangles, maths and easing helpers.',
  'MicroEngine.Audio': 'Sound effects and music: synthesis, WAV decoding, the mixer games play sounds through, and the audio device contract.',
  'MicroEngine.Core': 'The game and its loop: `Game`, settings, runners and sessions, launchers and platform composition, scripted input, saves, content files, achievements, application identity, the headless platform.',
  'MicroEngine.Desktop': 'The desktop launcher with the official desktop platforms (SDL 3, raylib, SDL software, headless).',
  'MicroEngine.Graphics': 'Drawing: the renderer contract, images in code and from PNG, sprites and atlases, animation, camera, parallax, particles, light, the pixel font.',
  'MicroEngine.Input': 'Input actions over keyboard, gamepad, mouse and touch: bindings, analog values, on-screen controls, custom sources.',
  'MicroEngine.Kernel': 'The kernel: engine composition (builder, services, modules, capabilities), the frame schedule and its phases, loop-owned time, engine events, logging and generational handles.',
  'MicroEngine.Raylib': 'The raylib platform: desktops and browsers (WebAssembly), with the browser launcher and browser storage.',
  'MicroEngine.Scenes': 'The scene stack: scenes, transitions, coroutines and tweens.',
  'MicroEngine.Sdl': 'The SDL 3 platform: Windows, Linux, macOS, Android and iOS.',
  'MicroEngine.UI': 'Pixel-art widgets: dialogue boxes, menus, typewriter text, themes, achievement banners and list.',
  'MicroEngine.World': 'Tile layers, collision grids, swept movement against tiles, autotiling and plain entities.',
};

function run(command, args, options = {}) {
  const r = spawnSync(command, args, { cwd: repo, encoding: 'utf8', maxBuffer: 64 * 1024 * 1024, ...options });
  if (r.status !== 0) {
    console.error(`${command} ${args.join(' ')} failed:\n${(r.stdout ?? '') + (r.stderr ?? '')}`.trim());
    process.exit(1);
  }
  return r.stdout;
}

// 1. Build the assemblies (their XML documentation files sit next to them), then let DocFX read them.
const work = fs.mkdtempSync(path.join(os.tmpdir(), 'microengine-api-'));
run('dotnet', ['build', 'src/MicroEngine.Desktop/MicroEngine.Desktop.csproj', '-c', 'Release', '-v', 'q', '-nologo', `-p:OutDir=${path.join(work, 'bin')}${path.sep}`]);
// Without symbols DocFX cannot turn a type of another assembly into a link to its source at the current commit, which
// would make every page change with every commit (and differ in a copy without .git).
for (const file of fs.readdirSync(path.join(work, 'bin'))) if (file.endsWith('.pdb')) fs.rmSync(path.join(work, 'bin', file));
const config = {
  metadata: [{
    src: [{ src: path.join(work, 'bin'), files: assemblies.map(a => `${a}.dll`) }],
    dest: path.join(work, 'md'),
    outputFormat: 'markdown',
    namespaceLayout: 'flattened',
    memberLayout: 'samePage',
  }],
};
fs.writeFileSync(path.join(work, 'docfx.json'), JSON.stringify(config, null, 2));
run('dotnet', ['tool', 'run', 'docfx', 'metadata', path.join(work, 'docfx.json')]);

// 2. Adapt each page.
const generated = fs.readdirSync(path.join(work, 'md')).filter(f => f.endsWith('.md')).sort(); // listing order differs between systems
const names = new Set(generated.map(f => f.slice(0, -3)));
const pages = new Map();

/** `<xref href="MicroEngine.Input.InputMap.Vector(System.String,...)">` becomes a link to the type page, or code. */
function xref(href) {
  const target = decodeURIComponent(href).replace(/\(.*$/, '');
  const parts = target.split('.');
  const label = parts.at(-1);
  for (let n = parts.length; n > 1; n--) {
    const type = parts.slice(0, n).join('.');
    if (names.has(type)) {
      const anchor = n < parts.length ? memberAnchor(type, target.replace(/[.`]/g, '_')) : null;
      return `[${label}](${type}.md${anchor ? `#${anchor}` : ''})`;
    }
  }
  return `\`${label}\``;
}
// DocFX ids members as Namespace_Type_Member, followed by the parameter types for methods: the first id that starts
// with the member's name is its first overload.
const pageCache = new Map();
function memberAnchor(type, prefix) {
  if (!pageCache.has(type)) pageCache.set(type, fs.readFileSync(path.join(work, 'md', `${type}.md`), 'utf8'));
  const ids = [...pageCache.get(type).matchAll(/id="([^"]+)"/g)].map(m => m[1]);
  return ids.find(id => id === prefix) ?? ids.find(id => id.startsWith(`${prefix}_`)) ?? null;
}

for (const file of generated) {
  const name = file.slice(0, -3);
  let text = fs.readFileSync(path.join(work, 'md', file), 'utf8').replace(/\r\n/g, '\n');
  text = text.replace(/^# <a id="[^"]*"><\/a>\s*/m, '# ');
  text = text.replace(/<xref href="([^"]+)"[^>]*><\/xref>/g, (_, href) => xref(href));
  // <example><code> blocks arrive as raw HTML; as fenced blocks they get the site's highlighting and scroll sideways on
  // small screens instead of widening the page.
  text = text.replace(/<pre><code class="lang-(\w+)">([\s\S]*?)<\/code><\/pre>/g, (_, lang, code) =>
    `\`\`\`${lang}\n${code.replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&quot;/g, '"').replace(/&#39;/g, "'").replace(/&amp;/g, '&').trimEnd()}\n\`\`\``);
  // Inherited members that every .NET object has (ToString, Equals...) say nothing about the engine.
  text = text.replace(/#### Inherited Members\n\n((?:\[object\.[^\n]*\n)+)\n?/g, '');
  const namespace = /^# Namespace /.test(text) ? name : [...Object.keys(guides)].filter(ns => name.startsWith(`${ns}.`)).sort((a, b) => b.length - a.length)[0];
  const guide = typeGuides[name] ?? guides[namespace];
  if (guide) text = text.replace(/^(Assembly: [^\n]*\n|# Namespace [^\n]*\n)/m, m => `${m}Guide: [${guide[0]}](${guide[1]})  \n`);
  if (/^# Namespace /.test(text) && namespaceSummaries[name]) text = text.replace(/^(# Namespace [^\n]*\n)/, `$1\n${namespaceSummaries[name]}\n\n`);
  // Search: a type page repeats many words of the guides (Touch, Control, Save); weighting its text down keeps the
  // tutorial and the manual above it, while its title still finds it.
  text = text.replace(/^(# [^\n]*\n)([\s\S]*)$/, (_, h1, body) => `${h1}\n<div data-pagefind-weight="0.3">\n\n${body.trim()}\n\n</div>\n`);
  pages.set(file, text.trimEnd() + '\n');
}

const namespaces = generated.map(f => f.slice(0, -3)).filter(n => /^# Namespace /.test(pages.get(`${n}.md`)));
pages.set('README.md', `# API reference

The public types of the MicroEngine SDK ${version}, one page per namespace and per type: signatures, parameters, default
values and the descriptions written in the engine's source. It covers the four engine packages (\`MicroEngine\`,
\`MicroEngine.Sdl\`, \`MicroEngine.Raylib\`, \`MicroEngine.Desktop\`); the \`microengine\` tool is documented in
[The microengine tool](../guide/cli.md).

This reference says what each member is. How the pieces work together, with examples, is in the
[engine manual](../manual/lifecycle.md); every page here links to its chapter under "Guide". New to MicroEngine? Start
with the [tutorial](../tutorial/README.md).

| Namespace | Contains | Guide |
| --- | --- | --- |
${namespaces.map(n => `| [${n}](${n}.md) | ${namespaceSummaries[n] ?? ''} | ${guides[n] ? `[${guides[n][0]}](${guides[n][1]})` : ''} |`).join('\n')}

## Reading a page

- **Signatures** are C#. Optional parameters show their default value (\`float fadeSeconds = 0.4\`); \`?\` marks a value
  that may be null.
- **Records** (\`GameSettings\`, \`SfxParams\`, \`TouchLayout\`...) are configured with object initialisers and copied with
  \`with\`: \`new GameSettings("My Game") { Background = Color.Hex("1a1c2c") }\`.
- **Examples** on type pages are short fragments that name your own classes (\`TitleScene\`, \`player\`); complete programs
  that compile and run are in the [focused examples](../examples/focused.md) and the [tutorial](../tutorial/README.md).
- **Platform types** (\`SdlPlatform\`, \`RaylibPlatform\`, \`HeadlessPlatform\`) implement the same \`IPlatform\` contract; a game
  never names them, its launcher does ([Backends and platforms](../manual/backends.md)).
- The SDK is in Alpha: these APIs may change between 0.x versions, and every such change is listed in the
  [migration notes](../migrations.md).
`);

// 3. Write, or compare.
if (check) {
  const current = fs.existsSync(out) ? fs.readdirSync(out).filter(f => f.endsWith('.md')) : [];
  const differs = [...new Set([...current, ...pages.keys()])].filter(f =>
    !pages.has(f) || !current.includes(f) || fs.readFileSync(path.join(out, f), 'utf8').replace(/\r\n/g, '\n') !== pages.get(f));
  fs.rmSync(work, { recursive: true, force: true });
  if (differs.length) {
    console.error(`error: docs/api is out of date (${differs.length} pages: ${differs.slice(0, 5).join(', ')}${differs.length > 5 ? '...' : ''}); run node site/scripts/api-reference.mjs`);
    process.exit(1);
  }
  console.log(`docs/api: ${pages.size} pages, up to date`);
} else {
  fs.rmSync(out, { recursive: true, force: true });
  fs.mkdirSync(out, { recursive: true });
  for (const [file, text] of pages) fs.writeFileSync(path.join(out, file), text);
  fs.rmSync(work, { recursive: true, force: true });
  console.log(`docs/api: ${pages.size} pages written`);
}
