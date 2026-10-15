# CAP-001 — H0 headless lifecycle contract

Revision: H0-1.1, 2026-10-13; H0-1 initial finalization 2026-10-12. Status: approved delegated technical specification; implemented and automatically verified. Manual milestone acceptance pending.
Authority: [ADR-0002](../decisions/0002-initial-execution-scope.md), owner-approved scope and delegated technical choices. FR-001, NFR-001/NFR-002/NFR-004.

## Scope and evolution

Headless is a valid permanent hosting mode. H0 performs synchronous initialization, one consumer work callback, and teardown; it is not a graphics substitute or frame scheduler. The consumer may own its own loop. No native backend, ECS, scenes, audio, physics, clocks, input or async execution. Windows x64 validation only, not a cross-platform support promise. Configuration describes reusable acquisition behavior; a session contains one execution's state/ownership. Configuration is not a runtime. Future graphics must not require headless consumers to use graphics-specific contracts; no backend architecture is selected.

## Minimal public C# surface — namespace MicroEngine.Hosting

~~~csharp
public sealed class EngineConfiguration
{
    public EngineConfiguration(params Func<IDisposable>[] acquisitions);
    public ExecutionSession CreateSession();
}
public sealed class ExecutionSession : IDisposable
{
    public ExecutionReport Run(Action<ExecutionSession> work);
    public void Stop();
    public void Dispose();
}
public enum ExecutionOutcome { Succeeded, Stopped, Failed }
public sealed class ExecutionReport
{
    public ExecutionOutcome Outcome { get; }
    public Exception? PrimaryError { get; }
    public IReadOnlyList<Exception> CleanupErrors { get; }
}
~~~

Session/report construction is internal except CreateSession; configuration copies and validates the factory array. No public ledger, acquisition-step interface, resource counts or state machine enum. Exception objects supply needed diagnostic causes; the engine never logs their messages/content automatically. Reports expose an immutable snapshot of ordered errors, not mutable internal lists. Factory delegates may capture mutable application data; snapshotting the array does not make that data immutable or thread-safe.

## Behavior and ownership

L1: Internal created -> initializing -> running -> stopping -> stopped; stopped terminal. Successful normal callback return ends the session and releases resources. No hidden repeated frames.
L2: One valid Run attempt per session. Null callback throws ArgumentNullException before consuming the session. Run reentrancy, repeated Run, Run after Dispose and wrong-thread session calls throw InvalidOperationException before their own mutations. A fresh session is required for another run; configuration may create several sessions.
L3: Each factory is invoked once in configuration order. Returned nonnull IDisposable transfers ownership to host before the next stop check. A throwing factory must clean its own partial/unreturned state; host releases only successful transfers. Null or a duplicate resource reference returned by a factory fails initialization; host never disposes the same transferred reference twice. No later factory/work on initialization failure. Factories must not return shared ownership to concurrent sessions.
L4: Release attempts occur once per committed resource in reverse order, including after failures. A failed release does not imply actual native resource release; no automatic retry. Teardown clears the host ledger after attempts, with diagnostic errors retained, not silently hidden.
L5: Stop is idempotent/cooperative, not interruption. Before Run, first valid Run returns Stopped without invoking factories/work; that attempt consumes the session. During a factory, finish/commit its returned resource, then stop before later factories/work. During work, its current callback may finish; host invokes no further work. Return Stopped after cleanup unless an execution/cleanup failure occurred. Stop after completed Run is harmless.
L6: Dispose before Run prevents any run (rejected); repeated Dispose harmless. Dispose inside factory/work requests stop and defers teardown until that callback returns; no resource released while callback still uses it. Dispose after Run never repeats releases. Session resource release does not depend on caller remembering Dispose after Run.
L7: Session affinity is captured at CreateSession on the current managed thread. Run/Stop/Dispose are synchronous and enforce that identity. Wrong-thread use rejects without mutating ownership/state. Configuration creates independent sessions on each caller's thread; it has no session ownership. No marshalling, locks, async or cross-thread cancellation contract.
L8: Exceptions from acquisition/work are execution failures returned in a report after cleanup. Initialize/work error is primary; release errors secondary in attempted order. Without prior failure, first release error becomes primary, remaining releases secondary. Stop plus any failure returns Failed. Success/Stopped have null PrimaryError and empty CleanupErrors.
L9: Invalid API use is not an execution outcome. Direct invalid calls throw; if a host-generated API-usage exception (including argument validation) escapes a factory/work/cleanup delegate, Run cleans every committed resource and rethrows the original exception. If execution/cleanup errors also exist, Run throws a standard ArgumentException or InvalidOperationException wrapper matching the misuse category whose InnerException is an AggregateException preserving the misuse first, execution primary if distinct, and release failures in order. Consumer-thrown unrelated InvalidOperationException remains an ordinary execution failure; caught/recovered misuse inside consumer work is the consumer's responsibility. Public null arguments are validated before consuming a run. Engine-created argument validation is marked internally, including configuration validation; consumer-created ordinary ArgumentException remains an execution failure. Invalid factory return values are execution failures, not marked API misuse.

