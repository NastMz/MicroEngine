# Provider-neutral frame validation
Branch feat/h1-runtime; baseline a75f22c4098bc8a2954f0d2520eca646b0baf753. Local coherent units and authorized normal push to origin; no PR/main merge.
Runner: existing serial dotnet Release suites plus native smoke. Memory mirror unavailable: no authoritative runtime identity.

## Specs
S1: User: "continua" under established technical autonomy; "Las decisiones deben respetar el manifiesto y los principios de MicroEngine." Concrete scope derived from accepted ADR-0004/F-011, not a new game capability: remove Loop's named Desktop friend privilege while preserving H1 error/cleanup behavior.
S2: "Las capacidades deben diseñarse mediante contratos cohesionados, mínimos y extensibles" and "Las implementaciones concretas deben poder incorporarse como módulos o adaptadores reemplazables sin requerir modificaciones sustanciales del núcleo." Scope: one public validation guard consumed by existing Desktop and an external test-provider assembly; no registry/new backend/universal interface.
S3: "No alteres criterios de aceptación ni reduzcas cobertura"; retain H031/Loop24/Desktop9 and native smoke contracts. Guard API specification/design needs independent review before implementation; execution needs RED/GREEN and independent implementation verification.
S4: "Tienes permitido hacer commit y push de ahora en adelante"; "no uses la nomenclatura codex/*". Authorized configured origin/Git authentication, work-type branches; preserve .gitignore/.codegraph, no force/main/PR/release. No subjective H1 acceptance inferred.

## Tasks
T1 S1-S3 inline: ME-014 completed, independent push_policy_review designPASS; ADR-0005/H1-R1.1 accepted, runtimeN/A.
T2 S1-S3 inline: ME-015 public guard/remove friend/provider regression tests; pending; independent verification mandatory for public error contract.
T3 S4 inline: scoped commits/evidence/normal push; pending.

## Log
L1 Original current request: "continua". Prior explicit authorization: "Tienes permitido hacer commit y push de ahora en adelante, sigue trabajando"; naming correction: "Si pero no uses la nomenclatura codex/* usa git flow (feat, fex, docs, etc)".
L2 2026-10-16: inspected current source with CodeGraph, named friend is actual F-011 coupling. Public external providers already conform without native dependency but lack shared guard; candidate one-method API will be independently reviewed before code. Parent writes inline (no writer trigger); risk public error contract requires independent design/implementation verification. Forecast under400 authored lines for the bounded correction plus proportional evidence; no PR is prepared.
L3 Delivery policy independently reviewed push_policy_review PASS; commit a75f22c normal push origin/feat/h1-runtime confirmed. Pre-existing .gitignore/.codegraph preserved. RDD globaloff observed, unchanged.

L4 2026-10-16: independent designPASS, minimal one-method guard approved. Clarification7Desktop guard sites migrate,1FrameControl internal guard remains. ME-015 readiness gate satisfied after ME-014 completion; code not yet written.
