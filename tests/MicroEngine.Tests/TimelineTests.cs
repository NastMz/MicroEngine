using System.Numerics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace MicroEngine.Tests;

/// <summary>Keyframe tracks and the timeline that plays them with named events.</summary>
[TestClass]
public sealed class TimelineTests
{
    [TestMethod]
    public void A_track_interpolates_between_its_keys_and_holds_its_ends()
    {
        var track = Track.Float(new(0.5f, 10), new(1.5f, 20), new(2.5f, 0));
        Assert.AreEqual(2.5f, track.Duration);
        Assert.AreEqual(10, track.At(0), "Before the first key: its value.");
        Assert.AreEqual(10, track.At(0.5f));
        Assert.AreEqual(15, track.At(1f), 1e-5f);
        Assert.AreEqual(20, track.At(1.5f));
        Assert.AreEqual(10, track.At(2f), 1e-5f);
        Assert.AreEqual(0, track.At(2.5f));
        Assert.AreEqual(0, track.At(99), "After the last key: its value.");
    }

    [TestMethod]
    public void A_key_eases_the_segment_that_arrives_at_it()
    {
        var track = Track.Float(new(0, 0), new(1, 100, Ease.InQuad), new(2, 200));
        Assert.AreEqual(25, track.At(0.5f), 1e-4f, "InQuad(0.5) = 0.25 of the way to the eased key.");
        Assert.AreEqual(150, track.At(1.5f), 1e-4f, "The next segment has no ease: linear.");
    }

    [TestMethod]
    public void Tracks_exist_for_points_colours_and_any_type_with_its_own_blend()
    {
        var point = Track.Vector2(new(0, new Vector2(0, 10)), new(2, new Vector2(4, 30)));
        Assert.AreEqual(new Vector2(2, 20), point.At(1));
        var colour = Track.Color(new(0, new Color(0, 0, 0, 255)), new(1, new Color(200, 100, 50, 255)));
        Assert.AreEqual(new Color(100, 50, 25, 255), colour.At(0.5f));
        var text = new Track<string>((a, b, t) => t < 0.5f ? a : a + b, new(0, "a"), new(1, "b"));
        Assert.AreEqual("ab", text.At(0.75f));
    }

    [TestMethod]
    public void A_step_track_holds_each_value_until_the_next_key()
    {
        var track = Track.Step<string>(new(0, "idle"), new(0.3f, "raise"), new(0.6f, "strike"));
        Assert.AreEqual("idle", track.At(0.29f));
        Assert.AreEqual("raise", track.At(0.3f), "A key's own time shows its value.");
        Assert.AreEqual("raise", track.At(0.59f));
        Assert.AreEqual("strike", track.At(5));
    }

