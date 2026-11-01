# MicroEngine.Sdl

The SDL 3 platform of [MicroEngine](https://github.com/NastMz/MicroEngine): window, 2D renderer, keyboard, gamepads,
touch and audio on Windows, Linux, macOS, Android and iOS. Native SDL 3 comes from the `ppy.SDL3-CS` package.

Reference it alone to ship SDL only (no raylib), with your own platform catalog:

```csharp
return GameLauncher.Run(new MainGame(), args, new PlatformCatalog().Add(SdlPlatform.Profile).Add(HeadlessPlatform.Profile));
```

MicroEngine is in **Alpha** (0.x): public APIs may change incompatibly between versions; each change is listed in the
[migration notes](https://github.com/NastMz/MicroEngine/blob/main/docs/migrations.md). MIT licensed.
