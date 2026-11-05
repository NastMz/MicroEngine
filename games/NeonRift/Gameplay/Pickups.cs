using System.Numerics;
using NeonRift.Art;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace NeonRift.Gameplay;

/// <summary>
/// A weapon capsule dropped by a carrier. Letters: S scatter, L lance, R rapid, B barrier, 1 extra life.
/// It pops up, falls onto the ground and blinks out after a while if ignored.
/// </summary>
public sealed class Capsule : Actor
{
    public const float Lifetime = 12f;
    private Body _body = new() { Box = new RectF(-6, -6, 12, 12) };
    public char Letter { get; }
    private protected override void AttachTo(IWorld world, EntityId id) => world.Attach<Capsule>(id, this);

    public Capsule(Vector2 at, char letter)
    {
        Position = at;
        Letter = letter;
        Velocity = new Vector2(30, -170);
        Hitbox = new RectF(-9, -9, 18, 18);
    }

    /// <summary>Adds the actor's state, then the letter and whether it rests on the ground.</summary>
    public override void HashState(StateHasher hash)
    {
        base.HashState(hash);
        hash.Add((int)Letter);
        hash.Add(_body.Grounded);
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        var v = Velocity;
        var pos = Position;
        _body.Move(ref pos, ref v, dt, Stage.Grid, 600, 200);
        if (_body.Grounded) v.X = 0;
        Position = pos;
        Velocity = v;
        if (Age > Lifetime || Position.Y > Stage.Def.PixelHeight + 30 || Bounds.Right < Stage.Camera.View.Left) Destroy();
    }

    /// <summary>Applies the capsule to the player and the session.</summary>
    public void Collect(Player player, Session session)
    {
        switch (Letter)
        {
            case 'B': player.Barrier = Player.BarrierTime; break;
            case '1': session.Lives++; break;
            default:
                if (WeaponSpec.FromLetter(Letter) is { } kind)
                {
                    player.Equip(kind);
                    session.Weapon = kind;
                }
                break;
        }
        Destroy();
    }

    internal override void Render(IRenderer r, Assets art)
    {
        if (Age > Lifetime - 3 && (int)(Age * 8) % 2 == 0) return;
        DrawPod(r, art, Vector2.Round(DrawPosition), Letter, Age);
    }

    /// <summary>A glowing energy cell with the weapon letter, shared with the carrier that holds it.</summary>
    internal static void DrawPod(IRenderer r, Assets art, Vector2 at, char letter, float time)
    {
        var glow = letter switch { 'S' => Color.Hex("ff5a7a"), 'L' => Color.Hex("5af0ff"), 'R' => Color.Hex("ffd84a"), 'B' => Color.Hex("9a7aff"), _ => Color.Hex("7aff9a") };
        float pulse = 0.5f + 0.5f * MathF.Sin(time * 6);
        r.SetBlendMode(BlendMode.Additive);
        r.FillCircle(at, 10 + pulse * 2, glow.WithAlpha(0.18f));
        r.SetBlendMode(BlendMode.Alpha);
        r.FillCircle(at, 8.5f, Color.Hex("10101c"));
        r.FillCircle(at, 7.5f, glow);
        r.FillCircle(at, 6f, Color.Hex("1a1830"));
        if (art.Font is { } font)
        {
            var text = letter.ToString();
            var size = font.Measure(text);
            font.Draw(r, text, Vector2.Round(at - size / 2) + new Vector2(0, 1), Color.White);
        }
    }
}
