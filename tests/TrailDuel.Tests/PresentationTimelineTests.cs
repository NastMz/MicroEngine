using System.Text;
using MicroEngine.Scenes;
using TrailDuel.Scenes;

namespace TrailDuel.Tests;

/// <summary>
/// What the battle's and the overworld's presentation does, and on which update: every sound that starts, every time the
/// battle's menu opens or closes and every change of screen, for runs that go through the intro, each kind of move,
/// damage, a faint, experience, a thrown orb, a trainer's challenge and the fades of a door and of a defeat. The
/// animations are sequences a coroutine waits for (eases, pauses, one-shot effects), so a sound a line later than
/// before is an animation that changed length. The lists were recorded from the game as it was before its eases moved
/// to the engine's tweens and its effects to <c>Scene.Effects</c>; they are kept as one number each (the list's hash)
/// and a few landmarks, and <see cref="Describe"/> prints the whole list when one fails.
/// </summary>
[TestClass]
public sealed class PresentationTimelineTests
{
    /// <summary>Runs a debug start for <paramref name="updates"/> updates with actions held from an update for a number of updates, and lists what happened.</summary>
    private static List<string> Record(string start, int updates, params (int Update, string Action, int Held)[] presses)
    {
        using var h = new Harness(start);
        var events = new List<string>();
        var played = h.Platform.SilentAudio.Played;
        int heard = played.Count;
        bool choosing = false;
        string screen = Screen(h);
        for (int update = 1; update <= updates; update++)
        {
            foreach (var (at, action, held) in presses)
            {
                if (at == update) h.Game.Input.SetForced(action, true);
                if (at + held == update) h.Game.Input.SetForced(action, false);
            }
            h.Step();
            for (; heard < played.Count; heard++) events.Add($"{update} {played[heard]}");
            bool now = h.Game.Scenes.Current is BattleScene { AwaitingChoice: true };
            if (now != choosing) events.Add($"{update} {(now ? "menu" : "busy")}");
            choosing = now;
            if (Screen(h) is var s && s != screen) events.Add($"{update} > {screen = s}");
        }
        return events;
    }

    private static string Screen(Harness h) => h.Game.Scenes.Current?.GetType().Name ?? "none";

    /// <summary><paramref name="first"/>, then <paramref name="action"/> pressed (held two updates) every <paramref name="every"/> updates.</summary>
    private static (int Update, string Action, int Held)[] Every(int from, int to, int every, string action, params (int Update, string Action, int Held)[] first) =>
        [.. first, .. Enumerable.Range(0, (to - from) / every + 1).Select(i => (from + i * every, action, 2))];

    /// <summary>FNV-1a of the list, a line per event.</summary>
    private static uint Hash(List<string> events)
    {
        uint hash = 2166136261;
        foreach (byte b in Encoding.UTF8.GetBytes(string.Join("\n", events))) hash = (hash ^ b) * 16777619;
        return hash;
    }

    private static string Describe(List<string> events) => $"{events.Count} events, hash {Hash(events)}:\n{string.Join("\n", events)}";

    private static void AssertTimeline(List<string> events, int count, uint hash, params string[] landmarks)
    {
        foreach (string landmark in landmarks) CollectionAssert.Contains(events, landmark, Describe(events));
        Assert.AreEqual((count, hash), (events.Count, Hash(events)), Describe(events));
    }

    /// <summary>The master's battle: the trainer's send-outs, then FIGHT and the first move every time the menu is up.</summary>
    [TestMethod]
    public void TheTrainerBattlePlaysEverySoundOnTheSameUpdate()
    {
        var events = Record("trainer", 3000, Every(300, 3000, 45, "confirm"));
        AssertTimeline(events, TrainerCount, TrainerHash, TrainerLandmarks);
    }

    /// <summary>A wild battle: the bag, an orb thrown (it breaks free), then RUN.</summary>
    [TestMethod]
    public void TheWildBattleWithAThrownOrbPlaysEverySoundOnTheSameUpdate()
    {
        var events = Record("battle", 1700, Every(1200, 1700, 60, "confirm",
            (550, "right", 2), (560, "confirm", 2), (600, "down", 2), (610, "down", 2), (620, "confirm", 2)));
        AssertTimeline(events, WildCount, WildHash, WildLandmarks);
    }

    /// <summary>The effects showcase: every move of the list used by each side in turn, with its impact.</summary>
    [TestMethod]
    public void TheMoveShowcasePlaysEverySoundOnTheSameUpdate()
    {
        var events = Record("fx", 2400);
        AssertTimeline(events, ShowcaseCount, ShowcaseHash, ShowcaseLandmarks);
    }

    /// <summary>Route 1: the trainer's exclamation bubble, the walk, the challenge, a lost battle and the fades back to the shrine.</summary>
    [TestMethod]
    public void ALostTrainerBattleFadesBackToTheShrineOnTheSameUpdates()
    {
        var events = Record("defeat", 2600, Every(200, 2600, 30, "confirm", (30, "up", 62)));
        AssertTimeline(events, DefeatCount, DefeatHash, DefeatLandmarks);
    }

    /// <summary>The shrine by the brook: the healer's question, the glow, and the door out with its fades.</summary>
    [TestMethod]
    public void TheShrineHealsAndTheDoorFadesOnTheSameUpdates()
    {
        var events = Record("brook", 1500, Every(460, 1100, 40, "confirm",
            (150, "up", 16), (180, "left", 125), (320, "up", 40), (410, "up", 30), (1150, "down", 200)));
        AssertTimeline(events, ShrineCount, ShrineHash, ShrineLandmarks);
    }

    private const int TrainerCount = 222, WildCount = 109, ShowcaseCount = 54, DefeatCount = 106, ShrineCount = 93;
    private const uint TrainerHash = 534255765, WildHash = 1541805968, ShowcaseHash = 2959043912, DefeatHash = 1737478019, ShrineHash = 3023760486;
    private static readonly string[] TrainerLandmarks = ["325 throw", "346 sendout", "375 atk:Gale", "465 menu", "595 statdown", "740 hit", "2146 faint", "2236 > PartyScene"];
    private static readonly string[] WildLandmarks = ["56 atk:Leaf", "393 menu", "741 throw", "771 sendout", "810 land", "831 breakfree", "1121 status", "1561 heal"];
    private static readonly string[] ShowcaseLandmarks = ["392 atk:Normal", "426 hit", "816 atk:Flame", "858 hit", "2152 atk:Volt", "2186 hit"];
    private static readonly string[] DefeatLandmarks = ["91 exclaim", "261 encounter", "330 > BattleScene", "874 faint", "1083 > OverworldScene", "1130 text"];
    private static readonly string[] ShrineLandmarks = ["342 door", "500 text", "661 jingle:heal", "820 text"];
}
