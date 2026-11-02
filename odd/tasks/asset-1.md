# ASSET-1: Asset pipeline, scalable fonts and content management

Objective: a first asset pipeline so a developer can build a 2D HD game from external images, TTF/OTF fonts and atlases,
without code-generated art or pixel-art assumptions. Branch `feat/asset-1` from `main` at 98b1a7d. Delivery: work-unit
commits, one PR (chained on request), no merge or release without the owner. Test runner: `dotnet test MicroEngine.slnx
-c Release`; SDK: `node sdk/tests/*.mjs`.

## Specs

Quoted fragments are the owner's request (2026-11-02), verbatim.

- S1 Research: "Antes de diseñar una implementación propia, investiga las soluciones existentes en el ecosistema .NET y
  de desarrollo de videojuegos." "MonoGame Content Pipeline y sus herramientas actuales." "Bibliotecas de carga y
  procesamiento de imágenes." "Sistemas de rasterización y gestión de fuentes." "Herramientas de atlas y spritesheets."
  "Cachés y administradores de recursos." "Sistemas de empaquetado y transformación de contenido." "No reinventes
  soluciones maduras sin una razón técnica."
- S2 Fonts: "Incorpora soporte para fuentes TTF y OTF." "Cargar fuentes externas." "Utilizar múltiples familias
  tipográficas." "Trabajar con diferentes tamaños." "Renderizar texto escalable." "Medir texto para interfaces y
  disposición de elementos." "Gestionar fuentes de forma reutilizable." "Conservar las fuentes bitmap existentes." "El
  texto debe resultar apropiado tanto para juegos pixel art como para interfaces HD."
- S3 HD graphics: "Carga de imágenes externas." "Sprites HD." "Filtrado nearest y linear configurable." "Escalado
  adecuado." "Transparencia y regiones de textura." "Reutilización de recursos gráficos." "Gestión eficiente de memoria y
  texturas." "No impongas filtrado nearest ni escalado entero como comportamientos universales del motor."
- S4 Atlases: "Importar atlas." "Definir regiones y sprites." "Utilizar metadatos de recursos cuando corresponda."
  "Reutilizar texturas." "Trabajar con secuencias de animación." "Evitar cargas y duplicaciones innecesarias."
  "Mantén la compatibilidad funcional con los recursos que utilizan las cuatro demos actuales".
- S5 Lifecycle: "Identificación de assets." "Caché." "Reutilización." "Ciclo de vida." "Liberación de memoria."
  "Errores de carga." "Diagnósticos." "Carga síncrona o diferida según necesidades." "Interacción con los distintos
  backends." "Evalúa cuidadosamente las responsabilidades del motor, de la plataforma y del juego."
- S6 Pipeline: "Debe facilitar la incorporación de archivos externos sin obligar al desarrollador a escribir código de
  conversión para cada juego." "Investiga qué transformaciones conviene realizar durante la compilación y cuáles durante
  la ejecución." "Integra el resultado con el SDK, las plantillas y las herramientas CLI existentes cuando corresponda."
  "Mantén una experiencia sencilla para proyectos pequeños, sin exigir un proceso de compilación de assets cuando no
  aporte valor."
- S7 Packaging: Windows, Linux, macOS, Android, iOS, WebAssembly. "Aprovecha la infraestructura existente de CI,
  runners, emuladores, simuladores y herramientas de prueba."
- S8 Performance: load times, memory, caches, font rasterization, texture creation and reuse, backend transfers,
  avoidable allocations, gameplay behaviour. "evita introducir procesamiento costoso en el hilo de renderizado durante el
  juego." "Utiliza las herramientas de profiling existentes de PERF-1 y PERF-2."
- S9 Consumer: "Crea un proyecto pequeño de integración que utilice el SDK como lo utilizaría un desarrollador
  externo." external images, HD sprites, configurable filtering, TTF/OTF, several fonts and sizes, atlases,
  management and reuse, packaging and running. "No construyas todavía DEMO-5."
- S10 Docs: how to add assets, load images, scalable fonts, filtering, atlases, manage assets, ship games with assets;
  update templates and examples. "No introduzcas capas de compatibilidad obsoletas innecesarias."
- S11 Deliverables include limitations, recommendations for DEMO-5 and a PR ready for review. Acceptance: "un
  desarrollador debe poder crear un juego 2D HD con imágenes externas, fuentes escalables y recursos reutilizables, sin
  depender de recursos generados en código ni de decisiones visuales propias del pixel art."

## Tasks

| ID | Specs | Route | Status | Commit |
| --- | --- | --- | --- | --- |
| T1 Research and ADR 0025 | S1, S5, S6 | inline | pending | |
| T2 Texture filtering, smooth canvas scaling, HD windows, texture updates | S3, S8 | inline | pending | |
| T3 Image formats (StbImageSharp) and zero-copy uploads | S3, S8 | inline | pending | |
| T4 Scalable fonts (FontStashSharp), `IFont`, widgets | S2, S8 | inline | pending | |
| T5 Atlas import (TexturePacker/Aseprite JSON, Starling XML), timed animations | S4 | inline | pending | |
| T6 Asset manager: cache, scopes, diagnostics, preload | S5, S8 | inline | pending | |
| T7 Build integration: automatic Content items, checks, template | S6 | inline | pending | |
| T8 Consumer project and external check | S9, S7 | inline | pending | |
| T9 Packaging on every platform | S7 | inline + CI | pending | |
| T10 Measurements | S8 | inline | pending | |
| T11 Documentation, evaluation, PR | S10, S11 | inline | pending | |

Route evidence: no writer trigger holds (units depend on each other's contracts: textures → fonts → assets), so the
parent writes inline following this logbook.

## Log

- L1 (2026-11-02) The owner's request, verbatim, is the ASSET-1 mission text in the session that opened this branch
  (sections 1-11 and the acceptance criterion); S1-S11 quote it.
- L2 (2026-11-02) Exploration: the core decodes PNG in managed code, textures are always nearest, the canvas is a
  render target presented nearest with an integer-first fit (`FrameLayout.Fit`), `BitmapFont` is code-built only,
  `SpriteAtlasBuilder` packs `PixelImage`s at run time, `ContentFolder` resolves `Content/` (unpacked on Android, copied
  into the browser's virtual file system from `content.txt`). Every game declares its own `Content/**` item.
