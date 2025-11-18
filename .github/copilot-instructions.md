# Copilot Instructions (Universal Engineering Standards)

These instructions define the **quality, architecture, style, security, telemetry, and delivery principles** that Copilot must follow in this repository.  
The goal is to produce **clear, modular, maintainable, scalable, secure, and observable code**, regardless of language or framework.

> Summary: Design for change, fail visibly, measure what you build, and automate what you repeat.

---

## 0. Context Before Writing

1. Review project guidelines (`README`, `CONTRIBUTING`, `ARCHITECTURE`, `SECURITY`, etc.).
2. Follow established naming conventions, folder structures, and formatting rules.
3. Reuse existing modules/utilities before adding new dependencies.
4. Align designs with the repository’s architecture (Clean, Layered, Hexagonal, DDD, etc.).

---

## 1. Architecture and Design

-   **Separation of concerns:** each module/service/class has one clear responsibility.
-   **Unidirectional dependencies:** outer layers depend on inner layers, never the reverse.
-   **Program to abstractions, not concrete implementations.**
-   **Prefer composition over inheritance.**
-   **Favor immutability:** avoid global mutable state and uncontrolled side effects.
-   **Expected errors:** return predictable, structured results.
-   **Unexpected errors:** log context and preserve the root cause.
-   **Events over orchestration:** use domain events for decoupling; apply **Sagas** for multi-step/distributed workflows.
-   Document choices as short, focused ADRs.
-   **Folder structure:** organize by feature or domain, not by type. Create subfolders for complex features. Not a flat structure with hundreds of files.
-   **Modularity:** break large files or classes into smaller, reusable modules.
-   **No monoliths:** avoid single files that mix logic, UI, and configuration.
-   **Avoid circular dependencies:** refactor with interfaces, events, or abstraction layers.
-   **Dependency Injection:** manage dependencies through constructors, factories, or DI containers.
-   **Scalability:** design components to handle growth in data, users, or complexity.
-   **OOP Programming:** Use OOP as the default paradigm unless the project is explicitly functional.

---

## 2. Code Standards

-   **Descriptive, consistent naming;** avoid vague abbreviations.
-   **Small, focused units:** one purpose per function/class/file.
-   **Avoid duplication:** DRY without over-engineering abstractions.
-   **Comments explain “why”,** not “what”.
-   **Single technical language (English)** across code and comments.
-   **External configuration only:** never hardcode parameters or secrets.
-   \*\*Imports:
    -   Do not use deep relative imports (`../../`).
    -   Always use absolute paths or module aliases.
-   **Warnings as errors:** do not ignore or suppress linter/IDE warnings. Fix the root cause.
-   **Function signatures:** keep them simple; avoid excessive parameters. Use objects or data classes for grouping.
-   **Primitive obsession:** avoid overusing primitive types for complex data. Use domain-specific types or classes. For example, use a `UserId` class instead of a plain `int` or `string`.
-   **No circular dependencies:** break via interfaces or events.
-   **Formatter/linter compliance required.**
-   **Avoid control-flow explosion:** replace big `if/else/switch` with patterns:
    -   **State, Strategy, Command, Factory, Polymorphism.**
    -   Aim for extensibility with lower cyclomatic complexity.

---

## 3. Constants, Enums, and Configuration

-   **No magic numbers/strings.**  
    Example: `if (status === 3)` → `if (status === STATUS.APPROVED)`
-   Use constants/enums/dictionaries with semantic names, grouped by domain.
-   Config values via env/config files; validate on startup; provide safe defaults.
-   Avoid a single giant “constants” file; organize by concern.
-   Externalize UI text/messages for localization.

---

## 4. Comments, Logging, and Messages

-   **Comments:** concise, technical, rationale-focused; no emojis or colloquialisms.
-   **Logging:** structured with levels (`trace|debug|info|warn|error`); no `print/console.log` in production.
-   Include correlation IDs and relevant context.
-   Never log secrets or PII.
-   **TODO/FIXME** must include context and issue link (e.g., `// TODO: #123`).

---

## 5. Security and Data

-   Validate and sanitize all inputs at the boundary.
-   Least-privilege, deny-by-default authorization.
-   Secrets never in code/commits; use secret stores/env vars.
-   Only proven crypto libraries; encrypt data in transit/at rest.
-   Audit dependencies for vulns and license compliance.

---

## 6. Performance, Resilience, and Scalability

-   Choose efficient data structures; avoid unnecessary scans/loops.
-   Caching with explicit TTL and stampede protection.
-   Retries with backoff, timeouts, circuit breakers for I/O.
-   Pagination/streaming for large results.
-   Concurrency with safe cancellation; define and document limits.

---

## 7. Error Handling

-   **Expected errors:** structured, predictable results.
-   **Unexpected errors:** internal details logged; user-safe messages exposed.
-   Consistent domain error types (`InvalidInput`, `UserNotFound`, etc.).
-   Uniform schema (code, message, details; stack trace only in debug).

