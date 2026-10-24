# MicroEngine three-game completion mission

Objective: evolve MicroEngine and deliver Meadow Run, Courtyard Key, and Trail Duel as small, complete, integrated reference games.
Branch: work-unit feature branches; local main integration after objective verification. Delivery strategy: exception-ok because this multi-horizon mission is intentionally larger than one review slice; each behavior remains a separate Conventional Commit and remote publication remains subject to the explicit remote-authorization contract.
Test runner: commands from `docs/engineering/contributing.md`, including managed suites, native smoke wrappers, asset verification, and public-boundary probes applicable to each work unit.
Engram mirror: pending because the Engram MCP tools are not exposed in this session; local file is authoritative until synchronization becomes available.

## Specs

S1. Make the user's autonomous mission the current operational direction: agents own roadmap refinement, specifications, ADRs, implementation, objective verification, ordinary local integration, and selection of the next useful increment without routine approval prompts. Preserve safety, quality controls, subjective product acceptance, destructive-operation limits, and the stricter remote-authorization contract.
S2. Complete Meadow Run as a coherent platformer campaign with approximately three genuinely different levels, progression and difficulty, varied terrain and obstacles, simple enemies, collectibles and an additional integrated mechanic, animation and visual effects, music and sound effects, HUD/menus/pause/retry/restart, and victory/defeat/campaign completion.
S3. Complete Courtyard Key as a small top-down adventure with connected and visually differentiated locations, NPCs/dialogue/interactions, items/inventory/use, chained objectives, fitting obstacles/enemies/combat, animation/audio/effects, UI/menus, world progression, and completion. Apply the demonstrated tileset, multicell-region, layer, depth, and collision-separation lessons.
S4. Complete Trail Duel as a small coherent RPG with explorable locations, NPCs/dialogue, combatants distinct from the explorer, a selected team/companion model, statistics/skills/turn combat, menus/inventory/progression, differentiated exploration and combat presentation, animation/effects/audio/music, persistence when justified, and a completed adventure.
S5. Evolve only demonstrably useful general-purpose engine infrastructure across the three consumers. Candidate areas include audio, animation, particles, UI/navigation, cameras/transforms, maps/tiles/layers, states/transitions, content/resources, collision/spatial behavior, persistence, diagnostics, and advanced rendering. Preserve ADR-0004/NFR-005 and ADR-0012/NFR-006: implementation-independent replaceable providers, cohesive opt-in contracts, no genre-owned Core rules, no speculative backend or abstraction.
S6. Apply Asset Readiness before content-dependent work: inventory existing/gaps, select free compatible sources, preserve provenance/licenses, inspect formats/dimensions/animation/composition, document technical limits, and evaluate contextual visual/sonic coherence.
S7. Verify every work unit through observable public behavior, regression tests, native checks, asset/composition inspection, integration checks, and independent verification where risk requires it. Keep subjective uncertainties for integrated human evaluation unless they genuinely block further technical work.
S8. Maintain traceable work-unit commits and stable local main. Preserve unrelated work, never force-push or rewrite shared history, and never expose credentials. Remote operations require explicit destination, operation, and credential/session authorization even when repository policy otherwise permits ordinary pushes.
S9. Mission completion requires all three games to be playable and integrated with MicroEngine, principal flows verified, shared infrastructure coherent and genre-independent, documentation/tests current, ordinary authorized delivery synchronized, and a combined human-review handoff prepared. Passing unit tests or displaying one screen is insufficient.
S10. “Integrated” currently means three independently runnable, repository-coordinated consumers integrated with the same general-purpose MicroEngine contracts. A unified launcher is not required and must not be introduced without concrete consumer evidence.

## Tasks

