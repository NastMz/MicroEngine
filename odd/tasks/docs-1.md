# DOCS-1: complete developer documentation and learning experience

Objective: turn the public documentation into a progressive, technically exact manual that teaches how to build a game
with the published SDK 0.4.0-alpha.1. Branch `docs/developer-manual` (from `main` at db5fa1b). Delivery: `single-pr`
(documentation, samples and checks reviewed together; the owner asked for one PR). Tests: `dotnet test
MicroEngine.slnx -c Release`, `npm test` and `npm run build` in `site/`, `node sdk/tests/docs.mjs --public`,
`node sdk/tests/external.mjs --public`.

## Specs

- S1 Sources: "Trabaja sobre el estado más reciente de `main`." Code, public APIs, the six nuget.org packages, CLI,
  templates, four demos, tests, SDL3 and raylib, distribution tools, internal docs. "Los documentos internos son
  material de investigación, no contenido que deba exponerse directamente al visitante." "No utilices nombres como
  PLATFORM-3, PRODUCT-1, INPUT-1 o SDK-1 en la documentación orientada a desarrolladores."
- S2 Audit of every page: correct, superficial, no examples, undocumented APIs, incomplete, obsolete, duplicated,
  confusing navigation, missing working examples, documented-but-missing, implemented-but-undocumented. "No descartes
  contenido preciso únicamente por reorganizar la documentación."
- S3 Learning paths: Getting Started, Tutorials, Engine Manual, API Reference, Platforms & Distribution, Advanced
  Topics. "Separa claramente los tutoriales para principiantes de la documentación avanzada."
- S4 Main tutorial from the official template covering the 17 listed steps "en el orden que resulte más natural". "El
  resultado debe ser un videojuego pequeño, funcional y comprensible." "No inventes sistemas inexistentes para
  completar artificialmente el tutorial." "Cuando una capacidad requiera lógica del propio juego porque MicroEngine
  todavía no proporcione una abstracción especializada, explícalo correctamente."
- S5 Manual per existing subsystem (problem, when, how, types, configuration, examples, options, relations,
  performance, cross-platform behaviour, known limits, common errors) for Core, Graphics, Input, Audio, Assets, Text,
  Persistence, Platforms, Distribution. "No presupongas que existe un ECS, framework general de UI, motor de física o
  pipeline avanzado de assets si todavía no están implementados."
- S6 API reference: "Evalúa herramientas existentes de documentación de APIs .NET antes de construir algo propio."
  "Prefiere reutilizar herramientas maduras." "No conviertas la referencia de API en un simple volcado de nombres de
  clases sin explicaciones."
- S7 Examples: "Los ejemplos de la documentación deben compilar y funcionar." "Implementa una estrategia de
  verificación automatizada para los ejemplos importantes." "Utiliza los paquetes públicos `0.4.0-alpha.1` como
  referencia" "Distingue claramente documentación de versión publicada y cambios en desarrollo." "Incluye una colección
  de ejemplos pequeños y enfocados, sin obligar al usuario a estudiar una demo completa para comprender una sola
  funcionalidad."
- S8 Developer questions answered concretely (scene, texture, sprite, walking, gamepad, touch, sound, text, save,
  Android, Windows executable, name and icon, other backend, own implementation).
- S9 Design and navigation: hierarchy, menus, search, cross links, code, diagrams, tutorial step navigation, mobile,
  readability, accessibility. "Conserva la identidad visual de MicroEngine." "No conviertas el manual en un dashboard".
- S10 Public vs internal: internal documents "no deben aparecer en la navegación ni en las rutas públicas generadas del
  sitio." "No añadas secciones de evidencia de CI, runners, dispositivos utilizados para pruebas ni enlaces a
  evaluaciones internas." "Distingue requisitos técnicos reales de particularidades de nuestros entornos de prueba."
- S11 SDK integration: real, verified instructions to install packages, CLI, templates, create, build, run, package,
  metadata, signing, update Alpha versions. "No utilices comandos inventados." "Comprueba los comandos contra los
  artefactos publicados en NuGet.org."
- S12 Validation: site build, links, navigation, examples, main tutorial, nuget.org install, snippets, responsive,
  accessibility, package and demo links; an onboarding test from scratch in an independent folder using only the
  public instructions. "Si el tutorial requiere pasos no documentados, corrígelo."
- S13 Constraints: no new engine subsystems, no API rewrites unless a real defect, no roadmap capabilities, no
  development code presented as published, no deleting correct docs to shorten pages, no shallow promotion. "La landing
  puede ser breve y comercial. El manual debe ser profundo y práctico."
- S14 Deliverables 1-13, including "Informe de lagunas reales del SDK descubiertas durante el trabajo" and "PR listo
  para revisión". "No publiques cambios en producción sin autorización."
