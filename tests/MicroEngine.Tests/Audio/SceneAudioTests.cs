using System.Numerics;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.Testing;

namespace MicroEngine.Tests.Audio;

/// <summary>
/// A scene's sounds in space (<see cref="SceneAudio"/>): a listener and emitters in world units. What is heard is
/// checked on the samples a capture device renders; the values each emitter was given are read from the emitter.
/// </summary>
[TestClass]
public sealed class SceneAudioTests
{
    private const int Rate = 48000, StepFrames = Rate / 60;

    private sealed class TestGame(Scene first) : Game
    {
        public override GameSettings Settings { get; } = new("scene-audio");
        protected override void Load()
        {
            Audio.AddSound("hum", new AudioClip([.. Enumerable.Repeat(0.5f, Rate)], Rate));
            Audio.AddSound("blip", new AudioClip([.. Enumerable.Repeat(0.5f, Rate / 10)], Rate));
            // A second that rises from 0 to 0.5: its level tells how far into it a voice is.
            Audio.AddSound("rise", new AudioClip([.. Enumerable.Range(0, Rate).Select(i => 0.5f * i / Rate)], Rate));
            Scenes.Push(first);
        }
    }

    private sealed class Level : Scene
    {
        public Action<Level>? OnEnter, OnUpdate;
        public override void Enter() => OnEnter?.Invoke(this);
        public override void Update(float dt) => OnUpdate?.Invoke(this);
        public override void Draw(IRenderer renderer) { }
    }

    private static (TestGame Game, CaptureAudioDevice? Capture, Level Scene) Start(Action<Level>? enter = null, bool silent = false)
    {
        var scene = new Level { OnEnter = enter };
        var game = new TestGame(scene);
        var capture = silent ? null : new CaptureAudioDevice(60, Rate);
        var platform = silent ? new HeadlessPlatform() : HeadlessPlatform.For(new PlatformRequest(game.Settings) { Audio = () => capture! });
        game.InitializeForTest(platform);
        game.Audio.SfxVolume = 1;
        return (game, capture, scene);
    }

    private static void Step(Game game, int count = 1)
    {
        for (int i = 0; i < count; i++) game.StepForTest();
    }

    private static (float Left, float Right) Heard(CaptureAudioDevice capture, int frame) => (capture.Output.Samples[frame * 2], capture.Output.Samples[frame * 2 + 1]);

    // ---- Distance and side

    [TestMethod]
    public void An_emitter_is_heard_by_its_distance_and_its_side_of_the_listener()
    {
        var (game, capture, scene) = Start();
        Step(game);
        var sounds = scene.Sounds;
        Assert.AreEqual((16f, 320f, SoundRolloff.Linear), (sounds.MinDistance, sounds.MaxDistance, sounds.Rolloff), "the defaults");
        var emitter = sounds.Emit("hum", new Vector2(160, 0), loop: true);
        Assert.IsTrue(emitter.IsActive && emitter.IsAudible, "it starts with the step that made it");
        Step(game, 3);
        float gain = 1 - (160 - 16) / (320f - 16);
        Assert.AreEqual(gain, emitter.Gain, 1e-6f, "a straight line between the two distances");
        Assert.AreEqual(0.75f, emitter.Pan, 1e-6f, "half way to the distance where it is all on one side");
        int frame = (int)(capture!.Duration * Rate) - 100;
        var (left, right) = Heard(capture, frame);
        Assert.AreEqual(0.5f * gain, right, 1e-5f, "the right speaker has it whole");
        Assert.AreEqual(0.5f * gain * 0.5f, left, 1e-5f, "the left one half of it");

        emitter.Position = new Vector2(-400, 0);
        Step(game, 2);
        Assert.AreEqual(0, emitter.Gain, "past its maximum distance");
        Assert.IsFalse(emitter.IsAudible, "it holds no voice");
        Assert.IsTrue(emitter.IsActive, "a loop waits out of range");
        Step(game, 6);
        Assert.AreEqual(0, game.Audio.Statistics.ActiveVoices);
        Assert.AreEqual((0f, 0f), Heard(capture, (int)(capture.Duration * Rate) - 10));

        emitter.Position = new Vector2(0, 8);
        emitter.Volume = 0.5f;
        Step(game, 5);
        Assert.AreEqual((1f, 0.5f), (emitter.Gain, emitter.Pan), "within its minimum distance: full, in the middle");
        var close = Heard(capture, (int)(capture.Duration * Rate) - 10);
        Assert.AreEqual(0.25f, close.Left, 1e-5f, "its own volume scales it");
        Assert.AreEqual(0.25f, close.Right, 1e-5f);
    }

