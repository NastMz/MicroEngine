namespace MicroEngine.Examples;

// Example-only policy; window creation, usage text and exit handling stay with each consumer.
internal readonly record struct LaunchOptions(int FrameLimit, bool FailUpdate)
{
    internal static bool TryParse(string[] args, out LaunchOptions options)
    {
        ArgumentNullException.ThrowIfNull(args);
        options = default;
        int limit = 0;
        bool fail = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--smoke" && limit == 0 && i + 1 < args.Length &&
                int.TryParse(args[++i], out int n) && n is >= 1 and <= 600)
                limit = n;
            else if (args[i] == "--fail-update" && !fail)
                fail = true;
            else
                return false;
        }
        options = new LaunchOptions(limit, fail);
        return true;
    }
}
