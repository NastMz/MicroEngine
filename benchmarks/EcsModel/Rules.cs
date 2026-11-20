using System.Globalization;
using System.Numerics;
using System.Text;

namespace EcsModel;

/// <summary>What a projectile carries.</summary>
public enum Element { None, Fire, Ice }

/// <summary>The numbers both models share, so a difference between them is a difference of behaviour.</summary>
public static class Rules
{
    public const float Dt = 1f / 60f, Width = 320, Height = 180;
    /// <summary>A projectile hits the nearest thing within this distance (the lowest tag when two are equally near).</summary>
    public const float HitRadius = 6;
    public const int ShotLife = 180;
    /// <summary>Burning: lasts <see cref="BurnSteps"/>, hurts by 1 every <see cref="BurnTick"/> steps and, at each of those, reaches everything within <see cref="SpreadRadius"/>.</summary>
    public const int BurnSteps = 120, BurnTick = 20;
    public const float SpreadRadius = 24;
    /// <summary>Frozen: cannot move for this many steps.</summary>
    public const int FrozenSteps = 90;
}

/// <summary>What the script asks for at one step. Things are named by a tag the script chooses, never by a store's id.</summary>
public abstract record Command(int Step);
public sealed record SpawnEnemy(int Step, int Tag, Vector2 At, Vector2 Velocity, int Health) : Command(Step);
public sealed record Shoot(int Step, Vector2 From, Vector2 Velocity, int Damage, Element Element = Element.None) : Command(Step);
public sealed record Shield(int Step, int Tag) : Command(Step);
public sealed record SpawnCrate(int Step, int Tag, Vector2 At, int Health) : Command(Step);

/// <summary>What a game would observe of one thing after a step.</summary>
public readonly record struct Seen(int Tag, char Kind, Vector2 At, int Health, bool Burning, bool Frozen, bool Shielded);

/// <summary>
/// One version of the slice. The step has a fixed order, the same in every version: commands, movement, projectile
/// hits, status effects, deaths.
/// </summary>
public interface ISlice
{
    void Step(IReadOnlyList<Command> due);
    /// <summary>Every thing alive, in any order.</summary>
    List<Seen> Things();
    int Shots { get; }
    int Kills { get; }
    int CratesBroken { get; }
    /// <summary>The designer's late question (step 3): the tags of everything burning within <paramref name="radius"/> of <paramref name="center"/> that is not shielded.</summary>
    List<int> BurningUnshieldedNear(Vector2 center, float radius);
}

public static class Script
{
    /// <summary>
    /// The script of a step of the comparison. Step 1: enemies and plain projectiles. Step 2 adds fire, ice and
    /// shields, placed so every rule and every interaction happens. Step 3 adds crates among the enemies.
    /// </summary>
    public static List<Command> For(int step)
    {
        var script = new List<Command>();
        // A row of slow walkers, a tight group that fire can spread through, and two fast ones that bounce.
        for (int i = 0; i < 6; i++) script.Add(new SpawnEnemy(0, i, new Vector2(40 + i * 40, 40), new Vector2(0, 6 + i), 12));
        for (int i = 0; i < 5; i++) script.Add(new SpawnEnemy(0, 10 + i, new Vector2(150 + i * 12, 120), new Vector2(i - 2, 0), 9));
        script.Add(new SpawnEnemy(0, 20, new Vector2(10, 90), new Vector2(170, 35), 20));
        script.Add(new SpawnEnemy(0, 21, new Vector2(300, 20), new Vector2(-140, 90), 20));
        if (step >= 3)
            for (int i = 0; i < 4; i++) script.Add(new SpawnCrate(0, 30 + i, new Vector2(140 + i * 24, 140), 6));
        if (step >= 2)
        {
            script.Add(new Shield(5, 12));
            script.Add(new Shield(5, 2));
            script.Add(new Shield(5, step >= 3 ? 31 : 3));   // a crate cannot be shielded: the command does nothing
        }

        // Volleys from below at the walkers and from the left through the group, a new one every 20 steps.
        for (int volley = 0; volley < 24; volley++)
        {
            int at = 10 + volley * 20;
            var element = step < 2 ? Element.None : (volley % 4) switch { 0 => Element.Fire, 1 => Element.None, 2 => Element.Ice, _ => Element.Fire };
            script.Add(new Shoot(at, new Vector2(40 + volley % 6 * 40, 170), new Vector2(0, -150), 3, element));
            script.Add(new Shoot(at, new Vector2(100, 120), new Vector2(160, 0), 2, element));
            // Two at once at the same target: which one a shield absorbs must not depend on the store's order.
            script.Add(new Shoot(at + 3, new Vector2(100, 121), new Vector2(160, 0), 1, volley % 3 == 0 ? Element.Ice : Element.None));
            if (step >= 3) script.Add(new Shoot(at + 7, new Vector2(140 + volley % 4 * 24, 175), new Vector2(0, -120), 1, volley % 2 == 0 ? Element.Fire : Element.Ice));
        }
        return script;
    }

    public const int Steps = 600;

    /// <summary>Plays the script and returns one line per step of what a game would observe.</summary>
    public static List<string> Trace(ISlice slice, int step, int steps = Steps)
    {
        var script = For(step);
        var trace = new List<string>(steps);
        var due = new List<Command>();
        var text = new StringBuilder();
        for (int i = 0; i < steps; i++)
        {
            due.Clear();
            foreach (var command in script) if (command.Step == i) due.Add(command);
            slice.Step(due);
            text.Clear();
            text.Append(CultureInfo.InvariantCulture, $"{i} shots {slice.Shots} kills {slice.Kills} crates {slice.CratesBroken}");
            var things = slice.Things();
            things.Sort((a, b) => a.Tag.CompareTo(b.Tag));
            foreach (var t in things)
                text.Append(CultureInfo.InvariantCulture, $" | {t.Kind}{t.Tag} {t.At.X:0.###},{t.At.Y:0.###} hp{t.Health}{(t.Burning ? " B" : "")}{(t.Frozen ? " F" : "")}{(t.Shielded ? " S" : "")}");
            if (step >= 3)
                text.Append(" | near ").AppendJoin(',', slice.BurningUnshieldedNear(new Vector2(176, 130), 40));
            trace.Add(text.ToString());
        }
        return trace;
    }
}
