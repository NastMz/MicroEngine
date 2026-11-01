# MyGameTitle

A game made with [MicroEngine](https://github.com/NastMz/MicroEngine).

```bash
microengine run                  # build and play on this computer
microengine package windows      # or linux, macos, android, ios, ios-simulator, web
microengine doctor               # what this machine can build, and how to install what is missing
```

- `MyGame.csproj` holds the game's identity: name, application id, version, icon (`icon.png`) and orientation.
- `MainGame.cs` loads the shared assets; `Scenes/PlayScene.cs` is the first scene.
- `Content/` holds asset files (images, sounds, music, data), read with `ContentFolder`.
- The scene draws text with MicroEngine's built-in pixel font (`Font`).

Guides: [getting started](https://github.com/NastMz/MicroEngine/blob/main/docs/guide/getting-microengine.md),
[packaging](https://github.com/NastMz/MicroEngine/blob/main/docs/guide/packaging.md),
[making a game](https://github.com/NastMz/MicroEngine/blob/main/docs/making-a-game.md).
