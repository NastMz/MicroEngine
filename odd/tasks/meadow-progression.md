# Meadow Run phase 2 — content-first progression
Baseline8976f97; feat/meadow-progression. Local coherent commits; no push/main until applicable owner authority. Cached stacked-to-main strategy, no PR.
Forecast~450 authored lines, two coherent units (process/design then playable), heuristic only. Existing serial dotnet suites and PlatformerNativeSmoke.

## Specs
S1. "Vamos a comenzar la segunda fase de MicroEngine: **validar que el motor permite desarrollar minijuegos más completos**, no únicamente demos técnicas aisladas." Refine only next executable Meadow Run increment, preserve accepted demos.
S2. "Prepara un roadmap incremental para evolucionar Meadow Run hacia un minijuego con:" and "Estas son capacidades objetivo que deben refinarse según su valor. No tienes que implementarlas simultáneamente ni agregar mecánicas artificiales solo para completar una lista." Stage2-3levels/progression, enemies, optional items/powers, feedback/animation, audio/music, HUD/menu/pause/restart, win/loss/continue without simultaneous implementation.
S3. "Antes de implementar cada incremento, inventaría los recursos que necesita y compáralos con los ya existentes." / "Prioriza la coherencia visual y sonora con los recursos existentes de Meadow Run." Establish lightweight Asset Readiness; current first slice reuses verified originals, no content gap.
S4. "No crees sistemas genéricos gigantescos de forma anticipada, pero tampoco obligues al juego a reconstruir infraestructura técnica que debería proporcionar MicroEngine." Keep level geometry/progression game-owned; reuse TwoD/Loop/Desktop/images/animation; new engine capability only on demonstrated reusable need.
S5. "Incorpora al procedimiento de ingeniería un paso ligero de Asset Readiness para tareas con contenido visual o sonoro." Readiness lists existing/missing/selected/licensing/formats/limits plus contextual composition validation where relevant; N/A for non-content tasks.
S6. "Empieza por planificar el siguiente horizonte, evaluar los recursos disponibles y preparar el primer incremento ejecutable. Continúa autónomamente con las tareas justificadas hasta que exista una experiencia que realmente necesite mi evaluación." Execute reviewed two-level consumer progression then independent proof; stop at meaningful new playable progression, not ordinary asset/technical approval.

## Tasks
- T1 / S1-S6 / inline + read-only map and design review / in progress / ME071 process/roadmap/readiness/spec.
- T2 / S1-S4,S6 / inline / pending / ME072 two-level campaign keeping original level1 API and replay coverage.
- T3 / S1-S6 / independent final verification / pending / ME073 native progression/pixel/lease/CLI and protected regressions, subjective handoff.

## Log
L1. Owner request verbatim follows:
He revisado y acepto la evaluación transversal de los tres escenarios técnicos y la finalización de LAUNCH-1.

Vamos a comenzar la segunda fase de MicroEngine: **validar que el motor permite desarrollar minijuegos más completos**, no únicamente demos técnicas aisladas.

Nuestro primer objetivo será evolucionar Meadow Run hacia un pequeño plataformas con 2–3 niveles, enemigos, progresión y suficientes mecánicas para poner a prueba la madurez del motor.

Quiero incorporar además una regla permanente a nuestro proceso AI-first: **planificación y preparación de assets antes de cada incremento que los necesite**.

### 1. Dirección del siguiente horizonte

Prepara un roadmap incremental para evolucionar Meadow Run hacia un minijuego con:

- Varios niveles y progresión.
- Enemigos y comportamientos sencillos.
- Objetos y posibles poderes.
- Animaciones y feedback visual.
- Sonidos y música.
- HUD, menú, pausa y reinicio.
- Condiciones de victoria, derrota y continuación.

Estas son capacidades objetivo que deben refinarse según su valor. No tienes que implementarlas simultáneamente ni agregar mecánicas artificiales solo para completar una lista.

### 2. Investigación y preparación de recursos

Antes de implementar cada incremento, inventaría los recursos que necesita y compáralos con los ya existentes.

Investiga y selecciona los assets que falten, incluyendo sprites, animaciones, enemigos, fondos, UI, sonidos, música y efectos visuales.

Como fuentes iniciales puedes consultar:

- https://kenney.nl/assets/
- https://opengameart.org/
- https://itch.io/game-assets/free

Y como candidatos concretos de audio:

