// Writes the Tiled fixtures that are binary or encoded (PNGs, the JSON maps, tileset and template):
// node tests/MicroEngine.Tiled.Tests/Maps/generate.mjs tests/MicroEngine.Tiled.Tests/Maps. The XML files are written by hand.
import { deflateSync } from 'node:zlib';
import { writeFileSync } from 'node:fs';
import { join } from 'node:path';
const root = process.argv[2];

function crc32(buf) {
  let crc = 0xffffffff;
  for (const b of buf) {
    let c = (crc ^ b) & 0xff;
    for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    crc = (crc >>> 8) ^ c;
  }
  return (crc ^ 0xffffffff) >>> 0;
}
function chunk(type, data) {
  const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
  const td = Buffer.concat([Buffer.from(type), data]);
  const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(td));
  return Buffer.concat([len, td, crc]);
}
function png(w, h, pixel) {
  const raw = Buffer.alloc((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) raw.set(pixel(x, y), y * (w * 4 + 1) + 1 + x * 4);
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 6;
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr), chunk('IDAT', deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]);
}
// terrain.png: 2 x 2 tiles of 16 px, red, green, blue, yellow, each with a white top-left pixel.
const colors = [[255, 0, 0, 255], [0, 255, 0, 255], [0, 0, 255, 255], [255, 255, 0, 255]];
writeFileSync(join(root, 'tilesets/terrain.png'), png(32, 32, (x, y) => x % 16 === 0 && y % 16 === 0 ? [255, 255, 255, 255] : colors[(y >> 4) * 2 + (x >> 4)]));
// chest.png: 16 x 32, brown, taller than the grid.
writeFileSync(join(root, 'props/chest.png'), png(16, 32, () => [120, 70, 20, 255]));

const H = 0x80000000, V = 0x40000000, D = 0x20000000;
const ground = [(D | 3) >>> 0, 0, 0, 0, 4, 0, 0, 5, 1, (H | 1) >>> 0, 2, (H | V | 2) >>> 0];
const data = Buffer.alloc(ground.length * 4);
ground.forEach((g, i) => data.writeUInt32LE(g, i * 4));
const doorProperties = [
  { name: 'target', type: 'string', value: 'cave' },
  { name: 'locked', type: 'bool', value: true },
  { name: 'cost', type: 'int', value: 3 },
  { name: 'speed', type: 'float', value: 1.5 },
  { name: 'glow', type: 'color', value: '#80ff00ff' },
  { name: 'sprite', type: 'file', value: '../props/chest.png' },
  { name: 'partner', type: 'object', value: 2 },
];
const map = {
  type: 'map', version: '1.10', tiledversion: '1.11.0', orientation: 'orthogonal', renderorder: 'right-down',
  width: 4, height: 3, tilewidth: 16, tileheight: 16, infinite: false, parallaxoriginx: 32, parallaxoriginy: 24, backgroundcolor: '#203040', class: 'dungeon',
  compressionlevel: -1, nextlayerid: 11, nextobjectid: 10,
  properties: [{ name: 'music', type: 'string', value: 'cave' }],
  tilesets: [
    { firstgid: 1, source: '../tilesets/terrain.tsj' },
    { firstgid: 5, name: 'props', tilewidth: 16, tileheight: 32, tilecount: 2, columns: 0, margin: 0, spacing: 0,
      grid: { orientation: 'orthogonal', width: 1, height: 1 },
      tiles: [{ id: 0, type: 'chest', image: '../props/chest.png', imagewidth: 16, imageheight: 32, properties: [{ name: 'bonus', type: 'float', value: 0.5 }, { name: 'kind', type: 'string', value: 'treasure' }, { name: 'loot', type: 'int', value: 10 }] },
        { id: 2, image: '../props/chest.png', imagewidth: 16, imageheight: 32 }] },
  ],
  layers: [
    { type: 'tilelayer', id: 1, name: 'ground', x: 0, y: 0, width: 4, height: 3, opacity: 1, visible: true,
      encoding: 'base64', compression: 'zlib', data: deflateSync(data).toString('base64') },
    { type: 'tilelayer', id: 10, name: 'decor', x: 0, y: 0, width: 4, height: 3, opacity: 1, visible: false,
      data: [0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0] },
    { type: 'group', id: 2, name: 'entities', x: 0, y: 0, offsetx: 8, offsety: 0, opacity: 1, visible: true, layers: [
      { type: 'objectgroup', id: 3, name: 'spawns', x: 0, y: 0, offsetx: 0, offsety: 4, opacity: 1, visible: true, draworder: 'topdown', objects: [
        { id: 1, name: 'door', type: 'door', x: 16, y: 16, width: 16, height: 32, rotation: 90, visible: true, properties: doorProperties },
        { id: 2, name: 'spawn', type: '', point: true, x: 32, y: 40, width: 0, height: 0, rotation: 0, visible: true },
        { id: 3, name: '', type: '', ellipse: true, x: 0, y: 0, width: 8, height: 8, rotation: 0, visible: true },
        { id: 4, name: '', type: '', polygon: [{ x: 0, y: 0 }, { x: 8, y: 0 }, { x: 8, y: 8 }], x: 10, y: 10, width: 0, height: 0, rotation: 0, visible: true },
        { id: 5, name: '', type: '', polyline: [{ x: 0, y: 0 }, { x: 5, y: 5 }], x: 20, y: 20, width: 0, height: 0, rotation: 0, visible: false },
        { id: 6, name: '', type: '', text: { text: 'Hello', wrap: false }, x: 0, y: 0, width: 40, height: 10, rotation: 0, visible: true },
        { id: 7, name: '', type: '', gid: (H | 5) >>> 0, x: 48, y: 48, width: 16, height: 32, rotation: 0, visible: true },
        { id: 9, template: 'parts/torch.tj', x: 0, y: 48 },
        { id: 8, template: '../templates/enemy.tj', x: 24, y: 24, properties: [{ name: 'hp', type: 'int', value: 5 }] },
      ] },
    ] },
    { type: 'imagelayer', id: 9, name: 'sky', x: 0, y: 0, opacity: 0.5, parallaxx: 0.5, parallaxy: 0.25, visible: true, image: '../props/chest.png', imagewidth: 16, imageheight: 32, repeatx: true, repeaty: false },
  ],
};
writeFileSync(join(root, 'maps/level.tmj'), JSON.stringify(map, null, 1));