---

## 8. External Contracts (APIs, CLIs, Events)

-   Version all public contracts (`/v1/`, `/v2/`, …).
-   Backward compatibility by default; formal deprecation when breaking.
-   Define payload/rate limits and retention policies.
-   Document error formats and examples.
-   Apply idempotency where appropriate.

---

## 9. Testing

-   Follow the testing pyramid: **unit ≫ integration ≫ end-to-end**.
-   One behavior per test; descriptive names; deterministic and isolated.
-   Mock the edges, not the core; prefer realistic stubs/fakes.
-   Cover edge cases and failure paths; quality over raw percentage.

### 9.1 Test-Driven Development (TDD)

-   **Tests drive the code (RED→GREEN→REFACTOR).**
-   The **code must fail** when behavior is missing; do not bend tests to the implementation.
-   Tests specify observable behavior/contracts; avoid asserting private internals.
-   Control time/randomness/I/O via seams and adapters.
-   For legacy, add **characterization tests** before refactoring.

---

## 10. Observability & Telemetry (OpenTelemetry-First Abstraction)

-   **Abstraction mandate:** all telemetry (**traces, metrics, logs**) **must use OpenTelemetry APIs/SDKs** (or equivalent vendor-neutral facade).
    -   **Do not** call cloud vendor SDKs (Azure, AWS, GCP) directly from business code.
    -   Vendors are configured via **exporters** and/or **collectors** outside the domain layer.
-   **Propagation:** use **W3C Trace Context** (traceparent/tracestate) across boundaries.
-   **Semantic conventions:** consistent span/metric names and attributes; avoid high-cardinality labels.
-   **Span hygiene:** clear operation names, start/end around meaningful work; set status on errors; attach exceptions as events.
-   **Metrics:** correct instruments (counter, up/down counter, histogram, gauge); explicit units.
-   **Logs:** structured, correlated to trace/span IDs.
-   **Resource attributes:** define `service.name`, `service.version`, environment, region.
-   **Export:** prefer **OTLP** to an **OpenTelemetry Collector**; switch vendors via config, not code.
-   **Privacy:** never emit secrets/PII; redact or hash sensitive fields.
-   **Testing:** support in-memory exporters/fakes for tests.

---

## 11. Dependency Management

-   Prefer built-in or internal libraries.
-   Evaluate stability, security, and license before adopting third-party code.
-   Isolate dependencies behind interfaces; pin/lock versions for reproducibility.

---

## 12. Data Access & Repository Design (Modular Repository Pattern)

-   **No “god” repositories** or generic base CRUD for all types.
-   Small, cohesive repositories per aggregate/entity group/bounded context.
-   **CQRS:** separate **write** (invariants/transactions) from **read** (queries/projections).
-   No ORM leakage to upper layers; return domain models/DTOs.
-   **Specification/Criteria** for complex filtering/sorting/paging.
-   Pagination mandatory; explicit batching; explicit transaction boundaries.
-   Idempotent writes where relevant; concurrency via optimistic versioning or equivalent.
-   Cache policy documented; soft vs hard delete explicit.
-   Emit metrics (latency, throughput) and structured logs (no PII).

---

## 13. Project Scaffolding and CLIs (No Manual Bootstrapping)

-   **Do not hand-craft baseline project files** (e.g., `package.json`, `.csproj`, `pyproject.toml`, build configs) or folder structures **by typing them from scratch**.
-   **Always prefer official or de-facto CLI scaffolding tools** (framework/tool initializers) to generate:
    -   project structure, config files, scripts, linters/formatters, test harness, gitignore, and recommended defaults.
-   **Copilot behavior in scaffolding contexts:**
    -   **Ask the user to run the appropriate CLI** (show the exact command and minimal flags).
    -   If multiple CLIs exist, **present the options with pros/cons** and default to the most standard one.
    -   Only propose manual file edits **after** scaffolding is created or when the CLI lacks the needed option.
-   **Reproducibility:** ensure commands are copy-pasteable, idempotent, and include notes on prerequisites (runtime, package manager).
-   **Post-scaffold tasks:** configure aliases/absolute imports, testing setup, lint/format, CI skeleton, and OpenTelemetry bootstrap.

---

## 14. Version Control and Pull Requests

-   Commits small, atomic, and descriptive.
-   PRs include purpose, scope, risks, and rollback plan.
-   Never merge failing CI; follow the repo’s branching strategy.
-   Semantic versioning when applicable.

---

## 15. CI/CD and Automation

-   PR pipelines: lint, format, build, test, security analysis.
-   Main/release: reproducible, signed artifacts; environment-specific config.
-   Automate changelogs, versioning, release tagging.
-   Fail fast; errors must be clear and actionable.

---

