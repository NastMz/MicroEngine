// The landing's journey: one 3D world the reader travels through by scrolling. Everything drawn is a pure function
// of one number, the progress p from 0 to 1, so the scroll is a timeline that plays forwards and backwards.
//
// The world, in the order the camera meets it (all three films are in it):
//   the kernel on its table, and six pieces that arrive and dock around it          (Assembly)
//   a plane above the assembly with a real game on it; the camera dives into it      (Seated)
//   three more real games, hanging in depth, that the camera flies past
//   a tape of steps under a playhead: the score of "Every step", step for step       (Every step)
//   the mark, assembled from its parts, with the amber module docking last
//
// Flat shading only, three tones a colour, as in the films: no lights, no shadows, no gradients. Fog fades distant
// things into the ground, which is the page's own paper, and the other theme's for the tape.
import {
  BoxGeometry, BufferGeometry, CanvasTexture, Color, Fog, Group, InstancedMesh, Line, LineDashedMaterial,
  Matrix4, Mesh, MeshBasicMaterial, NearestFilter, PerspectiveCamera, PlaneGeometry, Scene, SRGBColorSpace, TextureLoader, Vector3,
  WebGLRenderer, type Material, type Texture,
} from 'three';

export interface JourneyPalette {
  paper: string; table: string; rule: string; ink: string; ink3: string; amber: string;
  /** The other theme's ground and ink, for the tape. */
  inversePaper: string; inverseInk: string;
  families: Record<'graphics' | 'physics' | 'audio' | 'input' | 'platform' | 'tools', string>;
}
export interface JourneyOptions {
  canvas: HTMLCanvasElement;
  palette: JourneyPalette;
  /** Real captures of the demos, in the order the camera meets them. */
  games: string[];
  /** Family icons on the brand's 16-unit grid: [board path, module path]. */
  icons: Record<string, [string, string]>;
  /** The steps of every voice of a score: 0 silence, 1 a note starting, 2 a note held. */
  tape: number[][];
  /** Keep the picture in the middle of the frame: for the stills, which have no words over them. */
  centred?: boolean;
  onChange: () => void;
}
export interface Journey { render(p: number): void; resize(width: number, height: number): void; dispose(): void }

const clamp = (x: number) => Math.min(1, Math.max(0, x));
const ramp = (p: number, a: number, b: number) => { const t = clamp((p - a) / (b - a)); return t * t * (3 - 2 * t); };
const easeIn = (p: number, a: number, b: number) => { const t = clamp((p - a) / (b - a)); return t * t * t; };
const easeOut = (p: number, a: number, b: number) => 1 - Math.pow(1 - clamp((p - a) / (b - a)), 3);

/** Where the story is at each progress. The film's chapters (index.astro) use the same numbers. */
export const beats = {
  pieces: [0.1, 0.27], plane: [0.275, 0.32], dive: [0.33, 0.42], through: 0.455, gallery: [0.46, 0.62],
  tape: [0.66, 0.845], mark: [0.86, 0.9], module: [0.905, 0.93],
} as const;

