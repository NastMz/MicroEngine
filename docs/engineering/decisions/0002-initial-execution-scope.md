# ADR-0002: Initial execution scope

- Status: accepted (owner direction and bounded technical delegation)
- Date: 2026-10-12
- Approval: repository owner, 2026-10-12; explicit scoped approval and delegation recorded below. Construction follows reviewed H0 contract readiness.
- Related: FR-001, NFR-001/NFR-002/NFR-003, CAP-001; [ADR-0001](0001-greenfield-direction.md)
- Supersedes / superseded by: none
- Preparation: ME-003; baseline 75a5eef1770f31235c69f8dcc9019de530226886

## Context and problem

The next horizon must prove an externally consumed lifecycle without assuming an entity,
scene or native backend architecture. [ME-001 research](../investigations/platform-scope.md)
compares desktop targets and distribution; [lifecycle](../specs/lifecycle.md) and
[frame execution](../specs/frame-execution.md) contain independently reviewed proposed traces.
Review establishes consistency, not acceptance or platform support. No projects or runners exist.

## Alternatives considered

| Concern | Alternatives and tradeoffs |
| --- | --- |
| Platform promise | Windows-first limits native validation cost; Windows/Linux/macOS expands reach and deployment/test obligations. Neither is currently supported by implementation evidence. |
| First proof | Pure managed headless lifecycle isolates ownership/error behavior; native window proof validates real integration but requires backend, platform, licensing and hardware investigation before construction. |
| Consumption | Source/project reference minimizes initial tooling; local NuGet package exercises packaging/asset resolution but adds package identity and distribution work. Remote publication is separately authorized. |
| Error channel | Structured report makes primary/cleanup failures explicit; propagated primary exception needs an accessible cleanup diagnostic channel. Neither may silently turn failure into success. |
| Stop before run | First run returns requested-stop without acquisition; alternatively reject the stopped instance. Both must reject later/reentrant runs without side effects. |
| Test arrangement | Dedicated test project with an approved test framework integrates test discovery; a dependency-free executable assertion harness avoids packages but requires explicit assertion/exit-code reporting and a different runner packet. |
| Thread scope | Single-thread lifecycle avoids synchronization claims; marshalled external stop adds concurrency design and tests. |

## Historical proposal and justification — superseded by approval below

Recommend H0 as a headless, single-threaded, source-consumed lifecycle proof with a structured
execution report. First run after pre-run stop returns requested-stop; later/reentrant runs
are rejected. Choose a dedicated test project only after its framework/dependency versions
are approved. Native backend: none in this proposed H0; this is not a decision against later
native support. Platform support promise and target framework/SDK remain owner choices.

CAP-002 fixed ticks, input, pause and interpolation remain outside proposed H0. Do not treat
its refined options as selected defaults. H0 needs only ordered consumer work and safe teardown,
not a scheduler or game framework. Public C# signatures must be frozen in the lifecycle spec
before implementation: session construction, acquisition registration, run callback, stop,
dispose, report fields and rejection channel. This ADR deliberately does not approve signatures.

### Exact proposed construction surfaces

These are candidate paths for owner approval, not permission to create files. No names or
layout are inherited automatically from the prototype.

- global.json — approved SDK pin
- engine/Hosting/Hosting.csproj — approved target framework, library dependencies
- engine/Hosting/ExecutionSession.cs — public session lifecycle
- engine/Hosting/ExecutionReport.cs — outcome and ordered diagnostics
- engine/Hosting/AcquisitionStep.cs — acquisition/ownership transfer contract
- checks/Hosting.Contracts/Hosting.Contracts.csproj — approved test framework
- checks/Hosting.Contracts/LifecycleTests.cs — public trace/rejection/error tests
- examples/LifecycleProbe/LifecycleProbe.csproj — separate consumer project reference
- examples/LifecycleProbe/Program.cs — normal and injected-failure consumer scenarios
- docs/engineering/specs/lifecycle.md — accepted public contract and trace mapping
- docs/engineering/backlog.md — fully populated ME-004 packet and evidence
- docs/engineering/current-state.md — actual implementation/support state
- docs/engineering/README.md — approved build/test entry points

