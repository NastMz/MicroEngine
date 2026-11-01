# MicroEngine.Desktop

The desktop launcher of [MicroEngine](https://github.com/NastMz/MicroEngine) with its official platforms: SDL 3 (the
default), raylib, SDL's CPU renderer and headless. A game's `Program.cs` is one line:

```csharp
return DesktopLauncher.Run(new MainGame(), args);
```

Players and tests choose another platform with `--platform` or `MICROENGINE_PLATFORM`; `--list-platforms` lists them.

MicroEngine is in **Alpha** (0.x): public APIs may change incompatibly between versions; each change is listed in the
[migration notes](https://github.com/NastMz/MicroEngine/blob/main/docs/migrations.md). MIT licensed.
