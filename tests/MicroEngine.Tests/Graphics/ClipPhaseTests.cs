using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace MicroEngine.Tests;

/// <summary>
/// Clips a game drives: each instance starts on a frame of its own (<see cref="AnimationClip.FrameIndexAt"/> with a
/// frame offset), and a strip built in code is a clip when it is added with a rate.
/// </summary>
[TestClass]
public sealed class ClipPhaseTests
{
    private static SpriteAtlas Atlas(Action<SpriteAtlasBuilder> add)
    {
        var builder = new SpriteAtlasBuilder();
        add(builder);
        return builder.Build(new HeadlessGraphicsDevice());
    }

    private static IEnumerable<PixelImage> Images(int count) => Enumerable.Range(0, count).Select(i => new PixelImage(4 + i, 4));

    /// <summary>The times a game draws at: a clock that adds 1/60 s in float, as the demos' do, for two minutes.</summary>
    private static IEnumerable<float> Times()
    {
        float time = 0;
        for (int step = 0; step < 7200; step++)
        {
            yield return time;
            time += 1 / 60f;
        }
    }

    [TestMethod]
    public void Frames_added_with_a_rate_are_also_a_clip_of_that_name()
    {
        using var atlas = Atlas(b => b.AddFrames("fireball", Images(2), 12, new Vector2(5, 5)).AddFrames("puff", Images(3), 20, loop: false).AddFrames("plain", Images(2)));
        CollectionAssert.AreEqual(new[] { "fireball", "puff" }, atlas.Animations.ToArray(), "frames without a rate are no clip");
        var fire = atlas.Clip("fireball");
        Assert.AreEqual(12f, fire.FramesPerSecond);
        Assert.IsTrue(fire.Loop);
        Assert.AreEqual(2, fire.Frames.Count);
        Assert.AreSame(atlas["fireball1"], fire.Frames[1]);
        Assert.AreEqual(new Vector2(5, 5), fire.Frames[0].Origin);
        Assert.IsFalse(atlas.Clip("puff").Loop);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SpriteAtlasBuilder().AddFrames("x", Images(2), 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SpriteAtlasBuilder().AddFrames("x", Images(2), float.NaN));
    }

    [TestMethod]
    public void The_links_of_a_fire_bar_each_start_on_their_own_frame()
    {
        // Meadow Run, Props.cs: a.Frames("fireball")[(int)(time * 12 + i) % 2] for each link i.
        using var atlas = Atlas(b => b.AddFrames("fireball", Images(2), 12));
        var clip = atlas.Clip("fireball");
        foreach (float time in Times())
            for (int i = 1; i <= 6; i++)
            {
                int expected = (int)(time * 12 + i) % 2;
                Assert.AreEqual(expected, clip.FrameIndexAt(time, i), $"link {i} at {time}");
                Assert.AreSame(atlas.Frames("fireball")[expected], clip.FrameAt(time, i));
            }
    }

    [TestMethod]
    public void Torches_start_on_a_frame_given_by_where_they_stand()
    {
        // Meadow Run, Props.cs: a.Frames("torch")[(int)(time * 9 + Position.X) % 3].
        using var atlas = Atlas(b => b.AddFrames("torch", Images(3), 9));
        var clip = atlas.Clip("torch");
        foreach (float x in new[] { 8f, 24f, 56f, 328f, 1016f, 2840f })
            foreach (float time in Times())
                Assert.AreEqual((int)(time * 9 + x) % 3, clip.FrameIndexAt(time, x), $"torch at x {x}, time {time}");
    }

    [TestMethod]
    public void The_coins_of_a_title_screen_spin_one_frame_apart()
    {
        // Meadow Run, TitleScene.cs: coin[(int)(t * 8 + i) % coin.Count] for i in 0..3.
        foreach (int count in new[] { 4, 6 })
        {
            using var atlas = Atlas(b => b.AddFrames("coin", Images(count), 8));
            var clip = atlas.Clip("coin");
            foreach (float time in Times())
                for (int i = 0; i < 4; i++)
                    Assert.AreEqual((int)(time * 8 + i) % count, clip.FrameIndexAt(time, i), $"coin {i} of {count} frames at {time}");
        }
    }

    [TestMethod]
    public void Without_an_offset_the_index_is_the_frame_FrameAt_gives()
    {
        using var atlas = Atlas(b => b.AddFrames("walk", Images(5), 10).AddFrames("pop", Images(4), 10, loop: false));
        var walk = atlas.Clip("walk");
        var pop = atlas.Clip("pop");
        foreach (float time in Times().Take(600))
        {
            Assert.AreSame(walk.FrameAt(time), walk.Frames[walk.FrameIndexAt(time)]);
            Assert.AreSame(pop.FrameAt(time), pop.Frames[pop.FrameIndexAt(time)]);
        }
        Assert.AreEqual(3, pop.FrameIndexAt(0, 3), "a clip that plays once starts on the offset frame");
        Assert.AreEqual(3, pop.FrameIndexAt(5, 2), "and holds its last frame");
        Assert.AreEqual(0, pop.FrameIndexAt(0, -2), "a start before the first frame holds the first");
        Assert.AreEqual(3, walk.FrameIndexAt(0, -2), "a looping clip counts backwards from its end");
        Assert.AreEqual(4, walk.FrameIndexAt(0, -0.5f));
        Assert.AreEqual(0, walk.FrameIndexAt(0, float.NaN), "an offset that is not a number is none");
    }

    [TestMethod]
    public void An_offset_counts_frames_in_a_clip_with_a_duration_per_frame()
    {
        var frames = Images(3).Select(i => new Sprite(null!, new RectF(0, 0, i.Width, 4), default)).ToArray();
        var clip = new AnimationClip(frames, [0.1f, 0.3f, 0.2f]);
        Assert.AreEqual(0, clip.FrameIndexAt(0));
        Assert.AreEqual(1, clip.FrameIndexAt(0, 1), "one frame ahead: the start of the second frame");
        Assert.AreEqual(1, clip.FrameIndexAt(0.25f, 1), "0.25 s into a frame of 0.3 s");
        Assert.AreEqual(2, clip.FrameIndexAt(0.35f, 1));
        Assert.AreEqual(1, clip.FrameIndexAt(0, 1.999f), "almost two frames ahead: still the end of the second frame");
        Assert.AreEqual(2, clip.FrameIndexAt(0, 2));
        Assert.AreEqual(0, clip.FrameIndexAt(0, 3), "a whole pass ahead is the start");
        Assert.AreEqual(2, clip.FrameIndexAt(0, -1), "one frame back from the start is the last frame");
        Assert.AreSame(frames[1], clip.FrameAt(0.05f, 0.5f), "half a frame ahead of 0.05 s is 0.1 s: the second frame");

        var once = new AnimationClip(frames, [0.1f, 0.3f, 0.2f], loop: false);
        Assert.AreEqual(2, once.FrameIndexAt(0, 7), "past the end holds the last frame");
        Assert.AreEqual(1, once.FrameIndexAt(0.05f, 1));
    }
}
