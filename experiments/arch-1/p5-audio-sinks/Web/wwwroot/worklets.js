// ARCH-1 P5: the two AudioWorklet processors. They run on Web Audio's rendering thread, not on the page's thread.

// The sink: plays interleaved stereo chunks posted from the page, in order. When the queue is empty it outputs
// silence and counts those frames (starved). Reports what it played every 8 render quanta.
class P5Sink extends AudioWorkletProcessor {
  constructor() {
    super();
    this.queue = []; this.offset = 0; this.played = 0; this.starved = 0; this.started = false; this.n = 0;
    this.port.onmessage = e => { this.queue.push(e.data); this.started = true; };
  }
  process(inputs, outputs) {
    const [l, r] = outputs[0];
    for (let i = 0; i < l.length; i++) {
      const chunk = this.queue[0];
      if (!chunk) { l[i] = 0; r[i] = 0; if (this.started) this.starved++; continue; }
      l[i] = chunk[this.offset]; r[i] = chunk[this.offset + 1];
      this.offset += 2; this.played++;
      if (this.offset >= chunk.length) { this.queue.shift(); this.offset = 0; }
    }
    if (++this.n % 8 === 0) this.port.postMessage({ played: this.played, starved: this.starved, time: currentTime });
    return true;
  }
}
registerProcessor('p5-sink', P5Sink);

// The tap: passes its input through and measures, at the output, how much audio was not the sink's own continuous
// stream. The probe writes its output frame number into the right channel ((n mod 2^20) + 1) / 2^20, so for every
// non-zero right sample the tap knows which rendered frame it is hearing. offset = output frames heard - rendered frame
// number is constant while playback is continuous; silence or a repeated buffer makes it grow, skipped audio makes it
// shrink. Each change is reported with the context time it was seen and its size in frames.
class P5Tap extends AudioWorkletProcessor {
  constructor() { super(); this.heard = 0; this.offset = null; this.n = 0; this.silent = 0; }
  process(inputs, outputs) {
    const input = inputs[0], output = outputs[0];
    const left = input && input[0], right = input && input[1];
    for (let c = 0; c < output.length; c++) {
      if (input && input[c]) output[c].set(input[c]); else output[c].fill(0);
    }
    const frames = output[0].length;
    for (let i = 0; i < frames; i++, this.heard++) {
      const v = right ? right[i] : 0;
      if (v === 0) { if (this.offset !== null) this.silent++; continue; }
      const frame = Math.round(v * 1048576) - 1;
      const offset = this.heard - frame;
      if (this.offset === null) { this.offset = offset; continue; }
      const delta = ((offset - this.offset) % 1048576 + 1048576 + 524288) % 1048576 - 524288;
      if (delta !== 0) { this.port.postMessage({ at: currentTime + i / sampleRate, frames: delta, silent: this.silent }); this.offset += delta; }
      this.silent = 0;
    }
    if (++this.n % 375 === 0) this.port.postMessage({ heartbeat: true, heard: this.heard, locked: this.offset !== null });
    return true;
  }
}
registerProcessor('p5-tap', P5Tap);
