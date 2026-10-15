# Executable backlog

Single task-state source of truth. Dated task records distinguish preparation from implementation.
Priority P0 precedes P1; ID ascending breaks ties. Completion is recorded only with task evidence.
Read [execution](execution.md), [autonomy](autonomy.md) and the [task template](templates/task.md).

## Current horizon: prepare the first lifecycle increment

| ID | Kind | Priority | State | Dependencies | Packet |
| --- | --- | --- | --- | --- | --- |
| ME-001 | investigation | P0 | completed | none | Below |
| ME-002 | specification | P0 | completed | none | Below |
| ME-003 | decision preparation | P0 | completed | ME-001, ME-002 | Below |
| ME-004 | implementation | P1 | completed | ME-003; owner approval | Below |

ME-001/002/003 preparation and ME-004 automated construction are complete. H0 owner acceptance was recorded2026-10-15; the next bounded horizon is H1 candidate preparation, not implementation. Earlier preparation authority did not itself approve construction; later scoped approval is recorded in ADR-0002.
Select from the current table; initial planning selection was ME-001.

## ME-001 — Investigate initial platform and delivery constraints

- Authority: owner planning request, 2026-10-12; research/documentary output only.
- Links: FR-001, NFR-002/NFR-003; CAP-001; [roadmap](roadmap.md).
- Scope: compare Windows-only versus Windows/Linux/macOS desktop targets and source versus
  package distribution. Distinguish maintainability, native deployment, CI and test access.
- Exclusions: do not select a backend, create projects, install dependencies, contact private
  services or claim platform support. No implementation.
- Allowed edit surfaces: docs/engineering/investigations/platform-scope.md (new);
  docs/engineering/backlog.md (ME-001 state/evidence only);
  docs/engineering/findings.md (linked discoveries only).
- Deliverable: dated report with sourced constraints, viable options/tradeoffs, uncertainties
  and one bounded owner decision. Public official sources; record URLs and access dates.
- Acceptance: both platform and delivery options compared; facts versus assumptions marked;
  no recommendation accepted implicitly; inputs for ME-003 explicit.
- Checks: readback, source support, relative links and git diff --check; runtime tests N/A.
- Risk/review: low-risk research; structural/source verification, no native runtime.
- Failure: inaccessible evidence is a documented limitation; block only consequential claims.
  No mutation beyond report means rollback is removal of this report and its task notes.
- Evidence required: baseline, report locator, citations, review and unresolved decisions.
- Claim/evidence: not claimed; no results recorded.

- 2026-10-12: claimed by Codex root for this authorized session; baseline 75a5eef1770f31235c69f8dcc9019de530226886. Existing documentary changes and user .gitignore edit preserved; output restricted to packet surfaces.

- 2026-10-12: report authored at investigations/platform-scope.md. Platform/source/package comparisons and source support read back; .NET deployment cross-checked with Context7. Runtime tests N/A (research-only); recommendation remains pending.

- 2026-10-12: structural/source verification by Codex root passed; git diff --check exit 0; official URLs/access date recorded. Acceptance: platform alternatives, delivery alternatives, facts/assumptions and ME-003 inputs satisfied. Evidence: investigations/platform-scope.md; baseline 75a5eef1770f31235c69f8dcc9019de530226886; uncommitted documentary result. No platform/backend approval.

## ME-002 — Refine lifecycle and frame contracts without selecting implementation

- Authority: owner planning request, 2026-10-12; proposed contracts only.
- Links: NFR-001/NFR-002; CAP-001/CAP-002;
  [lifecycle spec](specs/lifecycle.md), [frame spec](specs/frame-execution.md).
- Scope: verify contract consistency through traces of success, zero-frame exit, init/run/
  cleanup failures, repeated stop, overload and zero/multiple ticks; refine examples.
- Exclusions: no framework/library selection, API signatures, implementation or approval.
- Allowed edit surfaces: docs/engineering/specs/lifecycle.md;
  docs/engineering/specs/frame-execution.md;
  docs/engineering/backlog.md (ME-002 state/evidence only);
  docs/engineering/findings.md (linked discoveries only).
- Acceptance: each invariant has observable test intent; all failure transitions and ordering
  specified; frame behavior distinguished from lifecycle; open choices explicitly enumerated.
- Checks: tabletop trace review and document/link checks. Runnable engine tests N/A;
  proposed future assertions must not be recorded as passing tests.
- Risk/review: proposed public-contract design; independent consistency review before closing.
- Failure: contradiction unresolved -> blocked with exact case. Restore only own edits if needed.
- Evidence: baseline, revised spec locators, trace table, reviewer verdict, approval still pending.
- Claim/evidence: not claimed; no results recorded.

- 2026-10-12: claimed by Codex root; baseline 75a5eef1770f31235c69f8dcc9019de530226886. ME-001 closed; ME-002 independent specification scope only.

