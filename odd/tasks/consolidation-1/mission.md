# CONSOLIDATION-1, L1: the owner's request, verbatim (2026-11-19)

Misión: Consolidación arquitectónica integral de MicroEngine 2D

Vas a asumir una misión de ingeniería de gran alcance sobre MicroEngine.
Tu objetivo es auditar, corregir, remodelar y consolidar la arquitectura y las capacidades 2D del motor hasta alcanzar una base técnicamente sólida, coherente, extensible y preparada para que desarrolladores externos puedan construir videojuegos reales.
No se trata de resolver tres issues aisladas ni de implementar una lista de funcionalidades. Se trata de revisar integralmente el producto que hemos construido y corregir las decisiones arquitectónicas que no estén suficientemente justificadas.
Tienes autonomía para organizar y ejecutar este trabajo.
Puedes investigar, planificar, dividir tareas, crear y reabrir issues, actualizar ADR, modificar contratos públicos, reemplazar implementaciones, incorporar dependencias, desarrollar módulos, crear pruebas, modificar documentación y reorganizar componentes cuando exista una justificación técnica y de producto.
No tienes obligación de preservar una decisión simplemente porque ya fue implementada, aprobada o documentada.
Estamos en Alpha. Podemos permitirnos corregir profundamente la arquitectura antes de comprometerla con usuarios externos.

## 1. Filosofía de MicroEngine

MicroEngine es un motor de videojuegos generalista, modular y extensible.
La propuesta del producto es:
Ofrecer capacidades listas para utilizar, con valores predeterminados razonables, permitiendo que el desarrollador sustituya sus implementaciones cuando lo necesite.
No queremos un framework que obligue a cada desarrollador a buscar bibliotecas y construir repetidamente las conexiones con gráficos, físicas, audio, input, escenas, animaciones y demás sistemas.
Tampoco queremos un motor monolítico que imponga todas sus capacidades.
Queremos que las responsabilidades comunes de un motor estén resueltas de manera coherente, y que las decisiones específicas de cada videojuego permanezcan bajo el control de su desarrollador.
Los subsistemas deben colaborar sin quedar innecesariamente acoplados.
La arquitectura debe facilitar tanto los juegos sencillos como los proyectos que necesitan capacidades más sofisticadas.
No confundas extensibilidad con trasladar la integración al usuario.
Que el desarrollador pueda integrar una biblioteca externa no significa automáticamente que MicroEngine esté ofreciendo una buena experiencia.
La pregunta es qué trabajo debería resolver el motor y qué responsabilidad debe conservar cada juego.

## 2. El problema que motiva esta misión

Durante las últimas evaluaciones encontramos un sesgo metodológico importante.
Varias decisiones fueron tomadas principalmente considerando:

* Benchmarks y rendimiento absoluto.
* Necesidades de nuestras demos actuales.
* Coste de incorporar infraestructura.
* Posibilidad de implementar algo manualmente.
* Existencia de una biblioteca externa que el desarrollador podría conectar.
* Deseo de evitar cambios en contratos o APIs existentes.

Estos factores son relevantes, pero son insuficientes para determinar el valor de una capacidad de motor.
Una nueva infraestructura no es un defecto por sí misma. Introducir contratos, adaptadores, servicios o dependencias es parte normal de desarrollar nuevas funcionalidades.
De igual manera, una característica no necesita mejorar FPS, throughput o memoria para aportar valor.
Puede justificar su existencia mediante composición, mantenibilidad, ergonomía, reutilización, confiabilidad, organización o reducción de trabajo manual.
No estamos construyendo MicroEngine para ganar benchmarks. Estamos construyendo MicroEngine para desarrollar videojuegos.
El rendimiento sigue siendo importante. Debemos preservar presupuestos razonables de CPU, GPU, memoria y tiempo por frame, y prevenir problemas estructurales.
Pero las decisiones deben basarse en el conjunto de sus trade-offs, no en una sola métrica.
No aceptes como justificación suficiente:

* «Ninguna demo lo necesita».
* «El sistema actual es suficientemente rápido».
* «La implementación nueva requiere infraestructura».
* «La biblioteca externa se puede usar manualmente».
* «Necesitamos modificar APIs».
* «Es una funcionalidad avanzada».
* «El usuario puede implementarla en su juego».

