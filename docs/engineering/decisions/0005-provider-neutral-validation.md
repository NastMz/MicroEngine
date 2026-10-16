# ADR-0005 — Provider-neutral validation of frame API use

Status: accepted2026-10-16 by Codex root under delegated technical authority after independent push_policy_review design PASS. Authority: delegated technical decisions; FR-001,NFR-001/002/004/005, [ADR-0004](0004-replaceable-infrastructure.md), F-011. Supersedes only ADR-0003/H1-R1's named-friend validation mechanism after review; existing functional scenario remains unchanged. Independent design PASS recorded below.

## Problem and alternatives

Loop names Desktop in InternalsVisibleTo solely so Desktop can construct internal FrameApiException. This is a concrete reverse-name coupling despite no native project reference. Public external IFramePlatform implementations cannot use the same invalid-use classification without friend access. Existing native adapter validations are a real present consumer, not a hypothetical registry/backend.

Options: retain friends (perpetuates privileged provider names); make exception constructor public (permits direct arbitrary marker construction and changes origin contract); reclassify all adapter misuse as Failed (weakens existing H1 acceptance); or expose one engine-owned contract-check operation used by adapters (recommended). A general ownership/lifetime manager or universal provider interface would be unnecessary scope.

## Accepted exact contract

Add public static class MicroEngine.Loop.FrameApi with public static void Require(bool condition, string? message = null). If condition true, return normally without state/callbacks or side effects; message may be null/blank and is ignored. If false, synchronously throw the existing sealed FrameApiException, created by Loop, with message unchanged when nonblank, otherwise exactly "Invalid frame API use.". No arbitrary message argument can change failure type. No public exception constructor or public factory returning the marker. The method holds no mutable/global/provider state, is callable from any thread, and validates ONLY the supplied predicate. It is not an ownership validator, permission/security boundary or proof that a caller's predicate is correct.

A provider uses Require for documented invalid API use (wrong thread, disposed/lifetime/order/geometry, conflicting native ownership), NOT to wrap native loading/operational errors. Deliberately invoking Require(false) is itself an explicit invalid-use guard invocation and therefore API failure; ordinary consumer-created exceptions remain execution failures. This is a controlled shared validation operation, not an absolute claim that consumers cannot induce an engine marker. A custom backend's misuse contract now has the same opt-in path as Desktop without assembly privilege.

Remove Loop's InternalsVisibleTo Desktop attribute/associated import. Replace Desktop's direct marker throws with Require predicates preserving their current order, messages, types and pre-native-call checks. Keep Desktop's internal test-only friendship to Desktop.Contracts (within adapter implementation, not engine dependency). Loop's existing FrameControl may continue constructing its own internal exception; no requirement to refactor unrelated code. Keep FrameApiException constructor internal and sealed, and FrameLoop exact marker/aggregate classification unchanged.

## Compatibility and evidence requirements

H1-R1.1 is additive API plus removal of an internal implementation privilege, no existing public signature removed. Current external providers need no source change unless choosing guard parity. Current ordinary consumer/native exceptions remain Failed; invalid use via engine validation remains thrown after H0 cleanup, with nested draw/End/cleanup diagnostics unchanged. No provider registry, extra backend, renderer features, target/export promise or H0 contract change.

Public tests in nonfriend Loop.Contracts assembly: Require true (null/blank/custom message) no effect; false (null/blank/custom) exact type/message; external provider invokes false before drawing, reports same marker only after release; draw+End+Dispose failure preserves marker-first nested and cleanup order; ordinary InvalidOperationException and ApplicationException containing an engine marker in an arbitrary nonaggregate InnerException remain operational failures. Assert Loop assembly exposes no Desktop friend. Existing H031/Loop24/Desktop9 remain. Observe runnable RED for false predicates/friend boundary before GREEN. Restore/build native consumer and seven-scenario NativeSmoke harness; real Windows desktop required, failures never waived.

Independent design before acceptance/ready; independent scratch implementation verification and parent spotcheck before completion. Prove a public-only external provider can use the guard without changing Loop or referencing Desktop/Raylib. No broader multi-backend/native portability certification follows. F-011 becomes resolved only after this evidence, not at proposal time.

## Consequences and rollback

One small public method is a maintenance commitment, chosen for an actual adapter need. Predicate/message correctness remains each adapter's responsibility; intentional false calls produce intentional API errors. Do not normalize arbitrary operational exceptions into misuse. Future richer validation requires its own concrete case/review. Rollback own new guard/calls/tests/doc delta together, restore existing internal friend if reverting; no automatic destructive rollback or force push. H0/H1 subjective gate unchanged.

## Review and approval

Independent push_policy_review design PASS2026-10-16 recommends adoption; minimal public guard/ordinary-error distinction and exact checks are consistent with ADR-0004 and current H1 scope. Codex root accepts ADR/H1-R1.1 under delegated authority. Source readback clarifies7Desktop guards migrate,1Loop-internal guard retained. Runtime N/A at design gate; construction evidence remains pending.

## Implementation evidence2026-10-16

ME-015: author runnableRED8fail/4pass -> GREEN76existing+new suite cases (36Loop/9Desktop/31H0), native seven-scenario harnessPASS. Independent provider_guard_verify repeats fresh TEMP76/76, focused12/12subset, native harnessPASS;0warnings/errors, no blockers. Core no Desktop friendship in compiled metadata, public test-provider assembly has no Desktop reference/friendship. All7Desktop checks preserve predicates/messages/order;1internal FrameControl check retained. No FrameLoop/Hosting/native API/game source changed. F-011 resolved for the named coupling only, not universal conformance.
