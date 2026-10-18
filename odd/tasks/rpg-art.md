# H3 asset selection and bounded art integration
Baseline1f38a29; feat/rpg-art. Local work-unit commits; cached stacked-to-main strategy, no PR/push. Mature localmain integration permitted, product acceptance separate.
Forecast ~350 authored lines; dotnet Rpg.Contracts and RpgNativeSmoke, representative old regressions.

## Specs
S1. "Evalúa disponibilidad, licencias, estilo visual, resolución, animaciones y compatibilidad de los recursos. Puedes seleccionar otros paquetes CC0 si encuentras alternativas mejores." Evaluate all six owner candidates from official pages and actual downloads, distinguish facts from unverified claims, preserve selected originals/licenses/provenance.
S2. "No es obligatorio utilizar todos estos recursos. Prefiero pocos assets visualmente coherentes a mezclar muchos paquetes incompatibles." Select Tiny Town/Tiny Dungeon for matched16px environments/opponent, retain already verified animated ArMM hero rather than fake missing animation. Static battle art is explicit, no need to import every effect/UI pack.
S3. "El motor no debe conocer conceptos específicos de Pokémon, criaturas capturables o reglas de combate." / "La exploración y el combate deben poder presentar contenido visual muy diferente utilizando las capacidades públicas del motor." Preserve engine/model/input/HP/outcomes and old examples; only game art/assets/content links/tests/docs change.
S4. "Evita repetir los problemas de composición e interpretación de tilesets detectados en Courtyard Key." Use original packed sheets with verified16px stride, explicit consumer edge/corner/interior roles and whole compound terrain, not arbitrary independent images or a new autotile/import/editor system.
S5. "Incorpora la selección de assets y su documentación al horizonte H3 antes de construir los escenarios correspondientes." Selection/contract/independent design review before changing presentation. Existing RPG1 functional implementation is preserved, not discarded or retroactively claimed to use new art.
S6. "No amplíes el alcance técnico del RPG simplemente por disponer de más recursos gráficos." / "Continúa autónomamente con el roadmap y las validaciones establecidas. Solicita mi intervención únicamente cuando exista una experiencia jugable que requiera evaluación subjetiva o un bloqueo estratégico legítimo." Keep same two rooms/encounter/three actions/outcomes. No status ailments/VFX rules/new GUI APIs, depth/semantic/map tools or capabilities by inventory. Verify all prior behavior/ownership and request only new subjective presentation assessment.

## Tasks
- T1 / S1-S6 / inline + independent design / completed / ME067 evaluation, exact original asset bundle and approved bounded art contract before presentation.
- T2 / S2-S6 / inline / in progress / ME068 public-only art integration, semantic source regressions RED/GREEN and native evidence.
- T3 / S1-S6 / independent verification / pending / full model/renderer regressions, original/Git hashes, protected parity and objective closure.

## Log
L1. Owner request verbatim:
Una consideración adicional para H3, nuestro escenario de RPG inspirado en Pokémon.

Antes de implementar la presentación gráfica, necesitamos seleccionar recursos adecuados para los dos contextos principales del juego:

1. **Exploración:** mapas, tilesets, personajes, NPCs, interiores y objetos interactivos.
2. **Combate:** sprites de criaturas, fondos de batalla, indicadores de estado, interfaz de selección de acciones y efectos visuales.

Te propongo investigar estos paquetes como candidatos:

**Exploración:**

- [https://kenney.nl/assets/tiny-town](https://kenney.nl/assets/tiny-town)
- [https://kenney.nl/assets/tiny-dungeon](https://kenney.nl/assets/tiny-dungeon)
- [https://kenney.nl/assets/rpg-urban-pack](https://kenney.nl/assets/rpg-urban-pack)

**Combate:**

- [https://opengameart.org/content/main-art-from-monster-rpg-2](https://opengameart.org/content/main-art-from-monster-rpg-2)
- [https://opengameart.org/content/cartoon-enemies](https://opengameart.org/content/cartoon-enemies)
- [https://opengameart.org/content/stalagmite-monster](https://opengameart.org/content/stalagmite-monster)

Evalúa disponibilidad, licencias, estilo visual, resolución, animaciones y compatibilidad de los recursos. Puedes seleccionar otros paquetes CC0 si encuentras alternativas mejores.

No es obligatorio utilizar todos estos recursos. Prefiero pocos assets visualmente coherentes a mezclar muchos paquetes incompatibles.

**Consideraciones arquitectónicas:**

- El motor no debe conocer conceptos específicos de Pokémon, criaturas capturables o reglas de combate.
- La exploración y el combate deben poder presentar contenido visual muy diferente utilizando las capacidades públicas del motor.
- Las criaturas pueden tener representaciones diferentes para exploración y combate.
- Las interfaces de batalla deben construirse mediante capacidades generales de UI, ampliándolas solo cuando exista una necesidad demostrada.
- Evita repetir los problemas de composición e interpretación de tilesets detectados en Courtyard Key.
- Distingue siempre entre recursos gráficos, reglas del videojuego y capacidades reutilizables de MicroEngine.

Incorpora la selección de assets y su documentación al horizonte H3 antes de construir los escenarios correspondientes.

No amplíes el alcance técnico del RPG simplemente por disponer de más recursos gráficos.

Continúa autónomamente con el roadmap y las validaciones establecidas. Solicita mi intervención únicamente cuando exista una experiencia jugable que requiera evaluación subjetiva o un bloqueo estratégico legítimo.

L2. ExistingRPG1 is functional atmain1f38a29, not erased; owner art directive now precedes replacement presentation. Preserved.gitignore/.codegraph, no authoritative memoryidentity so mirrorpending/no memorycalls. RDD explicitlyoff. Read-only asset research delegated for6sources afterbudget, parentwritesinline(no writertrigger). Officialpublicdownloads authorized, tempinspection only. Initial researcher confused packed/unpackedspacing and assetrelease/licenseversion; parent verified actualdimensions/XML/license and obtained Urban officialZIP, targeted factual correction requested. Never turn unverified animation/style/CC0 claims into guarantees. Existinganimatedhero preserved, no animation coverage reduction. Newthree-sheet bundle changes texturecount4->3 legitimately; retain strict complete-bundle cache/lifetime assertions and all prior model/clock/route tests.

L3. Independent checkpoint_design_review PASS; parentapprovedRPG-ART1 beforepresentation, interiorcell assertionadvisoryadopted. All264packedtiles comparedpixelwise tooriginalindividualPNGs proving16stride; sevenselectedoriginalhashesPASS. TinyTown/Dungeonrelease1.1/1.0 bothCC0-1.0, metadata1gapreferstounpackedonly. Research targetedcorrectionacknowledged. Source/TGA assumption correctedactualMonsterRPG2combatZIPPNG/XML; UrbanZIPactual486individualPNGverified. Sevenoriginals importedimmutable, authoredHeroNOTICE distinctfromoriginallicense. T1rollbackonlynewassetbundle/selectionpacket/validator; no runtimechangeatthiscommit.

L4. T1 selection committed8a2dd0a. T2 new terrain regression RED observed, then21 RPG cases GREEN; native original terrain/hero/monster pixels, three-sheet lifetime and all CLI7/outcomes PASS. Source rules/Core unchanged. Three new art captures preserve historical RPG1 captures. T3 independent frozen-commit verification next; Engram mirror remains pending because runtime identity is unavailable.
