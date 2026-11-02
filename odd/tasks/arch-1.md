# ARCH-1: Architectural renaissance — adversarial audit, industry research, target architecture

Objective: Phase I of a foundational review: audit every subsystem against the vision of a generalist, modular,
multi-platform 2D/2.5D/3D engine, research how other engines solved the same problems, and design the target
architecture. Branch `docs/arch-1` from `feat/asset-1` at 5706285. Delivery: work-unit commits; no PR, merge or
release without the owner. Phase II (reconstruction) starts only after the owner approves the design.

## Specs

Quoted fragments are the owner's request (2026-11-02), verbatim; the full request is `L1`.

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

## Tasks

| ID | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 | S1, S3-S6, S8 | delegated: 4 domain auditors (core/SDK, rendering/platform, input/audio/world, infrastructure) | done | annexes A1-A4, experiments in `docs/evaluations/arch-1/experiments/` |
| T2 | S2 | delegated: 3 researchers (rendering, engine core/physics/assets/audio/input, validation/packaging) | done | annexes R1-R3 (230+ sources) |
| T3 | S1-S9 | inline: cross-check, resolve contradictions, write `docs/evaluations/arch-1.md` | done | dossier; D1 and D4 spot-checked against source |
| T4 | S10 | inline: present for approval | waiting for the owner | |

Route evidence: T1/T2 fire the mapping trigger (the audit spans the whole repository and external sources, far beyond
the inline evidence budget); the owner explicitly asked for delegated, parallel, domain-specialised agents.

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
