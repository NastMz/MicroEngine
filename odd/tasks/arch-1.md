# ARCH-1: Architectural renaissance — adversarial audit, industry research, target architecture

Objective: Phase I (done) audited every subsystem against the vision of a generalist, modular, multi-platform
2D/2.5D/3D engine, researched other engines and designed the target architecture; the owner approved it on 2026-11-02
with five amendments (`L4`-`L5`). **Phase II (reconstruction) is under way** under the approved plan in
[`docs/evaluations/arch-1.md`](../../docs/evaluations/arch-1.md) §9-§9.1; this file is its persistent register.
Governance (`L6`): the architect directs, reviews and merges; agents implement every unit through a pull request and
never merge their own; no NuGet publication, public or demo release, production site deployment, visibility,
secrets or permission change. Phase I branch `docs/arch-1` from `feat/asset-1` at 5706285; Phase II branches per unit.

## Specs

Quoted fragments are the owner's messages (2026-11-02), verbatim; the full texts are `L1` (request), `L4` (five
adjustments), `L5` (approval) and `L6` (Phase II protocol).

- S1 Adversarial audit: "Realiza una revisión adversarial de absolutamente toda la arquitectura actual." "Si
  diseñáramos MicroEngine desde cero, con nuestra experiencia actual y las lecciones de la industria, ¿volveríamos a
  tomar esta decisión?" "cada crítica y propuesta deberá respaldarse con evidencia técnica."
- S2 Industry research: "Godot." "Unity." "Unreal Engine." "MonoGame." "SDL y SDL_GPU." "Raylib." "Prioriza código
  fuente, documentos de diseño, decisiones arquitectónicas, propuestas técnicas, issues, pull requests, discusiones de
  mantenedores y experiencias documentadas." "diferencia lo que está públicamente documentado de lo que solo puede
  inferirse."
- S3 Subsystems: "Núcleo y ciclo de vida", "Renderizado y gráficos", "Input", "Audio", "Física", "Entidades, escenas y
  mundo", "Assets", "Plataformas", "SDK y herramientas"; "Identifica también subsistemas ausentes".
- S4 Logical and physical architecture: "Propón la estructura física y lógica que corresponda a la arquitectura
  objetivo, incluso si implica reorganizar completamente la solución." "No confundas crear carpetas con solucionar
  problemas de acoplamiento."
- S5 Implicit assumptions: "Busca sistemáticamente decisiones ocultas que el motor toma en nombre del desarrollador."
  "Los defaults plug-and-play siguen siendo un requisito fundamental."
- S6 Substitution: "Determina si las interfaces actuales representan contratos de sustitución reales o simples
  envoltorios de una implementación concreta."
- S7 Validation infrastructure: "Cuestiona especialmente la idoneidad de las comparaciones pixel-perfect como criterio
  universal de validación entre renderizadores y plataformas diferentes."
- S8 Evidence per finding: "1. Qué existe actualmente. 2. Qué problema representa. 3. Qué evidencia lo demuestra.
  4. Qué han aprendido otros proyectos. 5. Qué alternativas existen. 6. Qué solución propones. 7. Qué consecuencias
  tiene para MicroEngine."
- S9 Target architecture and dossier: "La propuesta debe distinguir claramente qué necesitamos implementar ahora, qué
  interfaces deben quedar preparadas y qué capacidades pueden incorporarse después." Dossier contents as listed in
  section 11 of `L1`.
- S10 Gate: "Al concluir esta fase, presenta el diseño para revisión y aprobación. No comiences la reconstrucción
  estructural de la Fase II sin esa aprobación."
- S11 Approved amendments (`L5`): "1. La nueva planificación de P1–P7, ejecutando cada prototipo antes de fijar el
  contrato o tomar la decisión que debe validar." "2. Un contrato gráfico único y completo, con recursos compartidos,
  capacidades explícitas y validación mediante prototipos de nivel 0 y nivel 1." "3. La eliminación obligatoria del
  adaptador heredado de Entity al finalizar la etapa 5, sin incorporarlo como API pública permanente." "4. Los nuevos
  criterios de cierre por etapa y una estrategia de validación basada en propiedades y tolerancias justificadas."
  "5. La definición de finalización de Fase II, incluyendo una prueba vertical 3D real y una demostración de
  sustitución del sistema físico." From `L4`: "Hay que ejecutar cada uno antes de tomar la decisión irreversible que
  pretende validar." "Mi condición: validar la arquitectura del dispositivo antes de estabilizar sus contratos, no
  después de construir dos backends sobre ellos." "No queremos terminar conservando dos modelos de entidades porque
  resultó cómodo durante la reconstrucción." "las tolerancias visuales deben depender de las propiedades evaluadas, no
  convertirse simplemente en referencias más permisivas." Integrated in dossier §6.3-§6.5, §7, §8, §9, §9.1.
- S12 Physics distinction (`L5`): "En la prueba de física, distingue las capacidades comunes entre el motor cinemático
  de tiles y Box2D de las capacidades específicas de cada implementación. No presupongas equivalencia funcional
  completa entre ambos." Dossier §9.1 table and both scenarios.
- S13 Definition of done (`L4`, `L6` §8): "Exigiría al menos una prueba vertical de renderizado 3D antes de afirmar que
  la arquitectura está preparada. No un juego 3D completo, pero sí una implementación que ejerza los contratos nuevos"
  "Continúa coordinando las etapas de la Fase II hasta cumplir los criterios de finalización establecidos en el
  dossier actualizado, incluidas las pruebas verticales 3D y de sustitución de física." "Entrega la reconstrucción
  integrada en `main`, sin publicar una nueva versión." Dossier §9.1.
- S14 PR flow (`L6` §1-§3): "Todas las modificaciones deberán ser realizadas por agentes delegados." "Cada unidad de
  implementación deberá entregarse mediante una PR." The agent must "Implementar su tarea.", "Ejecutar las pruebas
  correspondientes.", "Documentar los cambios.", "Publicar su rama.", "Abrir la PR.", "Corregir los problemas
  encontrados durante la revisión." "Los agentes implementadores no deben fusionar sus propias PRs."
