// A local stand-in for Vercel's static hosting, used by the browser checks: serves site/dist with the headers from
// vercel.json, trailing-slash redirects, directory index files and the 404 page. It approximates the platform; it
// is not Vercel (no edge compression, no CDN caching).
import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const siteRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const types = {
  '.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.mjs': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8', '.json': 'application/json', '.wasm': 'application/wasm', '.png': 'image/png',
  '.webp': 'image/webp', '.svg': 'image/svg+xml', '.xml': 'application/xml', '.txt': 'text/plain; charset=utf-8',
  '.wav': 'audio/wav', '.ogg': 'audio/ogg', '.woff2': 'font/woff2', '.woff': 'font/woff', '.dat': 'application/octet-stream',
};

/** vercel.json `source` patterns as regular expressions (the subset this site uses: literals and regex groups). */
export function headerRules(config) {
  return (config.headers ?? []).map(rule => ({ pattern: new RegExp(`^${rule.source}$`), headers: rule.headers }));
}

export function headersFor(rules, pathname) {
  const result = {};
  for (const rule of rules) if (rule.pattern.test(pathname)) for (const { key, value } of rule.headers) result[key] = value;
  return result;
}

export function serve({ dir = path.join(siteRoot, 'dist'), port = 0 } = {}) {
  const rules = headerRules(JSON.parse(fs.readFileSync(path.join(siteRoot, 'vercel.json'), 'utf8')));
  const requests = [];
  const server = http.createServer((request, response) => {
    const url = new URL(request.url, 'http://localhost');
    const pathname = decodeURIComponent(url.pathname);
    requests.push(pathname);
    let file = path.join(dir, pathname);
    if (!file.startsWith(dir)) return end(response, 403);
    const isDir = fs.existsSync(file) && fs.statSync(file).isDirectory();
    if (isDir && !pathname.endsWith('/')) return end(response, 308, { Location: `${pathname}/${url.search}` });
    if (!isDir && !path.extname(pathname) && !pathname.endsWith('/')) return end(response, 308, { Location: `${pathname}/${url.search}` });
    if (isDir) file = path.join(file, 'index.html');
    let status = 200;
    if (!fs.existsSync(file)) { status = 404; file = path.join(dir, '404.html'); }
    response.writeHead(status, {
      'Content-Type': types[path.extname(file)] ?? 'application/octet-stream',
      'Cache-Control': 'public, max-age=0, must-revalidate',
      ...headersFor(rules, pathname),
    });
    fs.createReadStream(file).pipe(response);
  });
  return new Promise(resolve => server.listen(port, '127.0.0.1', () => {
    resolve({ server, requests, url: `http://127.0.0.1:${server.address().port}`, close: () => new Promise(r => server.close(r)) });
  }));
}

function end(response, status, headers = {}) {
  response.writeHead(status, { 'Content-Type': 'text/plain; charset=utf-8', ...headers });
  response.end();
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const { url } = await serve({ port: Number(process.argv[2] ?? 4322) });
  console.log(`Serving site/dist at ${url}/`);
}
