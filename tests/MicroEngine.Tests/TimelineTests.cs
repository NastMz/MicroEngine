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

    // ---- Events ----

    [TestMethod]
    public void An_event_at_the_duration_fires_once_when_the_timeline_ends()
    {
        var seen = new List<string>();
        var timeline = new Timeline(1).At(1, "end");
        timeline.Reached += seen.Add;
        timeline.Update(0.5f);
        Assert.AreEqual(0, seen.Count);
        timeline.Update(0.5f);
        CollectionAssert.AreEqual(new[] { "end" }, seen, "Landing exactly on the end reaches it.");
        Assert.IsTrue(timeline.Finished);
        timeline.Update(0.5f);
        Assert.AreEqual(1, seen.Count, "A finished timeline raises nothing more.");
    }

    [TestMethod]
    public void Events_at_the_start_and_at_the_duration_of_a_loop_fire_once_per_pass()
    {
        var seen = new List<string>();
        var timeline = new Timeline(1, loop: true).At(0, "start").At(1, "end");
        timeline.Reached += seen.Add;
        timeline.Update(1f);
        CollectionAssert.AreEqual(new[] { "start", "end", "start" }, seen, "The end of one pass, then the start of the next.");
        Assert.AreEqual(0f, timeline.Time, "A looping playhead that lands on the end is at the start.");
        timeline.Update(0.5f);
        Assert.AreEqual(3, seen.Count, "The start it landed on already fired.");
        timeline.Update(0.75f);
        CollectionAssert.AreEqual(new[] { "start", "end", "start", "end", "start" }, seen);
    }

    [TestMethod]
    public void An_update_of_several_passes_fires_every_pass_in_order_and_ends_at_the_wrapped_time()
    {
        var seen = new List<string>();
        float x = 0;
        var track = Track.Float(new(0, 0), new(1, 10));
        var timeline = new Timeline(1, loop: true).Add(track, v => x = v).At(0, "a").At(0.5f, "b");
        timeline.Reached += seen.Add;
        timeline.Update(3.25f);
        CollectionAssert.AreEqual(new[] { "a", "b", "a", "b", "a", "b", "a" }, seen);
        Assert.AreEqual(0.25f, timeline.Time, 1e-5f);
        Assert.AreEqual(track.At(timeline.Time), x, "The value applied is the track's value at the wrapped playhead.");
    }

    [TestMethod]
    public void Every_handler_of_an_update_sees_the_same_playhead_and_values()
    {
        float x = 0;
        var times = new List<float>();
        var values = new List<float>();
        var timeline = new Timeline(1).Add(Track.Float(new(0, 0), new(1, 10)), v => x = v).At(0.1f, "a").At(0.2f, "b").At(0.3f, "c");
        timeline.Reached += _ => { times.Add(timeline.Time); values.Add(x); };
        timeline.Update(0.4f);
        CollectionAssert.AreEqual(new[] { 0.4f, 0.4f, 0.4f }, times);
        CollectionAssert.AreEqual(new[] { 4f, 4f, 4f }, values);
    }

    // ---- Large and unusual steps ----

    [TestMethod]
    public void A_huge_step_on_a_looping_timeline_ends_and_raises_a_bounded_number_of_passes()
    {
        int count = 0;
        var timeline = new Timeline(1, loop: true).At(0.5f, "tick");
        timeline.Reached += _ => count++;
        // A step so long that subtracting one pass from it changes nothing in single precision.
        Assert.IsTrue(Task.Run(() => timeline.Update(1e9f)).Wait(TimeSpan.FromSeconds(5)), "The update must end.");
        Assert.IsTrue(timeline.Time >= 0 && timeline.Time < 1, $"The playhead stays inside the pass (was {timeline.Time}).");
        Assert.IsTrue(count is >= 999 and <= 1001, $"One update raises the events of at most 1,000 passes (raised {count}).");

        var tiny = new Timeline(1e-6f, loop: true).At(0, "tick");
        int ticks = 0;
        tiny.Reached += _ => ticks++;
        Assert.IsTrue(Task.Run(() => tiny.Update(3600)).Wait(TimeSpan.FromSeconds(5)), "The update must end.");
        Assert.IsTrue(ticks <= 1001, $"Raised {ticks}.");
        Assert.IsTrue(tiny.Time >= 0 && tiny.Time < tiny.Duration);
    }

    [TestMethod]
    public void A_huge_step_on_a_timeline_that_does_not_loop_ends_it_once()
    {
        var seen = new List<string>();
        var timeline = new Timeline(1).At(0, "start").At(1, "end");
        timeline.Reached += seen.Add;
        timeline.Update(float.MaxValue);
        CollectionAssert.AreEqual(new[] { "start", "end" }, seen);
        Assert.AreEqual(1f, timeline.Time);
        Assert.IsTrue(timeline.Finished);
    }

    [TestMethod]
    public void A_step_of_zero_applies_the_tracks_and_raises_nothing()
    {
        var seen = new List<string>();
        int applied = 0;
        var timeline = new Timeline(1).Add(Track.Float(new(0, 0), new(1, 10)), _ => applied++).At(0, "start");
        timeline.Reached += seen.Add;
        timeline.Update(0);
        Assert.AreEqual(1, applied);
        Assert.AreEqual(0, seen.Count, "Nothing was played yet.");
        timeline.Update(0.1f);
        timeline.Update(0);
        timeline.Update(0.1f);
        CollectionAssert.AreEqual(new[] { "start" }, seen, "The event at 0 fires once, in the first update that plays.");
    }

    [TestMethod]
    public void A_step_that_is_negative_or_not_finite_is_refused_and_changes_nothing()
    {
        int applied = 0;
        var timeline = new Timeline(1, loop: true).Add(Track.Float(new(0, 0), new(1, 10)), _ => applied++);
        timeline.Update(0.25f);
        foreach (float bad in new[] { -0.1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timeline.Update(bad), $"{bad}");
        Assert.AreEqual(0.25f, timeline.Time);
        Assert.AreEqual(1, applied);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timeline.Speed = float.PositiveInfinity);
    }

    [TestMethod]
    public void Speed_changed_while_playing_applies_from_the_next_update()
    {
        var seen = new List<string>();
        var timeline = new Timeline(2).At(0.5f, "a").At(1.5f, "b");
        timeline.Reached += seen.Add;
        timeline.Update(0.25f);
        timeline.Speed = 2;
        timeline.Update(0.25f);
        Assert.AreEqual(0.75f, timeline.Time);
        CollectionAssert.AreEqual(new[] { "a" }, seen);
        timeline.Speed = 0.5f;
        timeline.Update(1f);
        Assert.AreEqual(1.25f, timeline.Time);
        timeline.Speed = 4;
        timeline.Update(1f);
        CollectionAssert.AreEqual(new[] { "a", "b" }, seen);
        Assert.IsTrue(timeline.Finished);
    }

    // ---- Seek ----

    [TestMethod]
    public void Seeking_back_plays_the_events_again_and_seeking_forward_skips_them()
    {
        var seen = new List<string>();
        var timeline = new Timeline(1).At(0.2f, "a").At(0.4f, "b").At(0.6f, "c").At(0.8f, "d");
        timeline.Reached += seen.Add;
        timeline.Update(0.5f);
        timeline.Seek(0.1f);
        Assert.AreEqual(2, seen.Count, "Moving the playhead raises nothing.");
        timeline.Update(0.2f);
        CollectionAssert.AreEqual(new[] { "a", "b", "a" }, seen, "Played again from where it was moved to.");
        timeline.Seek(0.7f);
        timeline.Update(0.2f);
        CollectionAssert.AreEqual(new[] { "a", "b", "a", "d" }, seen, "The events it jumped over never fire.");
    }

    [TestMethod]
    public void An_event_at_the_place_sought_fires_once_in_the_next_update_that_plays()
    {
        var seen = new List<string>();
        var timeline = new Timeline(1, loop: true).At(0.5f, "here");
        timeline.Reached += seen.Add;
        timeline.Seek(0.5f);
        timeline.Update(0);
        Assert.AreEqual(0, seen.Count);
        timeline.Update(0.1f);
        timeline.Update(0.1f);
        CollectionAssert.AreEqual(new[] { "here" }, seen);
    }

    [TestMethod]
    public void Seeking_to_the_end_finishes_a_timeline_that_does_not_loop()
    {
        var seen = new List<string>();
        float x = 0;
        int applied = 0;
        var timeline = new Timeline(1).Add(Track.Float(new(0, 0), new(1, 10)), v => { x = v; applied++; }).At(1, "end");
        timeline.Reached += seen.Add;
        timeline.Seek(1);
        Assert.IsTrue(timeline.Finished);
        Assert.AreEqual(10, x, "The tracks were applied at the end.");
        Assert.AreEqual(1, applied);
        timeline.Update(0.1f);
        Assert.AreEqual(1, applied, "A finished timeline leaves the values to the game, however it got to its end.");
        Assert.AreEqual(0, seen.Count, "Moving the playhead raises nothing, the event at the end included.");
        timeline.Seek(0.5f);
        Assert.IsFalse(timeline.Finished, "Moved back from the end, it plays again from there.");
        Assert.AreEqual(5, x, 1e-5f);
        timeline.Update(1f);
        CollectionAssert.AreEqual(new[] { "end" }, seen);
        Assert.IsTrue(timeline.Finished);
    }

    [TestMethod]
    public void Seek_keeps_the_playhead_inside_the_timeline_and_refuses_a_time_that_is_not_a_number()
    {
        var seen = new List<string>();
        float x = -1;
        var timeline = new Timeline(1, loop: true).Add(Track.Float(new(0, 0), new(1, 10)), v => x = v).At(0, "start").At(1, "end");
        timeline.Reached += seen.Add;
        timeline.Seek(-3);
        Assert.AreEqual(0f, timeline.Time);
        Assert.AreEqual(0, x);
        timeline.Seek(float.PositiveInfinity);
        Assert.AreEqual(1f, timeline.Time, "Beyond the end is the end, on a looping timeline too: it does not wrap.");
        Assert.AreEqual(10, x);
        Assert.IsFalse(timeline.Finished);
        timeline.Update(0.25f);
        CollectionAssert.AreEqual(new[] { "end", "start" }, seen, "From the end, a loop reaches its end event and starts over.");
        Assert.AreEqual(0.25f, timeline.Time, 1e-5f);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timeline.Seek(float.NaN));
        Assert.AreEqual(0.25f, timeline.Time, 1e-5f, "A refused seek changes nothing.");
    }

    // ---- Loop boundary, duration, hash ----

    [TestMethod]
    public void The_value_applied_is_always_the_tracks_value_at_the_playhead_across_the_wrap()
    {
        float x = 0;
        var track = Track.Float(new(0, 0), new(0.5f, 10, Ease.OutCubic), new(1, 0, Ease.InCubic));
        var timeline = new Timeline(1, loop: true).Add(track, v => x = v);
        for (int i = 0; i < 200; i++)
        {
            timeline.Update(0.07f);
            Assert.IsTrue(timeline.Time >= 0 && timeline.Time < 1, $"Step {i}: {timeline.Time}");
            Assert.AreEqual(track.At(timeline.Time), x, $"Step {i}");
        }
    }

    [TestMethod]
    public void The_duration_is_the_timelines_own_not_its_tracks()
    {
        float x = 0;
        var seen = new List<string>();
        var timeline = new Timeline(2).Add(Track.Float(new(0, 0), new(4, 40)), v => x = v).At(2, "late");
        timeline.Reached += seen.Add;
        Assert.AreEqual(2f, timeline.Duration);
        timeline.Update(5);
        Assert.AreEqual(20, x, 1e-4f, "A track longer than the timeline is played up to the timeline's end.");
        CollectionAssert.AreEqual(new[] { "late" }, seen, "An event may lie after the last key of every track.");
    }

    [TestMethod]
    public void Equal_states_hash_the_same_and_a_seek_changes_the_hash()
    {
        static ulong Hash(Timeline timeline)
        {
            var hash = new StateHasher();
            timeline.HashState(hash);
            return hash.Value;
        }
        static Timeline Played(bool loop, params float[] steps)
        {
            var timeline = new Timeline(1, loop).At(0.5f, "half");
            foreach (float step in steps) timeline.Update(step);
            return timeline;
        }
        var a = Played(true, 0.25f, 0.25f);
        var b = Played(true, 0.25f, 0.25f);
        Assert.AreEqual(Hash(a), Hash(b));
        b.Seek(0.25f);
        Assert.AreNotEqual(Hash(a), Hash(b), "Another place.");
        b.Seek(0.5f);
        Assert.AreNotEqual(Hash(a), Hash(b), "The same place, but the event there is still to fire.");

        var ended = Played(false, 2);
        var sought = Played(false);
        sought.Seek(1);
        Assert.AreEqual(Hash(ended), Hash(sought), "Finished is finished, however it got there.");
    }
}
