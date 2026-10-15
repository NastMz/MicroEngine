using MicroEngine.Hosting;

namespace MicroEngine.Loop;

public static class FrameLoop
{
    public static ExecutionReport Run(Func<IFramePlatform> createPlatform, LoopOptions options,
        Action<UpdateFrame> update, Action<ICanvas, PresentationFrame> draw)
    {
        ArgumentNullException.ThrowIfNull(createPlatform);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(draw);
        IFramePlatform? platform = null;
        var configuration = new EngineConfiguration(() => platform = createPlatform());
        using var session = configuration.CreateSession();
        var control = new FrameControl();
        var report = session.Run(_ =>
        {
            long accumulator = 0;
            var pending = new List<string>();
            while (!control.Stopped)
            {
                var frame = platform!.Poll();
                if (frame.CloseRequested) { control.RequestStop(); break; }
                if (frame.Input is null || frame.Elapsed < TimeSpan.Zero)
                    throw new InvalidOperationException("Invalid platform frame input or elapsed time.");
                long discarded = 0;
                if (!frame.Active)
                {
                    accumulator = 0;
                    pending.Clear();
                }
                else
                {
                    if (frame.Input.Pressed.Count > options.MaxPendingInputs - pending.Count)
                        throw new InvalidOperationException("Pending input capacity exceeded.");
                    pending.AddRange(frame.Input.Pressed);
                    var elapsed = Math.Min(frame.Elapsed.Ticks, options.MaxElapsed.Ticks);
                    discarded = frame.Elapsed.Ticks - elapsed;
                    accumulator = checked(accumulator + elapsed);
                    var ticks = 0;
                    while (accumulator >= options.Step.Ticks && ticks < options.MaxUpdates && !control.Stopped)
                    {
                        var input = new InputSnapshot(frame.Input.Held, pending);
                        update(new UpdateFrame(options.Step, input, control));
                        pending.Clear();
                        accumulator -= options.Step.Ticks;
                        ticks++;
                    }
                    var wholeBacklog = accumulator - accumulator % options.Step.Ticks;
                    discarded = checked(discarded + wholeBacklog);
                    accumulator -= wholeBacklog;
                }
                if (control.Stopped) break;
                platform.Begin();
                Exception? drawFailure = null;
                try
                {
                    var canvas = platform.Canvas ?? throw new InvalidOperationException("Platform returned a null canvas.");
                    draw(canvas, new PresentationFrame((double)accumulator / options.Step.Ticks, TimeSpan.FromTicks(discarded), control));
                }
                catch (Exception error) { drawFailure = error; }
                try { platform.End(); }
                catch (Exception endFailure)
                {
                    if (drawFailure is not null) throw new AggregateException(drawFailure, endFailure);
                    throw;
                }
                if (drawFailure is not null)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(drawFailure).Throw();
            }
            if (control.Stopped) session.Stop();
        });
        if (ContainsApiError(report.PrimaryError) || report.CleanupErrors.Any(ContainsApiError))
        {
            if (report.PrimaryError is FrameApiException apiError && report.CleanupErrors.Count == 0)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(apiError).Throw();
            var causes = new List<Exception>();
            if (report.PrimaryError is not null) causes.Add(report.PrimaryError);
            causes.AddRange(report.CleanupErrors);
            throw new AggregateException("Invalid frame API use; all failure causes retained.", causes);
        }
        return report;
    }

    private static bool ContainsApiError(Exception? error) => error is FrameApiException
        || error is AggregateException aggregate && aggregate.InnerExceptions.Any(ContainsApiError);
}
