namespace MicroEngine.Hosting;

/// <summary>Reusable acquisition configuration for permanent headless hosting.</summary>
public sealed class EngineConfiguration
{
    private readonly Func<IDisposable>[] _acquisitions;

    public EngineConfiguration(params Func<IDisposable>[] acquisitions)
    {
        if (acquisitions is null) throw new MissingExecutionArgumentException(nameof(acquisitions));
        _acquisitions = acquisitions.ToArray();
        if (_acquisitions.Any(factory => factory is null))
            throw new InvalidExecutionArgumentException("Acquisition factories cannot be null.", nameof(acquisitions));
    }

    public ExecutionSession CreateSession() => new(_acquisitions);
}
