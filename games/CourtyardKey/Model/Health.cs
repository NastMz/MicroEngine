namespace CourtyardKey.Model;

/// <summary>
/// Hit points measured in half hearts, with invulnerability frames after each hit.
/// Shared by the hero, enemies and the boss so damage rules stay identical everywhere.
/// </summary>
public sealed class Health
{
    public int Max { get; private set; }
    public int Current { get; private set; }
    /// <summary>Seconds of remaining invulnerability.</summary>
    public float Invulnerable { get; private set; }
    public bool IsDead => Current <= 0;
    public bool IsInvulnerable => Invulnerable > 0;

    public Health(int max, int? current = null)
    {
        if (max <= 0) throw new ArgumentOutOfRangeException(nameof(max));
        Max = max;
        Current = Math.Clamp(current ?? max, 0, max);
    }

    /// <summary>Applies damage unless invulnerable or already dead. Returns true when the hit landed.</summary>
    public bool TryDamage(int amount, float invulnerability)
    {
        if (amount <= 0 || IsDead || IsInvulnerable) return false;
        Current = Math.Max(0, Current - amount);
        Invulnerable = invulnerability;
        return true;
    }

    public int Heal(int amount)
    {
        if (amount <= 0 || IsDead) return 0;
        int before = Current;
        Current = Math.Min(Max, Current + amount);
        return Current - before;
    }

    public void Restore() => Current = Max;
    public void SetCurrent(int value) => Current = Math.Clamp(value, 0, Max);

    public void RaiseMax(int amount, bool fill)
    {
        Max += amount;
        Current = fill ? Max : Math.Min(Max, Current + amount);
    }

    public void Update(float dt) => Invulnerable = MathF.Max(0, Invulnerable - dt);
    public void ClearInvulnerability() => Invulnerable = 0;
}
