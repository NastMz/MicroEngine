# MicroEngine

The backend-independent core of [MicroEngine](https://github.com/NastMz/MicroEngine), a code-first 2D game engine for
.NET: the game loop, scenes, input actions (keyboard, gamepad, touch), the graphics model, audio synthesis, UI,
saves, achievements and a headless platform for tests. It has no native dependency.

It also brings the build targets that give a game its application identity (name, id, version, icon, orientation),
which `microengine package` turns into each system's metadata.

Most games reference a platform package instead, which brings this one: **MicroEngine.Desktop** (SDL 3 and raylib),
or **MicroEngine.Sdl** alone. Start with the template:

```bash
dotnet tool install -g MicroEngine.Cli --prerelease
microengine new MyGame
```

MicroEngine is in **Alpha** (0.x): public APIs may change incompatibly between versions; each change is listed in the
[migration notes](https://github.com/NastMz/MicroEngine/blob/main/docs/migrations.md). MIT licensed.
