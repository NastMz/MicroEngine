// Scroll-linked motion for the landing, without a library: each [data-scroll] element gets its progress through the
// viewport as a custom property, and CSS does the rest. Everything is a function of the scroll position, so it plays
// backwards when the reader scrolls up, and nothing ever holds the scroll.
//
//   data-scroll="enter"    --p goes 0 to 1 while the element's top travels from the bottom edge to 40% of the viewport
//   data-scroll="leave"    0 to 1 while the element scrolls away past the top (the opening)
//   data-scroll="through"  0 to 1 from the moment it enters at the bottom until it has left at the top
//   data-scroll="pin"      0 to 1 across a tall element whose child is position: sticky
//   data-scroll-steps="9"  also sets --step, the progress rounded to that many whole steps
//   data-reveal            gets data-seen once its top is inside the viewport, and loses it when it goes back below
//
// The motion styles apply only under html.scroll-live, which this script sets unless the reader asked for reduced
// motion; without it (or without JavaScript) every element shows its finished state. The page tape (the progress
// ruler at the top) is a position indicator, not an animation, so it runs either way.
type Mode = 'enter' | 'leave' | 'through' | 'pin';
interface Item { el: HTMLElement; mode: Mode; steps: number; last: number }

const clamp = (value: number) => Math.min(1, Math.max(0, value));

function progress(mode: Mode, rect: DOMRect, viewport: number): number {
  switch (mode) {
    case 'enter': return clamp((viewport - rect.top) / (viewport * 0.6));
    case 'leave': return clamp(-rect.top / (rect.height * 0.8));
    case 'pin': return clamp(-rect.top / Math.max(1, rect.height - viewport));
    default: return clamp((viewport - rect.top) / (viewport + rect.height));
  }
}

export function startScroll(): void {
  const root = document.documentElement;
  const reduced = matchMedia('(prefers-reduced-motion: reduce)');
  const items: Item[] = [...document.querySelectorAll<HTMLElement>('[data-scroll]')].map(el => ({
    el, mode: (el.dataset.scroll || 'through') as Mode, steps: Number(el.dataset.scrollSteps || 0), last: -1,
  }));
  const reveals = [...document.querySelectorAll<HTMLElement>('[data-reveal]')];
  const tape = document.querySelector<HTMLElement>('[data-page-tape]');
  const marks = tape ? [...document.querySelectorAll<HTMLElement>('main > section[id]')] : [];
  let ticks: HTMLElement[] = [];
  let live = false;
  let queued = false;

  function setLive(): void {
    live = !reduced.matches;
    root.classList.toggle('scroll-live', live);
    if (!live) {
      for (const item of items) { item.el.style.removeProperty('--p'); item.el.style.removeProperty('--step'); item.last = -1; }
      for (const el of reveals) el.removeAttribute('data-seen');
    }
  }

  // The page tape: one tick where each section starts, laid out along the width of the page.
  function layoutTape(): void {
    if (!tape) return;
    const total = Math.max(1, root.scrollHeight - innerHeight);
    if (ticks.length !== marks.length) {
      tape.querySelectorAll('.tick').forEach(tick => tick.remove());
      ticks = marks.map(() => { const tick = document.createElement('span'); tick.className = 'tick'; tape.append(tick); return tick; });
    }
    marks.forEach((section, i) => {
      const at = clamp((section.getBoundingClientRect().top + scrollY) / total);
      ticks[i].style.left = `${(at * 100).toFixed(3)}%`;
      ticks[i].dataset.at = String(at);
    });
  }

  function update(): void {
    queued = false;
    const viewport = innerHeight;
    if (tape) {
      const at = clamp(scrollY / Math.max(1, root.scrollHeight - viewport));
      tape.style.setProperty('--at', at.toFixed(4));
      for (const tick of ticks) tick.toggleAttribute('data-passed', Number(tick.dataset.at) <= at + 0.0005);
    }
    if (!live) return;
    for (const item of items) {
      const rect = item.el.getBoundingClientRect();
      // Far from the viewport an element sits at one of its ends: nothing to compute.
      if (rect.bottom < -viewport || rect.top > viewport * 2) continue;
      const p = progress(item.mode, rect, viewport);
      if (Math.abs(p - item.last) < 0.0005) continue;
      item.last = p;
      item.el.style.setProperty('--p', p.toFixed(4));
      if (item.steps) item.el.style.setProperty('--step', String(Math.round(p * item.steps)));
    }
    for (const el of reveals) el.toggleAttribute('data-seen', el.getBoundingClientRect().top < viewport * 0.88);
  }

  const schedule = (): void => { if (!queued) { queued = true; requestAnimationFrame(update); } };
  const relayout = (): void => { layoutTape(); schedule(); };

  setLive();
  layoutTape();
  update();
  addEventListener('scroll', schedule, { passive: true });
  addEventListener('resize', relayout, { passive: true });
  addEventListener('load', relayout);
  reduced.addEventListener('change', () => { setLive(); schedule(); });
  // Images and fonts change the page's height after the first layout; the tape follows.
  if ('ResizeObserver' in window) new ResizeObserver(relayout).observe(document.body);
}
