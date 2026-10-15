# Next validation order and native options

ME-006; researched 2026-10-15. Documentary investigation only: no dependency installed,
native spike or runtime validation. Public primary sources accessed on this date;
mutable main-branch descriptions are not immutable release verification.

## Order alternatives

| Option | Benefit (assessment) | Cost/consequence (assessment) |
| --- | --- | --- |
| Graphical H1.0 next | Exercises presentation/input/lifecycle boundary early | New native dependency and desktop/hardware failure surface; scope approval required |
| Headless CAP-002 first | Deterministic timing without native dependency | Defers real event/presentation evidence; still needs approved timing contract beyond H0 |
| Full H1 now | Broad game validation | Too many untested contracts; not recommended next horizon |

Recommend only H1.0 scope next, followed by detailed contracts; not full-H1 construction.

## Observed binding facts

[Raylib-cs README](https://github.com/raylib-cs/raylib-cs) describes net8/net10 targeting,
native raylib6 and passive maintenance. Its example has a compact window/draw/close
loop. [NuGet8.1.0](https://www.nuget.org/packages/Raylib-cs/8.1.0) is a published
candidate page, not a selected/tested dependency. Main-branch claims must be checked
against the exact package. Review licenses for binding and bundled native components;
no legal certification is made here.

[SDL3-CS README](https://github.com/flibitijibibo/SDL3-CS) describes generated
function-by-function C interop, CoreCLR bindings requiring .NET8+ and zlib licensing.
This is not proof of a tested net10/native pair or complete packaging.
[SDL's bindings list](https://wiki.libsdl.org/SDL3/LanguageBindings) identifies this
binding. [Window documentation](https://wiki.libsdl.org/SDL3/SDL_CreateWindow)
documents creation failure and possible window/drawable size differences.

## Assessment, not accepted choices

Raylib-cs is the provisional first candidate for a small primitive 2D tracer: a shorter
consumer path, with broad framework coupling/passive-maintenance risks. SDL3-CS offers
a lower-level boundary at the cost of more managed wrapping and explicit deployment/error
handling. Neither is chosen or proven compatible. Keep interop private and headless
use independent of native loading. Verify exact native thread requirements; H0's
affinity rule alone does not prove native compliance.

## Evidence required before selection/construction

ME-008 must verify exact binding/native versions, TFM compatibility, RID/architecture,
transitive/runtime assets, license notices and reproducible Windows restore/build/load.
Pin selected artifacts and document update/rollback. Specify main-thread requirements,
events/focus/resize, ownership/partial teardown, public API insulation and no-native
headless operation. A build is not a deployment/startup test. No performance,
multiplatform or distribution-support promise follows from these sources.

Recommend delegating reversible private binding/version selection after scope approval
and independent review, not asking the owner to choose a library name. Escalate public
native types, H0 breakage or hard-to-reverse coupling.
