# H0 toolchain selection

Date: 2026-10-12. Authority: ADR-0002 bounded owner delegation. Selected under delegated authority after independent documentary review; installation not needed.

## Versions and rationale

- SDK policy: stable .NET 10.0, global.json version 10.0.100 with rollForward latestFeature and allowPrerelease false. The version is a minimum baseline, NOT an exact installation requirement: select the highest installed stable 10.0 feature band/patch (for example 10.0.402). Keep net10.0 for library/tests/consumer; do not automatically select .NET 11 or previews. Record the actual SDK in verification evidence rather than requiring every contributor to install the original 10.0.401. No net10.0-windows target/native dependency: Windows is the validation boundary, not API coupling.
- .NET 10 is active LTS through 2028-11-14 according to [official release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json) and [support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), accessed 2026-10-12. Installed runtime 10.0.12. .NET 8 nears support end; .NET 9 has shorter runway; no preview target selected. This does not certify other platforms.
- MSTest.TestFramework 4.4.1 and MSTest.TestAdapter 4.4.1; Microsoft.NET.Test.Sdk 18.10.0. Stable published test-only packages; standard VSTest dotnet test path avoids an SDK runner migration for H0. Explicit PackageReferences, PrivateAssets all. No production NuGet dependencies.
- Package support/target evidence: [framework](https://www.nuget.org/packages/MSTest.TestFramework/4.4.1), [adapter](https://www.nuget.org/packages/MSTest.TestAdapter/4.4.1), [test SDK](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.10.0), accessed 2026-10-12. Framework/adapter compatible targets include .NET 8+; net10 compatibility must additionally be observed by restore/build/test. Not a claim of an independent package LTS guarantee. Versions chosen as a compatible stable set, not asserted newest.
- Context7 /microsoft/testfx cross-check: [adapter usage](https://github.com/microsoft/testfx/blob/main/src/Adapter/MSTest.TestAdapter/PACKAGE.md) confirms Framework+Adapter pair and test attributes. MSTest.Sdk/Microsoft.Testing.Platform is viable but unnecessary additional runner choice. xUnit/NUnit are alternatives; no requirement warrants changing familiar Microsoft tooling here.

## Reproducible checks (repository root)

~~~powershell
dotnet --version
dotnet restore checks/Hosting.Contracts/Hosting.Contracts.csproj --configfile NuGet.config -m:1
dotnet restore examples/LifecycleProbe/LifecycleProbe.csproj --configfile NuGet.config -m:1
dotnet build engine/Hosting/Hosting.csproj -c Release --no-restore
dotnet test checks/Hosting.Contracts/Hosting.Contracts.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false
dotnet run --project examples/LifecycleProbe/LifecycleProbe.csproj -c Release --no-restore -- --scenario success
dotnet run --project examples/LifecycleProbe/LifecycleProbe.csproj -c Release --no-restore -- --scenario callback-failure
dotnet run --project examples/LifecycleProbe/LifecycleProbe.csproj -c Release --no-restore -- --scenario stopped
~~~

Expected probe exit codes: 0, 1, 0 respectively. Wrong arguments exit 2 with usage. No implicit global solution/CI/publish required. NuGet restore may contact the public package feed; this is not remote execution or publication. No authenticated source probing.

## Environment finding

Initial dotnet --info displayed SDK/runtime inventories but exited 1 in workload InstallerBase initialization. dotnet --version succeeds. No workloads are required for managed H0; verify actual restore/build/test before attributing a runtime blocker. Do not modify machine-wide installation or workload state automatically.

## Observed execution environment

Shell restore with explicit public source succeeded (exit 0). Node child restore/build reads failed with missing NuGet path1; process-local environment attempts did not establish a reliable runner. Shell may incur delayed startup and a FileSystem provider warning but produced actual test output. Use shell commands for recorded functional checks; an output ending at test discovery is not a pass. Escalated shell attempt was refused by MXC due unresolved readonly mount Z:/ .agents; no denied path accessed or policy weakened. NuGet.config scopes sources to public nuget.org with clear; no ambient private-source dependency. This is local test tooling, no production dependency.

## Resumed validation — 2026-10-13

Independent h0_build_diagnosis reproduced silent parent graph failure with parallel MSBuild and successful serial build (consumer reference is build-only). Use -m:1 for this small graph; no engine threading change. Sandboxed VSTest failed to connect to testhost after90seconds; the same approved test command outside sandbox passed26/26, zero skipped, after source normalization. This is runner permission evidence, not a production workaround. Do not weaken sandbox globally or claim discovery as test success. Native review mode query outside sandbox: global off, exit0; no native review started or switch changed.

## SDK selection correction — 2026-10-15

Owner reported that 10.0.401 was absent while 10.0.402 was installed and requested no exact older-SDK requirement. Reproduced dotnet --version failure with rollForward disable; changed policy only to baseline10.0.100/latestFeature/stable. [Official SDK resolution documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json#rollforward) confirms highest installed feature band/patch within10.0. The baseline need not be installed. Tradeoff: builds can use different servicing/feature bands, so evidence records actual SDK rather than claiming byte-identical toolchains. net10.0, package versions and public contracts unchanged.

Observed GREEN: dotnet --version selects10.0.402; public-only serial restore and full31-case suite exit0 (31 passed,0failed,0skipped). No SDK installed/downgraded; earlier10.0.401 evidence remains historical. Version resolution permits other stable10.0 SDKs, but only actual tested SDKs are claimed verified. Independent sdk_rollforward_verify narrow verification passed (actual10.0.402, all targetsnet10.0, no preview/major roll-forward); H0 manual status unchanged.

## H1-R1 selected tooling2026-10-15

Retain stable SDK roll-forward/net10, serial build and existing MSTest versions. Native-only Desktop project selects Raylib-cs8.1.0; raylib6/Win-x64 assets and licenses must be checked from restored package. Sources: [tagged project](https://github.com/raylib-cs/raylib-cs/blob/v8.1.0/Raylib-cs/Raylib-cs.csproj), [interop](https://github.com/raylib-cs/raylib-cs/blob/v8.1.0/Raylib-cs/interop/Raylib.cs), [NuGet](https://www.nuget.org/packages/Raylib-cs/8.1.0), accessed2026-10-15; Context7 raylib docs cross-check. Build declaration does not prove installed assets/native loading. No package in Hosting/Loop/model.

For each checks/Loop.Contracts/Loop.Contracts.csproj or checks/Desktop.Contracts/Desktop.Contracts.csproj: dotnet restore PROJECT --configfile NuGet.config -m:1; dotnet test PROJECT -c Release --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false. Native consumer: dotnet restore examples/FirstPlayable/FirstPlayable.csproj --configfile NuGet.config -m:1; dotnet build examples/FirstPlayable/FirstPlayable.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false. Run scenarios in ME-010; real desktop required. No workloads/installers, no remote execution.

H1 native verification2026-10-15: exact package/assets/metadata and Windows startup confirmed in [evidence](evidence/h1-0/README.md); no managed transitive packages, licenses/provenance recorded, redistribution review still future scope. Reproducible integration check: powershell -NoProfile -File checks/Desktop.Contracts/NativeSmoke.ps1 (restore/build by default); -NoBuild uses existing Release outputs. Asserts60presentations with updates and close, forced update failure/close, five invalid CLI cases; exits0/1/2 exact, child timeout15seconds. Real active Windows desktop needed; missing focus is failure, not fabricated pass.