Todas estas afirmaciones pueden formar parte del análisis, pero ninguna constituye por sí sola una conclusión arquitectónica.

## 3. Autonomía y autoridad de ejecución

Tienes libertad para determinar cómo realizar esta misión.
Puedes:

1. Inspeccionar completamente el repositorio y su historial arquitectónico.
2. Ejecutar exploraciones paralelas y delegar investigaciones cuando tus herramientas lo permitan.
3. Redactar cuantos documentos técnicos y evaluaciones necesites.
4. Crear, reabrir, reorganizar, relacionar y cerrar issues según su estado real.
5. Modificar ADR existentes o reemplazarlos mediante decisiones nuevas.
6. Rediseñar interfaces, abstracciones y contratos públicos.
7. Incorporar bibliotecas maduras como componentes opcionales o predeterminados cuando corresponda.
8. Eliminar abstracciones innecesarias o sustituir implementaciones inadecuadas.
9. Crear módulos, adaptadores, importadores, herramientas y servicios.
10. Añadir, modificar o eliminar pruebas obsoletas.
11. Actualizar documentación, ejemplos, plantillas y referencias de API.
12. Organizar el trabajo en fases, ramas, PR y lotes de integración.
13. Ajustar el plan conforme descubras problemas y dependencias que no conocíamos al comienzo.

No esperes instrucciones del propietario para tomar cada decisión técnica ordinaria.
Tu responsabilidad es investigar, justificar, ejecutar y verificar.
No obstante, conserva la trazabilidad de los cambios importantes. Una reconstrucción transversal necesita documentación que explique por qué se realizó, qué alternativas se descartaron y qué consecuencias produce.
No uses tu autonomía como excusa para introducir complejidad gratuita.
No existe una preferencia predeterminada por conservar o reemplazar código.
La intervención correcta es la menor que resuelva adecuadamente el problema, aunque eso signifique reconstruir un subsistema entero.

## 4. Primera fase: auditoría transversal del motor

Antes de lanzar implementaciones independientes, realiza una revisión integral de las capacidades y contratos existentes.
No te limites a las issues abiertas.
Investiga:

* Arquitectura del kernel y ciclo de vida.
* Modelo de escenas, mundo, entidades y componentes.
* Máquinas de estados y coordinación del comportamiento.
* Gestión, procesamiento y empaquetado de contenido.
* Gráficos 2D, dispositivos, materiales, shaders y render targets.
* Sprites, atlas, animaciones, fuentes y texto.
* Físicas y sus contratos.
* Input, acciones, dispositivos y rebinding.
* Audio, mezcla, buses, efectos y recursos.
* Navegación y comportamiento de agentes.
* Persistencia, serialización, versiones y migraciones.
* Localización e internacionalización.
* Eventos, temporización, determinismo y depuración.
* Herramientas CLI, plantillas, samples, documentación y distribución.
* Compatibilidad con escritorio, navegador y plataformas móviles.

Analiza tanto el funcionamiento interno como la experiencia que recibe un desarrollador externo.
Busca especialmente:

* Responsabilidades que deberían pertenecer al motor pero están repetidas en los juegos.
* Contratos que impiden aprovechar funcionalidades de bibliotecas o backends existentes.
* Integraciones que requieren demasiado código de infraestructura.
* Capacidades ofrecidas mediante caminos paralelos que evitan las abstracciones normales.
* Decisiones descartadas por falta de necesidad en nuestras demos.
* Funcionalidades inaccesibles desde las APIs públicas.
* Abstracciones acopladas innecesariamente al pixel art o a casos particulares.
* Inconsistencias entre módulos.
* Capacidades anunciadas que no están suficientemente integradas.
* Limitaciones que podrían dificultar construir videojuegos de otros géneros.

No conviertas esta revisión en una lista interminable de posibles funcionalidades.
Distingue las deficiencias reales del producto de las extensiones especulativas.

Diagnóstico transversal

Produce una visión de conjunto de la arquitectura.
Identifica las causas comunes de varias deficiencias y las dependencias entre las intervenciones propuestas.
No quiero que resuelvas diez issues independientemente si cinco comparten un problema de diseño fundamental.
Determina qué cimientos deben corregirse antes de intervenir otros subsistemas.

