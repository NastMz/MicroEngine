using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Platform;
using MicroEngine.Testing;
using NeonRift.Gameplay;
using NeonRift.Scenes;

namespace NeonRift.Tests;

/// <summary>
/// What happens in a run, update by update, written down without the state it is kept in: where every actor is, what
/// the player's pose is, which sounds start, which frame every effect shows, what the drawn timers read. The digests
/// were recorded before the stage's timers, phases and menus moved onto the engine's pieces, and every run must keep
/// giving them: the state hash may change with how state is kept, what a player sees and hears on each update may not.
/// Set NEON_PINS_DIR to a folder to get each run's lines (one per update), to find the update a change starts on.
/// </summary>
[TestClass]
public sealed class PinnedBehaviourTests
{
    /// <summary>Each run: its name, the script (or none: stepped until the play scene leaves), the launch options and its digest.</summary>
    public static IEnumerable<object[]> Runs =>
    [
        ["auto1", "", 1, true, true, "Pulse", "1584D4152E2D8DD5"],
        ["auto2", "", 2, true, true, "Pulse", "CA059921B35B6863"],
        ["auto3", "", 3, true, true, "Pulse", "3428DE0D05EAD196"],
        ["mortal1", "", 1, false, true, "Pulse", "B1D6256F052EC6D8"],
        ["mortal2", "", 2, false, true, "Pulse", "E026007A1A2E3885"],
        ["mortal3", "", 3, false, true, "Pulse", "1E72A917C7B5AF14"],
        ["stage1", "stage1", 1, false, false, "Pulse", "E4A665EB2A615952"],
        ["poses", "poses", 1, true, false, "Pulse", "D8483A9CD477442E"],
        ["tour", "tour", 1, true, false, "Pulse", "B67ED64D2472D2FC"],
        ["tour-mortal", "tour", 1, false, false, "Pulse", "B1381037F8C3AF97"],
        ["tour-spread", "tour", 1, true, false, "Spread", "0A607EB56794FA3E"],
        ["tour-lance", "tour", 1, true, false, "Lance", "456A4EF0C549145C"],
        ["tour-rapid", "tour", 1, true, false, "Rapid", "68A7A73A791CA815"],
        ["touch", "touch", 1, false, false, "Pulse", "56E75D9EDF1E5D59"],
    ];