- S15 Merge authorization and its limits (`L6` §4-§5): "tienes autorización expresa para aprobar y fusionar PRs a
  `main` sin solicitar aprobación individual del propietario." "Antes de fusionar, verifica que los cambios cumplen los
  criterios de aceptación correspondientes y que las pruebas relevantes han sido ejecutadas." "Mantén documentadas las
  incompatibilidades temporales, sus responsables y la etapa en la que deberán resolverse." "No omitas verificaciones
  arbitrariamente para acelerar merges." Not authorized: "Publicar nuevas versiones en NuGet." "Crear releases
  públicas." "Publicar nuevas versiones de las demos." "Desplegar versiones públicas del sitio." "Eliminar el
  repositorio o modificar su visibilidad." "Modificar secretos, credenciales o permisos administrativos." "Si los
  workflows habituales activan publicaciones o despliegues automáticamente después de un merge, coordina previamente
  su desactivación o aislamiento mediante agentes, sin comprometer la ejecución del CI de validación."
- S16 Register (`L6` §6): "Mantén un registro persistente y actualizado con:" "Estado de cada etapa." "Decisiones
  arquitectónicas aprobadas." "Contratos vigentes." "Agentes responsables." "Tareas pendientes." "Dependencias." "PRs
  abiertas y fusionadas." "Resultados de validación." "Problemas conocidos." "Bloqueos." "Próximos pasos." "La
  reconstrucción debe poder continuar coherentemente entre sesiones sin depender de recordar conversaciones extensas."
  This file's `## Register`.

## Tasks

Phase I tasks keep their IDs. Phase II IDs: `G` governance, `S<stage>-…` stage units, `P<n>` prototypes. Every Phase
II task is delegated to an agent and delivered as one pull request (S14); "unassigned" means no agent or branch yet.
A stage's tasks are checked against the stage closure criteria in dossier §9.

| ID | Specs | Route | Status | PR | Evidence |
| --- | --- | --- | --- | --- | --- |
| T1 | S1, S3-S6, S8 | delegated: 4 domain auditors (core/SDK, rendering/platform, input/audio/world, infrastructure) | done | — (in the Phase I PR) | annexes A1-A4, experiments in `docs/evaluations/arch-1/experiments/` |
| T2 | S2 | delegated: 3 researchers (rendering, engine core/physics/assets/audio/input, validation/packaging) | done | — (in the Phase I PR) | annexes R1-R3 (230+ sources) |
| T3 | S1-S9 | inline: cross-check, resolve contradictions, write `docs/evaluations/arch-1.md` | done | — (in the Phase I PR) | dossier; D1 and D4 spot-checked against source |
| T4 | S10 | inline: present for approval | done: approved 2026-11-02 with five amendments | — | `L4`, `L5` |
| T5 | S11-S13, S16 | delegated: documentation agent, branch `docs/arch-1-plan` | PR open | see Open PRs | dossier amended (§6.3-§6.5, §7-§11, §9.1), annex notes, this register |
| G1 | S15 | delegated: another agent, branch `fix/phase-ii-deploy-freeze` | in progress | — | freezes automatic production site deployments from `main`; merges first |
| S0-D1 | S11 | delegated (unassigned) | pending | — | D1 `ComposedPlatform` forwarding, with tests |
| S0-D2 | S11 | delegated (unassigned) | pending | — | D2 release-gating package checks red on `main` |
| S0-D3 | S11 | delegated (unassigned) | pending | — | D3 fingerprint passes real regressions; lasting fix is S0-V3 |
| S0-D4 | S11 | delegated (unassigned) | pending | — | D4 headless clock ignores `UpdateRate` |
| S0-D9 | S11 | delegated (unassigned) | pending | — | D9 `AssetCache.Unload` with shared holders; duplicate loads |
| S0-D11 | S11 | delegated (unassigned) | pending | — | D11 smaller defects (list in dossier §3.1) |
| S0-C1 | S11, S15 | delegated (unassigned) | pending | — | validate nightly on `main` |
| S0-C2 | S11 | delegated (unassigned) | pending | — | browser suite `web/tests/run.mjs` in CI (D10) |
| S0-C3 | S11 | delegated (unassigned) | pending | — | SDL keyboard and audio-output tests (D10) |
| S0-V1 | S11 | delegated (unassigned) | pending | — | conformance skeleton with the rules already agreed |
| S0-V2 | S11 | delegated (unassigned) | pending | — | recording device + CPU reference rasterizer |
| S0-V3 | S11 | delegated (unassigned) | pending | — | property-based goldens beside the fingerprint (dossier §8 tolerance policy) |
| P1 | S11 | delegated (unassigned), `experiments/` or draft PR | pending (starts with stage 0) | — | gate: before stage 2a starts |
| P3 | S11 | delegated (unassigned), `experiments/` or draft PR | pending (starts with stage 0) | — | gate: graphics contract frozen (end of stage 1 spike); tier-1 half of S1-2 |
| P4 | S11 | delegated (unassigned), `experiments/` or draft PR | pending (starts with stage 0) | — | gate: platform/device split of stage 1 frozen |
| S1-1 | S11 | delegated (unassigned) | pending | — | kernel, platform split, hosting, one composition path, architecture test |
| S1-2 | S11 | delegated (unassigned) | pending | — | graphics contract spike: tier-0 SDL_Renderer + tier-1 SDL_GPU adapters driving one 2D batcher |
| S2a | S11 | delegated (unassigned) | pending | — | graphics foundation on the frozen contract |
| S2b | S11 | delegated (unassigned) | pending | — | input |
| P5 | S11 | delegated (unassigned), `experiments/` or draft PR | pending | — | gate: `IAudioSink` frozen (before stage 2c closes) |
| S2c | S11 | delegated (unassigned) | pending | — | audio and content |
| P6 | S11, S12 | delegated (unassigned), `experiments/` or draft PR | pending | — | gate: physics contract of stage 3 frozen |
| P7 | S11 | delegated (unassigned), `experiments/` or draft PR | pending | — | gate: `IWorld` contract of stage 3 frozen |
| S3 | S11, S12 | delegated (unassigned) | pending | — | world, physics common contract + capabilities, `Physics.Tiles`, transitional `Entity` adapter |
| S4 | S11 | delegated (unassigned) | pending | — | UI and modules |
| S5 | S11 | delegated (unassigned) | pending | — | consumers migrated; `Entity` adapter deleted; exclusion list empty |
| P2 | S11, S13 | delegated (unassigned), `experiments/` or draft PR | pending | — | gate: tier-1 shader toolchain fixed (inside the 3D vertical slice) |
| S6-1 | S13 | delegated (unassigned) | pending | — | 3D vertical slice on tier-1 SDL_GPU (dossier §9.1 item 2) |
| S6-2 | S12, S13 | delegated (unassigned) | pending | — | Box2D v3 backend and the physics substitution demonstration (§9.1 item 3) |
| S6-3 | S13 | delegated (unassigned) | pending | — | final report, integrated in `main`, no release (§9.1 item 4) |

