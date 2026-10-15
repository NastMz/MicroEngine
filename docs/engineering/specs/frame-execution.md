Current relation2026-10-15: [H1-R1](h1-runtime.md) approves/implements a bounded fixed-step/input/presentation slice outside H0. The earlier alternative matrix below remains historical proposal evidence, not a competing active implementation contract.

# CAP-002 — Frame execution contract refinement

Status: proposed; approval pending; implementation: none. ME-002 refinement, 2026-10-12.
Requirements: NFR-001/NFR-002/NFR-003. Companion: [lifecycle](lifecycle.md).
CAP-002 is outside approved H0 scope (ADR-0002); no scheduler/input library is approved.

## Proposed ordering and invariants

F1: At the frame boundary check stop before acquiring frame state. Read monotonic elapsed
time once; reject negative/nonfinite elapsed and invalid configuration explicitly.
F2: Observe platform events and capture input before simulation. Stable held state may be
used by successive ticks. Ordered discrete edges are consumed once by the chosen simulation
channel, not once per catch-up tick. Zero ticks retain unconsumed edges.
F3: Accumulator starts at zero. Approved fixed interval h is positive/finite; max tick count
is a positive integer. Each tick receives h. Subtract h only after a completed tick;
failure ends the session, not retries an uncertain tick.
F4: Presentation follows ticks and cannot change authoritative simulated state through draw.
Interpolation uses a remainder strictly below h; a retained full-tick backlog is not passed
as alpha > 1. Choose backlog/remainder semantics explicitly below.
F5: Begin/end bracket native drawing. After committed begin, attempt end exactly once even
if drawing fails; partial begin belongs to the adapter. Error precedence follows lifecycle.
F6: Stop during any callback suppresses later non-cleanup callbacks and frames. If begin
has not occurred do not open a frame; if it has, perform end then session teardown.
F7: Catch-up is bounded. No silent clamp or time discard. Report applied elapsed clamp,
executed ticks and discarded/deferred time according to the approved overload policy.
F8: Paused simulation does not accumulate a resume burst. Real-time input/presentation
behavior and cancellation of pending edges require a chosen pause policy.
F9: Fixed interval does not promise cross-platform floating-point or solver determinism.

## Explicitly unresolved options

| Concern | Alternatives | Consequences / before implementation |
| --- | --- | --- |
| Overload after max ticks | Discard remaining whole ticks, keep fractional remainder; or defer whole-tick backlog separately | First loses simulated time with diagnostic; second carries latency. Define cap and presentation alpha in either case |
| Long elapsed clamp | Reject/signal overload; or apply configured clamp with discarded-time diagnostic | No undisclosed default or coercion |
| Paused edges | Retain ordered simulation edges; or cancel them with a reason on pause transition | Retention needs a bounded queue/overflow rule; cancellation changes gameplay semantics |
| Input channel | Minimal lifecycle has no input; or explicitly approved simulation-edge queue | Do not create an input framework merely for a lifecycle experiment |
| Pause presentation | Continue real-time presentation or freeze presentation too | Callback/clock semantics need approval |
| Interpolation | No interpolation in minimal H0; or explicit fractional presentation value | Never use accumulated whole backlog as interpolation |
| Parameter defaults | Approved values versus mandatory explicit configuration | h/max/clamp/queue overflow cannot be guessed |

These choices are proposals. Determinism beyond ordered traces is not accepted scope.

## Tabletop trace matrix

Numbers below are test inputs, not selected defaults. h = 10 ms, max = 2 for examples.

| Case | Trace and expected observable result |
| --- | --- |
| U01 Zero ticks | elapsed 4; accumulator 4; no tick; alpha .4 if approved; pending edge retained |
| U02 Next frame consumes edge | previous 4 + elapsed 6 = 10; one tick(h); remainder 0; edge delivered once |
| U03 Multiple ticks | elapsed 25; two ticks(h,h); remainder 5; held state twice; one edge once; alpha .5 |
| U04 Overload/discard option | elapsed 45; two ticks; whole 20 discarded with diagnostic; remainder 5; alpha .5 |
| U05 Overload/defer option | elapsed 45; two ticks; deferred whole 20 separated from fractional 5; alpha .5; later catch-up bounded |
| U06 Simulation fails | tick throws; no presentation/begin; session teardown; no retry/next frame |
| U07 Draw fails | begin commits; draw throws; end attempted; session teardown; draw primary |
| U08 End also fails | U07 plus end throws; draw primary, end secondary |
| U09 Invalid inputs | negative/NaN elapsed, h <= 0, noninteger/nonpositive max rejected before callbacks |
| U10 Pause/resume | no accumulation during pause; resume adds only approved new elapsed, not paused duration |
| U11 Stop before begin | earlier callback requests stop; no presentation/draw; teardown only |
| U12 Stop during draw | begin; draw requests stop; no later draw; end once; teardown |
| U13 Begin partial failure | adapter cleans partial state; no draw or host end; session teardown |
| U14 Pending edge cancellation | zero ticks then pause; edge retained or cancelled according to accepted policy, never silently dropped |
| U15 Queue overflow | approved bound reached; explicit overflow diagnostic and accepted policy; no unbounded memory |

Future tests cover F1-F8 through these traces, and F9 via documentation/limited guarantee.
Engine unit/integration commands remain N/A until project scope is approved. Tabletop review
checks arithmetic/order only; it cannot certify a native frame or installed scheduler.