## 16. Documentation

-   Each module/feature documents purpose, usage, limitations, and examples.
-   Record key decisions as ADRs; include integration/interoperability notes.
-   Keep docs in sync; update them in the same PR as code changes.

---

## 17. Modularization and Componentization

-   **Always modularize, regardless of size.**
-   Avoid monolithic files mixing logic/UI/config.
-   Even in **vanilla** projects (HTML/JS/Python/etc.):
    -   Split logic into reusable modules/partials/components.
    -   Avoid giant `index.html`/`main.js`; use templates/components to enforce DRY.
-   Componentization prevents spaghetti code and simplifies testing.

---

## 18. Event-Driven Design and Sagas

-   **Use events for decoupling** domain behaviors; one event per meaningful change.
-   Avoid single functions/services handling multiple workflows.
-   Use pub/sub or event buses; name events explicitly; ensure **idempotency**.
-   **Sagas (process managers)** coordinate multi-step/distributed workflows.

---

## 19. Editorial Style

-   English only; professional tone.
-   No emojis, jokes, or personal tags.
-   Commit/PR messages must be clear and actionable.
-   Comments concise, neutral, and purposeful.

---

## 20. Copilot Behavior

-   Favor clarity, modularity, maintainability, and event-driven design.
-   Replace excessive conditionals with patterns (State/Strategy/Command/Factory/Polymorphism).
-   **Telemetry:** instrument with **OpenTelemetry APIs**; export via config/collector; never hardcode vendor SDKs.
-   **Scaffolding:** never write baseline files by hand; **ask the user to run the official CLI** with exact commands.
-   Prefer TDD: propose failing tests first, then minimal implementation, then refactor.
-   Generate **modular repositories** (no generic base CRUD).
-   No deep relative imports; no debug prints in production.
-   Explain non-obvious choices with brief technical comments.
-   Prefer internal/standard libs over new dependencies.
-   When repo rules conflict, follow them and note the reason.
-   Never write code that violates these standards for speed or convenience.
-   Never compromise on quality, security, or maintainability.
-   Always prefer clarity and simplicity over cleverness.
-   Never assume; when in doubt, ask for clarification.
-   Never generate incomplete or placeholder code (e.g., `// TODO`, `function foo() {}`).
-   Strive to make every contribution a lasting improvement.
-   Always think long-term, not just short-term fixes.
-   Aim to leave the codebase better than you found it.
-   Document your thought process and decisions clearly.
-   When generating code, always consider the full lifecycle: development, testing, deployment, maintenance.
-   Remember that code is read more often than it is written.
-   Strive for excellence in every line of code.
-   Always align with the project’s established architecture and patterns.
-   Never introduce breaking changes without a clear migration path.
-   Always validate and sanitize all inputs.
-   Always handle errors gracefully and predictably.
-   Always log sufficient context for debugging without exposing sensitive data.
-   Always write tests that are meaningful and maintainable.
-   Always ensure observability is in place.
-   Always follow best practices for dependency management.
-   Always keep documentation up to date.
-   Always adhere to the project’s CI/CD standards.
-   Always follow the project’s version control and branching strategies.
-   Always respect the project’s coding style and formatting rules.
-   Always prioritize security in every aspect of the code.
-   After completing the code, review if is needed to update the version, changelog or roadmap files.
    -   The roadmap must be updated in the sections: version, phase, Summary of Milestones and Revision History.
    -   The changelog must include a new entry with the version, date, author, and a summary of changes.
    -   The version must be updated in the project file (`version.json`) according to semantic versioning principles.

---

## 21. Pull Request Validation Checklist

-   [ ] Code is modular/componentized (no monolith files).
-   [ ] Event-driven patterns applied; Sagas used when needed.
-   [ ] Repositories are modular/cohesive; CQRS respected; no ORM leakage.
-   [ ] **OpenTelemetry-first:** traces/metrics/logs via OTel abstraction; vendor exporters configured, not hardcoded.
-   [ ] **Project scaffolding used CLI** (no hand-crafted baseline files); commands documented.
-   [ ] Tests followed TDD (failing first); deterministic and relevant.
-   [ ] No deep relative imports; no debug/print statements.
-   [ ] Conditionals replaced with appropriate patterns where applicable.
-   [ ] Constants/enums replace magic values.
-   [ ] Structured error handling and logging.
-   [ ] Pagination/criteria-based queries implemented.
-   [ ] Inputs validated; secrets protected.
-   [ ] Observability present (logs, metrics, traces, health).
-   [ ] Documentation and ADRs updated.

## Final Note

-   Never compromise on these standards for speed or convenience.
-   Always aim for long-term maintainability and quality.
-   When in doubt, prefer clarity and simplicity over cleverness.
-   If a rule conflicts with project-specific guidelines, follow the project rules and document the reason.
-   Strive to make every contribution a lasting improvement.

