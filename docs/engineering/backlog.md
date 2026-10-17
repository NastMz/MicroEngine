# Executable backlog

Single task-state source of truth. Dated task records distinguish preparation from implementation.
Priority P0 precedes P1; ID ascending breaks ties. Completion is recorded only with task evidence.
Read [execution](execution.md), [autonomy](autonomy.md) and the [task template](templates/task.md).

## Accepted foundation and current executable horizons

| ID | Kind | Priority | State | Dependencies | Packet |
| --- | --- | --- | --- | --- | --- |
| ME-001 | investigation | P0 | completed | none | Below |
| ME-002 | specification | P0 | completed | none | Below |
| ME-003 | decision preparation | P0 | completed | ME-001, ME-002 | Below |
| ME-004 | implementation | P1 | completed | ME-003; owner approval | Below |
| ME-005 | specification | P0 | completed | H0 acceptance | Below |
| ME-006 | investigation | P0 | completed | H0 acceptance | Below |
| ME-007 | decision preparation | P0 | completed | ME-005, ME-006 | Below |
| ME-008 | specification | P1 | completed | ME-007; reviewed delegated ADR-0003 approval | Below |
| ME-009 | implementation | P1 | completed | ME-008 | Below |
| ME-010 | implementation | P1 | completed | ME-009 | Below |
| ME-011 | implementation correction | P0 | completed | ME-009 | Below |
| ME-012 | subjective product validation | P1 | completed | ME-010 | Below |
| ME-013 | architectural documentation | P0 | completed | none | Below |
| ME-014 | specification | P0 | completed | ME-013 | Below |
| ME-015 | implementation correction | P0 | completed | ME-014 | Below |
| ME-016 | specification | P0 | completed | ME-012 | Below |
| ME-017 | consumer implementation | P0 | completed | ME-016 | Below |
| ME-018 | specification | P0 | completed | ME-017 | PO-1 below |
| ME-019 | consumer implementation | P1 | completed | ME-018 | PO-1 below |
| ME-020 | specification | P0 | completed | ME-019 | AV-1 below |
| ME-021 | presentation implementation | P1 | completed | ME-020 | AV-1 below |
| ME-022 | specification | P0 | completed | ME-021 | RE-1 below |
| ME-023 | adapter implementation | P0 | completed | ME-022 | RE-1 below |
| ME-024 | consumer implementation | P1 | completed | ME-023 | RE-1 below |
| ME-025 | delivery policy | P0 | completed | none; owner instruction | Below |
| ME-026 | specification | P0 | completed | ME-024 | IM-1 below |
| ME-027 | consumer implementation | P1 | completed | ME-026 | IM-1 below |
| ME-028 | specification | P0 | completed | ME-027 | TM-1 below |
| ME-029 | consumer implementation | P1 | completed | ME-028 | TM-1 below |
| ME-030 | specification | P1 | completed | ME-029 | [CP-1](specs/consumer-checkpoint.md) |
| ME-031 | consumer implementation | P1 | completed | ME-030 | [CP-1](specs/consumer-checkpoint.md) |
| ME-032 | specification | P1 | completed | ME-031 | [CF-1](specs/checkpoint-files.md) |
| ME-033 | consumer adapter | P1 | completed | ME-032 | [CF-1](specs/checkpoint-files.md) |
| ME-034 | specification | P1 | completed | ME-033 | [CL-1](specs/checkpoint-launch.md) |
| ME-035 | consumer integration | P1 | completed | ME-034 | [CL-1](specs/checkpoint-launch.md) |
| ME-036 | onboarding documentation | P1 | completed | ME-035 | DOC-001 packet below |
| ME-037 | specification | P1 | completed | ME-036 | [NL-1](specs/optional-labels.md) |
| ME-038 | optional presentation | P1 | completed | ME-037 | [NL-1](specs/optional-labels.md) |
| ME-039 | specification | P1 | completed | ME-038 | [IMG-1](specs/optional-images.md) |
| ME-040 | optional image resources | P1 | completed | ME-039 | [IMG-1](specs/optional-images.md) |
| ME-041 | specification | P1 | completed | ME-040 | [SPR-1](specs/sprite-regions.md) |
| ME-042 | optional sprite regions | P1 | completed | ME-041 | [SPR-1](specs/sprite-regions.md) |
| ME-043 | specification | P1 | completed | ME-042 | [MAP-1](specs/map-viewport.md) |
| ME-044 | map model and presentation | P1 | completed | ME-043 | [MAP-1](specs/map-viewport.md) |
| ME-045 | map executable and verification | P1 | in-review | ME-044 | [MAP-1](specs/map-viewport.md) |

Use this table for current selection. Dated records below preserve earlier authority,
dependencies and failed/pending checks; later closure evidence supersedes those states.
H0 owner acceptance is recorded; later technical slices do not imply full milestone acceptance.

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


## ME-005 — Specify the next H1 validation tracer

- Authority: owner2026-10-15 accepted H0 and requested next-roadmap specifications/tasks. Documentary preparation only, not H1 scope approval.
- Dependencies: H0 accepted with source0771a8344861e3c1d76f7a4285869cec1ba60220.
- Objective: propose the smallest H1-facing external platformer validation, map reusable engine vs game-owned behavior and retain full-H1 gaps.
- Allowed surfaces: specs/h1-first-playable.md and milestones/H1.md (new under docs/engineering); roadmap.md and backlog.md ME-005 state/evidence, all under docs/engineering.
- Acceptance: observable scenario/exit/failure traces; CAP-002/003/004 boundaries; CAP-006/007/008 gaps explicitly deferred as needed, no full-H1 completion claim; game jump/collision rules outside mandatory engine; approval gaps and verification ownership explicit.
- Checks: relative links, trace/requirement mapping, git diff --check and independent documentary consistency review (can share ME-007 review batch). Runtime N/A, no executable created.
- Risk/rollback: proposed contracts only, preserve H0. No accepted signature/technology/requirement changes; own new proposal/task notes reversible.
- Claim: none; baselinec4f8e6b2b25b3e6c0fe2ed0130f718ebaa4c4dbc.

- 2026-10-15: claimed by Codex root; ready gate satisfied: H0 accepted, documentary authority/paths/criteria/checks complete; baselinec4f8e6b.

- 2026-10-15: completed after independent h1_preparation_verify readback. ME-005 CAP-003/004 label swap corrected in one batch; targeted recheck passed. ME-006 source claims supported, assessments/unknowns explicit. Evidence: specs/h1-first-playable.md, milestones/H1.md, investigations/h1-native-options.md. Runtime N/A; proposals not accepted. Baseline c4f8e6b.

## ME-006 — Investigate bounded native-validation choices

- Authority: same owner next-horizon preparation request, documentary/public-source investigation only. H0 accepted; independent of ME-005.
- Allowed surfaces: docs/engineering/investigations/h1-native-options.md (new), docs/engineering/backlog.md ME-006 state/evidence.
- Objective: compare graphical tracer versus headless timing-first order and two viable native binding candidates, with reversibility/packaging/threading/ownership/validation costs.
- Acceptance: dated primary sources, facts vs assessment/unknowns, candidate version support not asserted as installed/tested; no license/support assumption beyond sources; identify exact future evidence before dependency selection.
- Exclusions: no package install, native spike, engine/demo code, platform support promise, private service or publication; no accepted backend choice.
- Checks: source readback/links, git diff --check and independent documentary source/consistency readback (shared ME-007 batch allowed). Runtime N/A.
- Risk/rollback: research only; remove own report/task notes if needed, retain H0/source evidence.
- Claim: none; baselinec4f8e6b2b25b3e6c0fe2ed0130f718ebaa4c4dbc.

- 2026-10-15: claimed by Codex root; ready gate satisfied: H0 accepted, research-only authority/paths/criteria/checks complete; baselinec4f8e6b. Earlier candidate lookup is bibliographic preparation, detailed evidence read follows claim.

- 2026-10-15: completed after independent h1_preparation_verify readback. ME-005 CAP-003/004 label swap corrected in one batch; targeted recheck passed. ME-006 source claims supported, assessments/unknowns explicit. Evidence: specs/h1-first-playable.md, milestones/H1.md, investigations/h1-native-options.md. Runtime N/A; proposals not accepted. Baseline c4f8e6b.

## ME-007 — Prepare H1 construction-boundary decision

