# Contributing to MicroEngine

Thank you for your interest in contributing to MicroEngine!

This document provides guidelines for contributing to the project, including code standards, architecture rules,
submission process, and quality requirements.

---

## Table of Contents

1. [Code of Conduct](#code-of-conduct)
2. [Getting Started](#getting-started)
3. [Development Workflow](#development-workflow)
4. [Architecture and Design Principles](#architecture-and-design-principles)
5. [Code Standards](#code-standards)
6. [Testing Requirements](#testing-requirements)
7. [Documentation Requirements](#documentation-requirements)
8. [Pull Request Process](#pull-request-process)
9. [Commit Message Guidelines](#commit-message-guidelines)
10. [Review Process](#review-process)

---

## Code of Conduct

All contributors must adhere to the [Code of Conduct](CODE_OF_CONDUCT.md).  
We are committed to maintaining a respectful, inclusive, and collaborative environment.

---

## Getting Started

### Prerequisites

- **.NET 9.0 SDK** or later
- **Git** for version control
- **Visual Studio 2022**, **Rider**, or **VS Code** with C# extension
- Familiarity with C# and game engine architecture

### Clone the Repository

```bash
git clone https://github.com/NastMz/MicroEngine.git
cd MicroEngine
```

### Build the Solution

```bash
dotnet build MicroEngine.sln
```

### Run Tests

```bash
dotnet test
```

---

## Development Workflow

### 1. Create a Feature Branch

```bash
git checkout -b feature/your-feature-name
```

Use descriptive branch names:

- `feature/` for new features
- `fix/` for bug fixes
- `refactor/` for code improvements
- `docs/` for documentation changes

### 2. Make Your Changes

Follow the [Architecture and Design Principles](#architecture-and-design-principles) and
[Code Standards](#code-standards).

### 3. Write Tests

All new features and bug fixes must include tests. See [Testing Requirements](#testing-requirements).

### 4. Update Documentation

Update relevant documentation in `docs/` and code comments.

### 5. Commit Your Changes

Follow [Commit Message Guidelines](#commit-message-guidelines).

### 6. Submit a Pull Request

See [Pull Request Process](#pull-request-process).

---

## Architecture and Design Principles

MicroEngine follows strict architectural rules defined in `.github/copilot-instructions.md` and `docs/ARCHITECTURE.md`.

### Core Principles

#### 1. Dimension-Agnostic Design

The engine core must not assume 2D-specific constraints.  
All systems must be extensible to 3D in future versions.

**Requirements:**

- Use `Vector3` even if Z is unused in 2D contexts
- Design transform hierarchies to support any dimensionality
- Abstract camera systems from projection types
- Avoid hardcoded 2D assumptions in core logic

#### 2. Strict Layered Architecture

```
Game Layer (user code)
    ↓
Engine Core (platform-independent logic)
    ↓
Backends (rendering, input, audio implementations)
```

**Rules:**

- ✓ Upper layers depend on lower layers
- ✗ Lower layers never depend on upper layers
- ✗ Core never imports backend-specific code
- ✓ Backends implement interfaces defined by core

#### 3. Separation of Concerns

Each subsystem has one clear responsibility:

- **ECS:** Entity-component-system framework
- **Scene Manager:** Game state transitions
- **Resource Manager:** Asset lifetime and caching
- **Render Backend:** Drawing primitives and textures
- **Input Backend:** User interaction
- **Audio Backend:** Sound playback
- **Physics:** Collision detection and spatial queries

#### 4. Backend Independence

The core defines interfaces:

- `IRenderBackend`
- `IInputBackend`
- `IAudioBackend`

Backends are implemented as separate modules and can be swapped without modifying core code.

#### 5. Event-Driven Design

Use domain events for decoupling behaviors. Avoid monolithic functions handling multiple workflows.

- Use pub/sub or event buses
- Name events explicitly (e.g., `EntityDestroyed`, `SceneLoaded`)
- Ensure idempotency where applicable

---

## Code Standards

### General Guidelines

- **Language:** C# with .NET 9.0+ features
- **Style:** Follow Microsoft C# Coding Conventions
- **Naming:** PascalCase for public members, camelCase for private fields with `_` prefix
- **File Organization:** One class per file (except nested types)

### Code Quality Rules

#### 1. Clarity and Maintainability

- Descriptive, consistent naming (avoid vague abbreviations)
- Small, focused methods (one purpose per method)
- Comments explain "why", not "what"
- English only for code and comments

#### 2. No Magic Values

```csharp
// Bad
if (status == 3) { }

// Good
if (status == Status.Approved) { }
```

Use constants, enums, or configuration for all literal values.

#### 3. Avoid Deep Nesting

Replace excessive conditionals with patterns:

- State Pattern
- Strategy Pattern
- Command Pattern
- Factory Pattern
- Polymorphism

#### 4. Dependency Injection

Manage dependencies through constructors or DI containers. Avoid static dependencies.

```csharp
// Bad
public class GameEngine
{
    private readonly RenderBackend _renderer = new RaylibRenderer();
}

// Good
public class GameEngine
{
    private readonly IRenderBackend _renderer;

    public GameEngine(IRenderBackend renderer)
    {
        _renderer = renderer;
    }
}
```

#### 5. Error Handling

- Use explicit error types (no generic exceptions)
- Log unexpected errors with context
- Return structured results for expected errors
- Never swallow exceptions silently

```csharp
// Expected errors
public Result<Texture> LoadTexture(string path)
{
    if (!File.Exists(path))
        return Result.Failure<Texture>(new ResourceNotFoundError(path));

    // Load texture...
}

// Unexpected errors
catch (Exception ex)
{
    _logger.Error(ex, "Failed to load texture from {Path}", path);
    throw;
}
```

#### 6. Immutability

- Prefer readonly fields and properties
- Avoid global mutable state
- Use immutable data structures where appropriate

#### 7. No Primitive Obsession

Use domain-specific types instead of primitives:

```csharp
// Bad
public void MoveEntity(int entityId, float x, float y) { }

// Good
public void MoveEntity(EntityId entityId, Vector2 position) { }
```

---

## Testing Requirements

### Test Strategy

Follow the testing pyramid: **unit ≫ integration ≫ end-to-end**

#### Unit Tests

- Test one behavior per test
- Descriptive test names (e.g., `LoadTexture_WhenFileNotFound_ReturnsError`)
- Deterministic and isolated
- Mock external dependencies (I/O, time, randomness)

```csharp
[Fact]
public void AddComponent_WhenEntityExists_AddsComponentSuccessfully()
{
    // Arrange
    var entity = _world.CreateEntity();
    var component = new TransformComponent();

    // Act
    entity.AddComponent(component);

    // Assert
    Assert.True(entity.HasComponent<TransformComponent>());
}
```

#### Integration Tests

Test interactions between subsystems (e.g., ECS + Scene Manager).

#### Test Coverage

- All public APIs must have tests
- Cover edge cases and failure paths
- Quality over raw percentage
- Avoid testing implementation details

### Test-Driven Development (TDD)

When adding new features:

1. **Write a failing test** (RED)
2. **Implement minimal code** to pass (GREEN)
3. **Refactor** while keeping tests green (REFACTOR)

---

## Documentation Requirements

### Code Documentation

- All public APIs must have XML documentation comments
- Include usage examples for complex APIs
- Document thread safety, ownership, and lifecycle

```csharp
/// <summary>
/// Loads a texture from the specified file path.
/// </summary>
/// <param name="path">Absolute path to the texture file.</param>
/// <returns>Result containing the loaded texture or an error.</returns>
/// <exception cref="ArgumentNullException">Thrown when path is null.</exception>
public Result<Texture> LoadTexture(string path) { }
```

### Architectural Documentation

When introducing new subsystems or patterns:

- Update `docs/ARCHITECTURE.md`
- Create module documentation in `docs/MODULES/`
- Record design decisions as ADRs (Architecture Decision Records)

### Update Existing Docs

Keep documentation synchronized with code changes in the same PR.

---

## Pull Request Process

### Before Submitting

- [ ] All tests pass (`dotnet test`)
- [ ] Code follows style guidelines
- [ ] No compiler warnings
- [ ] Documentation updated
- [ ] Commit messages follow guidelines

### PR Title and Description

**Title format:**

```
[Type] Brief description
```

Types: `Feature`, `Fix`, `Refactor`, `Docs`, `Test`, `Chore`

**Description template:**

```markdown
## Summary

Brief description of changes.

## Motivation

Why is this change needed?

## Changes

- Detailed list of changes
- Include breaking changes if any

## Testing

How was this tested?

## Risks

Any potential risks or side effects?

## Rollback Plan

How can this be reverted if needed?
```

### Review Checklist

Reviewers will check:

- [ ] Code follows architecture principles
- [ ] Tests are meaningful and pass
- [ ] No unnecessary dependencies added
- [ ] Error handling is correct
- [ ] Documentation is complete
- [ ] No performance regressions
- [ ] Breaking changes are justified and documented

---

## Commit Message Guidelines

### Format

```
<type>: <subject>

<body>

<footer>
```

### Type

- `feat`: New feature
- `fix`: Bug fix
- `refactor`: Code restructuring
- `docs`: Documentation changes
- `test`: Test additions or updates
- `chore`: Build, tooling, or maintenance

### Subject

- Imperative mood ("Add feature", not "Added feature")
- No period at the end
- Maximum 72 characters

### Body (optional)

- Explain **why** the change was made
- Include context and rationale
- Wrap at 72 characters

### Footer (optional)

- Reference issues: `Closes #123`
- Note breaking changes: `BREAKING CHANGE: description`

### Examples

```
feat: add 3D transform support to ECS

Extends the transform component to support 3D coordinates while maintaining
backward compatibility with 2D systems.

Closes #45
```

```
fix: prevent resource leak in texture disposal

Resources were not being released when textures were unloaded, causing
memory leaks in long-running sessions.

Closes #67
```

---

## Review Process

### Timeline

- Initial review: within 48 hours
- Feedback cycle: iterative until approved
- Merge: after approval from at least one maintainer

### Feedback

- Constructive and respectful
- Focus on code quality and architecture
- Explain reasoning for requested changes

### Approval

PRs require:

- ✓ All CI checks passing
- ✓ At least one approval from a maintainer
- ✓ No unresolved conversations
- ✓ Up-to-date with main branch

---

## Additional Resources

- [Architecture Documentation](docs/ARCHITECTURE.md)
- [Engine Design Document](docs/ENGINE_DESIGN_DOCUMENT.md)
- [Core Requirements](docs/CORE_REQUIREMENTS.md)
- [Module Documentation](docs/MODULES/)
- [Roadmap](docs/ROADMAP.md)

---

## Questions?

If you have questions about contributing:

1. Check existing documentation
2. Search existing issues
3. Open a discussion on GitHub
4. Contact maintainers

---

**Thank you for contributing to MicroEngine!**

We appreciate your efforts to make this project better.

---

**Last Updated:** November 2025  
**Version:** v0.13.0 (Dev)