    [TestMethod]
    public void The_curve_of_the_fall_is_chosen_per_emitter()
    {
        var (game, _, scene) = Start(silent: true);
        Step(game);
        var sounds = scene.Sounds;
        sounds.MinDistance = 10;
        sounds.MaxDistance = 110;
        float Gain(float distance, SoundRolloff rolloff, Func<float, float>? curve = null)
        {
            var emitter = sounds.Emit("hum", new Vector2(0, distance), loop: true);
            emitter.Rolloff = rolloff;
            emitter.Curve = curve;
            Step(game);
            float gain = emitter.Gain;
            emitter.Stop();
            return gain;
        }
        foreach (var rolloff in Enum.GetValues<SoundRolloff>())
        {
            Assert.AreEqual(1, Gain(0, rolloff), $"{rolloff} at the source");
            Assert.AreEqual(1, Gain(10, rolloff), $"{rolloff} at the minimum distance");
            Assert.AreEqual(0, Gain(110, rolloff), $"{rolloff} at the maximum distance");
            Assert.AreEqual(0, Gain(5000, rolloff), $"{rolloff} far away");
            float previous = 1;
            for (float d = 15; d < 110; d += 5)
            {
                float gain = Gain(d, rolloff);
                Assert.IsTrue(gain < previous && gain > 0, $"{rolloff} falls all the way: {gain} at {d} after {previous}");
                previous = gain;
            }
        }
        Assert.AreEqual(0.5f, Gain(60, SoundRolloff.Linear), 1e-6f);
        Assert.AreEqual(0.5f, Gain(60, SoundRolloff.Smooth), 1e-6f);
        Assert.AreEqual((10f / 20 - 10f / 110) / (1 - 10f / 110), Gain(20, SoundRolloff.Inverse), 1e-6f, "one over the distance: half at twice the minimum, less its value at the maximum");
        Assert.IsTrue(Gain(35, SoundRolloff.Inverse) < Gain(35, SoundRolloff.Linear) && Gain(35, SoundRolloff.Linear) < Gain(35, SoundRolloff.Smooth));

        Assert.AreEqual(0.75f, Gain(60, SoundRolloff.Linear, t => 1 - t * t), 1e-6f, "a curve of the game's own");
        Assert.AreEqual(1, Gain(60, SoundRolloff.Linear, _ => 7), "kept within 0 and 1");
        Assert.AreEqual(0, Gain(60, SoundRolloff.Linear, _ => -3));
        Assert.AreEqual(0, Gain(60, SoundRolloff.Linear, _ => float.NaN));
    }

    [TestMethod]
    public void The_pan_width_is_the_scenes_or_each_emitters_maximum_distance()
    {
        var (game, _, scene) = Start(silent: true);
        Step(game);
        var sounds = scene.Sounds;
        var emitter = sounds.Emit("hum", new Vector2(-80, 0), loop: true);
        Step(game);
        Assert.AreEqual(0.5f - 0.5f * 80 / 320, emitter.Pan, 1e-6f);
        sounds.PanDistance = 160;
        Step(game);
        Assert.AreEqual(0.25f, emitter.Pan, 1e-6f);
        emitter.Position = new Vector2(-200, 0);
        Step(game);
        Assert.AreEqual(0, emitter.Pan, "all on the left from the pan distance on");
        emitter.Position = new Vector2(0, -100);
        Step(game);
        Assert.AreEqual(0.5f, emitter.Pan, "above the listener is in the middle");
    }

    // ---- Following