    [TestMethod]
    [DynamicData(nameof(Runs))]
    public void ARunDoesTheSameThingsOnTheSameUpdates(string name, string script, int stage, bool god, bool auto, string weapon, string digest)
    {
        var game = new RiftGame(new LaunchOptions("stage", stage, Weapon: Enum.Parse<WeaponKind>(weapon), God: god, Auto: auto));
        var log = new RunLog(game);
        if (script.Length == 0)
        {
            game.InitializeForTest(HeadlessPlatform.For(game.Settings));
            for (int i = 0; i < 60 * 60 * 8 && game.Scenes.Current is PlayScene; i++)
            {
                game.StepForTest();
                log.Record();
            }
            Assert.IsNotInstanceOfType<PlayScene>(game.Scenes.Current, $"{name}: the run never left the stage.");
        }
        else
        {
            string captures = Path.Combine(Path.GetTempPath(), "neon-rift-pins", Guid.NewGuid().ToString("N"));
            using var platform = new RecordingPlatform(HeadlessPlatform.For(game.Settings), log);
            var result = GameRunner.Run(game, platform, new RunOptions
            {
                Script = StateTraceRuns.Script($"games/NeonRift/scripts/{script}.script"),
                Seed = 1,
                Mute = true,
                CaptureDirectory = captures,
            });
            if (Directory.Exists(captures)) Directory.Delete(captures, recursive: true);
            if (result.Error is not null) throw new AssertFailedException($"The run failed: {result.Error}", result.Error);
        }
        string text = log.ToString();
        if (Environment.GetEnvironmentVariable("NEON_PINS_DIR") is { Length: > 0 } dir)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, name + ".txt"), text);
        }
        string actual = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..16];
        Assert.IsTrue(log.Updates > 200, $"{name}: only {log.Updates} updates of play were recorded.");
        Assert.AreEqual(digest, actual, $"{name}: {log.Updates} updates of play recorded; what happens on some update changed.");
    }

    /// <summary>Writes one line per update of play.</summary>
    private sealed class RunLog(RiftGame game)
    {
        private readonly StringBuilder _text = new();
        private readonly StringBuilder _heard = new();
        private Stage? _hooked;
        private long _recorded = -1;

        public int Updates { get; private set; }

        public override string ToString() => _text.ToString();

        private static string B(float value) => BitConverter.SingleToUInt32Bits(value).ToString("x8", CultureInfo.InvariantCulture);
        private static string V(Vector2 value) => B(value.X) + B(value.Y);

        /// <summary>Records the update just made, once; called after every step or before every frame.</summary>
        public void Record()
        {
            if (game.UpdateCount == _recorded || game.UpdateCount == 0) return;
            _recorded = game.UpdateCount;
            var t = _text;
            t.Append(CultureInfo.InvariantCulture, $"{game.UpdateCount} {game.Scenes.Current?.GetType().Name}");
            if (game.Scenes.Current is not PlayScene { Stage: { } stage })
            {
                t.Append('\n');
                return;
            }
            Updates++;
            if (_hooked != stage)
            {
                // The sounds and music of the stage, in the order it asks for them.
                _hooked = stage;
                var sound = stage.SoundSink;
                stage.SoundSink = (n, volume, pitch, pan) =>
                {
                    _heard.Append(CultureInfo.InvariantCulture, $" snd:{n},{B(volume)},{B(pitch)},{B(pan)}");
                    sound?.Invoke(n, volume, pitch, pan);
                };
                var music = stage.MusicSink;
                stage.MusicSink = n =>
                {
                    _heard.Append(CultureInfo.InvariantCulture, $" mus:{n}");
                    music?.Invoke(n);
                };
            }
            var s = stage.Session;
            var p = stage.Player;
            t.Append(CultureInfo.InvariantCulture, $" {stage.Phase} fin={stage.Finished} cam={V(stage.Camera.Position)} cp={stage.CheckpointIndex} ses={s.Score},{s.Lives},{s.Deaths},{s.Weapon}");
            // What drawing reads of the phase's time: the warning's blink, the clear text, the game over's wait.
            t.Append(CultureInfo.InvariantCulture, $" blink={(int)(stage.PhaseTime * 4) % 2} clear={stage.PhaseTime > 3.2f} over={stage.PhaseTime > 1.2f}");
            t.Append(CultureInfo.InvariantCulture, $" | P {p.State} {V(p.Position)} {V(p.Velocity)} f={p.Facing} g={p.Grounded} c={p.Crouching} s={p.Somersault} aim={V(p.Aim)} hp={p.Health} inv={B(p.Invulnerable)} bar={B(p.Barrier)} w={p.Weapon.Kind} run={p.RunFrame} mz={V(p.Muzzle())}");
            if (p.State == PlayerState.Dying) t.Append(CultureInfo.InvariantCulture, $" dying={B(p.StateTime)}");
            if (stage.Boss is { } boss) t.Append(CultureInfo.InvariantCulture, $" | B {boss.Alive} {V(boss.Position)} in={boss.Entering} hp={boss.Health}");
            t.Append(" | E");
            foreach (var e in stage.Enemies)
            {
                t.Append(CultureInfo.InvariantCulture, $" {e.GetType().Name}:{V(e.Position)}:{e.Health}:{(e.Armored ? 1 : 0)}");
                if (e is EggPod egg) t.Append(egg.Open ? 'o' : 'c');
            }
            t.Append(" | S");
            foreach (var shot in stage.PlayerShots) t.Append(CultureInfo.InvariantCulture, $" {V(shot.Position)}:{V(shot.Velocity)}");
            t.Append(" | X");
            foreach (var shot in stage.EnemyShots) t.Append(CultureInfo.InvariantCulture, $" {V(shot.Position)}:{V(shot.Velocity)}:{(shot.Heavy ? 1 : 0)}");
            t.Append(" | C");
            foreach (var c in stage.Pickups) t.Append(CultureInfo.InvariantCulture, $" {c.Letter}:{V(c.Position)}");
            t.Append(" | F");
            foreach (var (clip, at, scale, frame) in ShownEffects(stage)) t.Append(CultureInfo.InvariantCulture, $" {clip}:{V(at)}:{B(scale)}:{frame}");
            t.Append(" |").Append(_heard).Append('\n');
            _heard.Clear();
        }

        /// <summary>The effects the stage shows after an update: the clip, where, how large and which frame.</summary>
        private IEnumerable<(string Clip, Vector2 At, float Scale, int Frame)> ShownEffects(Stage stage)
        {
            foreach (var fx in stage.Effects)
                yield return (fx.Clip, Vector2.Round(fx.Position), fx.Scale, game.Art.Clip(fx.Clip).FrameIndexAt(fx.Age));
        }
    }

    /// <summary>A headless platform that records the run's log before every frame is drawn (one frame per update in a scripted run).</summary>
    private sealed class RecordingPlatform(HeadlessPlatform inner, RunLog log) : IPlatform
    {
        public IRenderer BeginFrame()
        {
            log.Record();
            return inner.BeginFrame();
        }

        public void EndFrame() => inner.EndFrame();
        public IGraphicsDevice Graphics => inner.Graphics;
        public MicroEngine.Audio.IAudioDevice Audio => inner.Audio;
        public MicroEngine.Input.IInputSource Input => inner.Input;
        public IMonotonicClock Clock => inner.Clock;
        public bool PumpEvents(IEngineEventSink sink) => inner.PumpEvents(sink);
        public ISurface? MainSurface => inner.MainSurface;
        public bool CanBlock => inner.CanBlock;
        public bool TryGetCapability<T>([NotNullWhen(true)] out T? capability) where T : class => inner.TryGetCapability(out capability);
        public Color Letterbox { get => inner.Letterbox; set => inner.Letterbox = value; }
        public void SaveScreenshot(string path) => inner.SaveScreenshot(path);
        public void ToggleFullscreen() => inner.ToggleFullscreen();
        public void Dispose() => inner.Dispose();
    }
}
