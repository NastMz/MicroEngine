# SITE-REDESIGN: visual and conceptual identity of MicroEngine's public site

Objective: give MicroEngine a recognisable product identity and apply it to the whole public site (landing, demos,
players, platforms, roadmap, Starlight docs). Branch `feat/site-redesign` (from `main` at b551c7c). Delivery: work units
merged into the feature branch, then one PR `feat/site-redesign` → `main` for the owner's review; the site is not
published (`.github/phase-freeze` stays). Checks: `npm ci && SITE_DEMOS=none npm run build`, `npm test`,
`node scripts/snippets.mjs` in `site/`; `npm run test:browser` on a build with demos (Chrome, Firefox).

## Specs

- S1 Understand first: review the current site, its implementation, assets, content and navigation, and the engine's
  documentation and evolution, "especialmente ARCH-1". "Distingue con rigor las capacidades disponibles de las que
  están en reconstrucción, experimentación o planificación." "La identidad visual actual no debe limitar la percepción
  del motor a sus primeras demos pixel art. Tampoco debemos ocultar ese origen".
- S2 Research and art direction: study engines and tools beyond their landing pages; "no es copiar Godot, Unity,
  Unreal, Defold o MonoGame". "Evita caer en una estética genérica de SaaS, documentación técnica indiferenciada o
  videojuego retro." Explore alternatives, then select one direction grounded in MicroEngine's particularities.
- S3 Visual identity: symbols, brand, logo, iconography, typography, palette, composition, illustrations and diagrams,
  motion, code treatment, demo and screenshot presentation, docs identity, responsive and accessibility. "La nueva
  identidad debería ser reconocible tanto en una página completa como en elementos pequeños, por ejemplo una tarjeta de
  demo, un encabezado o una ilustración técnica."
- S4 Narrative: what MicroEngine is, for whom, what differentiates it. "No quiero que el sitio parezca únicamente una
  galería de cuatro videojuegos." Cover value proposition, modularity, developer experience, graphics capabilities,
  platforms, SDK tools, examples and demos, real project status, docs and first steps, vision and roadmap. "No anuncies
  como disponibles las funcionalidades 3D, los backends o los sistemas que todavía estén en desarrollo."
- S5 Whole-site redesign: landing, demo pages, players, navigation, product sections, Starlight docs. "No quiero una
  landing espectacular acompañada de documentación que parece pertenecer a otro proyecto." "Conserva la funcionalidad
  de las demos, el acceso a la documentación y las capacidades existentes del sitio."
- S6 Own visual resources: not only "iconos genéricos, gradientes, tarjetas y capturas colocadas sin una dirección
  artística". "Prioriza recursos reproducibles, mantenibles y adecuados para distribución pública, respetando las
  licencias correspondientes."
- S7 Quality: performance and load times, accessibility, keyboard navigation, responsive, docs and code legibility,
  supported browsers, WebAssembly players, lazy loading of heavy resources, consistency, frontend maintainability.
  "Evita introducir dependencias pesadas o efectos costosos sin una justificación clara."
- S8 Way of working: browser tests, captures and visual reviews at several resolutions; "Comprueba el resultado real,
  no únicamente que los componentes compilen." "No des por terminado el trabajo mientras existan secciones visualmente
  desconectadas, problemas evidentes de composición o regresiones funcionales."
- S9 Deliverables: renewed identity; documented, reusable art direction; coherent design system; own graphic assets;
  redesigned landing and public pages; docs visually integrated; demos and players working; functional, visual,
  responsive and accessibility tests; explanation of the main creative decisions. "Entrega el trabajo mediante PR para
  revisión. No publiques el sitio rediseñado sin autorización."
- S10 Constraints inherited from the repository (AGENTS.md): public pages carry no engineering material (code names,
  decision records, evaluations, task records, CI); demos keep their "all rights reserved" notice; quoted code equals
  its sample region; unpublished capabilities are labelled "next SDK version"; every `site/scripts/check-dist.mjs`
  rule keeps passing.

## Direction (decided 2026-11-05)

