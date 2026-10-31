namespace TrailDuel.Rules;

public readonly record struct DamageResult(int Damage, float Effectiveness, bool Critical, bool Stab);

/// <summary>
/// Pure battle math. Every random roll comes from the supplied <see cref="Random"/> so a seeded
/// battle is fully reproducible (tests rely on this).
/// </summary>
public static class Formulas
{
    public const int MaxStage = 6;

    /// <summary>Classic stage multiplier: +1 = x1.5, +2 = x2, -1 = x0.67, -2 = x0.5 ...</summary>
    public static float StageMultiplier(int stage)
    {
        stage = Math.Clamp(stage, -MaxStage, MaxStage);
        return stage >= 0 ? (2f + stage) / 2f : 2f / (2f - stage);
    }

    public static int CritChanceDenominator(MoveData move) => move.HighCrit ? 8 : 16;

    public static bool RollCritical(MoveData move, Random rng) => rng.Next(CritChanceDenominator(move)) == 0;

    public static bool RollHit(MoveData move, Random rng) => move.NeverMisses || rng.Next(100) < move.Accuracy;

    /// <summary>
    /// Damage = ((2L/5 + 2) * Power * A / D / 50 + 2) * STAB * type * crit * random(0.85..1).
    /// Burn halves physical attack. Critical hits ignore the attacker's negative and the defender's positive stages.
    /// </summary>
    public static DamageResult Damage(Creature attacker, int[] attackerStages, Creature defender, int[] defenderStages,
        MoveData move, Random rng, bool? forceCritical = null)
    {
        float effectiveness = TypeChart.Effectiveness(move.Type, defender.Type);
        bool crit = forceCritical ?? RollCritical(move, rng);
        float randomFactor = (85 + rng.Next(16)) / 100f;
        if (effectiveness == 0) return new DamageResult(0, 0, false, false);

        bool physical = move.Category == MoveCategory.Physical;
        var atkStat = physical ? Stat.Attack : Stat.Special;
        var defStat = physical ? Stat.Defense : Stat.Special;
        int atkStage = attackerStages[(int)atkStat], defStage = defenderStages[(int)defStat];
        if (crit) { atkStage = Math.Max(0, atkStage); defStage = Math.Min(0, defStage); }

        float a = attacker.Stat(atkStat) * StageMultiplier(atkStage);
        float d = defender.Stat(defStat) * StageMultiplier(defStage);
        if (physical && attacker.Status == StatusCondition.Burn) a *= 0.5f;

        float baseDamage = (2f * attacker.Level / 5f + 2f) * move.Power * a / MathF.Max(1, d) / 50f + 2f;
        bool stab = move.Type == attacker.Type;
        float total = baseDamage * (stab ? 1.5f : 1f) * effectiveness * (crit ? 1.5f : 1f) * randomFactor;
        return new DamageResult(Math.Max(1, (int)total), effectiveness, crit, stab);
    }

    public static float EffectiveSpeed(Creature c, int[] stages) => c.Speed * StageMultiplier(stages[(int)Stat.Speed]);

    /// <summary>
    /// Capture value "a" (0..255): lower HP, a better orb, a status and a high species rate all help.
    /// a = (3*max - 2*hp) * rate * orb / (3*max) * statusBonus.
    /// </summary>
    public static float CaptureValue(Creature target, float orbMultiplier)
    {
        float status = target.Status switch
        {
            StatusCondition.Sleep => 2f,
            StatusCondition.Burn or StatusCondition.Poison => 1.5f,
            _ => 1f,
        };
        float max = target.MaxHp;
        float a = (3 * max - 2 * target.Hp) * target.Species.CatchRate * orbMultiplier / (3 * max) * status;
        return Math.Clamp(a, 1, 255);
    }

    /// <summary>Probability of one shake check passing, from the capture value.</summary>
    public static float ShakeProbability(float captureValue)
    {
        if (captureValue >= 255) return 1f;
        float b = 1048560f / MathF.Sqrt(MathF.Sqrt(16711680f / captureValue));
        return Math.Clamp(b / 65536f, 0, 1);
    }

    /// <summary>Overall chance of a capture (all four checks pass).</summary>
    public static float CaptureChance(Creature target, float orbMultiplier) => MathF.Pow(ShakeProbability(CaptureValue(target, orbMultiplier)), 4);

    /// <summary>Rolls a capture. Returns the shakes shown (0..3) and whether the creature was caught.</summary>
    public static (int Shakes, bool Caught) RollCapture(Creature target, float orbMultiplier, Random rng)
    {
        float p = ShakeProbability(CaptureValue(target, orbMultiplier));
        int passed = 0;
        while (passed < 4 && rng.NextDouble() < p) passed++;
        return (Math.Min(passed, 3), passed == 4);
    }

    /// <summary>Escape chance: always when faster; otherwise improves with each attempt.</summary>
    public static bool RollEscape(float playerSpeed, float enemySpeed, int attempt, Random rng)
    {
        if (playerSpeed >= enemySpeed) return true;
        float odds = (playerSpeed * 128f / MathF.Max(1, enemySpeed) + 30f * attempt) % 256f;
        return rng.Next(256) < odds;
    }

    /// <summary>Experience earned for defeating <paramref name="defeated"/>; trainer battles pay 1.5x.</summary>
    public static int ExperienceFor(Creature defeated, bool trainer)
        => Math.Max(1, (int)(defeated.Species.BaseXp * defeated.Level / 7f * (trainer ? 1.5f : 1f)));
}
