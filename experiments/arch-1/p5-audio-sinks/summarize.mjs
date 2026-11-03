// ARCH-1 P5: one table row per JSON report line on stdin (desktop, Android logcat lines or web results).
//   node summarize.mjs < reports.jsonl
// Columns: sink, device period, target, sound latency (median/max), then per phase the audio lost (wall time minus
// audio the device took from the sink, ms; on the web, the output tap's dropout ms) and the longest gap between render
// calls (ms).
import fs from 'node:fs';
const lines = fs.readFileSync(0, 'utf8').split(/\r?\n/).map(l => l.slice(l.indexOf('{'))).filter(l => l.startsWith('{"platform"'));
const r = n => (n == null || Number.isNaN(n) ? '-' : Math.round(n));
console.log('| Sink | Period ms | Target ms | Sound latency ms (median / max) | Steady | Stall 100 ms | Stall 300 ms | Allocation burst + GC |');
console.log('| --- | --- | --- | --- | --- | --- | --- | --- |');
for (const line of lines) {
  const j = JSON.parse(line);
  const cell = p => {
    return p.dropoutMs != null ? `${r(p.dropoutMs)} dropout (deficit ${r(p.lostMs)}, gap ${r(p.gapMsMax)})` : `${r(p.lostMs)} lost (gap ${r(p.gapMsMax)})`;
  };
  const ph = j.phases;
  console.log(`| ${j.sink} | ${r(j.devicePeriodMs)} | ${r(j.targetBufferedMs)} | ${r(j.soundLatencyMsMedian)} / ${r(j.soundLatencyMsMax)} | ${cell(ph.steady)} | ${cell(ph.stall100)} | ${cell(ph.stall300)} | ${cell(ph.gc)} |`);
}
