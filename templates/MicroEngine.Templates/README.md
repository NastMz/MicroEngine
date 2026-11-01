# MicroEngine.Templates

`dotnet new` templates for [MicroEngine](https://github.com/NastMz/MicroEngine), a code-first 2D game engine for .NET.

```bash
dotnet new install MicroEngine.Templates
dotnet new microengine-game -n MyGame --app-id com.example.mygame
cd MyGame
dotnet run
```

The `microengine` tool (`dotnet tool install -g MicroEngine.Cli`) runs and packages the game:
`microengine package windows|linux|macos|android|ios|web`. Alpha: APIs may change between 0.x versions.
