// Versions and download targets as the build system defines them, read at build time so the site never repeats them:
// the SDK version from Directory.Build.props, each demo's version from its project file, the downloadable targets
// from packaging/targets.json (packaging/README.md).
import fs from 'node:fs';
import path from 'node:path';
import { repoRoot } from './repo-docs.mjs';

const property = (file, name) => {
  const value = fs.readFileSync(path.join(repoRoot, file), 'utf8').match(new RegExp(`<${name}>([^<$]+)</${name}>`))?.[1];
  if (!value) throw new Error(`${file} has no <${name}>`);
  return value.trim();
};

/** The SDK version, for example `0.2.0-alpha`. */
export const sdkVersion = property('Directory.Build.props', 'MicroEngineVersion');

/** `0.2` from `0.2.0-alpha`: the Alpha series a version belongs to. */
export const sdkSeries = sdkVersion.split('.').slice(0, 2).join('.');

/** A demo's own version, from games/<Id>/<Id>.csproj. */
export const gameVersion = id => property(`games/${id}/${id}.csproj`, 'Version');

/** The downloadable targets (id, label, detail, requirements, install steps, archive). */
export const { targets } = JSON.parse(fs.readFileSync(path.join(repoRoot, 'packaging', 'targets.json'), 'utf8'));

/** Install steps of a target with the game's id and title filled in (packaging/targets.mjs does the same). */
export const installSteps = (target, id, title) => target.install.map(step => step.replaceAll('<Game>', id).replaceAll('<Title>', title));