- [ ] T1 — S1, S5, S7-S10 — Route: inline (no writer trigger). Status: in-review. Reconcile governance, roadmap, backlog, execution, current-state, and contributor entry documentation with this mission; claim the first bounded work unit. Commit: pending.
- [ ] T2 — S2, S6-S8 — Route: inline (no writer trigger). Reconcile MR-6 objective closure with the new mission authority, run proportional regression/native/audio evidence, independently verify if required, integrate verified local work, and define the next Meadow campaign slice. Commit: pending.
- [ ] T3 — S2, S5-S8 — Route: inline by default; delegate only if a mandatory trigger fires. Deliver Meadow Run campaign completion through bounded vertical slices and integrated review. Commit: pending.
- [ ] T4 — S3, S5-S8 — Route: inline by default; delegate only if a mandatory trigger fires. Deliver Courtyard Key adventure completion through bounded vertical slices and integrated review. Commit: pending.
- [ ] T5 — S4-S8 — Route: inline by default; delegate only if a mandatory trigger fires. Deliver Trail Duel RPG completion through bounded vertical slices and integrated review. Commit: pending.
- [ ] T6 — S7-S10 — Route: inline by default; independent verification required for high-risk/shared-contract changes. Run cross-game hardening, documentation synchronization, combined verification, authorized delivery reconciliation, and prepare the integrated human evaluation. Commit: pending.

## Log

### L2 — 2026-10-24 — T1 verification

Governance reconciliation changed only the 11 authorized documentation surfaces. Author checks:
`git diff --check` PASS; local Markdown links PASS across all 11 files; mission structure PASS.
Independent verification against baseline `1c61799` returned no findings and PASS for S1, S5,
S7, S8, S9 and S10. Runtime suites were N/A because no product source, tests, assets,
dependencies or executable configuration changed. Engram mirror remains pending because its MCP
tools are unavailable in this session.

### L1 — 2026-10-24 — Original user request (verbatim)

# MicroEngine — Misión de desarrollo autónomo

## 1. Objetivo y modelo de responsabilidad

A partir de este momento, asumes la responsabilidad técnica y operativa integral de MicroEngine.

MicroEngine es un motor 2D generalista, modular y extensible, desarrollado mediante un experimento AI-first. El objetivo es comprobar hasta qué punto un agente autónomo puede diseñar, implementar, verificar, mantener y evolucionar un proyecto de software complejo con una intervención humana mínima.

**Tu misión principal es desarrollar y finalizar tres videojuegos pequeños pero completos utilizando MicroEngine:**

1. **Meadow Run:** plataformas inspirado en Super Mario.
2. **Courtyard Key:** aventura cenital inspirada en The Legend of Zelda.
3. **Trail Duel:** RPG inspirado en Pokémon y los RPG clásicos por turnos.

Estos juegos no son el producto principal: son consumidores de referencia utilizados para demostrar, exigir y validar las capacidades generales del motor.

No debes limitarte a satisfacer los tres escenarios técnicos actuales. Esos escenarios constituyen el punto de partida.

La misión se considera completada cuando los tres minijuegos sean experiencias integradas, funcionales y razonablemente terminadas, y cuando MicroEngine proporcione una infraestructura general adecuada para desarrollarlos sin reconstruir continuamente los mismos mecanismos técnicos.

## 2. Autoridad y autonomía

Actúas como propietario técnico y operativo del repositorio.

Tienes autoridad permanente para:

- Investigar y tomar decisiones técnicas.
- Definir, modificar y ejecutar el roadmap.
- Crear y refinar tareas, especificaciones y ADR.
- Diseñar y modificar contratos públicos del motor.
- Implementar y refactorizar sistemas.
- Investigar, descargar, verificar e incorporar recursos gráficos y sonoros gratuitos con licencias compatibles.
- Ejecutar pruebas, inspecciones y revisiones independientes.
- Corregir errores y mejorar implementaciones.
- Crear ramas, hacer commits y resolver conflictos.
- Integrar cambios verificados en `main`.
- Realizar pushes ordinarios al remoto autorizado.
- Documentar y mantener actualizado el proyecto.
- Continuar automáticamente con el siguiente trabajo necesario.

No necesitas autorización humana individual para realizar estas operaciones ordinarias.

Mantén la integridad del repositorio: no fuerces pushes, no destruyas trabajo no preservado, no expongas credenciales ni eludas protecciones externas. Respeta los límites reales de permisos y del entorno.

**No confundas autonomía con ausencia de controles de calidad. Eres responsable de aplicar esos controles sin trasladarme su ejecución.**

## 3. Unidad de delegación: la misión completa

La unidad de trabajo que te delego no es un ticket, una funcionalidad ni un incremento.

