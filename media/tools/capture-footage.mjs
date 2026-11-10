// Captures real gameplay footage for the films from the demo games' own deterministic scripted runs.
//
// Each clip reuses the inputs of a script that already lives in games/<Name>/scripts, adds one "capture" command every
// other update (60 updates per second -> 30 frames per second) and lets the game write the frames. FFmpeg then joins
// the frames into a silent 1920x1080 MP4 with nearest-neighbour scaling, so pixels stay crisp. A clip is captured once
// (media/.cache/footage) and copied into the assets of every film that uses it.
//
// Called by films.mjs ("footage"); needs FFmpeg and the .NET SDK on PATH.
import { execFileSync } from 'node:child_process';
import { copyFileSync, existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

// name: game, the script whose inputs are replayed, developer variables, first and last captured update.
const clips = {
  meadow: { game: 'MeadowRun', script: 'showcase.script', env: { MEADOW_START: 'level', MEADOW_FORM: 'bloom' }, from: 20, to: 296 },
  caves: { game: 'MeadowRun', script: 'tour.script', env: { MEADOW_START: 'level', MEADOW_LEVEL: '2' }, from: 40, to: 400 },
  sky: { game: 'MeadowRun', script: 'tour.script', env: { MEADOW_START: 'level', MEADOW_LEVEL: '3' }, from: 40, to: 400 },
  rift: { game: 'NeonRift', script: null, env: { RIFT_START: 'stage', RIFT_AUTO: '1', RIFT_GOD: '1', RIFT_WEAPON: 'Scatter' }, from: 240, to: 720 },
  grotto: { game: 'NeonRift', script: null, env: { RIFT_START: 'stage', RIFT_STAGE: '2', RIFT_AUTO: '1', RIFT_GOD: '1' }, from: 240, to: 720 },
  // The long take that keeps playing on a screen while modules seat beneath it.
  screen: { game: 'NeonRift', script: null, env: { RIFT_START: 'stage', RIFT_AUTO: '1', RIFT_GOD: '1' }, from: 300, to: 1290 },
  // A long, bright take of Meadow Run's first level, for a screen that plays for several bars.
  tour: { game: 'MeadowRun', script: 'tour.script', env: { MEADOW_START: 'level', MEADOW_FORM: 'bloom' }, from: 40, to: 560 },
  warden: { game: 'CourtyardKey', script: 'boss.script', env: { COURTYARD_START: 'boss' }, from: 40, to: 460 },
  duel: { game: 'TrailDuel', script: 'master-battle.script', env: { TRAIL_DEBUG: 'trainer' }, from: 560, to: 1000 },
};

const saveVariable = { MeadowRun: 'MEADOW_SAVE_DIR', NeonRift: 'RIFT_SAVE_DIR', CourtyardKey: 'COURTYARD_SAVE_DIR', TrailDuel: 'TRAIL_SAVE_DIR' };

function captureClip(name, { media, repo }) {
  const clip = clips[name];
  if (!clip) throw new Error(`Unknown clip '${name}'.`);
  const cache = join(media, '.cache', 'footage');
  const output = join(cache, `${name}.mp4`);
  if (existsSync(output)) return output;
  const frames = join(cache, name);
  rmSync(frames, { recursive: true, force: true });
  mkdirSync(frames, { recursive: true });

  // The original inputs, without their own captures and quit, plus the dense captures.
  const inputs = clip.script
    ? readFileSync(join(repo, 'games', clip.game, 'scripts', clip.script), 'utf8').split(/\r?\n/)
        .filter(line => line.trim() && !line.trim().startsWith('#') && !/\s(capture|quit)\b/.test(line))
    : [];
  if (inputs.some(line => line.trim().startsWith('@'))) throw new Error(`${clip.script} uses relative times; resolve them first.`);
  const lines = [...inputs];
  let index = 0;
  for (let update = clip.from; update <= clip.to; update += 2) lines.push(`${update} capture f${String(index++).padStart(4, '0')}.png`);
  lines.push(`${clip.to + 2} quit`);
  const scriptFile = join(frames, 'run.script');
  writeFileSync(scriptFile, lines.join('\n') + '\n');

  console.log(`[${name}] ${clip.game}: ${index} frames`);
  execFileSync('dotnet', ['run', '--project', join(repo, 'games', clip.game), '-c', 'Release', '--',
    '--script', scriptFile, '--hidden', '--mute', '--captures', frames], {
    stdio: ['ignore', 'ignore', 'inherit'],
    env: { ...process.env, ...clip.env, [saveVariable[clip.game]]: join(frames, 'saves') },
  });
  if (!existsSync(join(frames, 'f0000.png'))) throw new Error(`${name}: the run wrote no frames.`);

  execFileSync('ffmpeg', ['-y', '-loglevel', 'error', '-framerate', '30', '-i', join(frames, 'f%04d.png'),
    '-vf', 'scale=1920:1080:flags=neighbor', '-c:v', 'libx264', '-preset', 'slow', '-crf', '12', '-pix_fmt', 'yuv420p',
    '-g', '1', '-movflags', '+faststart', output], { stdio: 'inherit' });
  rmSync(frames, { recursive: true, force: true });
  return output;
}

/** Puts every clip a film uses into its assets/footage, capturing the ones not captured yet. */
export function capture(film, roots) {
  if (!film.footage.length) return console.log(`[${film.slug}] uses no footage`);
  const target = join(roots.media, film.slug, 'assets', 'footage');
  mkdirSync(target, { recursive: true });
  for (const name of film.footage) copyFileSync(captureClip(name, roots), join(target, `${name}.mp4`));
  console.log(`[${film.slug}] footage: ${film.footage.join(', ')}`);
}
