using System.Text;

namespace P5.Audio;

/// <summary>
/// Offline rendering for tests: the same scripted session (120 simulation steps at 60 Hz, a sound on some steps) is
/// rendered with no device, as a pulled device would ask for it (blocks of B frames whatever the step boundaries) and as a
/// pumped sink would (one pump per step). The output hash shows whether the mix depends on how the sink asks.
/// <list type="bullet">
/// <item><b>next-block</b>: a sound starts at the first block rendered after it was posted (what a live mixer does).</item>
/// <item><b>stamped</b>: a sound starts at an exact output frame computed from its simulation step plus a fixed
/// scheduling delay.</item>
/// </list>
/// </summary>
public static class Determinism
{
    private const int Rate = 48000, StepFrames = Rate / 60, Steps = 120, Delay = 2 * StepFrames;

    private static bool SoundAt(int step) => step % 7 == 3 || step % 11 == 5;

    /// <summary>A pulled device: before each block, the simulation steps whose time has come post their sounds.</summary>
    public static ulong Pulled(int block, bool stamped)
    {
        var fmt = new AudioFormat(Rate, 2);
        var mixer = new ProbeMixer(fmt);
        var buffer = new float[block * 2];
        var hash = new Fnv();
        int nextStep = 0;
        long total = (long)Steps * StepFrames;
        for (long pos = 0; pos < total; pos += block)
        {
            while (nextStep < Steps && (long)nextStep * StepFrames <= pos)
            {
                if (SoundAt(nextStep)) mixer.Play(660 + nextStep, 0.2f, 3000, nextStep, stamped ? (long)nextStep * StepFrames + Delay : -1);
                nextStep++;
            }
            var span = buffer.AsSpan(0, (int)Math.Min(block, total - pos) * 2);
            mixer.Render(span, 0);
            hash.Add(span);
        }
        return hash.Value;
    }

    /// <summary>A pumped sink: one pump of one step's frames per simulation step, in blocks of <paramref name="block"/>.</summary>
    public static ulong Pumped(int block, bool stamped)
    {
        var fmt = new AudioFormat(Rate, 2);
        var mixer = new ProbeMixer(fmt);
        var sink = new OfflineSink(fmt, block, StepFrames);
        sink.Start(mixer.Render);
        for (int step = 0; step < Steps; step++)
        {
            if (SoundAt(step)) mixer.Play(660 + step, 0.2f, 3000, step, stamped ? (long)step * StepFrames + Delay : -1);
            sink.Pump();
        }
        // Same length as Pulled for the largest block, so both hashes cover identical frames when they agree.
        var hash = new Fnv();
        var all = new float[sink.Output.Count];
        for (int i = 0; i < all.Length; i++) all[i] = sink.Output[i];
        hash.Add(all);
        return hash.Value;
    }

    public static string Run()
    {
        int[] blocks = [128, 441, 800, 1024, 4000];
        var sb = new StringBuilder("{");
        foreach (bool stamped in new[] { false, true })
        {
            var hashes = new HashSet<ulong>();
            sb.Append('"').Append(stamped ? "stamped" : "nextBlock").Append("\":{");
            foreach (int b in blocks)
            {
                ulong pulled = Pulled(b, stamped), pumped = Pumped(b, stamped);
                hashes.Add(pulled); hashes.Add(pumped);
                sb.Append($"\"pull{b}\":\"{pulled:x16}\",\"pump{b}\":\"{pumped:x16}\",");
            }
            sb.Append($"\"distinctOutputs\":{hashes.Count}}},");
        }
        sb.Length--;
        return sb.Append('}').ToString();
    }

    private sealed class Fnv
    {
        public ulong Value = 14695981039346656037UL;
        public void Add(ReadOnlySpan<float> data)
        {
            foreach (float f in data)
            {
                Value ^= (uint)BitConverter.SingleToInt32Bits(f);
                Value *= 1099511628211UL;
            }
        }
    }
}
