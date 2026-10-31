using System.Numerics;

namespace MicroEngine.Input;

/// <summary>
/// On-screen gamepad for touch screens: a d-pad on the left, A/B/X and a shoulder button on the right, Start at the top.
/// Fingers press the gamepad buttons the games already bind (<see cref="InputMap.CreateDefault"/>), so a game needs no
/// touch code. Positions are normalised to the window (0..1 on both axes); radii are fractions of its height, so the
/// controls keep their size and stay round at any aspect ratio. Backends feed fingers and draw <see cref="Controls"/>.
/// </summary>
public sealed class TouchPad
{
    public readonly record struct Control(PadButton Button, Vector2 Center, float Radius);

    private readonly Dictionary<ulong, PadButton> _fingers = [];

    /// <summary>Whether the backend draws the controls: true on touch-first systems, and after the first touch elsewhere.</summary>
    public bool Visible { get; set; }

    /// <summary>The controls laid out for a window of the given width / height.</summary>
    public IEnumerable<Control> Controls(float aspect)
    {
        aspect = Math.Max(aspect, 0.1f);
        Vector2 At(float xInHeights, float y) => new(xInHeights / aspect, y);
        Vector2 FromRight(float xInHeights, float y) => new(1 - xInHeights / aspect, y);
        const float pad = 0.085f, step = 0.12f, y = 0.74f;
        yield return new Control(PadButton.DPadLeft, At(0.22f - step, y), pad);
        yield return new Control(PadButton.DPadRight, At(0.22f + step, y), pad);
        yield return new Control(PadButton.DPadUp, At(0.22f, y - step), pad);
        yield return new Control(PadButton.DPadDown, At(0.22f, y + step), pad);
        yield return new Control(PadButton.A, FromRight(0.22f, y + step), pad);
        yield return new Control(PadButton.B, FromRight(0.22f - step, y), pad);
        yield return new Control(PadButton.X, FromRight(0.22f + step, y), pad);
        yield return new Control(PadButton.RightShoulder, FromRight(0.22f, y - step), pad * 0.8f);
        yield return new Control(PadButton.Start, new Vector2(0.5f, 0.08f), pad * 0.6f);
    }

    /// <summary>A finger went down or moved: it presses the control under it (or nothing). Fingers can slide between controls.</summary>
    public void Set(ulong finger, Vector2 position, float aspect)
    {
        Visible = true;
        var hit = PadButton.None;
        float best = float.MaxValue;
        foreach (var c in Controls(aspect))
        {
            // Distances in heights, so the hit area is round like the drawn control; a little larger than it.
            var d = new Vector2((position.X - c.Center.X) * aspect, position.Y - c.Center.Y);
            float distance = d.Length();
            if (distance <= c.Radius * 1.35f && distance < best) (hit, best) = (c.Button, distance);
        }
        if (hit == PadButton.None) _fingers.Remove(finger);
        else _fingers[finger] = hit;
    }

    public void Release(ulong finger) => _fingers.Remove(finger);
    public void ReleaseAll() => _fingers.Clear();

    public bool IsDown(PadButton button)
    {
        foreach (var b in _fingers.Values) if (b == button) return true;
        return false;
    }
}
