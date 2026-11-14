using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace Examples;

/// <summary>
/// Keyframe tracks played by timelines: a strike that moves an arm, colours an anvil and names its pose from one
/// playhead, with an event at the moment it lands, over a breath that loops on its own timeline.
/// </summary>
public sealed class TimelineExample : Scene
{
    private static readonly Color Rest = Color.Hex("566c86"), Hot = Color.Hex("ffcd75");

    #region tracks
    // A track is data: keys in seconds, each with the curve of the way to it. It holds no playhead, so every figure
    // that moves this way can share it.
    private static readonly Track<float> Swing = Track.Float(
        new(0, 0), new(0.35f, -2.2f, Ease.OutCubic), new(0.5f, 0.35f, Ease.InCubic), new(0.9f, 0, Ease.OutBack));
    private static readonly Track<Color> Glow = Track.Color(
        new(0, Rest), new(0.5f, Rest), new(0.52f, Hot), new(0.9f, Rest));
    // A step track holds each value until the next key: for sprites, names and flags.
    private static readonly Track<string> Poses = Track.Step<string>(
        new(0, "RAISE"), new(0.35f, "STRIKE"), new(0.5f, "READY"));
    private static readonly Track<float> Breath = Track.Float(
        new(0, 0), new(0.8f, 3, Ease.InOutSine), new(1.6f, 0, Ease.InOutSine));
    #endregion

    /// <summary>The arm's angle in radians: 0 rests on the anvil, negative is raised.</summary>
    public float Arm { get; private set; }
    /// <summary>How far the breath lowers the body, in pixels.</summary>
    public float Bob { get; private set; }
    /// <summary>The name of the pose the strike is in.</summary>
    public string Pose { get; private set; } = "READY";
    /// <summary>Times the strike landed.</summary>
    public int Hits { get; private set; }
    private Color _anvil = Rest;

    #region timeline
    private readonly Timeline _strike = new(0.9f);
    private readonly Timeline _breath = new(1.6f, loop: true);

    public override void Enter()
    {
        _strike
            .Add(Swing, angle => Arm = angle)        // each update: the track's value at the playhead
            .Add(Glow, colour => _anvil = colour)
            .Add(Poses, pose => Pose = pose)
            .At(0.5f, "hit");                        // a named moment the game reacts to
        _strike.Reached += name => { if (name == "hit") Hits++; };
        _strike.Seek(_strike.Duration);              // starts finished, at rest

        _breath.Add(Breath, y => Bob = y);           // another timeline over another value: a layer
    }
    #endregion

    #region update
    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        if (Input.Pressed("confirm") && _strike.Finished) _strike.Seek(0);   // plays it again from the start
        _breath.Update(dt);
        _strike.Update(dt);
    }
    #endregion

    public override void Draw(IRenderer r)
    {
        var shoulder = new Vector2(132, 111 + Bob);
        var head = shoulder + MathX.FromAngle(Arm, 34);
        r.FillRect(new RectF(156, 116, 30, 10), _anvil);
        r.FillRect(new RectF(164, 126, 14, 14), Rest);
        r.FillEllipse(new Vector2(122, 122 + Bob), 11, 18 - Bob, Color.Hex("41a6f6"));
        r.FillCircle(new Vector2(122, 96 + Bob), 8, Color.Hex("ffcd75"));
        r.DrawLine(shoulder, head, 3, Color.Hex("f4f4f4"));
        r.FillCircle(head, 5, Color.Hex("94b0c2"));
        Font.DrawCentered(r, $"{Pose}  HITS {Hits}", new Vector2(160, 60), Color.White, scale: 2);
        Heading.Draw(r, Font, "TRACKS AND TIMELINES", "CONFIRM: STRIKE");
    }
}