## Trace-to-acceptance matrix

| Case | Observable public-interface expectation |
| --- | --- |
| T01 normal | factory A/B; work; release B/A; Succeeded |
| T02 pre-run Stop | zero factory/work; Stopped; second Run rejected |
| T03 Stop in last factory | returned resource owned/released; zero work |
| T04 partial factory failure | step cleans its partial state; earlier resource released; primary retained |
| T05 Stop in factory B | B returns/commits then B/A cleanup; no C/work |
| T06 callback failure | no retry/work continuation by host; reverse cleanup; original primary |
| T08 cleanup failure | later release fails; earlier release still attempted; Failed |
| T09 multiple failures | work primary; release B/A ordered secondary |
| T10 repeated Stop/Dispose | releases not repeated |
| T11 invalid nested/second/disposed Run | rejection; active outer Run teardown then rethrow if misuse escapes |
| T12 empty configuration | work succeeds; no assumed subsystem |
| T14 Dispose in callback | resources remain alive until callback exits; reverse cleanup then Stopped |
| T15 configuration snapshot/reuse | editing supplied array cannot replace factories; fresh sessions independent |
| T16 thread affinity | foreign-thread Run/Stop/Dispose rejected; owner session still usable |
| T17 arguments/acquisition result | invalid config/callback rejected; null/duplicate returned resource fails without double release |
| T18 misuse plus cleanup failure | invalid-operation throw with ordered retained diagnostic causes |
| T07/T13 native end/begin errors | N/A for H0; remain CAP-002 candidates, no invented frame adapter |

Use deterministic trace resources as application disposables, not mocks of a selected backend. Run public acceptance tests from a separate assembly; no InternalsVisibleTo. External probe validates success, callback-failure and pre-run-stop through project reference and exit/output checks. Tests do not assert private fields.

## Validation and approval record

Owner accepted architectural limits on 2026-10-12 and delegated minimal signatures/mechanisms. H0-1 finalized by Codex root under owner delegation after h0_design_review supplied-snapshot consistency review; no blockers. Explicit consumer restore correction confirmed by targeted recheck. Tooling prevented reviewer direct filesystem reads; supplied contract snapshot reviewed, not runtime proof. Native frame/scheduler policy remains unapproved. [Toolchain](../toolchain.md); [ME-004](../backlog.md#me-004--implement-minimal-externally-consumed-lifecycle).

H0-1.1 correction: independent targeted authority review confirmed argument-validation gap against owner error/misuse separation. No signature, public type or dependency added. Tests observed RED4/31 for engine argument origin; internal marker correction followed. Targeted independent recheck h0_final_verify passed: 15 affected tests, no new public types/signatures/dependencies. Parent full suite31 passed, zero failed/skipped.

## Implemented evidence — 2026-10-13

[Hosting library](../../../engine/Hosting/Hosting.csproj), [public contract tests](../../../checks/Hosting.Contracts/LifecycleTests.cs), [independent consumer](../../../examples/LifecycleProbe/Program.cs). Parent full public suite31/31; independent initial scratch26+3probes passed; targeted correction scratch15/15 passed. Native-frame cases T07/T13 remain N/A. No scheduler/timing guarantee. Source candidate manifest after correction: BB537B427946017C372596984FA6D9DCCC890997ADB7C28F00EB3A1852C42870; baseline75a5eef1770f31235c69f8dcc9019de530226886, uncommitted codex/h0-lifecycle. [H0 manual packet](../milestones/H0.md).
