# Discoveries, debt and new needs

This is a lightweight intake ledger, not a second backlog. No item authorizes implementation.
Record concrete discoveries during work; link their eventual task or decision rather than
copying it. Keep old IDs when resolved.

## Record format and triage

ID (F-NNN), date/source task, kind (defect/debt/discovery/need), evidence, impact,
affected criteria, proposed next action, status (recorded/triaged/linked/resolved/deferred)
and task/ADR links. Never record secrets or unsupported root causes.

An in-scope acceptance defect may be corrected in the current task with evidence.
An out-of-scope need remains intake until refinement and approval. Prioritize by blocked
accepted behavior and consequence, not opportunistic enthusiasm. Substantial product vision/strategic scope changes go to the owner; technical contract changes follow delegated ADR/spec approval and independent review in autonomy.md. Preserve technical debt intentionally deferred with reason.
Review relevant entries at selection and milestone refinement; no periodic meeting required.

## Known planning constraints

| ID | Source | Kind/evidence | Impact and next action | State |
| --- | --- | --- | --- | --- |
| F-001 | Planning setup, 2026-10-12 | Platform/distribution targets not approved | ME-001 investigates; ME-003 prepares owner decision | linked |
| F-002 | Planning setup, 2026-10-12 | No greenfield projects or runnable engine test commands | ME-004 remains blocked until explicit contracts/authority and runner packet exist | linked |

| F-003 | ME-002, 2026-10-12 | discovery: cleanup attempt versus successful native release must be distinguished | L4 reports failed releases instead of claiming they succeeded; approval pending | linked |
| F-004 | ME-002, 2026-10-12 | discovery: retained whole-tick backlog would make naive interpolation exceed one | F4 separates fractional remainder; overload/pause/edge policies remain ME-003 decisions | linked |

## F-005 — H0 runner environment differs by tool

Origin: ME-004. Node child dotnet commands fail NuGet path resolution; shell commands restore and run actual tests despite FileSystem warning. Public sources only. Escalation failed unresolved sandbox readonly mount; no policy changed. Use observed shell test summaries, never test discovery alone as PASS. Workload inventory failure does not imply managed build failure. Status: recorded environment limitation, no scope expansion.

## F-006 — Argument validation needs origin classification

Origin: ME-004 independent targeted authority review, 2026-10-13. Earlier H0-1 narrowed misuse to own invalid-operation errors; own ArgumentNull/Argument validation escaping callbacks incorrectly became execution failures. Corrected H0-1.1 with internal origin marker; ordinary consumer exceptions remain execution failures. RED4/31, GREEN31/31; targeted independent15/15 pass. Status: resolved within scope; no public surface/dependency added.

## F-007 — Serial MSBuild and sandboxed testhost connection

Origin: ME-004, 2026-10-13. Independent h0_build_diagnosis reproduced silent graph failure with >1MSBuild node around build-only executable consumer reference; serial graph builds. Use -m:1 in documented checks. Sandboxed VSTest aborted90s connecting to testhost; identical authorized outside-sandbox command passed31 tests. Status: environment limitation recorded, not a need for engine concurrency or global sandbox changes. Revisit parallel build only when actual workload requires it; no new speculative task.

## F-008 — Stop must be checked after platform callbacks

Origin ME-011,2026-10-15 final acceptance audit. Public supplied platform can request retained FrameControl stop during Begin/Canvas access. RED2cases drew again despite stop because FrameLoop checked only before Begin. Fix rechecks after Poll/Begin/Canvas while retaining End after committed Begin; full managed24pass, H031pass and native9pass, native harnessPASS. Independent targeted verdict recorded in backlog before closure. No criterion/scope change or coverage reduction. Status: resolved; independent targeted baseline2/2fail/current24/24pass, no blockers.

## F-009 — Narrow renderer surface does not imply narrow native binary

Origin ME-010,2026-10-15. Restored raylib6build logs include audio/3D modules even though MicroEngine exposes only primitive2D. Desktop alone references exact Raylib-cs8.1.0; managed headless graph has none. Footprint/update and embedded third-party redistribution review are real costs, not new engine-capability authority. Status: documented/deferred distribution review, no redistribution approved.

## F-010 — Next platformer gap is concrete environment interaction

Origin H1.0 tracer evidence,2026-10-15. External model currently clamps one floor and draws one player, with no obstacle/animation/content. Next refinement candidates: bounded static rectangle obstacle queries and consumer-owned movement resolution; minimal pose/clip timing after the need is specified. Do not infer ECS/scene/physics/asset-manager necessity. Status: recorded next-horizon input; full H1 not accepted, no speculative broad implementation task. Subjective baseline feedback may affect feel/presentation, not general geometry correctness.

## F-011 — Desktop friend-name coupling is not a public extension seam

Origin ME-013/ADR-0004,2026-10-15; baseline e5df4fe, engine/Loop/Contracts.cs assembly InternalsVisibleTo Desktop, internal FrameApiException constructor. Loop/Hosting have no concrete project/package dependency, but this is real name-based privileged error-origin coupling. Public IFramePlatform/ICanvas implementations already run without Desktop/friendship; their ordinary exceptions remain execution failures, not equivalent privileged engine misuse. Status: recorded limitation, NOT resolved by documentation. No friend-name copying for future adapters; any change to this error/adapter boundary must review a backend-independent guard/origin mechanism preserving failure distinction before broadening conformance claims. No speculative API or automatic refactor/new backend task now. Current H0/H1 acceptance unchanged.

F-011 implementation update2026-10-16: reviewed ADR-0005/H1-R1.1 introduced FrameApi.Require and removed Loop Desktop friendship. Seven Desktop checks migrated,1Loop-internal guard retained; runtime tests and public nonfriend provider prove author checks36/9/31+native harness. Independent verification pending; earlier unresolved documentary assessment retained historically, no full-backend/portability claim.

F-011 resolved2026-10-16 (ME-015): independent provider_guard_verify confirms no Desktop friend in compiled Loop metadata, public-only nonfriend provider guard tests and seven migrated native guard sites preserve semantics. Loop36/Desktop9/H031 all pass and native harnessPASS. Remove/generalize named-friend privilege requirement fulfilled via ADR-0005; older assessment retained historically. This does not prove arbitrary backend compatibility or future target support.

## F-012 — Consumer landing-edge tolerance

Origin owner subjective feedback2026-10-16/ME-016. Movement immediate/comfortable and primitives readable; landing-edge jump feels occasionally missed, explicitly nonblocking observation. landing_input_map scratch public model trace tick39 reproduces a compatible case: grounded eligibility tested before contact; edge ignored then landing resolved, no queued intent. Loop edge retention already correct; not confirmed native event loss/engine defect. PC-1 proposes optional per-controller buffering, parameterless disabled, sample100ms; coyote deferred because no ledge/walk-off case. Status linked ME-016/017; unchanged movement constants/other models/Core.