No solution, backend, Core genre module or future project is necessary for this candidate.
Package consumption or native-window selection invalidates this surface/runner proposal;
refine the packet first rather than improvising additional files.

### Proposed check commands and acceptance

Commands below are future checks, NOT executed evidence: projects do not exist. Run from
repository root only after SDK, framework, package versions and signatures are approved.

~~~powershell
dotnet restore checks/Hosting.Contracts/Hosting.Contracts.csproj
dotnet build engine/Hosting/Hosting.csproj -c Release
dotnet test checks/Hosting.Contracts/Hosting.Contracts.csproj -c Release
dotnet run --project examples/LifecycleProbe/LifecycleProbe.csproj -c Release -- --scenario success
dotnet run --project examples/LifecycleProbe/LifecycleProbe.csproj -c Release -- --scenario callback-failure
~~~

The consumer must use public types only from a separate project, with no internal access or
prototype reference. Success emits ordered acquisition/callback/reverse-release evidence and
exit 0. Injected callback failure emits its primary error and ordered cleanup evidence, exits
nonzero, never emits success, and performs no next callback. Proposed scenario flags belong
to the future probe only, not a presently available CLI.

Contract tests must cover accepted T01-T14 through the public interface: no partial resource
released by the host, every committed release attempted once even after failures, committed
frame cleanup where applicable, no callbacks after stop/dispose, no repeated acquisition on
rejected runs, and primary/secondary error ordering. Headless H0 must explicitly mark native
begin/end assertions not applicable unless a frame adapter is approved; it must not claim
native cleanup proof. Adopt no native resource abstraction merely to satisfy a conceptual trace.

Documentary preflight: links/source consistency, git diff --check, independent design readback.
Implementation checks additionally require observed failing tests before behavior changes,
passing final tests, separate consumer evidence and the review/rollback plan in ME-004.
A successful test command alone does not imply platform support or manual milestone acceptance.

## Approval gates and consequences

Owner must explicitly decide platform/architecture scope, headless versus native proof,
source versus package delivery, SDK/target framework, test framework/dependency versions,
thread/error/pre-run-stop policies and exact public signatures. Approval evidence needs date,
approver and the accepted scope; partial approval leaves remaining gates closed.

After approval, update accepted specs and this record consistently, then populate ME-004
with exact paths, dependencies, commands/environment, acceptance, independent review and
rollback. Only then may its readiness be reassessed. Preparation completion cannot unblock it.
If native proof is chosen, investigate candidate backend compatibility/licensing/deployment
before proposing that dependency; the current report does not select one.

Benefits of the recommendation: a narrow consumer-visible proof and deterministic failure
traces without premature subsystems. Costs: no window/game/manual native experience and no
package/support guarantee. Reconsider when an approved milestone requires native integration.
Rollback of this preparation removes only this proposed record and its own register/task notes;
retain research and reviewed proposed specs. Future implementation rollback must be separately
specified before construction.

## Preparation evidence

Inputs: ME-001 completed sourced research; ME-002 completed independent consistency review.
Runtime checks: N/A, no engine/project created. Independent reviewer contract_verify found no substantive blockers; one register-table correction resolved by targeted recheck. Relative-link checks and git diff --check passed. This is documentary evidence, not approved architecture or runtime proof.

## Owner approval and delegated authority — 2026-10-12

He revisado la propuesta de ADR-0002. Apruebo su dirección arquitectónica con las siguientes condiciones.

Decisiones aprobadas: H0 headless; validación inicial Windows sin promesa multiplataforma; consumidor independiente mediante API pública y referencia de proyecto; ejecución secuencial con afinidad de hilo; sesiones de un solo uso; ownership definido y limpieza ordenada ante fallos parciales; proyecto de pruebas independiente y herramientas estándar .NET; sin renderizado nativo, ECS, escenas, física, audio ni funciones fuera de H0.

