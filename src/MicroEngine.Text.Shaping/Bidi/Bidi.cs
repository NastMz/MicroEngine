namespace MicroEngine.Graphics;

/// <summary>The Unicode bidirectional character types (UAX #9, table 4), in the order of the generated class table.</summary>
internal enum BidiClass : byte
{
    L, R, AL, EN, ES, ET, AN, CS, NSM, BN, B, S, WS, ON, LRE, LRO, RLE, RLO, PDF, LRI, RLI, FSI, PDI,
}

/// <summary>
/// The Unicode bidirectional algorithm (UAX #9) for one line of one paragraph, without explicit embeddings: the
/// paragraph's direction from its first strong letter (P2, P3), the weak types (W1–W7), the neutrals (N1, N2), the
/// implicit levels (I1, I2) and the line's reordering (L1, L2). Explicit formatting characters (LRE, RLO, LRI, PDI...)
/// are ignored, like the other boundary neutrals (X9), and bracket pairs take no part (N0).
/// </summary>
internal static partial class Bidi
{
    /// <summary>A stretch of a line at one embedding level: right to left when the level is odd.</summary>
    public readonly record struct Run(int Start, int Length, int Level)
    {
        public bool RightToLeft => (Level & 1) == 1;
    }

    /// <summary>The bidirectional class of <paramref name="codePoint"/>.</summary>
    public static BidiClass ClassOf(int codePoint)
    {
        var starts = RangeStarts;
        int lo = 0, hi = starts.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) >> 1;
            if (starts[mid] <= codePoint) lo = mid;
            else hi = mid - 1;
        }
        return (BidiClass)RangeClasses[lo];
    }

    /// <summary>The classes of the line's UTF-16 units: both units of a surrogate pair take the pair's class.</summary>
    private static BidiClass[] Classes(string line)
    {
        var classes = new BidiClass[line.Length];
        for (int i = 0; i < line.Length; i++)
        {
            if (char.IsHighSurrogate(line[i]) && i + 1 < line.Length && char.IsLowSurrogate(line[i + 1]))
            {
                classes[i] = classes[i + 1] = ClassOf(char.ConvertToUtf32(line[i], line[i + 1]));
                i++;
            }
            else classes[i] = ClassOf(line[i]);
        }
        return classes;
    }

    /// <summary>Whether the line reads right to left: its first strong letter is Hebrew, Arabic or another right-to-left script (P2, P3).</summary>
    public static bool IsRightToLeft(string line) => ParagraphLevel(Classes(line)) == 1;

    private static int ParagraphLevel(BidiClass[] classes)
    {
        foreach (var c in classes)
        {
            if (c == BidiClass.L) return 0;
            if (c is BidiClass.R or BidiClass.AL) return 1;
        }
        return 0;
    }

    /// <summary>Whether the line needs the algorithm at all: it has a right-to-left letter or an Arabic number.</summary>
    private static bool HasRightToLeft(BidiClass[] classes) => Array.Exists(classes, c => c is BidiClass.R or BidiClass.AL or BidiClass.AN);

    /// <summary>The line's runs of one level, in the order they are drawn, left to right.</summary>
    public static List<Run> VisualRuns(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        var runs = new List<Run>();
        if (line.Length == 0) return runs;
        var classes = Classes(line);
        if (!HasRightToLeft(classes))
        {
            runs.Add(new Run(0, line.Length, 0));
            return runs;
        }
        var levels = Levels(classes);
        for (int i = 0; i < levels.Length;)
        {
            int start = i;
            while (i < levels.Length && levels[i] == levels[start]) i++;
            runs.Add(new Run(start, i - start, levels[start]));
        }
        // L2: from the highest level down to the lowest odd one, reverse every sequence of runs at that level or higher.
        int highest = runs.Max(r => r.Level), lowestOdd = runs.Where(r => r.RightToLeft).Select(r => r.Level).DefaultIfEmpty(highest + 1).Min();
        for (int level = highest; level >= lowestOdd; level--)
            for (int i = 0; i < runs.Count;)
            {
                if (runs[i].Level < level) { i++; continue; }
                int from = i;
                while (i < runs.Count && runs[i].Level >= level) i++;
                runs.Reverse(from, i - from);
            }
        return runs;
    }

    /// <summary>The resolved embedding level of each UTF-16 unit of the line.</summary>
    internal static int[] Levels(BidiClass[] original)
    {
        int paragraph = ParagraphLevel(original);
        var sos = paragraph == 0 ? BidiClass.L : BidiClass.R;
        // X9: explicit formatting characters and boundary neutrals take no part; they are given a level at the end.
        var kept = new List<int>(original.Length);
        for (int i = 0; i < original.Length; i++)
            if (original[i] is not (BidiClass.BN or >= BidiClass.LRE)) kept.Add(i);
        var t = kept.Select(i => original[i]).ToArray();
        int n = t.Length;

        // W1: a mark takes the type of what it marks.
        for (int i = 0; i < n; i++)
            if (t[i] == BidiClass.NSM) t[i] = i == 0 ? sos : t[i - 1];
        // W2: European digits after Arabic letters are Arabic numbers. W3: Arabic letters are right to left.
        var strong = sos;
        for (int i = 0; i < n; i++)
        {
            if (t[i] is BidiClass.L or BidiClass.R or BidiClass.AL) strong = t[i];
            else if (t[i] == BidiClass.EN && strong == BidiClass.AL) t[i] = BidiClass.AN;
        }
        for (int i = 0; i < n; i++)
            if (t[i] == BidiClass.AL) t[i] = BidiClass.R;
        // W4: one separator between two numbers of a kind joins them.
        for (int i = 1; i + 1 < n; i++)
        {
            if (t[i] == BidiClass.ES && t[i - 1] == BidiClass.EN && t[i + 1] == BidiClass.EN) t[i] = BidiClass.EN;
            else if (t[i] == BidiClass.CS && t[i - 1] == t[i + 1] && t[i - 1] is BidiClass.EN or BidiClass.AN) t[i] = t[i - 1];
        }
        // W5: terminators next to European digits (a currency sign, a percent) belong to the number.
        for (int i = 0; i < n;)
        {
            if (t[i] != BidiClass.ET) { i++; continue; }
            int from = i;
            while (i < n && t[i] == BidiClass.ET) i++;
            if ((from > 0 && t[from - 1] == BidiClass.EN) || (i < n && t[i] == BidiClass.EN))
                for (int j = from; j < i; j++) t[j] = BidiClass.EN;
        }
        // W6: the other separators and terminators are neutral.
        for (int i = 0; i < n; i++)
            if (t[i] is BidiClass.ES or BidiClass.ET or BidiClass.CS) t[i] = BidiClass.ON;
        // W7: European digits in left-to-right text are left to right.
        strong = sos;
        for (int i = 0; i < n; i++)
        {
            if (t[i] is BidiClass.L or BidiClass.R) strong = t[i];
            else if (t[i] == BidiClass.EN && strong == BidiClass.L) t[i] = BidiClass.L;
        }
        // N1, N2: neutrals between two letters of one direction take it (numbers count as right to left); others take the paragraph's.
        for (int i = 0; i < n;)
        {
            if (!IsNeutral(t[i])) { i++; continue; }
            int from = i;
            while (i < n && IsNeutral(t[i])) i++;
            var before = from == 0 ? sos : Direction(t[from - 1]);
            var after = i == n ? sos : Direction(t[i]);
            var resolved = before == after ? before : sos;
            for (int j = from; j < i; j++) t[j] = resolved;
        }

        // I1, I2: the implicit levels.
        var levels = new int[original.Length];
        Array.Fill(levels, -1);
        for (int k = 0; k < n; k++)
        {
            int level = paragraph;
            if (paragraph == 0) level += t[k] == BidiClass.R ? 1 : t[k] is BidiClass.AN or BidiClass.EN ? 2 : 0;
            else level += t[k] is BidiClass.L or BidiClass.EN or BidiClass.AN ? 1 : 0;
            levels[kept[k]] = level;
        }
        // L1: separators, and the white space before them and at the end of the line, return to the paragraph's level.
        bool trailing = true;
        for (int i = original.Length - 1; i >= 0; i--)
        {
            var c = original[i];
            if (c is BidiClass.S or BidiClass.B) { levels[i] = paragraph; trailing = true; }
            else if (trailing && c is BidiClass.WS or BidiClass.BN or >= BidiClass.LRE) levels[i] = paragraph;
            else trailing = false;
        }
        // The characters X9 set aside take the level of the character before them.
        for (int i = 0; i < levels.Length; i++)
            if (levels[i] < 0) levels[i] = i == 0 ? paragraph : levels[i - 1];
        return levels;
    }

    private static bool IsNeutral(BidiClass c) => c is BidiClass.B or BidiClass.S or BidiClass.WS or BidiClass.ON;

    private static BidiClass Direction(BidiClass c) => c == BidiClass.L ? BidiClass.L : BidiClass.R;
}