- S15 Acceptance: "Un desarrollador que nunca haya utilizado MicroEngine debe poder instalar el SDK público, seguir el
  manual, construir un videojuego pequeño y empaquetarlo sin tener que abrir los proyectos de nuestras cuatro demos ni
  estudiar la implementación interna del motor."

## Tasks

| ID | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 | S1 S2 S10 S14 | inline | pending | audit and SDK gaps in docs/evaluations/docs-1.md |
| T2 | S3 S9 S10 | inline | pending | site structure: groups, tutorial and API discovery, snippet check |
| T3 | S4 S7 | inline | pending | samples: package/project switch, tutorial checkpoints, focused examples, tests |
| T4 | S3 S11 S15 | inline | pending | Getting started pages |
| T5 | S4 S15 | inline | pending | tutorial pages |
| T6 | S5 S13 | inline | pending | manual: new and corrected pages |
| T7 | S7 S8 | inline | pending | how-to and examples pages |
| T8 | S3 S5 S11 | inline | pending | platforms, distribution, advanced |
| T9 | S6 | inline | pending | API reference with DocFX |
| T10 | S11 S12 | inline | pending | validation runs |
| T11 | S14 | inline | pending | PR |

Route evidence: one writer (this session); no unit has a parallel independent partner large enough for a writer, so
every task is inline.

## Log

