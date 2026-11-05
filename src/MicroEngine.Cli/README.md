# MicroEngine.Cli

`microengine`, the command-line tool of [MicroEngine](https://github.com/NastMz/MicroEngine): create, run, build and
package games for Windows, Linux, macOS, Android, iOS and the web.

```bash
dotnet tool install -g MicroEngine.Cli --prerelease
microengine new MyGame
cd MyGame
microengine run
microengine package windows      # linux, macos, android, ios, ios-simulator, web
microengine doctor               # what this machine can build
microengine shaders              # compile Shaders/*.hlsl ahead of time (Windows)
```

It drives the .NET SDK and each system's own tools (Android SDK, Xcode, codesign); signing keys come from environment
variables and are never stored or printed. Guide: [packaging](https://github.com/NastMz/MicroEngine/blob/main/docs/guide/packaging.md).

MicroEngine is in **Alpha** (0.x): public APIs may change incompatibly between versions; each change is listed in the
[migration notes](https://github.com/NastMz/MicroEngine/blob/main/docs/migrations.md). MIT licensed.
