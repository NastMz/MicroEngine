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
const assemblies = ['MicroEngine.Kernel', 'MicroEngine.Platform', 'MicroEngine.Graphics', 'MicroEngine.Graphics2D', 'MicroEngine.Text', 'MicroEngine.Text.Shaping', 'MicroEngine.Input', 'MicroEngine.Input.Touch', 'MicroEngine.UI', 'MicroEngine.Hosting', 'MicroEngine.Content', 'MicroEngine.Audio', 'MicroEngine.Scene', 'MicroEngine.Physics', 'MicroEngine.Physics.Tiles', 'MicroEngine.Saves', 'MicroEngine.Achievements', 'MicroEngine', 'MicroEngine.Sdl', 'MicroEngine.Raylib', 'MicroEngine.Desktop'];
const version = fs.readFileSync(path.join(repo, 'Directory.Build.props'), 'utf8').match(/<MicroEngineVersion>([^<]+)</)[1];

// Where each namespace is explained in the manual: every page of the namespace links its guide.
const guides = {
  'MicroEngine': ['Graphics', '../manual/rendering.md'],
  'MicroEngine.Achievements': ['Achievements', '../manual/achievements.md'],
  'MicroEngine.Audio': ['Audio', '../manual/audio.md'],
  'MicroEngine.Audio.Retro': ['Synthesized sound effects', '../manual/audio.md#synthesized-sound-effects'],
  'MicroEngine.Content': ['Assets', '../manual/assets.md#handles-loaders-and-content-sources'],
  'MicroEngine.Core': ['Game lifecycle and time', '../manual/lifecycle.md'],
  'MicroEngine.Desktop': ['Backends and platforms', '../manual/backends.md'],
  'MicroEngine.Graphics': ['Graphics', '../manual/rendering.md'],
  'MicroEngine.Graphics.Devices': ['The graphics device', '../manual/rendering.md#the-graphics-device'],
  'MicroEngine.Hosting': ['Composing a game for every platform', '../manual/backends.md#one-composition-for-every-platform'],
  'MicroEngine.Input': ['Input', '../manual/input.md'],
  'MicroEngine.Kernel': ['Modules and the frame schedule', '../manual/lifecycle.md#modules-and-the-frame-schedule'],
  'MicroEngine.Physics': ['Physics worlds', '../manual/world.md#physics-worlds'],
  'MicroEngine.Physics.Tiles': ['Physics worlds', '../manual/world.md#physics-worlds'],
  'MicroEngine.Platform': ['The platform host', '../manual/backends.md#the-platform-host'],
  'MicroEngine.Raylib': ['Backends and platforms', '../manual/backends.md'],
  'MicroEngine.Saves': ['Persistence', '../manual/persistence.md'],
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
  'MicroEngine.Core.GameHost': ['Composing a game for every platform', '../manual/backends.md#one-composition-for-every-platform'],
  'MicroEngine.Core.HostBuilderExtensions': ['Composing a game for every platform', '../manual/backends.md#one-composition-for-every-platform'],
  'MicroEngine.Core.AppIdentity': ['Name, id, version and icon', '../guide/app-identity.md'],
  'MicroEngine.Graphics.BitmapFont': ['Text', '../manual/text.md'],
  'MicroEngine.Graphics.Renderer2D': ['The 2D batcher and the camera', '../manual/rendering.md#the-2d-batcher-and-the-camera'],
  'MicroEngine.Graphics.IRenderer2D': ['The 2D batcher and the camera', '../manual/rendering.md#the-2d-batcher-and-the-camera'],
  'MicroEngine.Graphics.DeviceCanvas': ['The 2D batcher and the camera', '../manual/rendering.md#the-2d-batcher-and-the-camera'],
  'MicroEngine.Core.GameViews': ['The world view and the UI view', '../manual/rendering.md#the-world-view-and-the-ui-view'],
  'MicroEngine.Core.IScreenView': ['The world view and the UI view', '../manual/rendering.md#the-world-view-and-the-ui-view'],
  'MicroEngine.Graphics.Presentation': ['Presentation', '../manual/rendering.md#presentation-one-choice-per-field'],
  'MicroEngine.Graphics.ScaleRounding': ['Presentation', '../manual/rendering.md#presentation-one-choice-per-field'],
  'MicroEngine.Graphics.AspectMode': ['Presentation', '../manual/rendering.md#presentation-one-choice-per-field'],
  'MicroEngine.Sdl.SdlRendererDevice': ['The graphics device', '../manual/rendering.md#the-graphics-device'],
  'MicroEngine.Raylib.RaylibDevice': ['The graphics device', '../manual/rendering.md#the-graphics-device'],
};
const namespaceSummaries = {
  'MicroEngine': 'Basic value types: colours, rectangles, maths and easing helpers.',
  'MicroEngine.Achievements': 'Achievements as an optional engine module: the module that registers the tracker as a service of the engine and writes its progress.',
  'MicroEngine.Audio': 'Sound effects and music: the one mixer every backend plays through (voices with handles and priorities, buses, music streams, exact-frame scheduling), the sound bank and music games play through, the audio device contract, offline rendering for tests, WAV and OGG decoding, synthesis.',
  'MicroEngine.Audio.Retro': 'Retro sound in code: sound effects described by parameters (in the spirit of sfxr), the synthesizer that renders them, instruments and waveforms, and the tiny step sequencer behind `Song`.',
  'MicroEngine.Content': 'Content: the asset store under Game.Assets (typed handles, scopes that share and release, the loader registry, the reserved import step) and content sources beyond folders (zip archives, files a host lists).',
  'MicroEngine.Core': 'The game and its loop: `Game`, settings, runners and sessions, launchers and platform composition, scripted input, saves, content files, achievements, application identity, the headless platform.',
  'MicroEngine.Desktop': 'The desktop launcher with the official desktop platforms (SDL 3, raylib, SDL software, headless).',
  'MicroEngine.Graphics': 'Drawing: the renderer contract, images in code and from PNG, sprites and atlases, animation, camera, parallax, particles, light, the pixel font.',
  'MicroEngine.Graphics.Devices': 'The graphics device contract every backend implements: capabilities and the typed capability error, textures, render targets, samplers, buffers, pipelines, command lists with passes, readback and the surface target; the core validation backends inherit and the CPU reference device.',
  'MicroEngine.Hosting': 'Hosting: the launcher model every host shares (launch requests and their rules, platform selection, the host environment) and the composition a game configures once with its Configure entry point.',
  'MicroEngine.Input': 'Input: device events with ids (keys, text, pointers and touches, wheel, gamepads), players and device pairing, input layers, and actions over them: bindings and their persistence, analog values, on-screen controls, prompts, custom sources.',
  'MicroEngine.Kernel': 'The kernel: engine composition (builder, services, modules, capabilities), the frame schedule and its phases, loop-owned time, engine events, logging and generational handles.',
  'MicroEngine.Physics': 'The 2D physics contract: worlds of bodies owned by entities, box shapes with collision layers, collide-and-slide moves with their contacts, ray and overlap queries, sensors with buffered events, declared capabilities and the typed capability error, and the module that steps worlds and writes body poses to transforms.',
  'MicroEngine.Physics.Tiles': 'The tile backend of the physics contract: tile grids as static geometry, the exact swept mover, one-way platforms and moving solids that push and carry bodies.',
  'MicroEngine.Platform': 'The platform contracts: the host (clock, event pump, main surface, capabilities, whether it may block), the surface, display pacing and precise waits, storage for saves and content sources.',
  'MicroEngine.Saves': 'Saves as an optional engine module: the module that registers a save store as a service of the engine.',
  'MicroEngine.Raylib': 'The raylib platform for desktops (in the published SDK, also the browser platform).',
  'MicroEngine.Scenes': 'The scene stack (scenes, transitions, coroutines and tweens) and the scene world: entity ids, transforms with a hierarchy, typed attachments, interpolation for drawing and world clocks.',
  'MicroEngine.Sdl': 'The SDL 3 platform: Windows, Linux, macOS, Android, iOS and browsers (WebAssembly), with the browser launcher and the browser page it reads.',
  'MicroEngine.UI': 'Widgets: labels, buttons, panels, stacks, text fields, dialogue boxes, menus, typewriter text, toasts, themes, focus and the UI input layer.',
  'MicroEngine.World': 'Tile layers, collision grids, swept movement against tiles (`TileMotion`), autotiling and drawing tiles.',
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
// The desktop package brings every SDK assembly but the optional shaping package, built beside it.
for (const project of ['src/MicroEngine.Desktop/MicroEngine.Desktop.csproj', 'src/MicroEngine.Text.Shaping/MicroEngine.Text.Shaping.csproj'])
  run('dotnet', ['build', project, '-c', 'Release', '-v', 'q', '-nologo', `-p:OutDir=${path.join(work, 'bin')}${path.sep}`]);
// Without symbols DocFX cannot turn a type of another assembly into a link to its source at the current commit, which
// would make every page change with every commit (and differ in a copy without .git).
for (const file of fs.readdirSync(path.join(work, 'bin'))) if (file.endsWith('.pdb')) fs.rmSync(path.join(work, 'bin', file));
// The same metadata as YAML (`mref`) names every type a page refers to by its full id, which the Markdown does not do
// for types of other assemblies (see linkPlainTypes).
const config = {
  metadata: ['markdown', 'mref'].map(outputFormat => ({
    src: [{ src: path.join(work, 'bin'), files: assemblies.map(a => `${a}.dll`) }],
    dest: path.join(work, outputFormat === 'markdown' ? 'md' : 'yml'),
    outputFormat,
    namespaceLayout: 'flattened',
    memberLayout: 'samePage',
  })),
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
function rawPage(type) {
  if (!pageCache.has(type)) pageCache.set(type, fs.readFileSync(path.join(work, 'md', `${type}.md`), 'utf8'));
  return pageCache.get(type);
}

// DocFX links a type in a signature only when it is declared in the same assembly as the page: a type of another
// engine assembly (IRenderer and Texture on a MicroEngine.dll page, Color on a Graphics2D page) arrives as its plain
// name. Each type has its own page here, so the plain name is linked to it, but only in the lines that hold types
// (parameter, return, value, exception, inheritance and implementation lines), never in descriptions. Two types may
// share a name (Texture in MicroEngine.Graphics and MicroEngine.Graphics.Devices): the YAML of the page, which names
// every referenced type by its full id, decides, first for the member, then for the page; when it cannot, the name
// stays plain rather than pointing to the wrong type.
const typeNames = [...names].filter(name => !/^# <a id="[^"]*"><\/a>\s*Namespace /m.test(rawPage(name)));
const typeSections = new Set(['Parameters', 'Returns', 'Property Value', 'Field Value', 'Event Type', 'Exceptions', 'Implements', 'Inheritance', 'Derived', 'Type Parameters', 'Extension Methods']);
const linkPattern = /\[(?:[^\]\\]|\\.)*\]\([^)]*\)/g;
const yamlCache = new Map();
/** The page's YAML split into one block per id (`- uid: ...`), keyed by the Markdown anchor of that id. */
function yamlBlocks(page) {
  if (!yamlCache.has(page)) {
    const file = path.join(work, 'yml', `${page}.yml`);
    const text = fs.existsSync(file) ? fs.readFileSync(file, 'utf8').replace(/\r\n/g, '\n') : '';
    yamlCache.set(page, { text, blocks: new Map(text.split(/^- uid: /m).slice(1).map(b => [b.slice(0, b.indexOf('\n')).replace(/\W/g, '_'), b])) });
  }
  return yamlCache.get(page);
}
/** Whether YAML names a type (page name `Handle-1` is id `Handle`1`), optionally followed by `.member`. */
const mentions = (yaml, type, member = '') =>
  new RegExp(`(^|[^\\w.\`])${`${type.replace(/-(\d+)/g, '`$1')}${member && `.${member}`}`.replace(/[.`]/g, '\\$&')}([^\\w.\`]|$)`, 'm').test(yaml);
const yamlLists = { Implements: 'implements', Inheritance: 'inheritance', Derived: 'derivedClasses', 'Inherited Members': 'inheritedMembers', 'Extension Methods': 'extensionMethods' };
/** The part of a member's YAML that a Markdown section shows: the matching list for a type's sections, the `type:`
 *  lines (parameters, return value, exceptions) for a member's. */
function yamlScope(block, section) {
  if (yamlLists[section]) return new RegExp(`^  ${yamlLists[section]}:\\n((?:  - .*\\n)+)`, 'm').exec(block)?.[1] ?? '';
  return block.split('\n').filter(l => /^\s*(?:- )?type: /.test(l)).join('\n');
}
/** The page of the type a signature names as `Texture` or `InputMap.StickDirection`, or null. */
function typePage(written, page, member, section, memberName = '') {
  const candidates = typeNames.filter(t => { const key = t.replace(/-\d+/g, ''); return key === written || key.endsWith(`.${written}`); });
  if (candidates.length <= 1) return candidates[0] ?? null;
  const { text, blocks } = yamlBlocks(page);
  for (const yaml of [yamlScope(blocks.get(member) ?? '', section), text]) {
    const found = candidates.filter(c => mentions(yaml, c, memberName));
    if (found.length === 1) return found[0];
  }
  return null;
}
/** Links the plain type names of a line that holds only a type expression (`IReadOnlyList<Texture>`, `RenderTarget?`). */
function linkTypeLine(line, page, member, section) {
  const [, prefix, rest] = /^(`[^`]+` |\s*)(.*)$/.exec(line);
  const plain = rest.replace(linkPattern, '');
  if (!/^[\w\s<>\\?,[\]().*←]*$/.test(plain) || /(^|[^\w.])[a-z_]/.test(plain)) return line; // a description, not a type
  const parts = rest.split(new RegExp(`(${linkPattern.source})`));
  return prefix + parts.map((part, i) => (i % 2 ? part : part.replace(/(?<![\w.])[A-Z]\w*(?:\.[A-Z]\w*)*(?![\w.])/g, (written) => {
    const target = typePage(written, page, member, section);
    return target ? `[${written}](${target}.md)` : written;
  }))).join('');
}
/** `GraphicsDeviceBase.CreateTexture\(in TextureDescription\), ` becomes a link to that member, as DocFX writes it for
 *  members of the page's own assembly. */
function linkInheritedMember(line, page, member) {
  const m = /^([A-Z]\w*)\.(\w+(?:<[^>]*>)?)(\\\(.*?\\\))?(,? ?)$/.exec(line);
  const target = m && typePage(m[1], page, member, 'Inherited Members', m[2].replace(/<.*$/, ''));
  if (!target) return line;
  const heading = [...rawPage(target).matchAll(/^### <a id="([^"]+)"><\/a> (.*)$/gm)].find(h => h[2] === `${m[2]}${m[3] ?? ''}`);
  const anchor = heading?.[1] ?? memberAnchor(target, `${target.replace(/[.-]/g, '_')}_${m[2].replace(/<.*$/, '')}`);
  return `[${m[1]}.${m[2]}${m[3] ?? ''}](${target}.md${anchor ? `#${anchor}` : ''})${m[4]}`;
}
function linkPlainTypes(text, page) {
  let section = null;
  let member = '';
  return text.split('\n').map(line => {
    const heading = /^(#+) (.*)$/.exec(line);
    if (heading) {
      section = heading[1] === '####' ? heading[2] : null;
      if (heading[1].length < 4) member = /<a id="([^"]+)">/.exec(line)?.[1] ?? '';
      return line;
    }
    if (!section || !line.trim() || line.startsWith('[')) return line;
    if (section === 'Inherited Members') return linkInheritedMember(line, page, member);
    return typeSections.has(section) ? linkTypeLine(line, page, member, section) : line;
  }).join('\n');
}

for (const file of generated) {
  const name = file.slice(0, -3);
  let text = fs.readFileSync(path.join(work, 'md', file), 'utf8').replace(/\r\n/g, '\n');
  text = linkPlainTypes(text, name);
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
  // DocFX lists a namespace's nested types in an order that differs between runs and systems: sort each section's
  // entries by their name (ordinal), so the page depends only on the source.
  if (/^# Namespace /.test(text)) text = text.replace(/(^### [^\n]*\n\n)([\s\S]*?)(?=^### |(?![\s\S]))/gm, (_, heading, body) => {
    const entries = body.split(/\n(?= \[)/).map(e => e.replace(/\n+$/, ''));
    entries.sort((x, y) => (x < y ? -1 : x > y ? 1 : 0));
    return `${heading}${entries.join('\n\n')}\n\n`;
  });
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