- L1 (2026-11-01, user request, verbatim):

  ```text
  MicroEngine — DOCS-1
  Complete Developer Documentation & Learning Experience
  Objetivo
  Transformar la documentación pública de MicroEngine en un manual completo, práctico, progresivo y técnicamente preciso, que permita a un desarrollador aprender a crear videojuegos utilizando el SDK público.
  Actualmente contamos con documentación que describe las capacidades y la arquitectura del motor, pero no explica suficientemente cómo utilizarlas para desarrollar juegos reales.
  La documentación debe dejar de limitarse a describir qué tiene MicroEngine y empezar a enseñar cómo se construye un videojuego con él.
  Esta misión no consiste en añadir párrafos a las páginas actuales. Requiere revisar la estructura, profundidad, ejemplos, navegación y experiencia de aprendizaje.
  1. Fuentes de verdad
  Trabaja sobre el estado más reciente de `main`.
  Utiliza como referencia:
  * El código fuente real de MicroEngine.
  * Los contratos y APIs públicas.
  * Los seis paquetes publicados en NuGet.org.
  * La CLI y las plantillas oficiales.
  * Las cuatro demos existentes.
  * Las pruebas automatizadas.
  * Las implementaciones SDL3 y Raylib.
  * Las herramientas de distribución.
  * Los documentos internos de arquitectura y desarrollo.
  Los documentos internos son material de investigación, no contenido que deba exponerse directamente al visitante.
  No publiques ni enlaces desde el sitio documentos de misiones, evaluaciones, ADRs, informes de CI o registros internos.
  No utilices nombres como PLATFORM-3, PRODUCT-1, INPUT-1 o SDK-1 en la documentación orientada a desarrolladores.
  2. Auditoría de la documentación
  Recorre todas las páginas de documentación existentes y clasifica:
  * Contenido correcto y útil.
  * Explicaciones demasiado superficiales.
  * Conceptos sin ejemplos.
  * APIs sin documentación suficiente.
  * Instrucciones incompletas.
  * Referencias obsoletas.
  * Duplicación de contenido.
  * Navegación confusa.
  * Falta de ejemplos funcionales.
  * Características documentadas que todavía no existen.
  * Capacidades implementadas que no están documentadas.
  No descartes contenido preciso únicamente por reorganizar la documentación.
  3. Estructura de aprendizaje
  Organiza la documentación en recorridos diferenciados.
  Getting Started
  Debe permitir instalar el SDK desde NuGet.org, instalar las herramientas, crear un proyecto, ejecutar el juego y comprender su estructura.
  Tutorials
  Tutoriales progresivos para construir videojuegos reales desde cero.
  Engine Manual
  Documentación profunda de los subsistemas, con explicaciones, opciones, comportamientos y ejemplos.
  API Reference
  Referencia consultable de contratos, tipos y métodos públicos.
  Platforms & Distribution
  Compilación, empaquetado, requisitos y distribución en Windows, Linux, macOS, Android, iOS y WebAssembly.
  Advanced Topics
  Extensibilidad, backends personalizados, integración de subsistemas, configuración avanzada y arquitectura pública.
  Separa claramente los tutoriales para principiantes de la documentación avanzada.
  4. Tutorial principal: crear un videojuego completo
  Desarrolla un tutorial progresivo que comience desde un proyecto generado con las plantillas oficiales.
  Debe enseñar, en el orden que resulte más natural:
  1. Crear y ejecutar el proyecto.
  2. Entender la clase principal del juego.
  3. Crear y administrar escenas.
  4. Dibujar un personaje.
  5. Cargar sprites externos.
  6. Procesar acciones de entrada.
  7. Mover un personaje.
  8. Aplicar animaciones.
  9. Trabajar con cámara y coordenadas.
  10. Detectar interacciones y colisiones utilizando las capacidades disponibles.
  11. Reproducir efectos de sonido y música.
  12. Mostrar texto e interfaz.
  13. Gestionar estados del juego.
  14. Guardar y recuperar información.
  15. Incorporar un menú.
  16. Compilar y empaquetar el juego.
  17. Ejecutarlo en otro destino compatible.
  El resultado debe ser un videojuego pequeño, funcional y comprensible.
  No inventes sistemas inexistentes para completar artificialmente el tutorial.
  Cuando una capacidad requiera lógica del propio juego porque MicroEngine todavía no proporcione una abstracción especializada, explícalo correctamente.
  5. Manual de subsistemas
  Para cada subsistema realmente existente, documenta:
  * Qué problema resuelve.
  * Cuándo utilizarlo.
  * Cómo funciona.
  * Tipos y contratos principales.
  * Cómo configurarlo.
  * Cómo utilizarlo mediante ejemplos.
  * Opciones disponibles.
  * Relación con otros subsistemas.
  * Consideraciones de rendimiento.
  * Comportamiento multiplataforma.
  * Limitaciones funcionales conocidas.
  * Errores habituales.
  Cubre especialmente:
  Core: ciclo de vida, sesiones, escenas, tiempo, estados y actualización.
  Graphics: renderer, sprites, atlas, texturas, cámaras, escalado, capas, partículas y primitivas realmente implementadas.
  Input: acciones, bindings, teclado, ratón, gamepads, analógicos, controles táctiles, layouts, safe areas y fuentes personalizadas.
  Audio: reproducción, formatos, efectos, música y controles disponibles.
  Assets: organización, carga, rutas, recursos externos y formatos admitidos.
  Text: fuentes actuales, renderizado de texto y limitaciones de tipografías.
  Persistence: almacenamiento y recuperación de datos.
  Platforms: SDL3, Raylib, WebAssembly, configuración, perfiles y extensibilidad.
  Distribution: CLI, empaquetado, metadatos, iconos, versiones, firma y publicación.
  No presupongas que existe un ECS, framework general de UI, motor de física o pipeline avanzado de assets si todavía no están implementados.
  6. Referencia de API
  Evalúa herramientas existentes de documentación de APIs .NET antes de construir algo propio.
  Prefiere reutilizar herramientas maduras.
  Genera o integra una referencia técnica navegable a partir de los contratos públicos reales cuando resulte viable.
  Incluye descripciones útiles y ejemplos representativos.
  No conviertas la referencia de API en un simple volcado de nombres de clases sin explicaciones.
  Documenta diferencias relevantes entre implementaciones, parámetros, valores predeterminados y comportamiento observable.
  7. Ejemplos ejecutables y verificados
  Los ejemplos de la documentación deben compilar y funcionar.
  No aceptes fragmentos conceptuales presentados como código ejecutable.
  Implementa una estrategia de verificación automatizada para los ejemplos importantes.
  Utiliza los paquetes públicos `0.4.0-alpha.1` como referencia para las guías correspondientes a la versión publicada.
  Si el código de `main` tiene funcionalidades todavía no publicadas, no las presentes como disponibles en esa versión.
  Distingue claramente documentación de versión publicada y cambios en desarrollo.
  Incluye una colección de ejemplos pequeños y enfocados, sin obligar al usuario a estudiar una demo completa para comprender una sola funcionalidad.
  8. Experiencia de desarrollador
  La documentación debe responder fácilmente preguntas como:
  * ¿Cómo creo una escena?
  * ¿Cómo dibujo una textura?
  * ¿Cómo cargo un sprite?
  * ¿Cómo hago que el personaje camine?
  * ¿Cómo configuro un mando?
  * ¿Cómo agrego controles táctiles?
  * ¿Cómo reproduzco sonido?
  * ¿Cómo dibujo texto?
  * ¿Cómo guardo una partida?
  * ¿Cómo compilo para Android?
  * ¿Cómo genero un ejecutable Windows?
  * ¿Cómo personalizo nombre e icono?
  * ¿Cómo utilizo otro backend?
  * ¿Cómo integro una implementación propia?
  El lector debe encontrar respuestas concretas, ejemplos y explicaciones, no únicamente afirmaciones de que MicroEngine posee determinada capacidad.
  9. Diseño y navegación
  Revisa la experiencia de lectura:
  * Índice y jerarquía.
  * Menús y navegación.
  * Búsqueda.
  * Enlaces entre temas.
  * Fragmentos de código.
  * Ejemplos destacados.
  * Diagramas cuando aporten claridad.
  * Navegación entre pasos de tutoriales.
  * Adaptación móvil.
  * Legibilidad.
  * Accesibilidad.
  Conserva la identidad visual de MicroEngine.
  No conviertas el manual en un dashboard ni en un conjunto de páginas decorativas.
  Prioriza la utilidad y claridad técnica.
  10. Contenido público frente a contenido interno
  Mantén una frontera editorial estricta.
  La documentación pública describe el producto y enseña a usarlo.
  La documentación interna registra decisiones de ingeniería, investigaciones, misiones, implementación y validaciones.
  Los documentos internos pueden seguir disponibles en el repositorio para mantenedores, pero no deben aparecer en la navegación ni en las rutas públicas generadas del sitio.
  No añadas secciones de evidencia de CI, runners, dispositivos utilizados para pruebas ni enlaces a evaluaciones internas.
  Los requisitos de plataformas deben basarse en la combinación de tecnologías subyacentes y restricciones propias del motor.
  Distingue requisitos técnicos reales de particularidades de nuestros entornos de prueba.
  11. Integración con el SDK publicado
  La documentación debe ofrecer instrucciones reales y verificadas para:
  * Instalar paquetes NuGet.
  * Instalar la CLI.
  * Instalar las plantillas.
  * Crear proyectos.
  * Compilar.
  * Ejecutar.
  * Empaquetar para distintas plataformas.
  * Configurar metadatos.
  * Administrar la firma.
  * Actualizar versiones Alpha.
  No utilices comandos inventados.
  Comprueba los comandos contra los artefactos publicados en NuGet.org.
  12. Validación integral
  Ejecuta:
  * Compilación del sitio.
  * Comprobación de enlaces.
  * Pruebas de navegación.
  * Validación de ejemplos.
  * Pruebas del tutorial principal.
  * Instalación desde NuGet.org.
  * Revisión de fragmentos de código.
  * Pruebas responsive.
  * Comprobación de accesibilidad.
  * Verificación de enlaces a paquetes y demos.
  Realiza también una prueba de incorporación desde cero: en un directorio independiente, sigue únicamente las instrucciones públicas y construye el proyecto de ejemplo sin consultar código interno de MicroEngine.
  Si el tutorial requiere pasos no documentados, corrígelo.
  13. Restricciones
  No implementar nuevos subsistemas del motor durante esta misión.
  No reescribir APIs para simplificar un tutorial salvo que se encuentre un defecto real y se justifique su corrección.
  No crear capacidades que todavía pertenecen al roadmap.
  No presentar código de desarrollo como si ya estuviera publicado en NuGet.org.
  No eliminar documentación técnicamente correcta para acortar las páginas.
  No convertir la documentación en material promocional superficial.
  La landing puede ser breve y comercial. El manual debe ser profundo y práctico.
  14. Entregables
  Al finalizar entrega:
  1. Nueva estructura de documentación.
  2. Getting Started verificado.
  3. Tutorial completo y progresivo.
  4. Manual profundo de las APIs y subsistemas existentes.
  5. Referencia de API.
  6. Ejemplos ejecutables.
  7. Validación automatizada de ejemplos.
  8. Guías de distribución actualizadas.
  9. Navegación y búsqueda revisadas.
  10. Documentación coherente con NuGet.org.
  11. Informe de lagunas reales del SDK descubiertas durante el trabajo.
  12. Resultados de pruebas.
  13. PR listo para revisión.
  Trabaja autónomamente sobre una rama dedicada basada en el `main` actual.
  Puedes investigar, reorganizar, redactar, implementar ejemplos y ejecutar validaciones sin solicitar autorización en cada paso.
  No publiques cambios en producción sin autorización.
  Criterio final de aceptación
  Un desarrollador que nunca haya utilizado MicroEngine debe poder instalar el SDK público, seguir el manual, construir un videojuego pequeño y empaquetarlo sin tener que abrir los proyectos de nuestras cuatro demos ni estudiar la implementación interna del motor.
  El resultado debe sentirse como la documentación de un motor de videojuegos real, no como una presentación de sus características.
  ```

- L2 (2026-11-01) Exploration. `main` at db5fa1b equals the published SDK: `git diff sdk-v0.4.0-alpha.1..main -- src
  templates` changes one template README line, so documenting `main` documents 0.4.0-alpha.1. Existing docs: 4.7k lines,
  a deep and mostly accurate manual, but no progressive tutorial, no API reference, `making-a-game.md` still describes
  repository project references and says browser packages are unavailable, examples lean on demo code. API reference
  tool: DocFX 2.81 `metadata` with `outputFormat: markdown` produces 147 linked pages from the four engine assemblies
  and their XML docs; chosen over writing a generator.
