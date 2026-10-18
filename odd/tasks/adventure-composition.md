# Courtyard Key bounded scene composition
Baseline31ebe37, branch feat/adventure-composition. Local work-unit commits only; no push/main.
Delivery existing stacked-to-main preference for future authorized delivery; no PR now. Forecast ~250 authored lines; dotnet test Adventure.Contracts and AdventureNativeSmoke, prior regressions retained.

## Specs
S1. "Haz una mejora visual acotada utilizando las capacidades públicas existentes de MicroEngine. Evita modificar el núcleo si únicamente estamos cambiando composición artística y contenido." Author two richer fixed room compositions with existing licensed sheets only, no engine/API/dependency/model/editor/new mechanic. Review available terrain/paths/structures/decor, no disputed trees.
S2. "Mantén el ciclo jugable actual y sus pruebas de regresión." Preserve all model/gameplay/input/collision/interaction behavior and previous tests. Paths/flat decals cannot introduce apparent new blocking geometry; substantial structures align with existing solids, interactables/player/HUD stay visible. Retain 2x/source bounds/resource lifetime checks; run managed/native course and independent verification.
S3. "Después de la mejora, evalúa qué hemos aprendido sobre el pipeline de tiles, la composición de escenarios y la separación entre motor y contenido." Record selection, layer/content policy, evidence, honest limitations and whether a general capability expansion is actually necessary. This is one bounded art pass, not indefinite development; subjective new composition remains owner-owned.

## Tasks
- T1 / S1-S3 / inline / completed / content design and review existing atlas regions; meaningful RED not applicable to passive art selection.
- T2 / S1-S2 / inline / completed / runnable composition tests RED then GREEN/content/native proof, one coherent behavior commit.
- T3 / S1-S3 / independent verification / in progress / protected-byte/regression review and evidence/closure.

## Log
L1. Owner request verbatim:
He probado Courtyard Key y el ciclo funcional del escenario es satisfactorio. Las interacciones con la palanca, apertura de la puerta, transición al santuario, obtención de la llave y restauración del altar funcionan según lo esperado.
Mi observación principal es visual: la composición del escenario es demasiado básica y repetitiva. El patio utiliza prácticamente un único patrón de terreno y el santuario parece una habitación vacía. No transmite todavía la sensación de explorar una aventura cenital.
Quiero que revises los paquetes gráficos disponibles y evalúes si podemos construir escenarios visualmente más coherentes, aprovechando tiles de caminos, vegetación, paredes, estructuras, decoración y elementos ambientales.
No se trata de añadir mecánicas nuevas ni de construir un editor de mapas. Tampoco quiero que desarrollemos indefinidamente este ejemplo.
Haz una mejora visual acotada utilizando las capacidades públicas existentes de MicroEngine. Evita modificar el núcleo si únicamente estamos cambiando composición artística y contenido.
Si encuentras que representar mapas más elaborados requiere realmente ampliar una capacidad general del motor, documenta y justifica esa necesidad antes de implementarla.
Mantén el ciclo jugable actual y sus pruebas de regresión.
Después de la mejora, evalúa qué hemos aprendido sobre el pipeline de tiles, la composición de escenarios y la separación entre motor y contenido.
Puedes continuar autónomamente sin solicitar nuevas aprobaciones técnicas ordinarias.

L2. Owner accepts existing functional cycle, not existing or future composition. No main/push authorization inferred under latest local-only horizon restriction. Preserve .gitignore/.codegraph. Existing source/header/provenance reviewed via CodeGraph and targeted docs; read-only atlas mapping delegated after evidence budget. Parent writes inline, no writer trigger. Memory mirror pending: runtime identity unavailable, no agent memory calls permitted. RDD remains explicitly off. Existing reusable rendering suffices provisionally; no significant architecture decision/new ADR expected.

L3. New composition regression RED1 (missing solid-aligned pillar), then added stronger complete16x16-source diversity assertions after noticing edge clips could inflate total diversity; RED1 at newassertion, all originalassertionsretained. A proposed replacement of that newlyauthored count assertion was rejected byauto-review; no suchreplacementexecuted. Kept existingassertion and added strictfulltilecoverage instead. GREEN13/13, allprior12retained. ActualPNGnative originalpath/grass/floor/rugRGB added, gateopenreferenceupdatedto path, closedgate/2x/HUD460stepcourse/replay/sevenleases/CLI7PASS; assethashesPASS, build0warnings/errors. Onevisualreadback replaced an unintended grassedge crop with grassvariant before freeze. Rootcontentdesign uses existingcontracts only, noADR. Rollback entireconsumercomposition+newchecks/docs only. RDDexplicitlyoff. Independent final reviewpending; sourcefileshaven'tchanged outside narrowscope.
