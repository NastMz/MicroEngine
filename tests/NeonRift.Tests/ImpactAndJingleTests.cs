using System.Numerics;
using MicroEngine.Scenes;
using NeonRift.Gameplay;
using static NeonRift.Tests.Harness;

namespace NeonRift.Tests;

/// <summary>Each weapon leaves its own impact, armour throws sparks, and the last boss falls to the victory jingle.</summary>
[TestClass]
public sealed class ImpactAndJingleTests
{
    [TestMethod]
    [DataRow(WeaponKind.Pulse, "shot_hit")]
    [DataRow(WeaponKind.Spread, "hit2")]
    [DataRow(WeaponKind.Lance, "hit4")]
    [DataRow(WeaponKind.Rapid, "hit1")]
    public void A_shot_hitting_an_enemy_leaves_its_weapons_impact(WeaponKind kind, string sprite)
    {
        var stage = Settled();
        var weapon = WeaponSpec.Of(kind);
        var target = stage.Spawn(Boss.Create(Content.BossKind.Gunship, stage.Player.Position + new Vector2(120, -40)));
        // Past the fly-in, so the hit lands on the hull.
        Run(stage, 60 * 4, PlayerInput.None);
        stage.Spawn(new PlayerShot(weapon, target.Center - new Vector2(4, 0), new Vector2(weapon.Speed, 0)));
        Run(stage, 2, PlayerInput.None);

        Assert.IsTrue(stage.Effects.Any(e => e.Sprite == sprite), $"{kind}: {string.Join(", ", stage.Effects.Select(e => e.Sprite))}");
    }

    [TestMethod]
    public void A_shot_glancing_off_armour_throws_sparks()
    {
        var stage = Settled();
        var pod = stage.Spawn(new EggPod(stage.Player.Position + new Vector2(120, 0)));
        Assert.IsTrue(pod.Armored, "a closed egg pod is armoured");
        Assert.IsTrue(pod.Hit(1, pod.Center), "armour stops the shot");
        Assert.AreEqual(12, pod.Health, "armour takes no damage");
        Run(stage, 1, PlayerInput.None);
        Assert.IsTrue(stage.Effects.Any(e => e.Sprite == "hit3"), string.Join(", ", stage.Effects.Select(e => e.Sprite)));
    }

    [TestMethod]
    [DataRow(false, "fanfare")]
    [DataRow(true, "victory")]
    public void The_boss_of_the_last_stage_falls_to_the_victory_jingle(bool final, string jingle)
    {
        var sounds = new List<string>();
        var stage = new Gameplay.Stage(Flat(60), new Session(), new SceneWorld()) { Final = final, SoundSink = (name, _, _, _) => sounds.Add(name) };
        stage.Player.God = true;
        Run(stage, 60 * 12, In(right: true));
        Assert.IsNotNull(stage.Boss, $"Phase {stage.Phase}");
        Run(stage, 60 * 3, PlayerInput.None);
        while (stage.Boss.Alive) stage.Boss.Hit(1000, stage.Boss.Center);
        Run(stage, (int)(Gameplay.Stage.ClearTime * 60) + 2, PlayerInput.None);

        Assert.IsTrue(sounds.Contains(jingle), string.Join(", ", sounds.Distinct()));
        Assert.IsFalse(sounds.Contains(final ? "fanfare" : "victory"));
    }
}
