// Runs the landing's journey (components/landing/Journey.astro): binds the scroll to the 3D world of journey/scene.ts
// and shows each chapter's words while the camera is in its part of the world.
//
// The journey is an enhancement. The page ships as plain chapters, each with a still of its scene, and stays that way
// when the reader asked for reduced motion or to save data, when the device has little memory, or when WebGL is not
// available. Only otherwise is the 3D library fetched at all: it is a separate chunk, requested here.
import type { Journey, JourneyPalette } from './journey/scene.ts';

const clamp = (x: number) => Math.min(1, Math.max(0, x));

/** Whether this device should get the live journey. */
function capable(): boolean {
  if (matchMedia('(prefers-reduced-motion: reduce)').matches) return false;
  const nav = navigator as Navigator & { connection?: { saveData?: boolean }; deviceMemory?: number };
  if (nav.connection?.saveData) return false;
  if ((nav.deviceMemory ?? 8) < 4) return false;
  try {
    const probe = document.createElement('canvas');
    return !!(probe.getContext('webgl2') || probe.getContext('webgl'));
  } catch { return false; }
}

/** A token's colour as #rrggbb, resolved where `host` is: tokens are oklch and depend on the element's colour scheme. */
function resolver(): (host: HTMLElement, token: string) => string {
  const canvas = document.createElement('canvas');
  canvas.width = canvas.height = 1;
  const context = canvas.getContext('2d', { willReadFrequently: true })!;
  return (host, token) => {
    const probe = document.createElement('span');
    probe.style.color = `var(${token})`;
    host.append(probe);
    context.clearRect(0, 0, 1, 1);
    context.fillStyle = getComputedStyle(probe).color;
    context.fillRect(0, 0, 1, 1);
    probe.remove();
    const [r, g, b] = context.getImageData(0, 0, 1, 1).data;
    return '#' + [r, g, b].map(v => v.toString(16).padStart(2, '0')).join('');
  };
}

export async function startJourney(): Promise<void> {
  const root = document.querySelector<HTMLElement>('[data-journey]');
  const stage = root?.querySelector<HTMLElement>('.journey-stage');
  const canvas = root?.querySelector<HTMLCanvasElement>('canvas');
  const source = root?.querySelector<HTMLScriptElement>('script[type="application/json"]');
  const inverse = root?.querySelector<HTMLElement>('.journey-inverse');
  if (!root || !stage || !canvas || !source || !inverse || !capable()) return;

  const data = JSON.parse(source.textContent || '{}') as { games: string[]; icons: Record<string, [string, string]>; tape: number[][] };
  const colour = resolver();
  const families = ['graphics', 'physics', 'audio', 'input', 'platform', 'tools'] as const;
  const palette: JourneyPalette = {
    paper: colour(root, '--color-paper'), table: colour(root, '--color-paper-3'), rule: colour(root, '--color-rule'),
    ink: colour(root, '--color-ink'), ink3: colour(root, '--color-ink-3'), amber: colour(root, '--color-mark-module'),
    inversePaper: colour(inverse, '--color-paper'), inverseInk: colour(inverse, '--color-ink'),
    families: Object.fromEntries(families.map(f => [f, colour(root, `--family-${f}`)])) as JourneyPalette['families'],
  };

  let journey: Journey;
  let dirty = true;
  try {
    const { createJourney } = await import('./journey/scene.ts');
    // ?journey=still is how the stills of the plain chapters are taken (scripts/journey-stills.mjs).
    const centred = new URLSearchParams(location.search).get('journey') === 'still';
    journey = createJourney({ canvas, palette, games: data.games, icons: data.icons, tape: data.tape, centred, onChange: () => { dirty = true; schedule(); } });
  } catch {
    return; // no 3D after all: the chapters stay as they are
  }

  const chapters = [...root.querySelectorAll<HTMLElement>('.chapter')].map(el => ({
    el, from: Number(el.dataset.from), to: Number(el.dataset.to), shown: null as boolean | null,
  }));
  document.documentElement.classList.add('journey-live');

  let target = 0, shown = -1, queued = false;
  const measure = (): void => {
    const rect = root.getBoundingClientRect();
    target = clamp(-rect.top / Math.max(1, rect.height - innerHeight));
  };
  const resize = (): void => { journey.resize(stage.clientWidth, stage.clientHeight); dirty = true; measure(); schedule(); };

  function frame(): void {
    queued = false;
    // The picture follows the scroll with a short lag, so a wheel's steps become one movement.
    const next = shown < 0 ? target : shown + (target - shown) * 0.16;
    const settled = Math.abs(target - next) < 0.00015;
    const p = settled ? target : next;
    if (p !== shown || dirty) {
      shown = p; dirty = false;
      journey.render(p);
      for (const chapter of chapters) {
        const fade = 0.014;
        const inK = chapter.from <= 0 ? 1 : clamp((p - chapter.from) / fade), outK = chapter.to >= 1 ? 1 : clamp((chapter.to - p) / fade);
        const k = Math.min(inK, outK);
        chapter.el.style.opacity = k.toFixed(3);
        chapter.el.style.transform = `translate3d(0, ${((1 - inK) * 28 - (1 - outK) * 28).toFixed(1)}px, 0)`;
        const visible = k > 0.5;
        if (visible !== chapter.shown) { chapter.shown = visible; chapter.el.inert = !visible; chapter.el.toggleAttribute('data-current', visible); }
      }
      root.style.setProperty('--journey', p.toFixed(4));
    }
    if (!settled) schedule();
  }
  function schedule(): void { if (!queued) { queued = true; requestAnimationFrame(frame); } }

  addEventListener('scroll', () => { measure(); schedule(); }, { passive: true });
  addEventListener('resize', resize, { passive: true });
  resize();
  // A lost context (a tab that slept, a driver reset) puts the plain chapters back.
  canvas.addEventListener('webglcontextlost', () => document.documentElement.classList.remove('journey-live'));
}