- Authority: documentary preparation, never owner approval. Depends on completed ME-005/006 evidence.
- Allowed surfaces: docs/engineering/decisions/0003-next-validation-tracer.md (new), decisions/README.md, backlog.md ME-007/ME-008 records and current-state.md/roadmap.md under docs/engineering.
- Acceptance: one bounded strategic scope decision; viable alternatives/consequences/evidence; recommend but do not accept; identify delegated-local details versus reserved decisions; exact next readiness/verification requirements without inventing implementation authority.
- Checks: dependency/source/spec consistency, relative links, git diff --check and independent design readback; runtime N/A. Preserve H0 criteria and source.
- Rollback: own proposed ADR/index/task notes only. State: pending until dependencies completed.

- 2026-10-15: pending -> ready after ME-005/006 completed independent evidence; ready -> in-progress, claimed Codex root, baseline c4f8e6b. Documentary scope only.

- 2026-10-15: in-progress -> in-review -> completed after independent h1_preparation_verify design readback. One correction batch repaired ADR table seam and H1 CAP-002 mapping; targeted recheck passed. Evidence: decisions/0003-next-validation-tracer.md, register, roadmap/current-state; runtime N/A. ADR remains proposed; Historical blocker, now superseded by technical delegation: ME-008 awaited owner authority. No ready independent task remains.

## ME-008 — Finalize H1.0 technical construction packet (historical pre-delegation outline)

- State: pending, not executable until technical readiness. Depends on completed ME-007 and independently reviewed delegated technical approval of ADR-0003 under autonomy.md. No H1 implementation task is currently ready.
- Required before ready: accepted bounded scenario, delegated native dependency authority, exact source/project surfaces, public signatures/ownership/input-time semantics, package/native version and license/deployment validation plan, Windows environment and commands, deterministic/public-consumer checks, independent review/rollback.
- Intended scope after authority: finalize only that packet; implementation requires separate complete ready task(s). No library/demo/backend may be improvised from this blocked outline.
- Technical decision under delegated review: whether to introduce a native window/input/presentation validation increment next, or stay headless for CAP-002 first. Both exceed accepted H0 behavior.
- No allowed construction paths/runner are fabricated before that decision. H1 remains candidate, not accepted.

## Updates

Record dated transition reason, executor/reviewer, baseline/change locator, checks and
findings under each packet. Preserve past results. Agents may prepare tasks and approve technical contracts through independent review and recorded approval under autonomy.md; code or task completion alone is not approval. Add only concrete next-horizon work; future catalog rows are not tasks.

Delivery ledger2026-10-15: H0 implementation0771a8344861e3c1d76f7a4285869cec1ba60220; acceptance c4f8e6b2b25b3e6c0fe2ed0130f718ebaa4c4dbc; ME-005..007 reviewed proposals 7addfd3911aaea67c2170d99736637e7ecf3749c. Historical closure before broader delegation: ME-008 was blocked; reevaluate under current autonomy.md.

## ME-008 current execution packet —2026-10-15

Authority: owner technical delegation/autonomy.md. Pending -> ready after ME-007 completed and authority reevaluation; ready -> in-progress claimed Codex root baseline81d2d2d. Scope: docs/engineering/specs/h1-runtime.md, specs/h1-first-playable.md, decisions/0003-next-validation-tracer.md, toolchain.md, decisions/README.md, backlog.md/current-state.md/roadmap.md and odd/tasks/technical-autonomy-h1.md. Deliver exact H1-R1 signatures/ownership/time/input/failure/package/source/check/rollback packet; no engine writes yet. Acceptance: independent design review then explicit delegated ADR/spec approval, no H0 break or full-H1 acceptance. Checks: source evidence/readback/links/diff-check; runtime N/A until code. Failure: keep construction unready if review blockers remain.

## ME-009 — Fixed-step/input/presentation public runtime