- 2026-10-12: revised L1-L8/T01-T14 and F1-F9/U01-U15 traces; init ownership transfer, stop/dispose boundaries, cleanup-error ordering and overload alpha clarified. Specs remain proposed. Arithmetic and invariant-to-case mapping readback; engine tests N/A; independent consistency review requested.

- 2026-10-12: completed after independent reviewer contract_verify found no consistency blockers across L1-L8/T01-T14 and F1-F9/U01-U15; required corrections none. Engine tests N/A; contracts not approved. Documentary checks passed, uncommitted evidence at the two spec locators.

## ME-003 — Prepare minimal construction decision packet

- Authority: documentary preparation only; architecture approval belongs to owner.
- Depends on completed ME-001 and ME-002, not just files appearing.
- Scope: prepare bounded proposed ADR(s) for platform scope, minimal backend/project/test
  arrangement, and lifecycle contract. Identify approval questions without deciding them.
- Allowed surfaces: docs/engineering/decisions/0002-initial-execution-scope.md (new);
  docs/engineering/decisions/README.md;
  docs/engineering/backlog.md (ME-003/ME-004 records only);
  docs/engineering/specs/lifecycle.md; docs/engineering/specs/frame-execution.md.
- Acceptance: alternatives and evidence from both dependencies; exact proposed construction
  surfaces, check commands and external-consumer acceptance; explicit unresolved approvals.
- Checks/review: source/trace consistency and independent design readback; runtime N/A.
- Exclusions: installs, code, acceptance on behalf of owner and advance selection of ECS.
- Rollback: own proposed ADR and linked notes only. Preserve prior evidence.
- Evidence: approval packet locator and pending owner decisions; no approval assumed.
- State evidence: pending until dependencies completed.

- 2026-10-12: pending -> ready after ME-001/ME-002 completion; readiness verified against both evidence records. ready -> in-progress; claimed by Codex root, baseline 75a5eef1770f31235c69f8dcc9019de530226886; documentary preparation only.

- 2026-10-12: ADR-0002 proposed with dependency evidence, alternatives, exact candidate surfaces, future check commands and separate-consumer acceptance. All approvals pending; independent design readback requested. Runtime tests N/A.

- 2026-10-12: completed. Independent contract_verify design readback found no substantive blockers; one register-table seam corrected and targeted recheck confirmed resolved. Relative links and git diff --check passed. Evidence: decisions/0002-initial-execution-scope.md and decisions/README.md; baseline 75a5eef1770f31235c69f8dcc9019de530226886; uncommitted documentary output. Approval remains pending.

## ME-004 — Implement minimal externally consumed lifecycle

- State: completed; blocked -> pending -> ready after H0-1/toolchain independent review and link/readiness readback, 2026-10-12. Claimed by Codex root; baseline 75a5eef1770f31235c69f8dcc9019de530226886 on codex/h0-lifecycle before source construction. Reviewer h0_design_review supplied-snapshot gate; explicit consumer restore correction resolved.
- Authority: owner explicitly permits construction after documentary verification; ADR-0002 records accepted scope and technical delegation. No remote/main integration or commits authorized.
- Dependencies: ME-001/002/003 completed; independent H0-1 specification/toolchain readback.
- Objective: permanent headless hosting, immutable reusable configuration and single-use synchronous sessions; independent public consumer proves normal/failure/stopped behavior.
- Exclusions: native frames/backends, scheduler, ECS/scenes/physics/audio/input, packaging/CI, future games and shared runtime abstractions.
- Accepted construction surfaces after review: global.json; NuGet.config (public-only source configuration, delegated test tooling); engine/Hosting/{Hosting.csproj,EngineConfiguration.cs,ExecutionSession.cs,ExecutionReport.cs}; checks/Hosting.Contracts/{Hosting.Contracts.csproj,LifecycleTests.cs}; examples/LifecycleProbe/{LifecycleProbe.csproj,Program.cs}. New files only in these narrow directories; documentation updates in AGENTS.md, docs/engineering/{README.md,contributing.md,roadmap.md,current-state.md,backlog.md,autonomy.md,execution.md,requirements.md,document-policy.md,findings.md,milestone-delivery.md,toolchain.md,specs/lifecycle.md,specs/frame-execution.md,decisions/0002-initial-execution-scope.md,decisions/README.md,milestones/H0.md}; recovery log odd/tasks/h0-lifecycle.md. Preserve existing .gitignore and unrelated changes.
- Contract: specs/lifecycle.md H0-1.1 after independent review; CAP-002 unapproved/outside H0. Toolchain: toolchain.md exact versions.
- Acceptance A1: library/test/public-only consumer separate net10 projects, no production packages or legacy references.
- A2: H0-1 T01-T06/T08-T12/T14-T18 observable public tests pass, including failure ownership, ordered cleanup, misuse separate from execution failure, thread rejection and reusable configuration. T07/T13 native frames N/A.
- A3: independent consumer success/stopped exit 0 with expected trace; callback-failure exit 1 with primary type, cleanup trace and no success; invalid probe arguments exit 2 with usage.
- A4: actual SDK/restore/build/test/probe commands from toolchain.md observed, errors recorded; no workload or platform support inference. RED -> GREEN per behavior, final suite and independent verification.
- A5: synchronized docs, evidence and tested manual H0 packet; awaiting owner manual validation, not marked milestone passed.
- Risk/review: public contracts/toolchain require independent design gate and post-code independent verification. One correction batch then targeted recheck; unresolved blocker stops affected work.
- Rollback: remove only newly created H0 source/test/consumer/global.json and revert own H0 documentation deltas; preserve pre-session uncommitted planning work and .gitignore. No destructive rollback automatically.
- Evidence: baseline 75a5eef1770f31235c69f8dcc9019de530226886; branch codex/h0-lifecycle; executor Codex root. No claim to passing code yet.

