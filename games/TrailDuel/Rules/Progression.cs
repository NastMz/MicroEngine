namespace TrailDuel.Rules;

/// <summary>Stats before and after a level-up, for the "level up" panel.</summary>
public sealed record LevelUp(int Level, int[] Before, int[] After, IReadOnlyList<string> NewMoves);

/// <summary>Experience, levelling and move learning.</summary>
public static class Progression
{
    public static int[] StatLine(Creature c) => [c.MaxHp, c.Attack, c.Defense, c.Special, c.Speed];

    /// <summary>
    /// Adds experience and applies every level gained. HP grows by the max-HP increase.
    /// Moves are learned automatically while a slot is free; the rest are returned in
    /// <see cref="LevelUp.NewMoves"/> with no free slot for the caller to offer.
    /// </summary>
    public static List<LevelUp> GainXp(Creature c, int amount, out List<string> pendingMoves)
    {
        pendingMoves = [];
        var ups = new List<LevelUp>();
        if (c.Level >= Creature.MaxLevel) return ups;
        c.Xp += Math.Max(0, amount);
        while (c.Level < Creature.MaxLevel && c.Xp >= Creature.XpForLevel(c.Level + 1))
        {
            var before = StatLine(c);
            c.Level++;
            var after = StatLine(c);
            c.Hp = Math.Min(c.MaxHp, c.Hp + (after[0] - before[0]));
            var learned = new List<string>();
            foreach (var move in c.Species.MovesLearnedAt(c.Level))
            {
                if (c.Knows(move)) continue;
                if (c.Moves.Count < Creature.MaxMoves) { c.Moves.Add(new MoveSlot(move)); learned.Add(move); }
                else pendingMoves.Add(move);
            }
            ups.Add(new LevelUp(c.Level, before, after, learned));
        }
        if (c.Level >= Creature.MaxLevel) c.Xp = Creature.XpForLevel(Creature.MaxLevel);
        return ups;
    }

    /// <summary>Replaces the move in <paramref name="slot"/> with <paramref name="moveId"/>.</summary>
    public static void ReplaceMove(Creature c, int slot, string moveId)
    {
        if (slot < 0 || slot >= c.Moves.Count) throw new ArgumentOutOfRangeException(nameof(slot));
        c.Moves[slot] = new MoveSlot(moveId);
    }
}
