#!/usr/bin/env node
// Publication freeze (docs/ci.md, "Publication freeze"). While the file .github/phase-freeze exists on a commit,
// nothing automatic publishes from it: daily CI deploys no production site for a push to main (pull request
// previews and every check run as usual), sdk-release stops in preflight before any draft, push or deployment
// (sdk/version.mjs reads the same file), and release refuses (release.yml reads it on the ref and on main). The owner
// lifts it by deleting the file in a pull request.
//
//   node .github/scripts/freeze.mjs deploy --event <push|pull_request> --fork <true|false> --browser <none|layout|full>
//                                          ci's deployment target, printed on stdout
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

export const freezeFile = '.github/phase-freeze';
const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');

/** Why publication is frozen at `root` (the freeze file's first line that is neither blank nor a `#` comment), or null. */
export function freezeReason(root = repoRoot) {
  const file = path.join(root, freezeFile);
  if (!fs.existsSync(file)) return null;
  return fs.readFileSync(file, 'utf8').split(/\r?\n/).map(line => line.trim()).find(line => line && !line.startsWith('#')) ?? 'publication frozen';
}

/**
 * What daily CI deploys (ci.yml, Plan): production for a push to main that changes the published site, unless
 * publication is frozen; a preview for a pull request from this repository that changes the site; otherwise nothing.
 * `browser` is changes.mjs's level. A game's code reaches the site only as a release package, which daily CI never
 * builds (release.yml does), so it never asks for a preview on its own.
 */
export function deployTarget({ event, fork, browser, frozen }) {
  if (event === 'push') return browser !== 'none' && !frozen ? 'production' : 'none';
  if (fork) return 'none';
  return browser !== 'none' ? 'preview' : 'none';
}

function main([command, ...args]) {
  const value = name => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : undefined; };
  if (command !== 'deploy') {
    console.log('usage: freeze.mjs deploy --event <e> --fork <true|false> --browser <none|layout|full>');
    process.exitCode = command ? 2 : 0;
    return;
  }
  const reason = freezeReason();
  const event = value('--event'), browser = value('--browser') ?? 'none';
  console.log(deployTarget({ event, fork: value('--fork') === 'true', browser, frozen: Boolean(reason) }));
  if (reason && event === 'push' && browser !== 'none') console.error(`Production is not deployed: publication is frozen (${freezeFile}: ${reason}).`);
}

if (process.argv[1] && fileURLToPath(import.meta.url) === fs.realpathSync(process.argv[1])) main(process.argv.slice(2));