Condiciones: headless es modalidad futura válida, no temporal; distinguir configuración y sesión conceptualmente; separar fallos de ejecución de uso inválido de API; informe pequeño y estable sin ownership interno innecesario; no obligar a reconstruir runtime al añadir backend gráfico.

Delegación explícita: seleccionar y justificar SDK/target/dependencias verificando soporte y compatibilidad; definir firmas mínimas, estructura, mecanismos y pruebas dentro de H0 mediante especificación, revisión independiente y registro. No pedir aprobación por decisiones locales. Bloquear cambios de principios, expansión significativa o restricciones futuras difíciles de revertir.

Autoridad: preparar contratos/toolchain/revisión/ME-004 y, tras verificaciones documentales, implementar y probar H0. No publicar remoto, integrar main ni modificar fuera del alcance. Parar ante bloqueos estratégicos, errores no resolubles con seguridad o incremento funcional que requiera validación manual.

The approval accepts direction and delegates local technical decisions; it does not accept every historical proposed name/signature. No commit, remote or merge authority inferred. [H0 contract](../specs/lifecycle.md) and [toolchain](../toolchain.md) record delegated choices after independent review. Historical pending gates above describe preparation, not current authority.

### Delegated technical choices — finalized after independent review

Use immutable configuration with ordered synchronous disposable factories, reusable to create fresh single-use sessions. No public ownership ledger/acquisition-step type. Headless invokes consumer work once; consumer owns any loop. Stop is cooperative at factory/work boundaries. Small immutable report contains outcome, primary exception and ordered secondary cleanup exceptions only. Invalid API use throws standard argument/invalid-operation exceptions and is not a failure report. If invalid use escapes a callback, teardown still occurs before rethrow.

This is permanent headless hosting, not a mock graphics path. No public headless boolean, graphics interface, native handles or render loop constrains future backends; new capabilities may compose resource factories later without changing H0 guarantees. Future backend architecture is not selected.

Exact construction: global.json; engine/Hosting/{Hosting.csproj,EngineConfiguration.cs,ExecutionSession.cs,ExecutionReport.cs}; checks/Hosting.Contracts/{Hosting.Contracts.csproj,LifecycleTests.cs}; examples/LifecycleProbe/{LifecycleProbe.csproj,Program.cs}. AcquisitionStep.cs from historical proposal is not authorized/needed. Test dependencies and SDK are in toolchain.md.

### H0-1.1 delegated correction — 2026-10-13

API misuse follows engine origin, not just exception base type. An internal marker covers own argument/invalid-operation validation. Consumer-created standard argument/invalid-operation exceptions and invalid factory returns remain execution failures. Mixed failures preserve ordered causes in a standard category-matching wrapper. Independent targeted review required this correction to honor the owner condition; no exported types or signatures added.

## Technical finalization and implementation evidence — 2026-10-13

Owner approval remains scoped to H0 direction; delegated signatures/mechanisms/toolchain are finalized in H0-1.1 and toolchain.md. Independent documentary h0_design_review passed after explicit consumer restore correction (supplied snapshot because read tooling unavailable then). Independent source h0_final_verify scratch review and targeted argument-origin correction passed; 31 public acceptance cases passed in parent. No recommendation outside delegated scope accepted. ME-004 automated completion does not accept H0 manually or authorize H1. Local uncommitted source/evidence; no remote/main integration.

### Toolchain amendment — 2026-10-15

Owner instruction: "No fuerces la version de .net" after exact10.0.401 was unavailable with10.0.402 installed. Within delegated toolchain authority, replace exact SDK pin by stable10.0 minimum10.0.100/latestFeature selection. No preview or automatic11.x, no target/package/public API change. SDK10.0.402 restore/full31 tests passed; source evidence and manual instructions updated. Approval status and H0 acceptance unchanged.
