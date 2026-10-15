# ME-001: Initial platform and delivery investigation

Date/source access: 2026-10-12. Status: investigation result, not an accepted decision.
Requirements: FR-001, NFR-002/NFR-003. Baseline:
75a5eef1770f31235c69f8dcc9019de530226886. No backend selected, installation or platform test run.

## Verified source facts

- .NET identifies platform/architecture targets with RIDs. Managed portability does not
  prove portability of native dependencies. [RID catalog](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog).
- Framework-dependent applications require a suitable installed runtime; self-contained
  application deployment includes its runtime. This distinction concerns the consumer
  executable, not whether the framework is delivered as source or a package.
  [Publishing](https://learn.microsoft.com/en-us/dotnet/core/deploying/).
- NuGet can carry native assets selected by RID under runtimes-specific paths. Packaging
  these assets does not prove their OS dependencies or hardware behavior.
  [Native assets](https://learn.microsoft.com/en-us/nuget/create-packages/native-files-in-net-packages).
- GitHub-hosted runners offer Windows, Linux and macOS environments. Availability is not
  evidence that this repository can currently run GPU/audio/interactive acceptance there.
  [Hosted runners](https://docs.github.com/en/actions/concepts/runners/github-hosted-runners).

.NET deployment facts were also cross-checked through Context7 /dotnet/docs against
[Microsoft's source](https://github.com/dotnet/docs/blob/main/docs/core/deploying/index.md).
Exact .NET/OS versions, runner images/prices and backend support remain to be selected and
reverified at implementation; the prototype's .NET version is not inherited.

## Platform options — engineering assessment

| Option | Benefits | Costs and risks | Evidence needed before support claim |
| --- | --- | --- | --- |
| Windows desktop first | Smaller native verification matrix; focused initial consumer | Other users excluded initially; possible Windows assumptions | Approved Windows versions/architecture; actual local/native startup/cleanup and consumer checks |
| Windows/Linux/macOS desktop from first release | Early portability constraints; broader consumer reach | More native packaging, dependency and test environments; interactive validation across OSs | Per-OS/architecture runs, native load/error/cleanup and consumer deployment evidence |

No tested support exists yet. Portable contracts and headless tests can reduce coupling
in either option, but do not establish native compatibility. Headless CI covers lifecycle
logic; native integration and significant manual checks cover device/window behavior.
Cross-platform does not mean every distribution, architecture or driver is supported.

## Framework delivery options — engineering assessment

| Option | Benefits | Costs and risks | Required integration |
| --- | --- | --- | --- |
| Source consumption / project reference | Short feedback loop; easiest early contract experiments | Consumers build source; can accidentally access internals or absorb repository settings | Separate consumer, public APIs only, clean build with documented prerequisites |
| Local NuGet package consumption | Exercises published surface and dependency layout early | Package metadata/version/feed policy and native asset layout need decisions | Pack locally; restore consumer from isolated local feed; verify packaged assets/public API |

Remote NuGet publication is a separate authorization, not required to validate a local
package. Source consumption must not mean copied genre libraries or legacy migration.
Application framework-dependent versus self-contained deployment is a later independent
choice; it is not evidence that a library package runs on every OS.

## Recommendation — not approved

For the minimal lifecycle experiment, prefer an external source consumer and headless
integration, with an explicitly bounded first native platform if native resources are
included. Evaluate package consumption once a public boundary is stable enough to test.
Windows-first is the lower initial matrix option, not a requirement or support claim.
Cross-platform-first is reasonable if initial consumers require it and test access exists.

## Owner decision and ME-003 inputs

One bounded scope decision: choose initial platform promise and framework consumption
mode for H0. Alternatives are the two platform options combined with source or local
package consumption. Recommended starting combination: Windows-first/source consumption,
while avoiding native coupling in the lifecycle contract. This remains pending.

ME-003 must capture exact OS/architecture, .NET target, native versus headless H0 scope,
dependency approval, consumer/test paths and checks. Runtime version support, hardware
access, licensing, backend choice and private CI permissions are unknown, not assumed.
A pure headless H0 is another execution boundary option, not a replacement support claim.

## Acceptance and checks

Both platform options and both delivery options compared; source facts and assessments
separated; recommendation labeled unapproved; next-decision inputs listed.
Source readback and claim support checked against the four official pages above.
Document relative links and git diff --check checked by executor; build/native tests N/A:
research only and no engine/project exists. No measured cost/performance claims made.