Route evidence: T1/T2 fire the mapping trigger (the audit spans the whole repository and external sources, far beyond
the inline evidence budget); the owner explicitly asked for delegated, parallel, domain-specialised agents. Phase II:
every unit is delegated by the owner's protocol (S14).

## Register

Updated by agents at each merge, gate and stage closure (S16). Dates are ISO; PR numbers link to the pull request.

### Stage status

| Stage | Status | Gates still open | Closure criteria met (dossier §9 a-e) |
| --- | --- | --- | --- |
| 0. Repair and measure | not started; ready once G1 merges | — | — |
| 1. Kernel and composition | not started | P4 (split frozen), P3 + S1-2 (graphics contract frozen), P1 (before 2a) | — |
| 2a. Graphics foundation | not started | P1 (entry) | — |
| 2b. Input | not started | — | — |
| 2c. Audio and content | not started | P5 (`IAudioSink`) | — |
| 3. World and physics | not started | P6 (physics contract), P7 (`IWorld`) | — |
| 4. UI and modules | not started | — | — |
| 5. Consumers | not started | `Entity` adapter deleted; exclusion list empty | — |
| 6. Phase II completion | not started | P2 (shader toolchain) | — |

### Approved decisions

Approved by the owner on 2026-11-02 (dossier §7, §11). Each ADR is written and merged in the stage that implements
it; "written" records the merged ADR file.

| ADR | Decision | Stage | Written |
| --- | --- | --- | --- |
| 0026 | Kernel with modules, typed registry, phased schedule; `Game` façade | 1 | no |
| 0027 | The loop owns time; engine interpolation with teleport; world clocks | 1, 3 | no |
| 0028 | One complete graphics device contract with capabilities, built-in pipelines, typed capability error; frozen after the stage 1 spike | 1 (spike), 2a | no |
| 0029 | `Presentation` replaces `CanvasScaling`; pixel art is a preset | 2a | no |
| 0030 | One object model; transitional `Entity` adapter deleted at the end of stage 5 | 3 | no |
| 0031 | Physics common contract + declared capabilities; tile backend first; Box2D after P6 | 3, 6 | no |
| 0032 | Input as device events with ids, players, layers | 2b | no |
| 0033 | One managed mixer; backends are PCM sinks | 2c | no |
| 0034 | Content handles, loader registry, sources; optional build-time import | 2c | no |
| 0035 | Validation by contract: declared properties, justified tolerances, closure criteria, exclusion list | 0 | no |
| 0036 | Assemblies by boundary, architecture test; few packages, one version | 1 | no |

### Contracts in force

None is frozen yet. A contract is listed as frozen only after its gate passes and the freezing PR merges.

| Contract | Freeze gate | Status |
| --- | --- | --- |
| Platform/device split (`IPlatformHost`, `ISurface`, integrated backends) | P4 concluded; stage 1 | draft (dossier §6.4) |
| `IGraphicsDevice` and the graphics contract | S1-2 spike with P3: tier-0 and tier-1 adapters implement it without ad hoc exceptions; end of stage 1 | draft (dossier §6.4) |
| `IAudioSink` | P5 concluded; before stage 2c closes | draft |
| `IWorld` | P7 concluded; stage 3 | draft |
| `IPhysicsWorld2D` common contract and `PhysicsCapabilities` | P6 concluded; stage 3 | draft |
| Tier-1 shader toolchain | P2 concluded; inside the 3D vertical slice | draft |

### Open PRs

| PR | Task | Branch | Owner | Notes |
| --- | --- | --- | --- | --- |
| (to be filled when opened) | T5 | `docs/arch-1-plan` | documentation agent | Phase I dossier + approved amendments + this register; merges after G1 |
| — | G1 | `fix/phase-ii-deploy-freeze` | another agent | deploy freeze; merges first |

### Merged PRs

None yet.

### Temporary incompatibilities

Mirror of the checked-in exclusion list (dossier §9). Each entry: consumer, reason, owner, resolving stage. Stage 5
empties it.

| Consumer | Reason | Owner | Resolved by stage |
| --- | --- | --- | --- |
| — (empty) | | | |

### Validation results

None yet for Phase II. Phase I evidence: dossier experiments (`docs/evaluations/arch-1/experiments/`).

### Known problems

Confirmed defects from the audit (dossier §3.1), all scheduled in stage 0 except D5-D8, which the redesign resolves:

| ID | Summary | Task |
| --- | --- | --- |
| D1 | `ComposedPlatform` drops display pacing, timer, graphics menu, diagnostics | S0-D1 |
| D2 | Release-gating package checks red on `main`; not run daily | S0-D2, S0-C1 |
| D3 | Visual fingerprint passes real regressions, fails invisible shifts | S0-D3, S0-V3 |
| D4 | Headless clock hard-codes 60 Hz | S0-D4 |
| D5 | Backends disagree (clear vs clip, sub-pixel rows, disposed textures, capture alpha) | S0-V1, S2a |
| D6 | `TileMotion` tunnels and embeds | S3 |
| D7 | Straight alpha fringes | S2a |
| D8 | Three mixers with three voice policies | S2c |
| D9 | `AssetCache.Unload` disposes shared textures; duplicate loads | S0-D9 |
| D10 | Browser suite not in CI; no SDL keyboard/audio-output test | S0-C2, S0-C3 |
| D11 | Smaller defects (`EntityList` clear, font filter docs, flaky wait, `docs/ci.md` drift, frozen-overlay blend, QWERTY prompts) | S0-D11 |

### Blocks

- Stage 0 merges wait for G1 (the deploy freeze); T5 also merges after G1, since a docs change on `main` can trigger
  the production site deployment.

### Next steps

1. Merge G1 (deploy freeze), then T5 (this dossier and register).
2. Start stage 0's three units (defects, CI, validation tooling) and prototypes P1, P3, P4 in parallel.
3. Record each prototype verdict in the dossier before its gate.

## Log