    [TestMethod]
    public void An_emitter_follows_its_entity_without_a_click_and_stops_when_it_is_destroyed()
    {
        var (game, capture, scene) = Start();
        Step(game);
        var bee = scene.Objects.Create();
        scene.Objects.Local(bee).Position2D = new Vector2(-300, 0);
        var emitter = scene.Sounds.Emit("hum", bee, loop: true);
        Assert.AreEqual(bee, emitter.Entity);
        // It flies past the listener: six units a step, from the left to the right.
        scene.OnUpdate = s => { if (s.Objects.IsAlive(bee)) s.Objects.Local(bee).Position2D += new Vector2(6, 0); };
        int first = (int)(capture!.Duration * Rate);
        Step(game, 100);
        Assert.AreEqual(new Vector2(300, 0), emitter.Position, "it is where its entity is");
        var samples = capture.Output.Samples;
        int last = (int)(capture.Duration * Rate);
        float loudestLeft = 0, loudestRight = 0, jump = 0;
        for (int f = first + 1; f < last; f++)
        {
            jump = Math.Max(jump, Math.Max(Math.Abs(samples[f * 2] - samples[f * 2 - 2]), Math.Abs(samples[f * 2 + 1] - samples[f * 2 - 1])));
            loudestLeft = Math.Max(loudestLeft, samples[f * 2]);
            loudestRight = Math.Max(loudestRight, samples[f * 2 + 1]);
        }
        Assert.AreEqual(0.5f, loudestLeft, 1e-3f, "full volume as it passes");
        Assert.AreEqual(0.5f, loudestRight, 1e-3f);
        // A step moves the gain by 6/304 at most and the pan by 6/320: spread over 800 frames a sample moves a few
        // hundred-thousandths; a change made at once would be two hundred times that.
        Assert.IsTrue(jump < 5e-5f, $"the level glides from each step's value to the next's (largest change between two samples: {jump})");
        Assert.IsTrue(samples[(first + 10 * StepFrames) * 2] > samples[(first + 10 * StepFrames) * 2 + 1], "on the left before it passes");
        Assert.IsTrue(samples[(last - 10 * StepFrames) * 2] < samples[(last - 10 * StepFrames) * 2 + 1], "on the right after");

        scene.Objects.Destroy(bee);
        Step(game, 2);
        Assert.IsFalse(emitter.IsActive, "its entity is gone");
        Assert.AreEqual(0, scene.Sounds.Emitters.Count);
        Step(game, 6);
        Assert.AreEqual(0, game.Audio.Statistics.ActiveVoices, "and its voice has stopped");
        Assert.ThrowsExactly<ArgumentException>(() => scene.Sounds.Emit("hum", bee));
        Assert.ThrowsExactly<KeyNotFoundException>(() => scene.Sounds.Emit("none", Vector2.Zero));
    }

    [TestMethod]
    public void The_listener_is_an_entity_a_point_or_the_camera()
    {
        var (game, _, scene) = Start(silent: true);
        Step(game);
        var sounds = scene.Sounds;
        var emitter = sounds.Emit("hum", new Vector2(100, 0), loop: true);
        Step(game);
        Assert.AreEqual(Vector2.Zero, sounds.Listener, "the world's origin with nothing else");
        Assert.AreEqual(0.5f + 0.5f * 100 / 320, emitter.Pan, 1e-6f);

        scene.WorldCamera = new Camera2D(320, 180) { Position = new Vector2(40, 0) };
        Step(game);
        Assert.AreEqual(new Vector2(200, 90), sounds.Listener, "the centre of the scene's camera");
        Assert.AreEqual(0.5f - 0.5f * 100 / 320, emitter.Pan, 1e-6f);

        sounds.Listener = new Vector2(100, 0);
        Step(game);
        Assert.AreEqual((1f, 0.5f), (emitter.Gain, emitter.Pan), "a point the game sets");

        var hero = scene.Objects.Create();
        scene.Objects.Local(hero).Position2D = new Vector2(100, 160);
        sounds.Follow(hero);
        Step(game);
        Assert.AreEqual(new Vector2(100, 160), sounds.Listener);
        Assert.AreEqual(1 - (160 - 16) / 304f, emitter.Gain, 1e-6f, "an entity");
        scene.Objects.Destroy(hero);
        Step(game, 2);
        Assert.AreEqual(new Vector2(100, 160), sounds.Listener, "when it is destroyed the listener stays where it was");

        sounds.Listener = null;
        Step(game);
        Assert.AreEqual(new Vector2(200, 90), sounds.Listener, "back to the camera");
        sounds.Listener = new Vector2(float.NaN, 0);
        Assert.AreEqual(new Vector2(200, 90), sounds.Listener, "a point that is not a number is no point");
    }

    // ---- Lifetime

    [TestMethod]
    public void A_sound_that_plays_once_is_over_when_it_ends_and_never_heard_when_out_of_range()
    {
        var (game, capture, scene) = Start();
        Step(game);
        var near = scene.Sounds.Emit("blip", new Vector2(0, 0));
        var far = scene.Sounds.Emit("blip", new Vector2(5000, 0));
        Assert.IsTrue(near.IsAudible);
        Assert.IsFalse(far.IsAudible);
        Step(game, 2);
        Assert.IsFalse(far.IsActive, "out of range when it happened: gone");
        Assert.IsTrue(near.IsActive);
        Step(game, 6);
        Assert.IsFalse(near.IsActive, "a tenth of a second later it has ended");
        Assert.AreEqual(0, scene.Sounds.Emitters.Count);
        Assert.AreEqual(1, capture!.Played.Count, "one sound was played");
        Assert.AreEqual(0.5f, Heard(capture, StepFrames + 100).Left, 1e-5f);
        near.Stop();
        Assert.IsFalse(near.IsAudible);
    }

