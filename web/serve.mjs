// Serves built web packages for local play and tests: node web/serve.mjs [dir] [port]
// (defaults: dist/web, 8080). Sends the precompressed .br files when the browser accepts Brotli, as a
// production server should, and lists the packages at the root. Local use only: binds to 127.0.0.1.
import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';

const root = path.resolve(process.argv[2] ?? 'dist/web');
const port = Number(process.argv[3] ?? 8080);
const types = {
  '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.mjs': 'text/javascript', '.css': 'text/css',
  '.json': 'application/json', '.wasm': 'application/wasm', '.png': 'image/png', '.wav': 'audio/wav',
  '.ogg': 'audio/ogg', '.mp3': 'audio/mpeg', '.txt': 'text/plain; charset=utf-8', '.script': 'text/plain; charset=utf-8',
  '.dat': 'application/octet-stream', '.dll': 'application/octet-stream', '.pdb': 'application/octet-stream',
};

if (!fs.existsSync(root)) {
  console.error(`error: ${root} does not exist. Build packages first with web/build.sh.`);
  process.exit(1);
}

http.createServer((request, response) => {
  const url = new URL(request.url, 'http://localhost');
  let file = path.join(root, decodeURIComponent(url.pathname));
  if (file !== root && !file.startsWith(root + path.sep)) return send(response, 403, 'Forbidden');
  if (fs.existsSync(file) && fs.statSync(file).isDirectory()) {
    if (!url.pathname.endsWith('/')) return send(response, 301, '', { Location: url.pathname + '/' });
    if (file === root && !fs.existsSync(path.join(file, 'index.html'))) return send(response, 200, listing(), { 'Content-Type': types['.html'] });
    file = path.join(file, 'index.html');
  }
  if (!fs.existsSync(file)) return send(response, 404, 'Not found');
  const headers = { 'Content-Type': types[path.extname(file)] ?? 'application/octet-stream', 'Cache-Control': 'no-cache', Vary: 'Accept-Encoding' };
  if (/\bbr\b/.test(request.headers['accept-encoding'] ?? '') && fs.existsSync(file + '.br')) {
    headers['Content-Encoding'] = 'br';
    file += '.br';
  }
  response.writeHead(200, headers);
  fs.createReadStream(file).pipe(response);
}).listen(port, '127.0.0.1', () => console.log(`Serving ${root} at http://127.0.0.1:${port}/`));

function send(response, status, body, headers = {}) {
  response.writeHead(status, { 'Content-Type': 'text/plain; charset=utf-8', ...headers });
  response.end(body);
}

function listing() {
  const games = fs.readdirSync(root).filter(name => fs.existsSync(path.join(root, name, 'game.json')));
  const items = games.map(name => {
    const { title } = JSON.parse(fs.readFileSync(path.join(root, name, 'game.json'), 'utf8'));
    return `<li><a href="${name}/">${title}</a></li>`;
  });
  return `<!doctype html><meta charset="utf-8"><title>MicroEngine web packages</title><h1>MicroEngine web packages</h1><ul>${items.join('')}</ul>`;
}