"Socket": the engine as a board of labelled sockets where swappable modules seat (graphics device, physics, audio
sink, platform host, input). One product-true device repeated everywhere: the slot-and-module mark, family colours
that mean a module type, dashed empty sockets for what is not published, upstream technologies (SDL, Box2D, glTF,
.NET) drawn as credited modules. Docs device: code shown with the frame strip that code produces, captured headlessly by
the engine. Pixel art is one renderer's material, not the brand's costume. Rejected: "Fixed Step" alone (austere,
niche), "Resolution" (promises 3D before it ships), "Standing on shoulders" (credits others over MicroEngine's own
value). Research and the site map are summarised in `site/design/README.md` (T1).

## Tasks

| ID | Specs | Task | Route | Status |
|---|---|---|---|---|
| T1 | S1-S3, S6, S9 | Art direction doc, tokens, type, mark and logo set (explored variants, 16 px test), family icons, favicon, OG image | delegated (context backstop) | done, branch `feat/site-redesign-t1-brand`, PR #92 merged into `feat/site-redesign` at ea30d6e |
| T2 | S3-S6 | Site shell (header, nav, footer) and landing narrative with the socket-board illustrations | delegated | done, branch `feat/site-redesign-t2-landing`, PR #94 merged into `feat/site-redesign` at 71c5ff0 |
| T3 | S3, S5, S7 | Demo index and demo pages, player integration | delegated, parallel with T4/T5 | done, branch `feat/site-redesign-t3-demos`, PR #96 merged into `feat/site-redesign` at e718a10 |
| T4 | S3, S5, S7 | Starlight docs theme, code blocks, docs header; platforms, roadmap, 404 pages | delegated, parallel with T3/T5 | done, branch `feat/site-redesign-t4-docs`, PR #95 merged into `feat/site-redesign` at 05335ed |
| T5 | S1, S4, S10 | Content truth: stale claims in site data and public docs against `main` | delegated, parallel with T3/T4 | done, branch `feat/site-redesign-t5-content`, PR #93 merged into `feat/site-redesign` at f8137aa |
| T6 | S7-S9 | Browser, visual, responsive and accessibility checks; creative rationale; PR to `main` | delegated + architect review | done, branch `feat/site-redesign-t6-close` (PR into `feat/site-redesign`); the PR to `main` is the architect's |

## Log