    [TestMethod]
    public void Emitters_out_of_range_hold_no_voice_and_a_loop_comes_back_where_it_would_be()
    {
        var (game, capture, scene) = Start();
        Step(game);
        var sounds = scene.Sounds;
        var crowd = Enumerable.Range(0, 200).Select(i => sounds.Emit("hum", new Vector2(1000 + i * 10, 0), loop: true)).ToList();
        var rise = sounds.Emit("rise", new Vector2(2000, 0), loop: true);
        Step(game, 30);
        Assert.AreEqual(0, game.Audio.Statistics.ActiveVoices, "two hundred emitters, none within earshot");
        Assert.AreEqual(0, game.Audio.Statistics.RefusedSounds);
        Assert.AreEqual(201, sounds.Emitters.Count);

        sounds.Listener = new Vector2(2000, 0);
        Step(game, 6);
        Assert.IsTrue(rise.IsAudible);
        Assert.AreEqual(32, game.Audio.Statistics.ActiveVoices, "those in range take the voices there are");
        foreach (var emitter in crowd) emitter.Stop(0);
        Step(game, 6);
        // It was made 42 steps ago and came into range 12 steps ago: it is 0.7 s into its second, not 0.2 s.
        int frame = (int)(capture!.Duration * Rate) - 1;
        Assert.AreEqual(0.5f * (42 / 60f), Heard(capture, frame).Left, 0.02f, "it plays from where it would be had it been heard all along");

        sounds.StopAll(0);
        Assert.AreEqual(0, sounds.Emitters.Count);
        Assert.IsFalse(rise.IsActive);
    }

    [TestMethod]
    public void A_scene_that_leaves_takes_its_sounds_with_it()
    {
        var (game, capture, scene) = Start();
        Step(game);
        var loop = scene.Sounds.Emit("hum", Vector2.Zero, loop: true);
        var entity = scene.Objects.Create();
        var followed = scene.Sounds.Emit("hum", entity, loop: true);
        Step(game, 3);
        Assert.AreEqual(2, game.Audio.Statistics.ActiveVoices);
        game.Scenes.Pop();
        Step(game, 12);
        Assert.IsFalse(loop.IsActive || followed.IsActive);
        Assert.AreEqual(0, game.Audio.Statistics.ActiveVoices, "faded out over a tenth of a second");
        Assert.AreEqual((0f, 0f), Heard(capture!, (int)(capture!.Duration * Rate) - 10));
        loop.Stop();
        Assert.IsFalse(loop.IsAudible);
    }

    // ---- Doppler

    [TestMethod]
    public void The_doppler_shift_is_off_until_asked_for_and_follows_the_speed_of_approach()
    {
        var (game, capture, scene) = Start();
        Step(game);
        var sounds = scene.Sounds;
        sounds.MaxDistance = 100000;
        var car = sounds.Emit("hum", new Vector2(-3000, 0), loop: true);
        car.Pitch = 1.5f;
        // 340 units a second towards the listener, then past it.
        scene.OnUpdate = _ => car.Position += new Vector2(340 / 60f, 0);
        Step(game, 3);
        Assert.AreEqual(1.5f, car.PlayedPitch, "no shift by default");

        sounds.Doppler = 1;
        Step(game, 3);
        Assert.AreEqual(1.5f * 3400 / (3400 - 340f), car.PlayedPitch, 1e-3f, "coming closer at a tenth of the speed of sound: a ninth higher");
        car.Position = new Vector2(60000, 0);
        Step(game);

        Assert.AreEqual(1.5f * 0.5f, car.PlayedPitch, 1e-6f, "a jump away is kept to an octave");
        Step(game, 3);
        Assert.AreEqual(1.5f * 3400 / (3400 + 340f), car.PlayedPitch, 1e-3f, "going away: lower");

        sounds.SpeedOfSound = 680;
        sounds.Doppler = 2;
        Step(game, 2);
        Assert.AreEqual(1.5f * 680 / (680 + 680f), car.PlayedPitch, 1e-3f, "a slower sound and twice the effect");
        sounds.Doppler = float.NaN;
        sounds.SpeedOfSound = -5;
        Assert.AreEqual((2f, 680f), (sounds.Doppler, sounds.SpeedOfSound), "values that make no sense are not taken");
        Assert.IsTrue(capture!.Output.Samples.All(float.IsFinite));
    }

