namespace TurnExplorer;

internal sealed record LaunchOptions(int Limit, bool FailUpdate, string? LoadPath, string? SaveNewPath)
{
    public static bool TryParse(string[] args, out LaunchOptions? options)
    {
        ArgumentNullException.ThrowIfNull(args);
        options = null;
        var limit = 0;
        var failUpdate = false;
        string? load = null, save = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--smoke")
            {
                if (i + 1 >= args.Length || limit != 0 || !int.TryParse(args[i + 1], out var parsed)
                    || parsed is < 1 or > 600) return false;
                limit = parsed;
                i++;
            }
            else if (args[i] == "--fail-update" && !failUpdate) failUpdate = true;
            else if (args[i] is "--load" or "--save-new")
            {
                var isLoad = args[i] == "--load";
                if ((isLoad ? load : save) is not null || i + 1 >= args.Length) return false;
                var path = args[++i];
                if (string.IsNullOrWhiteSpace(path) || path.StartsWith("--", StringComparison.Ordinal)) return false;
                if (isLoad) load = path; else save = path;
            }
            else return false;
        }
        options = new LaunchOptions(limit, failUpdate, load, save);
        return true;
    }
}
