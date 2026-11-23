using MicroEngine.Audio;
using MicroEngine.Platform;

namespace MicroEngine.Tests.Audio;

/// <summary>
/// A bus is usable in the step that creates it: a sound started on it at once plays through it, its parents and
/// their volumes from its first frame.
/// </summary>
[TestClass]
public sealed class AudioBusCreationTests
{
    private const int Rate = 48000, Steps = 60, StepFrames = Rate / Steps;

    [TestMethod]
    public void A_sound_started_on_a_bus_in_the_step_that_created_it_plays_through_it_from_its_first_frame()
    {
        foreach (int block in new[] { 64, 441, 4096 })
        {
            var sink = new OfflineAudioSink(new AudioFormat(Rate, 2), Steps, block);
            var engine = new AudioEngine(sink, new AudioEngineSettings { UpdateRate = Steps, DelayFrames = 100, MaxVoices = 64 });
            var clip = new AudioClip([.. Enumerable.Repeat(1f, StepFrames)], Rate);
            // A chain of buses, each under the one before, each at half volume, created and used step by step while
            // the sink renders.
            string parent = AudioEngine.MasterBus;
            float expected = 1;
            for (int i = 0; i < 12; i++)
            {
                var bus = engine.AddBus($"bus{i}", parent);
                bus.Volume = 0.5f;
                expected *= 0.5f;
                engine.Play(clip, new PlayParameters { Bus = bus.Name });
                engine.Advance();
                engine.Pump();
                int start = i * StepFrames + 100;
                if (i > 0) Assert.AreEqual(expected, sink.Samples[start * 2], $"block {block}, bus {i}: its first frame");
                parent = bus.Name;
            }
            engine.Advance();
            engine.Pump();
            Assert.AreEqual(expected, sink.Samples[(11 * StepFrames + 100 + 700) * 2], $"block {block}: and the rest of it");
            Assert.AreEqual(0, engine.DroppedCommands);
        }
    }
}
