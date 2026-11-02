// ARCH-1 P4: download size of a web package, as web/build.sh counts it (every file a browser fetches, raw and with
// the .br copies a server sends), plus the native module (dotnet.native.*.wasm/js) on its own.
//   node sizes.mjs <package-dir> [<package-dir>...]
import fs from 'node:fs';
import path from 'node:path';

function walk(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap(e => e.isDirectory() ? walk(path.join(dir, e.name)) : [path.join(dir, e.name)]);
}
const kb = n => Math.round(n / 1024);
for (const dir of process.argv.slice(2)) {
  let raw = 0, br = 0, nativeRaw = 0, nativeBr = 0;
  for (const f of walk(dir)) {
    if (f.endsWith('.br') || f.endsWith('.gz') || /\.(zip|tar\.gz)$/.test(f)) continue;
    const n = fs.statSync(f).size;
    const b = fs.existsSync(f + '.br') ? fs.statSync(f + '.br').size : n;
    raw += n; br += b;
    if (/dotnet\.native\.[^/\\]+\.(wasm|js)$/.test(f)) { nativeRaw += n; nativeBr += b; }
  }
  console.log(JSON.stringify({ package: dir, totalKB: kb(raw), totalBrotliKB: kb(br), nativeKB: kb(nativeRaw), nativeBrotliKB: kb(nativeBr) }));
}