export function createJourney(options: JourneyOptions): Journey {
  const { canvas, palette, onChange } = options;
  const renderer = new WebGLRenderer({ canvas, antialias: true, alpha: false, powerPreference: 'high-performance' });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  const scene = new Scene();
  const paper = new Color(palette.paper), inversePaper = new Color(palette.inversePaper);
  const ink = new Color(palette.ink), inverseInk = new Color(palette.inverseInk), amber = new Color(palette.amber);
  scene.background = paper.clone();
  scene.fog = new Fog(paper.clone(), 2600, 9500);
  const camera = new PerspectiveCamera(32, 1, 20, 22000);
  const disposables: { dispose(): void }[] = [];
  const keep = <T extends { dispose(): void }>(thing: T) => { disposables.push(thing); return thing; };

  // --- Flat-shaded boxes: front, top (lighter) and side (darker), like the films' pieces. ---------------------
  const tone = (color: Color, amount: number) => color.clone().lerp(amount > 0 ? new Color('#ffffff') : new Color('#000000'), Math.abs(amount));
  const flat = (color: Color, fog = true) => keep(new MeshBasicMaterial({ color, fog }));
  /** Materials in BoxGeometry's face order: +x, -x, +y, -y, +z, -z. */
  const shaded = (color: Color, front?: Material) => {
    const side = flat(tone(color, -0.2)), top = flat(tone(color, 0.2));
    return [side, side, top, side, front ?? flat(color), side];
  };
  const box = (w: number, h: number, d: number, materials: Material[]) => new Mesh(keep(new BoxGeometry(w, h, d)), materials);

  /** A family's icon drawn on its colour: the face of a piece. */
  const iconTexture = (family: keyof JourneyPalette['families'], w: number, h: number) => {
    const scale = 2, c = document.createElement('canvas');
    c.width = w * scale; c.height = h * scale;
    const g = c.getContext('2d')!;
    g.fillStyle = palette.families[family];
    g.fillRect(0, 0, c.width, c.height);
    const size = Math.min(w, h) * 0.46 * scale, unit = size / 16;
    g.translate((c.width - size) / 2, (c.height - size) / 2);
    g.scale(unit, unit);
    const [board, module] = options.icons[family];
    g.fillStyle = palette.paper;
    g.fill(new Path2D(board), 'evenodd');
    g.globalAlpha = 0.6;
    g.fill(new Path2D(module));
    const texture = keep(new CanvasTexture(c));
    texture.colorSpace = SRGBColorSpace;
    return texture;
  };

  // --- The table and the kernel. ---------------------------------------------------------------------------------
  const world = new Group();
  scene.add(world);
  const table = new Mesh(keep(new PlaneGeometry(5200, 5200)), flat(new Color(palette.table)));
  table.rotation.x = -Math.PI / 2;
  world.add(table);
  const rule = keep(new MeshBasicMaterial({ color: new Color(palette.rule) }));
  for (const [w, d, x, z] of [[1840, 4, 0, 0], [4, 1840, 0, 0], [1600, 4, 0, 800], [1600, 4, 0, -800], [4, 1600, 800, 0], [4, 1600, -800, 0]]) {
    const line = new Mesh(keep(new PlaneGeometry(w, d)), rule);
    line.rotation.x = -Math.PI / 2;
    line.position.set(x, 1, z);
    world.add(line);
  }
  const kernel = box(300, 140, 300, shaded(ink));
  world.add(kernel);

  // --- Six pieces: where each docks, and the axis it travels along (media/assembly's first configuration). -------
  const FLOAT = 300;
  const guideMaterial = keep(new LineDashedMaterial({ color: new Color(palette.ink3), dashSize: 14, gapSize: 12 }));
  const pieces = ([
    ['graphics', [150, 240, 300], [239, 120, 0], [1, 0, 0]],
    ['audio', [150, 140, 300], [-239, 70, 0], [-1, 0, 0]],
    ['platform', [300, 90, 150], [0, 45, 239], [0, 0, 1]],
    ['physics', [300, 190, 150], [0, 95, -239], [0, 0, -1]],
    ['input', [140, 70, 140], [70, 189, 70], [0, 1, 0]],
    ['tools', [140, 110, 140], [-70, 209, -70], [0, 1, 0]],
  ] as const).map(([family, size, at, axis], i) => {
    const color = new Color(palette.families[family]);
    const face = keep(new MeshBasicMaterial({ map: iconTexture(family, size[0], size[1]) }));
    const mesh = box(size[0], size[1], size[2], shaded(color, face));
    world.add(mesh);
    const dock = new Vector3(...at), direction = new Vector3(...axis);
    const geometry = keep(new BufferGeometry().setFromPoints([dock.clone(), dock.clone().addScaledVector(direction, FLOAT)]));
    const guide = new Line(geometry, guideMaterial);
    guide.computeLineDistances();
    world.add(guide);
    return { mesh, guide, dock, direction, land: beats.pieces[0] + 0.02 + i * 0.022 };
  });

  // --- Real games on planes: the first rises from the assembly, the others hang in depth beyond it. --------------
  const loader = new TextureLoader();
  const frame = flat(ink);
  const gamePlane = (url: string, width: number) => {
    const group = new Group();
    const material = keep(new MeshBasicMaterial({ color: new Color(palette.table) }));
    loader.load(url, (texture: Texture) => {
      texture.colorSpace = SRGBColorSpace;
      texture.magFilter = NearestFilter;
      material.map = keep(texture);
      material.color.set('#ffffff');
      material.needsUpdate = true;
      onChange();
    });
    const height = width * 9 / 16;
    const back = new Mesh(keep(new PlaneGeometry(width + 28, height + 28)), frame);
    back.position.z = -2;
    group.add(back, new Mesh(keep(new PlaneGeometry(width, height)), material));
    scene.add(group);
    return group;
  };
  const PLANE_Y = 624;
  const first = gamePlane(options.games[0], 640);
  first.position.set(0, PLANE_Y, 0);
  const stem = new Line(keep(new BufferGeometry().setFromPoints([new Vector3(0, 140, 0), new Vector3(0, PLANE_Y - 194, 0)])), guideMaterial);
  stem.computeLineDistances();
  world.add(stem);
  const hung: [number, number, number, number][] = [[-700, 640, -1700, 0.42], [700, 700, -3200, -0.42], [-600, 580, -4700, 0.36]];
  const gallery = hung.map(([x, y, z, turn], i) => {
    const plane = gamePlane(options.games[i + 1], 1300);
    plane.position.set(x, y, z);
    plane.rotation.y = turn;
    return plane;
  });

  // --- The tape: every note of the score as a cell, rows across, steps in depth; the playhead crosses it. --------
  const STEP = 150, ROW = 124, TAPE_Y = 300, TAPE_START = -6300;
  const rows = options.tape.length;
  const cells: { row: number; step: number }[] = [];
  const FIRST_STEP = 56, STEPS = 72;
  options.tape.forEach((voice, row) => voice.slice(FIRST_STEP, FIRST_STEP + STEPS).forEach((value, step) => { if (value) cells.push({ row, step }); }));
  const cellMaterial = keep(new MeshBasicMaterial({ color: '#ffffff' }));
  const tape = new InstancedMesh(keep(new BoxGeometry(ROW - 14, 10, STEP - 14)), cellMaterial, cells.length);
  const placement = new Matrix4();
  cells.forEach(({ row, step }, i) => {
    placement.makeTranslation((row - (rows - 1) / 2) * ROW, TAPE_Y, TAPE_START - step * STEP);
    tape.setMatrixAt(i, placement);
    tape.setColorAt(i, inverseInk);
  });
  scene.add(tape);
  const steps = STEPS;
  const ticks = new InstancedMesh(keep(new BoxGeometry(4, 4, 40)), keep(new MeshBasicMaterial({ color: inverseInk })), steps);
  for (let step = 0; step < steps; step++) {
    placement.makeTranslation(-(rows / 2) * ROW - 40, TAPE_Y, TAPE_START - step * STEP + STEP / 2);
    ticks.setMatrixAt(step, placement);
  }
  scene.add(ticks);
  const playhead = box(rows * ROW + 120, 70, 8, [0, 0, 0, 0, 0, 0].map(() => flat(amber, false)));
  playhead.position.y = TAPE_Y + 30;
  scene.add(playhead);
  const unplayed = inversePaper.clone().lerp(inverseInk, 0.24);
  let lastHead = NaN;

  // --- The mark (site/src/components/brand/geometry.mjs on a 40-unit grid): four parts and the amber module. -----
  const MARK = new Vector3(0, 760, -19600);
  const mark = new Group();
  mark.position.copy(MARK);
  scene.add(mark);
  const boardInk = [0, 0, 0, 0, 0, 0].map(() => flat(ink, false));
  const parts = ([[640, 120, 0, 180, 0, 900], [80, 360, -280, -60, -1300, -60], [80, 360, 0, -60, 0, -1100], [80, 360, 280, -60, 1300, -60]] as const)
    .map(([w, h, x, y, fromX, fromY]) => {
      const mesh = box(w, h, 60, boardInk);
      mark.add(mesh);
      return { mesh, at: new Vector3(x, y, 0), from: new Vector3(fromX, fromY, 0) };
    });
  const module = box(120, 320, 60, [0, 0, 0, 0, 0, 0].map(() => flat(amber, false)));
  mark.add(module);

  // --- The camera's path: positions and what it looks at, joined by a spline so the flight never stops. ----------
  // shift moves the picture sideways (in half-widths of the frame) to leave room for the chapter's words.
  type Key = { p: number; at: [number, number, number]; look: [number, number, number]; shift: number };
  const keys: Key[] = [
    { p: 0, at: [1400, 940, 1920], look: [0, 110, 0], shift: 0.5 },
    { p: 0.1, at: [1120, 840, 2000], look: [0, 120, 0], shift: 0.5 },
    { p: 0.2, at: [-260, 1080, 2150], look: [0, 150, 0], shift: 0.54 },
    { p: 0.27, at: [-700, 800, 1700], look: [0, 220, 0], shift: 0.5 },
    { p: 0.33, at: [-560, 800, 1560], look: [0, 470, 0], shift: 0.36 },
    { p: 0.42, at: [0, PLANE_Y, 1], look: [0, PLANE_Y, 0], shift: 0 },   // z is set from the frame's shape below
    { p: 0.455, at: [0, PLANE_Y, 1], look: [0, PLANE_Y, 0], shift: 0 },
    { p: 0.5, at: [-60, 640, -600], look: [-700, 640, -1700], shift: 0.2 },
    { p: 0.55, at: [160, 680, -2100], look: [700, 700, -3200], shift: -0.1 },
    { p: 0.6, at: [-100, 620, -3600], look: [-600, 580, -4700], shift: 0.2 },
    { p: 0.66, at: [0, 1350, -5500], look: [0, 300, -7900], shift: 0.5 },
    { p: 0.845, at: [0, 1300, -14000], look: [0, 300, -16400], shift: 0.5 },
    { p: 0.9, at: [0, 720, -16900], look: [0, 560, -19600], shift: 0 },
    { p: 1, at: [0, 700, -17050], look: [0, 540, -19600], shift: 0 },
  ];
  const spline = (a: number, b: number, c: number, d: number, t: number) =>
    0.5 * ((2 * b) + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (-a + 3 * b - 3 * c + d) * t * t * t);
  const at = new Vector3(), look = new Vector3();
  let aspect = 1;

  function placeCamera(p: number): void {
    // The distance from which the first plane exactly covers the frame, whatever the frame's shape.
    const half = Math.tan((camera.fov * Math.PI) / 360);
    const cover = Math.min(180 / half, 320 / (half * aspect));
    keys[5].at[2] = cover; keys[6].at[2] = cover;
    let i = keys.findIndex(key => key.p > p) - 1;
    if (i < 0) i = keys.length - 2;
    const k0 = keys[Math.max(0, i - 1)], k1 = keys[i], k2 = keys[i + 1], k3 = keys[Math.min(keys.length - 1, i + 2)];
    const t = clamp((p - k1.p) / (k2.p - k1.p));
    // Holding still on the full-frame game, the spline must not overshoot: those two keys are joined straight.
    const straight = i === 5 || i === 4 || i === 6;
    const blend = (n: 0 | 1 | 2, of: 'at' | 'look') => straight ? k1[of][n] + (k2[of][n] - k1[of][n]) * (t * t * (3 - 2 * t)) : spline(k0[of][n], k1[of][n], k2[of][n], k3[of][n], t);
    at.set(blend(0, 'at'), blend(1, 'at'), blend(2, 'at'));
    look.set(blend(0, 'look'), blend(1, 'look'), blend(2, 'look'));
    // A tall frame sees less to each side: step back in proportion.
    const tall = aspect < 1.25;
    if (tall && (p < 0.33 || p > 0.86)) at.sub(look).multiplyScalar(Math.pow(1.25 / aspect, 0.6)).add(look);
    camera.position.copy(at);
    camera.lookAt(look);
    // On a wide frame the picture moves aside for the words; on a tall one it moves up, above them, except while
    // a game fills the frame.
    const aside = options.centred || tall ? 0 : k1.shift + (k2.shift - k1.shift) * t;
    const above = options.centred || !tall ? 0 : 0.2 * (1 - ramp(p, 0.33, 0.4) * (1 - ramp(p, 0.455, 0.5)));
    const { width, height } = renderer.domElement;
    if (aside || above) camera.setViewOffset(width, height, -aside * width * 0.5, above * height, width, height); else camera.clearViewOffset();
  }

  function render(p: number): void {
    // The assembly: every piece floats on its guide around the kernel, and docks when its turn comes.
    for (const piece of pieces) {
      const land = easeIn(p, piece.land, piece.land + 0.02);
      piece.mesh.position.copy(piece.dock).addScaledVector(piece.direction, FLOAT * (1 - land));
      piece.guide.visible = land < 1;
    }
    kernel.position.set(0, 70, 0);

    // The plane rises once everything has docked; past it, the camera is through and the table is behind.
    const rise = easeOut(p, beats.plane[0], beats.plane[1]);
    first.scale.setScalar(Math.max(0.001, rise));
    first.visible = rise > 0 && p < beats.through + 0.012;
    stem.visible = first.visible && p < beats.dive[0] + 0.05;
    world.visible = p < beats.through + 0.012;
    gallery.forEach(plane => { plane.visible = p > beats.dive[1] && p < 0.7; });

    // The ground: paper, and the other theme's while the tape runs. It never fades from one to the other: the other
    // ground crosses the frame as a hard edge (two passes, split by a scissor), and goes back the way it came.
    const inverse = ramp(p, 0.615, 0.655) * (1 - ramp(p, 0.84, 0.872));

    // The tape: cells the playhead has passed are solid, the one under it is amber, the rest are still to come.
    const onTape = p > 0.6 && p < 0.9;
    tape.visible = ticks.visible = playhead.visible = onTape;
    placeCamera(p);
    if (onTape) {
      const headZ = camera.position.z - 2500;
      playhead.position.z = headZ;
      playhead.position.x = 0;
      const head = Math.floor((TAPE_START - headZ) / STEP);
      if (head !== lastHead) {
        lastHead = head;
        cells.forEach((cell, i) => tape.setColorAt(i, cell.step < head ? inverseInk : cell.step === head ? amber : unplayed));
        if (tape.instanceColor) tape.instanceColor.needsUpdate = true;
      }
    }

    // The mark: its four parts dock one after another, then the amber module comes from the reader's side.
    mark.visible = p > beats.mark[0] - 0.01;
    parts.forEach((part, i) => {
      const k = easeIn(p, beats.mark[0] + i * 0.008, beats.mark[0] + 0.016 + i * 0.008);
      part.mesh.position.copy(part.from).lerp(part.at, k);
    });
    const dock = easeIn(p, beats.module[0], beats.module[1]);
    module.position.set(140, -80, 1500 * (1 - dock));
    module.visible = p > beats.module[0];
    const hit = p > beats.module[1] ? 14 * Math.exp(-(p - beats.module[1]) * 260) * Math.cos((p - beats.module[1]) * 900) : 0;
    mark.position.set(MARK.x, MARK.y + hit, MARK.z);

    const { width, height } = renderer.domElement;
    const edge = Math.round(width * inverse);
    const pass = (color: Color, x: number, w: number) => {
      if (w <= 0) return;
      (scene.background as Color).copy(color);
      scene.fog!.color.copy(color);
      renderer.setScissor(x / renderer.getPixelRatio(), 0, w / renderer.getPixelRatio(), height / renderer.getPixelRatio());
      renderer.render(scene, camera);
    };
    renderer.setScissorTest(true);
    pass(inversePaper, 0, edge);
    pass(paper, edge, width - edge);
  }

  function resize(width: number, height: number): void {
    aspect = width / Math.max(1, height);
    camera.aspect = aspect;
    camera.fov = aspect < 1 ? 46 : 32;
    camera.updateProjectionMatrix();
    renderer.setSize(width, height, false);
  }

  return {
    render, resize,
    dispose() { for (const thing of disposables) thing.dispose(); renderer.dispose(); },
  };
}



