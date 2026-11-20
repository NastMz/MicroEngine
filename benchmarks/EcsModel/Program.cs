namespace EcsModel;

/// <summary>The two versions of the slice at each step of the comparison.</summary>
public static class Slices
{
    public static ISlice Simple(int step) => step switch
    {
        1 => new Simple.Step1.Slice(),
        2 => new Simple.Step2.Slice(),
        3 => new Simple.Step3.Slice(),
        _ => throw new ArgumentOutOfRangeException(nameof(step)),
    };

    public static ISlice Ecs(int step) => step switch
    {
        1 => new Ecs.Step1.Slice(),
        2 => new Ecs.Step2.Slice(),
        3 => new Ecs.Step3.Slice(),
        _ => throw new ArgumentOutOfRangeException(nameof(step)),
    };
}

/// <summary>
/// Plays the script on both models at each step, says whether they agree, and reports how much code each step is and
/// how much of the step before it had to change. <c>dotnet run -c Release --project benchmarks/EcsModel</c>.
/// </summary>
public static class Program
{
    /// <summary>The lines of a source file that are code: not blank, not a comment, not a lone brace.</summary>
    public static List<string> CodeLines(string file) =>
        [.. File.ReadLines(file).Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith("//", StringComparison.Ordinal) && l is not ("{" or "}" or "});" or "};") && !l.StartsWith("namespace ", StringComparison.Ordinal))];

    /// <summary>How many lines of <paramref name="after"/> are new and how many of <paramref name="before"/> are gone (a longest common subsequence).</summary>
    public static (int Added, int Removed) Changed(List<string> before, List<string> after)
    {
        var common = new int[before.Count + 1, after.Count + 1];
        for (int i = before.Count - 1; i >= 0; i--)
            for (int j = after.Count - 1; j >= 0; j--)
                common[i, j] = before[i] == after[j] ? common[i + 1, j + 1] + 1 : Math.Max(common[i + 1, j], common[i, j + 1]);
        return (after.Count - common[0, 0], before.Count - common[0, 0]);
    }

    private static string Sources()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "MicroEngine.slnx"))) return Path.Combine(dir.FullName, "benchmarks", "EcsModel");
        throw new InvalidOperationException("The repository root (MicroEngine.slnx) was not found above the program.");
    }

    public static int Main()
    {
        bool same = true;
        for (int step = 1; step <= 3; step++)
        {
            var simple = Script.Trace(Slices.Simple(step), step);
            var ecs = Script.Trace(Slices.Ecs(step), step);
            int at = Enumerable.Range(0, simple.Count).FirstOrDefault(i => simple[i] != ecs[i], -1);
            Console.WriteLine($"step {step}: {(at < 0 ? $"the same at each of {simple.Count} updates" : $"first difference at update {at}")}");
            if (at < 0) continue;
            same = false;
            Console.WriteLine($"  simple {simple[at]}\n  ecs    {ecs[at]}");
        }

        string sources = Sources();
        Console.WriteLine("\nlines of code (no blanks, comments or lone braces), and what each step changed in the step before it:");
        foreach (string model in (string[])["Simple", "Ecs"])
        {
            List<string>? before = null;
            for (int step = 1; step <= 3; step++)
            {
                var lines = CodeLines(Path.Combine(sources, model, $"Step{step}.cs"));
                string change = "";
                if (before is not null)
                {
                    var (added, removed) = Changed(before, lines);
                    change = $", +{added} -{removed}";
                }
                Console.WriteLine($"  {model,-6} step {step}: {lines.Count} lines{change}");
                before = lines;
            }
        }
        return same ? 0 : 1;
    }
}
