# Migration notes

What changes between SDK versions and how to update a game. MicroEngine is in Alpha ([SDK status](guide/sdk.md)):
a version may change public APIs incompatibly, and when it does, the change is listed here with the version that makes
it. Newest first.

## 0.2.0-alpha

The first versioned SDK. Nothing a game calls was removed or renamed; a game written for the unversioned engine builds
unchanged.

- **Versions.** The SDK version is `MicroEngineVersion` in `Directory.Build.props`; engine assemblies carry it. Give your
  game its own `<Version>` in its project file.
- **`--report`.** A new launcher argument that prints the platform that runs and whether audio opened.
- **Native library errors.** A backend whose native library the system cannot load (for example SDL on a Linux with an
  older glibc) now ends the run with `error: platform '<name>' could not load its native library` and exit code 1
  instead of an unhandled exception. A host that caught `DllNotFoundException` around `GameLauncher.Run` no longer sees it.
- **macOS bundles.** `DesktopLauncher` reads content from `<App>.app/Contents/Resources/Content` when the executable runs
  inside an application bundle and has no `Content` folder next to it.
- **Content folders.** If your project copies `Content/**` to its output and keeps C# files there (as the demos did),
  exclude them: `<None Include="Content/**" Exclude="Content/**/*.cs" CopyToOutputDirectory="PreserveNewest" />`.
