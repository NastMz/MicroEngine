// The engine's audio sink on Web Audio's rendering thread: plays the interleaved stereo chunks the game posts (its
// mixer renders on the page's thread, once per frame, up to 100 ms ahead), in order. With nothing queued it plays
// silence. Every 8 render quanta it reports how many frames it has played, so the page knows how much is queued.
class MicroEngineSink extends AudioWorkletProcessor {
  constructor() {
    super();
    this.queue = [];
    this.offset = 0;
    this.played = 0;
    this.quanta = 0;
    this.port.onmessage = e => {
      if (e.data === 'clear') { this.queue = []; this.offset = 0; this.played = 0; }
      else this.queue.push(e.data);
    };
  }

  process(inputs, outputs) {
    const [left, right] = outputs[0];
    for (let i = 0; i < left.length; i++) {
      const chunk = this.queue[0];
      if (!chunk) { left[i] = 0; right[i] = 0; continue; }
      left[i] = chunk[this.offset];
      right[i] = chunk[this.offset + 1];
      this.offset += 2;
      this.played++;
      if (this.offset >= chunk.length) { this.queue.shift(); this.offset = 0; }
    }
    if (++this.quanta % 8 === 0) this.port.postMessage({ played: this.played, time: currentTime });
    return true;
  }
}

registerProcessor('microengine-sink', MicroEngineSink);
