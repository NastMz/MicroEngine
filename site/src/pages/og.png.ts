// Open Graph image, made at build time from a real game screenshot (no second copy in the repository).
import fs from 'node:fs';
import path from 'node:path';
import sharp from 'sharp';
import { repoRoot } from '../lib/repo-docs.mjs';

export async function GET() {
  const source = fs.readFileSync(path.join(repoRoot, 'games/NeonRift/screenshots/01-title.png'));
  const png = await sharp(source).resize(1200, 630, { fit: 'cover', kernel: 'nearest' }).png().toBuffer();
  return new Response(new Uint8Array(png), { headers: { 'Content-Type': 'image/png' } });
}
