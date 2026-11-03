// Compares two capture folders byte for byte (as web/tests/desktop.mjs does).
//
//   node diffcaps.mjs A_DIR B_DIR
import fs from 'node:fs';
import path from 'node:path';

const [a, b] = process.argv.slice(2);
const files = dir => fs.existsSync(dir) ? fs.readdirSync(dir).filter(f => f.endsWith('.png')).sort() : [];
const left = files(a), right = new Set(files(b));
const differing = left.filter(f => !right.has(f) || !fs.readFileSync(path.join(a, f)).equals(fs.readFileSync(path.join(b, f))));
console.log(`${left.length - differing.length}/${left.length} captures identical${differing.length ? ` (differ: ${differing.join(', ')})` : ''}`);
