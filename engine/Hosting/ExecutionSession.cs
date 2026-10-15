namespace MicroEngine.Hosting;

// Origin markers avoid interpreting consumer-created exceptions as engine API misuse.
internal interface IExecutionApiError { }
internal sealed class MissingExecutionArgumentException(string parameter)
    : ArgumentNullException(parameter), IExecutionApiError;
internal sealed class InvalidExecutionArgumentException(string message, string parameter)
    : ArgumentException(message, parameter), IExecutionApiError;

/// <summary>A single-use headless execution bound to its creating thread.</summary>
public sealed class ExecutionSession : IDisposable
{
    private sealed class ApiUsageException(string message)
        : InvalidOperationException(message), IExecutionApiError;
    private readonly Func<IDisposable>[] _acquisitions;
    private readonly int _ownerThread = Environment.CurrentManagedThreadId;
    private bool _started;
    private bool _disposed;
    private bool _stopRequested;
    internal ExecutionSession(Func<IDisposable>[] acquisitions) => _acquisitions = acquisitions;

    /// <summary>Acquires resources, invokes work once, and attempts all cleanup before returning.</summary>
    public ExecutionReport Run(Action<ExecutionSession> work)
    {
        EnsureOwnerThread();
        if (work is null) throw new MissingExecutionArgumentException(nameof(work));
        if (_started || _disposed) throw new ApiUsageException("Execution sessions are single-use.");
        _started = true;
        var owned = new List<IDisposable>();
        Exception? primary = null;
        Exception? misuse = null;
        var secondary = new List<Exception>();
        try
        {
            foreach (var factory in _acquisitions)
            {
                if (_stopRequested) break;
                var resource = factory() ?? throw new InvalidOperationException("A factory returned null.");
                if (owned.Any(item => ReferenceEquals(item, resource)))
                    throw new InvalidOperationException("A factory returned a resource already owned by this session.");
                owned.Add(resource);
            }
            if (!_stopRequested) work(this);
        }
        catch (Exception error) when (error is IExecutionApiError) { misuse = error; }
        catch (Exception error) { primary = error; }
        finally
        {
            for (var i = owned.Count - 1; i >= 0; i--)
            {
                try { owned[i].Dispose(); }
                catch (Exception error) when (error is IExecutionApiError)
                {
                    if (misuse is null) misuse = error;
                    else secondary.Add(error);
                }
                catch (Exception error)
                {
                    if (primary is null) primary = error;
                    else secondary.Add(error);
                }
            }
            owned.Clear();
        }
        if (misuse is not null)
        {
            if (primary is null && secondary.Count == 0)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(misuse).Throw();
            var causes = new List<Exception> { misuse };
            if (primary is not null) causes.Add(primary);
            causes.AddRange(secondary);
            var diagnostics = new AggregateException(causes);
            if (misuse is ArgumentException argument)
                throw new ArgumentException("Invalid execution API argument; additional failure diagnostics are retained.", argument.ParamName, diagnostics);
            throw new InvalidOperationException("Invalid execution API use; additional failure diagnostics are retained.", diagnostics);
        }
        return new ExecutionReport(primary is not null ? ExecutionOutcome.Failed : _stopRequested ? ExecutionOutcome.Stopped : ExecutionOutcome.Succeeded, primary, secondary);
    }

    /// <summary>Requests cooperative termination at the next host callback boundary.</summary>
    public void Stop()
    {
        EnsureOwnerThread();
        _stopRequested = true;
    }

    /// <summary>Prevents another run; active callback cleanup is deferred until it returns.</summary>
    public void Dispose()
    {
        EnsureOwnerThread();
        _stopRequested = true;
        _disposed = true;
    }

    private void EnsureOwnerThread()
    {
        if (Environment.CurrentManagedThreadId != _ownerThread)
            throw new ApiUsageException("Session methods must run on the thread that created the session.");
    }
}
