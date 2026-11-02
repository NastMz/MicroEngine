// ARCH-1 P1: side-by-side crop of two captures, scaled, with the pixels whose largest RGB difference exceeds a threshold
// in white (used for images/amd-vs-nvidia-hearts.png).
//
//   node crop.mjs A.png B.png OUT.png X Y W H SCALE THRESHOLD
import fs from 'node:fs';
import zlib from 'node:zlib';
import { decodePng } from '../../../web/tests/captures.mjs';
const [A, B, out, x0, y0, w, h, s, t] = process.argv.slice(2);
const [X, Y, W, H, S, T] = [x0, y0, w, h, s, t].map(Number);
const a = decodePng(fs.readFileSync(A)), b = decodePng(fs.readFileSync(B));
const OW = W * 3 * S + 8, OH = H * S;
const img = Buffer.alloc(OW * OH * 4, 128);
let big = [];
for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
  const i = ((Y + y) * a.width + X + x) * 4;
  let d = 0; for (let k = 0; k < 3; k++) d = Math.max(d, Math.abs(a.rgba[i + k] - b.rgba[i + k]));
  if (d > T) big.push([X + x, Y + y, [...a.rgba.subarray(i, i + 4)], [...b.rgba.subarray(i, i + 4)]]);
  for (let sy = 0; sy < S; sy++) for (let sx = 0; sx < S; sx++) {
    const put = (ox, c) => { const o = ((y * S + sy) * OW + ox + x * S + sx) * 4; img[o] = c[0]; img[o + 1] = c[1]; img[o + 2] = c[2]; img[o + 3] = 255; };
    put(0, a.rgba.subarray(i, i + 4)); put(W * S + 4, b.rgba.subarray(i, i + 4)); put(2 * W * S + 8, d > T ? [255, 255, 255] : [0, 0, 0]);
  }
}
const crcT = Array.from({ length: 256 }, (_, n) => { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; return c >>> 0; });
const crc = buf => { let c = 0xffffffff; for (const v of buf) c = crcT[(c ^ v) & 0xff] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; };
const chunk = (ty, body) => { const hd = Buffer.alloc(8); hd.writeUInt32BE(body.length); hd.write(ty, 4, 'latin1'); const tl = Buffer.alloc(4); tl.writeUInt32BE(crc(Buffer.concat([hd.subarray(4), body]))); return Buffer.concat([hd, body, tl]); };
const ih = Buffer.alloc(13); ih.writeUInt32BE(OW); ih.writeUInt32BE(OH, 4); ih.set([8, 6, 0, 0, 0], 8);
const raw = Buffer.alloc((OW * 4 + 1) * OH); for (let y = 0; y < OH; y++) img.copy(raw, y * (OW * 4 + 1) + 1, y * OW * 4, (y + 1) * OW * 4);
fs.writeFileSync(out, Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ih), chunk('IDAT', zlib.deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]));
console.log(big.length, 'pixels >', T, JSON.stringify(big.slice(0, 6)));
