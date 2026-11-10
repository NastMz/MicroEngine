#!/usr/bin/env node
// Builds and publishes MicroEngine's films (media/README.md). Nothing here runs in CI: a film is rebuilt when its
// sources change or a new one is made, by a person, on a machine with the .NET SDK, Node, FFmpeg and Chrome.
//
//   node media/tools/films.mjs prepare [slug]   fonts and brand files into each film's assets/ (from site/)
//   node media/tools/films.mjs audio [slug]     the score and effects, rendered by MicroEngine.Audio
//   node media/tools/films.mjs footage [slug]   real play, captured from the demos' scripted runs
//   node media/tools/films.mjs render [slug]    check, render the master, encode the web file and the poster
//   node media/tools/films.mjs build [slug]     all of the above, in that order
//   node media/tools/films.mjs publish          upload renders/<slug>.mp4 to the films release (needs gh, signed in)
//
// Without a slug a command runs for every film in media/films.json. Everything written is ignored by Git
// (media/.gitignore): fonts, brand files, audio, footage and renders are all regenerated from sources.
import { execFileSync } from 'node:child_process';
import { copyFileSync, existsSync, mkdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { capture } from './capture-footage.mjs';
import { embedScore } from './embed-score.mjs';

const media = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const repo = resolve(media, '..');
const catalog = JSON.parse(readFileSync(join(media, 'films.json'), 'utf8'));
// The HyperFrames CLI every film was made with; a newer one may lay out or encode differently.
const HYPERFRAMES = 'hyperframes@0.9.30';

const run = (command, args, options = {}) => execFileSync(command, args, { stdio: 'inherit', shell: process.platform === 'win32' && command === 'npx', ...options });
const filmsFor = slug => {
  const films = slug ? catalog.films.filter(f => f.slug === slug) : catalog.films;
  if (!films.length) throw new Error(`No film '${slug}' in media/films.json.`);
  return films;
};

/** Fonts from the site's own packages and the brand files from the site: a film never carries its own copies. */
function prepare(film) {
  const assets = join(media, film.slug, 'assets');
  const fonts = [
    ['bricolage-grotesque', 'bricolage-grotesque-latin-600-normal.woff2'],
    ['ibm-plex-mono', 'ibm-plex-mono-latin-400-normal.woff2'], ['ibm-plex-mono', 'ibm-plex-mono-latin-500-normal.woff2'],
    ['ibm-plex-sans', 'ibm-plex-sans-latin-400-normal.woff2'], ['ibm-plex-sans', 'ibm-plex-sans-latin-600-normal.woff2'],
  ];
  mkdirSync(join(assets, 'fonts'), { recursive: true });
  mkdirSync(join(assets, 'brand'), { recursive: true });
  for (const [family, file] of fonts) {
    const source = join(repo, 'site', 'node_modules', '@fontsource', family, 'files', file);
    if (!existsSync(source)) throw new Error(`${source} is missing: run "npm ci" in site/ first.`);
    copyFileSync(source, join(assets, 'fonts', file));
  }
  // The wordmark alone, outlined, in the ink of each ground (the lockup files' own colours).
  const outlines = JSON.parse(readFileSync(join(repo, 'site', 'src', 'assets', 'brand', 'outlines.json'), 'utf8'));
  for (const [name, ink] of [['wordmark-dark', '#f1eee9'], ['wordmark-light', '#171b20']]) {
    writeFileSync(join(assets, 'brand', `${name}.svg`),
      `<svg xmlns="http://www.w3.org/2000/svg" viewBox="114 -75 587 94"><path fill="${ink}" transform="translate(114 0)" d="${outlines.wordmark.d}"/></svg>\n`);
  }
  console.log(`[${film.slug}] fonts and brand files are in place`);
}

function audio(film) {
  const dir = join(media, film.slug);
  const args = ['run', '--project', join(dir, 'audio'), '-c', 'Release', '--', join(dir, 'assets', 'audio')];
  // "Every step" draws its own score: the program also writes the steps, which are then embedded in the film.
  if (film.slug === 'every-step') args.push(join(dir, 'assets', 'score.json'));
  run('dotnet', args);
  if (film.slug === 'every-step') embedScore(dir);
}

function render(film) {
  const dir = join(media, film.slug);
  const renders = join(dir, 'renders');
  mkdirSync(renders, { recursive: true });
  const master = join(renders, `${film.slug}.master.mp4`), web = join(renders, `${film.slug}.mp4`), poster = join(renders, `${film.slug}.png`);
  run('npx', ['--yes', HYPERFRAMES, 'check'], { cwd: dir });
  run('npx', ['--yes', HYPERFRAMES, 'render', '--quality', 'delivery', '-o', master], { cwd: dir });
  // The file the site plays: H.264 High in yuv420p with AAC, the index at the front so it starts before it has
  // downloaded. The master stays local.
  run('ffmpeg', ['-y', '-loglevel', 'error', '-i', master, '-c:v', 'libx264', '-preset', 'slow', '-crf', '21', '-profile:v', 'high',
    '-pix_fmt', 'yuv420p', '-g', '60', '-c:a', 'aac', '-b:a', '160k', '-movflags', '+faststart', web]);
  run('ffmpeg', ['-y', '-loglevel', 'error', '-ss', String(film.poster), '-i', master, '-frames:v', '1', '-vf', 'scale=1280:720', poster]);
  const mb = file => (statSync(file).size / 1e6).toFixed(1);
  console.log(`[${film.slug}] master ${mb(master)} MB, web ${mb(web)} MB, poster ${film.slug}.png`);
}

/** The films live in one GitHub release of their own, never "latest": demo and SDK releases are other series. */
function publish() {
  const { release, repository } = catalog;
  const files = catalog.films.map(film => join(media, film.slug, 'renders', `${film.slug}.mp4`));
  for (const file of files) if (!existsSync(file)) throw new Error(`${file} is missing: run "films.mjs build" first.`);
  let exists = true;
  try { execFileSync('gh', ['release', 'view', release, '--repo', repository], { stdio: 'ignore' }); } catch { exists = false; }
  if (!exists) {
    run('gh', ['release', 'create', release, '--repo', repository, '--title', 'MicroEngine films', '--latest=false',
      '--notes', 'The films shown on the MicroEngine site. Not a release of the engine or of the demos. All rights reserved (media/LICENSE).']);
  }
  run('gh', ['release', 'upload', release, ...files, '--repo', repository, '--clobber']);
  for (const film of catalog.films) console.log(`https://github.com/${repository}/releases/download/${release}/${film.slug}.mp4`);
}

const [command, slug] = process.argv.slice(2);
const steps = { prepare, audio, footage: film => capture(film, { media, repo }), render };
if (command === 'publish') publish();
else if (command === 'build') for (const film of filmsFor(slug)) for (const step of Object.values(steps)) step(film);
else if (steps[command]) for (const film of filmsFor(slug)) steps[command](film);
else {
  console.error('Usage: node media/tools/films.mjs prepare|audio|footage|render|build [slug]  |  publish');
  process.exit(2);
}
