# MicroEngine — Versioning & Release Strategy

**Version:** v0.13.0 (Dev)  
**Status:** Reference  
**Author:** Kevin Martínez  
**Last Updated:** November 2025

---



## Overview

MicroEngine uses a strict and automated versioning model based on:

- **Semantic Versioning (SemVer 2.0)** for predictable version numbers
- **Nerdbank.GitVersioning (NBGV)** for Git-based version stamping
- **NuGet multi-package distribution** for modular consumption
- **Tag-driven release workflow** for reproducible releases

This document explains how versions are defined, generated, and published.

**Related Documents:**

- 📘 [Architecture](ARCHITECTURE.md) — Engine structure and design
- 📘 [Contributing](../CONTRIBUTING.md) — Contribution guidelines
- 📘 [Roadmap](ROADMAP.md) — Development timeline

---

## Table of Contents

1. [Versioning Model](#1-versioning-model)
2. [Release Channels](#2-release-channels)
3. [Package Naming](#3-package-naming)
4. [Nerdbank GitVersioning (NBGV)](#4-nerdbank-gitversioning-nbgv)
5. [Git Tagging Rules](#5-git-tagging-rules)
6. [CI/CD Integration](#6-cicd-integration)
7. [Branching Strategy](#7-branching-strategy)
8. [Versioning Rules for Backwards Compatibility](#8-versioning-rules-for-backwards-compatibility)
9. [Version Validation](#9-version-validation)
10. [Migration Path](#10-migration-path)
11. [Deprecation Policy](#11-deprecation-policy)
12. [Examples](#12-examples)
13. [Troubleshooting](#13-troubleshooting)
14. [Summary](#14-summary)

---

## 1. Versioning Model

MicroEngine uses **Semantic Versioning 2.0**:

```text
MAJOR.MINOR.PATCH
```

### Version Components

#### MAJOR

Breaking API changes or architectural changes.

#### MINOR

New features added in a backward-compatible way.

#### PATCH

Bug fixes, stability improvements, or internal changes.
```text
vX.Y.Z
```

These trigger:

- NuGet package builds
- GitHub Release publication
- Documentation updates
- Changelog generation

### Prereleases

For development snapshots:

```text
vX.Y.Z-alpha.N
vX.Y.Z-beta.N
vX.Y.Z-rc.N
```

Prereleases are generated automatically by NBGV depending on branch and commit height.

**Prerelease Guidelines:**

- **Alpha:** Early testing, API unstable, features incomplete
- **Beta:** Feature complete, API frozen, testing phase
- **RC (Release Candidate):** Production-ready candidate, no new features

### Nightly Builds

Can be published from the `develop` or equivalent branch using auto-incrementing CI builds.

**Naming Convention:**

```text
vX.Y.Z-dev.{CommitHeight}+{CommitSha}
```

---

## 3. Package Naming

MicroEngine is distributed as **multiple NuGet packages**:

```text
MicroEngine.Core
MicroEngine.Backend.Raylib          (example)
MicroEngine.Backend.OpenGL          (future)
MicroEngine.Backend.SDL             (future)
MicroEngine.Backend.Vulkan          (future)
MicroEngine.Tools                   (future utilities)
MicroEngine.Editor                  (optional future project)
```

### Package Versioning Strategy

- **All packages share the same version number** to ensure consistency
- Packages may be released independently if only one module changes
- Version number is always incremented project-wide
- Breaking changes in any package trigger a MAJOR version bump for all packages

### Package Metadata

Each package includes:

- `PackageId`: Unique identifier
- `Version`: Semantic version
- `Authors`: Kevin Martínez
- `Description`: Package-specific functionality
- `PackageLicenseExpression`: MIT
- `PackageProjectUrl`: Repository URL
- `RepositoryUrl`: GitHub repository
- `PackageTags`: Relevant keywords for discovery

---

## 4. Nerdbank GitVersioning (NBGV)

NBGV automatically generates version numbers based on:

- Git tags
- Commit height
- Current branch
- Prerelease names
- Public release references

### Version JSON File

The root of the repository contains:

```text
version.json
```

**Example Configuration:**

```json
{
  "version": "0.1",
  "publicReleaseRefSpec": [
    "^refs/heads/main$",
    "^refs/tags/v\\d+\\.\\d+\\.\\d+$"
  ],
  "nugetPackageVersion": {
    "semVer": 2
  },
  "cloudBuild": {
    "buildNumber": {
      "enabled": true
    }
  }
}
```

### Configuration Explanation

- `"version": "0.1"`

  - Base version for all builds
  - Represents MAJOR.MINOR; PATCH is auto-calculated

- `"publicReleaseRefSpec"`

  - Defines which branches/tags generate **stable** versions
  - `main` branch + tags like `v1.0.0`
  - Ensures only authorized refs produce public releases

- `"semVer": 2`

  - Ensures NuGet compatibility with SemVer 2.0
  - Supports modern prerelease and build metadata formats

- `"cloudBuild"`
  - Enables CI/CD integration
  - Synchronizes build numbers with pipeline systems

### NBGV CLI Commands

```bash
# Install NBGV globally
dotnet tool install -g nbgv

# Get current version
nbgv get-version

# Create a new version tag
nbgv tag

# Prepare a new release
nbgv prepare-release
```

---

## 5. Git Tagging Rules

Stable releases must be tagged manually:

```bash
git tag v0.1.0
git push --tags
```

### Tag Naming Convention

- **Stable Release:** `v{MAJOR}.{MINOR}.{PATCH}` (e.g., `v1.0.0`)
- **Prerelease:** `v{MAJOR}.{MINOR}.{PATCH}-{LABEL}.{N}` (e.g., `v1.0.0-beta.1`)

### Tag Triggers

When a tag is pushed, the CI pipeline:

- Compiles the engine
- Runs all tests (unit, integration, smoke)
- Builds NuGet packages
- Publishes packages to NuGet.org
- Generates a GitHub Release with changelog
- Updates documentation (if configured)

### Tag Protection

- Only maintainers can create tags
- Tags follow strict naming convention
- CI validates tag format before building
- Invalid tags are rejected

---

## 6. CI/CD Integration

### Building Packages

A GitHub Actions workflow produces `.nupkg` files for:

- `MicroEngine.Core`
- All backends under `Engine.Backend.*`
- All tools and utilities

### Publishing Packages

When a tag `vX.Y.Z` is pushed:

- The CI pipeline publishes to NuGet.org
- Documentation is built and deployed
- Changelog is generated from commits
- GitHub Release is created with artifacts

**Publishing Command:**

```bash
dotnet nuget push **/*.nupkg --api-key $NUGET_KEY --source https://api.nuget.org/v3/index.json
```

### CI Workflow Steps

1. **Validate:** Check code formatting, linting, and tests
2. **Build:** Compile all projects
3. **Test:** Run test suite with coverage
4. **Package:** Create NuGet packages
5. **Publish:** Push to NuGet (only for tags)
6. **Release:** Create GitHub Release with changelog

### Environment Variables

- `NUGET_KEY`: API key for NuGet.org
- `GITHUB_TOKEN`: Automatic token for releases
- `BUILD_NUMBER`: CI build number

---

## 7. Branching Strategy

Recommended Git branching model:

```text
main      → stable releases only
develop   → active development (prereleases)
feature/* → short-lived branches for specific tasks
backend/* → development of new backends
hotfix/*  → urgent fixes for production
```

### Version Generation by Branch

- **main:** Stable versions (`1.0.0`)
- **develop:** Alpha prereleases (`1.1.0-alpha.5`)
- **feature/\*:** Dev builds (`1.1.0-dev.42+abc123`)
- **backend/\*:** Dev builds with branch name
- **hotfix/\*:** RC builds (`1.0.1-rc.1`)

### Branch Protection

- `main` requires PR approval and passing CI
- Direct commits to `main` are forbidden
- Tags can only be created from `main`

---

## 8. Versioning Rules for Backwards Compatibility

### Before 1.0.0

- API breaking changes are allowed
- MINOR updates may include breaking changes
- PATCH still reserved for fixes
- No compatibility guarantees
- Experimental features may be removed

### After 1.0.0

- All breaking changes require a MAJOR bump
- MINOR must be additive only
- PATCH must not change behavior beyond fixing issues
- Deprecations must follow the deprecation policy
- Public APIs are frozen unless breaking version is released

### API Stability Guarantees

- **Public API:** Stable after 1.0.0, breaking changes only in MAJOR
- **Internal API:** No guarantees, may change in MINOR
- **Experimental API:** Marked explicitly, no guarantees

---

## 9. Version Validation

### Pre-Commit Validation

- Enforce conventional commit messages
- Validate code formatting
- Run linters

### Pre-Tag Validation

- Ensure all tests pass
- Validate CHANGELOG.md is updated
- Confirm version.json is correct
- Check for breaking changes

### Post-Release Validation

- Verify package published successfully
- Validate package metadata
- Test package installation
- Confirm documentation is updated

---

## 10. Migration Path

When releasing breaking changes (MAJOR version bump):

### Migration Guide Requirements

1. **Identify Breaking Changes:** Document all API changes
2. **Provide Examples:** Show before/after code
3. **Offer Alternatives:** Suggest replacement APIs
4. **Create Scripts:** Automated migration tools when possible
5. **Test Migration:** Validate upgrade path with real projects

### Version Support Policy

- **Current MAJOR:** Full support, active development
- **Previous MAJOR:** Security fixes only, 6 months after new MAJOR
- **Older Versions:** No support

---

## 11. Deprecation Policy

### Deprecation Process

1. **Announce:** Mark API with `[Obsolete]` attribute
2. **Document:** Explain why and provide alternatives
3. **Timeline:** Minimum 1 MINOR version before removal
4. **Remove:** Only in next MAJOR version

### Deprecation Levels

- **Soft Deprecation:** Warning, functionality intact
- **Hard Deprecation:** Error, requires code changes
- **Removal:** API completely removed

**Example:**

```csharp
[Obsolete("Use NewMethod instead. This will be removed in v2.0.0")]
public void OldMethod() { }
```

---

## 12. Examples

### Example 1: Normal Development

**Scenario:** Developer commits to `develop` branch

**Result:**

- Automatic version: `0.1.0-alpha.21`
- Published to NuGet as prerelease
- Available for testing

### Example 2: Tagging a Stable Release

**Scenario:** Maintainer creates release tag

**Steps:**

```bash
git checkout main
git merge develop --no-ff
git tag v0.1.0
git push origin main --tags
```

**Result:**

- Version: `0.1.0`
- Published to NuGet as stable
- GitHub Release created

### Example 3: Hotfix

**Scenario:** Critical bug in production

**Steps:**

```bash
git checkout -b hotfix/critical-fix main
# Fix the bug
git commit -m "fix: critical issue in renderer"
git checkout main
git merge hotfix/critical-fix --no-ff
git tag v0.1.1
git push origin main --tags
```

**Result:**

- Version: `0.1.1`
- Published immediately
- Backported to develop if needed

### Example 4: Feature Branch

**Scenario:** Developer works on new ECS feature

**Result:**

- Version: `0.2.0-dev.15+abc1234`
- Not published to NuGet
- Available for local testing only

---

## 13. Troubleshooting

### Common Issues

#### Version Mismatch

**Problem:** NBGV generates unexpected version

**Solution:**

```bash
# Check current version
nbgv get-version

# Verify version.json
cat version.json

# Check git tags
git tag -l
```

#### Tag Already Exists

**Problem:** Tag already pushed

**Solution:**

```bash
# Delete local tag
git tag -d v0.1.0

# Delete remote tag
git push origin :refs/tags/v0.1.0

# Create new tag
git tag v0.1.0
git push --tags
```

#### Package Not Published

**Problem:** CI succeeded but package missing

**Solution:**

- Check NuGet API key is valid
- Verify package name doesn't conflict
- Review CI logs for errors
- Ensure tag matches expected format

---

## 14. Summary

MicroEngine uses a versioning system designed for:

- **Full automation:** No manual version updates
- **Consistency across packages:** All modules share version
- **Smooth prerelease flow:** Alpha, Beta, RC support
- **Clean stable releases:** Tag-driven production builds
- **Strict adherence to SemVer:** Predictable compatibility
- **Reproducibility:** Git-based version source of truth
- **Transparency:** Clear migration paths and deprecation

All versions are derived from Git state via Nerdbank.GitVersioning, ensuring reproducibility, correctness, and transparency throughout the entire release lifecycle.