- L1 (2026-11-02) Owner's request, verbatim:

  > MicroEngine — Renacimiento Arquitectónico
  > Auditoría adversarial, investigación industrial, rediseño y reconstrucción fundacional
  >
  > Misión
  > MicroEngine ha alcanzado un punto de madurez en el que necesitamos reevaluar sus fundamentos arquitectónicos.
  > El proyecto nació resolviendo necesidades concretas de videojuegos 2D, especialmente pixel art. Sin embargo, nuestra visión es desarrollar un motor generalista, multiplataforma, extensible y modular, capaz de soportar videojuegos 2D, 2.5D y 3D.
  > No queremos que las decisiones tomadas durante las primeras implementaciones condicionen incorrectamente los próximos años de desarrollo.
  > Esta misión supone una revisión fundacional completa del motor, su SDK, sus herramientas, sus proyectos, sus demos y su infraestructura de validación.
  > Tu función es actuar como arquitecto principal, investigador técnico y orquestador de agentes especializados.
  > No debes abordar esta investigación como una tarea individual secuencial. Organiza y delega el trabajo entre los agentes y subagentes que consideres necesarios, con especialización por dominios, revisión cruzada e integración arquitectónica centralizada.
  > El trabajo se divide en dos fases principales, con una revisión y aprobación del diseño entre ambas.
  >
  > FASE I — AUDITORÍA ADVERSARIAL Y REDISEÑO
  >
  > 1. Auditoría crítica sin protección de la arquitectura existente
  > Realiza una revisión adversarial de absolutamente toda la arquitectura actual.
  > Adopta deliberadamente la perspectiva de un auditor que intenta encontrar las debilidades estructurales del sistema.
  > Ningún componente debe considerarse correcto simplemente porque:
  > * Funciona actualmente.
  > * Tiene pruebas automatizadas.
  > * Está bien implementado.
  > * Ya lo utilizan las cuatro demos.
  > * Cambiarlo sería costoso.
  > * Su diseño es sencillo.
  > * Fue una decisión arquitectónica anterior.
  > * Preservarlo evitaría romper compatibilidad.
  > Evalúa cada decisión considerando el motor que queremos construir, no únicamente los juegos que tenemos hoy.
  > La pregunta fundamental es:
  > Si diseñáramos MicroEngine desde cero, con nuestra experiencia actual y las lecciones de la industria, ¿volveríamos a tomar esta decisión?
  > Si la respuesta es negativa, identifica la alternativa apropiada.
  > La demolición de componentes existentes no constituye una restricción.
  > No obstante, cada crítica y propuesta deberá respaldarse con evidencia técnica. La intención adversarial es detectar errores de diseño, no inventarlos.
  >
  > 2. Investigación profunda de la industria
  > La auditoría no debe basarse exclusivamente en criterios propios.
  > Investiga cómo los motores y frameworks existentes han abordado problemas equivalentes, incluyendo:
  > * Godot.
  > * Unity.
  > * Unreal Engine.
  > * MonoGame.
  > * SDL y SDL_GPU.
  > * Raylib.
  > * Otros motores, frameworks, middleware y proyectos abiertos relevantes que descubras.
  > Estudia su arquitectura, implementación y evolución histórica.
  > Prioriza código fuente, documentos de diseño, decisiones arquitectónicas, propuestas técnicas, issues, pull requests, discusiones de mantenedores y experiencias documentadas.
  > Investiga especialmente:
  > * Qué problemas intentaron resolver.
  > * Qué diseños eligieron inicialmente.
  > * Qué errores descubrieron.
  > * Qué decisiones tuvieron que revertir.
  > * Qué subsistemas reconstruyeron.
  > * Qué limitaciones permanecen.
  > * Qué complejidades resultaron inevitables.
  > * Qué soluciones han demostrado funcionar.
  > * Qué lecciones podemos trasladar a MicroEngine.
  > No utilices otros motores como autoridad incuestionable.
  > Distingue sus requisitos particulares de las necesidades generales de un motor.
  > Cuando estudies implementaciones propietarias, diferencia lo que está públicamente documentado de lo que solo puede inferirse.
  > El objetivo es aprovechar conocimiento acumulado en la industria, no copiar superficialmente una arquitectura.
  >
  > 3. Auditoría integral de subsistemas
  > Revisa todos los subsistemas actuales y los límites entre ellos.
  > Incluye, como mínimo:
  > Núcleo y ciclo de vida
  > Inicialización, servicios, composición, dependencias, eventos, game loop, estado, configuración, extensibilidad, threading y gestión de recursos.
  > Renderizado y gráficos
  > Arquitectura de renderizado, SDL_Renderer, SDL_GPU, Raylib, APIs gráficas, dispositivos, shaders, materiales, texturas, primitivas, fuentes, cámaras, transformaciones, render targets, filtrado, antialiasing, resolución, escalado, renderizado 2D y futura incorporación de 3D.
  > Input
  > Teclado, ratón, gamepads, dispositivos simultáneos, múltiples jugadores, mapeo de acciones, touch, gestos, eventos, prioridades, ciclo de vida y abstracciones entre plataformas.
  > Audio
  > Dispositivos, mezcla, reproducción, streaming, síntesis, efectos, audio posicional, espacialización 3D, recursos y extensibilidad de backends.
  > Física
  > Revisa la ausencia o presencia real de contratos y subsistemas físicos propios del motor.
  > Investiga la integración de motores de física 2D y 3D, sus modelos de simulación, colisiones, transformaciones, eventos y relación con entidades y escenas.
  > Estudia implementaciones existentes como Box2D, Jolt y otras alternativas apropiadas.
  > Entidades, escenas y mundo
  > Escenas, jerarquías, entidades, componentes, transformaciones, sistemas, objetos de juego, ECS, ciclo de vida, serialización y coordinación con física, gráficos y audio.
  > No presupongas que ECS es obligatorio ni que el diseño actual debe conservarse.
  > Assets
  > Pipeline de importación, procesamiento, formatos, fuentes, imágenes, atlas, modelos 3D, materiales, shaders, cachés, referencias, streaming, empaquetado y distribución.
  > Plataformas
  > Ventanas, gráficos, audio, input, almacenamiento, archivos, ciclo de vida, dispositivos y adaptación a Windows, Linux, macOS, Android, iOS y web.
  > SDK y herramientas
  > API pública, configuración, defaults, composición de subsistemas, CLI, paquetes NuGet, plantillas, proyectos consumidores, extensibilidad y experiencia de desarrollo.
  > Identifica también subsistemas ausentes o responsabilidades no contempladas que resulten arquitectónicamente relevantes para la visión futura.
  >
  > 4. Arquitectura lógica y física
  > Reevalúa completamente la organización del código.
  > No asumas que las carpetas actuales `Core`, `Graphics`, `Assets` y equivalentes representan límites arquitectónicos adecuados.
  > Investiga:
  > * División de responsabilidades.
  > * Proyectos y assemblies.
  > * Namespaces.
  > * Dependencias entre módulos.
  > * Contratos públicos e internos.
  > * Separación entre abstracciones e implementaciones.
  > * Organización por características, dominios o capas.
  > * Ciclo de vida y composición de servicios.
  > * Dependencias opcionales.
  > * Límites de paquetes.
  > * Capacidad de sustituir implementaciones.
  > * Independencia de plataformas.
  > * Preparación para 2D y 3D.
  > Propón la estructura física y lógica que corresponda a la arquitectura objetivo, incluso si implica reorganizar completamente la solución.
  > No confundas crear carpetas con solucionar problemas de acoplamiento.
  >
  > 5. Supuestos implícitos y comportamientos mágicos
  > Busca sistemáticamente decisiones ocultas que el motor toma en nombre del desarrollador.
  > El problema identificado en ASSET-1, donde una opción de antialiasing determina implícitamente el filtrado de textura, es un ejemplo de lo que debemos investigar en todo el proyecto.
  > Identifica:
  > * Defaults implícitos.
  > * Configuraciones acopladas indebidamente.
  > * Restricciones heredadas del pixel art.
  > * Supuestos de un solo jugador.
  > * Supuestos exclusivamente 2D.
  > * Dependencias directas de backends.
  > * Comportamientos específicos de una demo incorporados al núcleo.
  > * Limitaciones injustificadas de formatos o resoluciones.
  > * Responsabilidades que deberían pertenecer a subsistemas independientes.
  > Los defaults plug-and-play siguen siendo un requisito fundamental.
  > La cuestión es que deben corresponder a decisiones coherentes, documentadas y sustituibles cuando corresponda.
  >
  > 6. Capacidad real de sustitución
  > Evalúa si MicroEngine cumple realmente su promesa de subsistemas reemplazables.
  > Investiga qué ocurre cuando un desarrollador desea sustituir:
  > * Renderer.
  > * API gráfica.
  > * Backend de ventanas.
  > * Motor físico.
  > * Sistema de audio.
  > * Gestor de recursos.
  > * Implementación de input.
  > * Otros subsistemas relevantes.
  > Determina si las interfaces actuales representan contratos de sustitución reales o simples envoltorios de una implementación concreta.
  > Analiza costes de integración, capacidades compartidas, características opcionales, compatibilidad entre módulos y gestión de fallos.
  >
  > 7. Auditoría de infraestructura y validación
  > Incluye toda la infraestructura que sostiene MicroEngine.
  > Reevalúa:
  > * Pruebas unitarias.
  > * Pruebas de integración.
  > * Pruebas funcionales.
  > * Pruebas de plataformas.
  > * Pruebas visuales.
  > * Comparación de imágenes.
  > * Validación temporal.
  > * Rendimiento.
  > * Pruebas de audio.
  > * Pruebas de input.
  > * CI/CD.
  > * Empaquetado y publicación.
  > * Demos como consumidores del SDK.
  > * Documentación y ejemplos.
  > Cuestiona especialmente la idoneidad de las comparaciones pixel-perfect como criterio universal de validación entre renderizadores y plataformas diferentes.
  > Investiga enfoques de comparación perceptual, tolerancias justificadas, invariantes geométricos, pruebas temporales y otros mecanismos existentes.
  > No conserves un método de validación únicamente porque ya está automatizado.
  > La infraestructura debe demostrar que los subsistemas cumplen sus contratos, no que producen accidentalmente una salida idéntica bajo todas las condiciones.
  >
  > 8. Investigación y pruebas arquitectónicas
  > No limites la auditoría a leer código y redactar opiniones.
  > Cuando sea necesario, desarrolla prototipos aislados, pruebas de concepto, modelos de dependencias, experimentos y comparativas.
  > Investiga código existente de otros motores para comprender cómo resolvieron problemas similares.
  > Distingue entre:
  > * Problemas confirmados.
  > * Riesgos arquitectónicos.
  > * Limitaciones de diseño.
  > * Especializaciones legítimas.
  > * Capacidades faltantes.
  > * Decisiones que requieren experimentación adicional.
  > Para cada hallazgo relevante, explica:
  > 1. Qué existe actualmente.
  > 2. Qué problema representa.
  > 3. Qué evidencia lo demuestra.
  > 4. Qué han aprendido otros proyectos.
  > 5. Qué alternativas existen.
  > 6. Qué solución propones.
  > 7. Qué consecuencias tiene para MicroEngine.
  >
  > 9. Orquestación de agentes
  > Utiliza los agentes especializados disponibles de manera activa.
  > Distribuye investigaciones y revisiones por dominios cuando resulte útil.
  > Cada agente debe producir hallazgos verificables, referencias y recomendaciones técnicas.
  > Realiza revisiones cruzadas entre dominios relacionados.
  > La responsabilidad de integrar los resultados y resolver contradicciones corresponde al arquitecto principal.
  > Evita que cada agente diseñe su subsistema ignorando las dependencias con los demás.
  > Organiza la investigación de manera que el trabajo pueda avanzar en paralelo y permita descubrir problemas no previstos inicialmente.
  >
  > 10. Diseño de la arquitectura objetivo
  > A partir de la auditoría, produce una propuesta arquitectónica integral.
  > Debe contemplar:
  > * Visión y principios arquitectónicos.
  > * Organización lógica.
  > * Organización física.
  > * Núcleo y composición de subsistemas.
  > * Contratos e interfaces.
  > * Extensibilidad.
  > * Separación entre plataforma y motor.
  > * Renderizado 2D y futura incorporación de 3D.
  > * Física 2D y 3D.
  > * Entidades y escenas.
  > * Audio.
  > * Input.
  > * Assets.
  > * SDK y herramientas.
  > * Estrategia de testing y CI.
  > * Dependencias externas.
  > * Capacidades opcionales.
  > * Estrategia multiplataforma.
  > La propuesta debe distinguir claramente qué necesitamos implementar ahora, qué interfaces deben quedar preparadas y qué capacidades pueden incorporarse después.
  > Pensar a largo plazo no implica implementar inmediatamente todas las funcionalidades de un motor 3D completo.
  > Sí implica evitar decisiones que hagan inviable o excesivamente costosa su incorporación posterior.
  >
  > 11. Resultado de la Fase I
  > Entrega un dossier arquitectónico completo que incluya:
  > * Inventario del sistema actual.
  > * Informe adversarial por subsistema.
  > * Investigación de la industria con fuentes verificables.
  > * Lecciones históricas aplicables.
  > * Mapa de dependencias y acoplamientos.
  > * Problemas y riesgos clasificados.
  > * Arquitectura objetivo.
  > * Propuesta de estructura de solución, proyectos y paquetes.
  > * Contratos fundamentales propuestos.
  > * Decisiones arquitectónicas y alternativas descartadas.
  > * Prototipos y evidencia experimental.
  > * Nueva estrategia de validación.
  > * Plan de reconstrucción organizado en etapas.
  > * Dependencias entre trabajos.
  > * Estrategia de integración y pruebas.
  > * Riesgos técnicos y costes arquitectónicos.
  > Al concluir esta fase, presenta el diseño para revisión y aprobación. No comiences la reconstrucción estructural de la Fase II sin esa aprobación.
  >
  > FASE II — RECONSTRUCCIÓN FUNDACIONAL
  > Esta fase comenzará después de aprobar la arquitectura objetivo.
  >
  > 12. Reconstrucción sin obligación de preservar la Alpha anterior
  > Implementa el diseño aprobado, incluso cuando requiera sustituir componentes, romper contratos públicos, reorganizar proyectos o modificar profundamente la infraestructura.
  > No conserves implementaciones incompatibles con la arquitectura objetivo únicamente por su antigüedad o por evitar cambios.
  > MicroEngine continúa en Alpha.
  > La estabilidad de APIs anteriores no tiene prioridad sobre la corrección arquitectónica del sistema futuro.
  >
  > 13. Ejecución coordinada
  > Divide la reconstrucción en trabajos especializados y ejecutables.
  > Utiliza subagentes para implementar subsistemas independientes cuando sea apropiado.
  > Establece contratos compartidos antes de desarrollar implementaciones que dependan de ellos.
  > Integra las piezas progresivamente y comprueba su interacción real.
  > Mantén una responsabilidad centralizada sobre la coherencia del motor.
  >
  > 14. Reconstrucción de herramientas e infraestructura
  > Actualiza junto con el motor:
  > * SDK y paquetes.
  > * CLI.
  > * Plantillas.
  > * Documentación.
  > * Benchmarks.
  > * Pruebas automatizadas.
  > * Pruebas visuales.
  > * CI/CD.
  > * Distribución multiplataforma.
  > * Demos y proyectos consumidores.
  > No asumas que la infraestructura anterior debe permanecer intacta.
  > Reconstrúyela donde el nuevo diseño lo requiera.
  >
  > 15. Validación de la nueva arquitectura
  > Utiliza las cuatro demos existentes, Asset Gallery y nuevos escenarios de integración para validar las capacidades del motor.
  > Estos proyectos deben actuar como consumidores y pruebas de la arquitectura, no como restricciones que determinen su diseño.
  > Evalúa tanto capacidades actuales como escenarios representativos de evolución futura.
  > Comprueba la modularidad real, la sustitución de subsistemas, el comportamiento multiplataforma y la coherencia de la experiencia del desarrollador.
  >
  > 16. Resultado de la Fase II
  > Entrega una arquitectura implementada, documentada, integrada y validada.
  > Organiza la reconstrucción en PRs o unidades de integración coherentes cuando sea apropiado.
  > Documenta decisiones, resultados, limitaciones y trabajos futuros.
  > No publiques versiones ni fusiones cambios a `main` sin autorización.
  >
  > PRINCIPIOS RECTORES
  > Visión antes que legado. La arquitectura se evalúa contra la visión futura de MicroEngine, no contra sus primeras demos.
  > Evidencia antes que opinión. Las decisiones relevantes deben estar fundamentadas en código, pruebas, experiencias de la industria o investigación técnica verificable.
  > Reutilización antes que reinvención. Investiga las implementaciones existentes, comprende sus decisiones internas y aprovecha el conocimiento acumulado.
  > Modularidad real. Los subsistemas deben tener responsabilidades y límites claros, con capacidad efectiva de sustitución.
  > Defaults sin encierro. MicroEngine debe funcionar con configuraciones predeterminadas apropiadas sin impedir personalización avanzada.
  > Complejidad justificada. No evites una arquitectura necesaria por ser compleja, pero tampoco reproduzcas complejidad ajena sin comprender su propósito.
  > Libertad para reconstruir. Ningún componente, carpeta, contrato o tecnología existente está protegido de una sustitución fundamentada.
  > Coherencia global. Las soluciones especializadas deben formar parte de una arquitectura integral.
  > Validación significativa. Las pruebas deben demostrar propiedades y contratos relevantes, no únicamente reproducir resultados históricos.
  > Preparación para 3D. El trabajo actual debe sostener una evolución real hacia videojuegos 2D, 2.5D y 3D.
  >
  > Instrucción final
  > No abordes esta misión como una refactorización incremental ordinaria.
  > Esta es una evaluación fundacional de MicroEngine, respaldada por investigación industrial y dirigida a definir la arquitectura sobre la que continuará evolucionando el proyecto.
  > Actúa como arquitecto principal y orquestador.
  > Investiga exhaustivamente, cuestiona lo existente, distribuye el trabajo, contrasta resultados, resuelve contradicciones y produce una arquitectura objetivo concreta.
  > Comienza por la Fase I. La Fase II queda condicionada a la aprobación del diseño arquitectónico.

