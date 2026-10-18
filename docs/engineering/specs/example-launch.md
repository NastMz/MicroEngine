# LAUNCH-1 — Shared example launch parsing
Status: approved2026-10-18 by root under technical delegation, after independent cross_scenario_map PASS.
FR001/002, NFR002/004/005. Scope: example support, not a new engine capability or public API.

## Problem and choice
Three accepted consumers duplicate identical smoke/failure parsing. Extract only that proven
pure policy into internal linked source examples/Shared/LaunchOptions.cs, compiled by the three
executables and a new test project. No package/project/backend dependency or engine change.
A generic runner would also change ownership and diagnostics; defer it. Leaving duplication
is safe but allows grammar drift. Linked internal source avoids a new published support API.
No significant engine architecture decision is made; no new ADR required for this local extraction.

## Exact contract
Internal readonly record struct LaunchOptions(int FrameLimit, bool FailUpdate) exposes static
bool TryParse(string[] args, out LaunchOptions options). Null args throw ArgumentNullException;
invalid input returns false and default options without side effects. Empty args succeeds(0,false).
Use existing int.TryParse semantics/current culture, not a new numeric grammar.
Accept --smoke once with one integer1..600 and --fail-update once, in either order.
Reject unknown/wrong-case tokens, missing/out-of-range/noninteger values and duplicated flags,
including duplicate --smoke even if equal. No flags added. No files, console, window or provider
are touched. Each consumer retains exact existing Usage string, exit2 on false, parsing before
assets/native startup; startup/primary/cleanup output and exit0/1 remain unchanged.

## Allowed surfaces and exclusions
examples/Shared/LaunchOptions.cs (new), the three examples/{Platformer,Adventure,Rpg}/Program.cs
and matching csproj, checks/ExampleLaunch.Contracts/* (new), linked engineering evidence/backlog/log.
Only replace parsing and add linked source references. No lifecycle/runner/report/loop/counters,
model/renderer/assets/SDK/dependencies/native-probe behavior changes. Never migrate old tracers.

## Acceptance and verification
Tests must run RED against an unimplemented stub, then GREEN for empty args, boundaries1/600,
both flag orders, existing whitespace/sign int parsing, invalid token/case/value/duplicate/missing,
and atomic invalid output. Null is a separate API misuse test. Test uses the actual linked source.
New project uses existing net10.0/MSTest4.4.1/TestSdk18.10.0, no dependency upgrades.
Run dotnet test checks/ExampleLaunch.Contracts/ExampleLaunch.Contracts.csproj -c Release
with cached restore and serial toolchain flags. Run existing PlatformerNativeSmoke.ps1,
AdventureNativeSmoke.ps1 and RpgNativeSmoke.ps1 unmodified: existing gameplay/pixel assertions
and exact CLI output/exits/cleanup must pass. Existing278 tests retained plus new parser cases.
Independent final frozen-commit verification checks original-source parity outside allowed scope,
all three executable grammars and untouched reports/startup/cleanup. No manual check adds value
for identical behavior. Risk item3: shared consumed CLI semantics, despite compatibility intent.
Revert this scoped unit normally if necessary; no data changes or history rewrite.