**La unidad de trabajo es completar los tres videojuegos y evolucionar MicroEngine para soportarlos.**

Puedes dividir el trabajo internamente en tantos incrementos como necesites.

Puedes usar TDD, hitos, ramas, verificaciones independientes, checkpoints, revisiones de arquitectura y commits pequeños.

Sin embargo, esos mecanismos son herramientas de ingeniería internas, no puntos obligatorios de intervención humana.

Cuando completes un incremento:

1. Verifica su funcionamiento.
2. Revisa su calidad.
3. Corrige los defectos encontrados.
4. Documenta lo necesario.
5. Integra y publica el trabajo verificado.
6. Determina el siguiente incremento necesario.
7. Continúa ejecutando.

No te detengas simplemente porque terminaste un ticket, se agotaron las tareas marcadas como *ready*, falta refinar el siguiente horizonte o todavía no existe una especificación técnica.

**Preparar la siguiente tarea también es tu responsabilidad.**

El roadmap es un instrumento para ejecutar la misión, no una lista de posibilidades que permanecen bloqueadas hasta recibir nuevas órdenes.

## 4. Alcance de los tres videojuegos

Desarrolla los juegos hasta convertirlos en minijuegos completos, no en demostraciones técnicas mínimas.

### Meadow Run — Plataformas

Debe ofrecer una pequeña campaña con aproximadamente tres niveles auténticamente diferentes.

Considera, entre otras capacidades:

- Progresión de niveles y dificultad.
- Diseño variado de terreno y obstáculos.
- Enemigos con comportamientos sencillos.
- Coleccionables y alguna mecánica adicional de juego.
- Animaciones y efectos visuales.
- Música y efectos sonoros.
- HUD, menús, pausa, reintento y reinicio.
- Victoria, derrota y finalización de campaña.

No consideres que dos niveles son distintos únicamente porque poseen identificadores diferentes.

### Courtyard Key — Aventura cenital

Debe evolucionar hacia una pequeña aventura con exploración, objetivos y progresión.

Considera:

- Varias localizaciones conectadas, con mapas visualmente diferenciados.
- NPCs, diálogos e interacciones.
- Objetos, inventario y uso de elementos.
- Misiones o pequeños objetivos encadenados.
- Obstáculos, enemigos y combate sencillo si encaja en el diseño.
- Animaciones, audio y efectos visuales.
- Interfaz y menús.
- Progresión del mundo y condiciones de finalización.

Utiliza lo aprendido sobre composición de tilesets, regiones multicelda, capas, profundidad visual y separación de colisiones.

### Trail Duel — RPG por turnos

Debe evolucionar hacia un pequeño RPG que combine exploración y combate.

Considera:

- Mundo explorable con distintas localizaciones.
- NPCs y diálogos.
- Criaturas o combatientes diferenciados del protagonista explorador.
- Equipos o compañeros, según el diseño elegido.
- Estadísticas, habilidades y combate por turnos.
- Menús, inventario y progresión.
- Presentación visual diferenciada entre mundo y combate.
- Animaciones, efectos, audio y música.
- Persistencia cuando resulte pertinente.
- Victoria y cierre de una pequeña aventura.

No necesitas reproducir todas las mecánicas de Pokémon, pero sí desarrollar una experiencia RPG coherente, no simplemente un menú de ataque y defensa.

### Criterio común

Tienes libertad para ajustar las mecánicas, el número de escenarios y el contenido exacto según el esfuerzo real y el valor de validación.

No persigas la extensión de un videojuego comercial. Busca tres experiencias pequeñas, reconocibles, integradas y completas.

**Una mecánica superficial añadida para marcar una casilla no aporta tanto valor como una funcionalidad bien integrada.**

## 5. Evolución del motor

Esta misión también debe impulsar activamente las capacidades generales de MicroEngine.

No te limites a resolver únicamente lo que exige el siguiente incremento de un videojuego.

Evalúa las necesidades del conjunto de los tres consumidores y diseña una arquitectura que facilite desarrollarlos y mantenerlos.

Entre las áreas que probablemente debas investigar se encuentran:

- Audio, reproducción, música y mezcla.
- Animación por fotogramas y por propiedades.
- Partículas y efectos visuales.
- Interfaces de usuario y navegación.
- Cámaras y transformaciones.
- Mapas, tilesets, capas y composición.
- Escenas, estados y transiciones.
- Recursos y gestión de contenido.
- Colisiones y comportamiento espacial.
- Persistencia.
- Herramientas de desarrollo y diagnóstico.
- Renderizado avanzado cuando exista necesidad concreta.

Esta lista no exige construir inmediatamente todos esos sistemas.

Pero tampoco autoriza descartarlos sistemáticamente por el argumento de que una demo individual todavía no los necesita.

### Criterio de incorporación

Una capacidad general puede justificarse por:

1. Una necesidad concreta de uno de los videojuegos.
2. Una necesidad compartida identificable al planificar varios consumidores.
3. Una limitación verificable de ergonomía o composición.
4. Infraestructura técnica que claramente corresponde al motor y no a las reglas particulares del juego.
5. Una necesidad arquitectónica para mantener extensibilidad y evitar acoplamiento indebido.

No es obligatorio esperar a que exista código duplicado en varios juegos para reconocer una abstracción razonable.

Al mismo tiempo, no construyas subsistemas gigantescos por anticipación.

**Diseña con visión transversal; implementa con alcance proporcionado.**

El motor proporciona mecanismos generales. Los juegos conservan sus reglas, contenido y decisiones de diseño.

## 6. Investigación y gestión autónoma de assets

Eres responsable de conseguir y preparar el contenido necesario para los tres videojuegos.

Antes de implementar una funcionalidad con requisitos artísticos o sonoros:

- Investiga los recursos existentes.
- Identifica los recursos faltantes.
- Busca alternativas gratuitas y legalmente reutilizables.
- Comprueba las licencias y procedencia.
- Inspecciona formatos, dimensiones, animaciones y composición.
- Evalúa coherencia visual y sonora.
- Descarga e integra los archivos seleccionados.
- Conserva documentación y verificaciones relevantes.

Puedes utilizar Kenney, OpenGameArt y otros repositorios confiables de assets.

No necesitas mi aprobación para seleccionar recursos gratuitos adecuados.

No utilices contenido comercial protegido ni dependencias de pago sin autorización.

Las partículas y otros efectos pueden construirse mediante las capacidades del motor cuando resulte más adecuado que buscar imágenes externas.

## 7. Validación autónoma

Debes ejecutar y verificar tu propio trabajo.

Utiliza pruebas automatizadas, pruebas nativas, capturas, escenarios reproducibles y cualquier herramienta de inspección disponible.

Evalúa tanto los contratos técnicos como el resultado observable.

Una captura correctamente renderizada no garantiza una buena composición artística.

Un conjunto de pruebas aprobadas no garantiza una experiencia jugable satisfactoria.

Por tanto, combina:

- Verificación funcional.
- Pruebas de regresión.
- Inspección de recursos y composición.
- Revisión técnica independiente.
- Pruebas de integración entre capacidades.
- Evaluación visual y jugable preliminar dentro de tus posibilidades.

Corrige autónomamente los problemas detectados.

Cuando una evaluación dependa de sensaciones humanas que no puedas determinar con fiabilidad, registra esa incertidumbre y continúa con el resto del trabajo que no dependa de ella.

No conviertas cada incertidumbre subjetiva en una interrupción.

## 8. Gestión del repositorio

Mantén `main` como referencia estable del desarrollo validado.

Puedes usar ramas para aislar trabajo, pero no acumules funcionalidades finalizadas indefinidamente en ellas.

Integra oportunamente los cambios que superen las verificaciones y sincroniza el remoto autorizado.

Realiza commits durante el desarrollo y conserva trazabilidad.

Gestiona el trabajo pendiente sin depender de que yo te recuerde hacer merges o pushes.

No reescribas historia compartida, no fuerces pushes ni elimines trabajo único no preservado.

Si una operación está realmente bloqueada por permisos, protecciones o restricciones de herramientas, documenta el impedimento y conserva el estado recuperable.

## 9. Relación con el responsable humano

Mi función es definir la visión general y evaluar ocasionalmente el producto.

No soy el administrador diario del backlog, el revisor obligatorio de cada commit ni el responsable de recordarte que debes continuar.

Puedes informar avances sin solicitar aprobación.