- L2 (2026-11-02) Seven agents launched in parallel (T1, T2); reports land in the session scratchpad and are
  integrated into the dossier.
- L3 (2026-11-02) All seven reports received and kept as annexes under `docs/evaluations/arch-1/`. Contradictions
  resolved in the dossier (section 5): graphics contract vs implementation order, physics timing, entity model,
  package granularity, mixer, raylib's place, asset processing. Confirmed defects independent of the redesign listed
  as D1-D11 (stage 0). Next: the owner's review of sections 6, 7, 9 and 11; Phase II does not start without it.

- L4 (2026-11-02) The owner's five adjustments to the Phase I proposal, verbatim:

  > Cinco ajustes que que quiero que evalues de la propuesta antes de iniciar la reconstrucción
  > 1. Adelantar los prototipos que determinan contratos fundamentales
  > El plan coloca P1–P7 después de presentar las etapas, pero algunos experimentos pueden cambiar interfaces que se construirán antes.
  > Prototipo	Cuándo debe resolverse	Motivo
  > P5 — Salidas PCM web/móvil	Antes de cerrar la etapa 2c	Puede modificar IAudioSink
  > P7 — Entidades y componentes	Antes de cerrar el contrato de la etapa 3	Debe validar la ergonomía real de IWorld
  > P3 — GPU y composición 3D	Antes de congelar el contrato gráfico	Puede revelar limitaciones del dispositivo propuesto
  > P4 — SDL3 en navegador	Antes de consolidar definitivamente los hosts	Puede afectar la separación entre plataforma y dispositivo
  >
  > No hace falta ejecutar los siete experimentos antes de empezar. Hay que ejecutar cada uno antes de tomar la decisión irreversible que pretende validar. arch-1.md
  >
  > 2. Resolver una diferencia entre los dos contratos gráficos propuestos
  > En el anexo A2, IGraphicsDevice contempla buffers, shaders, pipelines y listas de comandos. En el contrato integrado del dossier, la interfaz expuesta es más reducida y no define expresamente todos esos mecanismos.
  >
  > Esa diferencia importa porque estamos diseñando la base gráfica para 2D y 3D.
  >
  > No necesitamos implementar todas las capacidades ahora, pero sí decidir si pertenecerán al mismo contrato, a extensiones consultables o a interfaces especializadas.
  >
  > Mi condición: validar la arquitectura del dispositivo antes de estabilizar sus contratos, no después de construir dos backends sobre ellos.
  > 3. No preservar Entity como compatibilidad por principio
  > La etapa 3 propone mantener Entity como una capa de compatibilidad temporal.
  >
  > Eso puede ser práctico para migrar las demos, pero no debe convertirse en una restricción del nuevo diseño.
  >
  > Aceptaría esa capa únicamente como herramienta transitoria, con una decisión explícita sobre su eliminación. No queremos terminar conservando dos modelos de entidades porque resultó cómodo durante la reconstrucción.
  > 4. Revisar las dependencias de la estrategia de testing
  > La nueva estrategia T0–T4 está mucho mejor fundamentada que las comparaciones actuales. Distingue determinismo, conformidad, comandos, apariencia y pruebas de dispositivos reales.
  >
  > Sin embargo, exigir que todas las demos estén funcionales al final de cada etapa podría imponer trabajo de adaptación repetitivo. La migración definitiva de consumidores está prevista en la etapa 5.
  >
  > Yo permitiría consumidores temporales o adaptadores durante la reconstrucción, siempre que cada etapa tenga pruebas significativas de sus propios contratos. No obligaría a migrar cuatro juegos completos varias veces para demostrar que el núcleo compila.
  >
  > Además, las tolerancias visuales deben depender de las propiedades evaluadas, no convertirse simplemente en referencias más permisivas. arch-1.md
  >
  > 5. Aclarar qué significa terminar la Fase II
  > La etapa 6 incluye SDL_GPU, shaders, una malla 3D, Box2D y la decisión sobre el host web, pero está marcada como «separadamente aprobada».
  >
  > Eso crea dos posibles criterios de finalización:
  > - Terminar la reconstrucción estructural hasta la etapa 5.
  > - Demostrar además que las nuevas abstracciones soportan capacidades avanzadas mediante los experimentos y prototipos de la etapa 6.
  > Para mí, la arquitectura no debería declararse validada para 3D solamente porque sus interfaces contienen Vector3, Quaternion y capacidades gráficas opcionales.
  >
  > Exigiría al menos una prueba vertical de renderizado 3D antes de afirmar que la arquitectura está preparada. No un juego 3D completo, pero sí una implementación que ejerza los contratos nuevos

