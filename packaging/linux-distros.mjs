#!/usr/bin/env node
// Plays the Linux packages in containers of other distributions (packaging/README.md, "Linux distributions"): a
// package built on one system is not assumed to work on every distribution. Each image installs only what a desktop
// already has (an X server, Mesa's OpenGL, a sound server), extracts the archives as a player would, runs every
// game's scripted scenarios with the package's own executable, and the captures are compared on the host with the
// SDL Linux reference. An image whose glibc is older than the one promised in packaging/targets.json is expected to
// refuse to start, which checks the promise from the other side.
//
//   node packaging/linux-distros.mjs --packages DIR [--images debian:12,ubuntu:22.04] [--out DIR]
//
// DIR holds <Game>-linux-x64.tar.gz files (package.mjs output). Needs Docker.
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { repoRoot, target } from './targets.mjs';

const args = process.argv.slice(2);
const option = (name, fallback) => { const i = args.indexOf(`--${name}`); return i >= 0 ? args[i + 1] : fallback; };
const packages = path.resolve(option('packages') ?? (console.error('--packages DIR is required'), process.exit(2)));
const out = path.resolve(option('out', path.join(repoRoot, 'dist', 'linux-distros')));
const t = target('linux-x64');

// Image, its glibc, and how it installs an X server, Mesa and PulseAudio.
const apt = 'apt-get update -qq && DEBIAN_FRONTEND=noninteractive apt-get install -y -qq --no-install-recommends xvfb xauth libgl1-mesa-dri libegl1 libglx-mesa0 pulseaudio ca-certificates > /dev/null';
const dnf = 'dnf install -y -q xorg-x11-server-Xvfb xorg-x11-xauth mesa-dri-drivers mesa-libEGL mesa-libGL pulseaudio which > /dev/null';
const images = {
  'ubuntu:22.04': { glibc: '2.35', install: apt },
  'debian:12': { glibc: '2.36', install: apt },
  'fedora:41': { glibc: '2.40', install: dnf },
  'debian:11': { glibc: '2.31', install: apt },
};
const selected = (option('images') ?? Object.keys(images).join(',')).split(',');
const { scenarios } = JSON.parse(fs.readFileSync(path.join(repoRoot, 'web/tests/scenarios.json'), 'utf8'));
const saveDir = { MeadowRun: 'MEADOW_SAVE_DIR', CourtyardKey: 'COURTYARD_SAVE_DIR', TrailDuel: 'TRAIL_SAVE_DIR', NeonRift: 'RIFT_SAVE_DIR' };
const games = [...new Set(scenarios.map(s => s.game))].filter(g => fs.existsSync(path.join(packages, `${g}-linux-x64.tar.gz`)));