    [TestMethod]
    public void A_track_refuses_no_keys_and_keys_out_of_order()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Track.Float());
        Assert.ThrowsExactly<ArgumentException>(() => Track.Float(new(1, 0), new(1, 5)));
        Assert.ThrowsExactly<ArgumentException>(() => Track.Float(new(1, 0), new(0.5f, 5)));
        Assert.ThrowsExactly<ArgumentException>(() => Track.Float(new Keyframe<float>(float.NaN, 0)));
        Assert.ThrowsExactly<ArgumentNullException>(() => new Track<float>(null!, new Keyframe<float>(0, 1)));
        Assert.ThrowsExactly<ArgumentNullException>(() => new Track<float>(MathX.Lerp, null!));
    }

    [TestMethod]
    public void A_timeline_applies_every_track_at_its_playhead()
    {
        float x = -1, angle = -1;
        var timeline = new Timeline(2)
            .Add(Track.Float(new(0, 0), new(2, 20)), v => x = v)
            .Add(Track.Float(new(0, 0), new(1, 90)), v => angle = v);
        timeline.Update(0.5f);
        Assert.AreEqual(0.5f, timeline.Time);
        Assert.AreEqual(5, x, 1e-5f);
        Assert.AreEqual(45, angle, 1e-5f);
        timeline.Update(1f);
        Assert.AreEqual(15, x, 1e-5f);
        Assert.AreEqual(90, angle, "A track shorter than the timeline holds its last key.");
        Assert.IsFalse(timeline.Finished);
    }

    [TestMethod]
    public void A_timeline_that_does_not_loop_ends_on_its_last_values_and_stops_applying()
    {
        float x = 0;
        int applied = 0;
        var timeline = new Timeline(1).Add(Track.Float(new(0, 0), new(1, 10)), v => { x = v; applied++; });
        timeline.Update(0.6f);
        timeline.Update(0.6f);
        Assert.IsTrue(timeline.Finished);
        Assert.AreEqual(1f, timeline.Time);
        Assert.AreEqual(10, x);
        Assert.AreEqual(2, applied);
        timeline.Update(0.6f);
        Assert.AreEqual(2, applied, "A finished timeline leaves the values to the game.");
    }

    [TestMethod]
    public void Events_fire_once_in_time_order_when_the_playhead_reaches_them()
    {
        var seen = new List<string>();
        var timeline = new Timeline(1).At(0.75f, "land").At(0, "start").At(0.25f, "raise").At(0.25f, "whoosh").At(1, "end");
        timeline.Reached += seen.Add;
        timeline.Update(0.1f);
        CollectionAssert.AreEqual(new[] { "start" }, seen, "An event at 0 fires in the first update.");
        timeline.Update(0.15f);
        CollectionAssert.AreEqual(new[] { "start", "raise", "whoosh" }, seen, "Reaching the time exactly fires; equal times keep the order they were added in.");
        timeline.Update(0.1f);
        Assert.AreEqual(3, seen.Count, "Nothing fires twice.");
        timeline.Update(5f);
        CollectionAssert.AreEqual(new[] { "start", "raise", "whoosh", "land", "end" }, seen, "One long update fires everything it passed.");
        timeline.Update(1f);
        Assert.AreEqual(5, seen.Count);
    }

    [TestMethod]
    public void Events_see_the_values_of_the_update_that_reached_them()
    {
        float x = 0, atEvent = -1;
        var timeline = new Timeline(1).Add(Track.Float(new(0, 0), new(1, 10)), v => x = v).At(0.5f, "half");
        timeline.Reached += _ => atEvent = x;
        timeline.Update(0.6f);
        Assert.AreEqual(6, atEvent, 1e-5f);
    }

    [TestMethod]
    public void A_looping_timeline_starts_over_and_fires_its_events_every_pass()
    {
        var seen = new List<string>();
        float x = 0;
        var timeline = new Timeline(1, loop: true).Add(Track.Float(new(0, 0), new(1, 10)), v => x = v).At(0, "start").At(0.5f, "step");
        timeline.Reached += seen.Add;
        timeline.Update(0.75f);
        timeline.Update(0.5f);
        Assert.AreEqual(0.25f, timeline.Time, 1e-5f);
        Assert.AreEqual(2.5f, x, 1e-4f);
        Assert.IsFalse(timeline.Finished);
        CollectionAssert.AreEqual(new[] { "start", "step", "start" }, seen);
        timeline.Update(2f);
        CollectionAssert.AreEqual(new[] { "start", "step", "start", "step", "start", "step", "start" }, seen, "An update longer than the timeline fires every pass it covered.");
    }

    [TestMethod]
    public void Speed_scales_time_and_zero_pauses()
    {
        var timeline = new Timeline(4) { Speed = 2 };
        timeline.Update(0.5f);
        Assert.AreEqual(1f, timeline.Time);
        timeline.Speed = 0;
        timeline.Update(1f);
        Assert.AreEqual(1f, timeline.Time);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timeline.Speed = -1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timeline.Speed = float.NaN);
    }

    [TestMethod]
    public void Seek_moves_the_playhead_applies_and_fires_nothing()
    {
        var seen = new List<string>();
        float x = 0;
        var timeline = new Timeline(1).Add(Track.Float(new(0, 0), new(1, 10)), v => x = v).At(0.25f, "a").At(0.75f, "b");
        timeline.Reached += seen.Add;
        timeline.Seek(0.5f);
        Assert.AreEqual(5, x, 1e-5f);
        Assert.AreEqual(0, seen.Count);
        timeline.Update(0.5f);
        CollectionAssert.AreEqual(new[] { "b" }, seen, "Only what lies after the place it was moved to.");
        Assert.IsTrue(timeline.Finished);
        timeline.Seek(0);
        Assert.IsFalse(timeline.Finished);
        timeline.Update(0.3f);
        CollectionAssert.AreEqual(new[] { "b", "a" }, seen, "Moved back to the start, it plays again.");
        timeline.Seek(7);
        Assert.AreEqual(1f, timeline.Time, "A place beyond the end is the end.");
    }

    [TestMethod]
    public void An_event_handler_may_move_the_playhead()
    {
        var seen = new List<string>();
        var timeline = new Timeline(1).At(0.25f, "back").At(0.5f, "never");
        timeline.Reached += name => { seen.Add(name); if (seen.Count == 1) timeline.Seek(0); };
        timeline.Update(0.9f);
        CollectionAssert.AreEqual(new[] { "back" }, seen, "Moving the playhead drops the rest of that update's events.");
        Assert.AreEqual(0f, timeline.Time);
    }

    [TestMethod]
    public void A_timeline_refuses_what_cannot_play()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Timeline(0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Timeline(float.PositiveInfinity));
        var timeline = new Timeline(1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timeline.At(1.5f, "late"));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timeline.At(-0.1f, "early"));
        Assert.ThrowsExactly<ArgumentNullException>(() => timeline.At(0.5f, null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => timeline.Add(null!, (float _) => { }));
        Assert.ThrowsExactly<ArgumentNullException>(() => timeline.Add(Track.Float(new(0, 0), new(1, 1)), null!));
    }

    [TestMethod]
    public void Two_runs_with_the_same_steps_hash_the_same_and_the_playhead_changes_the_hash()
    {
        static ulong Run(int steps)
        {
            var timeline = new Timeline(1, loop: true);
            for (int i = 0; i < steps; i++) timeline.Update(1 / 60f);
            var hash = new StateHasher();
            timeline.HashState(hash);
            return hash.Value;
        }
        Assert.AreEqual(Run(30), Run(30));
        Assert.AreNotEqual(Run(30), Run(31));
    }
}
