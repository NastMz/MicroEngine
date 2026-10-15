# H1.0 — Proposed first graphical tracer

Status: historical scenario proposal refined into approved [H1-R1](h1-runtime.md),2026-10-15. Use H1-R1 for exact construction contract and milestone evidence for actual checks. ME-005 original proposal preserved below.
See [H1](../milestones/H1.md). H0 acceptance does not approve this scope.

## Purpose and boundaries

Validate a narrow path toward an external Mario-inspired game, not complete H1.
Candidate engine capabilities: CAP-002 time execution, CAP-003 input and
CAP-004 presentation. The consumer owns movement, gravity, jump eligibility and floor
resolution. No platformer module, ECS, scenes, physics solver, audio, animation
or asset pipeline. H0 headless operation must remain independent of native loading.

## Proposed observable scenarios

| Trace | Input | Expected observable result | Future evidence |
| --- | --- | --- | --- |
| G01 | Launch consumer | One window with primitive player/floor | Native smoke, image and process trace |
| G02 | Hold/release horizontal input | Movement while held; release stops horizontal movement | Scripted input/state assertions |
| G03 | Grounded jump press, hold, airborne release/repress | One jump per eligible press, no held/airborne repetition | Consumer-rule tests |
| G04 | Land | Consumer resolves player onto floor without falling through | Deterministic state tests |
| G05 | Close window or request stop | No later update/presentation; owned native resources released once | Integration trace/exit |
| G06 | Initialization/update failure | Acquired resources released; failure distinct from invalid API use | Injected failures/outcome |
| G07 | Lose focus while input held | No stuck held input on return | Adapter tests and feasible native automation |
| G08 | Zero/one/multiple updates per presentation | Fixed delta, bounded overload, input edges not duplicated | Fake-clock traces |

These are proposed targets, not passing results. Keys, dimensions, units, tick rate
and overload cap are local details to specify before construction. ME-008 must
define exact frame ordering, edge consumption, stop boundaries and focus/minimize
semantics with examples. Existing frame-execution proposals are not accepted by reference.

## Contract guardrails

Keep native types/handles private; separate timing/input/presentation from game rules.
Specify single-thread ownership and partial-initialization cleanup. Do not expand
H0 reports with native internals or require rebuilding the headless runtime.
No signatures, project partition or binding are selected here. H0-incompatible
changes require separate authority and regression review.

## Verification and acceptance

Before construction ME-008 must supply exact paths/signatures, package/native versions,
runner commands, deployment checks and independent review. Automate deterministic
traces, H0 regressions, native startup/stop/failure and feasible focus/input checks.
Record hardware/session limitations; screenshots alone prove neither input nor cleanup.
Human evaluation concerns product feel/readability and unobservable behavior, not
repetition of automated checks. H1.0 acceptance would not accept full H1.

Full H1 still requires refinement of CAP-005/006/007/008, including animation/content
and richer spatial interaction as applicable. None is declared implemented here.
