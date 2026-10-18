# Courtyard Key licensed adventure art
Baseline 0a6e95f; branch feat/adventure-art; local work-unit commits only, no push/main.
Delivery: existing stacked-to-main preference retained for future authorized delivery; no PR now. Forecast ~300 authored lines; runner dotnet test and AdventureNativeSmoke.ps1.

## Specs
S1. "utilizar assets reales, gratuitos y con licencias verificables"; evaluate the three owner links, preserve source/license evidence and original selected-file hashes. No legal guarantee inferred from a CC0 label.
S2. "No reinicies ni descartes trabajo funcional que ya hayas realizado. Integra los recursos en el horizonte correspondiente, usando exclusivamente las APIs públicas del motor." Preserve AdventureModel, engine and prior consumer bytes; presentation-only content update with public decoder/cache/regions/animation.
S3. "No quiero que el Core conozca paquetes concretos ni que se introduzcan dependencias del estilo gráfico de Zelda." Sheet names/regions/direction policy remain game content, no engine or dependency changes.
S4. "La incorporación de sprites debe servir también para validar las capacidades generales de carga de recursos, atlas, animaciones, escalado y renderizado 2D cuando sean necesarias." Test actual selected PNGs, source bounds, directional frames and scale, movement/reset clock, cache/lifetime, unchanged public gameplay course and CLI. Independent review before technical closure; subjective presentation remains owner-owned.

## Tasks
- T1 / S1-S4 / inline / completed / asset assessment and bounded presentation contract; no meaningful runnable RED for provenance docs.
- T2 / S2-S4 / inline / completed / new presentation expectations RED then implementation GREEN and native proof.
- T3 / S1-S4 / independent verification / in progress / fresh isolated verification, evidence and local closure commit.

## Log
L1. Owner request (verbatim, including truncated final word):
Una consideración adicional para el escenario de aventura cenital inspirado en Zelda.
En el plataformas anterior utilizamos un paquete gráfico de Kenney. Para este nuevo escenario quiero seguir la misma estrategia: utilizar assets reales, gratuitos y con licencias verificables, en lugar de construir toda la presentación mediante figuras provisionales.
He identificado este paquete como primera opción:
[https://opengameart.org/content/zelda-like-tilesets-and-sprites](https://opengameart.org/content/zelda-like-tilesets-and-sprites)
Es un paquete de ArMM1998 publicado bajo CC0, con tiles de exteriores, interiores, cuevas, personajes, animaciones y objetos.
Evalúa si resulta adecuado para las necesidades actuales del juego. Como alternativas puedes considerar:
[https://ansimuz.itch.io/legend-of-faune](https://ansimuz.itch.io/legend-of-faune)
[https://kenney-assets.itch.io/scribble-dungeons](https://kenney-assets.itch.io/scribble-dungeons)
Selecciona los recursos apropiados, verifica su licencia y registra su procedencia.
No reinicies ni descartes trabajo funcional que ya hayas realizado. Integra los recursos en el horizonte correspondiente, usando exclusivamente las APIs públicas del motor.
No quiero que el Core conozca paquetes concretos ni que se introduzcan dependencias del estilo gráfico de Zelda.
La incorporación de sprites debe servir también para validar las capacidades generales de carga de recursos, atlas, animaciones, escalado y renderizado 2D cuando sean necesarias.
Mantén

L2. Mapping delegated to existing read-only explorer after bounded evidence budget; parent writes inline (no writer trigger). Memory mirror pending: authoritative runtime identity unavailable, no memory calls permitted. Preserve unrelated .gitignore/.codegraph. RDD remains previously explicitly disabled; ordinary independent verification retained. No new architecture/API decision: existing ADR0004/0012 apply.

L3. New presentation tests RED3/3 against baseline; GREEN12/12 after changes. Native route460/reset/replay/sevenimages/HUD/gate sourceRGB/2x original character pixel comparison and7productionCLIcasesPASS. Asset hashesPASS. Native outdatedsixmarker and transparentcandidategrass failed honestly before corrections. Independent final verification pending. No meaningful RED for license documentation. Rollback is presentation/assets/its checks+linked docs only, no engine/model changes. RDD explicitly globaloff confirmed elevated; no native RDD started.
