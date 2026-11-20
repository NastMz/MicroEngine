# MicroEngine.Aseprite

Files saved by the [Aseprite](https://www.aseprite.org/) editor for
[MicroEngine](https://github.com/NastMz/MicroEngine): `.aseprite` and `.ase` files load straight into sprite atlases
through the game's assets (folders, archives and browser packages alike), with no export step. Frames are the
picture Aseprite shows (visible layers flattened with their blend modes), frame tags become animation clips with each
frame's duration, direction and repeat, and slices become named sprites and pivots.

```csharp
var hero = Assets.Aseprite("characters/hero.aseprite");   // an ordinary SpriteAtlas
var animator = hero.CreateAnimator("idle");                // every tag is a clip
// In Update: animator.Play(moving ? "run" : "idle"); animator.Update(dt);
// In Draw:   renderer.Draw(animator.Frame, position);
```

Frames are named "hero 0", "hero 1"... like Aseprite's own sheet export. The first slice with a pivot anchors every
frame, so a character's feet are set once in the editor. Files are parsed by
[AsepriteDotNet](https://github.com/AristurtleDev/AsepriteDotNet), which this package's public API never exposes.

MicroEngine is in **Alpha** (0.x): public APIs may change incompatibly between versions; each change is listed in the
[migration notes](https://github.com/NastMz/MicroEngine/blob/main/docs/migrations.md). MIT licensed; AsepriteDotNet
is under MIT.