    // ---- A device without sound, state and garbage

    [TestMethod]
    public void A_silent_device_lists_each_sound_once_and_decides_nothing()
    {
        var hashes = new List<string>();
        foreach (bool silent in new[] { true, false })
        {
            var (game, _, scene) = Start(silent: silent);
            Step(game);
            var entity = scene.Objects.Create();
            scene.Sounds.Emit("hum", entity, loop: true);
            scene.Sounds.Emit("blip", new Vector2(30, 0));
            scene.Sounds.Emit("blip", new Vector2(9000, 0));
            scene.OnUpdate = s => s.Objects.Local(entity).Position2D += new Vector2(1, 0);
            Step(game, 20);
            hashes.Add(game.ComputeStateHash().ToString());
            if (silent)
            {
                var device = (SilentAudioDevice)game.Audio.Device;
                CollectionAssert.AreEqual(new[] { "hum", "blip" }, device.Played, "each audible sound once, not once per step");
                Assert.AreEqual(1, scene.Sounds.Emitters.Count, "the loop waits; the sounds that play once are over");
            }
        }
        Assert.AreEqual(hashes[0], hashes[1], "the state of a run does not depend on whether its sounds are heard");
    }

    [TestMethod]
    public void Places_and_distances_that_are_not_usable_numbers_do_not_break_a_step()
    {
        float[] odd = [float.NaN, float.PositiveInfinity, float.NegativeInfinity, -1, 0, float.MaxValue, float.MinValue, float.Epsilon];
        var (game, capture, scene) = Start();
        Step(game);
        var sounds = scene.Sounds;
        foreach (float value in odd)
        {
            sounds.MinDistance = value;
            sounds.MaxDistance = value;
            sounds.PanDistance = value;
            sounds.StopFade = value;
            var a = sounds.Emit("hum", new Vector2(value, 10), loop: true, volume: value);
            var b = sounds.Emit("hum", new Vector2(5, value), loop: true);
            (b.MinDistance, b.MaxDistance, b.Pitch, b.Volume) = (value, value, value, value);
            var c = sounds.Emit("blip", new Vector2(1, 1), volume: value);
            (c.MinDistance, c.MaxDistance) = (0, value);
            sounds.Listener = new Vector2(value, value);
            Step(game, 3);
            sounds.Listener = null;
            Step(game, 3);
            foreach (var emitter in new[] { a, b, c })
                Assert.IsTrue(emitter.Gain is >= 0 and <= 1 && emitter.Pan is >= 0 and <= 1 && float.IsFinite(emitter.PlayedPitch), $"{value}: gain {emitter.Gain}, pan {emitter.Pan}, pitch {emitter.PlayedPitch}");
            sounds.StopAll(value);
        }
        Step(game, 10);
        Assert.IsTrue(capture!.Output.Samples.All(float.IsFinite), "every sample rendered is a number");
        Assert.AreEqual(0, game.Audio.Statistics.ActiveVoices);
    }

    [TestMethod]
    public void A_step_with_moving_emitters_allocates_nothing()
    {
        var (game, _, scene) = Start();
        Step(game);
        var sounds = scene.Sounds;
        sounds.Doppler = 1;
        var entities = Enumerable.Range(0, 40).Select(_ => scene.Objects.Create()).ToArray();
        for (int i = 0; i < entities.Length; i++)
        {
            scene.Objects.Local(entities[i]).Position2D = new Vector2(i * 20 - 400, i % 5 * 30);
            sounds.Emit("hum", entities[i], loop: true);
        }
        var system = sounds;
        float phase = 0;
        void Move()
        {
            // The emitters sweep back and forth through the listener's range; the scene's own step is left out of the
            // measurement, which is of this system.
            phase += 0.05f;
            for (int i = 0; i < entities.Length; i++)
                scene.Objects.Local(entities[i]).Position2D = new Vector2(i * 20 - 400 + 300 * (phase % 2 < 1 ? phase % 1 : 1 - phase % 1), i % 5 * 30);
            system.CallLateUpdate(1 / 60f);
            game.Audio.Update(1 / 60f);
        }
        AllocationAssert.None(Move, warmup: 50, calls: 100, rounds: 5, because: "placing emitters every step");
        Assert.IsTrue(game.Audio.Statistics.ActiveVoices > 0);
    }
}
