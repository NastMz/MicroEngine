namespace TrailDuel.Rules;

/// <summary>
/// Chooses enemy moves. Wild creatures pick any usable move at random; trainers score each move
/// by expected damage (power, STAB, type matchup, accuracy) or by the situational value of status
/// moves, with a little jitter so near-equal options vary between battles.
/// </summary>
public static class TrainerAI
{
    public static int ChooseMove(Creature user, int[] userStages, Creature target, int[] targetStages, bool smart, Random rng)
    {
        var usable = Enumerable.Range(0, user.Moves.Count).Where(i => user.Moves[i].Pp > 0).ToList();
        if (usable.Count == 0) return -1;
        if (!smart) return usable[rng.Next(usable.Count)];
        int best = usable[0];
        float bestScore = float.MinValue;
        foreach (int i in usable)
        {
            float score = Score(user, userStages, target, targetStages, user.Moves[i].Data) * (1f + (float)rng.NextDouble() * 0.1f);
            if (score > bestScore) { bestScore = score; best = i; }
        }
        return best;
    }

    public static float Score(Creature user, int[] userStages, Creature target, int[] targetStages, MoveData move)
    {
        if (move.IsDamaging)
        {
            float eff = TypeChart.Effectiveness(move.Type, target.Type);
            float stab = move.Type == user.Type ? 1.5f : 1f;
            float acc = move.NeverMisses ? 1f : move.Accuracy / 100f;
            bool physical = move.Category == MoveCategory.Physical;
            float ratio = physical
                ? user.Attack * Formulas.StageMultiplier(userStages[(int)Stat.Attack]) / MathF.Max(1, target.Defense * Formulas.StageMultiplier(targetStages[(int)Stat.Defense]))
                : user.Special * Formulas.StageMultiplier(userStages[(int)Stat.Special]) / MathF.Max(1, target.Special * Formulas.StageMultiplier(targetStages[(int)Stat.Special]));
            float expected = move.Power * eff * stab * acc * ratio;
            if (move.Effect == MoveEffect.Drain && user.Hp < user.MaxHp / 2) expected *= 1.3f;
            return expected;
        }
        // Status moves are worth roughly one medium attack when they make sense, nothing otherwise.
        return move.Effect switch
        {
            MoveEffect.Sleep or MoveEffect.Burn or MoveEffect.Poison => target.Status == StatusCondition.None ? 45f : 0f,
            MoveEffect.HealSelf => user.HpFraction < 0.4f ? 90f : 0f,
            MoveEffect.RaiseSelf => userStages[(int)move.Stat] < 2 && user.HpFraction > 0.6f ? 30f : 0f,
            MoveEffect.LowerFoe => targetStages[(int)move.Stat] > -2 && user.HpFraction > 0.5f ? 25f : 0f,
            _ => 0f,
        };
    }
}
