namespace MicroEngine.Hosting;

public enum ExecutionOutcome
{
    Succeeded,
    Stopped,
    Failed
}

/// <summary>Execution outcome with the primary cause and ordered secondary cleanup causes.</summary>
public sealed class ExecutionReport
{
    public ExecutionOutcome Outcome { get; }
    public Exception? PrimaryError { get; }
    public IReadOnlyList<Exception> CleanupErrors { get; }

    internal ExecutionReport(ExecutionOutcome outcome, Exception? primaryError, IEnumerable<Exception> cleanupErrors)
    {
        Outcome = outcome;
        PrimaryError = primaryError;
        CleanupErrors = Array.AsReadOnly(cleanupErrors.ToArray());
    }
}