- L1 (2026-11-05, owner's request, verbatim):

> SITE-REDESIGN — Renacimiento visual y conceptual de MicroEngine
> Misión
> Quiero realizar un rediseño integral del sitio público de MicroEngine.
> No busco un lavado de cara, un cambio de colores ni una reorganización superficial de componentes. Quiero construir una identidad visual y de producto propia, reconocible y coherente con la nueva visión del motor.
> Tú conoces MicroEngine desde sus primeras implementaciones: su filosofía, sus capacidades, sus demos, sus decisiones arquitectónicas, su SDK, su infraestructura multiplataforma y la reconstrucción fundacional ARCH-1.
> Utiliza ese conocimiento para liderar la nueva dirección artística y conceptual del sitio.
> El objetivo es que alguien pueda ver una sección, una ilustración, una composición o un elemento característico y reconocer que pertenece a MicroEngine.
> 1. Comprender el producto antes de diseñar
> Comienza revisando el sitio actual, su implementación, sus recursos visuales, su contenido y su experiencia de navegación.
> Analiza también la documentación y la evolución arquitectónica del motor, especialmente ARCH-1.
> Identifica qué comunica actualmente el sitio, qué funciona, qué se ha quedado desactualizado y qué percepción genera sobre el proyecto.
> MicroEngine nació desarrollando videojuegos 2D pixel art, pero su visión es mucho más amplia:
>
> * Motor generalista para videojuegos 2D, 2.5D y 3D.
> * Desarrollo principalmente mediante C#/.NET.
> * Núcleo modular y subsistemas reemplazables.
> * Configuración plug-and-play con posibilidades de personalización.
> * Soporte multiplataforma.
> * SDK, CLI, herramientas y documentación.
> * Demos jugables que validan sus capacidades.
> * Una filosofía de aprovechar tecnologías existentes sin reinventar innecesariamente cada subsistema.
>
> La identidad visual actual no debe limitar la percepción del motor a sus primeras demos pixel art.
> Tampoco debemos ocultar ese origen: es parte de la historia del proyecto.
> Distingue con rigor las capacidades disponibles de las que están en reconstrucción, experimentación o planificación.
> 2. Investigar referencias y construir una dirección artística
> Estudia cómo presentan su identidad los motores, frameworks y herramientas de desarrollo de videojuegos relevantes.
> No te limites a analizar sus landing pages. Investiga también su lenguaje visual, ilustraciones, documentación, comunicación de características, experiencias interactivas y relación entre identidad de marca y producto técnico.
> Busca referencias tanto dentro como fuera de la industria de videojuegos cuando resulten útiles.
> El objetivo no es copiar Godot, Unity, Unreal, Defold o MonoGame, sino comprender qué decisiones de diseño funcionan y encontrar una personalidad propia para MicroEngine.
> Tienes autonomía para definir la dirección artística.
> No te estoy imponiendo una paleta, una tipografía, una mascota, un estilo de ilustración ni una composición específica.
> Quiero que investigues, explores alternativas y selecciones una dirección convincente, sustentada en las particularidades de MicroEngine.
> Evita caer en una estética genérica de SaaS, documentación técnica indiferenciada o videojuego retro.
> El resultado debe sentirse como un producto tecnológico creativo, con carácter propio.
> 3. Reconstruir la identidad visual
> Desarrolla un lenguaje visual coherente que pueda extenderse más allá de la página principal.
> Considera:
>
> * Identidad gráfica y símbolos reconocibles.
> * Marca, logotipo e iconografía.
> * Tipografía y jerarquías.
> * Paleta y sistema cromático.
> * Composición, ritmo visual y uso del espacio.
> * Ilustraciones, diagramas y recursos gráficos propios.
> * Movimiento, transiciones e interacciones.
> * Tratamiento visual de fragmentos de código.
> * Presentación de demos y capturas.
> * Identidad de las páginas de documentación.
> * Diseño responsive y accesibilidad.
>
> No todos estos elementos tienen que cambiar por obligación. Evalúa qué merece conservarse, evolucionar o sustituirse.
> Busca coherencia entre todos ellos, no una colección de efectos visuales inconexos.
> La nueva identidad debería ser reconocible tanto en una página completa como en elementos pequeños, por ejemplo una tarjeta de demo, un encabezado o una ilustración técnica.
> 4. Replantear la narrativa del sitio
> El diseño debe comunicar con claridad qué es MicroEngine, a quién está dirigido y qué lo diferencia.
> No quiero que el sitio parezca únicamente una galería de cuatro videojuegos.
> Las demos son importantes porque demuestran que el motor funciona, pero el verdadero producto es MicroEngine.
> Revisa cómo presentamos:
>
> * La propuesta de valor del motor.
> * Su filosofía de modularidad.
> * Su experiencia de desarrollo.
> * Sus capacidades gráficas.
> * Las plataformas soportadas.
> * Las herramientas del SDK.
> * Los ejemplos y demos.
> * El estado real del proyecto.
> * La documentación y los primeros pasos.
> * La visión y el roadmap.
>
> Define una narrativa visual y editorial que permita entender el producto rápidamente y profundizar después.
> Mantén rigor técnico. No anuncies como disponibles las funcionalidades 3D, los backends o los sistemas que todavía estén en desarrollo.
> La ambición puede comunicarse sin confundir presente y futuro.
> 5. Rediseño integral del sitio
> Una vez definida la dirección artística, aplícala de manera consistente al sitio completo.
> Esto incluye la landing, las páginas de demos, la integración de los reproductores, la navegación, las secciones de producto y la documentación pública basada en Astro/Starlight.
> No quiero una landing espectacular acompañada de documentación que parece pertenecer a otro proyecto.
> La identidad debe extenderse a toda la experiencia, respetando las necesidades particulares de cada sección.
> Puedes reorganizar componentes, layouts, estilos, recursos e implementación frontend si el diseño lo justifica.
> Conserva la funcionalidad de las demos, el acceso a la documentación y las capacidades existentes del sitio.
> 6. Recursos visuales propios
> Dedica especial atención a los recursos gráficos.
> Quiero evitar que el resultado dependa exclusivamente de iconos genéricos, gradientes, tarjetas y capturas colocadas sin una dirección artística.
> Investiga y desarrolla una propuesta visual propia para representar los conceptos fundamentales del motor.
> Puedes utilizar herramientas de generación gráfica, SVG, ilustraciones, composiciones técnicas, animaciones y otros recursos apropiados.
> Los elementos deben formar parte de un mismo lenguaje gráfico y estar al servicio de la identidad.
> Prioriza recursos reproducibles, mantenibles y adecuados para distribución pública, respetando las licencias correspondientes.
> 7. Experiencia y calidad
> El rediseño no debe sacrificar la utilidad del sitio.
> Presta atención a:
>
> * Rendimiento y tiempos de carga.
> * Accesibilidad.
> * Navegación mediante teclado.
> * Responsive.
> * Legibilidad de documentación y código.
> * Compatibilidad con los navegadores soportados.
> * Integración de los reproductores WebAssembly.
> * Carga diferida de recursos pesados.
> * Consistencia visual.
> * Mantenibilidad del frontend.
>
> Evita introducir dependencias pesadas o efectos costosos sin una justificación clara.
> La calidad técnica del sitio también debe representar la calidad técnica que aspiramos a ofrecer con el motor.
> 8. Forma de trabajo
> Toma la iniciativa creativa y técnica.
> Organiza la investigación, dirección artística, implementación y validación mediante agentes especializados cuando resulte útil.
> Explora alternativas antes de seleccionar la dirección definitiva, pero no conviertas la misión en una sucesión interminable de propuestas sin implementación.
> Una vez establecida una dirección fundamentada, ejecútala con coherencia.
> Realiza pruebas de navegador, capturas y revisiones visuales en diferentes resoluciones. Comprueba el resultado real, no únicamente que los componentes compilen.
> No des por terminado el trabajo mientras existan secciones visualmente desconectadas, problemas evidentes de composición o regresiones funcionales.
> Coordina el trabajo con ARCH-1 para evitar conflictos innecesarios y para que la comunicación del sitio corresponda al estado real del motor.
> 9. Entregables
> Al finalizar, quiero encontrar:
>
> 1. Una identidad visual y conceptual renovada para MicroEngine.
> 2. Una dirección artística documentada y reutilizable.
> 3. Un sistema de diseño coherente.
> 4. Recursos gráficos propios y reconocibles.
> 5. Landing y páginas públicas rediseñadas.
> 6. Documentación integrada visualmente con el resto del producto.
> 7. Demos y reproductores funcionando correctamente.
> 8. Pruebas funcionales, visuales, responsive y de accesibilidad.
> 9. Una explicación de las decisiones creativas principales y de cómo representan la filosofía de MicroEngine.
>
> Entrega el trabajo mediante PR para revisión. No publiques el sitio rediseñado sin autorización.
> Criterio de éxito
> No quiero simplemente que MicroEngine tenga un sitio más moderno.
> Quiero que tenga una identidad de producto reconocible, capaz de acompañar su evolución desde los juegos 2D actuales hacia HD, 2.5D y 3D.
> Que transmita ingeniería sólida sin parecer un producto corporativo genérico.
> Que exprese creatividad sin quedar encerrado en una estética retro.
> Que muestre capacidades reales sin exagerarlas.
> Y, sobre todo, que no parezca una plantilla adaptada a MicroEngine, sino un sitio diseñado específicamente para MicroEngine.
> Tienes libertad para encontrar esa identidad. Quiero ver tu propuesta artística convertida en un producto funcional, coherente y distintivo.

- L2 (2026-11-05): mapping (site map: Astro 7.4.4 + Starlight 0.42.8, plain CSS tokens, Pixelify Sans + IBM Plex, amber
  "pixel workbench at night", dark-only landing; players load only on Play; `check-dist.mjs`, `repo-docs.mjs` and the
  puppeteer browser suite in Chrome and Firefox are the guard rails; production deploys are frozen by
  `.github/phase-freeze`) and research (Godot, Unity, Unreal, Defold, MonoGame, Bevy, raylib, LÖVE, Stride, FNA, PICO-8,
  Flax, Heaps; Zig, Rust, Astro, Linear, Stripe docs, Teenage Engineering, Playdate, Tailwind). Four concepts weighed;
  "Socket" chosen with the frame-strip docs device (see Direction). Stale claims found: raylib named as the browser
  platform, `0.4.0-alpha.1` in the roadmap data, "2D only / no physics engine" statements, the landing's catalog
  excerpt over-matching, no mention of physics, the scene world, HD rendering or TTF fonts.
- L3 (2026-11-05): CI runs only on pull requests to `main`, so unit PRs into `feat/site-redesign` are verified locally by
  their writer and spot-checked by the architect; the final PR to `main` runs the full CI.
- L4 (2026-11-05): T1 brand foundation. Mark: three constructions drawn on a 16-unit grid and rasterised at
  16/32/64/256 px, one colour and full colour, light and dark (`site/design/explorations/`); chosen B "m-socket" (the
  kernel bar with two sockets, one module seated: reads as "m", holds at 16 px in one colour); A (corner module) read
  as a generic copy icon, C (`[■]`) became the Tools family icon. Geometry lives in
  `site/src/components/brand/geometry.mjs`; logo files are generated from it (`site/design/tools/build-assets.mjs`) and
  a test fails on drift. Tokens rebuilt in OKLCH with `light-dark()`: warm paper and graphite neutrals, amber signal,
  six family hues plus a neutral kernel, status tokens (only what ships gets colour; future drawn dashed/dotted in
  neutral), game tones kept. Decision: the landing and demo pages get both themes (system preference, and Starlight's
  stored choice); code panels and the player stage stay graphite in both (`color-scheme: dark`). Display face
  Bricolage Grotesque 600 (OFL) replaces Pixelify Sans; Plex kept. `site/scripts/contrast.mjs` checks 198 pairs (AA,
  both themes, inside sRGB). Components in `site/src/components/brand/`: Mark, Lockup, FamilyIcon, Socket, Callout,
  CodeLabel, FrameStrip; shared rules in `site/src/styles/brand.css`. OG image now drawn at build time from the
  lockup and a socket board (four shipping families, one empty dashed socket). Art direction: `site/design/README.md`
  (not published). Checks: `npm ci`; `SITE_DEMOS=none npm run build` (check-dist complete); `npm test` 51/51;
  `node scripts/snippets.mjs` 104 blocks; contrast 198/198. Screenshots of /, /demos/neon-rift/ and a docs page,
  light and dark, 1280 and 375: no horizontal overflow; two light-theme defects found and fixed (hero code figure and
  player panel title inheriting light ink).
- L5 (2026-11-05): T5 content truth. New `site/src/data/capabilities.ts`: 25 modules with `status` (`available` |
  `next` | `repository` | `planned`), `family` (kernel, graphics, physics, audio, input, platform, tools), contract,
  credited upstream technologies and a docs link; `publishedSdk` is read from `sdk/version.json`. `roadmap.ts` keeps
  `completed`, `next`, `vision` and `statusLabels`, and adds `stages` and `roadmap` (every item with status and family;
  planned items carry `horizon` and `track`); the SDK version is no longer written in the data (it said alpha.1).
  `platforms.ts` rows gain `status`, `family` and `runtimeNext` (browser: SDL 3 in the next SDK version). Verified
  against `src/` packaging: `MicroEngine.Graphics.SdlGpu` is bundled into the `MicroEngine.Sdl` package
  (`MicroEngine.Sdl.csproj`), so the SDL_GPU device is `next` (desktop, opt-in), not repository-only as the unit rules
  assumed; `MicroEngine.Graphics3D` is packed nowhere and Box2D only on request: both `repository`. Docs corrected:
  guide `status`, `introduction`, `sdk` (next version's package contents), `getting-microengine`; `howto`; tutorial
  README, 07 and 13; root README. Not in T5's surfaces: `docs/reference/limitations.md` still says "2D only; no 3D,
  physics engine" and that TrueType fonts are not supported. Tests: three content tests in `site/tests/site.test.mjs`.
  Checks: `npm ci`; `SITE_DEMOS=none npm run build` (check-dist complete); `npm test` 54/54; snippets 104 blocks;
  contrast 198/198; screenshots of /roadmap/, /platforms/ and four docs pages, light and dark, at 1280/768/375/320:
  no horizontal overflow, no script errors.
- L6 (2026-11-05): T5 extended at the architect's request (SDL_GPU finding confirmed). `docs/reference/limitations.md`:
  "2D only; no 3D, physics engine" split into 3D (repository-only minimal path), physics (tile collision today,
  physics worlds next, Box2D repository-only) and networking/scripting rows; fonts row says TTF/OTF arrive with the next
  SDK version and BMFont files are not read. `site/design/README.md` §7: SDL_GPU is a seated `next` module; Graphics3D
  and Box2D stay repository-only. Checks rerun: build (check-dist complete), `npm test` 54/54, snippets 104, contrast
  198/198; limitations page screenshots in both themes at 1280/768/375/320, no overflow.
- L7 (2026-11-05): T2 shell and landing. Shell: plain paper (the 32 px grid is gone), header nav on one line at every
  width (its own line under the lockup below 48 rem), current page marked by a short amber bar, grouped footer opening
  with the board's 2 px ink edge, flat buttons (offset shadows removed). Landing rewritten around the engine: opening
  (claim, who it is for, an SVG board drawing with callouts, a facts line), the stance (SDL 3, raylib, .NET 10, NVorbis as
  credited modules), the socket board driven by `site/src/data/capabilities.ts` (kernel bar with its modules, six family
  bays, dashed/dotted empty sockets for repository/planned, an extension point for custom platforms), the desktop
  catalog code (excerpt now ends at the blank line after `CreateCatalog`, so the `Run` overloads no longer show),
  developer experience (template, CLI, the HelloEngine sprite and its rendering), graphics (a Meadow Run frame strip
  and four graphics sockets), a compact platform list, the four demos as `DemoCard`s, status (Alpha meaning, the four
  availability levels with module counts, limits), first steps, and direction. `.sprite-code` removed from `brand.css`.
  Architect corrections applied: SDL_GPU is a seated `next` module (from the capability list); demo wording "desktop and
  mobile browsers, with a keyboard, a gamepad or on-screen touch controls". Checks: `npm ci`; `SITE_DEMOS=none npm run
  build` (check-dist complete); `npm test` 56/56; snippets 104; contrast 198/198. Screenshots of / (1280/768/414/375/320) and /demos/,
  /demos/neon-rift/, /platforms/, /roadmap/ and the 404 page (1280/375/320), light and dark: no horizontal
  overflow, no content past the right edge, navigation on one line, no /play/ request, no script errors.
- L8 (2026-11-05): T4 docs and product pages. `starlight.css` rewritten: every `--sl-*` colour on tokens in both themes,
  header on a 2px ink rail with the lockup and a "Docs" legend, mono sidebar group legends, current page seated (amber
  tint and edge), display-face h1/h2, tables as parts lists, pagination, search, TOC and asides from tokens; Mermaid
  recolouring kept. Overrides in `site/src/components/docs/`: `PageTitle` (family icon and name plus sidebar group above
  the title, from a slug mapping in `families.mjs`; API pages by namespace; socket cards under Graphics, Audio, Input,
  Tile maps and collision, Backends, with SDL_GPU `next` and Box2D an empty dashed `repo` bay) and `MarkdownContent`
  (written statuses rendered as status tags, outside code and headings). Expressive Code (`site/ec.config.mjs`): one
  dark theme on graphite in both themes, the CodeLabel strip (file left, language right via a small plugin, 2px ink
  rail), no tabs or terminal chrome, syntax contrast lifted to 5.5:1. `/platforms/` (six platform modules under one
  rail, parts lists, `runtimeNext` as a next-version row), `/roadmap/` (Present board: available and next seated;
  Future board on a dashed rail: repository items dashed, planned dotted by track; the 3D and editor sentence corrected)
  and `/404` (empty dashed socket) redrawn on T5's data. Checks: `npm ci`; `SITE_DEMOS=none npm run build` (check-dist
  complete); `npm test` 56/56; snippets 104; contrast 198/198; `node tests/browser.mjs --browsers chrome --suites
  layout` all checks passed (diagrams themed). Screenshots of every changed page, light and dark, at 1280/768/375 and
  320 for platforms and roadmap: no horizontal overflow.
- L9 (2026-11-05): T3 demos and players. `/demos/` presents the games as proof ("Proof, built with MicroEngine"): T2's
  `DemoCard` under a rail, each with family markers of the engine features its code calls
  (`src/components/demos/exercises.mjs` + `Exercises.astro`: saves and achievements, tile maps, particles, tile
  collision, synthesized sound, sound files, input actions, touch controls). Each marker carries the C# patterns that
  must appear in `games/<Id>`; a new `npm test` case fails when one stops matching (RED observed by adding tile
  collision to Trail Duel, which moves on a grid). Lede fixed: keyboard, gamepad or touch; tested in Chrome and Firefox
  on desktop and on emulated phones and tablets; the Starlight description says "desktop and mobile browsers". Demo
  pages: header with the markers linked to the manual; the player is the game-coloured frame (with the numbered
  screenshots and the other demos' cards); downloads as modules with the visitor's system in amber; parts under rails
  like the platforms page. Player: one graphite object in both themes, start plate over a clear poster (no gradient),
  state panels as verdict plates, ink focus ring on amber buttons; behaviour unchanged (same script, selectors,
  states, origin checks). Checks: `SITE_DEMOS=none npm run build` (check-dist complete), `npm test` 59/59, snippets
  104, contrast 198/198. Browser suites (layout, play, touch; Chrome and Firefox) on a build with the v0.4.0 release
  packages, mirrored from the public release into a local folder store without a token, download URLs set to the
  public release addresses: 985 ok, 9 failed, all `firefox phone /docs/*: controls at least 24px — SearchCtrlK` (the
  docs search button, not touched by T3); every demo, play and touch check passed.
- L10 (2026-11-05): T6 verification and close. Fixes: the docs search button is a 44 px magnifier on phones (Firefox
  measured it under 24 px; the browser check was kept); `docs/manual/backends.md` no longer names
  `SdlGraphicsDevice`/`RaylibGraphicsDevice` (internal, non-public types in 0.4.0-alpha.2 and gone on `main`): the
  table says "Internal to the platform" with the next SDK version's `SdlRendererDevice`, `SdlGpuDevice` (when the game
  asks for the GPU device), `RaylibDevice` and `DeviceRenderer`; `Socket.astro` usage shows SDL_GPU seated `next` and
  Box2D as the empty `repo` socket; unused tokens removed (`--color-grid`, `--color-planned`, `--color-exploring`,
  `--color-vision`, `--radius-md`, `--ease-in`, `--ease-in-out`; contrast pairs 198 → 186). Accessibility: axe-core
  4.10.3 (scratch install, injected by puppeteer, not a site dependency) found 8 serious `scrollable-region-focusable`
  issues (landing frame strip and install commands at 375 px, docs tables): frame strips take focus with their caption
  as label, install commands wrap, docs tables and Mermaid diagrams get `tabindex="0"` (`docs/scroll-regions.mjs`,
  unit test), and a new browser check "sideways-scrolling regions are reachable from the keyboard" (RED: 25 failures
  in Chrome; GREEN after). Visual fixes: landing board drawing ("Actions" touched its module; callout 2's leader crossed
  "SDL 3"), sockets in a row now share a height so status lines align (graphics row, kernel bar), empty-socket notes
  in the body face. Design doc opens with "Creative decisions" (choices, philosophy, how to extend). Checks: `npm
  ci`; `SITE_DEMOS=none npm run build` (check-dist complete); `npm test` 60/60; snippets 104; contrast 186/186; build
  with the v0.4.0 release packages from a folder store (check-dist complete, 4 packages); `npm run test:browser`
  all suites, Chrome and Firefox: 1102 ok, 0 failed; axe on 11 routes x light/dark x 1280/375: 0 serious or critical
  (16 moderate left: Expressive Code's unnamed `pre` regions and generated API heading order); keyboard walk:
  skip link first, focus visible on every stop, Play → Fullscreen → Restart → Exit returns focus to Play, Ctrl+K
  search opens, finds and closes; no animation runs under reduced motion. Performance, local server without
  compression, versus `main` at b551c7c: `/` 14 requests, 281.6 → 331.2 KiB (fonts 84.5 → 98.6, CSS 16.8 → 34.9);
  `/demos/neon-rift/` before Play 12 requests, 236.0 → 273.4 KiB, no /play/ request; `/docs/manual/rendering/` 19
  requests, 412.6 → 441.8 KiB. Screenshots of 11 routes at 1280/768/375/320 in both themes: no overflow, no clipped
  content, no small targets, no italic headings, no script errors.
