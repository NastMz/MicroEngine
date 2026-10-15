using MicroEngine.Hosting;

if (args.Length != 2 || args[0] != "--scenario" ||
    args[1] is not ("success" or "callback-failure" or "stopped"))
{
    Console.WriteLine("usage: LifecycleProbe --scenario <success|callback-failure|stopped>");
    return 2;
}

var scenario = args[1];
var configuration = new EngineConfiguration(
    () => Acquire("A"),
    () => Acquire("B"));
using var session = configuration.CreateSession();
if (scenario == "stopped") session.Stop();

var report = session.Run(_ =>
{
    Console.WriteLine("work");
    if (scenario == "callback-failure")
        throw new ApplicationException("Injected consumer failure.");
});
Console.WriteLine($"outcome:{report.Outcome}");
if (report.PrimaryError is not null)
    Console.WriteLine($"primary:{report.PrimaryError.GetType().Name}");
return report.Outcome == ExecutionOutcome.Failed ? 1 : 0;

static IDisposable Acquire(string name)
{
    Console.WriteLine($"acquire:{name}");
    return new ProbeResource(name);
}

sealed class ProbeResource(string name) : IDisposable
{
    public void Dispose() => Console.WriteLine($"release:{name}");
}