## 5. Decisiones y áreas que ya requieren reevaluación

Estas áreas forman parte del alcance inicial obligatorio.

ENTITY-1 — ECS (#151)
Reevalúa ECS como una alternativa de modelado, composición y coordinación de comportamiento.
No lo reduzcas a almacenamiento eficiente de entidades.
Estudia la experiencia real de utilizar ECS en un juego complejo y si una integración oficial opcional puede eliminar infraestructura repetitiva.
Considera Arch, Friflo y otras bibliotecas maduras.
Conserva el modelo sencillo actual como predeterminado mientras siga justificándose.
La PR #170 contiene investigación aprovechable, pero su integración no constituye una decisión definitiva sobre el valor de ECS.
Revisa el ADR 0059.

CONTENT-1 — Contenido (#113)
Reevalúa la producción completa de recursos para videojuegos.
Considera importación, procesamiento, validación, atlas, sprites, animaciones, fuentes, audio, empaquetado, variantes de plataforma y recarga durante el desarrollo.
Estudia qué herramientas existentes pueden reutilizarse y si conviene proporcionar procesamiento opcional.
Los recursos procesados deben integrarse coherentemente con las APIs, ciclos de vida y sistemas de renderizado del motor.
La PR #172 fue cerrada sin integrar porque su solución KTX especializada no resolvía suficientemente la experiencia general del desarrollador.
Conserva cualquier investigación u optimización reutilizable.
No presupongas que necesitamos KTX ni un pipeline propietario.

FSM-1 — Máquinas de estados (#171)
Reevalúa las necesidades de máquinas de estados para personajes, IA, combate, animación y comportamiento.
Analiza el código repetido en las demos.
Estudia bibliotecas maduras antes de considerar una implementación propia.
Determina qué integración resulta útil con el reloj, las escenas, los eventos y las animaciones.
No confundas las reglas de una máquina de estados con la infraestructura reutilizable que permite ejecutarlas.

INPUT
Revisa las decisiones que excluyeron el ratón del sistema de acciones y rebinding.
Considera videojuegos de estrategia, gestión, point-and-click y otros géneros.
Evalúa si existe una fragmentación innecesaria entre distintos mecanismos de entrada.

AUDIO
Reconsidera las capacidades de procesamiento, buses, efectos, ducking, crossfade y audio espacial cuando corresponda.
Distingue las capacidades que un juego puede implementar razonablemente de las que requieren acceso al backend de audio.
No descartes funcionalidades únicamente porque las demos actuales no las utilicen.

NAVEGACIÓN
Revisa las limitaciones relativas a evitación, tamaños de agentes, campos de flujo y navegación avanzada.
Determina qué corresponde al motor, qué puede ser opcional y qué debe permanecer en la lógica del juego.

FÍSICAS
Examina la cobertura de los contratos públicos respecto a las capacidades reales de los backends.
Presta atención a articulaciones, restricciones y simulaciones que actualmente podrían resultar inaccesibles.
Mantén la filosofía de Tiles por defecto y Box2D opcional mientras la reevaluación no justifique cambiarla.

GRÁFICOS Y SHADERS
Evalúa capacidades avanzadas como materiales, render targets, stencil, MSAA, mipmaps y posprocesado.
Revisa la ergonomía del sistema de shaders compilados mediante CLI y consumidos desde C#.
Distingue correctamente entre preparación fuera de línea, carga, utilización en runtime y administración de recursos.
Una herramienta CLI no es un problema por sí misma; el problema sería que el desarrollador deba reconstruir una integración que debería proporcionar el motor.

PERSISTENCIA
Reevalúa la gestión de versiones, migraciones, evolución de formatos y compatibilidad de guardados entre versiones publicadas de un juego.
Considera NativeAOT, trimming y la infraestructura de serialización existente.

LOCALIZACIÓN
Investiga si MicroEngine necesita ofrecer servicios reutilizables para traducciones, recursos localizados, cambios de idioma y relación con UI, fuentes y contenido.
No asumas que cada juego debería implementar toda esa infraestructura independientemente.

OTRAS DECISIONES HISTÓRICAS
Audita las evaluaciones anteriores, especialmente las relacionadas con GRAPHICS, AUDIO, NAV, WORLD e INPUT.
Identifica decisiones cerradas mediante argumentos que esta nueva metodología considera insuficientes.
Si existe evidencia para reabrirlas, hazlo.
No necesitas limitarte a las áreas enumeradas aquí.

## 6. Metodología obligatoria de evaluación

Para cada capacidad que necesite una decisión arquitectónica, responde:
A. Necesidad: ¿Qué tipo de videojuego necesita esta funcionalidad y qué problema resuelve?
B. Experiencia actual: ¿Qué tiene que escribir, configurar, conectar o mantener el desarrollador con MicroEngine tal como existe?
C. Alternativas: ¿Qué bibliotecas, herramientas, patrones y soluciones existentes podemos aprovechar?
D. Experiencia objetivo: ¿Qué ofrecería una integración razonable de MicroEngine? Demuéstralo con código o un flujo representativo.
E. Límite de responsabilidades: ¿Qué debería hacer el motor y qué corresponde al videojuego?
F. Integración: ¿Cómo encaja con los subsistemas actuales, sus contratos, ciclos de vida y mecanismos de sustitución?
G. Trade-offs: ¿Qué ganamos y qué perdemos en ergonomía, complejidad, mantenimiento, portabilidad, dependencias, memoria y rendimiento?
H. Decisión: ¿Conservar, ampliar, remodelar, reconstruir, añadir como módulo opcional o diferir justificadamente?
I. Validación: ¿Qué pruebas y escenarios demuestran que la solución proporciona la capacidad deseada?
No conviertas este método en burocracia innecesaria.
Úsalo para razonar y actuar correctamente.
Los benchmarks deben responder dudas concretas, no dirigir toda la investigación.

## 7. Política de reconstrucción

Utiliza cuatro niveles de intervención:
Conservar: la decisión supera la reevaluación y no necesita modificaciones sustanciales.
Ampliar: la arquitectura es correcta, pero faltan contratos, adaptadores o capacidades.
Remodelar: deben reorganizarse componentes, responsabilidades o APIs sin sustituir necesariamente todo el subsistema.
Reconstruir: el diseño fundamental no resulta adecuado y debe reemplazarse.
Puedes seleccionar cualquiera de los niveles.
No existe una penalización conceptual por reconstruir código existente.
Tampoco existe mérito en rehacer algo que ya está bien resuelto.
No persigas cambios por tamaño de diff, cantidad de funcionalidades o número de commits.
La calidad de la arquitectura y de la experiencia final es la prioridad.

## 8. Organización autónoma del trabajo

Diseña tu propio plan de ejecución a partir de los resultados de la auditoría.
Puedes crear iniciativas, issues subordinadas, dependencias, fases de integración y grupos de trabajo.
No estás obligado a resolver FSM, CONTENT y ECS en ese orden si el análisis transversal identifica otra secuencia más conveniente.
Procura mantener las modificaciones verificables y evita acumular cambios inconexos imposibles de diagnosticar.
Si un cambio modifica un contrato común, evalúa sus consumidores antes de iniciar intervenciones dependientes.
Mantén actualizados:

* Estado de las decisiones.
* Issues afectadas.
* Dependencias.
* Cambios ejecutados.
* Pruebas realizadas.
* Limitaciones conocidas.
* Riesgos pendientes.
* Criterios de cierre.

Puedes organizar la ejecución en múltiples PR o integraciones parciales.
No necesitas solicitar aprobación por cada cambio ordinario.
Sí debes detenerte ante decisiones que impliquen compromisos externos irreversibles, costes económicos adicionales, exposición de secretos o cambios fuera del ámbito autorizado del repositorio.
No fuerces soluciones cuando las herramientas o el entorno impidan verificarlas. Documenta claramente lo que quedó sin comprobar.

## 9. Implementación y calidad

Cada intervención debe entregar una capacidad completa y coherente, no solamente un prototipo técnico.
Cuando corresponda, incluye:

* Contratos públicos adecuados.
* Implementaciones funcionales.
* Integración con el ciclo de vida del motor.
* Manejo de errores.
* Gestión correcta de recursos.
* Pruebas unitarias y de integración.
* Compatibilidad entre backends.
* Evaluación de NativeAOT y trimming.
* Validación de plataformas aplicables.
* Documentación técnica y de usuario.
* Samples representativos.
* Migración de consumidores afectados.

Evita introducir rutas alternativas de uso innecesariamente incompatibles con las APIs normales del motor.
No aceptes soluciones cuya utilidad dependa de que cada videojuego reconstruya manualmente el mismo pegamento.
Cuando una biblioteca externa sea apropiada, intégrala mediante un módulo útil en lugar de reimplementarla sin necesidad.
No obstante, evita abstracciones que oculten capacidades importantes o se conviertan en fachadas sin valor.

## 10. Validación mediante videojuegos

La arquitectura no quedará validada únicamente porque sus pruebas automáticas pasen.
Debemos demostrar que resulta apropiada para desarrollar juegos.
Una vez consolidadas las capacidades fundamentales:

1. Moderniza las cuatro demos existentes con las APIs actuales.
2. Revisa los samples para que representen buenas prácticas y no soluciones manuales obsoletas.
3. Elimina infraestructura duplicada que ahora deba proporcionar el motor.
4. Valida casos integrados entre subsistemas.
5. Prepara la base para DEMO-5, DEMO-6 y DEMO-7.

No conviertas esta misión en una campaña de mejoras estéticas o contenido nuevo de las demos.
La modernización busca verificar la arquitectura y la experiencia de desarrollo.
Las tres demos futuras continuarán siendo pruebas de producción, no un requisito para aprobar artificialmente decisiones que puedan demostrarse con escenarios más pequeños.

## 11. Criterio de terminación

No consideres terminada la misión simplemente porque:

* Se cerraron las issues.
* Compila el repositorio.
* Pasan los tests.
* Mejoraron los benchmarks.
* La documentación está actualizada.
* Las demos actuales siguen funcionando.

Todas esas condiciones pueden ser necesarias, pero no suficientes.
La misión termina cuando exista evidencia razonable de que:

1. Las decisiones arquitectónicas cuestionadas fueron reevaluadas.
2. Las deficiencias reales y relevantes fueron corregidas o quedaron justificadamente delimitadas.
3. Los contratos y subsistemas forman una arquitectura coherente.
4. Las capacidades comunes están disponibles sin integración repetitiva innecesaria.
5. Los mecanismos de sustitución funcionan donde la modularidad los exige.
6. La experiencia de desarrollo es comprensible y consistente.
7. Los ejemplos y juegos de referencia utilizan correctamente el SDK.
8. No quedan regresiones conocidas sin tratar o documentar.
9. Los ADR reflejan las decisiones efectivamente implementadas.
10. El núcleo 2D dispone de una base sólida para los siguientes trabajos del roadmap.

No pretendas demostrar que el motor puede cubrir absolutamente todos los géneros y escenarios imaginables.
La meta es una consolidación rigurosa y razonable, no perfección infinita.

## 12. Instrucciones de inicio

Comienza inspeccionando el estado actual del repositorio, las issues, las PR y los ADR.
Realiza la auditoría transversal y determina la estrategia de ejecución.
Después toma las decisiones organizativas que consideres necesarias: crea o reabre issues, define dependencias, documenta la arquitectura objetivo y comienza las intervenciones siguiendo ese orden.
No te detengas después de entregar un diagnóstico. El diagnóstico es el comienzo del trabajo, no el resultado final.
Continúa ejecutando el plan, integrando y validando cambios dentro de tus capacidades y del entorno disponible.
Si encuentras un problema estructural que invalida una decisión anterior, replantéala y corrígela.
Si descubres que una solución existente es apropiada, consérvala.
Si una implementación necesita reconstruirse, reconstrúyela.
Si una biblioteca madura permite resolver correctamente el problema, reutilízala.
Si necesitas reorganizar las tareas, reorganízalas.
Tienes autonomía para elegir los medios.
La dirección arquitectónica, sin embargo, es inequívoca:
MicroEngine debe ser un motor de videojuegos, no una colección de componentes rápidos que el desarrollador tenga que ensamblar por su cuenta.
Tu misión es consolidar ese producto.
Empieza.
