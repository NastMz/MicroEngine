using System.Numerics;

namespace MicroEngine.Tests;

/// <summary>The edges of a keyframe track: its ends, its keys, the keys it refuses.</summary>
[TestClass]
public sealed class TrackTests
{
    [TestMethod]
    public void A_track_of_one_key_is_that_value_at_any_time()
    {
        var track = Track.Float(new Keyframe<float>(2, 7));
        Assert.AreEqual(2f, track.Duration);
        foreach (float time in new[] { -5f, 0f, 2f, 9f, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            Assert.AreEqual(7, track.At(time), $"{time}");
    }

    [TestMethod]
    public void Outside_its_keys_a_track_holds_the_nearest_end_and_a_time_that_is_not_a_number_reads_the_first_key()
    {
        var track = Track.Float(new(-1, 5), new(1, 15));
        Assert.AreEqual(5, track.At(-2));
        Assert.AreEqual(5, track.At(float.NegativeInfinity));
        Assert.AreEqual(10, track.At(0), 1e-5f, "Keys may lie before 0.");
        Assert.AreEqual(15, track.At(float.MaxValue));
        Assert.AreEqual(15, track.At(float.PositiveInfinity));
        Assert.AreEqual(5, track.At(float.NaN));
    }

    [TestMethod]
    public void At_a_keys_time_the_value_is_the_keys_whatever_its_curve()
    {
        static float Half(float _) => 0.5f;
        var track = Track.Float(new(0, 0), new(1, 100, Half), new(2, 300, Half));
        Assert.AreEqual(0, track.At(0));
        Assert.AreEqual(100, track.At(1));
        Assert.AreEqual(300, track.At(2));
        Assert.AreEqual(50, track.At(0.999f), "Before the key its curve decides.");
        Assert.AreEqual(200, track.At(1.001f));
    }

    [TestMethod]
    public void A_step_track_switches_exactly_at_the_keys_time()
    {
        var track = Track.Step<int>(new(0, 1), new(0.5f, 2), new(1, 3));
        Assert.AreEqual(1, track.At(MathF.BitDecrement(0.5f)));
        Assert.AreEqual(2, track.At(0.5f));
        Assert.AreEqual(2, track.At(MathF.BitDecrement(1f)));
        Assert.AreEqual(3, track.At(1f));
        Assert.AreEqual(1, track.At(-1), "Before the first key: its value.");
    }

    [TestMethod]
    public void A_curve_that_overshoots_reaches_the_blend()
    {
        static float Over(float _) => 1.5f;
        Assert.AreEqual(15, Track.Float(new(0, 0), new(1, 10, Over)).At(0.5f));
        Assert.AreEqual(new Vector2(15, -15), Track.Vector2(new(0, Vector2.Zero), new(1, new Vector2(10, -10), Over)).At(0.5f));
        Assert.AreEqual(new Color(200, 100, 50, 255), Track.Color(new(0, new Color(0, 0, 0, 255)), new(1, new Color(200, 100, 50, 255), Over)).At(0.5f), "A colour stops at the key.");
    }

    [TestMethod]
    public void A_track_refuses_equal_times_times_out_of_order_and_times_that_are_not_finite()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Track.Step<int>());
        Assert.ThrowsExactly<ArgumentException>(() => Track.Step<int>(new(0, 1), new(0, 2)), "Two keys at one time.");
        Assert.ThrowsExactly<ArgumentException>(() => Track.Vector2(new(2, Vector2.Zero), new(1, Vector2.One)), "Out of order: keys are not sorted for you.");
        Assert.ThrowsExactly<ArgumentException>(() => Track.Float(new(0, 0), new(float.PositiveInfinity, 1)));
        Assert.ThrowsExactly<ArgumentException>(() => Track.Float(new(float.NegativeInfinity, 0), new(0, 1)));
        Assert.ThrowsExactly<ArgumentException>(() => Track.Float(new(0, 0), new(float.NaN, 1)));
    }

    [TestMethod]
    public void A_track_cannot_be_changed_after_it_is_created()
    {
        var keys = new Keyframe<float>[] { new(0, 0), new(1, 10) };
        var track = Track.Float(keys);
        keys[1] = new(1, 99);
        Assert.AreEqual(10, track.At(1), "The keys were copied.");
        Assert.AreEqual(2, track.Keys.Count);
        Assert.AreEqual(new Keyframe<float>(1, 10), track.Keys[1]);
        Assert.IsFalse(track.Keys is Keyframe<float>[], "The list of keys does not give its storage away.");
    }
}