Authority: autonomy.md, accepted ADR-0003/H1-R1; FR-001/002,NFR-001/002. Ready after ME-008 independent design PASS. Scope new engine/Loop/*.cs, Loop.csproj; examples/FirstPlayable.Model/*.cs, csproj; checks/Loop.Contracts/*.cs,csproj; own backlog/evidence and H1/current-state/capabilities docs. No H0 source changes or native dependency. Acceptance all approved managed time/input/stop/error/ownership traces and consumer movement/jump/floor tested through public API; headless project graph has no Desktop; H031regressions unchanged. Runner serial restore/test Loop.Contracts and Hosting.Contracts Release from toolchain, test-first RED where deterministic. Risk public API/failure semantics, independent verifier required. Rollback remove only new projects and own docs; preserve user .gitignore. Claim not yet.

## ME-010 — Desktop adapter and external H1.0 tracer

Authority same ADR/spec. Depends on completed ME-009. Scope engine/Desktop/*.cs,Desktop.csproj; checks/Desktop.Contracts/*.cs,csproj,NativeSmoke.ps1; examples/FirstPlayable/*.cs,csproj; docs/engineering/{milestones/H1.md,current-state.md,toolchain.md,capabilities.md,backlog.md,roadmap.md,findings.md}; odd/tasks/technical-autonomy-h1.md; docs/engineering/evidence/h1-0/* (native capture and reproducible local evidence only). Exact dependency Raylib-cs8.1.0, native raylib6 verification pending. Acceptance native adapter seam failure/thread/reservation/lifetime/focus tests, public consumer --smoke/--fail-update/invalid args, actual Windows native start/stop + package/runtime/license/hash evidence, unchanged H0/Loop regressions and independent implementation verification. Commands serial restore/test Desktop.Contracts, build FirstPlayable, dotnet run --project examples/FirstPlayable -c Release --no-build -- --smoke 60 and --smoke 60 --fail-update; invalid argument exit2. Expected stopped0/failed1/usage2, no hanging process. Native unavailable means blocked applicable proof, never fabricated pass. Subjective feel/readability remains owner gate, not automated technical acceptance. Risk native dependency/global affinity/public contract; independent verifier. Rollback only new Desktop/consumer/seam tests/docs, no destructive unrelated edits. Pending until ME-009 evidence.

ME-008 closure2026-10-15: independent design PASS after one correction batch and targeted recheck. Accepted ADR-0003/H1-R1 by delegated technical authority; exact paths/signatures/runner/failure/native evidence requirements documented. Runtime N/A at design gate, no implementation success inferred.

ME-009 claim2026-10-15: ready -> in-progress, Codex root, baseline4d71b6e. H1-R1 exact packet; parent inline writer, independent verifier after own RED/GREEN.

ME-009 own verification2026-10-15: RED20failed/1passed runnable tests against stubs; GREEN21passed/0failed/0skipped, H031passed/0failed/0skipped. Serial Release commands/toolchain, SDK10.0.402. Managed source/model only, no native reference. Independent managed_runtime_verify scratch review underway, closure pending.

ME-009 closure2026-10-15: independent managed_runtime_verify scratch21/21 plus nested misuse/end/dispose probe22/22 PASS, no blockers/native dependencies; parent persisted probe and reran22/22. H031/31 unchanged. ME-010 pending -> ready after managed closure -> in-progress claimed Codex root baseline4d71b6e plus forthcoming managed commit; packet/surfaces fixed above.

ME-010 validation in progress2026-10-15: initial stub compilation warnings corrected without suppression; runnable RED8failed/1passed -> GREEN9passed/0failed/0skipped. Consumer Release build0warnings/0errors. Actual Windowsx64/NVIDIA OpenGL3.3 native smoke60frames/58updates Stopped exit0, forced update ApplicationException Failed exit1 and successful native close, unknown args exit2. Native package metadata has no managed dependencies, net8/net10 groups, Zlib license; runtime hash recorded in milestone. Independent verification still pending.

ME-010 closure2026-10-15: independent desktop_tracer_verify fresh scratch9/9, Release0warnings, native normal/failure/invalid exact exits0/1/2 and NativeSmoke.ps1 seven scenarios PASS; package/hash/license metadata and640x360 scoped capture independently checked. No reproduced blockers. H1.0 objective technical acceptance recorded by Codex root under autonomy.md; subjective feel/readability remains pending, full H1 not accepted. No technical owner-approval blocker. Source/docs/tests/consumer/evidence committed together.

## Next horizon boundary

H1.0 is a functional product-evaluation handoff. Next full-H1 candidates are static obstacle/spatial interaction and animation/content, not Zelda/Pokemon implementation yet. No ready construction task exists without a refined contract; technical preparation/approval remains delegated and may resume without owner permission. Current handoff asks only for indispensable subjective baseline movement/jump/readability feedback, not repeat technical checks. Do not invent an owner technical-approval dependency or silently accept full H1.

## ME-011 — Honor stop at platform presentation boundaries

Claim2026-10-15 Codex root baseline0dfb2ce; current own acceptance audit reproduced RED2fail for supplied platform requesting retained FrameControl.Stop during Begin or Canvas getter: draw still ran afterward. Same H1-R1 stop criterion, no requirement change. Authority autonomy.md/ADR-0003. Scope engine/Loop/FrameLoop.cs, checks/Loop.Contracts/LoopTests.cs and linked evidence/backlog/odd log only. Acceptance suppress later Canvas/draw after stop while End/Dispose exactly once; existing24managed tests and31H0 regressions retained. Runner same Loop.Contracts plus focused --filter StopInsidePlatformPresentationBoundary. Risk shared public loop contract; independent targeted scratch verification before closure. Rollback only scoped stop checks/test, no H0/native/user edits. ME-010 final closure held for seam fix; earlier native PASS remains valid historical evidence.

ME-011 closure2026-10-15: independent managed_runtime_verify reproduced baseline2/2fail and fresh current24/24pass, no remaining blocker. Parent rebuilt native consumer/harnessPASS then Desktop9/H031pass; ME-010 final technical acceptance restored after seam correction,64tests total. No human subjective acceptance inferred.

## ME-012 — Evaluate H1.0 baseline experience

Authority: reserved subjective product gate in autonomy.md. Executor: owner, no agent claim; technical prerequisites completed ME-010/011, not awaiting technical approval. Scope: evaluate movement/jump response and primitive player/floor readability in the delivered FirstPlayable revision; record feedback in milestone H1.md/findings.md, no engine change. Blocker: indispensable subjective experience cannot be established by automated correctness tests. Do not request replay of technical smoke/failure/usage cases. Acceptance: explicit feedback or product acceptance tied to delivered revision, no inference by agent. Does not block unrelated technical work by default; this active horizon has no other ready task. Next refinement input is F-010, not a pre-approved distant capability list.

Final source/evidence commit c540bcd5e6650f7765d2df7158e0e3691f55fc83; stop fix43c8df9a9984db8d7c718ab179e6428a01e3aee7. Complete local ledger: evidence/h1-0/README.md.

## ME-013 — Formalize implementation-agnostic architectural directive

Claim2026-10-15: Codex root, baseline e5df4fe59cc1cf6cac8daa671819d2caf34d02bd. Authority: explicit owner directive, documentation/design only; no engine implementation or new research phase. Scope AGENTS.md; docs/engineering/{manifesto.md,requirements.md,README.md,execution.md,document-policy.md,backlog.md,current-state.md,findings.md}; decisions/{0004-replaceable-infrastructure.md,0003-next-validation-tracer.md,README.md}; specs/h1-runtime.md; templates/{adr.md,capability.md}. Acceptance: document existing coverage/gaps, seven restrictions and all non-goals; permanent review criterion; honest H1 boundary/coupling assessment with minimal design clarification only, no APIs/code/dependencies/platform promises or hito criteria changed. Checks relative links/status consistency/diff-check, source/project graph readback; runtime tests N/A because executable bytes unchanged. Significant architectural ADR requires independent review before recorded acceptance. Rollback own documentary deltas only; preserve unrelated .gitignore/.codegraph.

ME-013 closure2026-10-15: in-progress -> in-review -> completed. technical_authority_review independent PASS, no blockers/correction needed. ADR-0004 accepted by Codex root under owner directive/delegation; NFR-005 architectural intent approved, implementation conformance not globally claimed. Existing partial coverage documented, all seven constraints/non-goals and permanent review links present.91relative links/0missing, dependency readback/diff-check pass, executable diff from e5df4fe empty. Runtime N/A/not rerun; no new research phase/backend/exporter/API/function/acceptance change. F-011 remains unresolved implementation coupling with explicit future review trigger. Coherent local documentation commit follows; .gitignore/.codegraph excluded, no remote/main integration.

## ME-014 — Specify provider-neutral frame validation

Authority: technical autonomy, ADR-0004/F-011, current continuation request; independent from subjective ME-012. Pending -> ready -> in-progress claimed Codex root2026-10-16 baselinea75f22c. Scope docs/engineering/decisions/{0005-provider-neutral-validation.md,README.md}, specs/h1-runtime.md, backlog.md,current-state.md,findings.md; odd/tasks/provider-neutral-guards.md. Deliver exact additive guard signature/truth table/error-origin and migration contract, alternatives, H1 compatibility, source/test/evidence packet. No engine changes before independent design approval. Checks links/diff/readback; runtime N/A design. Review significant ADR independently before delegated acceptance. Rollback only own proposals/task notes.

## ME-015 — Remove concrete friend coupling through public validation

State pending until accepted reviewed ME-014. Authority autonomy/ADR-0004 and forthcoming reviewed ADR-0005. Scope engine/Loop/Contracts.cs; new engine/Loop/FrameApi.cs; engine/Desktop/DesktopPlatform.cs; new checks/Loop.Contracts/ProviderGuardTests.cs; docs/engineering/{backlog.md,current-state.md,findings.md,specs/h1-runtime.md,decisions/0005-provider-neutral-validation.md}; odd/tasks/provider-neutral-guards.md. No dependencies/other backends/behavioral scope expansion. Acceptance exact guard tests from public-only nonfriend provider; Loop has no Desktop friend metadata; preserve all64existing tests, message/type/cause/cleanup precedence and native seven-case harness. Commands serial Loop/Desktop/Hosting Release runners and checks/Desktop.Contracts/NativeSmoke.ps1; design must define RED cases. Risk public contract origin semantics, independent scratch verifier required; no source normalization after verification except scoped review corrections/retest. Safe rollback only these own changes, no unrelated edits/native reimplementation.

ME-014 closure2026-10-16: independent push_policy_review design PASS, no blockers; clarified7Desktop guard sites migrate,1Loop-internal retained. ADR-0005/H1-R1.1 accepted by Codex root under delegation; no functional criterion relaxed. Runtime N/A at design gate. ME-015 pending -> ready after exact signature/behavior/surfaces/runner/review/rollback packet satisfied.

ME-015 claim2026-10-16: ready -> in-progress, Codex root baselinea1d4666. Reviewed exact guard contract/surfaces; parent inline writer, test-first runnable RED before guard implementation/migration, independent verifier after own checks.

ME-015 author verification2026-10-16: runnable new test RED8failed/4passed; GREEN36Loop/9Desktop/31Hosting all pass,0failed/0skipped. Consumer rebuild0warnings/0errors; NativeSmoke.ps1 seven scenariosPASS. Seven Desktop guard sites migrated,1FrameControl internal guard unchanged; Loop named friend removed; no new dependencies or backend. Independent scratch verification pending before closure.

ME-015 final scope clarification2026-10-16: ADR-0003/0004 receive only bidirectional migration links required to preserve supersession/assessment provenance, no change to their scope or historical evidence.
ME-015 closure2026-10-16: independent provider_guard_verify fresh TEMP restore/test Loop36/36,Desktop9/9,H031/31=76/76; focused12/12 subset, native seven-scenario harnessPASS with0warnings/0errors. Source equivalence check preserves7Desktop predicates/messages/order;1Loop-internal guard retained, FrameLoop/H0/native/consumer bytes unchanged. Compiled metadata has no Desktop friendship, external provider assembly uses public guard only. No blockers; F-011 resolved for this named-friend coupling, not universal backend/portability conformance. Actor Codex root, reviewed ADR-0005/H1-R1.1. Parent spotcheck and coherent source/docs/tests commit follows; normal push to authorized branch only. ME-012 subjective product feedback remains pending, no technical owner approval requested.

ME-014/015 delivery2026-10-16: contracta1d4666, implementationc59efd2; normal push to origin/feat/h1-runtime confirmed. Parent focused12/12spotcheck passed after independent verification. No remaining ready task in current horizon; ME-012 remains subjective feedback only, no global technical approval blocker. F-010 is next refinement input, not automatically approved distant functionality.

ME-012 closure2026-10-16: owner explicitly reports immediate/comfortable movement and clear player/floor, plus nonblocking near-landing jump observation and permission to continue. Criterion was feedback OR acceptance: feedback received, task completed; no invented explicit acceptance of full H1 or future buffered behavior. Current deliveredexample553a5d9 association by unchanged game/model source, user did not provide a commit hash. Observation recorded as gameplay need, not confirmed engine defect.

## ME-016 — Specify optional consumer jump buffering

Authority current owner feedback/request, autonomy, FR-002/NFR-001/002/004/005, ADR-0003/0004. Independent from broader full-H1 implementation. Pending -> ready -> in-progress claimed Codex root2026-10-16 baseline553a5d9. Surfaces docs/engineering/{specs/platformer-controller.md,backlog.md,milestones/H1.md,current-state.md,findings.md}; odd/tasks/platformer-jump-buffer.md. Deliver exact options/update/window/reset/pause semantics, test packet, buffer/coyote alternatives and rationale; source proof landing_input_map public trace. No core/API/backend changes or new research phase. Acceptance independent consumer-design review and delegated spec approval; no new permanent engine rule, disabled behavior preserved. Runtime N/A design (reproduction already observed in scratch), links/diff/readback. Rollback own spec/task evidence only.

## ME-017 — Add opt-in buffer to example controller

Pending until completed reviewed ME-016. Surfaces examples/FirstPlayable.Model/{GameModel.cs,PlatformerOptions.cs}; examples/FirstPlayable/{Program.cs,ControllerPlatform.cs}; checks/Loop.Contracts/{Loop.Contracts.csproj,JumpBufferTests.cs}; docs/engineering/{backlog.md,specs/platformer-controller.md,specs/h1-runtime.md,milestones/H1.md,current-state.md,findings.md}; odd/tasks/platformer-jump-buffer.md. No engine source/config/dependency changes. Criteria/tests per consumer spec: optional window, disabled exact landing-edge behavior, inside/exact/expired window, one-use/refresh/no-held/no-air-jump, reset and inactive/close consumer bridge. Keep all76existing tests and native seven-scenario harness; observe RED before feature implementation. Public tests serial Loop.Contracts runner, full Desktop/Hosting regressions; native harness rebuilds actual consumer. Risk shared consumer behavior, independent scratch verification mandatory. No criterion/coverage reduction, no coyote placeholder/ledge/physics feature. Rollback own option/controller/test/docs changes only; preserve unrelated .gitignore.

ME-016 closure2026-10-16: independent jump_buffer_review designPASS, no blockers. PC-1 approved by Codex root under user gameplay request/delegation. Public contact-boundary reproduction supports consumer tolerance change (fixture correction recorded in the feature log), not engine-loss diagnosis. Buffer100ms explicit sample choice/zero-disabled compatibility; coyote deferred without ledges. Runtime N/A designgate, exact tests/signatures/surfaces/review/failure/rollback supplied. ME-017 pending -> ready.

ME-017 claim2026-10-16: ready -> in-progress Codex root baseline389552c, branchfeat/platformer-jump-buffer. Approved PC-1, source/test surfaces fixed; parent test-first inline implementation, independent verification after checks.

ME-017 author evidence2026-10-16: corrected cancellation RED3failed/14passed; GREEN focused17/17, full Loop53/Desktop9/Hosting31=93passed,0failed/0skipped. NativeSmoke.ps1 seven scenariosPASS; rebuilt consumer0warnings/0errors. Engine source/dependencies unchanged; independent implementation verification pending.

ME-017 closure2026-10-16: independent jump_buffer_verify fresh TEMP53/53PASS; scratch mutation disabling queue causes corrected landing probe failure, restored1/1PASS. S1-S4 satisfied, no blockers; Program100ms/wrapper wired, default disabled, unchanged engine/movement/native-free checks. Parent93/93 and seven native cases verified above. Objective consumer technical completion only; buffered subjective feel unobserved. No ready task remains in this bounded feedback horizon; later roadmap refinement is separate from this scoped change.

ME-016/017 delivery references: specification389552c, implementation/tests/evidence bcd2b20119468234e562226ea6ecc590b3ec2ea5, branchfeat/platformer-jump-buffer. Parent final17/17spotcheckPASS. Normal push blocked before execution by runtime destination/credential authorization review; requires explicit owner confirmation. Technical completion unchanged; no main integration/PR/release.

## ME-018 — Specify static obstacle consumer slice

Claim2026-10-16 Codex root baseline81de6a9; ready -> in-progress -> in-review. Authority owner continuation after main integration, roadmap H1/F-010, FR-002, ADR-0003/0004. Scope docs/engineering/{specs/platformer-obstacles.md,backlog.md,current-state.md,roadmap.md,findings.md}; odd/tasks/h1-static-obstacles.md. Deliver PO-1 exact geometry/validation/ownership/axis/contact/buffer/sample contracts, alternatives, runnable packet and rollback. Independent scope map confirms consumer-owned static rectangle interaction is the next documented gap; no engine solver/API extraction justified yet. Acceptance independent design PASS and delegated approval before ME-019 readiness. Runtime N/A specification; links/readback/diff checks. No new product milestone acceptance.

## ME-019 — Demonstrate static obstacle interaction

Pending until ME-018 independent review/approval. Scope new examples/FirstPlayable.Model/StaticObstacle.cs; examples/FirstPlayable.Model/GameModel.cs; examples/FirstPlayable/Program.cs; new checks/Loop.Contracts/ObstacleTests.cs; docs/engineering/{specs/platformer-obstacles.md,backlog.md,current-state.md,roadmap.md,findings.md,milestones/H1.md}; odd/tasks/h1-static-obstacles.md. Exact contract/acceptance/commands in PO-1. Preserve93existing regressions plus native seven cases. Runnable RED before implementation; focused/full GREEN; independent scratch verification and parent spotcheck. Risk item3 shared consumer model; no engine/config/dependency changes. Rollback own rectangle/resolution/example/test deltas, preserve prior PC-1 and unrelated files. No coyote/animation/dynamic solver implied. No known failing baseline checks; new geometry edge cases must be pinned, not waived.

ME-018 closure2026-10-16: independent obstacle_design_review recheckPASS for support/PC-1, exact crossing predicates and deterministic validation corrections. Codex root approves PO-1 under delegated authority. Local links/diff checksPASS; runtime N/A design. ME-019 now ready with exact acceptance/commands/surfaces and independent verification gate; no obstacle code implemented yet.

ME-019 claim2026-10-16: ready -> in-progress -> in-review, Codex root baseline663d629 on feat/h1-static-obstacles. Runnable initial RED13failed/1passed of14 after fixture compilation corrected; GREEN14/14 then added raised-top expired/large-delta probe, Loop68/68 passes. Parent inline implementation; independent verifier required before closure. PO-1 exact surfaces, no engine/package changes.

ME-019 closure2026-10-16: independent obstacle_verify PASS in fresh short TEMP copy, Loop68/68;1000mixed-update comparison with actual663d629 baseline for buffer off/on matched exactly. Independent probes cover contact-launch support, nearest/directional sweeps, tunneling, corner, geometry/ownership/validation, order/duplicates and rendered collision list. Initial long scratch testhost path could not resolve executable; clean shorter scratch succeeded (environment-only limitation, no skipped proof). Parent108tests/native7PASS; no engine/project/package/dependency changes. Objective technical slice accepted by Codex root under delegation, PO-1; subjective obstacle playability unobserved. No full-H1 acceptance. Next product handoff is the bounded obstacle sample; later animation/content refinement remains candidate, not prepared work.

ME-018/019 delivery: contract663d629, implementation/tests/evidence8cd722c55bf379fe47ff2ed1735b9b98fe49100a, normal push confirmed origin/feat/h1-static-obstacles. Parent15/15focused spotcheckPASS. Main81de6a9 has previous buffered increment only. No new prepared task in the bounded obstacle horizon; meaningful subjective obstacle handoff documented in H1 packet.

ME-019 subjective follow-up2026-10-16: owner reports verbatim 'Funciona sin ningun problema' after obstacle sample handoff. Positive feedback received, no reported issues; existing technical completion unchanged. No full-H1 acceptance/new main integration inferred. Associated delivered629b201/implementation8cd722c; owner supplied no hash.

## ME-020 — Specify procedural animation slice

Authority owner continuation2026-10-16 after positive obstacle feedback, roadmap H1 CAP-008, FR-002/ADR-0003/0004. Claimed Codex root baseline1d9a5fd on feat/h1-avatar-animation; ready -> in-progress -> in-review. Scope docs/engineering/{specs/avatar-animation.md,backlog.md,current-state.md,roadmap.md,capabilities.md,milestones/H1.md}; odd/tasks/h1-avatar-animation.md. Deliver AV-1 exact simulation-time pose/gait/facing/renderer/content contract, alternatives and test packet. Independent design approval before construction, no engine/asset framework. Acceptance readback/local links/diff and independent design PASS; runtime N/A at design gate. Rollback only own draft/docs, no prior gameplay changes.

## ME-021 — Animate the external sample avatar

Pending until ME-020 reviewed delegated AV-1 approval. Exact source/test/docs surfaces, contracts, acceptance and serial runners in AV-1. Preserve108tests, no engine/GameModel/obstacle/dependency changes. Runnable RED then focused/full GREEN; native seven scenarios, independent scratch verifier and parent spotcheck mandatory before closure. Claim records baseline after spec commit. Objective technical acceptance delegated; subjective readability of new art/poses human-owned. Risk item3 presentation observable; rollback own animator/renderer/Program/link/tests/doc deltas. No coyote/texture/provider methods/full-H1 acceptance inferred.

ME-020 closure2026-10-16: independent animation_design_review PASS, no blockers; AV-1 approved by Codex root under delegated authority, no significant engine architecture change. Consumer procedural content/timing/facing/rendering contracts exact; no engine/asset/dependency/game rules. Diff/readbackPASS, runtimeN/A design. ME-021 ready.

ME-021 claim2026-10-16: ready -> in-progress -> in-review, Codex root baseline93be595 on feat/h1-avatar-animation. Exact AV-1 surfaces; runnable scaffoldRED9failed/0passed then focusedGREEN9/9 and Loop77/77. Parent inline writer, independent implementation verification required before closure. No GameModel/engine/package edits; native/full regression results follow.

ME-021 author evidence2026-10-16:117tests=77Loop+9Desktop+31Hosting pass/0failed/0skipped, focused9/9; native seven-scenario harnessPASS with consumer rebuild0warnings/0errors. No engine/gameplay/package/dependency changes. Independent verification pending, no subjective/full-H1 acceptance.

ME-021 closure2026-10-16: independent animation_verify PASS fresh shortTEMP77/77 plus independent timing/overflow/atomicity/phase/reset/instance/render-purity/exact-content probes. Consumer build0warnings/0errors. Hash comparison:11engine files and GameModel/StaticObstacle/PlatformerOptions exact baseline, no new engine files. Parent117tests/native sevenPASS; local links/diff checksPASS. Codex root accepts objective AV-1 technical slice under delegation. No skipped required proof or reproduced blockers. New presentation subjective feedback remains unobserved; no full-H1 acceptance. No independent ready task remains in this bounded animation horizon; further sprite/assets/clip contracts require concrete refinement, not implied by this demonstration.

ME-020/021 delivery2026-10-16: contract93be595, implementation/tests/evidence1206951c93db601eaa2cda7b01ff8a9f79b2bcca normal push confirmed origin/feat/h1-avatar-animation, parent focused9/9spotcheckPASS. No main integration/PR/release; subjective AV-1 pose/readability feedback unobserved, no independent ready task in current bounded horizon.

ME-021 owner follow-up2026-10-16: "Funciona sin ningun problema, dale con lo siguiente". Positive AV-1 feedback associated4081247/1206951 (no user hash), not exhaustive/full-H1 acceptance or merge permission. No code changed by feedback.

## ME-022 — Specify independent H2 exploration tracer

Claim2026-10-16 Codex root baseline4081247, feat/h2-room-explorer, ready -> in-progress -> in-review. Authority current owner continuation, roadmap H2/FR-002/ADR-0004/autonomy. Scope RE-1 and linked engineering backlog/current-state/roadmap/H1/H2/capabilities/contributing; odd/tasks/h2-room-explorer.md. Acceptance exact consumer movement/topology/transition/input adapter/CLI/project/test/rollback contract reviewed independently then technically approved. No engine scene/map/resource API; no new strategic product scope. Runtime N/A design, links/diff/readback. Independent scope map confirms H2 preparation is next bounded need, H1 subjective acceptance not inferred.

## ME-023 — Expose additive Desktop vertical actions

Pending ME-022 accepted RE-1. Surfaces new engine/Desktop/DesktopActions.cs, checks/Desktop.Contracts/DesktopActionTests.cs; engine/Desktop/RaylibApi.cs plus RE-1/backlog/current-state/odd task evidence. Exact mapping/order/query/legacy/alias behavior in RE-1. Runnable RED then focused/full Desktop tests and existing native harness; independent scratch verification before closure (may cover whole RE-1 feature after both work units, before objective acceptance). No public contract/dependency/focus/lifecycle changes; safe rollback own helper and additive up/down mapping. Risk3shared mapping, independent verifier required. No baseline failures.

## ME-024 — Validate two-room exploration through public engine API

Pending ME-023 and reviewed RE-1. Exact new model/executable/tests/smoke and Loop.Contracts reference/doc surfaces in RE-1; no FirstPlayable/model changes. Acceptance all model/transition/input/native/CLI criteria, preserve117baseline tests, independent scratch verification and parent spotcheck, commit/push traceability, objective technical acceptance vs unobserved subjective navigation. RED/GREEN commands and recovery in RE-1. Risk3/5 consumer contracts/project wiring; no engine world/ECS/asset APIs. No baseline failures, no new package versions.

ME-022 closure2026-10-16: independent explorer_design_review targeted recheckPASS after contradictory unchanged-code wording corrected. RE-1 approved by Codex root under delegated authority. No runtime needed for design; links/readback/diff checks. ME-023ready; ME-024 remains pending independent input closure, do not bypass dependency.

ME-023 claimed Codex root2026-10-16 baselinebf86152, ready -> in-progress -> in-review. Runnable scaffoldRED11failed/0passed; new+legacyDesktop GREEN20/20. Initial direct method-group conversion failed compilation because Raylib returnsCBool; bool-targeted lambdas corrected binding without dependency/public API change. Native existing-consumer harness checked before independent closure. ME-024 remains pending.

ME-023 closure2026-10-17: independent vertical_input_verify_resume fresh TEMP20/20 plus3independent mapping/query/order probesPASS, no blockers or workspace edits. Previous verifier ended without verdict due unavailable capacity, never treated as PASS. Author128tests=77Loop+20Desktop+31Hosting and existing native sevenPASS. No focus/lifecycle/time/publicAPI change. ME-024 dependency satisfied ->ready. Parent focused spotcheck before commit.

ME-024 claim2026-10-17: ready -> in-progress -> in-review, Codex root baseline2dfccbd. Runnable model scaffoldRED8failed/0passed; GREEN8/8 after exact public float-boundary fixture correction (Y120 not reachable with the original single-update setup; alternate public boundary origins used, no criterion weakened/private mutation). Author136tests=85Loop+20Desktop+31Hosting pass/0failed/0skipped, new native seven scenariosPASS with0warnings/0errors. Old platformer native harness rerun separately. Independent consumer verification pending; no subjective navigation acceptance claimed. Delivery forecast grows beyond400 with new projects/tests/harness, review-chain preference requested asynchronously before next commit; no PR/merge authorization inferred.

ME-024 closure2026-10-17: independent room_explorer_verify fresh shortTEMP85/85 plus independent public movement/both-portal boundaries/atomicity/MaxValue/spawn/overshoot/replay probesPASS, new native seven scenarios independentlyPASS, build0warnings/0errors. Initial verifier-only west-edge setup was corrected without product changes; no product blockers. Parent136tests and both native seven-case harnessesPASS. Persisted west inclusive/reject boundary probes into existing public test rows; final parent focused8/8PASS. Loop/Hosting/FirstPlayable source hashes unchanged vs2dfccbd; model references only Loop, no platformer dependency. Codex root accepts objective RE-1 tracer under delegation, not full-H2 or subjective navigation. No independent ready task remains in this bounded horizon; actual user navigation/transition readability is the meaningful next handoff.

ME-022/023/024 delivery2026-10-17: contractbf86152, input2dfccbd at origin/feat/h2-vertical-input, consumer/tests/evidence2ce5175136cbf06928d5096084f32cec79da1b8f at origin/feat/h2-room-explorer; normal pushes confirmed. Stacked-to-main slices176input/contract and319consumer lines (before ledger), prior unmerged H1 slices explicit prerequisites in odd/tasks/h2-room-explorer.md. No PR/main integration. Manual H2.0 handoff in milestones/H2.md, subjective navigation unobserved.

ME-024 owner follow-up2026-10-17: verbatim 'Funciona perfectamente' after RE-1 handoff. Positive subjective exploration-tracer feedback recorded against delivered02f048f/implementation2ce5175 (owner supplied no hash); no reported issues. Completed task state/technical evidence unchanged; no full-H2 acceptance or main integration inferred.

## ME-025 — Integrate evaluated increments after positive owner feedback

Owner explicit instruction2026-10-17, Codex root claim baseline9db127f on docs/positive-feedback-integration, ready -> in-progress -> in-review. Scope AGENTS.md; docs/engineering/{autonomy.md,execution.md,milestone-delivery.md,templates/task.md,current-state.md,backlog.md}; odd/tasks/positive-feedback-integration.md. Actual existing main integration9db127f/push completed. Acceptance exact continuing positive-feedback scope/destination/authentication/verification/preservation/idempotency/record/continuation rules; independent policy review and links/diff/source-integrity checks before closure. RuntimeN/A documentary authority, not new code. Risk5delivery/authority; no relaxed technical checks, full milestone acceptance, PR/release/force or unrelated work. Rollback only own policy/doc changes, never already published history/unrelated files. Next product work separately scoped after roadmap map.

ME-025 closure2026-10-17: independent positive_integration_policy_review targeted recheckPASS after current-state active-summary correction. AGENTS/autonomy/execution/delivery template synchronized with explicit standing owner authority; no technical acceptance/review/scope safeguards reduced. Links/diff readback/source-integrity checks, runtimeN/A unchanged code. Existing main9db127f normal-pushed,0/0. Policy commit/main delivery authorized by current explicit instruction; later new product increments still need their own positive owner feedback before main delivery.

ME-025 delivery2026-10-17: policy commit8ff1f0a independently reviewedPASS, fast-forwarded/pushed main after existing source9db127f;0/0remote divergence. Current explicit policy/integration instruction satisfied; new product branch feat/h2-marker-interaction has no automatic main authorization until positively evaluated.

## ME-026 — Specify manual marker interaction probe

Owner continuation2026-10-17, C-002/H2/FR-002/ADR-0004. Claimed Codex root baseline8ff1f0a, ready -> in-progress -> in-review. Exact surfaces IM-1 linked spec/backlog/current-state/roadmap/H2/capabilities and odd task file; no source before review. Deliver precise consumer-only pressed-edge eligibility/toggle/event/trace/shared-content/validation rules, baseline preservation, tests/commands/rollback. Independent design PASS then delegated approval before ME-027ready; runtimeN/A design, links/readback/diff checks. No NPC/dialogue/input/engine infrastructure scope.

## ME-027 — Demonstrate room-local manual interaction

Pending approved reviewed ME-026. Exact source/tests/docs surfaces, semantics and runners in IM-1. Keep136baseline tests and existing movement/room/CLI behavior; public RED then focused/full GREEN, both native harnesses, independent scratch verifier with actual baseline trace comparison and parent spotcheck. Risk3shared consumer behavior; no engine/project/dependency changes. Technical acceptance delegated, subjective discoverability/readability owner-owned; positive feedback later authorizes evaluated increment main integration, not unrelated or full-H2 scope. Rollback own marker deltas only. No known baseline failures.

ME-026 closure2026-10-17: independent marker_design_reviewPASS no blockers; IM-1 approved by Codex root under technical delegation. Exact final-overlap/edge/toggle/event/validation/render/trace/room retention contracts and runnable packet, no engine abstraction. RuntimeN/A design, diff/readback; ME-027ready.

ME-027 execution record2026-10-17: Codex root, baseline4419243, feat/h2-marker-interaction, ready -> in-progress -> in-review. Parent exclusively owned source/tests; explicit claim bookkeeping recorded late after initial RED/GREEN, not represented as an earlier write. Scope remained approved IM-1/no competing writer. Runnable RED8failed/5passed of13 ->GREEN13/13, Loop98/98/Desktop20/20/H031/31 (149total; harness results follow). Independent implementation verification required before closure; no engine/project/dependency edits.

ME-027 closure2026-10-17: independent marker_verify freshTEMP98/98 plus strict boundary/corner/coalescing/held/case/invalid-state/order/transition/no latent probesPASS;1000updates against actual4419243 baseline room/X/Y identical. Program constants/room/colors/order/fixed-update lowercase trace verified. Independent RoomExplorer native sevenPASS/build0warnings/0errors. Parent149tests/both native seven-case harnessesPASS, links/diff checksPASS. Engine/FirstPlayable/project/package/dependency bytes unchanged. Codex root accepts objective IM-1 under delegation; subjective marker discoverability/response unobserved, no full-H2 acceptance. No independent ready task remains in this bounded horizon; positive owner feedback after handoff will authorize normal main integration under policy, not prior feedback on older source.

ME-026/027 delivery2026-10-17: contract4419243, source/tests/evidenceb21266f9ecd6ffdcf79870857d4705547a309468 normal push confirmed origin/feat/h2-marker-interaction, parent13/13focused spotcheckPASS. Main8ff1f0a already includes prior evaluated work/policy, no new marker integration yet. Next positive owner feedback on this verified increment authorizes normal integration/push without another routine permission question. H2 manual interaction handoff documents only subjective experience, not repeated technical checks.

ME-027 owner feedback2026-10-17: 'Funciona perfectamente', associated0a8bfe3/b21266f marker delivery. Positive subjective feedback recorded; existing technical closure unchanged. Standing owner policy authorizes normal main integration/push of this evaluated scope; no full-H2 acceptance or unrelated work inferred.

ME-027 integration2026-10-17: positive owner feedback triggered normal main fast-forward/push8ff1f0a..de1c08d,0/0remote divergence; source engine/examples/checks identical to evaluated0a8bfe3. No new code or repeated operational checks/full-H2 acceptance. Unrelated local work preserved.

## ME-028 — Specify first menu/turn consumer

Owner continuation2026-10-17, H3/C-003/FR-002/ADR-0004. Claimed Codex root baseline827171d on feat/h3-turn-menu, ready -> in-progress -> in-review. Deliver TM-1 precise consumer composition/open/pause/ordered selection/confirmation/phase/return/render/trace/project/CLI/test/rollback contract and independent review before approval. Exact docs/task surfaces in TM-1. No engine UI/text/combat/save API. RuntimeN/A design; links/readback/diff and independent design PASS required. Consumer-first alternative avoids speculative optional text contract before actual label need.

## ME-029 — Demonstrate exploration/menu/turn flow

Pending reviewed ME-028 approval. Exact new model/executable/test/harness/project-ref and docs surfaces/commands/acceptance in TM-1. Preserve149baseline tests and all prior source dependencies/behaviors, observe RED/GREEN; independent whole-feature scratch verifier with public ordered-edge/phase/pause/resume probes and source baseline comparison, parent spotcheck, both old native harnesses plus new one. Risk3/5 new consumer contract/project graph; no engine/old-game code edits. Managed model/tests and executable/native evidence are coherent review slices under cached stacked-to-main, no source compression/criteria reduction. Objective technical closure delegated; new positive owner feedback needed for main delivery; no full-H3 acceptance.

ME-028 closure2026-10-17: independent turn_menu_design_review targeted visibility/test-seam recheckPASS. TM-1 approved by Codex root under technical delegation; phasepipsalways/menuonlyopen and native-free renderer proof exact. No engine/UI/text/battle API decision. RuntimeN/A design, links/readback/diff checks; ME-029ready. Existing backlog heading refreshed to reflect current horizons rather than historical H1 preparation.

ME-029 claim2026-10-17: Codex root baselinecf92075 on feat/h3-turn-menu, ready -> in-progress before source edits. Approved TM-1 exact surfaces/acceptance; parent inline writer. Commit slices managed model/tests then executable/renderer/native proof; whole-feature independent review before closure, no task dependency bypass.

ME-029 managed slice2026-10-17 baselinecf92075: runnableRED6failed/0passed ->focusedGREEN6/6, fullLoop104/104. New private-composition model and public ordered-menu/pause/phase/invalid/replay tests only; no previous model/engine source changes. Task remains in-progress, native presentation and whole-feature independent verification pending before closure.

ME-029 presentation slice2026-10-17: actual-source renderer scaffoldRED2failed/0passed ->combined menu/model/renderGREEN8/8, Loop106/106. New executable shares public content and renderer, no prior source edits. Whole-feature remains in-review until independent verification; managed0a93f9a is a review boundary, not technical acceptance.

ME-029 author final2026-10-17:157tests=106Loop+20Desktop+31Hosting pass/0failed/0skipped, focused8/8; all three native seven-case harnessesPASS and builds0warnings/0errors. Whole-feature independent turn_menu_verify pending; no source changes to engine/old examples, no package-version changes, no new main delivery yet.

ME-029 closure2026-10-17: independent turn_menu_verify whole candidate(cf92075 baseline, managed0a93f9a plus renderer/executable overlay) freshTEMP157/157 and2450independent assertionsPASS. Ordered edges/duplicates/case/held/single confirmation/stale occurrence/Return/reopen/phase/invalid-state/MaxValue/isolation/baselineExplorer and exact renderer4/9/4 geometry/colors/purity/null checks verified. New native harness normal/failure/five invalid scenariosPASS with exits0/1/2/native close/build0warnings/0errors. All8candidate source/project/test/harness files matched tested snapshot hashes;25prior engine/examples files unchanged. Parent157tests/all three native harnessesPASS; no blockers/skipped required proof. Codex root accepts objective TM-1 under delegation, not subjective menu usability/full-H3 acceptance. No independent ready task in bounded horizon; new positive owner feedback later authorizes this evaluated increment main integration.

ME-029 delivery2026-10-17: contractcf92075, managed0a93f9a and presentationb8ac239630eece0112614ebe178e05cb16a09fc7 published to origin task branches feat/h3-turn-model and feat/h3-turn-menu. Parent final focused8/8 and diffcheckPASS. Main remains827171d; no PR/main integration or full-H3 acceptance. Subjective menu feedback is the next meaningful product validation, not a request to repeat automated checks.

ME-029 owner feedback2026-10-17: "Funciona perfectamente" on delivered733b9b8. Bounded TM-1 experience positively evaluated; existing technical proof retained, no source or acceptance-criteria changes. Standing policy authorizes normal main integration/push of this scope, not full-H3 acceptance. Fetch confirmed local/remote main827171d synchronized and ancestor of evaluated HEAD; unrelated .gitignore/.codegraph preserved.

ME-029 integration2026-10-17: main/origin main fast-forward827171d..bb32d15 and push succeeded,0/0 divergence. Engine/examples/checks comparison against evaluated733b9b8 exit0 (identical); no redundant runtime rerun for passive feedback documentation. Independent next_after_turn_map readback found no eligible task or concrete need for new UI/text/storage/battle scope. CAP-012/013 remain discovery candidates; full-H3 acceptance not inferred. Unrelated local edits preserved.

## ME-030 / ME-031 — Consumer checkpoint horizon

ME-030 P1 in-progress Codex root baseline751686a, owner continuation2026-10-17, C-003/CAP-013/ADR-0004. CP-1/ADR-0006 packets define exact surfaces/contracts/criteria; independent design review before approval. ME-031 pending ME-030, no source construction yet. No engine storage/file IO/native changes or full-H3 acceptance.

ME-030 completed2026-10-17: CP-1/ADR-0006 independent checkpoint_design_review PASS/no blockers, Codex root delegated approval. RuntimeN/A design; diff/readback checks. ME-031 ready, claimed in-progress before source construction under approved CP-1.

ME-031 author proof2026-10-17: checkpointRED4failed/0passed ->GREEN4/4; full110Loop+20Desktop+31Hosting=161passed/0failed/0skipped. All three existing native seven-case harnessesPASS exact0/1/2/cleanup; no engine/executable/native/dependency changes. Independent checkpoint_verify whole-feature scratch proof pending; task in-review, not completed.

ME-031 blocker2026-10-17: owner resumed 'continua'. Independent checkpoint_verify PARTIAL, not PASS. FreshGUIDTEMP copy/all5source hashes matched implementationa4dc7a1; static contract checks and unchanged prior Update bodies/project dependencies corroborated. Restore hung twice (normal then bounded local-cache-only/no-audit retry), terminated; independent probes/full suites NOT executed. Author final4/4spotcheck/161regressions/three native harnesses remain valid but do not replace independent execution. Taskblocked, no weakened criteria/technical acceptance/main integration. No independent ready task in CP-1 horizon; recovery requires an executable isolated verifier environment, not owner repetition of tests.

ME-031 recovery/closure2026-10-17: owner 'Que debo hacer o lo haces tu?' resumed technical recovery, not waiver of proof. Outside-sandbox dotnet10.0.402/local-cache-only restore with NuGetAudit=false/serial/no-shared-compilation/no-node-reuse exited0 promptly. Independent checkpoint_verify freshGUIDTEMP copy (no git/index/bin/obj reuse)4/4public checkpoint tests and110Loop+20Desktop+31Hosting=161/161PASS, no blockers/advisories; all5candidate hashes matched before/after/current implementationa4dc7a1. Existing Update bodies/project dependencies retained. Earlier PARTIAL is historical, now resolved execution-context limitation, no code/SDK/package change. Parent final4/4spotcheckPASS, source parity/diffcheckPASS. Codex root accepts objective CP-1, ME-031completed; no subjective test needed for byte contract, main751686a unchanged.

## ME-032 / ME-033 — Checkpoint file horizon

ME-032 completed2026-10-17 at5d6f2c3: CF-1/ADR-0007 reviewed checkpoint_design_review PASS/no blockers, Codex root delegated approval. Advisory preserving write+close errors incorporated before source. ME-033 claimed ready->in-progress before construction, baseline3e77a8d; focusedRED5failed/0passed ->GREEN5/5, no existing model/engine/native graph change. Full regressions/independent verification required; in-review, no technical acceptance yet.

ME-033 closure2026-10-17 implementation7232471: independent checkpoint_verify freshGUIDTEMP/no prior build/index reuse, local-cache/outside-sandbox proofPASS. Independent16public checks (relative path/exact bytes/round-trip/no overwrite/temp ownership/locked read/large malformed handle release/directory target/missing parent), focused5/5 and full115Loop+20Desktop+31Hosting=166/166PASS. Four source/project/test hashes match before/after/current; Storage references only consumer model, no packages/Desktop/native. Prior engine/models/executables/Desktop/Hosting source diff empty. Parent final5/5/frozen-sourceparity/diff/link checksPASS; no blockers/advisories. Compound write/close/cleanup failure branches static-reviewed only, explicitly not fault-injected per CF-1. Codex root accepts objective CF-1; ME-033completed, no subjective gate/main integration/full-H3 acceptance. Existing native evidence retained on unchanged runtime sources.

## ME-034 / ME-035 — Checkpoint launch integration

ME-034completed2026-10-17 contract554d4c6: CL-1 independent checkpoint_design_review PASS, Codex root delegated approval before source. ME-035claimed ready->in-progress baselinebb75223. Internal source-linked parser RED3failed->GREEN3/3, Loop118/118; oldflag grammar retained. Parser slice does not change executable yet; native integration/RED/GREEN/full independent proof required before closure.

ME-035 integration proof2026-10-17: parsercced7fa preserved old grammar; new process harnessREDusage exit2 for --save-new before Program integration ->GREEN14cases. Parentfull118Loop+20Desktop+31Hosting=169PASS, build0warnings/0errors. Opt-in --load before native/no fallback; --save-new after nonfailed loop cleanup only, preserve existing destinations, failuresexit1/invalidusage2. Independent wholefeature/native regression pending, taskin-review.

ME-035 closure2026-10-17: initial independent checkpoint_verify169/169/old3native/new14cases/7independent process probesPASS, but found one genuine new-effect blocker in parser args[++i] after invalid smoke numeric value. Initial79bf05c FAIL preserved, no false acceptance. Single correctionb768c13: atomic lookahead, regressionRED1failed2passed ->GREEN3/3, parentLoop118/118/newnative17PASS. Independent freshGUIDTEMP targeted recheck ONLY: corrected3file hashes match, parser3/3/native17/Releasebuild0warnings0errorsPASS, no blockers/advisories, unchanged-component initial proof retained (not a second full sweep). Parentfinal3/3/sourceparity/diff/linkPASS. Codex root accepts objective CL-1, ME-035completed; no engine/model/storage/game-update/render changes, no SDK/package changes or subjective test needed. Main751686a unchanged.

## ME-036 — Synchronize current onboarding

P1 DOC-001/NFR-004, owner continuation2026-10-17, baselinecb038c8. Claimed Codex root ready->in-progress on docs/current-onboarding. Read-only next_after_turn_map found engineering README still presenting H1 preparation/current2026-10-15, contributing hardcoded feat/h1-runtime and duplicated superseded delivery restrictions. Scope only engineering/README.md, contributing.md, current-state.md current summary, backlog.md; preserve historical dated evidence and existing autonomy policy. Deliver current verified H0/H1/H2/H3/CP1/CF1/CL1 pointers, distinguish task branches/main and technical/product acceptance; no new product/permission decision. Check relative links/readback/status consistency/git diff/source parity; runtimeN/A passive documentation. One bounded independent authority-consistency readback for mirrored delivery guidance, no code/SDK/dependency changes or artificial runtime rerun. Rollback own documentary edits only, preserve unrelated files. No new ADR/spec/ODD feature artifact required for this understood correction.

ME-036 closure2026-10-17: four-document status/authority synchronization complete, independent checkpoint_design_review PASS/no blockers against unchanged autonomy.md. Local-link targets/readback/status consistency/git diffPASS; engine/examples/checks/global.json/NuGet.config identical to baselinecb038c8. Runtime/tests N/A documentation-only, no redundant rerun. Main751686a untouched, latest verified behavior remains CL-1cb038c8; no fullmilestone/newpermission/feature approval. Unrelated .gitignore/.codegraph preserved; no ready implementation task created from speculative text/UI needs. Repository evidence retained while runtime memory registration unavailable.

## ME-037 / ME-038 — Optional in-window labels

ME-037completed2026-10-17 contract6df157c/ADR-0008 accepted by Codex root after checkpoint_design_review PASS. Concrete CAP-004/012 menu names, not retroactive TM-1 defect or broad text/UI promise. ME-038claimed before source at373efa5, parentinline, DesktopRED4->GREEN4, nativeREDNotImplemented->GREEN444glyphRGBpixels/black0/cleanclose, consumerRED2->GREEN2. Full175tests/0failed/0skip; sourcefreeze in-review pending independent wholefeature proof. Exact NL-1 edit surfaces/contracts, no package/SDK changes; primitive Fill fallback4/9/4 preserved.

ME-038 closure2026-10-17: independent checkpoint_verify freshGUIDTEMP/no build/index reuse175/175=120Loop+24Desktop+31Hosting, focused6/6,14independent public provider/renderer checks, native coordinate extremes/minmaxsizes/empty probePASS. LabelNativeSmoke blackRGB0/glyphRGB444/cleanclose, all4old native harnesses (persistence17included)PASS, Release4consumers/probe0warnings0errors. Ten frozen source/project/test/harness hashes match before/after/currenta9e3236; project/package dependencies unchanged, Raylib confined to Desktop and native testprobe, prior othergames/models/storage/CLI unchanged. Parentfinal6/6/sourceparity/diff/linkPASS, no blockers/advisories. Codex root accepts objective NL-1, ME-038completed; optional subjective readability not claimed/fullH3 not accepted/main751686a untouched. Source commits d404f13/a9e3236 coherent stack slices, no PR. Runtime identity unavailable; repository records evidence, unrelated local work preserved.

ME-038 owner feedback2026-10-17: 'Funciona perfectamente' on delivered1dce7b3/a9e3236 NL-1. Bounded label presentation positively evaluated; no fullH3/milestone acceptance or new feature scope. Fetch confirmed main/origin751686a0/0 and ancestor of evaluatedHEAD. Integration range contains only reviewed verified CP-1/CF-1/CL-1/onboarding/NL-1 units; latest independent175/old4native/nativeglyph/provider checks cover full evaluated source stack. Standing policy authorizes normal fast-forward/push of that scope, no force/PR/release. Unrelated .gitignore/.codegraph excluded, no source changes or redundant runtime rerun for feedback docs.

ME-038 integration2026-10-17: main/origin main fast-forward751686a..01c9ea5 and normal push succeeded,0/0 divergence. Entire evaluated source/configuration matches1dce7b3 (engine/examples/checks/global.json/NuGet.config diff exit0); no new runtime verification required for unchanged reviewed bytes. Existing175/native/provider/cleanup evidence retained; fullH3 acceptance not inferred. Backlog table38tasks allcompleted, no ready/active independent task; no speculative scope added during delivery. Unrelated .gitignore/.codegraph preserved.

## ME-039 / ME-040 — Optional image resource closure

ME-039 completed at17c2a53: independent checkpoint_design_review PASS preceded delegated approval of ADR-0009/IMG-1. ME-040 implementation a1f6a9c (image values/public profile/context-owned leases/Desktop/native proof), 5e3d54e (badge/fallback/tests/docs), normalized candidateed79408. Observed author RED7 resource cases, RED2 consumer cases, native RED NotImplementedException; GREEN185/185=122Loop+32Desktop+31Hosting. Additional classification test was introduced after implementation and passed directly; no RED claim for it.

Independent checkpoint_verify PASS/no blockers/advisories on frozened79408 in fresh GUID system-TEMP source copy with no build/index reuse. Full185/185, focused10/10, all6native harnesses (image; labels444glyph pixels; persistence17cases; Turn7; Room7; Desktop7),14independent public/native probes. Five fresh Release builds0warnings/0errors;15candidate source/project/harness hashes matched before/after. Existing models/storage/AvatarRenderer/package/SDK/config unchanged againstba815c0. Parent image native spotcheckPASS and git diff --checkPASS. Actual OOM/compound native cleanup failures received static review only, not injected runtime proof. RDD globaloff unchanged; no native RDD started. Codex root accepts objective IMG-1 and completes ME-040 under delegation. Subjective badge appearance is not claimed accepted; no fullH1/othermilestone acceptance or main integration inferred. Pre-existing .gitignore/.codegraph preserved. Next horizon remains candidate content/animation needs, not an automatically approved asset manager, decoder or battle system.

IMG-1 owner feedback/integration2026-10-17: owner reports "Funciona perfectamente, continua" against delivered87a599e. Positive bounded presentation feedback recorded; existing standing policy authorizes normal integration/push of evaluated verified source. Main fast-forwardedba815c0..87a599e and pushed to authorized origin,0/0divergence. Engine/examples/checks/config parity with delivered87a599e passed; no changed bytes require rerunning185tests. No fullH1/H2/H3 acceptance, unrelated scope or release inferred. .gitignore/.codegraph preserved.

## ME-041 / ME-042 — SPR-1 closure2026-10-17

ME-041 completed468680e: independent checkpoint_design_review PASS before Codex root approval of SPR-1/ADR-0010. ME-042 provider/source-region/native proof52e2acd and consumer sheet/fallback/tests/docs552f468. Author observed RED4region/RED4consumer/nativeREDNotImplemented ->GREEN193/193=126Loop+36Desktop+31Hosting plus real region orientation/point/alpha readback. Existing image, label, resource budgets and cleanup contracts unchanged.

Independent checkpoint_verify PASS S1/S2/S3, no change-caused blockers, frozen552f468 in fresh GUID system-TEMP source copy excluding Git/index/build artifacts. Focused8/8, full193/193,7native harnesses PASS (Region/Image/Label/TurnPersistence17/Turn/Room/Desktop), fresh Release builds6examples/probes. Independent managed pixel comparison all8avatar cells and native pixel-identical old/new rendering for all8integer poses plus representative fractional position passed. Old AvatarAnimation/AvatarRenderer/AvatarImage/game models/storage/package/config unchanged. All15candidate source/project/harness hashes matched workspace after checks; parent native Region spotcheck and git diff/link checksPASS. Two owned scratch dotnet hosts lingered after successful logs; reviewer terminated only identified scratch PIDs, with no result/workspace loss. Extreme native OOM remains static-only unchanged limitation. RDDglobaloff unchanged; native RDD not run.

Codex root accepts objective SPR-1 and completes ME-042. No subjective/fullH1/H2/H3 acceptance fabricated; output equivalence is automatic evidence, not an obligation to repeat manual tests. Main/origin/main7507858 retains positively evaluated IMG-1 only; new source-region increment remains on feat/sprite-regions until applicable integration authority. Pre-existing .gitignore/.codegraph preserved. No independent ready task remains; refine a concrete next roadmap need rather than automatically creating generic clip/asset/atlas/decoder or encounter systems.