// One shell script per image: extract, report, then every scenario (env as in desktop.mjs).
const quote = s => `'${String(s).replace(/'/g, `'\\''`)}'`;
const play = [
  'set -u', 'export XDG_RUNTIME_DIR=/tmp/xdg; mkdir -p $XDG_RUNTIME_DIR; chmod 700 $XDG_RUNTIME_DIR',
  'pulseaudio --start --exit-idle-time=-1 2> /dev/null || echo "note: no sound server"',
  'echo "glibc $(ldd --version | head -1 | grep -oE "[0-9]+\\.[0-9]+$")"',
  ...games.flatMap(g => [
    `mkdir -p /tmp/p && tar -xzf /packages/${g}-linux-x64.tar.gz -C /tmp/p`,
    `echo "== ${g}"; /tmp/p/${g}/${g} --report --hidden --audio device --frames 60 2>&1 | grep -E "^(platform|audio|done|error)" | head -4; echo "exit $?"`,
    ...scenarios.filter(s => s.game === g).map(s => {
      const name = path.basename(s.script, '.script');
      const env = Object.entries({ ...s.env, [saveDir[g]]: `/tmp/saves-${name}` }).map(([k, v]) => `${k}=${quote(v)}`).join(' ');
      return `mkdir -p /out/desktop/${g}/${name} && env ${env} /tmp/p/${g}/${g} --script /scripts/${g}/${s.script} --hidden --mute --platform sdl --captures /out/desktop/${g}/${name} > /out/${g}-${name}.log 2>&1; echo "${g} ${name}: exit $?"`;
    }),
  ]),
].join('\n');

let failures = 0;
const rows = [];
for (const image of selected) {
  const spec = images[image] ?? (console.error(`unknown image ${image}`), process.exit(2));
  const dir = path.join(out, image.replace(/[:/]/g, '-'));
  fs.rmSync(dir, { recursive: true, force: true });
  fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(path.join(dir, 'play.sh'), `${play}\n`);
  const mounts = ['-v', `${packages}:/packages:ro`, '-v', `${dir}:/out`, ...games.flatMap(g => ['-v', `${path.join(repoRoot, 'games', g, 'scripts')}:/scripts/${g}:ro`])];
  const expectStart = spec.glibc.localeCompare(t.glibc, 'en', { numeric: true }) >= 0;
  // On an older glibc nothing needs installing: starting the game loads SDL, which must fail on a missing glibc symbol
  // version with the launcher's error. Anything else (no package, a crash, an install error) is not that evidence.
  const game = games[0];
  const command = expectStart ? `${spec.install} && xvfb-run -a -s "-screen 0 1280x720x24" bash /out/play.sh`
    : `mkdir -p /tmp/p && tar -xzf /packages/${game}-linux-x64.tar.gz -C /tmp/p && /tmp/p/${game}/${game} --frames 1 --mute; echo "exit $?"`;
  const run = spawnSync('docker', ['run', '--rm', '--init', ...mounts, image, 'bash', '-c', command], { encoding: 'utf8', maxBuffer: 64 * 1024 * 1024 });
  const log = `${run.stdout}${run.stderr}`;
  fs.writeFileSync(path.join(dir, 'container.log'), log);
  const started = /^platform: sdl/m.test(log);
  const audio = [...log.matchAll(/^audio: (\w+)/gm)].map(m => m[1]);
  let result;
  if (!expectStart) {
    const refused = /^error: platform 'sdl' could not load its native library/m.test(log) && /GLIBC_[\d.]+' not found/.test(log) && /^exit 1$/m.test(log);
    result = refused ? `PASS: SDL refused to load, as promised (glibc ${spec.glibc} < ${t.glibc}): ${log.match(/GLIBC_[\d.]+' not found/)?.[0]}`
      : `FAIL: expected SDL to report a missing glibc version; got: ${log.trim().split('\n').slice(-3).join(' | ')}`;
    if (!refused) failures++;
  } else {
    const compare = spawnSync('node', [path.join(repoRoot, 'web/tests/desktop.mjs'), '--skip-run', '--games', games.join(','), '--out', dir, '--reference', path.join(repoRoot, t.reference)], { encoding: 'utf8' });
    const summary = compare.stdout.trim().split('\n').pop();
    const ok = started && compare.status === 0;
    if (!ok) failures++;
    result = `${ok ? 'PASS' : 'FAIL'}: ${started ? 'started on SDL' : 'did not start'}; audio ${audio.join('/') || 'not reported'}; ${summary}`;
  }
  rows.push(`| ${image} | ${spec.glibc} | ${result} |`);
  console.log(`${image} (glibc ${spec.glibc}): ${result}`);
}
const table = ['### Linux packages on other distributions', '', `Games: ${games.join(', ')}; X server Xvfb, Mesa, PulseAudio inside each container.`, '',
  '| Image | glibc | Result |', '| --- | --- | --- |', ...rows, ''].join('\n');
if (process.env.GITHUB_STEP_SUMMARY) fs.appendFileSync(process.env.GITHUB_STEP_SUMMARY, `${table}\n`);
fs.writeFileSync(path.join(out, 'summary.md'), table);
process.exit(failures ? 1 : 0);