- L5 (2026-11-02) The owner's approval, verbatim:

  > Apruebo los cinco ajustes propuestos para ARCH-1.
  > Actualiza el dossier, los anexos y el plan de reconstrucción donde corresponda, incorporando:
  >
  > 1. La nueva planificación de P1–P7, ejecutando cada prototipo antes de fijar el contrato o tomar la decisión que debe validar.
  > 2. Un contrato gráfico único y completo, con recursos compartidos, capacidades explícitas y validación mediante prototipos de nivel 0 y nivel 1.
  > 3. La eliminación obligatoria del adaptador heredado de Entity al finalizar la etapa 5, sin incorporarlo como API pública permanente.
  > 4. Los nuevos criterios de cierre por etapa y una estrategia de validación basada en propiedades y tolerancias justificadas.
  > 5. La definición de finalización de Fase II, incluyendo una prueba vertical 3D real y una demostración de sustitución del sistema físico.
  >
  > En la prueba de física, distingue las capacidades comunes entre el motor cinemático de tiles y Box2D de las capacidades específicas de cada implementación. No presupongas equivalencia funcional completa entre ambos.
  > Una vez actualizado y coherente el dossier, considero aprobada la arquitectura objetivo para iniciar la Fase II bajo este plan.

- L6 (2026-11-02) The owner's Phase II protocol, verbatim:

  > MicroEngine — Fase II: Protocolo de dirección y ejecución autónoma
  > Incorporamos las siguientes reglas obligatorias a la reconstrucción arquitectónica aprobada.
  > Estas instrucciones modifican expresamente el modelo de trabajo y las autorizaciones anteriores.
  > 1. Tu responsabilidad exclusiva: arquitecto y orquestador
  > Durante toda la Fase II, actuarás exclusivamente como arquitecto principal, director técnico, coordinador de agentes y responsable de integración.
  > No debes implementar ni modificar directamente código del motor, pruebas, herramientas, configuraciones, documentación o infraestructura.
  > Todas las modificaciones deberán ser realizadas por agentes delegados.
  > Tu responsabilidad consiste en mantener el contexto global del proyecto, planificar las etapas, definir contratos compartidos, distribuir tareas, gestionar dependencias, resolver contradicciones, revisar entregables y tomar decisiones de integración.
  > No te conviertas en implementador cuando un agente encuentre dificultades. Delega también las correcciones, investigaciones complementarias y tareas de integración.
  > Puedes consultar código, ejecutar verificaciones y utilizar las herramientas necesarias para supervisar el trabajo, pero no asumir directamente las tareas de implementación.
  > 2. Organización mediante agentes especializados
  > Utiliza tantos agentes y subagentes como resulte apropiado.
  > Paraleliza trabajos independientes para mejorar la velocidad de ejecución.
  > Cada agente debe recibir un alcance concreto, los contratos relevantes, sus dependencias y criterios de aceptación.
  > Evita que varios agentes modifiquen simultáneamente los mismos contratos fundamentales sin coordinación.
  > Cuando aparezcan incompatibilidades entre propuestas, resuélvelas desde la dirección arquitectónica y devuelve instrucciones claras a los agentes correspondientes.
  > Prioriza mantener una visión integral del sistema durante toda la reconstrucción.
  > 3. Flujo obligatorio mediante pull requests
  > Cada unidad de implementación deberá entregarse mediante una PR.
  > El agente responsable debe:
  >
  > * Implementar su tarea.
  > * Ejecutar las pruebas correspondientes.
  > * Documentar los cambios.
  > * Publicar su rama.
  > * Abrir la PR.
  > * Corregir los problemas encontrados durante la revisión.
  >
  > Tú serás responsable de revisar cada PR.
  > Evalúa su conformidad con la arquitectura aprobada, los contratos compartidos, las dependencias y los criterios de aceptación de la etapa.
  > Puedes aprobar, rechazar o solicitar correcciones.
  > Los agentes implementadores no deben fusionar sus propias PRs.
  > 4. Autorización excepcional para integrar a main
  > Durante esta Fase II tienes autorización expresa para aprobar y fusionar PRs a `main` sin solicitar aprobación individual del propietario.
  > Esta autorización sustituye temporalmente la restricción anterior que exigía autorización para cada merge.
  > Puedes realizar integraciones sucesivas conforme avancen las etapas y sus dependencias.
  > No necesitas detener la reconstrucción esperando una respuesta del propietario después de cada PR.
  > Antes de fusionar, verifica que los cambios cumplen los criterios de aceptación correspondientes y que las pruebas relevantes han sido ejecutadas.
  > Cuando una etapa permita expresamente dejar consumidores antiguos temporalmente incompatibles, evalúa el resultado según los contratos y pruebas de esa etapa, no según la obligación de que todas las demos funcionen continuamente.
  > Mantén documentadas las incompatibilidades temporales, sus responsables y la etapa en la que deberán resolverse.
  > No omitas verificaciones arbitrariamente para acelerar merges.
  > 5. Límites de la autorización
  > La autonomía concedida incluye:
  >
  > * Crear ramas.
  > * Delegar implementaciones.
  > * Abrir y revisar PRs.
  > * Solicitar y coordinar correcciones.
  > * Aprobar PRs.
  > * Fusionar cambios a `main`.
  > * Actualizar la arquitectura y documentación mediante agentes.
  > * Gestionar las dependencias entre etapas.
  >
  > No incluye autorización para:
  >
  > * Publicar nuevas versiones en NuGet.
  > * Crear releases públicas.
  > * Publicar nuevas versiones de las demos.
  > * Desplegar versiones públicas del sitio.
  > * Eliminar el repositorio o modificar su visibilidad.
  > * Modificar secretos, credenciales o permisos administrativos.
  >
  > La publicación y release final quedan reservadas al propietario.
  > Si los workflows habituales activan publicaciones o despliegues automáticamente después de un merge, coordina previamente su desactivación o aislamiento mediante agentes, sin comprometer la ejecución del CI de validación.
  > 6. Gestión del contexto y continuidad
  > Esta misión es extensa. Debes evitar que la coordinación dependa exclusivamente de tu ventana de contexto.
  > Mantén un registro persistente y actualizado con:
  >
  > * Estado de cada etapa.
  > * Decisiones arquitectónicas aprobadas.
  > * Contratos vigentes.
  > * Agentes responsables.
  > * Tareas pendientes.
  > * Dependencias.
  > * PRs abiertas y fusionadas.
  > * Resultados de validación.
  > * Problemas conocidos.
  > * Bloqueos.
  > * Próximos pasos.
  >
  > Actualiza ese registro mediante agentes cuando corresponda.
  > La reconstrucción debe poder continuar coherentemente entre sesiones sin depender de recordar conversaciones extensas.
  > 7. Control de calidad arquitectónica
  > No sacrifiques coherencia global por velocidad.
  > Cada integración debe acercar MicroEngine a la arquitectura objetivo aprobada.
  > Evita introducir nuevamente los acoplamientos, comportamientos implícitos y limitaciones identificados durante la auditoría adversarial.
  > Si una investigación o prototipo demuestra que una decisión aprobada debe cambiar, revisa el diseño, documenta la evidencia y coordina la modificación correspondiente.
  > No mantengas una decisión incorrecta únicamente porque ya fue aprobada anteriormente.
  > 8. Finalización de la reconstrucción
  > Continúa coordinando las etapas de la Fase II hasta cumplir los criterios de finalización establecidos en el dossier actualizado, incluidas las pruebas verticales 3D y de sustitución de física.
  > Una vez terminada la reconstrucción:
  >
  > * Presenta el estado final del motor.
  > * Resume las decisiones arquitectónicas efectivamente implementadas.
  > * Documenta las pruebas y plataformas verificadas.
  > * Identifica las capacidades pendientes.
  > * Confirma el estado de los paquetes, demos, CLI, documentación y CI.
  > * Entrega la reconstrucción integrada en `main`, sin publicar una nueva versión.
  >
  > El propietario realizará la validación personal y autorizará posteriormente las publicaciones y releases pertinentes.
  > Regla central: tú diriges, coordinas, revisas y fusionas. Tus agentes investigan, implementan, prueban y corrigen. La arquitectura y su coherencia son tu responsabilidad; la ejecución técnica se distribuye.

- L7 (2026-11-02) T5: the architect's directive 1 integrated the five amendments into the dossier in place:
  prototypes scheduled in the stage where they must conclude, each with its gate, P1/P3/P4 starting with stage 0
  (§9); the complete single `IGraphicsDevice` contract with built-in pipelines and `GraphicsCapabilityException`,
  frozen after a stage 1 spike on SDL_Renderer (tier 0) and SDL_GPU (tier 1, P3) (§6.4, ADR 0028 row); the `Entity`
  adapter as a transitional `[Obsolete]` non-packable project deleted at the end of stage 5 (§6.3, §6.5, ADR 0030
  row); stage closure criteria, the exclusion list and the tolerance policy (§8, §9); the Phase II definition of done
  with the 3D vertical slice and the physics common-contract/capability split (§9.1, ADR 0031 row); risks and
  "decisions taken" (§10, §11). Annexes A2 and A3 carry a superseded-points note. This file became the Phase II
  register (S11-S16, Phase II tasks, `## Register`). Next: G1 merges, then this PR; stage 0 and P1/P3/P4 start.
