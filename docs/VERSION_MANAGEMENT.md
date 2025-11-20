version.json
    ↓
Nerdbank.GitVersioning (build-time)
    ↓
obj/.../MicroEngine.Core.Version.cs (ThisAssembly class)
    ↓
EngineInfo.cs (public wrapper)
    ↓
Application code (Program.cs, scenes, etc.)
```

---

## Updating Version

1. **Edit `version.json`:**

    ```json
    {
        "version": "0.8.0-alpha",
        "publicReleaseRefSpec": ["^refs/heads/main$"]
    }
    ```

2. **Build the project:**

    ```powershell
    dotnet build
    ```

3. **Done!** Version automatically updated everywhere.

### Semantic Versioning Rules (Pre-1.0.0)

-   **MAJOR.MINOR.PATCH-PRERELEASE**
-   **0.x.y releases:** Minor version increments may introduce breaking changes
-   **Patch increments:** Bug fixes only (no breaking changes)
-   **Pre-release labels:** `alpha`, `beta`, `rc1`, `rc2`, etc.
-   **Stable 1.0.0+:** Breaking changes require major version increment

---

## EngineInfo API

`EngineInfo` provides a clean public interface to version information:

```csharp
using MicroEngine.Core.Engine;

// Version information
string version = EngineInfo.VERSION;        // "0.7.0-alpha"
string fullName = EngineInfo.FullName;      // "MicroEngine v0.7.0-alpha"
string semantic = EngineInfo.SemanticVersion; // "0.7.0"

// Version components
int major = EngineInfo.MAJOR;               // 0
int minor = EngineInfo.MINOR;               // 7
int patch = EngineInfo.PATCH;               // 0
string preRelease = EngineInfo.PRE_RELEASE; // "alpha"

// Version flags
bool isPreRelease = EngineInfo.IsPreRelease; // true
bool isStable = EngineInfo.IsStable;         // false

// Git metadata (from Nerdbank)
string commitId = EngineInfo.GitCommitId;    // "1ab64f76..."
DateTime commitDate = EngineInfo.GitCommitDate; // 2025-01-18 17:00:00
```

---

## Nerdbank.GitVersioning Configuration

Configuration is in `version.json`:

```json
{
    "$schema": "https://raw.githubusercontent.com/dotnet/Nerdbank.GitVersioning/main/src/NerdBank.GitVersioning/version.schema.json",
    "version": "0.7.0-alpha",
    "publicReleaseRefSpec": ["^refs/heads/main$"],
    "cloudBuild": {
        "buildNumber": {
            "enabled": false
        }
    }
}
```

### Key Fields

-   **`version`**: Base version in SemVer format (MAJOR.MINOR.PATCH-PRERELEASE)
-   **`publicReleaseRefSpec`**: Git refs that trigger public releases (main branch)
-   **`cloudBuild.buildNumber.enabled`**: Controls build number in version (disabled for cleaner versions)

---

## Build Integration

Nerdbank.GitVersioning is configured in `Directory.Build.props` (repository root):

```xml
<ItemGroup>
  <PackageReference Include="Nerdbank.GitVersioning" Version="3.7.5">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

This ensures all projects in the solution automatically get version generation.

---

## Troubleshooting

### Version Not Updating

**Problem:** Changed `version.json` but version still shows old value.

**Solution:**

1. Clean build artifacts: `dotnet clean`
2. Rebuild: `dotnet build`
3. Check `obj/Debug/net9.0/MicroEngine.Core.Version.cs` for generated `ThisAssembly` class

### Compilation Errors

**Problem:** `ThisAssembly` not found.

**Solution:**

1. Verify Nerdbank.GitVersioning in `Directory.Build.props`
2. Ensure `version.json` exists in repository root
3. Run `dotnet restore` to restore packages
4. Rebuild project

### Git Metadata Missing

**Problem:** `GitCommitId` is empty or invalid.

**Solution:**

1. Ensure you're in a Git repository: `git status`
2. Ensure at least one commit exists
3. Rebuild project to regenerate with current commit info

---

## Migration from Custom Scripts

**Previous approach (removed in v0.7.0):**

-   PowerShell script (`generate-version.ps1`) to generate `EngineInfo.cs`
-   MSBuild target to run script before build

**Current approach:**

-   Nerdbank.GitVersioning auto-generates `ThisAssembly`
-   `EngineInfo` wraps `ThisAssembly` for clean public API
-   No scripts or manual generation needed

**Benefits of migration:**

-   Standard .NET tooling (no PowerShell dependency)
-   Richer metadata (git commit info, dates)
-   Industry-proven solution
-   Better IDE integration
