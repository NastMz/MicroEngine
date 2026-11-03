// Makes a scratch copy of the repository with the state probe (StateProbe.cs) wired into the game loop.
// The repository itself is never changed.
//
//   node experiments/arch-1/browser-determinism/apply-probe.mjs --out DIR [--ref REF] [--scripted-fixed-step]
//
// --ref                  the commit to copy (default HEAD), through `git archive`
// --scripted-fixed-step  also applies the candidate fix: a scripted run advances one step per frame and draws at
//                        FrameBlend 1 on every platform, as headless and desktop scripted runs already do
//
// Then build the copy as usual: `dotnet build DIR/MicroEngine.slnx -c Release` for desktop, `DIR/web/build.sh --dev
// --out DIR/dist/web-dev <Game>` for the browser.
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const repo = path.resolve(here, '../../..');
const args = process.argv.slice(2);
const option = (name, fallback) => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : fallback; };
const out = option('--out', '');
if (!out) throw new Error('--out DIR is required (a scratch folder outside the repository)');
const target = path.resolve(out);
if (target.startsWith(repo + path.sep)) throw new Error('--out must be outside the repository');
const ref = option('--ref', 'HEAD');
const fixed = args.includes('--scripted-fixed-step');

fs.rmSync(target, { recursive: true, force: true });
fs.mkdirSync(target, { recursive: true });
const archive = path.join(target, '..', `${path.basename(target)}.tar`);
execFileSync('git', ['-C', repo, 'archive', '--format=tar', '-o', archive, ref]);
// Relative paths: GNU tar reads "C:/..." as a remote host.
execFileSync('tar', ['-xf', path.join('..', path.basename(archive))], { cwd: target });
fs.rmSync(archive);

fs.copyFileSync(path.join(here, 'StateProbe.cs'), path.join(target, 'src/MicroEngine/Core/StateProbe.cs'));

const runner = path.join(target, 'src/MicroEngine/Core/GameRunner.cs');
// Archives may carry CRLF line ends (core.autocrlf); the patterns below use LF.
let code = fs.readFileSync(runner, 'utf8').replaceAll('\r\n', '\n');
function replace(from, to) {
  if (!code.includes(from)) throw new Error(`GameRunner.cs changed: cannot find ${JSON.stringify(from)}`);
  code = code.replace(from, to);
}
// One line of hashes after every fixed step, one line per frame, one per capture.
replace('            steps++;\n', '            steps++;\n            StateProbe.Step(_game, _frames, steps);\n');
replace('        return (stop, capture);\n    }', '        StateProbe.Frame(_game, _frames, steps);\n        return (stop, capture);\n    }');
replace('            _platform.SaveScreenshot(path);\n', '            StateProbe.Capture(_game, capture);\n            _platform.SaveScreenshot(path);\n');
if (fixed) {
  // Candidate fix: a script is a fixed-step run whatever the platform's clock says.
  replace('        if (_platform.RealTime)\n        {\n            double now = _platform.Now;',
    '        if (_platform.RealTime && _options.Script is null)\n        {\n            double now = _platform.Now;');
  replace('_game.FrameBlend = _platform.RealTime ? ', '_game.FrameBlend = _platform.RealTime && _options.Script is null ? ');
}
fs.writeFileSync(runner, code);
console.log(`probe copy of ${ref} in ${target}${fixed ? ' (with the scripted fixed-step fix)' : ''}`);
