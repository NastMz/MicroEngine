// Images used by the published Markdown (game screenshots), served from their repository files at build time as
// lossless WebP, so the documentation pages show them without a copy in site/.
import fs from 'node:fs';
import path from 'node:path';
import sharp from 'sharp';
import { docImages, docImageUrl, repoRoot } from '../../lib/repo-docs.mjs';

export function getStaticPaths() {
  return docImages().map(source => ({
    params: { image: docImageUrl(source).replace(/^\/docs-assets\//, '') },
    props: { source },
  }));
}

export async function GET({ props }: { props: { source: string } }) {
  const webp = await sharp(fs.readFileSync(path.join(repoRoot, props.source))).webp({ lossless: true, effort: 4 }).toBuffer();
  return new Response(new Uint8Array(webp), { headers: { 'Content-Type': 'image/webp' } });
}
