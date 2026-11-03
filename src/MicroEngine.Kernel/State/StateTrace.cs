using System.Globalization;
using System.Text;

namespace MicroEngine.Kernel;

/// <summary>The state hash after one fixed step.</summary>
/// <param name="Update">The update the step ended (1 for the first).</param>
/// <param name="Hash">The hash of the whole state.</param>
/// <param name="Sections">The hash of each section, in order; empty unless the trace asked for them (<see cref="StateTrace.Sections"/>).</param>
public sealed record StepHash(long Update, ulong Hash, IReadOnlyList<SectionHash> Sections);

/// <summary>Where two traces first differ (<see cref="StateTrace.FirstDivergence(StateTrace, StateTrace)"/>).</summary>
/// <param name="Update">The update of the first step that differs.</param>
/// <param name="Section">The first section whose hash differs, or null when the steps carry no section hashes or one trace has no such step.</param>
/// <param name="First">The step of the first trace, or null when it ended earlier.</param>
/// <param name="Second">The step of the second trace, or null when it ended earlier.</param>
public sealed record StateDivergence(long Update, string? Section, StepHash? First, StepHash? Second)
{
    /// <summary>A one-line description: the update, the section and both hashes.</summary>
    public override string ToString()
    {
        string where = Section is null ? "" : $", section '{Section}'";
        return $"state diverges at update {Update}{where}: {Describe(First)} vs {Describe(Second)}";
    }

    private static string Describe(StepHash? step) => step is null ? "(no step)" : step.Hash.ToString("x16", CultureInfo.InvariantCulture);
}

/// <summary>
/// A run's per-step state hashes: the loop records one <see cref="StepHash"/> after every fixed step when a run is given
/// one (<c>RunOptions.StateTrace</c>). Two runs of the same build, script and seed must record the same trace;
/// <see cref="FirstDivergence(StateTrace, StateTrace)"/> names the first update and section where two differ.
/// </summary>
/// <remarks>
/// The trace keeps its steps in memory; override <see cref="Record"/> to send them elsewhere (a file, as they come).
/// <see cref="ToText"/> and <see cref="Parse"/> give one line per step:
/// <c>update TAB hash [TAB section=hash]...</c>, hashes as 16 hexadecimal digits.
/// </remarks>
public class StateTrace
{
    private readonly List<StepHash> _steps = [];

    /// <summary>Whether each step also records the hash of every section (false by default: the whole hash only).</summary>
    public bool Sections { get; init; }

    /// <summary>The steps recorded so far, in order.</summary>
    public IReadOnlyList<StepHash> Steps => _steps;

    /// <summary>Receives the hash of a step; the default keeps it in <see cref="Steps"/>.</summary>
    public virtual void Record(StepHash step)
    {
        ArgumentNullException.ThrowIfNull(step);
        _steps.Add(step);
    }

    /// <summary>The first step and section where <paramref name="first"/> and <paramref name="second"/> differ, or null when they are identical.</summary>
    public static StateDivergence? FirstDivergence(StateTrace first, StateTrace second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        return FirstDivergence(first.Steps, second.Steps);
    }

    /// <summary>
    /// The first step and section where two lists of steps differ, or null when they are identical. Steps are compared by
    /// position: their update numbers and whole hashes; then, when both carry section hashes, the first section whose name
    /// or hash differs is named.
    /// </summary>
    public static StateDivergence? FirstDivergence(IReadOnlyList<StepHash> first, IReadOnlyList<StepHash> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        int count = Math.Max(first.Count, second.Count);
        for (int i = 0; i < count; i++)
        {
            var a = i < first.Count ? first[i] : null;
            var b = i < second.Count ? second[i] : null;
            if (a is null || b is null) return new StateDivergence((a ?? b)!.Update, null, a, b);
            if (a.Update == b.Update && a.Hash == b.Hash) continue;
            return new StateDivergence(a.Update, FirstSection(a.Sections, b.Sections), a, b);
        }
        return null;
    }

    private static string? FirstSection(IReadOnlyList<SectionHash> a, IReadOnlyList<SectionHash> b)
    {
        int count = Math.Max(a.Count, b.Count);
        for (int i = 0; i < count; i++)
        {
            if (i >= a.Count) return b[i].Name;
            if (i >= b.Count) return a[i].Name;
            if (a[i] != b[i]) return a[i].Name;
        }
        return null;
    }

    /// <summary>Writes the steps as text, one line per step (see the remarks of <see cref="StateTrace"/>).</summary>
    public void Write(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        foreach (var step in _steps) writer.Write(Format(step) + "\n");
    }

    /// <summary>The steps as text, one line per step.</summary>
    public string ToText()
    {
        var text = new StringWriter(CultureInfo.InvariantCulture);
        Write(text);
        return text.ToString();
    }

    /// <summary>One step as a line of text, without the line break: <c>update TAB hash [TAB section=hash]...</c>.</summary>
    public static string Format(StepHash step)
    {
        ArgumentNullException.ThrowIfNull(step);
        var line = new StringBuilder();
        line.Append(step.Update.ToString(CultureInfo.InvariantCulture)).Append('\t').Append(step.Hash.ToString("x16", CultureInfo.InvariantCulture));
        foreach (var section in step.Sections)
            line.Append('\t').Append(section.Name).Append('=').Append(section.Hash.ToString("x16", CultureInfo.InvariantCulture));
        return line.ToString();
    }

    /// <summary>Reads a trace written by <see cref="Write"/>; it records sections when any step has them.</summary>
    /// <exception cref="FormatException">A line is not a step.</exception>
    public static StateTrace Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var steps = new List<StepHash>();
        foreach (var raw in text.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            if (line.Length == 0) continue;
            var parts = line.Split('\t');
            if (parts.Length < 2 || !long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long update)
                || !ulong.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong hash))
                throw new FormatException($"Not a state trace step: '{line}'.");
            var sections = new List<SectionHash>(parts.Length - 2);
            for (int i = 2; i < parts.Length; i++)
            {
                int eq = parts[i].LastIndexOf('=');
                if (eq < 0 || !ulong.TryParse(parts[i].AsSpan(eq + 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong section))
                    throw new FormatException($"Not a section hash: '{parts[i]}'.");
                sections.Add(new SectionHash(parts[i][..eq], section));
            }
            steps.Add(new StepHash(update, hash, sections));
        }
        var trace = new StateTrace { Sections = steps.Exists(s => s.Sections.Count > 0) };
        foreach (var step in steps) trace._steps.Add(step);
        return trace;
    }
}