- https://kenney.nl/assets/interface-sounds
- https://kenney.nl/assets/impact-sounds
- https://opengameart.org/content/8-bit-platformer-sfx-0
- https://opengameart.org/content/platformer-chiptunes

Prioriza recursos CC0 o con permisos claros y compatibles con nuestro uso. Verifica las licencias individuales, conserva procedencia y registros de integridad cuando corresponda.

No necesitas mi aprobación para descargar e incorporar recursos públicos gratuitos cuya licencia haya sido verificada.

Prioriza la coherencia visual y sonora con los recursos existentes de Meadow Run. No mezcles paquetes arbitrariamente ni agregues contenido que no tenga una función concreta.

### 3. Efectos, partículas y animaciones

Distingue entre los recursos artísticos y la infraestructura necesaria para utilizarlos.

Por ejemplo, un efecto de polvo puede necesitar sprites o primitivas, pero el mecanismo de emisión y actualización de partículas podría convertirse en una capacidad general del motor.

Lo mismo ocurre con audio, animaciones, transiciones y efectos visuales.

No crees sistemas genéricos gigantescos de forma anticipada, pero tampoco obligues al juego a reconstruir infraestructura técnica que debería proporcionar MicroEngine.

Evalúa cada necesidad en función de su reutilización, extensibilidad y experiencia de consumo.

### 4. Preparación obligatoria de contenido

Incorpora al procedimiento de ingeniería un paso ligero de Asset Readiness para tareas con contenido visual o sonoro.

Debe permitir identificar los recursos existentes, los faltantes, los seleccionados, sus licencias, sus formatos y las limitaciones técnicas descubiertas.

No debe convertirse en una ceremonia burocrática ni bloquear tareas que no necesitan recursos externos.

Incluye, cuando sea apropiado, una validación de composición visual y sonora: no basta con que los archivos carguen; deben utilizarse correctamente y resultar coherentes en el contexto del juego.

### 5. Autonomía de desarrollo

Continúa bajo nuestro modelo AI-first.

Tienes autoridad para investigar, seleccionar recursos, diseñar, especificar, tomar decisiones técnicas, implementar, probar, revisar, documentar y cerrar tareas.

Utiliza el roadmap como guía y mantén siempre la separación entre capacidades generales del motor y reglas particulares del videojuego.

No necesitas esperar aprobación por cada decisión, paquete de recursos o incremento técnico verificable.

Realiza commits trazables, preserva el trabajo ajeno y respeta las reglas vigentes sobre integración y publicación.

Solicita mi intervención cuando exista un avance jugable que requiera evaluación subjetiva significativa o un bloqueo estratégico legítimo.

### 6. Objetivo arquitectónico

La finalidad no es simplemente convertir Meadow Run en un videojuego más bonito.

Queremos descubrir qué tan sencillo es construir un plataformas más completo utilizando MicroEngine.

Registra qué infraestructura general se reutiliza, qué capacidades nuevas se justifican y qué código sigue perteneciendo exclusivamente al juego.

Después utilizaremos esos aprendizajes para madurar los escenarios de aventura y RPG, sin convertir los géneros en dependencias del núcleo.

**Empieza por planificar el siguiente horizonte, evaluar los recursos disponibles y preparar el primer incremento ejecutable. Continúa autónomamente con las tareas justificadas hasta que exista una experiencia que realmente necesite mi evaluación.**

L2. Root8976f97; unrelated.gitignore/.codegraph preserved. Memory mirror pending runtimeidentity unavailable; no calls. RDDoff. Mapping budget delegated meadow_phase2_map before source exploration; parent LevelGame/renderer spotcheck confirms static singlecourse coupling. No writer trigger: parent inline. Firstcontent uses existing originalCC0 art only. Publicaudio pages investigated as future shortlist, no download/listening/codec support or subjective coherence claimed.

L3. Independent MR-2 designreview found blockers: exact campaign API/terminaltiming, avoidnullconstructorambiguity, validateIDbeforeworldconstruction, proveleasecountwithmanagedcanvasratherthanclaimnativevisibility. Root tightened spec; animationinstance reset and2000updatelevel2bound explicit. No implementation yet; targeted recheck only beforeapproval.

L4. DesignPASS after targeted correction, rootapprovesMR-2 beforecode. ME071complete/ME072claimed root8976f97. Contentreadyexistingoriginals; testproject may link renderer/assets to prove leasecountandanimationresetwithoutnativeprivileges. First meaningful gate is two-level playability; no added enemy/audio/powers work.
