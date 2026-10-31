// The demo packages included in this build (written by scripts/demos.mjs fetch), or null when the site was built
// without them (SITE_DEMOS=none).
import fs from 'node:fs';
import path from 'node:path';
import { repoRoot } from './repo-docs.mjs';

const file = path.join(repoRoot, 'site', 'public', 'play', 'demos.json');

export function playManifest() {
  return fs.existsSync(file) ? JSON.parse(fs.readFileSync(file, 'utf8')) : null;
}

export const megabytes = bytes => `${(bytes / 1048576).toFixed(1)} MB`;
