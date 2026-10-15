# Executable backlog

Single task-state source of truth. Dated task records distinguish preparation from implementation.
Priority P0 precedes P1; ID ascending breaks ties. Completion is recorded only with task evidence.
Read [execution](execution.md), [autonomy](autonomy.md) and the [task template](templates/task.md).

## Accepted H0 and current H1 preparation horizon

| ID | Kind | Priority | State | Dependencies | Packet |
| --- | --- | --- | --- | --- | --- |
| ME-001 | investigation | P0 | completed | none | Below |
| ME-002 | specification | P0 | completed | none | Below |
| ME-003 | decision preparation | P0 | completed | ME-001, ME-002 | Below |
| ME-004 | implementation | P1 | completed | ME-003; owner approval | Below |
| ME-005 | specification | P0 | completed | H0 acceptance | Below |
| ME-006 | investigation | P0 | completed | H0 acceptance | Below |
| ME-007 | decision preparation | P0 | completed | ME-005, ME-006 | Below |
| ME-008 | specification | P1 | blocked | ME-007; owner H1 scope authority | Below |

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

- 2026-10-15: in-progress -> in-review -> completed after independent h1_preparation_verify design readback. One correction batch repaired ADR table seam and H1 CAP-002 mapping; targeted recheck passed. Evidence: decisions/0003-next-validation-tracer.md, register, roadmap/current-state; runtime N/A. ADR remains proposed; ME-008 blocked on owner scope/dependency authority. No ready independent task remains.

## ME-008 — Finalize H1.0 technical construction packet

- State: blocked, not executable. Depends on completed ME-007 and owner approval of the new scope/dependency authority. No H1 implementation task is currently ready.
- Required before ready: accepted bounded scenario, delegated native dependency authority, exact source/project surfaces, public signatures/ownership/input-time semantics, package/native version and license/deployment validation plan, Windows environment and commands, deterministic/public-consumer checks, independent review/rollback.
- Intended scope after authority: finalize only that packet; implementation requires separate complete ready task(s). No library/demo/backend may be improvised from this blocked outline.
- Current missing decision: whether to introduce a native window/input/presentation validation increment next, or stay headless for CAP-002 first. Both exceed accepted H0 behavior.
- No allowed construction paths/runner are fabricated before that decision. H1 remains candidate, not accepted.

## Updates

Record dated transition reason, executor/reviewer, baseline/change locator, checks and
findings under each packet. Preserve past results. Agents may prepare tasks objectively,
not approve contracts. Add only concrete next-horizon work; future catalog rows are not tasks.

Delivery ledger2026-10-15: H0 implementation0771a8344861e3c1d76f7a4285869cec1ba60220; acceptance c4f8e6b2b25b3e6c0fe2ed0130f718ebaa4c4dbc; ME-005..007 reviewed proposals 7addfd3911aaea67c2170d99736637e7ecf3749c. ME-008 remains blocked; no independent ready task remains.
