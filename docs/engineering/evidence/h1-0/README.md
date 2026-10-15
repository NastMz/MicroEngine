# H1.0 local technical evidence

2026-10-15, Windowsx64, SDK10.0.402, NVIDIA GeForce RTX4070TiSUPER, native OpenGL3.3. No multiplatform/performance/distribution certification. [Contract](../../specs/h1-runtime.md), [milestone](../../milestones/H1.md), [backlog](../../backlog.md).

## Observed checks

- Loop.Contracts RED20failed/1passed against runnable stubs, then21pass; verifier diagnostic probe persisted, final22pass. Independent managed scratch21+1probe pass, no Desktop/Raylib in headless assets.
- H0 Hosting.Contracts31pass/0fail/0skip, no Hosting source delta.
- Desktop.Contracts initial compile-only warning failures were NOT counted as RED. After correcting stubs without suppressions: RED8fail/1pass -> GREEN9pass/0fail/0skip.
- FirstPlayable Release build0warnings/0errors. Native --smoke60: Stopped,60frames/58updates, exit0, native window closed. --smoke60 --fail-update: Failed/ApplicationException,2presentations/0completed updates, exit1, window closed. --unknown: usage exit2. Times/frame counts are environment observations, not deterministic promises.
- Client-only [capture](native-smoke.png),640x360: dark canvas, gold player at left/floor, green floor. Captured during separate --smoke180 process; exit0. No unrelated desktop area captured. This verifies visible primitive output only, not subjective quality or input.

Exact commands are in [toolchain](../../toolchain.md) and milestone. Independent native implementation verification completed below; file existence alone was never PASS.

## Dependency provenance

Desktop has exact Raylib-cs[8.1.0], net10 asset; restored nuspec repository commit d7b4d3c18f95f1eff9d19a91b1f428ef7d917780, license expression Zlib, net8/net10 dependency groups empty (no managed transitive packages). Native binaries and embedded third-party code are not thereby dependency-free.

- Restored nupkg SHA512(base64): X1oslaFJ6klTJeWNECJ3lCUhhx8X3xjE6brTGXNuqV5ET6LH3JtAEsrddS7rqJLaGM5tZj9jY3KQPnwUQsKQqg==
- runtimes/win-x64/native/raylib.dll:1905152bytes; SHA256 c62606798c3f736b479db7721aed884102060541b743fd81be3e687ac6de3e67.
- Native initialization log identifies raylib6.0. The bundled build includes broad raylib modules; MicroEngine exposes only approved primitive desktop functionality, no audio/3D capability.

Primary sources accessed2026-10-15: [package](https://www.nuget.org/packages/Raylib-cs/8.1.0), [tagged project](https://github.com/raylib-cs/raylib-cs/blob/v8.1.0/Raylib-cs/Raylib-cs.csproj), [binding license](https://github.com/raylib-cs/raylib-cs/blob/v8.1.0/LICENSE), [native license](https://github.com/raysan5/raylib/blob/6.0/LICENSE). Binding/native project notices permit use under zlib conditions; retain origin/notices and mark altered source when applicable. No third-party sources changed or binaries committed. Bundled native third-party notices/deployment review must be completed for any future redistribution: no distribution/release is approved here and this is not legal certification.

## Reproduction and limits

Serial restore/test/build commands use NuGet.config and stable installed .NET10 roll-forward. Native executables load package runtime assets from output; verified startup is not a package-distribution test. VSTest requires local loopback outside this sandbox, observed rather than hidden. Internal fake-native seam tests ownership/failure paths without forcing real GPU failure. Native main-entry-thread compliance remains a caller obligation; adapter checks creating-thread affinity. Subjective movement feel/readability remains human evaluation, not a mandatory replay of automatic cases.

## Independent final verification and technical acceptance

desktop_tracer_verify copied scoped sources/config into system-temp scratch, restored and ran9Desktop tests, built Release0warnings/0errors, repeated native normal/failure/invalid and NativeSmoke.ps1 seven scenarios PASS. Metadata/hashes/capture independently confirmed; no reproduced blockers. managed_runtime_verify scratch22(including subsequently persisted diagnostic probe) PASS. Codex root accepts H1.0 objective technical criteria under owner delegation2026-10-15, not subjective playability/full-H1. Parent final regression/spotcheck recorded in milestone.

## Final stop-boundary correction

ME-011 independently reproduced baseline0dfb2ce Begin/Canvas stop bug (two focused failures), current24managed tests pass in fresh scratch. Fix commit43c8df9. Parent rebuilt native consumer, seven-scenario harnessPASS, Desktop9/9,H031/31; final64tests,0failed/0skipped. Original22test results above are historical pre-correction evidence, not the final count. H0 source remains unchanged.