No debes detenerte para preguntarme si:

- Funcionó el menú.
- Se puede pausar.
- Suena correctamente un efecto.
- Pasan las pruebas.
- Debes continuar con otro nivel.
- Puedes implementar el siguiente subsistema.
- Debes integrar cambios verificados.
- Puedes seleccionar un asset adecuado.

Resuelve esos asuntos autónomamente.

### Intervención humana justificada

Solicita mi intervención cuando:

1. Exista un bloqueo real que no puedas resolver con las herramientas disponibles.
2. Necesites una decisión estratégica que cambie sustancialmente la visión del proyecto.
3. Una operación exceda tus permisos reales o implique costes, riesgos externos o destrucción de información.
4. Los tres videojuegos estén listos para una evaluación humana integrada, o exista un hito de experiencia verdaderamente significativo cuya validación resulte indispensable para continuar.

Si necesito revisar un videojuego, prefiero recibir una experiencia completa y comunicarte diez problemas juntos, en lugar de realizar diez revisiones para descubrirlos uno por uno.

Puedes mantener listas internas de incertidumbres visuales o de jugabilidad hasta la revisión integrada.

## 10. Continuidad y limitaciones operativas

Trabaja autónomamente todo lo que permita tu entorno de ejecución.

No finalices voluntariamente porque completaste el siguiente pequeño incremento.

Si un límite real de contexto, sesión, herramientas, presupuesto o tiempo impide continuar, deja un checkpoint preciso que incluya:

- Estado de los tres videojuegos.
- Estado de MicroEngine.
- Trabajo integrado y publicado.
- Trabajo pendiente o no integrado.
- Verificaciones realizadas.
- Problemas conocidos.
- Próxima acción concreta para reanudar la misión.

Distingue claramente una interrupción impuesta por el entorno de una decisión de detenerte.

No afirmes que seguirás ejecutándote en segundo plano si el entorno no proporciona esa capacidad.

## 11. Persistencia de la delegación

Revisa `AGENTS.md`, las políticas de autonomía, el roadmap y los demás documentos de ingeniería.

Actualízalos para que esta directriz sea la referencia operativa del proyecto, preservando las restricciones de seguridad y las instrucciones de mayor prioridad.

Elimina contradicciones que te obliguen a detenerte artificialmente tras cada incremento o que condicionen commits, merges y pushes ordinarios a autorizaciones humanas que ya han sido delegadas.

No interpretes este cambio como permiso para omitir especificaciones, pruebas, revisiones o controles.

**La intención es eliminar aprobaciones innecesarias, no eliminar disciplina de ingeniería.**

## 12. Criterios de finalización

La misión termina cuando:

- Los tres minijuegos sean jugables y estén integrados.
- Sus principales funcionalidades y flujos hayan sido verificados.
- MicroEngine soporte sus necesidades generales mediante una arquitectura coherente.
- Las capacidades compartidas no dependan innecesariamente de un género particular.
- La documentación y las pruebas estén actualizadas.
- El trabajo esté integrado y sincronizado con el remoto autorizado.
- El resultado pueda presentarse para una revisión humana conjunta.

No declares completada esta misión simplemente porque sus pruebas unitarias pasan o porque cada videojuego tiene una pantalla funcional.

**Tu entrega es el conjunto: MicroEngine y tres videojuegos pequeños pero completos.**

## 13. Instrucción de ejecución

Comienza inspeccionando el estado real del repositorio, las ramas, los avances ya implementados y las políticas vigentes.

Adapta el roadmap para cubrir la misión completa.

Identifica dependencias compartidas y organiza el orden de trabajo que consideres más eficaz. Puedes alternar entre los tres juegos si eso permite validar mejor las capacidades generales.

No tienes obligación de finalizar Meadow Run antes de tocar Courtyard Key o Trail Duel.

Realiza la planificación necesaria y comienza inmediatamente su ejecución.

Continúa investigando, implementando, probando, revisando, integrando y publicando cambios ordinarios de forma autónoma hasta completar la misión o alcanzar una limitación operativa real.

**Eres responsable de sacar MicroEngine adelante. No me entregues una sucesión de tareas pequeñas para administrarlas: entrégame el resultado de la misión.**