- 2026-10-13: normalized source and implemented public-only consumer. Final suite26 passed/0failed/0skipped outside sandbox; serial MSBuild required and sandbox loopback timeout recorded. Independent h0_final_verify now checks frozen code from scratch; closure pending. RDD globaloff observed with successful readonly query, no native review or mode change.

- 2026-10-13 closure: A1 separate public-only projects/net10, no production packages/legacy: pass. A2 public traces incl origin-classified argument misuse:31 passed/0failed/0skipped; native T07/T13 N/A. A3 success/failure/stopped/unknown exact output and exits0/1/0/2: pass in agent commands and tests. A4 SDK/build/restore confirmed, serial checks recorded; VSTest sandbox timeout and approved outside-sandbox success not conflated. A5 docs/source/tests synchronized, H0 delivery packet tested; manual acceptance pending.
- Independent h0_final_verify initial scratch source review26+3probes, then targeted authority correction required argument origin classification. One correction batch RED4/31 -> GREEN31/31; targeted scratch15/15 confirmed resolved, no remaining blockers, four public types/no friend assembly. Parent full31/31 spotcheck repeated afterward. RDD globaloff exit0; no native review/mode change.
- Change locator: baseline75a5eef1770f31235c69f8dcc9019de530226886; uncommitted codex/h0-lifecycle; source manifestBB537B427946017C372596984FA6D9DCCC890997ADB7C28F00EB3A1852C42870. Code/check surfaces above + capabilities.md current CAP-001 evidence only; no future scope changed. User .gitignore/preexisting documentary changes preserved. No commit/remote/main integration. Evidence: milestones/H0.md, toolchain.md, lifecycle H0-1.1 and recovery log. Next eligible tasks:none; owner manual H0 validation is next gate.

- 2026-10-15: owner explicitly accepted H0 based on delivered evidence/criteria; milestone verified, no scope expansion or asserted manual rerun. ME-004 remains completed. Implementation/test/consumer/contracts commit:0771a8344861e3c1d76f7a4285869cec1ba60220; precommit SDK10.0.402 restore+31tests pass,10source hashes matched, no unrelated .gitignore/.atl/.codegraph staged.

<details><summary>Historical blocked packet (before owner approval)</summary>

## ME-004 — Implement minimal externally consumed lifecycle

- State: blocked, not executable. Do not invent allowed paths or commands now.
- Blockers: platform/backend/project/test choices, accepted lifecycle contract, implementation
  authority and complete execution packet are absent.
- Links: CAP-001, NFR-001/NFR-002, proposed lifecycle spec.
- Intended bounded outcome: external consumer starts, runs, stops and cleans up, including
  partial failures, verified through public behavior.
- Exclusions: ECS, scene framework, physics, assets pipeline, genre demos and future scope.
- Before ready: ME-003 outputs explicitly approved; concrete paths, dependencies, runner
  commands/environment, acceptance tests and review/rollback plan populated here.
- Required evidence: consumer integration, lifecycle traces, failed-path cleanup, docs
  synchronized with actual supported API. Performance budgets only if approved.
- It remains blocked even if preparation tasks complete but human approval is missing.

- 2026-10-12: dependency ME-003 is completed but owner gates remain unmet. Still blocked: platform/support promise, headless/native proof, source/package consumption, SDK/target and test dependencies, lifecycle signatures/policies and explicit implementation authority. ADR-0002 records alternatives and proposed candidate paths/commands; none is accepted. No independent ready work remains in this horizon.


</details>

## Updates

Record dated transition reason, executor/reviewer, baseline/change locator, checks and
findings under each packet. Preserve past results. Agents may prepare tasks objectively,
not approve contracts. Add only concrete next-horizon work; future catalog rows are not tasks.