const tileset = {
  type: 'tileset', version: '1.10', tiledversion: '1.11.0', name: 'terrain', tilewidth: 16, tileheight: 16, tilecount: 4, columns: 2,
  image: 'terrain.png', imagewidth: 32, imageheight: 32, margin: 0, spacing: 0,
  properties: [{ name: 'biome', type: 'string', value: 'cave' }],
  tiles: [
    { id: 0, properties: [{ name: 'collision', type: 'string', value: 'solid' }] },
    { id: 1, properties: [{ name: 'collision', type: 'string', value: 'oneway' }] },
    { id: 2, animation: [{ tileid: 2, duration: 100 }, { tileid: 3, duration: 300 }] },
    { id: 3, type: 'lava', properties: [{ name: 'collision', type: 'string', value: 'solid, hazard' }] },
  ],
};
writeFileSync(join(root, 'tilesets/terrain.tsj'), JSON.stringify(tileset, null, 1));
writeFileSync(join(root, 'maps/parts/torch.tj'), JSON.stringify({ type: 'template', tileset: { firstgid: 1, source: '../../tilesets/terrain.tsj' },
  object: { id: 0, name: 'torch', type: '', gid: 2, x: 0, y: 0, width: 16, height: 16, rotation: 0, visible: true } }, null, 1));
writeFileSync(join(root, 'templates/enemy.tj'), JSON.stringify({ type: 'template', object: {
  id: 0, name: 'enemy', type: 'enemy', x: 0, y: 0, width: 8, height: 8, rotation: 0, visible: true,
  properties: [{ name: 'hp', type: 'int', value: 3 }, { name: 'kind', type: 'string', value: 'slime' }] } }, null, 1));

// An infinite map: two 16 x 16 chunks, one tile at (-1, -1) and one at (2, 1); an object at (0, 0).
const chunkA = new Array(256).fill(0); chunkA[255] = 1;
const chunkB = new Array(256).fill(0); chunkB[1 * 16 + 2] = 2;
writeFileSync(join(root, 'maps/infinite.tmj'), JSON.stringify({
  type: 'map', version: '1.10', tiledversion: '1.11.0', orientation: 'orthogonal', renderorder: 'right-down',
  width: 4, height: 4, tilewidth: 16, tileheight: 16, infinite: true, compressionlevel: -1, nextlayerid: 3, nextobjectid: 2,
  tilesets: [{ firstgid: 1, source: '../tilesets/terrain.tsj' }],
  layers: [
    { type: 'tilelayer', id: 1, name: 'ground', x: 0, y: 0, startx: -16, starty: -16, width: 32, height: 32, opacity: 1, visible: true,
      chunks: [{ x: -16, y: -16, width: 16, height: 16, data: chunkA }, { x: 0, y: 0, width: 16, height: 16, data: chunkB }] },
    { type: 'objectgroup', id: 2, name: 'things', x: 0, y: 0, opacity: 1, visible: true, draworder: 'topdown',
      objects: [{ id: 1, name: 'origin', type: '', point: true, x: 0, y: 0, width: 0, height: 0, rotation: 0, visible: true }] },
  ],
}, null, 1));
