# H3 checkpoint launch integration

Objective: opt-in process-restart persistence in TurnExplorer; branch feat/h3-checkpoint-launch baselinebb75223. Runner serial Release suites/native harnesses.
Delivery: cached stacked-to-main, no PR/main integration; forecast360lines, coherent parser/integration slices if needed. Memory mirror unavailable pending host identity.

## Specs
S1: "continua con el siguiente trabajo". Next C-003/CAP-013 horizon connects verified CP-1/CF-1 to actual consumer startup/exit without new engine capabilities.
S2: Default invocation/controls/game rules/old CLI behavior unchanged; opt-in load before native acquisition, save-new only after successful loop/cleanup; existing files never overwritten. Review contract first, reproduce RED, automate process restart/error/cleanup proof, no operational owner test.

## Tasks
T1 S1-S2 inline in-progress ME-034 independently reviewed launch contract; no new architecture/engine ADR needed for composing accepted CP-1/CF-1.
T2 S1-S2 inline pending ME-035 parser/tests/native integration/harness, independent whole-feature verification, synchronized docs/commits.

## Log
L1 Owner request verbatim: continua con el siguiente trabajo
L2 CF-1 objectively accepted, next concrete use is persistence across two processes of same game. Existing top-level Program not indexed by CodeGraph query, so direct bounded read used after query found no relevant code. Parent inline writer, highrisk shared CLI/fileeffects/projectgraph gets independent design/verification. Preserve .gitignore/.codegraph. Main751686a unchanged; new behavior not authorized for main by earlier positive feedback.

L3 Independent checkpoint_design_review PASS/no blockers: existing cleanup failure means Failed, no new ADR. Keep persistence catches narrow, not loop/native. Codex root approves CL-1 before source; ME-034completed, ME-035ready->in-progress. RuntimeN/A passive contract, diff/readback proof. Forecast may exceed400 across integration; cached stacked-to-main parser/test then nativeintegration/test slices, no source compression or PR inference.

L4 Parser RED3failed/0passed ->GREEN3/3, fullLoop118/118. Immutable options preserve old grammar/parse-before-effects; source-linked real parser tests. Commit parser/tests as coherent internal unit; no executable behavior change until native integration, no task closure/independent PASS yet.
